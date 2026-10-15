using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Aster.Sessions.Scripting;

public sealed record ScriptingEndpoint(int Port, int ProcessId, string Token)
{
    public static string FilePath(string settingsDirectory) => Path.Combine(settingsDirectory, "api", "endpoint.json");
    public static ScriptingEndpoint Create(int port) => new(port, Environment.ProcessId, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    public void Save(string settingsDirectory)
    {
        var path = FilePath(settingsDirectory); PrepareDirectory(settingsDirectory);
        var directory = Path.GetDirectoryName(path)!;
        var temporary = Path.Combine(directory, "endpoint-" + Guid.NewGuid().ToString("N") + ".tmp");
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        try
        {
            using (var stream = new FileStream(temporary, options)) JsonSerializer.Serialize(stream, this);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void PrepareDirectory(string settingsDirectory)
    {
        var directory = Path.GetDirectoryName(FilePath(settingsDirectory))!;
        if (Directory.Exists(directory) && new DirectoryInfo(directory).LinkTarget is not null) throw new IOException("The API credential directory cannot be a symbolic link.");
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory);
        else { Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
    }
    internal static FileStream AcquireLease(string settingsDirectory)
    {
        PrepareDirectory(settingsDirectory);
        var options = new FileStreamOptions { Mode = FileMode.OpenOrCreate, Access = FileAccess.ReadWrite, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new FileStream(Path.Combine(Path.GetDirectoryName(FilePath(settingsDirectory))!, "server.lock"), options);
    }
    public void Remove(string settingsDirectory)
    {
        var path = FilePath(settingsDirectory);
        try { if (File.Exists(path) && Read(path).Token == Token) File.Delete(path); }
        catch (Exception error) when (error is IOException or JsonException or ArgumentException) { }
    }
    public static ScriptingEndpoint Read(string path)
    {
        if (new FileInfo(path).Length > 4096) throw new InvalidDataException("Invalid API endpoint file.");
        var endpoint = JsonSerializer.Deserialize<ScriptingEndpoint>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty API endpoint file.");
        if (endpoint.Port is < 1 or > 65535 || endpoint.ProcessId <= 0 || endpoint.Token is null || endpoint.Token.Length != 64 || !endpoint.Token.All(Uri.IsHexDigit)) throw new InvalidDataException("Invalid API endpoint.");
        return endpoint;
    }
    public bool IsOwnerRunning()
    {
        try { using var process = Process.GetProcessById(ProcessId); return !process.HasExited; }
        catch (ArgumentException) { return false; }
    }
}
