using System;
using System.Collections.Generic;

namespace TerminalCheats.Cli.Models;

public sealed class CommandEvent
{
    public required string Command { get; init; }
    public string? Subcommand { get; init; }
    public IReadOnlyList<string> Flags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
    public string Raw { get; init; } = string.Empty;
    public CommandSource Source { get; init; } = new();
}

public sealed class CommandSource
{
    public string? Host { get; init; }
    public string? Shell { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}
