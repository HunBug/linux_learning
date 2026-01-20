using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TerminalCheats.Cli.Services;

public sealed class HistoryImporter
{
    private readonly FileSystemLayout _fs;

    public HistoryImporter(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        var lines = new List<string>();

        lines.AddRange(await ReadBashHistoryAsync(ct));
        lines.AddRange(await ReadZshHistoryAsync(ct));
        lines.AddRange(await ReadFishHistoryAsync(ct));
        lines.AddRange(await ReadCustomHistFileAsync(ct));

        if (lines.Count == 0)
        {
            Console.WriteLine("import: no history found");
            return 0;
        }

        var importPath = Path.Combine(_fs.RawDir, $"imported-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.txt");
        await File.WriteAllLinesAsync(importPath, lines, ct);
        Console.WriteLine($"import: wrote {lines.Count} line(s) to {Path.GetFileName(importPath)}");
        return 0;
    }

    private async Task<List<string>> ReadBashHistoryAsync(CancellationToken ct)
    {
        var lines = new List<string>();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bash_history");
        if (File.Exists(path))
        {
            try
            {
                var all = await File.ReadAllLinesAsync(path, ct);
                lines.AddRange(all.Where(l => !string.IsNullOrWhiteSpace(l)));
            }
            catch
            {
                // silently skip
            }
        }
        return lines;
    }

    private async Task<List<string>> ReadZshHistoryAsync(CancellationToken ct)
    {
        var lines = new List<string>();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zsh_history");
        if (!File.Exists(path)) return lines;

        try
        {
            var all = await File.ReadAllLinesAsync(path, ct);
            foreach (var line in all)
            {
                // zsh history may have timestamps or other metadata; extract command
                if (line.StartsWith(": "))
                {
                    // Format: : <timestamp>:0;<command>
                    var match = Regex.Match(line, @"^: \d+:\d+;(.+)$");
                    if (match.Success)
                    {
                        var cmd = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrWhiteSpace(cmd))
                            lines.Add(cmd);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }
        }
        catch
        {
            // silently skip
        }
        return lines;
    }

    private async Task<List<string>> ReadFishHistoryAsync(CancellationToken ct)
    {
        var lines = new List<string>();
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/share/fish/fish_history");
        if (!File.Exists(path)) return lines;

        try
        {
            var content = await File.ReadAllTextAsync(path, ct);
            var root = XDocument.Parse(content);
            var items = root.Descendants("item");
            foreach (var item in items)
            {
                var cmdEl = item.Element("command");
                if (cmdEl?.Value is { } cmd && !string.IsNullOrWhiteSpace(cmd))
                {
                    lines.Add(cmd);
                }
            }
        }
        catch
        {
            // silently skip
        }
        return lines;
    }

    private async Task<List<string>> ReadCustomHistFileAsync(CancellationToken ct)
    {
        var lines = new List<string>();
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrWhiteSpace(histFile) || !File.Exists(histFile)) return lines;

        try
        {
            var all = await File.ReadAllLinesAsync(histFile, ct);
            lines.AddRange(all.Where(l => !string.IsNullOrWhiteSpace(l)));
        }
        catch
        {
            // silently skip
        }
        return lines;
    }
}
