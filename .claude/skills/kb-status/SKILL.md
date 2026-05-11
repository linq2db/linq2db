---
name: kb-status
description: Read-only status report for the knowledge base — phase progress, source freshness (per-source cursor age), counts (areas, decisions, detected-issues by severity, GH index sizes), staleness summary (files with last_verified older than 90 days), and the last few entries from audit-log.md.
---

# /kb-status

Prints a compact dashboard of the KB's current state. Read-only — never modifies anything.

## When to run

- Any time the user wants to know whether `/kb-refresh` is needed.
- Before starting fresh work on a feature/bug — to confirm the KB's most recent verified SHA.
- After `/kb-build` finishes — to confirm all steps are `done`.

## Steps

### 1. Pull state summary

```bash
pwsh -NoProfile -File .claude/scripts/kb-state.ps1 <<'EOF'
{"op": "summary"}
EOF
```

Returns `{progress, cursors, audit_tail}`.

### 2. Compute counts from disk

In parallel `Bash` calls (or `Glob` + `Read`):

- `architecture/`: count `.md` files.
- `conventions/`: count.
- `history/by-year/`: list filenames; the count = year span.
- `history/decisions/`: count.
- `areas/`: list directories; per area, check whether all four sub-files exist (`INDEX.md`, `issues.md`, `decisions.md`, `tech-debt.md`, `patterns.md`).
- `github/issues-index.json`, `prs-index.json`, `discussions-index.json`: read; report length.
- `github/milestones.json`: read; report `{open: N, closed: N}`.
- `github/wiki/`: count `.md` files.
- `detected-issues/index.json`: read; count by severity (`high`, `med`, `low`) and by status (`open`, `triaged`, `accepted`, etc.).

### 3. Compute staleness

For every `.md` file under `architecture/`, `conventions/`, `history/decisions/`, `areas/<area>/`:

- Read frontmatter `last_verified`.
- If older than today by > 90 days: count as `stale`.
- Bucket by area for the report.

`Grep` shortcut: `Grep` for `last_verified:` across the KB, parse the date, compare. The skill can shell out to `pwsh` if needed but this is a fast in-process check.

### 4. Print the dashboard

Compose a markdown report and print it directly. Sections, in order:

```
# KB status

## Build progress
| Step | Status | Started | Finished |
|---|---|---|---|
| 0 — bootstrap | done | ... | ... |
| ... |
| 7 — github-indexes | partial | ... |  |
| ... |

## Source freshness
| Source       | Cursor                | Age      |
|--------------|----------------------|----------|
| code         | <sha (short)>         | 3 days   |
| commits      | <sha>                 | 3 days   |
| issues       | 2026-04-22T...        | 5 days   |
| prs          | 2026-04-23T...        | 4 days   |
| discussions  | 2026-04-20T...        | 7 days   |
| wiki         | <sha>                 | 12 days  |

## Counts
- Architecture docs: 8
- Conventions: 5
- History years: 12 (2014–2026)
- History decisions: 23
- Areas with full sub-files: 18 / 22
- GH indexes: issues 4823, prs 1247, discussions 89
- Wiki articles: 34
- Detected issues: open 142 (high 18, med 81, low 43); triaged 12; accepted 7; fixed 31

## Staleness
- KB files with last_verified > 90 days: 14
  - areas/PROV-ORACLE/: 3
  - areas/PROV-FIREBIRD/: 5
  - history/decisions/: 6

## Recent activity
<last 10 lines from state/audit-log.md>

## Suggestions
- 14 files are >90 days old — consider /kb-refresh.
- step 7 (github-indexes) is partial: <gate failures>. Re-run /kb-build to resume.
- 4 areas missing one or more sub-files: PROV-ACCESS, PROV-INFORMIX, ASPNET, TOOLS.
```

The "Suggestions" section is omitted if everything is healthy.

### 5. Stop

The skill prints the dashboard and exits. No follow-up actions, no questions.

## Do not

- Modify any file. Read-only.
- Spawn agents. The skill is a thin reporter; everything is in `state/` or computed from disk.
- Decide whether to refresh on the user's behalf — only suggest.
- Print the *full* `audit-log.md`; the tail of 10–20 lines is enough.
