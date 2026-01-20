using System.IO;
using System.Linq;
using System.Text;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class ReportWriter
{
    private readonly FileSystemLayout _fs;

    public ReportWriter(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<string> WriteAsync(PatternsSnapshot patterns, int? topCommands = null, int? topFlags = null, int? topOptions = null, CancellationToken ct = default)
    {
        _fs.EnsureBaseFolders();
        var report = BuildReport(patterns, topCommands, topFlags, topOptions);
        var path = Path.Combine(_fs.OutputDir, "report.txt");
        await File.WriteAllTextAsync(path, report, Encoding.UTF8, ct);
        return report;
    }

    private static string BuildReport(PatternsSnapshot patterns, int? topCommands, int? topFlags, int? topOptions)
    {
        var sb = new StringBuilder();
        var topCmds = topCommands ?? 20;
        var topFl = topFlags ?? 20;
        var topOpts = topOptions ?? 20;

        var totalUses = patterns.Commands.Values.Sum(c => c.TotalUses);
        var totalPatterns = patterns.Commands.Values.Sum(c => c.Patterns.Count);

        sb.AppendLine("terminal-cheats report");
        sb.AppendLine($"generated_at: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"commands: {patterns.Commands.Count}");
        sb.AppendLine($"total_uses: {totalUses}");
        sb.AppendLine($"unique_patterns: {totalPatterns}");
        sb.AppendLine();

        sb.AppendLine($"Top commands (limit {topCmds}):");
        var orderedCommands = patterns.Commands.Values
            .OrderByDescending(c => c.TotalUses)
            .ThenBy(c => c.Command, StringComparer.Ordinal)
            .Take(topCmds)
            .ToList();
        for (int i = 0; i < orderedCommands.Count; i++)
        {
            var c = orderedCommands[i];
            sb.AppendLine($"{i + 1,2}. {c.Command} | uses={c.TotalUses} | patterns={c.Patterns.Count}");
        }
        sb.AppendLine();

        var flagCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var optionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var cmd in patterns.Commands.Values)
        {
            foreach (var p in cmd.Patterns)
            {
                foreach (var flag in p.Flags)
                {
                    flagCounts[flag] = flagCounts.TryGetValue(flag, out var existing) ? existing + p.Frequency : p.Frequency;
                }

                foreach (var opt in p.Options.Keys)
                {
                    optionCounts[opt] = optionCounts.TryGetValue(opt, out var existing) ? existing + p.Frequency : p.Frequency;
                }
            }
        }

        sb.AppendLine($"Top flags (limit {topFl}):");
        var orderedFlags = flagCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Take(topFl)
            .ToList();
        for (int i = 0; i < orderedFlags.Count; i++)
        {
            sb.AppendLine($"{i + 1,2}. {orderedFlags[i].Key} | uses={orderedFlags[i].Value}");
        }
        sb.AppendLine();

        sb.AppendLine($"Top options (limit {topOpts}):");
        var orderedOptions = optionCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Take(topOpts)
            .ToList();
        for (int i = 0; i < orderedOptions.Count; i++)
        {
            sb.AppendLine($"{i + 1,2}. {orderedOptions[i].Key} | uses={orderedOptions[i].Value}");
        }

        return sb.ToString();
    }
}
