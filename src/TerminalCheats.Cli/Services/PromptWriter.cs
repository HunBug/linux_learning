using System.Text.Json.Serialization;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class PromptWriter
{
    private readonly FileSystemLayout _fs;

    public PromptWriter(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task WriteAsync(PatternsSnapshot patterns, RegenPlan plan, int? topPatternsPerCommand = null, CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        foreach (var item in plan.Commands)
        {
            try
            {
                if (!patterns.Commands.TryGetValue(item.Command, out var commandPatterns)) continue;
                var prompt = BuildPrompt(commandPatterns, item, topPatternsPerCommand);
                var fileName = NameUtil.ToSafeFileName(item.Command) + ".prompt.json";
                var path = Path.Combine(_fs.PromptsDir, fileName);
                await JsonUtil.WriteAsync(path, prompt, ct);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to write prompt for {item.Command}: {ex.Message}");
            }
        }
    }

    private static object BuildPrompt(CommandPatterns commandPatterns, RegenItem item, int? topPatternsPerCommand)
    {
        var patterns = commandPatterns.Patterns
            .OrderByDescending(p => p.Frequency)
            .Take(topPatternsPerCommand ?? int.MaxValue)
            .Select(p => new
            {
                signature = p.Signature,
                subcommand = p.Subcommand,
                flags = p.Flags,
                options = p.Options,
                arg_shapes = p.ArgShapes,
                frequency = p.Frequency,
                representative_example = p.RepresentativeExample,
                first_seen = p.FirstSeen,
                last_seen = p.LastSeen
            })
            .ToList();

        var examples = commandPatterns.Patterns
            .Where(p => !string.IsNullOrWhiteSpace(p.RepresentativeExample))
            .OrderByDescending(p => p.Frequency)
            .Take(8)
            .Select(p => new
            {
                pattern_signature = p.Signature,
                command_line = p.RepresentativeExample,
                source = new { host = (string?)null, shell = (string?)null, timestamp = p.LastSeen }
            })
            .ToList();

        return new
        {
            version = "0.1",
            entry_schema_version = "0.1",
            command = commandPatterns.Command,
            generated_at = DateTimeOffset.UtcNow,
            generator = new { reason = item.Reason, notes = (string?)null },
            stats = new { total_uses = commandPatterns.TotalUses, unique_patterns = commandPatterns.Patterns.Count },
            patterns,
            examples
        };
    }
}
