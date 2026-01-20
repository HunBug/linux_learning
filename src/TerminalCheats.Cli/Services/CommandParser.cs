using System.Collections.Generic;
using System.Text;
using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public static class CommandParser
{
    public static CommandEvent? Parse(string line)
    {
        try
        {
            var sanitized = Sanitizer.SanitizeLine(line);
            if (string.IsNullOrWhiteSpace(sanitized)) return null;

            var tokens = Tokenize(sanitized);
            if (tokens.Count == 0) return null;

            var cursor = 0;
            if (tokens[0] is "sudo" or "doas")
            {
                tokens.RemoveAt(0);
            }

            if (tokens.Count == 0) return null;

            var command = tokens[cursor++];
            string? subcommand = null;
            var flags = new List<string>();
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            var args = new List<string>();

            if (cursor < tokens.Count && !IsFlag(tokens[cursor]))
            {
                subcommand = tokens[cursor];
                cursor++;
            }

            while (cursor < tokens.Count)
            {
                var token = tokens[cursor];
                if (IsLongOptionWithValue(token, out var key, out var value))
                {
                    options[key] = value;
                    cursor++;
                    continue;
                }

                if (IsFlag(token))
                {
                    if (cursor + 1 < tokens.Count && !IsFlag(tokens[cursor + 1]) && token.StartsWith("--"))
                    {
                        options[token] = tokens[cursor + 1];
                        cursor += 2;
                        continue;
                    }

                    flags.Add(token);
                    cursor++;
                    continue;
                }

                args.Add(token);
                cursor++;
            }

            return new CommandEvent
            {
                Command = command,
                Subcommand = subcommand,
                Flags = flags,
                Options = options,
                Arguments = args,
                Raw = sanitized
            };
        }
        catch
        {
            // malformed lines should just be ignored
            return null;
        }
    }

    private static bool IsFlag(string token) => token.StartsWith('-') && token.Length > 1;

    private static bool IsLongOptionWithValue(string token, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;
        if (!token.StartsWith("--") || !token.Contains('=')) return false;
        var parts = token.Split('=', 2);
        key = parts[0];
        value = parts[1];
        return true;
    }

    private static List<string> Tokenize(string line)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                }
                else if (c == '\\' && i + 1 < line.Length)
                {
                    sb.Append(line[++i]);
                }
                else
                {
                    sb.Append(c);
                }
                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
                continue;
            }

            if (c == '\\' && i + 1 < line.Length)
            {
                sb.Append(line[++i]);
                continue;
            }

            sb.Append(c);
        }

        if (sb.Length > 0)
        {
            tokens.Add(sb.ToString());
        }

        return tokens;
    }
}
