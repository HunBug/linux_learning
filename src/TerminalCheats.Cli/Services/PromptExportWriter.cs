using System.IO;
using System.Linq;
using System.Text;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class PromptExportWriter
{
    private readonly FileSystemLayout _fs;

    public PromptExportWriter(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task WriteAsync(PatternsSnapshot patterns, RegenPlan plan, int? topPatternsPerCommand = null, CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        var outDir = Path.Combine(_fs.OutputDir, "prompts_web");
        Directory.CreateDirectory(outDir);

        foreach (var item in plan.Commands)
        {
            try
            {
                if (!patterns.Commands.TryGetValue(item.Command, out var commandPatterns)) continue;
                var content = BuildPrompt(commandPatterns, item, topPatternsPerCommand);
                var fileName = NameUtil.ToSafeFileName(item.Command) + ".txt";
                var path = Path.Combine(outDir, fileName);
                await File.WriteAllTextAsync(path, content, Encoding.UTF8, ct);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to export prompt for {item.Command}: {ex.Message}");
            }
        }
    }

    private static string BuildPrompt(CommandPatterns commandPatterns, RegenItem item, int? topPatternsPerCommand)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a CLI cheatsheet generator.");
        sb.AppendLine("Return a single JSON object only (no Markdown, no explanation, no code fences).");
        sb.AppendLine("Follow this shape and fill in thoughtful, concise values:");
        sb.AppendLine("{");
        sb.AppendLine("  \"version\": \"0.1\",");
        sb.AppendLine($"  \"command\": \"{commandPatterns.Command}\",");
        sb.AppendLine("  \"summary\": \"...\",");
        sb.AppendLine("  \"when_i_use_it\": [\"...\"],");
        sb.AppendLine("  \"syntax_patterns\": [\"...\"],");
        sb.AppendLine("  \"flags_and_options\": [{\"name\": \"--flag\", \"description\": \"...\", \"example\": \"...\"}],");
        sb.AppendLine("  \"subcommands\": [],");
        sb.AppendLine("  \"examples\": [{\"command\": \"...\", \"why\": \"...\"}],");
        sb.AppendLine("  \"pitfalls\": [\"...\"],");
        sb.AppendLine("  \"related_commands\": [\"...\"],");
        sb.AppendLine("  \"regenerated_at\": \"<ISO-8601>\",");
        sb.AppendLine($"  \"source\": {{\"patterns_hash\": \"{item.PatternsHash}\", \"input_hash\": \"{item.PatternsHash}\", \"run_id\": \"manual-web\"}}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Guidelines:");
        sb.AppendLine("- Keep answers factual, safe, and concise.");
        sb.AppendLine("- Use short sentences or fragments; avoid bullet markers.");
        sb.AppendLine("- Prefer the most frequent patterns as defaults.");
        sb.AppendLine("- If you are unsure, leave fields empty strings or omit optional examples.");
        sb.AppendLine();

        sb.AppendLine($"Command: {commandPatterns.Command}");
        sb.AppendLine($"Stats: total uses={commandPatterns.TotalUses}, unique patterns={commandPatterns.Patterns.Count}");
        sb.AppendLine();

        var patterns = commandPatterns.Patterns
            .OrderByDescending(p => p.Frequency)
            .ThenBy(p => p.Signature, StringComparer.Ordinal)
            .Take(topPatternsPerCommand ?? int.MaxValue)
            .ToList();
        sb.AppendLine("Patterns (most used first):");
        foreach (var p in patterns)
        {
            var flagStr = p.Flags.Any() ? string.Join(" ", p.Flags) : "";
            var optStr = p.Options.Any() ? string.Join(" ", p.Options.Select(o => $"{o.Key}=<{o.Value}>")) : "";
            var argStr = p.ArgShapes.Any() ? string.Join(" ", p.ArgShapes.Select(a => $"<{a}>")) : "";
            var parts = new[] { p.Subcommand, flagStr, optStr, argStr }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            sb.AppendLine($"- freq={p.Frequency} | sig={p.Signature} | {string.Join(" ", parts)}");
        }
        sb.AppendLine();

        var examples = patterns
            .Where(p => !string.IsNullOrWhiteSpace(p.RepresentativeExample))
            .OrderByDescending(p => p.Frequency)
            .Take(6)
            .ToList();
        sb.AppendLine("Examples:");
        foreach (var ex in examples)
        {
            sb.AppendLine($"- {ex.RepresentativeExample}");
        }

        return sb.ToString();
    }
}
