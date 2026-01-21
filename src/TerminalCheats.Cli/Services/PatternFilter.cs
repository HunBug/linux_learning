using System.Text.RegularExpressions;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class PatternFilter
{
    public sealed record Result(PatternsSnapshot Snapshot, int CommandsFiltered, int UsesFiltered);

    public Result Apply(PatternsSnapshot snapshot, AppConfig config)
    {
        if (config.CommandFilters.Count == 0) return new Result(snapshot, 0, 0);

        var regexes = config.CommandFilters
            .Select(pattern => SafeCompile(pattern))
            .Where(r => r != null)
            .ToList()!;
        if (regexes.Count == 0) return new Result(snapshot, 0, 0);

        var filtered = new PatternsSnapshot
        {
            Version = snapshot.Version,
            GeneratedAt = snapshot.GeneratedAt,
            Commands = new(StringComparer.OrdinalIgnoreCase)
        };

        var commandsFiltered = 0;
        var usesFiltered = 0;

        foreach (var kvp in snapshot.Commands)
        {
            var name = kvp.Key;
            if (regexes.Any(r => r!.IsMatch(name)))
            {
                commandsFiltered++;
                usesFiltered += kvp.Value.TotalUses;
                continue;
            }

            filtered.Commands[name] = kvp.Value;
        }

        return new Result(filtered, commandsFiltered, usesFiltered);
    }

    private static Regex? SafeCompile(string pattern)
    {
        try
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }
        catch
        {
            Console.Error.WriteLine($"Warning: could not compile filter regex '{pattern}'");
            return null;
        }
    }
}
