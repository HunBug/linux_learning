using TerminalCheats.Cli.Models;
using TerminalCheats.Cli.Services;

namespace TerminalCheats.Cli;

internal static class Program
{
    private static readonly IReadOnlyDictionary<string, Func<string[], Task<int>>> Handlers =
        new Dictionary<string, Func<string[], Task<int>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["import"] = RunImport,
            ["aggregate"] = RunAggregate,
            ["diff"] = RunDiff,
            ["prepare"] = RunPrepare,
            ["generate"] = RunGenerate,
            ["render"] = RunRender,
            ["run"] = RunAll
        };

    private static async Task<int> Main(string[] args)
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

        try
        {
            return await handler(tail);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("terminal-cheats CLI");
        Console.WriteLine("Commands:");
        Console.WriteLine("  import      Import histories from standard locations (bash/zsh/fish + $HISTFILE)");
        Console.WriteLine("  aggregate   Parse histories and emit normalized data");
        Console.WriteLine("  diff        Compare current patterns to state and plan regeneration");
        Console.WriteLine("  prepare     Write per-command JSON prompts for generation");
        Console.WriteLine("  generate    Run generator (or skip with --generator none) and write entries");
        Console.WriteLine("  render      Build renderer inputs (Markdown later)");
        Console.WriteLine("  run         End-to-end aggregate -> diff -> prepare -> generate -> render");
        Console.WriteLine("Global options: --root <path> (defaults to cwd)");
    }

    private static FileSystemLayout BuildLayout(Dictionary<string, string> options)
    {
        var root = options.TryGetValue("--root", out var val) ? val : Environment.CurrentDirectory;
        return new FileSystemLayout(root);
    }

    private static async Task<int> RunImport(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var importer = new HistoryImporter(fs);
        return await importer.ImportAsync();
    }

    private static async Task<int> RunAggregate(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var aggregator = new Aggregator(fs);
        var snapshot = await aggregator.RunAsync();
        Console.WriteLine($"aggregate: processed {snapshot.Commands.Values.Sum(c => c.TotalUses)} lines");
        return 0;
    }

    private static async Task<int> RunDiff(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var diff = new DiffService(fs);
        var plan = await diff.RunAsync();
        if (plan.Commands.Count == 0)
        {
            Console.WriteLine("diff: no changes detected");
        }
        else
        {
            foreach (var cmd in plan.Commands)
            {
                Console.WriteLine($"diff: {cmd.Command} -> {cmd.Reason}");
            }
        }
        return 0;
    }

    private static async Task<int> RunPrepare(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var patterns = await JsonUtil.ReadAsync<PatternsSnapshot>(fs.PatternsPath)
                       ?? throw new InvalidOperationException("patterns.json not found; run aggregate first.");
        var plan = await JsonUtil.ReadAsync<RegenPlan>(fs.RegenPlanPath)
                   ?? throw new InvalidOperationException("regen_plan.json not found; run diff first.");
        var writer = new PromptWriter(fs);
        await writer.WriteAsync(patterns, plan);
        Console.WriteLine($"prepare: wrote {plan.Commands.Count} prompt(s)");
        return 0;
    }

    private static async Task<int> RunGenerate(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var patterns = await JsonUtil.ReadAsync<PatternsSnapshot>(fs.PatternsPath)
                       ?? throw new InvalidOperationException("patterns.json not found; run aggregate first.");
        var plan = await JsonUtil.ReadAsync<RegenPlan>(fs.RegenPlanPath)
                   ?? throw new InvalidOperationException("regen_plan.json not found; run diff first.");

        var generator = options.TryGetValue("--generator", out var gen) ? gen : "none";
        if (!string.Equals(generator, "none", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("generate: external generator not wired; use --generator none");
            return 1;
        }

        var entries = new EntryWriter(fs);
        await entries.WritePlaceholderAsync(patterns, plan);
        var state = new StateUpdater(fs);
        await state.UpdateAsync(patterns, plan);
        Console.WriteLine($"generate: wrote {plan.Commands.Count} placeholder entrie(s) and updated state");
        return 0;
    }

    private static async Task<int> RunRender(string[] args)
    {
        Console.WriteLine("render: renderer deferred; ensure prompts/entries exist");
        return 0;
    }

    private static async Task<int> RunAll(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);

        var aggregator = new Aggregator(fs);
        var patterns = await aggregator.RunAsync();

        var diff = new DiffService(fs);
        var plan = await diff.RunAsync();

        var writer = new PromptWriter(fs);
        await writer.WriteAsync(patterns, plan);

        var generator = options.TryGetValue("--generator", out var gen) ? gen : "none";
        if (!string.Equals(generator, "none", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("run: external generator not wired; use --generator none");
            return 1;
        }

        var entries = new EntryWriter(fs);
        await entries.WritePlaceholderAsync(patterns, plan);
        var state = new StateUpdater(fs);
        await state.UpdateAsync(patterns, plan);

        Console.WriteLine("run: completed aggregate -> diff -> prepare -> generate (placeholder)");
        return 0;
    }
}
