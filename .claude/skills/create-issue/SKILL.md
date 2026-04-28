---
name: create-issue
description: Create a new issue on linq2db/linq2db covering Task/Bug/Feature work types. Verifies claims against the current codebase, drafts a structured body with file:line references, checks for duplicates, proposes issue type + up to 3 labels + milestone, and posts only after explicit user confirmation. Sets the GitHub issue type via REST PATCH after creation.
---

# create-issue

User-triggered workflow to file a GitHub issue on `linq2db/linq2db`.

Scope: all three GitHub issue types on this org — **Task** (specific piece of work), **Bug** (unexpected behavior), **Feature** (new capability / request). "Issue" here is GitHub's umbrella term, not a synonym for "bug".

## When to run

Only when the user explicitly asks to create / file / open an issue, task, bug, or feature on linq2db. Do not propose filing during unrelated work.

## Steps

### 1. Understand the topic and verify claims

1. Parse the user's description.
2. **Verify any claim about current linq2db behavior by reading the code.** If the user says "we don't do X", "Y is wrong", or "we emit Z" — grep/read the relevant files under `Source/` before drafting. Filing based on memory alone is not acceptable; provider dialects and SQL builders evolve and memories go stale.
3. **Verify any 3rd-party behavior claim against upstream docs.** If the premise rests on an external system — "Firebird 5 supports `IF NOT EXISTS`", "Oracle 23c adds X", "PostgreSQL 18 does Y", "the SQL standard requires Z" — fetch the evidence before drafting. Feature lists shift across minor versions, release-note summaries drop detail, and "added in v5" may have actually landed in v6. Sources, in priority order:
   - Official release notes / grammar / language reference for the exact version (WebFetch).
   - Upstream issue tracker (e.g. `FirebirdSQL/firebird`, `dotnet/runtime`).
   - Empirical reproduction (3–5 line script against a local container) — corroboration, not sole source.

   Include the proof links in the **Background** section. If verification fails, surface it to the user — a filed issue anchored on a false premise wastes downstream effort (FB5 → FB6 pivot of 2026-04-22 is the motivating case).
4. If the topic is a pure feature request with no behavior claim — internal or external — skip verification, but still ground the proposal in concrete file paths the change would touch.
5. Collect concrete `file:line` references for the **Current behavior** section.

### 2. Check for duplicates

Follow the search discipline in [`.claude/docs/issue-search.md`](../../docs/issue-search.md) in **topic mode** — extract distinctive terms, run parallel searches, rank, present. Instead of the full interactive flow `/find-issues` offers, stop after presenting the ranked candidate list and ask the user one of:

- **continue** — no duplicate, proceed to step 3
- **reference #N** — existing issue is related, reference it in the new body's "Notes" section but still file
- **abort** — existing issue already covers this, don't file

Alternatively, the user can run `/find-issues <topic>` first for the full interactive lookup and come back with an answer.

### 3. Classify issue type

Pick exactly one. Ask the user if ambiguous:

| Type | When |
|---|---|
| **Bug** | Incorrect output, crash, regression, broken behavior vs. documented/expected |
| **Feature** | New capability, new provider support, new public API surface, adopting a newer dialect feature visibly |
| **Task** | Internal work with no external behavior change: refactor, test gap, infrastructure, tracking work, internal DDL-emitter tweaks, doc updates |

Reference examples:
- "Firebird 5 doesn't use native `DROP TABLE IF EXISTS`" → **Task** (internal DDL-emitter change, no public API moves). Lean `Feature` only if the change exposes something new to the user.
- "`OrderBy` generates wrong SQL on SQLite" → **Bug**.
- "Add DuckDB provider" → **Feature**.
- "Split `DmlServiceBase` into per-provider files" → **Task**.

### 4. Draft title + body

Title: imperative and specific, under ~90 characters. Prefix the provider/area when helpful (`Firebird 5: …`, `Oracle 23c: …`, `EFCore: …`).

Body — use this exact structure:

```markdown
### Summary

<1–2 sentence statement of what's wrong or missing.>

### Background

<Optional. External facts a reviewer needs — standards, version history, related
features in the dialect. Skip if the summary already covers it.>

### Current behavior

<What linq2db does today, with `file:line` references. Include a short SQL/code
snippet when it clarifies.>

- `Source/LinqToDB/...:<line>` — <what happens here>

### Expected behavior

<What should happen, with an example of the desired SQL/API shape.>

### Notes

<Optional. Scope caveats, edge cases, related work, linked issues/PRs. Use this
to prevent scope creep, not to expand scope.>
```

Keep the body focused on what the user reported. Don't invent extra scope. Related observations you noticed while verifying go in **Notes** or — better — get flagged to the user as "want a separate issue for this?" rather than folded into the filed one.

### 5. Propose labels (up to 3)

Read the current label list once per session:

```
gh label list --repo linq2db/linq2db --limit 300 --json name,description
```

Infer candidates from the content. Aim for one label from each applicable category below, maximum 3 total. Skip a category if nothing fits — fewer labels is better than stretching the match.

**Category A — Provider** (when the issue targets a specific DB). Prefix `provider:`:

`firebird`, `oracle`, `postgresql`, `mssql`, `sqlserver`, `sqlite`, `sqlce`, `mysql`, `db2`, `informix`, `sap-hana`, `clickhouse`, `access`, `sybase`

Note both `provider: mssql` and `provider: sqlserver` exist — prefer `mssql` unless the user specifies otherwise.

**Category B — Non-core project area** (when the issue targets a project under `Source/` other than `LinqToDB` itself):

| Project / area | Label |
|---|---|
| `Source/LinqToDB.EntityFrameworkCore*` | `area: efcore` |
| `Source/LinqToDB.CLI` (scaffold / linq2db.cli) | `area: scaffold` |
| `Source/LinqToDB.Templates` (T4) | `area: T4` |
| `Source/LinqToDB.LINQPad` | `area: linqpad` |
| `Source/LinqToDB.FSharp*` | `area: fsharp` |
| `Source/LinqToDB.Remote*` | `area: remote-context` |
| `Tests/` | `area:tests` *(note: no space after colon — existing label quirk)* |

**Category C — Code area within `Source/LinqToDB`**. Prefix `area:`:

`linq`, `sql`, `mapping`, `fluent-mapping`, `async`, `schema`, `DDL`, `data-context`, `performance`, `bulk-copy`, `compiled-query`, `configuration`, `extensions`, `inheritance`, `set`, `types`, `AOT`, `compatibility`, `hints`, `documentation`, `nuget`, `infrastructure`, `vb.net`

Matching hints:
- DDL / create table / drop table / alter → `area: DDL`
- LINQ query translation / expression tree / `IQueryable` → `area: linq`
- SQL generation / `SqlBuilder` / SQL AST → `area: sql`
- Mapping attributes / column/table mapping → `area: mapping`
- Fluent mapping builder → `area: fluent-mapping`
- Async methods / cancellation → `area: async`
- `ISchemaProvider` / schema reading → `area: schema`
- Performance / perf regression → `area: performance`
- Bulk copy → `area: bulk-copy`
- Compiled query → `area: compiled-query`
- UNION / INTERSECT / EXCEPT → `area: set`
- Trimming / Native AOT → `area: AOT`

DML semantics (Insert/Update/Delete/Merge) have no dedicated `area:` label — omit Category C in that case rather than mislabeling as `area: DDL`.

**Do not apply:**
- `type: *` labels — issue type covers that.
- `status: *`, `severity: *`, `epic: *`, `resolution: *` — maintainer-managed.

### 6. Propose milestone

Always propose one (numbered list). Fetch:

```
gh api repos/linq2db/linq2db/milestones?state=open --jq '.[] | {number, title}'
```

Present in the order defined in [`.claude/docs/agent-rules.md`](../../docs/agent-rules.md) → **Pull request rules** → **Milestone** (next-version first, then remaining versioned by version, then non-versioned alphabetical).

Allow the user to reply with a number, a title, or "none" to skip.

### 7. Present the full proposal and wait

Show:
- **Title**
- **Type** (Task / Bug / Feature)
- **Labels** (the 0–3 inferred)
- **Milestone** (or "none")
- **Body** (full)
- **Duplicate candidates** (if any found in step 2)

Wait for explicit user confirmation (`post`, `go`, `create`, or equivalent) before the next step. If the user adjusts any field, re-present and wait again.

### 8. Post

Only after explicit confirmation:

1. Ensure the scratch dir exists:
   ```
   mkdir -p .build/.claude
   ```
2. Write the body to a file named after a slug of the title, e.g. `.build/.claude/create-issue-firebird-5-drop-if-exists.md`.
3. Create the issue:
   ```
   gh issue create \
     --repo linq2db/linq2db \
     --title "<title>" \
     --body-file .build/.claude/create-issue-<slug>.md \
     [--label "<l1>" --label "<l2>" --label "<l3>"] \
     [--milestone "<title>"]
   ```
   Do **not** self-assign. Add `--assignee @me` only if the user asked to be assigned.
4. Capture the issue number from the returned URL (format `https://github.com/linq2db/linq2db/issues/<n>`).
5. Set the GitHub issue type — `gh issue create` has no `--type` flag, so use the REST PATCH endpoint:
   ```
   gh api -X PATCH repos/linq2db/linq2db/issues/<n> -f type=<Task|Bug|Feature>
   ```
   Confirmed working against #5483 (2026-04-22). If the PATCH ever fails on a future API change, fall back to a GraphQL `updateIssue` mutation with `issueTypeId`, and surface the failure to the user rather than silently skipping.
6. Return the issue URL.

### 9. Do not

- Post to any repo other than `linq2db/linq2db` unless the user explicitly overrides `--repo`.
- Apply labels outside the three-category rule (no `type:`, `status:`, `severity:`, `epic:`, `resolution:`).
- Assign anyone (including `@me`) unless the user asked.
- Invent scope beyond the user's topic. Related findings go in **Notes** or get flagged as a separate-issue candidate.
- Edit or close existing issues — this skill only creates.
