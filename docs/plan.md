# Project Plan

## Objectives
- Aggregate shell histories from multiple machines/shells (collected via SSH, no agents installed remotely) into normalized command patterns.
- Detect significant usage changes and regenerate only affected cheatsheet entries.
- Keep outputs structured (per-command entries) and render Markdown views (all/focus) once the renderer exists.
- Prefer local Codex CLI generation; support a "prepare prompts only" path.

## Scope (first implementation)
- History import + normalization into pattern signatures and per-command inventories.
- State tracking + diff to decide which commands need regeneration.
- Prompt preparation per command (full context bundle) as JSON inputs.
- Codex CLI integration to generate structured entries; schema validation + write to `cheatsheets/entries/` in JSON.
- Renderer inputs produced, but renderer details deferred; focus on supplying complete data for rendering.
- Ignore rules are out of scope for v1 (keep architecture ready but no implementation now).

## Pipeline (desired CLI stages)
1) `tool aggregate`: read `raw/`, normalize commands, emit `stats.json`, `patterns.json`, and `snapshots/<run-id>.json`.
2) `tool diff`: compare `patterns.json` vs `state.json` to produce regen plan.
3) `tool prepare`: write JSON prompt bundles per command needing regeneration into `prompts/`.
4) `tool generate`: run Codex CLI (or skip with `--generator none`), validate JSON output, write JSON entries.
5) `tool render`: build `output/cheatsheet-all.md` and `output/cheatsheet-focus.md` from entries (renderer logic later; ensure inputs are ready now).
6) `tool run`: end-to-end aggregate → diff → prepare → generate → render.

## Data + files (planned)
- Inputs: `raw/` history drops; optional metadata (host, shell, timestamp) when available. Histories will be copied locally via SSH (no remote installs).
- Working data: `data/`, `patterns.json`, `snapshots/`, `state.json` (hashes, timestamps, per-command status).
- Prompts: `prompts/<command>.prompt.json` containing sanitized examples + pattern inventory.
- Entries: `cheatsheets/entries/<command>.json`, stable per command.
- Outputs: `output/cheatsheet-all.md`, `output/cheatsheet-focus.md` (renderer to be implemented later).
- Schemas: prompt and entry JSON schemas in `docs/schemas/prompt.schema.json` and `docs/schemas/entry.schema.json`.

## Open questions
- Sanitization defaults: redact tokens/credentials and collapse home paths/hostnames in examples—any extra redactions needed?
- Renderer ordering (when we build it): frequency vs alphabetical for all, recent vs learning-value for focus.

## Immediate actions (suggested)
- Build aggregation + diff pipeline in the new CLI (skeleton in place).
- Wire prompt generation and schema validation against `docs/schemas/prompt.schema.json` and `docs/schemas/entry.schema.json`.
- Add sample history files + sanitized fixtures for tests once pipeline exists.
