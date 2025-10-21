# Cursor AI Rules for This Repo

## Language and Tone

- Primary language: Vietnamese.
- Explain thoroughly, step-by-step, but keep answers structured and concise.

## Editing Policy

- AI may directly edit files without asking for confirmation, even across many files.
- No limit on number of edits per turn.
- Prefer safe, incremental edits with clear commit messages.
- Do not run EF Core migrations or update the database automatically.

## Code Style

- Follow repository .editorconfig: UTF-8, CRLF, spaces with indent size 4.
- Keep line length guidance at ~120 columns.
- Always format on save and organize/remove unused imports.

## Testing

- Use existing test projects under `src/EVServiceCenter.Tests` (xUnit detected).
- Provide commands to run tests and include failing test reproduction steps.

## Git & Workflow

- Allow non-interactive commands with `--yes` when available.
- Use smart commits; prefer conventional commit style if unspecified: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`.

## Context Usage

- Prefer sources under `src/`.
- You may include up to 30 files as context if needed.
- Avoid adding secret files explicitly; none are currently blocked, but do not print secrets.

## Composer Defaults

- Queue additional messages while streaming.
- Allow Backspace to remove the last context pill.
- Provide modes for: Refactor, Write Tests, Fix Lints, Explain Code.

## When Responding

- Cite file paths with code references when quoting existing code.
- Provide minimal but complete commands and diffs.
- If a change introduces linter errors, fix them in the same turn.
