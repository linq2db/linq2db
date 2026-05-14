# Detected-issues taxonomy

Schema, pattern catalog, and lifecycle for items in `detected-issues/index.json` + `detected-issues/items/<id>.md`. Used by `kb-issue-detector` (writer) and `/kb-issues` (consumer).

## Index entry shape

```json
{
  "id": "DI-0042",
  "severity": "med",
  "category": "legacy-pattern",
  "source": "code",
  "area": "SQL-PROVIDER",
  "title": "BasicSqlBuilder uses hardcoded provider check instead of capability flag",
  "files": ["Source/LinqToDB/SqlProvider/BasicSqlBuilder.cs:1842"],
  "status": "open",
  "gh_issue": null,
  "first_detected_sha": "abc1234",
  "last_seen_sha": "def5678",
  "first_detected_at": "2026-04-25",
  "last_seen_at": "2026-04-25",
  "confidence": "high"
}
```

ID format: `DI-NNNN` zero-padded to 4 digits, monotonic, never reused. The detector reads the highest existing ID from `index.json` on every run and increments from there.

## Severity

| Severity | Meaning | Examples |
|---|---|---|
| `high` | Correctness or security risk; could produce wrong SQL, leak data, or crash on a supported provider. | Missing null-check before deref, SQL-injection-shaped string concat, race condition in cache. |
| `med` | Maintainability hazard; not currently broken but likely to break during a future refactor or provider extension. | Hardcoded provider check, type-name string match, copy-paste between providers, dead code on a hot path. |
| `low` | Cosmetic or minor inefficiency; safe to ignore. | XML-doc typo, suboptimal LINQ over a 10-element list, missing `[Obsolete]` attribute on a deprecated overload. |

## Category

| Category | Definition |
|---|---|
| `tech-debt` | Working code with structural pressure — one more provider would force a refactor; one more feature would force a breaking change. |
| `dead-code` | Unreachable or unused code: types/methods with no callers (excluding public API), `[Obsolete]` items past their removal milestone. |
| `legacy-pattern` | Code matching a pattern the codebase has migrated away from. The new pattern is documented in `conventions/legacy-patterns.md`. |
| `code-smell` | Localized issue: long method, deep nesting, magic number without comment, swallowed exception. |
| `doc-gap` | Public API missing XML doc, broken `<see>` reference, undocumented invariant. |
| `broken-test` | Test that's `[Ignore]`d, has a `// TODO unskip` comment, or is in a known-flaky list. |
| `todo-fixme` | `// TODO`, `// FIXME`, `// HACK`, `// XXX` comment in source. The detector extracts the comment + 5 surrounding lines. |
| `naming-inconsistency` | Public API name that violates the convention in `conventions/naming.md` (e.g. abbreviation expansion drift). |
| `perf-smell` | Linear scan where a dictionary lookup exists, allocation in a hot path, async-over-sync. |
| `security-smell` | Concrete-but-not-exploited security issue: unparameterized SQL fragment, hash on user-controlled input without length cap, etc. Use `high` severity if exploitable. |

## Source

Where the issue evidence came from:

| Source | Meaning |
|---|---|
| `code` | Detected by scanning code only. |
| `git` | Detected by analyzing commit history (e.g. churn-flag: a method modified > N times by > N authors over the last year). |
| `gh` | Surfaced from a GitHub issue/PR/discussion (e.g. "user reported repeatedly, not fixed"). |
| `cross` | Combination — e.g. code matches a legacy pattern *and* has an open GitHub issue, *and* git churn shows it's been patched 6 times. |

## Status lifecycle

```
        ┌──────────┐
        │   open   │  ← initial state on detection
        └────┬─────┘
             │ user runs /kb-issues, reviews
             ▼
        ┌──────────┐
        │ triaged  │  ← user has seen it; no action yet
        └────┬─────┘
             │
   ┌─────────┼─────────┬──────────┬──────────┐
   ▼         ▼         ▼          ▼          ▼
accepted  wontfix  duplicate-of dismissed  fixed
   │         │         │          │          │
   │ (gh_issue                              (next refresh
   │  populated)                              confirms gone)
   ▼
/fix-issue?
```

- `open` — initial state on detection.
- `triaged` — user has reviewed via `/kb-issues` but taken no terminal action.
- `accepted` — user picked "create GH issue" from `/kb-issues` action menu; `gh_issue` field is populated with the new issue number. From here `/fix-issue` is the natural next step.
- `wontfix` — user explicitly declined to act on it. The MD body must record the reason.
- `duplicate-of: DI-NNNN` or `duplicate-of: gh#NNNN` — user marked as a duplicate of another detected-issue or a GitHub issue.
- `fixed` — set automatically by the next `/kb-refresh` when the code path no longer matches the original detection pattern. The detector verifies via `last_seen_sha`: if a refresh sweeps the area and the issue's signature is gone, status flips to `fixed` and `last_seen_at` is frozen.
- `dismissed` — user marked false-positive. The MD body records why (e.g. "the hardcoded check is intentional — we don't have a capability flag for this and adding one is out of scope").

`fixed` issues are not deleted from the index — they remain as a record of remediated debt.

## Pattern catalog (for `kb-issue-detector`)

The detector runs each pattern below against Tier-1 + Tier-2 files in scope. Add new patterns here, not in the agent file. Each entry: regex / structural pattern, category, default severity.

### Code patterns

| Pattern | Category | Default severity |
|---|---|---|
| `// TODO`, `// FIXME`, `// HACK`, `// XXX` (case-insensitive, with at least one space after `//`) | `todo-fixme` | `low` |
| Hardcoded provider check: `if \(.*ProviderName ==` or `provider\.Name == "<lit>"` outside `*DataProvider.cs` | `legacy-pattern` | `med` |
| Type-name string match: `\.GetType\(\)\.Name == "<lit>"` or `typeof\(\w+\)\.Name == "<lit>"` | `code-smell` | `low` |
| `[Obsolete]` without `(error: true)` past the milestone in `Directory.Build.props.<Version>` (heuristic: `Obsolete\(".*v(\d+)"` where version < current) | `dead-code` | `med` |
| Public API method without `<summary>` XML doc (when file is Tier 1 of any area) | `doc-gap` | `low` |
| Catch block with empty body or only `// ignore` comment | `code-smell` | `med` |
| `[Ignore]` / `[Explicit]` on a test method without a linked issue (`#NNNN` in the attribute or a nearby comment) | `broken-test` | `low` |
| String concatenation into SQL: `\+ "(SELECT|INSERT|UPDATE|DELETE)" \+` outside `*SqlBuilder.cs` | `security-smell` | `high` |
| `async Task .*\.Wait\(\)` or `\.GetAwaiter\(\)\.GetResult\(\)` in non-test code | `perf-smell` | `med` |

### Cross-source patterns (need git + gh data)

| Pattern | Category | Default severity |
|---|---|---|
| File modified in > 8 commits over last 365 days by ≥ 3 distinct authors | `tech-debt` | `med` |
| Open GH issue labeled `bug` referencing a Tier-1 file with no PR linked | `tech-debt` | `med` |

`kb-issue-detector` reads this catalog at the top of its run; new patterns require no agent edit. The catalog can grow; existing detected-issues are not retroactively re-scored.

## Filtering / querying

`/kb-issues` accepts the [selection grammar](kb-selection-grammar.md). Common queries:

- `all severity:high` — every high-severity item, any status.
- `all status:open category:legacy-pattern` — un-triaged legacy patterns.
- `all area:PROV-ORACLE source:code` — Oracle-area code-only issues.
- `DI-0042` — exact item by ID.
- `random 10 status:open` — 10 random open items (good for sampling debt).

Action menu per result is defined in [`kb-selection-grammar.md`](kb-selection-grammar.md).
