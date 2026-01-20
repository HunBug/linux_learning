# Copilot Instructions

## Context
- Project: local tool to aggregate shell histories, detect significant usage changes, and regenerate per-command cheatsheet entries incrementally.
- Source of truth for requirements: `initializing.md` and `docs/plan.md`.

## Coding preferences
- Language: .NET 8 / C# CLI; prefer BCL before adding deps.
- Style: small, composable functions; pure where possible. Use strong typing and clear models.
- Error handling: fail fast with clear messages; no silent skips. Validate inputs and schemas.
- I/O: keep everything local; never send secrets. Sanitize examples (tokens, home paths, hosts/users) before writing prompts or entries.
- Data: deterministic ordering for outputs; stable file names/IDs per command. Avoid unnecessary regeneration.
- Files: ASCII only unless the file already uses Unicode. Keep comments minimal and helpful around non-obvious logic.
- Tests: use xUnit or NUnit for .NET; add fixtures for sample histories and expected normalized outputs when implementing pipeline.

## Workflow guidance
- Preserve user-authored content; do not delete existing plan docs.
- Implement CLI subcommands: `aggregate`, `diff`, `prepare`, `generate`, `render`, `run`. Allow `--generator none` to skip model calls.
- Ignore rules are future work; keep architecture ready but do not implement now.
- Output formats: structured JSON entries and prompts; renderer to be built later (produce its inputs now).
- Logging: prefer structured/debug-friendly logs; avoid chatty stdout unless verbose flag is set.

## Pull request etiquette
- Include short rationale in commit messages. Note breaking changes or schema updates.
- Update docs (`README.md`, `docs/plan.md`) when behavior or interfaces change.

## Collection constraints
- Shell histories gathered via SSH copy from other hosts; do not install agents or additional tooling remotely.
