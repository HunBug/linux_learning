using System.Text.Json;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class Aggregator
{
    private readonly FileSystemLayout _fs;

    public Aggregator(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<PatternsSnapshot> RunAsync(CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        var events = await ReadEvents(ct);
        var snapshot = BuildSnapshot(events);
        await JsonUtil.WriteAsync(_fs.PatternsPath, snapshot, ct);
        var runId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var snapshotPath = Path.Combine(_fs.SnapshotsDir, $"patterns-{runId}.json");
        await JsonUtil.WriteAsync(snapshotPath, snapshot, ct);
        return snapshot;
    }

    private async Task<List<CommandEvent>> ReadEvents(CancellationToken ct)
    {
        var list = new List<CommandEvent>();

        // Check raw/ first
        if (Directory.Exists(_fs.RawDir))
        {
            var files = Directory.EnumerateFiles(_fs.RawDir, "*", SearchOption.AllDirectories);
            if (files.Any())
            {
                foreach (var file in files)
                {
                    var lines = await File.ReadAllLinesAsync(file, ct);
                    foreach (var line in lines)
                    {
                        var evt = CommandParser.Parse(line);
                        if (evt != null)
                        {
                            list.Add(evt);
                        }
                    }
                }
                return list;
            }
        }

        // Auto-detect standard history locations if raw/ is empty
        var importer = new HistoryImporter(_fs);
        var lines2 = new List<string>();
        lines2.AddRange(await ReadHistoryAsync(ct, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bash_history")));
        lines2.AddRange(await ReadHistoryAsync(ct, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zsh_history")));
        lines2.AddRange(await ReadHistoryAsync(ct, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/fish/fish_history")));

        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (!string.IsNullOrWhiteSpace(histFile))
        {
            lines2.AddRange(await ReadHistoryAsync(ct, histFile));
        }

        foreach (var line in lines2)
        {
            var evt = CommandParser.Parse(line);
            if (evt != null)
            {
                list.Add(evt);
            }
        }

        return list;
    }

    private async Task<List<string>> ReadHistoryAsync(CancellationToken ct, string path)
    {
        var lines = new List<string>();
        if (!File.Exists(path)) return lines;
        try
        {
            var all = await File.ReadAllLinesAsync(path, ct);
            foreach (var line in all)
            {
                if (line.StartsWith(": "))
                {
                    // zsh format: : <timestamp>:0;<command>
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"^: \d+:\d+;(.+)$");
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
        catch (UnauthorizedAccessException)
        {
            // silently skip files we can't read
        }
        catch
        {
            // silently skip unreadable files
        }
        return lines;
    }

    private PatternsSnapshot BuildSnapshot(IEnumerable<CommandEvent> events)
    {
        var snapshot = new PatternsSnapshot
        {
            Version = "0.1",
            GeneratedAt = DateTimeOffset.UtcNow
        };

        foreach (var evt in events)
        {
            if (!snapshot.Commands.TryGetValue(evt.Command, out var command))
            {
                command = new CommandPatterns { Command = evt.Command };
                snapshot.Commands[evt.Command] = command;
            }

            command.TotalUses++;
            var signature = BuildSignature(evt);
            var existing = command.Patterns.FirstOrDefault(p => p.Signature == signature.Signature);
            if (existing == null)
            {
                command.Patterns.Add(signature);
                existing = signature;
            }

            existing.Frequency++;
            existing.RepresentativeExample ??= evt.Raw;
            existing.FirstSeen ??= evt.Source.Timestamp;
            existing.LastSeen = evt.Source.Timestamp ?? existing.LastSeen;
        }

        return snapshot;
    }

    private PatternSignature BuildSignature(CommandEvent evt)
    {
        var flags = evt.Flags.OrderBy(f => f, StringComparer.Ordinal).ToList();
        var options = evt.Options
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => ArgShapeClassifier.Classify(kvp.Value), StringComparer.Ordinal);
        var argShapes = evt.Arguments.Select(ArgShapeClassifier.Classify).ToList();

        var canonical = new
        {
            command = evt.Command,
            subcommand = evt.Subcommand,
            flags,
            options,
            argShapes
        };
        var json = JsonSerializer.Serialize(canonical, JsonUtil.Options);
        var signature = HashUtil.Sha256Hex(json);

        return new PatternSignature
        {
            Signature = signature,
            Subcommand = evt.Subcommand,
            Flags = flags,
            Options = options,
            ArgShapes = argShapes,
            Frequency = 0
        };
    }
}
