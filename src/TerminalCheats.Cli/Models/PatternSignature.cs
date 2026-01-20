using System;
using System.Collections.Generic;

namespace TerminalCheats.Cli.Models;

public sealed class PatternSignature
{
    public required string Signature { get; init; }
    public string? Subcommand { get; init; }
    public IReadOnlyList<string> Flags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Options { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> ArgShapes { get; init; } = Array.Empty<string>();
    public int Frequency { get; set; }
    public string? RepresentativeExample { get; set; }
    public DateTimeOffset? FirstSeen { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
}
