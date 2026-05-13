# Audit-claude per-check rules

Per-check rules invoked from [`/audit-claude`](../skills/audit-claude/SKILL.md) → step 2 ("Run the eight checks"). The skill's orchestration (enumerate corpus → run checks → assemble report → offer patches → apply slnx → report) lives in the SKILL.md; the rules below are the body of step 2.

Each check produces zero or more finding records of the shape defined in the SKILL.md ("Run the eight checks" intro). Run them in parallel where possible; checks are mostly independent.

## 2a. Dead-reference check

For every Markdown link `[text](path)`, `path]` reference, `@path` import, and every inline `file:line` reference in a `.md` or `.ps1` file, resolve `path` against the repo root. Flag paths that don't exist. Skip external URLs (`http://`, `https://`, `mailto:`) and absolute Windows/Unix paths outside the repo. For `@path` in `CLAUDE.md` (Claude Code's import syntax), check the exact path.

Proposed fix (mechanical when the file was renamed and there's one plausible new target; creative when ambiguous): suggest the new path or ask.

## 2b. Slnx-mismatch check

Diff on-disk `.claude/` files against `linq2db.slnx` entries starting with `.claude/` or equal to `CLAUDE.md`.

- **On disk, not in slnx** — error unless the file is gitignored AND not an always-included exception (`.claude/settings.local.json`). Fix: run `/update-slnx`.
- **In slnx, not on disk** — error unless the file is an always-included exception. Fix: run `/update-slnx`.

Don't emit individual edit patches for slnx mismatches — the slnx structure is owned by `/update-slnx`. Emit a single aggregated finding pointing at the skill.

## 2c. Template-gap check

For each `.claude/skills/*/SKILL.md`:

- Frontmatter exists and contains `name:` and `description:`.
- H1 (`# <name>`) exists.
- A section with any of these headings exists: "When to run", "When to use", "Trigger", "Scope".
- A section with any of these exists: "Steps", "Workflow", "Procedure".

Missing frontmatter field → error. Missing H1 → error. Missing "When to run"-ish → warning. Missing "Steps"-ish → warning. Missing "Don'ts" / "Don't" → info.

For agents (`.claude/agents/*.md`) the frontmatter must contain `name`, `description`, `tools`, and `model`. The body should contain an "Inputs" or "When invoked" section. Flag missing frontmatter fields as error; missing body sections are info-level at most.

**Subagent model checks (extension of frontmatter validation):**

- **`model:` set explicitly.** Field missing or empty → error. Every subagent pins its model so it doesn't silently inherit the parent's model — leaving it unset leaks model selection between contexts and makes audit/cost analysis fragile.
- **Recognized short name.** `model:` value not in `{opus, sonnet, haiku}` → error. Specific full IDs like `claude-sonnet-4-6` pin to a point release that ages out; suggest the short name in the proposed fix unless the user explicitly indicates they want version-pinning.
- **Model fit** (warning, `fixKind: creative`). Convention: `opus` for heavy synthesis (`code-reviewer`), `sonnet` as default for moderate analysis (`baselines-reviewer`, `test-writer`, `kb-research`, `kb-architect`, `kb-historian`, `kb-issue-detector`), `haiku` for narrow execution (`test-runner`, `kb-github-curator`). Flag when the assignment looks off — e.g. a review / audit / synthesis subagent (description contains "review", "audit", "judgment", "compare") pinned to `haiku`, or a pure-execution subagent (description is dominated by "run", "fetch", "execute" without a synthesis step) pinned to `opus`. The user owns the call; surface as creative so they can keep, change, or override.

Proposed fix for missing-section / scaffold gaps: mechanical — suggest the scaffold of the missing section pulled from the median existing skill. The body still needs the author's content (creative).

## 2d. Duplicated-rule check

Grep-and-compare. For each of the following "canonical rule" patterns, find every occurrence and check for wording divergence:

- Branch-naming rules (who owns them: `CLAUDE.md` → Branch Conventions, `.claude/docs/agent-rules.md` → Creating a new branch). If both state the *schema* with different words, flag as duplicated-rule; if one is the authoritative statement and the other is a one-line pointer, that's fine.
- Commit / push / PR approval rules.
- Bash chaining / permission-friendly patterns.
- GitHub content-editing guardrails.
- Memory system rules.

This is not a full-text dedup — it's a fixed list of "rules that must live in one place". Update the list in this doc when a new cross-cutting rule appears. Proposed fix: creative — propose canonical location + pointer in the other file; user confirms.

## 2e. Retired-path check

`Grep` the corpus for path-ish tokens (`Source/\S+`, `Tests/\S+`, `Build/\S+`, `Data/\S+`, `.claude/\S+`) and check each against `Glob`. Flag tokens that look like paths and don't resolve. Skip ones inside code fences that are illustrative examples (common pattern: `Source/LinqToDB/...` as a placeholder).

Distinguish: a path that never existed (probably an example) vs. a path that clearly used to exist. Heuristic: if the parent directory exists and the filename has the standard repo shape (`<PascalCase>.cs`, `<kebab>.md`), it's likely a real retired path.

Proposed fix: mechanical when a rename with one plausible target; creative otherwise.

## 2f. Terminology-drift check

`Grep` for each glossary-listed variant (`sub-agent`, `slash command`, `user-initiated`, `database driver`, `DB adapter`, and lowercase `linq to db` outside code fences). Flag occurrences. Proposed fix: mechanical single-word replacement, but skip flagging legitimate uses inside code fences or when the variant is in a quoted user prompt / external tool name.

## 2g. Refactor-candidate check

Line-count-driven, with stricter thresholds for the **always-loaded payload** — `CLAUDE.md` plus every doc reachable via `@import` chain from it. Those bytes are paid on every conversation, so a 100-line CLAUDE.md or a 250-line auto-imported doc deserves a louder signal than a fat skill that only loads when invoked.

**Always-loaded (warning, fixKind: creative):**

- `CLAUDE.md` over 100 lines — propose moving verbose sections into focused `.claude/docs/<topic>.md` files, leaving one-line pointers in their place. Sections already shaped as a single-line pointer don't count toward the budget; focus the proposal on the long-form sections that drive the count up.
- Any doc reachable via `@<path>` from `CLAUDE.md` over 250 lines (e.g. `.claude/docs/agent-rules.md`) — propose splitting by topic. Keep the most-referenced sections inline; move lower-traffic sections (large recipes, niche gotchas, single-use procedures) into focused docs and replace with a pointer.

For each always-loaded oversize finding, propose a concrete split (which sections move where, what pointer stays). The `proposedFix` is the per-section breakdown rather than a single unified diff — the user picks how aggressive to be.

**Per-skill / per-script (info, fixKind: manual-only):**

- `SKILL.md` over 250 lines — suggest factoring shared procedure into `.claude/docs/`.
- Two skills whose procedures overlap by more than half (heuristic: share > 50% of H3 section titles) — suggest a shared doc.
- `.ps1` over 300 lines with no helper functions in `_shared.ps1` — suggest extracting.
- `.claude/docs/*.md` referenced by exactly one skill — suggest inlining.

Per-skill / per-script findings stay manual-only — log the candidate with a one-line rationale, don't propose a specific patch.

## 2h. Memory-promotion check

Inspect the user's auto-memory store for entries whose content would help every agent on this codebase, not only the current user. Promote those into `.claude/` so the rule lives where it can be reviewed, version-controlled, and applied to other contributors' Claude Code sessions; leave personal memories where they are.

**Locate the memory directory.** Read the path from this session's `# auto memory` system-prompt section. If the directory doesn't exist or `MEMORY.md` is absent / empty, skip the check entirely (zero findings — never fabricate candidates).

**Per-entry triage.** For each pointer in `MEMORY.md`, `Read` the linked memory file. Read the frontmatter `type:` plus the body. Classify by type:

| Type | Promotion candidate? | Heuristic |
|---|---|---|
| `user` | Never. | User-personal by definition (role, knowledge profile, preferred response style). Promoting these is a category error — they describe one human, not the codebase. |
| `feedback` | Sometimes. | Project-truthy when the rule is **incident-driven** ("we got burned when…", "CI rejects X because…", "the slnx skill prompts twice when…") or **tooling-driven** (shape-of-tool requirements that any agent on this repo would hit). Personal when the rule is a **preference** ("I like terse responses", "no apologies"). The `**Why:**` line is the strongest signal — incidents and tool failures generalise; tastes don't. |
| `project` | Sometimes. | Project-truthy when the fact is **durable and codebase-relevant** — a milestone driving prioritisation, a stakeholder ask shaping scope, a long-running initiative. Skip when the fact is **conversation-scoped** ("currently fixing #5414") or about the user's own queue. |
| `reference` | Sometimes. | Project-truthy when the pointer is to a **project-shared resource** — a Linear board, dashboard, or wiki for *this* repo / service. Skip when it's a **personal tool** the user uses to organise their own work. |

**Per candidate, propose a target location.** Match content shape to the canonical destination — the user picks, but lead with a concrete suggestion:

| Memory shape | Likely destination |
|---|---|
| Workflow rule with **Why:** + **How to apply:** (mid-task discipline, agent guardrail) | `.claude/docs/agent-rules.md` (new bullet under the matching section) |
| Architecture / design fact about the codebase | `.claude/docs/architecture.md` or `.claude/docs/code-design.md` |
| Skill-specific rule that only applies inside one skill's flow | the relevant `.claude/skills/<name>/SKILL.md` (its `Don'ts` or workflow section) |
| Cross-cutting external resource pointer | `CLAUDE.md` (top-level reference list) or the most relevant doc |
| Subagent-level rule | the subagent's `.claude/agents/<name>.md` |

**Severity:** info. **fixKind:** creative — promotion always needs a voice rewrite (first-person → imperative, "I" → "the agent", drop user names) and a placement decision the audit can't make alone. Surface the proposal; don't auto-apply.

**The audit never edits the memory file.** Promotion is a one-way copy: write the rephrased rule into `.claude/`; the user decides separately whether to keep, rewrite, or `/forget` the memory entry. Removing memory is the user's prerogative — even when the same content has just been promoted to project level.
