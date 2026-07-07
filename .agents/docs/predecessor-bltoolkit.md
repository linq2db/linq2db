# BLToolkit predecessor

linq2db originated as a fork of [BLToolkit](https://github.com/igor-tkachev/bltoolkit), authored by Igor Tkachev. The linq2db repo's initial commit `6f5bb039a3ba6452aa0983ebe32f71f6c897f618` (2011-12-12) imported BLToolkit wholesale; APIs that look ancient in linq2db's `git log` may have been authored years earlier in BLToolkit and only carried into linq2db at the import boundary.

## When to consult

- A user asks for the *original* author / date / motivation of an API and `git log -S '<symbol>'` in linq2db points at the 2011-12-12 initial commit (= boundary of imported history; the symbol existed before).
- Investigating long-standing semantics (`Sql.GetDate`, `Sql.CurrentTimestamp`, `Sql.CurrentTimestamp2`, the `[SqlFunction]` attribute system, the LINQ provider machinery, the `IDataProvider` shape) where pre-2011 design decisions still constrain the current code.

Don't consult for current behavior — BLToolkit hasn't tracked linq2db's evolution since the 2011 fork. The two codebases diverged immediately after the import.

## How to walk it

`gh api` works against `igor-tkachev/bltoolkit` the same as any GitHub repo. The repo is small enough that whole-file commit history fits in one call:

```
gh api "repos/igor-tkachev/bltoolkit/commits?path=Source/Data/Linq/Sql.cs&per_page=100" \
  --jq '.[] | "\(.sha[0:10])|\(.commit.author.name)|\(.commit.author.date[0:10])|\(.commit.message | split("\n")[0])"'
```

For each commit of interest, fetch the patch and grep the file diff for the symbol you're tracing — `+` lines mark introduction, context lines mark pre-existing content. The patch is `.files[].patch` in the commit response:

```
gh api repos/igor-tkachev/bltoolkit/commits/<sha> \
  --jq '.files[] | select(.filename | contains("Sql.cs")) | .patch' \
  | grep -nE '<symbol>' | head -20
```

To find which file holds a symbol when the path isn't obvious, use code search:

```
gh api "search/code?q=repo:igor-tkachev/bltoolkit+<symbol>+filename:<file>" \
  --jq '.items[] | {path, sha, url}'
```

## File-path mapping (LINQ era)

The 2017 reorganization in linq2db (`beb417cb` *"Reorganized projects and solution"*) renamed `Source/Data/Linq/...` → `Source/LinqToDB/...` and flipped the namespace from `BLToolkit.Data.Linq` to `LinqToDB`. Rough mapping for LINQ-related code:

| linq2db path                                            | BLToolkit path                |
|---|---|
| `Source/LinqToDB/Sql/Sql.cs` (originally `Source/LinqToDB/Sql.cs`) | `Source/Data/Linq/Sql.cs`     |
| `Source/LinqToDB/Linq/...`                              | `Source/Data/Linq/...`        |
| `Source/LinqToDB/SqlQuery/...` (now `Source/LinqToDB/Internal/SqlQuery/...`) | `Source/Data/Sql/...`         |
| `Source/LinqToDB/DataProvider/<Provider>/...`           | `Source/Data/DataProvider/...` |

Consult `git log` on the early linq2db commits (`beb417cb` 2017-10-08, `47c3636a` 2016-03-01 *"Updated compatibility classes"*, `ad7d5a42` 2016-09-23 *".Net Core support"*) for exact rename history when the rough map doesn't fit.

## What's findable vs what isn't

**Findable in BLToolkit:**
- API author / date / motivation pre-2011-12-12.
- Original SQL emission shape (the early `[SqlFunction("…", "…")]` attribute set, before linq2db's translator-based dispatch).
- Concept lineage — why an API exists, what problem it solved when it was added.

**Not findable in BLToolkit (by design):**
- Current per-provider translation behavior — that's in linq2db's `Source/LinqToDB/Internal/DataProvider/<Provider>/Translation/`. Always verify behavior claims against the translator code, never the historical attribute set (see `agent-rules.md` → **Provider behavior claims must be verified against translator code**).
- Bug fixes, public-API surface changes, perf work post-2011 — those are in the linq2db git history.
- Anything about EF Core, DuckDB, ClickHouse, YDB, or providers added after the fork.

## Author attribution

When citing a pre-2011 introduction date, attribute as `igor.tkachev` (the author email on the BLToolkit commit) rather than the linq2db author of the 2011 import commit (which would be misleading — Tkachev was both). Use the full BLToolkit commit SHA (e.g. `82a81c86cc`) for traceability; linq2db's import commit (`6f5bb039`) is the boundary, not the origin.
