using System;
using System.Collections.Generic;
using System.Linq;

namespace TerminalCheats.Cli;

internal static class Program
{
    private static readonly IReadOnlyDictionary<string, Func<string[], int>> Handlers =
        new Dictionary<string, Func<string[], int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["aggregate"] = RunAggregate,
            ["diff"] = RunDiff,
            ["prepare"] = RunPrepare,
            ["generate"] = RunGenerate,
            ["render"] = RunRender,
            ["run"] = RunAll
        };

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0];
        var tail = args.Skip(1).ToArray();

        if (!Handlers.TryGetValue(command, out var handler))
        {
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 1;
        }

        return handler(tail);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("terminal-cheats CLI (skeleton)");
        Console.WriteLine("Commands:");
        Console.WriteLine("  aggregate   Parse histories and emit normalized data");
        Console.WriteLine("  diff        Compare current patterns to state and plan regeneration");
        Console.WriteLine("  prepare     Write per-command JSON prompts for generation");
        Console.WriteLine("  generate    Run generator (or skip with --generator none) and write entries");
        Console.WriteLine("  render      Build renderer inputs (Markdown later)");
        Console.WriteLine("  run         End-to-end aggregate -> diff -> prepare -> generate -> render");
    }

    private static int RunAggregate(string[] args)
    {
        Console.WriteLine("aggregate: not implemented yet");
        return 0;
    }

    private static int RunDiff(string[] args)
    {
        Console.WriteLine("diff: not implemented yet");
        return 0;
    }

    private static int RunPrepare(string[] args)
    {
        Console.WriteLine("prepare: not implemented yet");
        return 0;
    }

    private static int RunGenerate(string[] args)
    {
        Console.WriteLine("generate: not implemented yet");
        return 0;
    }

    private static int RunRender(string[] args)
    {
        Console.WriteLine("render: not implemented yet");
        return 0;
    }

    private static int RunAll(string[] args)
    {
        Console.WriteLine("run: not implemented yet");
        return 0;
    }
}
