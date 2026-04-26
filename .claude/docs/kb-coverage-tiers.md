# Knowledge base coverage tiers

Used by every KB indexer agent to decide which files must be visited, which can be sampled, and which to skip. Gate thresholds in [`kb-build-steps.md`](kb-build-steps.md) reference the tier counts here.

## Tier 1 — must visit (100%)

Mandatory. A step is `partial` (not `done`) if any Tier-1 file in its scope was not visited.

- Public API surface: every file under `Source/LinqToDB/` whose top-level types are `public` and not nested in `LinqToDB.Internal.*`.
- Abstract base types: `ISqlBuilder`, `IDataProvider`, `BasicSqlBuilder`, `BasicSqlOptimizer`, `ExpressionBuilder`, every `*Visitor` base class.
- Build / framework anchors: `Directory.Build.props`, `Source/LinqToDB/LinqToDB.csproj`, `linq2db.slnx`, `global.json`.
- Per-provider entry points: `Source/LinqToDB/DataProvider/<provider>/<Provider>DataProvider.cs`, `<Provider>SqlBuilder.cs`, `<Provider>SqlOptimizer.cs`.
- Translator roots: every `Source/LinqToDB/Linq/Builder/*Builder.cs` whose name does not end in `Helpers.cs` or `Methods.cs`.
- Test infrastructure: `Tests/Base/**/*.cs`, `Tests/Linq/Tests.csproj`, `Tests/Tests.Playground/Tests.Playground.csproj`.
- Documentation anchors: `CLAUDE.md`, `README.md`, `RELEASE.md`.

Per-area Tier-1 file lists are pinned in [`kb-areas.md`](kb-areas.md). Indexers cross-check the on-disk file list against the pinned list and report any drift (file removed / renamed) as a gate failure.

## Tier 2 — sample (≥90%)

Implementation files, per-provider helpers, test fixtures. Gate threshold: at least 90% of Tier-2 files in scope must be visited; every skip must be paired with a one-line reason in the coverage block.

Patterns:

- `Source/LinqToDB/**/*.cs` not classified as Tier 1 or Tier 3.
- `Tests/Linq/Linq/**/*.cs`, `Tests/Linq/UserTests/**/*.cs`, `Tests/Linq/Update/**/*.cs`, etc. — fixture bodies.
- `Source/LinqToDB.AspNet/**`, `Source/LinqToDB.Tools/**`, `Source/LinqToDB.EntityFrameworkCore/**` — companion product surfaces (areas defined in `kb-areas.md`).

Acceptable skip reasons (recorded in coverage block):

- `near-duplicate of <other-file>` — generated overload, partial-class continuation, or per-TFM clone with trivial differences.
- `trivial overload` — pure dispatcher with one-line bodies.
- `deprecated` — marked `[Obsolete]` and slated for removal.
- `out-of-scope for this step` — file belongs to a different area's scope.

Anything else must be visited.

## Tier 3 — skip and log

Counted but never visited. The coverage block records the count only.

- Generated code: `*.g.cs`, `*.Designer.cs`, `*.Generated.cs`, anything under a folder ending `/obj/` or `/bin/`.
- Build outputs and caches: `bin/`, `obj/`, `.vs/`, `.idea/`, `TestResults/`, `node_modules/`.
- Test data files: `Tests/Linq/UserDataProviders.json`, `Tests/Linq/CreateData/**` SQL scripts, baseline `.sql` / `.txt` files under `Tests/**/Baselines/**`.
- Repo plumbing: `.git/`, `.github/` workflows (covered separately as Tier 2 only when an area's scope includes CI), `.build/`.

## Tier classification rules

For a step's scope, an indexer:

1. Resolves the scope to a path-pattern set from [`kb-areas.md`](kb-areas.md) (or a global step's pinned list).
2. Glob-enumerates all files matching the patterns.
3. Classifies each file: explicit Tier-1 list match → Tier 1; explicit Tier-3 pattern match → Tier 3; everything else → Tier 2.
4. Records the totals up front, visits Tier 1 + Tier 2 per the gate, emits the coverage block at the end.

If a file's classification is ambiguous (e.g. a borderline helper that arguably should be Tier 1), the indexer prefers Tier 2 and notes the call in its emitted artifact body — the human reviewer can promote it in `kb-areas.md` later.

## Gate thresholds

| Step kind | Tier-1 gate | Tier-2 gate |
|---|---|---|
| Architecture (overview, per-area INDEX) | 100% | ≥90% |
| Conventions | 100% on the canonical pattern catalog | n/a (curated) |
| History (commits / decisions) | 100% on the year/decision range | n/a |
| GitHub indexes | 100% on cursor consumption (no gap) | n/a |
| Wiki mirror | 100% on listed articles | n/a |
| Detected-issues sweep | 100% on Tier-1 + Tier-2 file list | (sweep itself defines tier — gate is "every Tier-2 file scanned") |
| Area roll-up | 100% on the area's required sub-files (`issues.md`, `decisions.md`, `tech-debt.md`, `patterns.md`) | n/a |

Gates are checked by `kb-state.ps1 apply-fences` and the orchestrating skill (`/kb-build`). Any failure flips the step's status to `partial` and appends a line to `state/audit-log.md`.
