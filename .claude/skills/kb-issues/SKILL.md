---
name: kb-issues
description: Query and act on the detected-issues store under .claude/knowledge-base/detected-issues/. Filter via the shared selection grammar (random N, by area, severity, category, source, file, status). Per-result actions include showing detail, creating a GitHub issue (delegates to /create-issue), driving /fix-issue, marking wontfix / duplicate / dismissed / triaged.
---

# /kb-issues

User-triggered query + triage UI for the KB's detected-issues store.

## Shared reference material

- [`../../docs/kb-issue-categories.md`](../../docs/kb-issue-categories.md) — schema, taxonomy, status lifecycle, pattern catalog
- [`../../docs/kb-selection-grammar.md`](../../docs/kb-selection-grammar.md) — selection grammar + action menu (this is the UX spec — do not duplicate here)
- [`../../docs/kb-architecture.md`](../../docs/kb-architecture.md) — KB layout
- [`../create-issue/SKILL.md`](../create-issue/SKILL.md) — delegated to for "create GH issue" action
- [`../fix-issue/SKILL.md`](../fix-issue/SKILL.md) — delegated to for "drive fix" action

## When to run

Only when the user explicitly invokes `/kb-issues`. Typical prompts:
- `/kb-issues` — interactive flow; first prompts for filter.
- `/kb-issues all severity:high` — direct filter.
- `/kb-issues random 10 status:open` — random sample.
- `/kb-issues DI-0042` — direct lookup of one item.
- `/kb-issues area:SQL-PROVIDER status:open` — multi-facet filter.

## Pre-conditions

- `.claude/knowledge-base/detected-issues/index.json` exists (`/kb-build` step 10 has run at least once). If not, print "No detected issues yet — run /kb-build (step 10) or /kb-refresh first" and exit.

## Steps

### 1. Parse selection input

If invoked with no args, prompt:

```
Filter? (e.g. "all severity:high", "random 10 status:open", "DI-0042", or just "all"):
```

Parse the input per [`../../docs/kb-selection-grammar.md`](../../docs/kb-selection-grammar.md). Reject unknown facets with a one-line error and re-prompt.

Special quick-form: a bare `all` filters to `status: open` by default (most common case). User can override with `all status:all`.

### 2. Load and filter the index

```bash
# Read directly via Read tool
```

Read `.claude/knowledge-base/detected-issues/index.json`.

Apply the filter:
- IDs / ranges → exact match on `id`.
- Facets → AND across the dict.
- `random N` → after filtering, sample N entries.

### 3. Print the result list

Numbered, compact table:

```
| #  | ID       | Sev   | Cat              | Area            | Status | Title                                                        |
|----|----------|-------|------------------|-----------------|--------|--------------------------------------------------------------|
| 1  | DI-0042  | med   | legacy-pattern   | SQL-PROVIDER    | open   | BasicSqlBuilder uses hardcoded provider check                |
| 2  | DI-0099  | high  | security-smell   | SQL-PROVIDER    | open   | String concat into SQL outside *SqlBuilder.cs                 |
| ...|
```

Append totals:

```
Showing N of M total detected issues. Filter: <filter description>.
```

### 4. Selection prompt

```
Select items (e.g. "1,3,5" / "1-5" / "all" / a specific ID), or 'q' to quit:
```

Resolve the selection per the grammar to a list of IDs.

### 5. Action prompt

For the selected items, present the action menu (per [`../../docs/kb-selection-grammar.md`](../../docs/kb-selection-grammar.md)):

```
Action: [d]etail / [g]h-issue / [f]ix / [w]ontfix / [u]plicate / [x] dismiss / [r]e-triage / [s]kip / [q]uit
```

If > 5 items selected for a write action, ask `Apply <action> to <N> items? [y/N]`.

### 6. Execute action

#### `d` — Detail

For each selected ID, `Read` `.claude/knowledge-base/detected-issues/items/<id>.md` and print the content. After printing, return to the action prompt for the same selection.

#### `g` — Create GH issue

For each selected item:

1. Skip if `gh_issue` already populated; surface `Already linked to gh#<N>`.
2. Read `items/<id>.md` to get the body. Construct a `/create-issue` invocation prompt:
   - **Title**: the issue's `title` field, prefixed with the area code: `[<area>] <title>`.
   - **Body**: the item's `## Pattern matched`, `## Why it matters`, `## Suggested fix`, `## Excerpt` sections, plus a footer `_Generated from KB detected-issue [DI-NNNN](.claude/knowledge-base/detected-issues/items/DI-NNNN.md)_`.
3. Delegate to `/create-issue` (the user-facing skill). Wait for completion — `/create-issue` will return the new issue number.
4. After success, update the KB:
   ```bash
   pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
   {"op":"apply-fences","agentOutput":"=== KB-INDEXER OUTPUT v1 ===\n=== INDEX-PATCH: detected-issues/index.json ===\n{\"op\":\"update\",\"id\":\"<DI-id>\",\"patch\":{\"status\":\"accepted\",\"gh_issue\":<N>}}\n=== END INDEX-PATCH ===\n=== END KB-INDEXER OUTPUT ===","currentSha":"<sha>"}
   EOF
   ```
5. Also `Edit` the item's MD file: change the `**Status**: open` line to `**Status**: accepted (#<N>)`.

#### `f` — Drive /fix-issue

Only valid when exactly one item is selected and it has `gh_issue` populated. If multiple selected, ask the user to narrow.

Delegate to `/fix-issue <gh_issue>`. The KB itself doesn't track fix progress — the GH issue is the source of truth from this point.

#### `w` — Mark wontfix

For each selected item, prompt:

```
Reason for wontfix on DI-NNNN (one line, required):
```

Reject empty reason. Then:

1. Update index: `update` patch setting `status: "wontfix"`.
2. `Edit` the item's MD: change `**Status**: open` → `**Status**: wontfix`. Append a `## Resolution` section with the reason.

#### `u` — Mark duplicate

For each selected item, prompt:

```
Canonical (DI-NNNN or gh#NNNN) for DI-NNNN:
```

Validate: if `DI-NNNN`, look up in index — must exist. If `gh#NNNN`, run `gh issue view <n> --repo linq2db/linq2db --json number,title,state` to confirm it's a real issue.

Then:
1. Update index: `status: "duplicate-of:<canonical>"`.
2. `Edit` MD: status line + append `## Resolution` with `Duplicate of <canonical>`.

#### `x` — Dismiss

Same as wontfix but `status: "dismissed"`.

#### `r` — Re-triage

For each selected item with `status: "open"`, set `status: "triaged"`. No reason needed. Useful for batch "I've seen these" sweeps.

#### `s` — Skip

Return to the result list (step 4) without acting.

#### `q` — Quit

Exit the skill.

### 7. Loop or stop

After an action completes, return to the action prompt for the same selection. The user can run multiple actions on the same set, or `s` to re-select.

## Bulk action confirmation

For any write action affecting > 5 items, the confirmation prompt is mandatory. The skill prints a one-line summary per item before asking, so the user knows exactly what will change.

## Audit log

Every status change made via this skill appends to `state/audit-log.md`:

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "append-audit", "event": "kb-issues triage", "lines": [
  "DI-0042 → wontfix (reason: ...)",
  "DI-0099 → accepted (#5512)"
]}
EOF
```

## Do not

- Auto-create GH issues without user confirmation. The action menu is explicit; never fast-path.
- Edit anything outside `detected-issues/` and `state/audit-log.md`.
- Re-detect issues. Detection is the indexer's job — `/kb-issues` only triages.
- Print issue MD bodies for every item in a long list. Use `d` for detail; for browsing, the table is enough.
- Modify `index.json` by hand-editing JSON — always go through `kb-state.ps1 apply-fences` to keep validation centralized.
