using System.IO;

namespace TerminalCheats.Cli.Services;

public sealed class FileSystemLayout
{
    public FileSystemLayout(string root)
    {
        Root = Path.GetFullPath(root);
    }

    public string Root { get; }
    public string RawDir => Path.Combine(Root, "raw");
    public string DataDir => Path.Combine(Root, "data");
    public string SnapshotsDir => Path.Combine(Root, "snapshots");
    public string PromptsDir => Path.Combine(Root, "prompts");
    public string EntriesDir => Path.Combine(Root, "cheatsheets", "entries");
    public string OutputDir => Path.Combine(Root, "output");
    public string PatternsPath => Path.Combine(Root, "patterns.json");
    public string StatePath => Path.Combine(Root, "state.json");
    public string RegenPlanPath => Path.Combine(Root, "data", "regen_plan.json");

    public void EnsureBaseFolders()
    {
        Directory.CreateDirectory(RawDir);
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(SnapshotsDir);
        Directory.CreateDirectory(PromptsDir);
        Directory.CreateDirectory(EntriesDir);
        Directory.CreateDirectory(OutputDir);
    }
}
