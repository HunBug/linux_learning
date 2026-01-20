using System.Text.RegularExpressions;

namespace TerminalCheats.Cli.Services;

public static partial class Sanitizer
{
    public static string SanitizeLine(string line)
    {
        var result = line.Trim();
        result = HomePath().Replace(result, "~/");
        result = TokenLike().Replace(result, "<TOKEN>");
        result = HostUser().Replace(result, m => m.Groups[1].Value);
        return result;
    }

    [GeneratedRegex(@"\b[a-zA-Z0-9]{24,}\b")]
    private static partial Regex TokenLike();

    [GeneratedRegex(@"/home/[^/]+/")]
    private static partial Regex HomePath();

    [GeneratedRegex(@"([a-zA-Z0-9_.-]+)@[a-zA-Z0-9_.-]+")]
    private static partial Regex HostUser();
}
