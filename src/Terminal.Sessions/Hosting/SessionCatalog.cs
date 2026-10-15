using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text.Json;

namespace Aster.Sessions.Hosting;

public sealed record HostedSession(Guid Id, string Token, string ProfileId, string Name, int HostProcessId = 0)
{
    public string PipeName => "aster-v1-" + Id.ToString("N");
}

/// <summary>Only local descriptors/capabilities are persisted. Shell output and environment stay in memory.</summary>
public sealed class SessionCatalog
{
    public string DirectoryPath { get; }
    public SessionCatalog(string directory)
    {
        DirectoryPath = Path.GetFullPath(directory);
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(DirectoryPath);
        else Directory.CreateDirectory(DirectoryPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
    public string FilePath(Guid id) => Path.Combine(DirectoryPath, id.ToString("N") + ".json");
    public HostedSession Create(string profileId, string name)
    {
        PruneDeadDescriptors();
        if (Directory.EnumerateFiles(DirectoryPath, "*.json").Take(65).Count() >= 64) throw new InvalidOperationException("There are already 64 hosted sessions. Close or recover existing sessions first.");
        var session = new HostedSession(Guid.NewGuid(), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), profileId, name);
        Write(session); return session;
    }
    private void PruneDeadDescriptors()
    {
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.json").Take(128))
        {
            try
            {
                var session = Read(path);
                if (session.HostProcessId == 0)
                {
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > TimeSpan.FromMinutes(1)) File.Delete(path);
                    continue;
                }
                try { using var process = Process.GetProcessById(session.HostProcessId); }
                catch (ArgumentException) { File.Delete(path); }
            }
            catch (Exception error) when (error is IOException or JsonException or ArgumentException) { }
        }
    }
    public void Write(HostedSession session)
    {
        var path = FilePath(session.Id); var temp = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var stream = new FileStream(temp, options))
            JsonSerializer.Serialize(stream, session);
        File.Move(temp, path, true);
    }
    public static HostedSession Read(string path)
    {
        if (new FileInfo(path).Length > 8192) throw new InvalidDataException("Invalid hosted session descriptor.");
        var session = JsonSerializer.Deserialize<HostedSession>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty hosted session descriptor.");
        if (session.Id == Guid.Empty || session.Token is null || session.ProfileId is null || session.Name is null || session.Token.Length != 64 || !session.Token.All(Uri.IsHexDigit) || session.Name.Length > 80 || session.ProfileId.Length > 256) throw new InvalidDataException("Invalid hosted session descriptor.");
        return session;
    }
    public async Task<IReadOnlyList<HostedSession>> DetachedAsync(CancellationToken token = default)
    {
        PruneDeadDescriptors();
        var result = new System.Collections.Concurrent.ConcurrentBag<HostedSession>();
        await Parallel.ForEachAsync(Directory.EnumerateFiles(DirectoryPath, "*.json").Take(64), new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token }, async (path, ct) =>
        {
            try
            {
                var session = Read(path);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(350);
                await using var pipe = CreatePipe(session); await pipe.ConnectAsync(timeout.Token);
                await HostProtocol.Write(pipe, HostProtocol.Hello, HostProtocol.Encode(new HostHello(session.Token, "Info")), timeout.Token);
                var frame = await HostProtocol.Read(pipe, timeout.Token); var ready = HostProtocol.Decode<HostReady>(frame.Data);
                if (frame.Kind == HostProtocol.Ready && ready.Error is null && !ready.Attached) result.Add(session);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or JsonException or ArgumentException) { }
        });
        return result.OrderBy(s => s.Name, StringComparer.Ordinal).ToArray();
    }
    internal static NamedPipeClientStream CreatePipe(HostedSession session) => new(".", session.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    public async Task TerminateAsync(HostedSession session)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var pipe = CreatePipe(session); await pipe.ConnectAsync(timeout.Token);
        await HostProtocol.Write(pipe, HostProtocol.Hello, HostProtocol.Encode(new HostHello(session.Token, "Terminate")), timeout.Token);
        _ = await HostProtocol.Read(pipe, timeout.Token);
    }
    internal Process Launch(HostedSession session)
    {
        var executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "Aster.Terminal.exe" : "Aster.Terminal");
        if (!File.Exists(executable)) throw new FileNotFoundException("The session host executable is missing.", executable);
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppContext.BaseDirectory };
        var runtime = new DirectoryInfo(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        if (runtime.Parent?.Parent?.Name == "shared") start.Environment["DOTNET_ROOT"] = runtime.Parent.Parent.Parent!.FullName;
        start.ArgumentList.Add("--session-host"); start.ArgumentList.Add(FilePath(session.Id));
        return Process.Start(start) ?? throw new IOException("Could not launch the session host.");
    }
}
