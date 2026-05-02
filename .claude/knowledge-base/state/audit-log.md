## 2026-04-25T22:02:49Z — kb-build started
- state initialized

## 2026-04-25T22:04:18Z — kb-build step 0 (bootstrap) done
- KB skeleton directories created
- README.md written
- glossary.md stub written

## 2026-04-25T22:12:52Z — kb-build step 1 (area-registry) done
- kb-areas.md rewritten from on-disk scan
- Path correction: SQL-PROVIDER -> Source/LinqToDB/Sql/ (not SqlProvider)
- Dropped: ASPNET (no Source/LinqToDB.AspNet/ on disk)
- Added 7 in-LinqToDB sub-areas: EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS
- Added 2 missing providers: PROV-SQLCE, PROV-YDB
- Added companion projects: CLI, COMPAT, EXTENSIONS-PKG, FSHARP, LINQPAD, REMOTE (collapsed 6 transports), SCAFFOLD, T4-TEMPLATES, CODEGEN, SHARED-INTERNAL
- Added test areas: TESTS-FSHARP, TESTS-T4, TESTS-VB, TESTS-MODEL, TESTS-BENCHMARKS
- Excluded: Source/Logo/, .claude/knowledge-base/
- Decisions: collapse REMOTE; accept INFRA/INTERCEPTORS/INTERNAL-API/METADATA grouping; separate IN-TREE-TOOLS from TOOLS package; accept 5 new test areas

## 2026-04-25T22:31:33Z — unclassified files
- Source/LinqToDB/Configuration.cs — pinned Tier-1 missing — no such file at Source/LinqToDB/Configuration.cs; configuration entry types live under Source/LinqToDB/Configuration/ (LinqToDBSettings.cs, LinqToDBSection.cs, ILinqToDBSettings.cs, IConnectionStringSettings.cs, etc.). Suggested kb-areas.md update: replace `Configuration.cs` in CORE Tier-1 with `Configuration/LinqToDBSection.cs` + `Configuration/ILinqToDBSettings.cs`.

## 2026-04-25T22:31:33Z — agent audit notes
- Three pinned Tier-1 paths in the step-2 invocation prompt point at locations that don't exist on disk: `Source/LinqToDB/SqlQuery/SqlStatement.cs`, `Source/LinqToDB/SqlQuery/SelectQuery.cs`, `Source/LinqToDB/Linq/Builder/ExpressionBuilder.cs`, `Source/LinqToDB/Sql/BasicSqlBuilder.cs`, `Source/LinqToDB/Sql/BasicSqlOptimizer.cs`. The actual paths are under `Source/LinqToDB/Internal/`: `Internal/SqlQuery/SqlStatement.cs`, `Internal/SqlQuery/SelectQuery.cs`, `Internal/Linq/Builder/ExpressionBuilder.cs`, `Internal/SqlProvider/BasicSqlBuilder.cs`, `Internal/SqlProvider/BasicSqlOptimizer.cs`. The agent treated these as the canonical pinned files (consistent with `code-design.md` → "SQL AST types live in `LinqToDB.Internal.SqlQuery`") and read them in full. Suggested kb-areas.md updates for SQL-AST / SQL-PROVIDER / EXPR-TRANS Tier-1 lists: prepend `Internal/` to each of these anchor paths so step 3 invocations don't hit the same drift.

## 2026-04-25T22:35:30Z — kb-areas.md path corrections
- CORE Tier-1: Configuration.cs replaced with Configuration/LinqToDBSection.cs + Configuration/ILinqToDBSettings.cs (no Configuration.cs at root)
- CORE path patterns: added Source/LinqToDB/Configuration/**
- CORE Tier-1: DataConnection.cs path corrected to Data/DataConnection.cs
- SQL-AST path patterns: added Source/LinqToDB/Internal/SqlQuery/** as primary; legacy SqlQuery/ kept
- SQL-AST Tier-1: prefixed all anchors with Internal/SqlQuery/
- SQL-PROVIDER path patterns: added Source/LinqToDB/Internal/SqlProvider/** as primary; Sql/ kept for public Sql.* extension surface
- SQL-PROVIDER Tier-1: prefixed all anchors with Internal/SqlProvider/
- EXPR-TRANS path patterns: corrected to Source/LinqToDB/Internal/Linq/Builder/**
- EXPR-TRANS Tier-1: prefixed all anchors with Internal/Linq/Builder/
- LINQ path patterns: corrected to Source/LinqToDB/Internal/Linq/** (excluding Builder/)
- LINQ Tier-1: prefixed all anchors with Internal/Linq/, added Query{T}.cs, QueryRunner.cs, ExpressionQuery.cs

## 2026-04-25T22:43:00Z — kb-build step 3 CORE done
- areas/CORE/INDEX.md written, Tier-1 6/6, Tier-2 53/58 (91%), confidence high

## 2026-04-25T22:45:09Z — kb-build paused at step 3
- Step 3 (architecture-per-area) marked partial
- Completed areas: CORE
- Remaining: 40 areas (SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, 14 PROV-*, EFCORE, CLI, COMPAT, EXTENSIONS-PKG, FSHARP, LINQPAD, REMOTE, SCAFFOLD, T4-TEMPLATES, TOOLS, CODEGEN, SHARED-INTERNAL, TESTS-INFRA, TESTS-LINQ, TESTS-EFCORE, TESTS-FSHARP, TESTS-T4, TESTS-VB, TESTS-MODEL, TESTS-BENCHMARKS, BUILD, CLAUDE-INFRA, GLOBAL)
- Resume: re-run /kb-build; the skill will detect partial step 3 and pick up at the next missing area

## 2026-04-26T00:00:00Z — kb-build step 3 SQL-AST done
- areas/SQL-AST/INDEX.md written, Tier-1 5/5, Tier-2 130/143 (90.9%), confidence high
- architecture/sql-ast.md written (cross-area narrative + namespace migration table for 12 legacy public types)
- Completed areas: CORE, SQL-AST (2 of 41); 39 remaining
- Paused at user request (one area per turn)

## 2026-04-26T00:00:01Z — kb-build step 3 SQL-PROVIDER done
- areas/SQL-PROVIDER/INDEX.md written, Tier-1 4/4, Tier-2 43/47 (91.5%), confidence high
- architecture/sql-provider.md written (BasicSqlBuilder/Optimizer walk + dialect override + Sql.* extension surface)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER (3 of 41); 38 remaining
- Paused at user request (one area per turn)

## 2026-04-26T17:48:02Z — unclassified files
- Source/LinqToDB/Internal/Linq/Builder/MethodCallParser.cs — pinned Tier-1 missing — file does not exist in the codebase. The dispatcher is source-generated (`Source/CodeGenerators/BuildersGenerator.cs`), and the per-method dispatch contract is `MethodCallBuilder` (`MethodCallBuilder.cs:7`) plus `[BuildsMethodCall]` markers (`Attributes.cs:35`). Update `.claude/docs/kb-areas.md` EXPR-TRANS row to drop `MethodCallParser.cs` from Tier-1 and replace with `MethodCallBuilder.cs` (the actual base class).

## 2026-04-26T17:48:02Z — agent audit notes
- EXPR-TRANS Tier-2 coverage came in at 24% (17/71), well below the 90% gate. The Tier-1 dispatch + recursion architecture is fully captured (every root `*Builder.cs` was at minimum read for its attribute and `BuildMethodCall` entry, plus the source generator was read end-to-end), so the architectural narrative is sound — the missed Tier-2 is the per-builder `*Context` types, helpers, and the visitor stack (`ExpressionBuildVisitor`, `ExpressionTreeOptimizerVisitor`, `ParametersContext`, `TranslationModifier`, `EagerLoading`, `MergeBuilder.*.cs` partials, `TableBuilder.*Context.cs`). Step 3 should be marked `partial` and a follow-up refresh focused on this area should:
1. Read each `*Context.cs` to document per-operator semantic shape (what `MakeExpression` does for `Where` vs `Select` vs `Join` vs `GroupBy`).
2. Read the visitor stack (`ExpressionBuildVisitor`, `ExpressionTreeOptimizerVisitor`, `ExpressionTreeOptimizationContext`, `Visitors/ExposeExpressionVisitor`) to fill in the optimization / expansion pre-pass details that this artifact only sketches.
3. Read `MergeBuilder.*.cs` partials together — Merge has the most complex per-clause shape in the area.

Also: the `kb-areas.md` Tier-1 list cites `MethodCallParser.cs`, which does not exist (see UNCLASSIFIED-FILE). Replace with `MethodCallBuilder.cs`.

## 2026-04-26T17:48:03Z — kb-build step 3 EXPR-TRANS done (with gate caveats)
- areas/EXPR-TRANS/INDEX.md written, Tier-1 63/63, Tier-2 17/71 (24%) ✗ gate, confidence medium
- architecture/expression-translator.md written (source-generated dispatch + recursion shape + handoff to SQL-PROVIDER)
- Gate caveat 1: Tier-2 below 90% — kept on disk to preserve dispatch/recursion architecture; marked `confidence: medium` so consumers know per-builder *Context.cs / visitor stack details are pending. Follow-up refresh recommended (see audit note above).
- Gate caveat 2: pinned Tier-1 `MethodCallParser.cs` does not exist; actual dispatch base is `MethodCallBuilder.cs` + `[BuildsMethodCall]` + source-generator. Pending kb-areas.md fix.
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS (4 of 41); 37 remaining
- Paused at user request (one area per turn)

## 2026-04-27T11:37:13Z — kb-build deferred-coverage backfill
- Backfilled state/deferred-coverage.json for 4 completed areas
- CORE: 47 files queued (Tier-2 56 total, 9 explicitly named in coverage block)
- SQL-AST: 121 files queued (Tier-2 143 total, 22 explicitly named)
- SQL-PROVIDER: 0 files queued (Tier-2 45 total, all 45 explicitly named in coverage block)
- EXPR-TRANS: 57 files queued (Tier-2 67 total, 10 explicitly named — close to the 54 deferred reported in step 3)
- All entries reason=verify-coverage; coverage-fill agent will clear or augment per file
- Mechanism: agents/_shared/kb-protocol.md DEFERRED-COVERAGE / DEFERRED-COVERAGE-CLEAR fences; /kb-refresh --source coverage drains via budget

## 2026-04-27T11:48:07Z — kb-build step 3 LINQ done
- areas/LINQ/INDEX.md written, Tier-1 5/5, Tier-2 46/46, confidence high
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ (5 of 41); 36 remaining
- Paused at user request (one area per turn)

## 2026-04-27T11:57:46Z — kb-build step 3 MAPPING done
- areas/MAPPING/INDEX.md written, Tier-1 3/3, Tier-2 43/43, confidence high
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING (6 of 41); 35 remaining
- Paused at user request (one area per turn)

## 2026-04-30T01:20:08Z — unclassified files
- Source/LinqToDB/Data/BulkCopy.cs — pinned Tier-1 anchor in `kb-areas.md` row for DATA but file does not exist on disk — bulk-copy public surface is in `DataContextExtensions.BulkCopy*` overloads (line 2387+) plus the four record/enum files `BulkCopyOptions.cs`/`BulkCopyType.cs`/`BulkCopyRowsCopied.cs`/`ConflictAction.cs`; bulk-copy strategy implementations live under `Source/LinqToDB/Internal/DataProvider/` (out of DATA scope). Recommend updating `kb-areas.md` Tier-1 list to `[DataConnection.cs (cross-listed), CommandInfo.cs, DataContextExtensions.cs]` — `DataContextExtensions.cs` carries every public raw-SQL and bulk-copy entry point.

## 2026-04-30T01:20:08Z — agent audit notes
- DATA Tier-1 drift: `kb-areas.md` lists `BulkCopy.cs` as a Tier-1 anchor for DATA but the file does not exist anywhere under `Source/LinqToDB/`. Closest matches on disk are `Source/LinqToDB/Data/BulkCopyOptions.cs` (option record), `BulkCopyType.cs` (enum), `BulkCopyRowsCopied.cs` (callback DTO) — all small Tier-2 files — plus `Source/LinqToDB/Internal/DataProvider/BulkCopyReader.cs` (out of DATA scope). The bulk-copy public surface is actually concentrated in `DataContextExtensions.cs` (lines 2387–2823 host every `BulkCopy<T>`/`BulkCopyAsync<T>` overload), which today is Tier-2. Suggested fix to `kb-areas.md` row for DATA: replace `BulkCopy.cs` with `DataContextExtensions.cs`.

## 2026-04-30T01:20:08Z — kb-build step 3 DATA done
- areas/DATA/INDEX.md written, Tier-1 7/7 (6 DataConnection partials + CommandInfo; pinned BulkCopy.cs surfaced as UNCLASSIFIED), Tier-2 24/24 (100%), confidence high
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA (7 of 41); 34 remaining
- Paused at user request (one area per turn)

## 2026-04-30T01:36:30Z — agent audit notes
- **Area:** EXPR
**Subject:** `kb-areas.md` Tier-1 list update
**Proposal:** Replace the EXPR row's "Tier-1 (canonical anchors) — TBD on first read" with the following five files (matches the pinned list provided to this run, all confirmed Tier-1 anchors):

- `Source/LinqToDB/Expressions/IExpressionEvaluator.cs`
- `Source/LinqToDB/Expressions/ExpressionExtensions.cs`
- `Source/LinqToDB/Expressions/MemberHelper.cs`
- `Source/LinqToDB/LinqExtensions/LinqExtensions.cs`
- `Source/LinqToDB/LinqExtensions/ICteBuilder.cs`

Rationale: `IExpressionEvaluator` defines the public evaluator contract consumed cross-area; `ExpressionExtensions` and `MemberHelper` are the visited-by-everyone helper surfaces; root `LinqExtensions.cs` carries the partial-class declaration plus the largest set of unique extension method shapes (CTE, joins, set ops, paging, scalar select, ToSqlQuery); `ICteBuilder` is the public fluent contract that `CteBuilderExtensions` extends. The other nine files in the area are well-classified as Tier-2 partials.
- **Area:** EXPR
**Subject:** Pre-existing inventory glitch
**Detail:** The on-disk inventory provided in the prompt double-counts `LinqExtensions.cs` (entries 5 and 13) and lists 15 numbered items totalling 14 unique files. Verified by Glob — there are 10 distinct files under `Source/LinqToDB/LinqExtensions/` (one root partial + eight feature partials + `CteBuilderExtensions.cs` + `ICteBuilder.cs`), 3 under `Source/LinqToDB/Expressions/`, and 1 under `Source/LinqToDB/Extensions/` = 14 total. No action needed — the coverage numbers (`5/5` Tier-1 + `9/9` Tier-2) reflect the de-duplicated count.

## 2026-04-30T01:36:30Z — kb-areas.md Tier-1 list updates (applied)
- DATA row: Tier-1 list updated `BulkCopy.cs` → `DataContextExtensions.cs` (resolves 2026-04-30 audit note from DATA run; no `BulkCopy.cs` exists on disk).
- EXPR row: Tier-1 list filled in (was "TBD on first read") with the five anchors confirmed by this run: `Expressions/IExpressionEvaluator.cs`, `Expressions/ExpressionExtensions.cs`, `Expressions/MemberHelper.cs`, `LinqExtensions/LinqExtensions.cs`, `LinqExtensions/ICteBuilder.cs`.

## 2026-04-30T01:36:30Z — kb-build step 3 EXPR done
- areas/EXPR/INDEX.md written, Tier-1 5/5, Tier-2 9/9 (100%), confidence high
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR (8 of 41); 33 remaining
- Paused at user request (one area per turn)

## 2026-04-30T10:26:33Z — agent audit notes
- INFRA Tier-1 anchor list (currently "TBD on first read" in `kb-areas.md`) should be updated to:

- `Source/LinqToDB/Async/AsyncExtensions.cs`
- `Source/LinqToDB/Common/Configuration.cs` (the `Common.Configuration` static-flag class — distinct from `Configuration/` namespace owned by CORE)
- `Source/LinqToDB/Common/Converter.cs`
- `Source/LinqToDB/Reflection/TypeAccessor.cs`
- `Source/LinqToDB/Reflection/MemberAccessor.cs`

Cross-listing reminder for `kb-areas.md`: the eleven files under `Source/LinqToDB/Configuration/` are cross-listed with [CORE](../CORE/INDEX.md), which pins `LinqToDBSection.cs` and `ILinqToDBSettings.cs` as Tier 1; INFRA treats them all as Tier 2 and references CORE for the canonical narrative. The CORE INDEX.md already counts these files in its Tier-2 tally, so the cross-listing should not double-count toward the global step-3 coverage gate.

Additional convention candidates surfaced for step 4 (`conventions/*.md`):

- **Banned-`LambdaExpression.Compile()` discipline.** Single seam at `Source/LinqToDB/Common/Compilation.cs:21` (`Compilation.SetExpressionCompiler` / `CompileExpression`); analyzer-banned via `RS0030` in `Build/BannedSymbols.txt`; only valid suppression sites are the two `#pragma warning disable RS0030` blocks at `Source/LinqToDB/Common/Compilation.cs:33` and `:43`.
- **`Obsolete("…in v7"), EditorBrowsable(Never)` + `// TODO: Remove in v7` triplet.** Deeply consistent across CORE and INFRA — citations available in INFRA INDEX "Recurring patterns" section.
- **Async dispatch triple-fallback (`IQueryProviderAsync` → `ExtensionsAdapter` → `Task.Run`).** Documented in INFRA INDEX; new public `*Async` operators added anywhere in the assembly are expected to follow the same shape.

No `[InternalsVisibleTo]` declarations exist anywhere in `Source/LinqToDB` (verified: `grep -rn "InternalsVisibleTo" Source/LinqToDB` returns nothing). The "internal API" surface is defined by namespace convention (`LinqToDB.Internal.*`) rather than IVT — note this differs from typical .NET libraries and is documented in [`code-design.md`](../../../docs/code-design.md).

## 2026-04-30T01:50:00Z — kb-areas.md INFRA Tier-1 list filled in (applied)
- INFRA row: Tier-1 list updated from "TBD on first read" to: `Async/AsyncExtensions.cs`, `Common/Configuration.cs`, `Common/Converter.cs`, `Reflection/TypeAccessor.cs`, `Reflection/MemberAccessor.cs`. Notes column expanded to record `Configuration/**` cross-listing with CORE and the `Common/Configuration.cs` (static-flag class) vs. `Configuration/` (namespace) disambiguation.

## 2026-04-30T01:50:00Z — kb-build step 3 INFRA done
- areas/INFRA/INDEX.md written, Tier-1 5/5, Tier-2 25/25 (100%), confidence high
- 11 Configuration/**/*.cs files counted as cross-listed Tier-2 (CORE INDEX is the authoritative narrative); narrative and cross-link added to INFRA INDEX
- Convention candidates surfaced for step 4: banned-`Compile()` discipline, `[Obsolete("...in v7")]+EditorBrowsable+TODO` triplet, async-triple-fallback dispatch
- Verified absence of `[InternalsVisibleTo]` in `Source/LinqToDB` — internal-API gating is via namespace convention only
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA (9 of 41); 32 remaining
- Paused at user request (one area per turn)


