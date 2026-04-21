# Claude Code setup

The `.claude/` directory holds Claude Code configuration for this project:

- `.claude/skills/<name>/SKILL.md` — project-specific skills invocable as `/<name>`. Current skills:
  - `/review-pr` — deep PR review. Spawns `code-reviewer` and `baselines-reviewer` subagents in parallel and posts a pending draft review after user confirmation.
  - `/verify-review` — follow-up that re-checks prior `/review-pr` findings against current PR HEAD, flips checkboxes in place, posts a new draft for partial fixes and genuinely new findings.
  - `/api-baselines` — refreshes `CompatibilitySuppressions.xml` under `Source/`; flags non-`LinqToDB.Internal.*` API changes for explicit user approval.
  - `/version-bump` — bumps `Directory.Build.props` `<Version>` and per-EF `<EFxVersion>` to match the next release milestone.
  - `/update-slnx` — syncs the `/.claude/*` virtual folders in `linq2db.slnx` with the on-disk contents of `.claude/`.
- `.claude/docs/*.md` — reference material linked or imported from `CLAUDE.md`. `agent-rules.md` is auto-imported into every Claude Code session; the others (`architecture.md`, `testing.md`, `claude-setup.md`) are linked and loaded on demand when the task touches that area.
- `.claude/settings.local.json` — **gitignored**. Personal project-level overrides: permissions allowlists, model/effort preferences, env vars. Created on demand; `{}` is a valid starting content.

The project deliberately does **not** commit a `.claude/settings.json`. Hooks, statuslines, and global preferences belong in your user profile (`~/.claude/`), not in the repo. That means nothing in the repo enforces the agent-side rules — compliance depends on you (and any hooks you install personally; see the Bash command rules in `.claude/docs/agent-rules.md`).

Settings precedence: project-local `.claude/settings.local.json` > user-level `~/.claude/settings.json` > Claude Code defaults.
