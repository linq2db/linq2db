---
name: chores
description: One-stop dispatcher for `.claude/` maintenance work on linq2db. Surveys staleness signals across the project (`.claude/` audit lag, slnx sync, Knowledge Base cursors, KB issue queue, API baselines drift, permission-allowlist lag) and renders a single table the user can pick from instead of remembering which periodic skill to run when. Each row hands off to the actual maintenance skill that performs the work — `chores` itself runs no maintenance, just routes. Use when the user says "chores", "/chores", "what needs maintenance", or "is anything overdue".
---

# /chores

## What this skill is (and isn't)

**Is:** a maintenance dashboard. It computes staleness signals for every periodic chore the project has, renders one table sorted by overdue-ness, and dispatches to the picked skill(s). Think of it as the project's "weekly maintenance" checklist.

**Isn't:**

- Not the actual worker — every chore handoff goes to a dedicated skill (`/audit-claude`, `/kb-refresh`, `/update-slnx`, etc.).
- Not for the work skills (`/review-pr`, `/verify-review`, `/fix-issue`, `/create-issue`, `/find-issues`, `/merge-duplicates`, `/test`, `/test-providers`, `/kb-ask`, `/kb-build`, `/kb-status`). Those are user-driven, not periodic.
- Not for `/session-reflect` — that one needs the *current conversation*, can't be batched.

## When to run

User-invoked. Good moments:

- Start of week / sprint planning.
- After a sizable batch of merges to `master` (multiple chores tend to come due together).
- When the user feels "things are out of sync but I forget which skill to run".

The skill is **stateless** — it does not record "last run" anywhere; the per-chore skills own their own freshness signals.

## The chore catalogue

Each row is one maintenance task the skill knows about. The **Signal** column says how the skill computes "is this overdue?". Rows are added/removed only when a corresponding skill is added/removed.

| Chore | Skill | Signal of staleness |
|-------|-------|---------------------|
| Audit `.claude/` for drift | `audit-claude` | count of commits to `.claude/**` since the last commit whose subject contains `audit` (case-insensitive substring). ≥ 20 → 🔴, ≥ 10 → 🟡 |
| Sync `.claude/` ↔ `linq2db.slnx` | `update-slnx` | diff between on-disk `.claude/**` files (excluding `.claude/knowledge-base/**`, which is intentionally not in the slnx) and `<File Path="...">` entries under `.claude/` in `linq2db.slnx`. ≥ 1 add/remove → 🔴 |
| Refresh Knowledge Base | `kb-refresh` | `.claude/knowledge-base/state/cursors.json`: take the oldest `updated_at` / `verified_at` across active sources (`code`, `issues`, `prs`, `discussions`, `wiki`, `commits`). ≥ 14 days → 🔴, ≥ 7 days → 🟡. If `code.sha` is `null`, KB is unbuilt → `?` with note "run `/kb-build` first" |
| KB issue queue triage | `kb-issues` (filtered: open severity high+med) | count of files under `.claude/knowledge-base/detected-issues/**` whose YAML frontmatter has `severity: high` or `severity: med` and `status: open`. ≥ 5 → 🔴, ≥ 1 → 🟡. If directory doesn't exist (KB unbuilt) → `?` |
| Refresh API baselines | `api-baselines` | count of commits to `Source/**/*.cs` since the last commit touching any `Source/**/CompatibilitySuppressions.xml`. ≥ 50 → 🔴, ≥ 20 → 🟡 |
| Tighten permission allowlist | `fewer-permission-prompts` | mtime of `.claude/settings.json` (and `settings.local.json`) vs. count of session transcripts in `~/.claude/projects/<this-project>/*.jsonl` since that mtime. ≥ 100 sessions → 🔴, ≥ 30 → 🟡. If transcript path can't be located, `?` |
| Bump versions for next release _(opt-in)_ | `version-bump` | never auto-flagged. Surfaced with a `—` in the **Overdue?** column. The user picks it explicitly when prepping a release |

If a signal can't be computed (KB not present, slnx unreadable, transcript path unknown), the row's **Overdue?** column shows `?` and **Note** explains. The chore is still pickable, but the skill doesn't push it.

## Procedure

Follow in order.

### 1. Compute signals

For each row in the catalogue:

- Run the cheap probe (`git log -1`, file mtime, `wc -l` on a JSON list, etc.).
- Convert to a **status**: `🔴 overdue` / `🟡 watch` / `🟢 fresh` / `?` (unknown) / `—` (opt-in).
- Compute a **Last** column showing the most-recent action timestamp (ISO date) or `—`.

The probes are read-only and should complete in seconds. If any probe takes more than ~5s, mark the status `?` and move on — chores is a survey, not a deep audit.

### 2. Render the table

```markdown
# Chores — <iso>

| # | Chore                              | Skill                       | Last         | Overdue?  |
|---|------------------------------------|-----------------------------|--------------|-----------|
| 1 | Audit `.claude/` for drift         | `audit-claude`              | 2026-04-21   | 🔴 overdue |
| 2 | Sync `.claude/` ↔ `linq2db.slnx`   | `update-slnx`               | 2026-05-01   | 🟢 fresh  |
| 3 | Refresh Knowledge Base             | `kb-refresh`                | 2026-04-30   | 🟢 fresh  |
| 4 | KB issue queue triage              | `kb-issues` (high+med)      | n/a (12 open)| 🔴 overdue |
| 5 | Refresh API baselines              | `api-baselines`             | 2026-04-12   | 🟡 watch  |
| 6 | Tighten permission allowlist       | `fewer-permission-prompts`  | —            | ?         |
| 7 | Bump versions for next release     | `version-bump`              | —            | —         |

## Notes
- <one short line per row whose signal needs explanation, e.g. "row 6: transcript path unreadable" or "row 4: KB not built — run /kb-build first">
```

Sort rows by status (overdue first, then watch, then fresh, then `?`, opt-in last). Keep the catalogue order within each status group.

### 3. Offer to dispatch

Ask once:

> _"Pick chores to run by number (e.g. `1, 3, 4`), `all overdue`, `all watch+overdue`, or `all`. Each chore hands off to its own skill, which will ask for its own confirmations and edits — `chores` does no work itself."_

Selection grammar follows [`kb-selection-grammar.md`](../../docs/kb-selection-grammar.md). Group shorthands supported here:

- `all overdue` — every row in 🔴.
- `all watch+overdue` — 🔴 ∪ 🟡.
- `all` — every pickable row (excludes `?` and `—` rows unless explicitly named).

### 4. Hand off

For each picked row, in the order the user listed them (not catalogue order):

1. Tell the user which skill is starting (`# Starting: <skill>`).
2. Invoke the skill via `Skill('<skill-name>')` with the right arg shape (e.g. `Skill('kb-issues', 'all severity:high status:open')` for the filtered KB issue triage).
3. **Wait for that skill to complete its own loop** (proposal → user-approval → application → handoff). `chores` does not preempt or shortcut another skill's interactive flow.
4. After the skill returns, add a one-line outcome to the running summary: `✓ <skill> — <count> changes / no changes / aborted by user`.

If the user picks 3+ chores, ask once before starting whether they want them **sequential** (default — each finishes before the next starts) or **paused-between** (after each, surface a "continue?" gate so the user can stop the chain). Don't run chores in parallel — each one expects the user's full attention for its own approvals.

### 5. Final summary

After the last chore in the picked set returns:

```
# Chores — done

- ✓ <skill> — <one-line outcome>
- ✓ <skill> — <one-line outcome>
- ✗ <skill> — aborted at <step>

## Suggested follow-ups
- <one-line: e.g. "review `git diff --cached` and commit before next chore" if any chore staged changes>
```

Do not auto-commit anything. Each underlying skill stops at "staged + ready to review" by design — `chores` preserves that contract. Per `agent-rules.md` → *Git commit rules*, commits need an explicit user request.

## Scope and discipline

- **No work happens inside `chores`.** Every effect comes from a downstream skill. If a chore's skill is missing or unrunnable, surface that as `?` and move on; do not re-implement the chore inline.
- **No new chore types without an owning skill.** A maintenance task without a dedicated skill belongs in the user's head or in `CLAUDE.md`, not in this catalogue. If you find yourself wanting to add "run the linter" as a chore but there's no skill for it — propose the skill first via `/session-reflect` or `/audit-claude`, then add the row here.
- **Probes must be cheap.** Anything over ~5s gets `?`. The user's mental model of `chores` is "1-second snapshot of what's overdue", not "10-minute multi-build inspection".
- **Never auto-pick `version-bump`.** It changes shipped version numbers and creates a release-prep branch. The row exists so the user can see it on the menu when they're already thinking about maintenance, not so the skill nudges them into it.
- **Don't double-count freshness.** Each chore owns its own signal — `chores` reads it, doesn't override it. If `kb-refresh` thinks the KB is fresh but the user disagrees, the fix is to improve `kb-refresh`'s freshness logic, not to layer a second timestamp here.

## Adding a new chore

When a new maintenance skill lands (e.g. a future `cleanup-temp-files`):

1. Add a row to the catalogue table.
2. Specify the staleness signal — must be a single read-only probe (file mtime, JSON field, `git log` count). No multi-step computation.
3. Decide the status thresholds (`🔴` / `🟡` / `🟢`) and document them in the row's signal column.
4. If the new chore is opt-in (long-running, side-effect-heavy, irreversible), document `—` in the **Overdue?** column.

Removing a chore: the corresponding skill must already be removed first.

## When the user picks a chore that's `🟢 fresh`

Run it anyway, but call out the freshness in the handoff message: `"# Starting: <skill> (note: signal was 🟢 fresh — run anyway?)"`. Wait for confirmation. The user may know something the signal doesn't.

## Don'ts

- Do not run any of the dispatched skills' work inline. The skill's contract is that the user gets to see and approve each step.
- Do not commit. Commits stay user-driven — each underlying skill respects this and `chores` doesn't override it.
- Do not invent chore rows for tasks without a skill. A signal without an owning worker is a future skill, not a present chore.
- Do not run probes that touch the network (e.g. `gh api`) as part of step 1 — chores must be cheap and offline-friendly. Network-dependent freshness checks belong inside the worker skill itself (e.g. `/kb-refresh` is the one that fetches GitHub deltas).
