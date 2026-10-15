using System.Diagnostics;
using System.Text;

namespace Aster.Sessions.Tmux;

public sealed record TmuxWindowInfo(int Id, string Name, TmuxLayout Layout, int ActivePane);
public sealed record TmuxPaneState(int Columns, int Rows, int CursorX, int CursorY, bool CursorVisible, bool ApplicationCursor, bool Alternate);

/// <summary>Explicit local control-mode client. tmux owns shells; disposing this client only detaches.</summary>
public sealed class TmuxController : IAsyncDisposable
{
    private readonly string _executable, _target;
    private readonly string? _socket;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _commands = new(1, 1);
    private readonly TaskCompletionSource _greeting = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Dictionary<int, TmuxPaneConnection> _panes = [];
    private readonly object _gate = new();
    private Process? _process;
    private Task? _reader, _stderr, _close;
    private TaskCompletionSource<string[]>? _response;
    private Action<string[]>? _apply;
    private Exception? _failure;
    private bool _closing;
    public event EventHandler? LayoutChanged;
    public event EventHandler<Exception>? Faulted;
    public bool IsRunning => _process is { HasExited: false } && !_lifetime.IsCancellationRequested && _failure is null;
    public TmuxController(string target, string? socket = null, string executable = "tmux")
    { TmuxProtocol.Quote(target); _target = target; _socket = socket; _executable = executable == "tmux" && OperatingSystem.IsMacOS()
        ? new[] { "/opt/homebrew/bin/tmux", "/usr/local/bin/tmux", "/usr/bin/tmux" }.FirstOrDefault(File.Exists) ?? executable : executable; }
    public async Task StartAsync(CancellationToken token = default)
    {
        if (_process is not null) throw new InvalidOperationException("Tmux client already started.");
        var start = new ProcessStartInfo(_executable) { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, StandardOutputEncoding = Encoding.Latin1, StandardInputEncoding = new UTF8Encoding(false) };
        if (_socket is not null) { start.ArgumentList.Add("-L"); start.ArgumentList.Add(_socket); }
        start.ArgumentList.Add("-C"); start.ArgumentList.Add("attach-session"); start.ArgumentList.Add("-t"); start.ArgumentList.Add(_target);
        start.Environment.Remove("TMUX"); start.Environment["TERM"] = "xterm-256color";
        _process = Process.Start(start) ?? throw new IOException("Unable to start tmux.");
        _reader = Task.Run(ReadAsync);
        _stderr = Task.Run(async () => { var buffer = new char[4096]; while (await _process.StandardError.ReadAsync(buffer, _lifetime.Token) > 0) { } });
        await _greeting.Task.WaitAsync(TimeSpan.FromSeconds(10), token);
        await CommandAsync("refresh-client -f ignore-size", token: token);
    }
    public TmuxPaneConnection CreatePane(int id)
    {
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
        lock (_gate)
        {
            if (_panes.ContainsKey(id)) throw new InvalidOperationException("Pane already has a frontend.");
            var pane = new TmuxPaneConnection(this, id); _panes.Add(id, pane); return pane;
        }
    }
    internal void RemovePane(TmuxPaneConnection pane) { lock (_gate) if (_panes.GetValueOrDefault(pane.Id) == pane) _panes.Remove(pane.Id); }
    public async Task<IReadOnlyList<TmuxWindowInfo>> WindowsAsync(CancellationToken token = default)
    {
        TmuxWindowInfo[] result = [];
        await CommandAsync("list-windows -F \"#{window_id}\t#{window_name}\t#{window_visible_layout}\t#{pane_id}\"", lines =>
        {
            if (lines.Length > 64) throw new InvalidDataException("Too many tmux windows (maximum 64).");
            result = lines.Select(line =>
            {
                var cells = line.Split('\t'); if (cells.Length != 4) throw new InvalidDataException("Invalid tmux window description.");
                return new TmuxWindowInfo(TmuxProtocol.Id(cells[0], '@'), string.Concat(cells[1].Where(c => !char.IsControl(c)).Take(256)), TmuxLayout.Parse(cells[2]), TmuxProtocol.Id(cells[3], '%'));
            }).ToArray();
            if (result.Sum(w => w.Layout.Panes.Count()) > 64) throw new InvalidDataException("Too many tmux panes (maximum 64 total).");
            foreach (var window in result) ApplyGrid(window.Layout);
        }, token);
        return result;
    }
    private void ApplyGrid(TmuxLayout layout)
    {
        lock (_gate) foreach (var pane in layout.Panes) _panes.GetValueOrDefault(pane.PaneId!.Value)?.SetGrid(pane.Columns, pane.Rows);
    }
    public Task<string[]> SplitAsync(int pane, bool below, CancellationToken token = default) => CommandAsync($"split-window {(below ? "-v" : "-h")} -t %{ValidId(pane)}", token: token);
    public Task<string[]> NewWindowAsync(CancellationToken token = default) => CommandAsync("new-window", token: token);
    public Task<string[]> KillPaneAsync(int pane, CancellationToken token = default) => CommandAsync($"kill-pane -t %{ValidId(pane)}", token: token);
    public Task<string[]> SelectPaneAsync(int pane, CancellationToken token = default) => CommandAsync($"select-pane -t %{ValidId(pane)}", token: token);
    public Task<string[]> ResizeAsync(int columns, int rows, CancellationToken token = default)
    {
        if (columns is < 2 or > 1000 || rows is < 2 or > 500 || columns * rows > 100000) throw new ArgumentOutOfRangeException(nameof(columns));
        return CommandAsync($"refresh-client -C {columns},{rows} -f !ignore-size", token: token);
    }
    private static int ValidId(int id) => id >= 0 ? id : throw new ArgumentOutOfRangeException(nameof(id));
    internal async Task HydrateAsync(TmuxPaneConnection pane, CancellationToken token)
    {
        var state = await CommandAsync($"display-message -p -t %{pane.Id} \"#{{pane_width}} #{{pane_height}} #{{cursor_x}} #{{cursor_y}} #{{cursor_flag}} #{{keypad_cursor_flag}} #{{alternate_on}}\"", token: token);
        var values = state.Single().Split(' ').Select(int.Parse).ToArray();
        if (values.Length != 7 || values[0] is < 1 or > 1000 || values[1] is < 1 or > 500 || values[0] * values[1] > 100000) throw new InvalidDataException("Invalid tmux pane state.");
        var metadata = new TmuxPaneState(values[0], values[1], values[2], values[3], values[4] != 0, values[5] != 0, values[6] != 0);
        // Capture is applied synchronously on the protocol reader before later output
        // notifications. No output block can contain a notification (tmux contract).
        await CommandAsync($"capture-pane -p -e -N -t %{pane.Id}", lines => pane.Hydrate(metadata, lines), token);
    }
    internal async Task SendAsync(int id, ReadOnlyMemory<byte> bytes, CancellationToken token)
    {
        for (var offset = 0; offset < bytes.Length; offset += 512)
        {
            var hex = Convert.ToHexString(bytes.Span.Slice(offset, Math.Min(512, bytes.Length - offset)));
            var command = new StringBuilder($"send-keys -H -t %{id}");
            for (var i = 0; i < hex.Length; i += 2) command.Append(' ').Append(hex, i, 2);
            await CommandAsync(command.ToString(), token: token);
        }
    }
    internal async Task<string[]> CommandAsync(string command, Action<string[]>? apply = null, CancellationToken token = default)
    {
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token); bounded.CancelAfter(TimeSpan.FromSeconds(10));
        await _commands.WaitAsync(bounded.Token);
        try
        {
            if (_failure is not null) throw new IOException("Tmux control client failed.", _failure);
            var response = new TaskCompletionSource<string[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            _apply = apply; _response = response;
            await _process!.StandardInput.WriteLineAsync(command.AsMemory(), bounded.Token); await _process.StandardInput.FlushAsync(bounded.Token);
            try { return await response.Task.WaitAsync(bounded.Token); }
            catch (OperationCanceledException) { Fail(new IOException("Tmux command timed out or was cancelled; detaching to preserve protocol ordering.")); throw; }
        }
        finally { _apply = null; _response = null; _commands.Release(); }
    }
    private async Task ReadAsync()
    {
        try
        {
            var chunk = new char[8192]; var line = new StringBuilder();
            var body = new List<string>(); string? block = null; var blockBytes = 0;
            while (true)
            {
                var count = await _process!.StandardOutput.ReadAsync(chunk, _lifetime.Token); if (count == 0) break;
                for (var i = 0; i < count; i++)
                {
                    var c = chunk[i];
                    if (c != '\n') { if (line.Length >= 1024 * 1024) throw new InvalidDataException("Oversize tmux protocol line."); line.Append(c); continue; }
                    var text = line.ToString(); line.Clear();
                    if (block is not null)
                    {
                        if (text == "%end " + block || text == "%error " + block)
                        {
                            var error = text.StartsWith("%error ", StringComparison.Ordinal);
                            if (!_greeting.Task.IsCompleted)
                            { if (error) throw new IOException(string.Join('\n', body)); _greeting.TrySetResult(); }
                            else if (_response is { } response)
                            {
                                if (error) response.TrySetException(new IOException(string.Join('\n', body)));
                                else { var result = body.ToArray(); _apply?.Invoke(result); response.TrySetResult(result); }
                            }
                            else throw new InvalidDataException("Unexpected tmux response.");
                            block = null; body.Clear(); blockBytes = 0;
                        }
                        else { blockBytes += text.Length + 1; if (blockBytes > 8 * 1024 * 1024) throw new InvalidDataException("Oversize tmux response."); body.Add(Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(text))); }
                        continue;
                    }
                    if (text.StartsWith("%begin ", StringComparison.Ordinal)) { block = text[7..]; continue; }
                    Notify(text);
                }
            }
            if (!_lifetime.IsCancellationRequested) throw new EndOfStreamException("Tmux control client detached.");
        }
        catch (Exception error) when (error is IOException or OperationCanceledException or InvalidOperationException or FormatException) { if (!_lifetime.IsCancellationRequested && !_closing) Fail(error); }
    }
    private void Notify(string text)
    {
        var extended = text.StartsWith("%extended-output ", StringComparison.Ordinal);
        if (extended || text.StartsWith("%output ", StringComparison.Ordinal))
        {
            var start = text.IndexOf(' ') + 1; var end = text.IndexOf(' ', start); if (end < 0) throw new InvalidDataException("Incomplete tmux output.");
            var id = TmuxProtocol.Id(text[start..end], '%'); var payload = end + 1;
            if (extended) { payload = text.IndexOf(" : ", payload, StringComparison.Ordinal); if (payload < 0) throw new InvalidDataException("Invalid extended output."); payload += 3; }
            var bytes = TmuxProtocol.DecodeOutput(text[payload..]);
            lock (_gate) _panes.GetValueOrDefault(id)?.Receive(bytes);
        }
        else if (text.StartsWith("%layout-change ", StringComparison.Ordinal))
        {
            var fields = text.Split(' '); if (fields.Length < 4) throw new InvalidDataException("Invalid tmux layout notification.");
            ApplyGrid(TmuxLayout.Parse(fields[3])); LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (text.StartsWith("%exit", StringComparison.Ordinal)) throw new EndOfStreamException("Tmux detached: " + text);
        else if (text.StartsWith("%layout-change ", StringComparison.Ordinal) || text.StartsWith("%window-", StringComparison.Ordinal) || text.StartsWith("%session-", StringComparison.Ordinal) || text == "%sessions-changed") LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Fail(Exception error)
    {
        if (Interlocked.CompareExchange(ref _failure, error, null) is not null) return;
        _greeting.TrySetException(error); _response?.TrySetException(error);
        lock (_gate) foreach (var pane in _panes.Values) pane.Fail(error);
        Faulted?.Invoke(this, error);
        _lifetime.Cancel();
        try { if (_process is { HasExited: false }) _process.Kill(); } catch (InvalidOperationException) { }
    }
    public ValueTask DisposeAsync() => new(_close ??= CloseAsync());
    private async Task CloseAsync()
    {
        if (_process is null) return;
        _closing = true;
        if (IsRunning) try { await CommandAsync("detach-client"); } catch (Exception error) when (error is IOException or OperationCanceledException) { }
        _lifetime.Cancel();
        try { await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)); } catch (TimeoutException) { if (!_process.HasExited) _process.Kill(); await _process.WaitForExitAsync(); }
        if (_reader is not null) await _reader;
        if (_stderr is not null) try { await _stderr; } catch (OperationCanceledException) { }
        lock (_gate) foreach (var pane in _panes.Values.ToArray()) pane.End();
        _process.Dispose(); _process = null;
    }
}
