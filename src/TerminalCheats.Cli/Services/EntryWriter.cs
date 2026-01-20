using System.Text.Json;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class EntryWriter
{
    private readonly FileSystemLayout _fs;

    public EntryWriter(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task WritePlaceholderAsync(PatternsSnapshot patterns, RegenPlan plan, int? topPatternsPerCommand = null, string generatorMode = "none", CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        foreach (var item in plan.Commands)
        {
            try
            {
                if (!patterns.Commands.TryGetValue(item.Command, out var commandPatterns)) continue;
                var entry = BuildPlaceholderEntry(commandPatterns, item.PatternsHash, topPatternsPerCommand, generatorMode);
                var fileName = NameUtil.ToSafeFileName(item.Command) + ".json";
                var path = Path.Combine(_fs.EntriesDir, fileName);
                await JsonUtil.WriteAsync(path, entry, ct);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to write entry for {item.Command}: {ex.Message}");
            }
        }
    }

    public static object BuildPlaceholderEntry(CommandPatterns commandPatterns, string patternsHash, int? topPatternsPerCommand, string generatorMode)
    {
        var syntaxPatterns = commandPatterns.Patterns
            .OrderByDescending(p => p.Frequency)
            .Take(topPatternsPerCommand ?? int.MaxValue)
            .Select(p => BuildSyntaxPattern(p))
            .ToList();

        var flags = commandPatterns.Patterns
            .SelectMany(p => p.Flags)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => new { name = f, description = "" })
            .ToList();

        var options = commandPatterns.Patterns
            .SelectMany(p => p.Options.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(o => o, StringComparer.Ordinal)
            .Select(o => new { name = o, description = "" })
            .ToList();

        var flagsAndOptions = flags.Concat(options).ToList();

        var examples = commandPatterns.Patterns
            .Where(p => !string.IsNullOrWhiteSpace(p.RepresentativeExample))
            .OrderByDescending(p => p.Frequency)
            .Take(8)
            .Select(p => new { command = p.RepresentativeExample!, why = "" })
            .ToList();

        return new
        {
            version = "0.1",
            command = commandPatterns.Command,
            summary = $"Placeholder entry generated locally (generator {generatorMode}).",
            when_i_use_it = Array.Empty<string>(),
            syntax_patterns = syntaxPatterns,
            flags_and_options = flagsAndOptions,
            subcommands = Array.Empty<object>(),
            examples,
            pitfalls = Array.Empty<string>(),
            related_commands = Array.Empty<string>(),
            regenerated_at = DateTimeOffset.UtcNow,
            source = new { patterns_hash = patternsHash, input_hash = patternsHash, run_id = (string?)null }
        };
    }

    private static string BuildSyntaxPattern(PatternSignature pattern)
    {
        var parts = new List<string> { pattern.Subcommand ?? string.Empty };
        parts.AddRange(pattern.Flags);
        parts.AddRange(pattern.Options.Select(o => $"{o.Key} <{o.Value}>").ToList());
        parts.AddRange(pattern.ArgShapes.Select(a => $"<{a}>").ToList());
        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
    }
}
