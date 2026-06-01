# Claude Code setup

The `.claude/` directory holds Claude Code configuration for this project:

- `.claude/skills/<name>/SKILL.md` — project-specific skills invocable as `/<name>`. The full catalogue (one row per skill, with staleness signals for the periodic ones) lives in [`/chores`](../skills/chores/SKILL.md); skills are also visible to Claude Code at session start via the system-reminder list. To know "what runs when", read either of those — don't maintain a parallel list here.
- `.claude/agents/<name>.md` — subagent contracts invoked from skills (e.g. `code-reviewer`, `kb-architect`).
- `.claude/scripts/*.ps1` — PowerShell Core helpers used by skills and subagents (multi-step `gh` / `git` orchestrations live here, not in raw Bash chains). Authoring contract: [`script-authoring.md`](script-authoring.md).
- `.claude/docs/*.md` — reference material linked or imported from `CLAUDE.md`. `agent-rules.md` is auto-imported into every Claude Code session; the others are linked and loaded on demand.
- `.claude/hooks/*.ps1` — opt-in PreToolUse / PostToolUse / SessionEnd hooks (wired in via `.claude/settings.local.json`).
- `.claude/settings.local.json` — **gitignored**. Personal project-level overrides: permissions allowlists, hook wiring, model/effort preferences, env vars. Created on demand; `{}` is a valid starting content.

The project deliberately does **not** commit a `.claude/settings.json`. Hooks, statuslines, and global preferences belong in your user profile (`~/.claude/`), not in the repo. That means nothing in the repo enforces the agent-side rules — compliance depends on you (and any hooks you install personally; see the Bash command rules in `.claude/docs/agent-rules.md`).

Settings precedence: project-local `.claude/settings.local.json` > user-level `~/.claude/settings.json` > Claude Code defaults.

## Authoring long instruction docs

Long-context models degrade in the *middle* of a long context ("lost in the middle" / context rot): a rule reasoned about reliably when it sits near the start or end of a file is more likely to be skimmed past when buried mid-file. Treat instruction docs accordingly:

- **Put the highest-traffic, most-violated rules at the top or bottom of a long doc**, not in the middle. `CLAUDE.md`'s *anchor set* (most-violated guardrails kept inline, lower-traffic ones delegated to imported docs) and `agent-rules.md`'s *one-line triggers → detail doc* pattern both exist for this reason — follow them when a doc grows.
- When an always-loaded doc is over budget (see `/audit-claude` → *Refactor candidate*) and can't be shortened, **hoist its load-bearing rule to the top/bottom** rather than leaving it mid-file.
- This is a placement convention, not a license to duplicate: one canonical statement, positioned where it'll actually be read.

## Permission allowlist syntax

When adding entries to `permissions.allow` in `.claude/settings.local.json`:

- **Prefix-match wildcard is space-then-asterisk**, not colon-then-asterisk: `Bash(git fetch *)` — *not* `Bash(git fetch:*)`. The `:*` form is obsolete; current Claude Code matching expects ` *`, and the rest of the file already uses ` *` consistently.
- **Exact-match patterns carry no wildcard at all**: `Bash(git status)` — not `Bash(git status*)` and not `Bash(git status:*)`.
- PowerShell-script entries follow the prefix convention: `Bash(pwsh -NoProfile -File .claude/scripts/<name>.ps1 *)`. Inserting `-NonInteractive` between `-NoProfile` and `-File` breaks the prefix match — see [`agent-rules.md`](agent-rules.md) → *Permission-friendly patterns*.
- **Allowlist target is `settings.local.json`, always.** This project doesn't commit a `.claude/settings.json` — every allowlist-touching skill (`/fewer-permission-prompts` and any future ones) writes into `.claude/settings.local.json`, even when the skill's own default points at `settings.json`. Do not create `settings.json`. Merge into the existing local file, dedupe against what's already there, and don't reorder unrelated keys.
