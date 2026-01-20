using System.Text;

namespace TerminalCheats.Cli.Services;

public static class NameUtil
{
    public static string? NormalizeCommandName(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        var trimmed = command.Trim();
        if (trimmed.Length > 128) return null;
        if (trimmed.Any(char.IsControl)) return null;
        var name = trimmed.Contains('/') ? Path.GetFileName(trimmed) : trimmed;
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name;
    }

    public static string ToSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "command";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (invalid.Contains(ch) || ch == ':' || ch == '\\')
                sb.Append('_');
            else
                sb.Append(ch);
        }
        var result = sb.ToString().Trim();
        if (result.Length > 80)
            result = result[..80];
        return string.IsNullOrWhiteSpace(result) ? "command" : result;
    }
}
