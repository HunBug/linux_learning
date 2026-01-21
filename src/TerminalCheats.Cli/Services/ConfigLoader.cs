using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class ConfigLoader
{
    private readonly FileSystemLayout _fs;

    public ConfigLoader(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
    {
        var cfg = await JsonUtil.ReadAsync<AppConfig>(_fs.ConfigPath, ct);
        return cfg ?? new AppConfig();
    }
}
