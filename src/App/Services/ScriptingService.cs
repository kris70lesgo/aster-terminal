using Avalonia.Threading;
using Aster.Sessions.Persistence;
using Aster.Sessions.Scripting;

namespace Aster.App.Services;

public sealed class ScriptingService
{
    public static ScriptingService Shared { get; } = new();
    private readonly SemaphoreSlim _mutation = new(1, 1);
    private ScriptingServer? _server;
    private DesktopScriptingTarget? _target;
    private string? _path;
    private int _port;
    public async Task ConfigureAsync(SettingsStore store)
    {
        Dispatcher.UIThread.VerifyAccess(); await _mutation.WaitAsync();
        try
        {
            var settings = store.Settings;
            if (_server is not null && settings.ScriptingEnabled && _path == store.Path && _port == settings.ScriptingPort) return;
            var previous = _server; _server = null; _target?.Dispose(); _target = null;
            if (previous is not null) await previous.DisposeAsync();
            if (!settings.ScriptingEnabled) return;
            var target = new DesktopScriptingTarget(store);
            var server = new ScriptingServer(target, Path.GetDirectoryName(store.Path)!, settings.ScriptingPort);
            try { await server.StartAsync(); }
            catch { target.Dispose(); await server.DisposeAsync(); throw; }
            _target = target; _server = server; _path = store.Path; _port = settings.ScriptingPort;
        }
        finally { _mutation.Release(); }
    }
    public void StopAtExit()
    {
        // No UI work may be awaited once the dispatcher has stopped. Cancel
        // queued API actions, remove credentials, and stop the loopback listener.
        _target?.Dispose(); _target = null; var server = _server; _server = null;
        if (server is not null) Task.Run(async () => await server.DisposeAsync()).GetAwaiter().GetResult();
    }
}
