using System.IO;
using System.Linq;
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
            ["report"] = RunReport,
            ["prompt-export"] = RunPromptExport,
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
        Console.WriteLine("  report      Emit human-readable summary to stdout and output/report.txt");
        Console.WriteLine("  prompt-export Export web-ready prompts to output/prompts_web/");
        Console.WriteLine("  render      Build renderer inputs (Markdown later)");
        Console.WriteLine("  run         End-to-end aggregate -> diff -> prepare -> generate -> render");
        Console.WriteLine("Global options: --root <path> (defaults to cwd)");
        Console.WriteLine("Caps: --top-commands <n>, --top-patterns-per-command <n>");
        Console.WriteLine("Report options: --top-flags <n>, --top-options <n>");
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
        var (patterns, filteredNote) = await LoadFilteredPatterns(fs, options);
        var diff = new DiffService(fs);
        var maxCommands = ParseOptionalInt(options, "--top-commands");
        var plan = await diff.RunAsync(patterns, maxCommands);
        if (filteredNote is not null) Console.WriteLine(filteredNote);
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
        var (patterns, filteredNote) = await LoadFilteredPatterns(fs, options);
        var plan = await JsonUtil.ReadAsync<RegenPlan>(fs.RegenPlanPath)
                   ?? throw new InvalidOperationException("regen_plan.json not found; run diff first.");
        var writer = new PromptWriter(fs);
        var topPatterns = ParseOptionalInt(options, "--top-patterns-per-command");
        await writer.WriteAsync(patterns, plan, topPatterns);
        if (filteredNote is not null) Console.WriteLine(filteredNote);
        Console.WriteLine($"prepare: wrote {plan.Commands.Count} prompt(s)");
        return 0;
    }

    private static async Task<int> RunGenerate(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var (patterns, filteredNote) = await LoadFilteredPatterns(fs, options);
        var plan = await JsonUtil.ReadAsync<RegenPlan>(fs.RegenPlanPath)
                   ?? throw new InvalidOperationException("regen_plan.json not found; run diff first.");

        var generator = options.TryGetValue("--generator", out var gen) ? gen : "none";
        var skipExternal = string.Equals(generator, "none", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(generator, "dry-run", StringComparison.OrdinalIgnoreCase);
        if (!skipExternal)
        {
            Console.WriteLine("generate: external generator not wired; use --generator none or --generator dry-run");
            return 1;
        }
        if (string.Equals(generator, "dry-run", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("generate: dry-run (writing placeholders only)");
        }

        var entries = new EntryWriter(fs);
        var topPatterns = ParseOptionalInt(options, "--top-patterns-per-command");
        await entries.WritePlaceholderAsync(patterns, plan, topPatterns, generator);
        var state = new StateUpdater(fs);
        await state.UpdateAsync(patterns, plan);
        if (filteredNote is not null) Console.WriteLine(filteredNote);
        Console.WriteLine($"generate: wrote {plan.Commands.Count} placeholder entrie(s) and updated state");
        return 0;
    }

    private static async Task<int> RunRender(string[] args)
    {
        Console.WriteLine("render: renderer deferred; ensure prompts/entries exist");
        return 0;
    }

    private static async Task<int> RunReport(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var (patterns, filteredNote) = await LoadFilteredPatterns(fs, options);

        var writer = new ReportWriter(fs);
        var topCommands = ParseOptionalInt(options, "--top-commands");
        var topFlags = ParseOptionalInt(options, "--top-flags");
        var topOptions = ParseOptionalInt(options, "--top-options");
        var report = await writer.WriteAsync(patterns, topCommands, topFlags, topOptions);
        Console.WriteLine(report);
        Console.WriteLine($"report: wrote {Path.Combine(fs.OutputDir, "report.txt")}");
        if (filteredNote is not null) Console.WriteLine(filteredNote);
        return 0;
    }

    private static async Task<int> RunPromptExport(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);
        var (patterns, filteredNote) = await LoadFilteredPatterns(fs, options);
        var plan = await JsonUtil.ReadAsync<RegenPlan>(fs.RegenPlanPath)
                   ?? throw new InvalidOperationException("regen_plan.json not found; run diff first.");

        var topPatterns = ParseOptionalInt(options, "--top-patterns-per-command");
        var maxCommands = ParseOptionalInt(options, "--top-commands");
        var filteredPlan = plan;
        if (maxCommands.HasValue)
        {
            filteredPlan = new RegenPlan
            {
                Version = plan.Version,
                GeneratedAt = plan.GeneratedAt,
                Commands = plan.Commands
                    .OrderByDescending(c => patterns.Commands.TryGetValue(c.Command, out var cp) ? cp.TotalUses : 0)
                    .ThenBy(c => c.Command, StringComparer.Ordinal)
                    .Take(maxCommands.Value)
                    .ToList()
            };
        }

        var exporter = new PromptExportWriter(fs);
        await exporter.WriteAsync(patterns, filteredPlan, topPatterns);
        Console.WriteLine($"prompt-export: wrote {filteredPlan.Commands.Count} web prompt(s) to {Path.Combine(fs.OutputDir, "prompts_web")}");
        if (filteredNote is not null) Console.WriteLine(filteredNote);
        return 0;
    }

    private static async Task<int> RunAll(string[] args)
    {
        var (options, _) = OptionParser.Parse(args);
        var fs = BuildLayout(options);

        var aggregator = new Aggregator(fs);
        var rawPatterns = await aggregator.RunAsync();
        var (patterns, filteredNote) = await ApplyFilters(fs, options, rawPatterns);

        var diff = new DiffService(fs);
        var maxCommands = ParseOptionalInt(options, "--top-commands");
        var plan = await diff.RunAsync(patterns, maxCommands);

        var writer = new PromptWriter(fs);
        var topPatterns = ParseOptionalInt(options, "--top-patterns-per-command");
        await writer.WriteAsync(patterns, plan, topPatterns);

        var generator = options.TryGetValue("--generator", out var gen) ? gen : "none";
        var skipExternal = string.Equals(generator, "none", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(generator, "dry-run", StringComparison.OrdinalIgnoreCase);
        if (!skipExternal)
        {
            Console.WriteLine("run: external generator not wired; use --generator none or --generator dry-run");
            return 1;
        }
        if (string.Equals(generator, "dry-run", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("run: dry-run (writing placeholders only)");
        }

        var entries = new EntryWriter(fs);
        await entries.WritePlaceholderAsync(patterns, plan, topPatterns, generator);
        var state = new StateUpdater(fs);
        await state.UpdateAsync(patterns, plan);
        if (filteredNote is not null) Console.WriteLine(filteredNote);

        Console.WriteLine("run: completed aggregate -> diff -> prepare -> generate (placeholder)");
        return 0;
    }

    private static async Task<(PatternsSnapshot snapshot, string? note)> LoadFilteredPatterns(FileSystemLayout fs, Dictionary<string, string> options)
    {
        var patterns = await JsonUtil.ReadAsync<PatternsSnapshot>(fs.PatternsPath)
                       ?? throw new InvalidOperationException("patterns.json not found; run aggregate first.");
        return await ApplyFilters(fs, options, patterns);
    }

    private static async Task<(PatternsSnapshot snapshot, string? note)> ApplyFilters(FileSystemLayout fs, Dictionary<string, string> options, PatternsSnapshot patterns)
    {
        var configLoader = new ConfigLoader(fs);
        var config = await configLoader.LoadAsync();
        var filter = new PatternFilter();
        var result = filter.Apply(patterns, config);
        var note = result.CommandsFiltered > 0
            ? $"filters: skipped {result.CommandsFiltered} command(s) ({result.UsesFiltered} uses) via config.json"
            : null;
        return (result.Snapshot, note);
    }

    private static int? ParseOptionalInt(Dictionary<string, string> options, string key)
    {
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return null;
    }
}
