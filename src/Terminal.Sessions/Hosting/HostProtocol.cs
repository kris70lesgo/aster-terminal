using System.Buffers.Binary;
using System.Text.Json;
using Devolutions.Terminal.Connection;

namespace Aster.Sessions.Hosting;

internal sealed record HostHello(string Token, string Action, TerminalLaunchOptions? Options = null, int PixelWidth = 0, int PixelHeight = 0);
internal sealed record HostReady(TerminalProcessMetadata? Process, int Columns, int Rows, bool Attached, bool Checkpointed, string? Error = null);
internal sealed record HostFrame(byte Kind, byte[] Data);

internal static class HostProtocol
{
    public const byte Hello = 1, Ready = 2, Output = 3, Input = 4, Grid = 5, Exit = 6, Terminate = 7, Detach = 8, ReplayEnd = 9;
    public const int MaxFrame = 1024 * 1024;
    public static readonly JsonSerializerOptions Json = new() { MaxDepth = 16 };
    public static byte[] Encode<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Json);
    public static T Decode<T>(byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, Json) ?? throw new InvalidDataException("Empty host message.");
    public static byte[] GridData(int columns, int rows, int pixelWidth = 0, int pixelHeight = 0)
    {
        ValidateGrid(columns, rows);
        var bytes = new byte[16]; BinaryPrimitives.WriteInt32LittleEndian(bytes, columns); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), rows);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), pixelWidth); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), pixelHeight); return bytes;
    }
    public static (int Columns, int Rows) ReadGrid(byte[] bytes)
    {
        if (bytes.Length is not (8 or 16)) throw new InvalidDataException("Invalid host grid.");
        var columns = BinaryPrimitives.ReadInt32LittleEndian(bytes); var rows = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4));
        ValidateGrid(columns, rows); return (columns, rows);
    }
    public static (int Width, int Height) ReadPixels(byte[] bytes)
    {
        if (bytes.Length == 8) return (0, 0);
        if (bytes.Length != 16) throw new InvalidDataException("Invalid pixel grid.");
        var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)); var height = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
        if (width is < 0 or > 1000000 || height is < 0 or > 1000000) throw new InvalidDataException("Invalid pixel dimensions.");
        return (width, height);
    }
    public static void ValidateGrid(int columns, int rows)
    {
        if (columns is < 2 or > 1000 || rows is < 2 or > 500 || columns * rows > 100000) throw new ArgumentException("Terminal dimensions must be 2–1000 columns, 2–500 rows and at most 100,000 cells.");
    }
    public static async Task Write(Stream stream, byte kind, ReadOnlyMemory<byte> payload, CancellationToken token = default)
    {
        if (payload.Length > MaxFrame) throw new InvalidDataException("Host frame exceeds 1 MiB.");
        var header = new byte[5]; header[0] = kind; BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(1), payload.Length);
        await stream.WriteAsync(header, token); await stream.WriteAsync(payload, token); await stream.FlushAsync(token);
    }
    public static async Task<HostFrame> Read(Stream stream, CancellationToken token = default)
    {
        var header = new byte[5]; await stream.ReadExactlyAsync(header, token);
        var size = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1));
        if (size is < 0 or > MaxFrame) throw new InvalidDataException("Invalid host frame length.");
        var bytes = new byte[size]; await stream.ReadExactlyAsync(bytes, token); return new(header[0], bytes);
    }
}
