# Claude Code setup

Claude Code configuration for this project lives under `.agents/` — the single agent-instruction source shared with Codex (`AGENTS.md`) and Copilot. `.claude/` is a **symlink to `.agents/`**: Claude Code hardcodes discovery of skills / subagents / hooks / settings to `.claude/`, and the symlink resolves that to `.agents/`. So `.claude/...` and `.agents/...` paths both point at the same files; the docs below use the canonical `.agents/...` form.

> **Windows checkout note.** The `.claude` → `.agents` symlink only materialises as a real link when Git has symlink support enabled. Without it (`core.symlinks=false` — the Windows default unless you clone from an elevated shell or with Developer Mode on), Git writes `.claude` as a plain ~9-byte text file containing the literal string `.agents`, and discovery under `.claude/` silently sees that text file instead of the tree (a confusing empty-`.claude/` failure). Enable it before cloning — `git config --global core.symlinks true` — and clone with Developer Mode on or from an elevated shell. If you already cloned, set the config, then re-checkout the link: `git checkout -- .claude`.

- `.agents/skills/<name>/SKILL.md` — project-specific skills invocable as `/<name>`. The full catalogue (one row per skill, with staleness signals for the periodic ones) lives in [`/chores`](../skills/chores/SKILL.md); skills are also visible to Claude Code at session start via the system-reminder list. To know "what runs when", read either of those — don't maintain a parallel list here.
- `.agents/agents/<name>.md` — subagent contracts invoked from skills (e.g. `code-reviewer`, `kb-architect`).
- `.agents/scripts/*.ps1` — PowerShell Core helpers used by skills and subagents (multi-step `gh` / `git` orchestrations live here, not in raw Bash chains). Authoring contract: [`script-authoring.md`](script-authoring.md).
- `.agents/docs/*.md` — reference material linked or imported from `CLAUDE.md`. `agent-rules.md` is auto-imported into every Claude Code session; the others are linked and loaded on demand.
- `.agents/rules/*.md` — path-scoped conditional rules. A rule with a `paths:` frontmatter glob list loads into context only when Claude opens/edits a file matching one of its globs (a rule with no `paths:` loads at session start like `CLAUDE.md`). Use these to auto-surface guidance that only matters for specific source trees — e.g. `cross-cutting-core.md` points at the `code-design.md` AST/translator invariants when editing `Source/LinqToDB/**/SqlQuery/**` or `**/Translation/**`. Frontmatter schema is `paths:`-only (undocumented keys risk a strict parser dropping the rule). Path-matching through the `.claude`→`.agents` symlink requires Claude Code **v2.1.198+**.
- `.agents/hooks/*.ps1` — opt-in PreToolUse / PostToolUse / SessionEnd hooks (wired in via `.agents/settings.local.json`).
- `.agents/settings.local.json` — **gitignored**. Personal project-level overrides: permissions allowlists, hook wiring, model/effort preferences, env vars. Created on demand; `{}` is a valid starting content.

The project deliberately does **not** commit a `.agents/settings.json`. Hooks, statuslines, and global preferences belong in your user profile (`~/.claude/`), not in the repo. That means nothing in the repo enforces the agent-side rules — compliance depends on you (and any hooks you install personally; see the Bash command rules in `.agents/docs/agent-rules.md`).

Settings precedence: project-local `.agents/settings.local.json` > user-level `~/.claude/settings.json` > Claude Code defaults.

## Agent-agnostic corpus guidance lives elsewhere

Guidance that applies to **any** agent maintaining this corpus — authoring long instruction docs ("lost in the middle" placement), the supply-chain risk of editing skills/hooks/agents, and the eval-harness proposal — has moved to [`maintaining-the-corpus.md`](maintaining-the-corpus.md). The rule for treating untrusted fetched agent/editor config as executable is in [`AGENTS.md`](../../AGENTS.md) → *Security*. This file keeps only the Claude-Code-specific harness mechanics below.

## Permission allowlist syntax

When adding entries to `permissions.allow` in `.agents/settings.local.json`:

- **Prefix-match wildcard is space-then-asterisk**, not colon-then-asterisk: `Bash(git fetch *)` — *not* `Bash(git fetch:*)`. The `:*` form is obsolete; current Claude Code matching expects ` *`, and the rest of the file already uses ` *` consistently.
- **Exact-match patterns carry no wildcard at all**: `Bash(git status)` — not `Bash(git status*)` and not `Bash(git status:*)`.
- PowerShell-script entries follow the prefix convention: `Bash(pwsh -NoProfile -File .agents/scripts/<name>.ps1 *)`. Inserting `-NonInteractive` between `-NoProfile` and `-File` breaks the prefix match — see [`agent-rules.md`](agent-rules.md) → *Permission-friendly patterns*.
- **Allowlist target is `settings.local.json`, always.** This project doesn't commit a `.agents/settings.json` — every allowlist-touching skill (`/fewer-permission-prompts` and any future ones) writes into `.agents/settings.local.json`, even when the skill's own default points at `settings.json`. Do not create `settings.json`. Merge into the existing local file, dedupe against what's already there, and don't reorder unrelated keys.

## Harness mechanics the corpus relies on

A few Claude Code internals that several `.claude/` rules quietly depend on — documented here so the rationale is visible (behaviors observed on the v2.1.x line; re-verify if the harness changes):

- **A background subagent can't show a permission dialog, so a gated action becomes a silent `deny`.** An agent spawned to run in the background that hits a permission-gated tool gets it auto-denied rather than queued for approval. This is *why* [`agent-rules.md`](agent-rules.md) → *Agent guardrails* says to frame subagent prompts to allow failure and to verify subagent output with `git status` — a background agent that "couldn't" may simply have been denied a tool, not actually blocked by the task.
- **Compaction paraphrases; it does not preserve recent turns verbatim.** When context auto-compacts, prior turns are replaced by a summary plus recently-accessed files — in-context recall of an exact string, line number, or decision is lossy afterward. Persist load-bearing facts to disk (`.build/.agents/…`, the knowledge base, a doc) rather than trusting they survive a compaction. This underwrites the *Temp files* rule and the KB's existence.
- **One tool call failing cancels its dependent siblings in the same batch.** Batch only genuinely independent calls in a single turn (the *Batch independent tool calls* rule); a dependent call chained into a parallel batch can be cancelled when an earlier sibling errors, so true dependencies stay sequential.
- **`bypassPermissions` still protects `.claude/`, `.git/`, and shell-config paths.** Even with permissions bypassed, edits to those trees stay gated — consistent with the curation discipline that `.claude/` is committed only on `infra/agents-curation`. **Caveat after the `.agents/` move:** this guard appears to be keyed on the literal `.claude/` path. The real files now live under `.agents/` (with `.claude` a symlink to it), so an edit addressed directly to `.agents/...` may *not* trip the same protection — the guard matches the `.claude/` spelling, not the resolved target. Treat `.agents/` edits with the same care; don't rely on the bypass guard to catch them. (Behavior unverified against the symlink layout — re-check if you depend on it.)

A fuller reverse-engineering of these internals (context-management tiers, autocompact buffer, hook return codes) is external write-up territory, not corpus material — the four above are the ones a rule here leans on.
