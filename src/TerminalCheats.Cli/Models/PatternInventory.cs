using System;
using System.Collections.Generic;

namespace TerminalCheats.Cli.Models;

public sealed class CommandPatterns
{
    public required string Command { get; init; }
    public int TotalUses { get; set; }
    public List<PatternSignature> Patterns { get; init; } = new();
}

public sealed class PatternsSnapshot
{
    public required string Version { get; init; }
    public required DateTimeOffset GeneratedAt { get; set; }
    public Dictionary<string, CommandPatterns> Commands { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RegenPlan
{
    public required string Version { get; init; }
    public required DateTimeOffset GeneratedAt { get; set; }
    public List<RegenItem> Commands { get; init; } = new();
}

public sealed class RegenItem
{
    public required string Command { get; init; }
    public required string Reason { get; init; }
    public required string PatternsHash { get; init; }
}

public sealed class StateSnapshot
{
    public required string Version { get; init; }
    public required DateTimeOffset GeneratedAt { get; set; }
    public Dictionary<string, StateEntry> Commands { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class StateEntry
{
    public required string PatternsHash { get; init; }
    public string? EntryPath { get; init; }
    public DateTimeOffset? GeneratedAt { get; init; }
}
