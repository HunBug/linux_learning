namespace TerminalCheats.Cli.Services;

public static class OptionParser
{
    public static (Dictionary<string, string> options, List<string> rest) Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rest = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                var parts = arg.Split('=', 2);
                if (parts.Length == 2)
                {
                    options[parts[0]] = parts[1];
                    continue;
                }

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    options[arg] = args[i + 1];
                    i++;
                }
                else
                {
                    options[arg] = "true";
                }
            }
            else
            {
                rest.Add(arg);
            }
        }

        return (options, rest);
    }
}
