using System.Text.Json;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class DiffService
{
    private readonly FileSystemLayout _fs;

    public DiffService(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<RegenPlan> RunAsync(PatternsSnapshot patterns, int? maxCommands = null, CancellationToken ct = default)
    {
        var state = await JsonUtil.ReadAsync<StateSnapshot>(_fs.StatePath, ct)
                    ?? new StateSnapshot { Version = "0.1", GeneratedAt = DateTimeOffset.MinValue };

        var plan = new RegenPlan
        {
            Version = "0.1",
            GeneratedAt = DateTimeOffset.UtcNow
        };

        var ordered = patterns.Commands
            .OrderByDescending(kvp => kvp.Value.TotalUses)
            .Take(maxCommands ?? int.MaxValue);

        foreach (var kvp in ordered)
        {
            var command = kvp.Key;
            var hash = ComputePatternsHash(kvp.Value);
            if (!state.Commands.TryGetValue(command, out var stateEntry))
            {
                plan.Commands.Add(new RegenItem { Command = command, Reason = "new_command", PatternsHash = hash });
                continue;
            }

            if (!string.Equals(stateEntry.PatternsHash, hash, StringComparison.Ordinal))
            {
                plan.Commands.Add(new RegenItem { Command = command, Reason = "patterns_changed", PatternsHash = hash });
            }
        }

        await JsonUtil.WriteAsync(_fs.RegenPlanPath, plan, ct);
        return plan;
    }

    public static string ComputePatternsHash(CommandPatterns patterns)
    {
        var canonical = new
        {
            patterns.Command,
            patterns.TotalUses,
            patterns = patterns.Patterns
                .OrderBy(p => p.Signature, StringComparer.Ordinal)
                .Select(p => new
                {
                    p.Signature,
                    p.Subcommand,
                    flags = p.Flags,
                    options = p.Options.OrderBy(kvp => kvp.Key, StringComparer.Ordinal),
                    p.ArgShapes
                })
        };
        var json = JsonSerializer.Serialize(canonical, JsonUtil.Options);
        return HashUtil.Sha256Hex(json);
    }
}
