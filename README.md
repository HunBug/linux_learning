# Terminal Cheatsheet Builder (planning)
Tooling (planned) to ingest shell histories from multiple machines, normalize command usage, detect significant changes, and regenerate structured cheatsheets incrementally.

## Repository status
- Planning and scaffolding in place; CLI skeleton created for .NET 8 / C#.
- Core system plan lives in [initializing.md](initializing.md) with details on goals and pipeline stages.
- Active plan and open questions are tracked in [docs/plan.md](docs/plan.md).

## Intent (v1)
- Import histories (bash/zsh/fish) from multiple hosts into `raw/` via SSH copy; no remote installs or agents.
- Normalize commands into patterns (command, subcommand, flags, options, argument shapes).
- Detect new or changed patterns and regenerate only the affected cheatsheet entries.
- Keep structured JSON outputs per command and later render aggregated Markdown views (renderer details deferred; focus on producing its inputs).
- Prefer local generation via Codex CLI; support “prepare prompts only” mode.

- `raw/` — dropped shell history inputs.
- `data/`, `snapshots/` — aggregated stats and per-run artifacts.
- `patterns.json`, `state.json` — inventories and generation state.
- `prompts/` — per-command JSON prompt bundles for model runs.
- `cheatsheets/entries/` — structured JSON command entries.
- `output/` — rendered Markdown (all/focus views, to be built later).
- `docs/` — planning and design notes.
## Planned layout
- `raw/` — dropped shell history inputs.
- `data/`, `snapshots/` — aggregated stats and per-run artifacts.
- `patterns.json`, `state.json` — inventories and generation state.
- `prompts/` — per-command JSON prompt bundles for model runs.
- `cheatsheets/entries/` — structured JSON command entries.
- `output/` — rendered Markdown (all/focus views, to be built later).
- `docs/` — planning and design notes (schemas in `docs/schemas/`).
- `src/` — .NET 8 CLI source.
- `raw/` — dropped shell history inputs.
- `data/`, `snapshots/` — aggregated stats and per-run artifacts.
- `patterns.json`, `state.json` — inventories and generation state.
- `prompts/` — per-command JSON prompt bundles for model runs.
- `cheatsheets/entries/` — structured JSON command entries.
- `output/` — rendered Markdown (all/focus views, to be built later).
- `docs/` — planning and design notes.

## Next steps
- Implement aggregation/diff/prompt preparation/generation in the CLI skeleton (`src/TerminalCheats.Cli`).
- Validate prompts and entries against JSON schemas in `docs/schemas/`.
- Add sample history fixtures for tests; renderer work comes later.
