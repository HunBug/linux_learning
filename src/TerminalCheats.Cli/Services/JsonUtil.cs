using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerminalCheats.Cli.Services;

public static class JsonUtil
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync<T>(string path, T data, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, Options, ct);
    }

    public static async Task<T?> ReadAsync<T>(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, ct);
    }
}
