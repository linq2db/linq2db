---
name: audit-claude
description: Audit the linq2db `.claude/` instruction corpus + `CLAUDE.md` + `.claude/scripts/` for duplicated rules, dead references, terminology drift, retired-path mentions, SKILL template gaps, and `linq2db.slnx` mismatches. Reports findings in severity order and offers per-finding patches after explicit user confirmation. Read-only until confirmation.
---

# /audit-claude

User-triggered static audit of the `.claude/` tree. Treats Claude Code instructions as code that can rot: duplicated rules drift apart, links break when files move, terminology slides ("agent" → "subagent" → "sub-agent"), and skills miss sections that make them harder to invoke consistently. This skill doesn't guess at intent — it enumerates observable problems and asks before touching anything.

## Scope

**In-scope.** Everything under `.claude/` plus the repo-root `CLAUDE.md`:

- `CLAUDE.md` — imported by Claude Code at session start.
- `.claude/docs/*.md` — long-form references imported by skills/agents.
- `.claude/agents/*.md` — subagent contracts.
- `.claude/skills/*/SKILL.md` — user-triggered skills.
- `.claude/scripts/*.ps1` — PowerShell helpers called by skills.
- `.claude/settings.local.json` — gitignored personal settings (audit for *presence* and slnx entry only, not content).

**Out of scope.** Project code (`Source/`, `Tests/`), other repo metadata (`.github/`, `Build/`, `Data/`), and the baselines clone at `../linq2db.baselines`. The audit doesn't second-guess the product itself — only the instructions the agent uses to work on it.

## Shared reference material

- **Agent rules** (branching, Bash, GitHub content): `.claude/docs/agent-rules.md`
- **Claude Code setup** (`.claude/` layout, settings precedence): `.claude/docs/claude-setup.md`
- **Slnx sync procedure**: `.claude/skills/update-slnx/SKILL.md`

## When to run

Only when the user explicitly asks to audit / lint / review the `.claude/` instructions. Do not invoke during unrelated work, even if you notice drift.

Reasonable cadence: once every few weeks, or after a batch of skill/doc changes lands. Running it back-to-back produces a near-empty report — a finding lingering through two audits is the signal that the fix is harder than it looks, not that the skill is under-reporting.

## What counts as a finding

Seven check categories, reported with a per-finding severity (**error** / **warning** / **info**):

| Category | Severity | Description |
|---|---|---|
| **Dead reference** | error | A link / path / `@import` points to a file that doesn't exist. Examples: `.claude/docs/foo.md` referenced from a skill after `foo.md` was renamed; `@.claude/docs/missing.md` at top of `CLAUDE.md`. |
| **Slnx mismatch** | error | A file exists under `.claude/` but isn't listed in `linq2db.slnx`, or `linq2db.slnx` lists a `.claude/` path that doesn't exist. Always-included entries (`CLAUDE.md`, `.claude/settings.local.json`) are exempt from the "missing on disk" variant. |
| **Template gap** | warning | A `SKILL.md` is missing a section that every other skill has. Baseline: YAML frontmatter (`name`, `description`), an H1, a "When to run" section, and a "Steps" or equivalent procedure section. Being missing the `Don'ts` section is a weaker warning (info). |
| **Duplicated rule** | warning | Two or more files restate the same normative rule with different wording. Flag when the content is the same and the wording diverges (otherwise identical copies are fine — the divergence is the problem). Propose consolidating into one canonical location and linking. |
| **Retired-path ref** | warning | A doc / skill mentions a file path or directory that no longer exists in the repo (not just `.claude/`). Common after refactors that move `Source/LinqToDB.Common → Source/LinqToDB/Internal/Common` etc. |
| **Terminology drift** | info | Inconsistent use of terms that should be uniform across the corpus. Baseline glossary — flag deviations from the left column: `subagent` (not `sub-agent` / `agent` when referring to `.claude/agents/*`); `skill` (not `slash command` when referring to `.claude/skills/*`); `user-triggered` (not `user-initiated`); `LINQ to DB` in user-facing prose, `linq2db` in code/paths; `provider` (not `database driver` / `DB adapter`). |
| **Refactor candidate** | info | Editorial smell — a single SKILL.md > 250 lines (consider factoring shared procedure into `.claude/docs/`); two skills with >50% overlapping procedure; a script > 300 lines without a `_shared.ps1` counterpart for common helpers; a doc that only exists to be imported by one skill (inline it). Always present these as **proposals**, not errors — the user decides. |

Out-of-scope non-findings (deliberately skipped): grammar / typos, stylistic preferences (active vs passive), formatting nits that don't affect rendered output. If the user wants those too, they can ask mid-audit; otherwise skip.

## Steps

### 1. Enumerate the corpus

Batched reads — `Glob` / `Read` can run in parallel:

- `Glob` for `.claude/**/*.md`, `.claude/scripts/*.ps1`, and read `CLAUDE.md`.
- Read `linq2db.slnx` once and extract every `<File Path="...">` whose path starts with `.claude/` or equals `CLAUDE.md`.
- Read `.gitignore` to identify gitignored paths under `.claude/` (for slnx exemption logic).

Produce three lists: **prose-files**, **script-files**, **slnx-claude-entries**.

### 2. Run the seven checks

Checks are mostly independent; run their searches in parallel where you can. Each check produces zero or more finding records of shape:

```json
{
  "id": "<category>-<short-slug>",
  "severity": "error|warning|info",
  "category": "dead-reference|slnx-mismatch|template-gap|duplicated-rule|retired-path|terminology-drift|refactor-candidate",
  "location": "<file>:<line-range or section>",
  "summary": "<one line>",
  "details": "<2–5 lines of context>",
  "fixKind": "mechanical|creative|manual-only",
  "proposedFix": "<unified diff, instruction, or null when manual-only>"
}
```

`fixKind` drives how step 4 offers the patch:
- **mechanical** — the fix is a single, obvious edit (broken link to updated path, add missing slnx entry, add missing section header). Show as a diff and offer to apply.
- **creative** — the fix involves a judgment call (how to merge two duplicated rules, which section to promote to the canonical doc). Surface the finding, propose an approach, let the user direct.
- **manual-only** — the fix needs non-local work (reorganize three files, split a skill). Log the finding, don't auto-patch.

#### 2a. Dead-reference check

For every Markdown link `[text](path)`, `path]` reference, `@path` import, and every inline `file:line` reference in a `.md` or `.ps1` file, resolve `path` against the repo root. Flag paths that don't exist. Skip external URLs (`http://`, `https://`, `mailto:`) and absolute Windows/Unix paths outside the repo. For `@path` in `CLAUDE.md` (Claude Code's import syntax), check the exact path.

Proposed fix (mechanical when the file was renamed and there's one plausible new target; creative when ambiguous): suggest the new path or ask.

#### 2b. Slnx-mismatch check

Diff on-disk `.claude/` files against `linq2db.slnx` entries starting with `.claude/` or equal to `CLAUDE.md`.

- **On disk, not in slnx** — error unless the file is gitignored AND not an always-included exception (`.claude/settings.local.json`). Fix: run `/update-slnx`.
- **In slnx, not on disk** — error unless the file is an always-included exception. Fix: run `/update-slnx`.

Don't emit individual edit patches for slnx mismatches — the slnx structure is owned by `/update-slnx`. Emit a single aggregated finding pointing at the skill.

#### 2c. Template-gap check

For each `.claude/skills/*/SKILL.md`:

- Frontmatter exists and contains `name:` and `description:`.
- H1 (`# <name>`) exists.
- A section with any of these headings exists: "When to run", "When to use", "Trigger", "Scope".
- A section with any of these exists: "Steps", "Workflow", "Procedure".

Missing frontmatter field → error. Missing H1 → error. Missing "When to run"-ish → warning. Missing "Steps"-ish → warning. Missing "Don'ts" / "Don't" → info.

For agents (`.claude/agents/*.md`) the template is looser but should contain frontmatter (`name`, `description`, `tools`) and an "Inputs" or "When invoked" section. Flag missing frontmatter as error; other sections are info-level at most.

Proposed fix: mechanical — suggest the scaffold of the missing section pulled from the median existing skill. Creative — section body still needs the author's content.

#### 2d. Duplicated-rule check

Grep-and-compare. For each of the following "canonical rule" patterns, find every occurrence and check for wording divergence:

- Branch-naming rules (who owns them: `CLAUDE.md` → Branch Conventions, `.claude/docs/agent-rules.md` → Creating a new branch). If both state the *schema* with different words, flag as duplicated-rule; if one is the authoritative statement and the other is a one-line pointer, that's fine.
- Commit / push / PR approval rules.
- Bash chaining / permission-friendly patterns.
- GitHub content-editing guardrails.
- Memory system rules.

This is not a full-text dedup — it's a fixed list of "rules that must live in one place". Update the list in-skill when a new cross-cutting rule appears. Proposed fix: creative — propose canonical location + pointer in the other file; user confirms.

#### 2e. Retired-path check

`Grep` the corpus for path-ish tokens (`Source/\S+`, `Tests/\S+`, `Build/\S+`, `Data/\S+`, `.claude/\S+`) and check each against `Glob`. Flag tokens that look like paths and don't resolve. Skip ones inside code fences that are illustrative examples (common pattern: `Source/LinqToDB/...` as a placeholder).

Distinguish: a path that never existed (probably an example) vs. a path that clearly used to exist. Heuristic: if the parent directory exists and the filename has the standard repo shape (`<PascalCase>.cs`, `<kebab>.md`), it's likely a real retired path.

Proposed fix: mechanical when a rename with one plausible target; creative otherwise.

#### 2f. Terminology-drift check

`Grep` for each glossary-listed variant (`sub-agent`, `slash command`, `user-initiated`, `database driver`, `DB adapter`, and lowercase `linq to db` outside code fences). Flag occurrences. Proposed fix: mechanical single-word replacement, but skip flagging legitimate uses inside code fences or when the variant is in a quoted user prompt / external tool name.

#### 2g. Refactor-candidate check

Line-count-driven:

- `SKILL.md` over 250 lines — suggest factoring shared procedure into `.claude/docs/`.
- Two skills whose procedures overlap by more than half (heuristic: share > 50% of H3 section titles) — suggest a shared doc.
- `.ps1` over 300 lines with no helper functions in `_shared.ps1` — suggest extracting.
- `.claude/docs/*.md` referenced by exactly one skill — suggest inlining.

Always manual-only. Log the candidate with a one-line rationale; don't propose a specific patch.

### 3. Assemble the report

Group findings by severity, then by category within severity. Display as a numbered list so the user can reference findings by number:

```
Audit report (17 findings):

Errors (4)
  1. [dead-reference] .claude/skills/review-pr/SKILL.md:42 → .claude/docs/old-review-docs.md (file missing)
  2. [dead-reference] CLAUDE.md:9 → @.claude/docs/agent-rules.md (file exists; check frontmatter casing)
  3. [slnx-mismatch] 2 files on disk not in linq2db.slnx: …
  4. [template-gap] .claude/skills/audit-claude/SKILL.md: missing frontmatter `description:`

Warnings (9)
  5. [duplicated-rule] Branch-naming rule restated with diverging wording in CLAUDE.md:38 and agent-rules.md:14
  …

Info (4)
  …
```

Per-finding detail line: one-line summary + `fixKind` tag + path to proposed fix (if any). Keep the top-level list scannable.

### 4. Offer patches

**Per-finding confirmation is the default.** For each finding with `fixKind: "mechanical"`, show:

- The current state (2–5 lines of surrounding context).
- The proposed diff (unified).
- Prompt: "apply / skip / batch-mechanical / abort".
  - `apply` → run the `Edit`, move to next finding.
  - `skip` → note as deferred, move on.
  - `batch-mechanical` → apply *all remaining* mechanical findings without further prompt; still pause for each creative / manual-only.
  - `abort` → stop the loop, leave any already-applied edits in place.

For `fixKind: "creative"`, don't propose a single diff. Instead present 2–3 plausible resolutions and ask the user to pick a direction (or write their own). Once direction is clear, re-frame as a mechanical fix and loop back to the per-finding flow.

For `fixKind: "manual-only"`, log the finding and move on — don't block the loop on user input.

### 5. Apply slnx updates (if any)

If any `slnx-mismatch` findings were approved for fixing, at the end of the loop **invoke `/update-slnx`** rather than hand-editing the slnx. The slnx is owned by that skill's canonical procedure.

### 6. Report

End with a short summary:
- **Applied:** N findings (by category)
- **Skipped:** M findings (by category) — list IDs so the user can re-audit and revisit
- **Manual-only:** K findings — list IDs
- **Still open:** any finding that surfaced during the loop but wasn't resolved.

Don't commit. Per `.claude/docs/agent-rules.md` → *Git commit rules*, commits need an explicit user request. The audit leaves the working tree staged-or-unstaged for the user to review and commit on their terms.

## Don'ts

- Do not run the audit spontaneously or "as a bonus" during unrelated work.
- Do not hand-edit `linq2db.slnx` directly for slnx-mismatch findings. Always route through `/update-slnx`.
- Do not auto-apply fixes without confirmation — even mechanical ones. The `batch-mechanical` option exists for the user to opt in explicitly.
- Do not flag style / grammar / formatting nits that don't affect rendering or semantic meaning. This skill is scoped to drift and decay, not polish.
- Do not edit agent-authored content or user-authored feedback records inside any memory file (the user-level `auto-memory` directory is out of scope even if it shows up in a `Grep`). That system is personal, not project-tracked.
- Do not commit. Changes stay in the working tree until the user asks.
