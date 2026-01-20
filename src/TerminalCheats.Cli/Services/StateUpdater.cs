using TerminalCheats.Cli.Models;

namespace TerminalCheats.Cli.Services;

public sealed class StateUpdater
{
    private readonly FileSystemLayout _fs;

    public StateUpdater(FileSystemLayout fs)
    {
        _fs = fs;
    }

    public async Task<StateSnapshot> UpdateAsync(PatternsSnapshot patterns, RegenPlan plan, CancellationToken ct = default)
    {
        var state = await JsonUtil.ReadAsync<StateSnapshot>(_fs.StatePath, ct)
                    ?? new StateSnapshot { Version = "0.1", GeneratedAt = DateTimeOffset.UtcNow };

        foreach (var item in plan.Commands)
        {
            var entryPath = Path.Combine(_fs.EntriesDir, $"{item.Command}.json");
            state.Commands[item.Command] = new StateEntry
            {
                PatternsHash = item.PatternsHash,
                EntryPath = entryPath,
                GeneratedAt = DateTimeOffset.UtcNow
            };
        }

        state.GeneratedAt = DateTimeOffset.UtcNow;
        await JsonUtil.WriteAsync(_fs.StatePath, state, ct);
        return state;
    }
}
