# Claude Code setup

The `.claude/` directory holds Claude Code configuration for this project:

- `.claude/skills/<name>/SKILL.md` — project-specific skills invocable as `/<name>`. Current skills: `/version-bump` (bumps `Directory.Build.props` versions against the next milestone) and `/update-slnx` (syncs the `/.claude/*` virtual folders in `linq2db.slnx`).
- `.claude/docs/*.md` — reference material linked or imported from `CLAUDE.md`. `agent-rules.md` is auto-imported into every Claude Code session; the others (`architecture.md`, `testing.md`, `claude-setup.md`) are linked and loaded on demand when the task touches that area.
- `.claude/settings.local.json` — **gitignored**. Personal project-level overrides: permissions allowlists, model/effort preferences, env vars. Created on demand; `{}` is a valid starting content.

The project deliberately does **not** commit a `.claude/settings.json`. Hooks, statuslines, and global preferences belong in your user profile (`~/.claude/`), not in the repo. That means nothing in the repo enforces the agent-side rules — compliance depends on you (and any hooks you install personally; see the Bash command rules in `.claude/docs/agent-rules.md`).

Settings precedence: project-local `.claude/settings.local.json` > user-level `~/.claude/settings.json` > Claude Code defaults.
