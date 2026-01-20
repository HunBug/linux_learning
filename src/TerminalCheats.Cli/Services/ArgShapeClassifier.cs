using System.Text.RegularExpressions;

namespace TerminalCheats.Cli.Services;

public static partial class ArgShapeClassifier
{
    public static string Classify(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "word";
        if (token.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || token.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "url";
        if (token.Contains('/') || token.StartsWith("~")) return "path";
        if (Number().IsMatch(token)) return "number";
        if (token.Contains("@")) return "addr";
        return "word";
    }

    [GeneratedRegex("^-?\\d+(\\.\\d+)?$")]
    private static partial Regex Number();
}
