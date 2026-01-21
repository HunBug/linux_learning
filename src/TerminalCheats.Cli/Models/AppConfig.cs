using System.Collections.Generic;

namespace TerminalCheats.Cli.Models;

public sealed class AppConfig
{
    public List<string> CommandFilters { get; init; } = new();
}
