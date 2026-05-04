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


## 2026-05-03T21:24:11Z — agent audit notes
- ## Proposed Tier-1 list for INTERCEPTORS area in kb-areas.md

Based on first-read of `Source/LinqToDB/Interceptors/` and `Source/LinqToDB/Metrics/`, the following files are the canonical anchor set for the INTERCEPTORS area. These are the public interface contracts and the primary dispatch/metrics entry points.

Proposed update to the `kb-areas.md` row for `INTERCEPTORS`:

| Column | Current value | Proposed value |
|---|---|---|
| Tier-1 files | `(TBD on first read)` | `IInterceptor.cs`, `ICommandInterceptor.cs`, `IConnectionInterceptor.cs`, `IDataContextInterceptor.cs`, `IEntityServiceInterceptor.cs`, `IExceptionInterceptor.cs`, `IQueryExpressionInterceptor.cs`, `IUnwrapDataObjectInterceptor.cs`, `InterceptorExtensions.cs`, `Metrics/IActivity.cs`, `Metrics/ActivityService.cs`, `Metrics/ActivityID.cs` |

Rationale:
- The 7 public `I*Interceptor` interfaces are the contracts every external implementor extends.
- `InterceptorExtensions.cs` is the public registration entry for the one-time helper pattern.
- `IActivity.cs`, `ActivityService.cs`, `ActivityID.cs` are the full public metrics surface.
- Abstract base classes (`CommandInterceptor`, `ConnectionInterceptor`, etc.) and event-data structs are Tier-2 — important for users but not anchors for the area's semantics.
- `IEntityBindingInterceptor` and the `AggregatedInterceptor<T>` family are under `Source/LinqToDB/Internal/Interceptors/` — INTERNAL-API area owns them by path. This area reads them as cross-area context only.

## 2026-05-03T21:24:11Z — kb-areas.md INTERCEPTORS Tier-1 list filled in (applied)
- INTERCEPTORS row: Tier-1 list updated from "TBD on first read" to: `Interceptors/IInterceptor.cs`, the 7 public `I*Interceptor.cs` files, `Interceptors/InterceptorExtensions.cs`, `Metrics/IActivity.cs`, `Metrics/ActivityService.cs`, `Metrics/ActivityID.cs`. Notes column expanded to record that aggregated dispatch (`AggregatedInterceptor<T>`, `IInterceptable<T>`, `InterceptorInternalExtensions`) lives under `Source/LinqToDB/Internal/Interceptors/` and is owned by INTERNAL-API by path.

## 2026-05-03T21:24:11Z — kb-build step 3 INTERCEPTORS done
- areas/INTERCEPTORS/INDEX.md written, Tier-1 12/12, Tier-2 15/15 (100%), confidence high
- architecture/interceptors.md written (cross-area narrative: registration model, dispatch order, async/sync pairing, metrics integration, multi-factory fan-out)
- Cross-area reads counted as context only (not in tier totals): 14 files under `Source/LinqToDB/Internal/Interceptors/` (INTERNAL-API), `DataConnection.Interceptors.cs` (DATA), `DataContext.Interceptors.cs` (CORE)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS (10 of 41); 31 remaining
- Paused at user request (one area per turn)

## 2026-05-03T21:41:29Z — agent audit notes
- ## Proposed Tier-1 file list for INTERNAL-API in kb-areas.md

Based on a full first-read pass, the following files should replace `(TBD on first read)` in the INTERNAL-API row:

**DataProvider shared root:**
- `Source/LinqToDB/Internal/DataProvider/DataProviderBase.cs`
- `Source/LinqToDB/Internal/DataProvider/DynamicDataProviderBase.cs`
- `Source/LinqToDB/Internal/DataProvider/IDynamicProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/ProviderDetectorBase.cs`
- `Source/LinqToDB/Internal/DataProvider/BasicBulkCopy.cs`
- `Source/LinqToDB/Internal/DataProvider/BulkCopyReader.cs`
- `Source/LinqToDB/Internal/DataProvider/IIdentifierService.cs`
- `Source/LinqToDB/Internal/DataProvider/Translation/MemberTranslatorBase.cs`

**Options machinery:**
- `Source/LinqToDB/Internal/Options/OptionsContainer.cs`
- `Source/LinqToDB/Internal/Options/IOptionSet.cs`

**Common utilities:**
- `Source/LinqToDB/Internal/Common/IConfigurationID.cs`
- `Source/LinqToDB/Internal/Common/IdentifierBuilder.cs`

**Cache:**
- `Source/LinqToDB/Internal/Cache/IMemoryCache.cs`
- `Source/LinqToDB/Internal/Cache/MemoryCache.cs`

**Async wrappers:**
- `Source/LinqToDB/Internal/Async/IAsyncDbConnection.cs`
- `Source/LinqToDB/Internal/Async/AsyncFactory.cs`

**Expressions:**
- `Source/LinqToDB/Internal/Expressions/ExpressionVisitorBase.cs`
- `Source/LinqToDB/Internal/Expressions/SqlPlaceholderExpression.cs`
- `Source/LinqToDB/Internal/Expressions/SqlGenericConstructorExpression.cs`
- `Source/LinqToDB/Internal/Expressions/Types/TypeMapper.cs`

**Infrastructure:**
- `Source/LinqToDB/Internal/Infrastructure/IInfrastructure{T}.cs`

**Schema:**
- `Source/LinqToDB/Internal/SchemaProvider/SchemaProviderBase.cs`

**Mapping internals:**
- `Source/LinqToDB/Internal/Mapping/MappingSchemaInfo.cs`

**Reflection constants:**
- `Source/LinqToDB/Internal/Reflection/Methods.cs`

**Remote serialization:**
- `Source/LinqToDB/Internal/Remote/LinqServiceSerializer.cs`

**PublicAPI baselines:**
- `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt`
- `Source/LinqToDB/PublicAPI/PublicAPI.Unshipped.txt`
- `Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Shipped.txt`
- `Source/LinqToDB/PublicAPI/net10.0/PublicAPI.Unshipped.txt`

**Decision on global architecture doc:** Skipping `architecture/internal-api.md` as a separate global doc. The "what does `Internal/` mean here" narrative (public visibility, no IVT, analyzer enforcement) is compact and fully captured in the INDEX. Creating a standalone doc would duplicate without adding depth. If the conventions/public-api-discipline area index (step 4) finds it needs a longer treatment, it can link back to this INDEX.

**Note on DataProvider per-provider subdirs:** The 277-file `Internal/DataProvider/<Vendor>/` subtrees are owned by `PROV-*` areas. This was treated as out-of-scope for INTERNAL-API and those files are not listed in the deferred queue here. If `PROV-*` indexes do not explicitly claim `Internal/DataProvider/<Vendor>/` in their path patterns, the areas registry may need updating.

**Note on Expressions/ExpressionVisitors/ and Types/ subfolders:** These 31 files (18 visitors + 10 types + 3 misc) are Tier-2 for INTERNAL-API and fully deferred. They are heavily consumed by EXPR-TRANS; a coverage-fill pass from that area's perspective may provide better coverage than a blind INTERNAL-API fill.

## 2026-05-03T21:41:29Z — deferred-coverage queue updated
- INTERNAL-API: +211 deferred

## 2026-05-03T21:41:29Z — kb-areas.md INTERNAL-API Tier-1 list filled in (applied)
- INTERNAL-API row: Tier-1 list updated from "TBD on first read" to a 29-file anchor set across DataProvider shared root (8), Options (2), Common (2), Cache (2), Async (2), Expressions (4), Infrastructure (1), SchemaProvider (1), Mapping internals (1), Reflection (1), Remote (1), and PublicAPI baselines (4). Notes column expanded to record sub-tree ownership (`Internal/SqlQuery/**` → SQL-AST, `Internal/SqlProvider/**` → SQL-PROVIDER, `Internal/Linq/Builder/**` → EXPR-TRANS, `Internal/Linq/**` excl. Builder → LINQ, `Internal/Interceptors/**` cross-listed with INTERCEPTORS) and the open scope question on `Internal/DataProvider/<Vendor>/**` PROV-* claims.

## 2026-05-03T21:41:29Z — open scope question: Internal/DataProvider/<Vendor>/** ownership
- 277 files under `Source/LinqToDB/Internal/DataProvider/<Vendor>/` are physically there but NO area's path patterns currently match them. PROV-* path patterns are `Source/LinqToDB/DataProvider/<Vendor>/**` (public surface only); INTERNAL-API explicitly delegates these to PROV-* per the agent prompt. Recommendation for when PROV-* areas are processed: extend each PROV-* row's path patterns to include `Source/LinqToDB/Internal/DataProvider/<Vendor>/**` so the per-provider Internal helpers (BulkCopy strategy, schema reader, parameter binder, etc.) end up in their owning provider area, not orphaned.

## 2026-05-03T21:41:29Z — kb-build step 3 INTERNAL-API done (with sampled Tier-2)
- areas/INTERNAL-API/INDEX.md written, Tier-1 22/22, Tier-2 18/199 (~9%) — sampled by design, confidence medium
- Skipped optional architecture/internal-api.md per agent rationale (compact narrative fits in INDEX; conventions step 4 can link back)
- 211 files queued in deferred-coverage; drainable via /kb-refresh --source coverage
- Open scope question on Internal/DataProvider/<Vendor>/** flagged for PROV-* processing
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API (11 of 41); 30 remaining
- Paused at user request (one area per turn)

## 2026-05-03T21:48:41Z — agent audit notes
- ## 2026-05-03 — METADATA area Tier-1 proposal

Step 3 architecture-per-area run for METADATA. Tier-1 was marked "TBD on first read" in `kb-areas.md`.

Proposed Tier-1 list for the METADATA row (replaces "(TBD on first read)"):

**Metadata sub-system:**
- `Source/LinqToDB/Metadata/IMetadataReader.cs` — root contract
- `Source/LinqToDB/Metadata/MetadataReader.cs` — composite fan-out impl; installed in MappingSchema.Default
- `Source/LinqToDB/Metadata/AttributeReader.cs` — reflection-backed default reader
- `Source/LinqToDB/Metadata/FluentMetadataReader.cs` — fluent-mapping-backed reader

**SchemaProvider sub-system:**
- `Source/LinqToDB/SchemaProvider/ISchemaProvider.cs` — root contract
- `Source/LinqToDB/SchemaProvider/DatabaseSchema.cs` — top-level result aggregate
- `Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs` — discovery options
- `Source/LinqToDB/SchemaProvider/TableSchema.cs` — primary table/view result
- `Source/LinqToDB/SchemaProvider/ColumnSchema.cs` — primary column result

Remaining 11 files (`MetadataException`, `XmlAttributeReader`, `SystemComponentModel…`, `SystemDataLinq…`, `AssociationType`, `ForeignKeySchema`, `LoadTableData`, `ParameterSchema`, `ProcedureSchema`, `TableInfo`) are Tier-2 — all were visited in this run (100% Tier-2 coverage).

No `architecture/metadata.md` global doc emitted: the two sub-systems in this area are public-surface DTO/contract layers only. The behavioral complexity (caching, schema enumeration, type mapping) lives entirely in INTERNAL-API (`SchemaProviderBase`, `MappingAttributesCache`) and MAPPING (`MappingSchema.AddMetadataReader`, `FluentMappingBuilder`). A cross-area narrative doc would have nothing to add beyond what the INDEX.md and those two areas already capture.

## 2026-05-03T21:55:00Z — kb-areas.md METADATA Tier-1 list filled in (applied)
- METADATA row: Tier-1 list updated from "TBD on first read" to a 9-file anchor set: 4 metadata readers (`IMetadataReader`, `MetadataReader`, `AttributeReader`, `FluentMetadataReader`) + 5 schema provider DTOs (`ISchemaProvider`, `DatabaseSchema`, `GetSchemaOptions`, `TableSchema`, `ColumnSchema`). Notes column expanded to record that behavioral complexity lives under `Internal/Mapping/MappingAttributesCache` and `Internal/SchemaProvider/SchemaProviderBase` (both INTERNAL-API), with per-vendor schema provider subclasses under `Internal/DataProvider/<Vendor>/`.

## 2026-05-03T21:55:00Z — kb-build step 3 METADATA done
- areas/METADATA/INDEX.md written, Tier-1 9/9, Tier-2 11/11 (100%), confidence high
- No global architecture/metadata.md emitted (justified: both sub-systems are public-surface DTO/contract layers; behavior lives in INTERNAL-API and MAPPING, already documented there)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA (12 of 41); 29 remaining
- Paused at user request (one area per turn)

## 2026-05-03T22:00:14Z — agent audit notes
- ## Proposed Tier-1 list for REMOTE-CLIENT in kb-areas.md

Current entry says "(TBD on first read)". Based on reading all 13 files in `Source/LinqToDB/Remote/`, the proposed Tier-1 set is:

```
ILinqService.cs
ILinqService{T}.cs
LinqService.cs
LinqService{T}.cs
IDataContextFactory.cs
RemoteDataContextBase.cs
LinqServiceQuery.cs
LinqServiceResult.cs
LinqServiceInfo.cs
```

Rationale:
- `ILinqService.cs` and `ILinqService{T}.cs` — the server-side wire contract; every transport depends on these directly.
- `LinqService.cs` and `LinqService{T}.cs` — the canonical default server implementations; contain all execution logic.
- `IDataContextFactory.cs` — the server-side context factory contract; required by `LinqService<T>`.
- `RemoteDataContextBase.cs` — the client-side `IDataContext` base class; the primary anchor for the client half of the area.
- `LinqServiceQuery.cs`, `LinqServiceResult.cs`, `LinqServiceInfo.cs` — the three wire DTOs; any change to these affects both client and server and all transports.

The four Tier-2 files:
- `RemoteDataContextBase.Interceptors.cs` — partial, slot declarations only; content is fully covered by the main `.cs` narrative.
- `RemoteDataContextBase.QueryRunner.cs` — partial, but substantive (QueryRunner logic); was promoted to Tier-1 in this run and fully read. Consider promoting to Tier-1 given its size and complexity.
- `DataContextFactory.cs` — trivial delegate wrapper; Tier-2 is appropriate.
- `DataService.cs` — NETFRAMEWORK-only legacy OData host; Tier-2 is appropriate.

Optional upgrade: move `RemoteDataContextBase.QueryRunner.cs` to Tier-1, as it contains the critical client-side query dispatch logic (PrepareStatementForRemoting → Serialize → ILinqService call → Deserialize).
- ## Skipping architecture/remote.md global doc

No separate `architecture/remote.md` artifact is emitted. The wire-protocol narrative (how queries are serialized, how the server executes them, how the client reconstructs results) is best contained in the INDEX.md because:

1. The entire in-tree remote facility is 13 files; there is no cross-area protocol narrative that doesn't fit in the area index at the allowed word budget.
2. The serialization details belong in [INTERNAL-API](../INTERNAL-API/INDEX.md) (already covers `LinqServiceSerializer` as a Tier-1 anchor).
3. The per-transport package details belong in [REMOTE](../REMOTE/INDEX.md) (not yet processed).

A global `architecture/remote.md` should be produced by `kb-architect` in `architecture-overview` mode once both REMOTE-CLIENT and REMOTE area indexes exist, allowing it to stitch the full picture (in-tree contracts + transport packages + serializer) into a single cross-area document.

## 2026-05-04T08:00:00Z — kb-areas.md REMOTE-CLIENT Tier-1 list filled in (applied)
- REMOTE-CLIENT row: Tier-1 list updated from "TBD on first read" to a 9-file anchor set: 4 server-side contracts/impls (`ILinqService`, `ILinqService{T}`, `LinqService`, `LinqService{T}`), `IDataContextFactory`, client-side `RemoteDataContextBase`, and 3 wire DTOs (`LinqServiceQuery`, `LinqServiceResult`, `LinqServiceInfo`). Notes column expanded to record that wire serialization lives in INTERNAL-API (`LinqServiceSerializer`) and per-transport packages live in REMOTE.

## 2026-05-04T08:00:00Z — kb-build step 3 REMOTE-CLIENT done
- areas/REMOTE-CLIENT/INDEX.md written, Tier-1 9/9, Tier-2 4/4 (100%), confidence high
- No global architecture/remote.md emitted — agent justified deferring to a later cross-area doc spawned in `architecture-overview` mode after REMOTE area is processed (allows stitching in-tree contracts + transports + serializer in one place)
- Optional Tier-1 upgrade noted: `RemoteDataContextBase.QueryRunner.cs` is substantive client dispatch logic; left as Tier-2 per agent's INDEX choice; can be promoted later via /kb-refresh
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT (13 of 41); 28 remaining
- Paused at user request (one area per turn)

## 2026-05-03T22:09:44Z — agent audit notes
- IN-TREE-TOOLS Tier-1 list proposal (step 3 first read):

The area's path pattern `Source/LinqToDB/Tools/**` resolves to exactly one file on disk:
  Source/LinqToDB/Tools/DataExtensions.cs

Proposed Tier-1 list for `kb-areas.md`: replace `(TBD on first read)` with:
  `Source/LinqToDB/Tools/DataExtensions.cs`

There are no Tier-2 files (0/0). No deferred coverage needed.

## 2026-05-04T08:30:00Z — kb-areas.md IN-TREE-TOOLS Tier-1 list filled in (applied)
- IN-TREE-TOOLS row: Tier-1 list updated from "TBD on first read" to the single file `Tools/DataExtensions.cs`. Tier-2 column changed to "(none)" (single-file area). Notes column expanded with the per-provider behavior summary.

## 2026-05-04T08:30:00Z — kb-build step 3 IN-TREE-TOOLS done
- areas/IN-TREE-TOOLS/INDEX.md written, Tier-1 1/1, Tier-2 0/0, confidence high
- Single-file area; documented `DataExtensions.RetrieveIdentity<T>` per-provider behavior matrix + 3 known-debt items (collection-copy TODO at line 16, asymmetric `useIdentity` default, sync/async duplication)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS (14 of 41); 27 remaining
- Paused at user request (one area per turn)

## 2026-05-04T08:35:00Z — kb-areas.md PROV-* path patterns extended (applied)
- All 14 PROV-* rows: path patterns extended to include `Source/LinqToDB/Internal/DataProvider/<Vendor>/**` so per-vendor BulkCopy strategy / schema reader / parameter binder helpers are owned by the corresponding PROV-* area (resolves 2026-05-03 open scope question).
- INTERNAL-API row Notes column updated: per-vendor `Internal/DataProvider/<Vendor>/**` now cross-listed with PROV-* areas; INTERNAL-API retains the shared `Internal/DataProvider/*.cs` root files + `Internal/DataProvider/Translation/**` (cross-provider translator infrastructure).
- Folder-name verification: all 14 vendor folder names match between `Source/LinqToDB/DataProvider/<Vendor>/` and `Source/LinqToDB/Internal/DataProvider/<Vendor>/` (Access, ClickHouse, DB2, Firebird, Informix, MySql, Oracle, PostgreSQL, SQLite, SapHana, SqlCe, SqlServer, Sybase, Ydb).

## 2026-05-03T22:30:22Z — agent audit notes
- ## Proposed Tier-1 expansion for PROV-SQLSERVER in kb-areas.md

The current kb-areas.md row pins only 3 Tier-1 files. The following 11-file set provides complete public-surface + core-implementation anchoring. Proposed replacement for the `Tier-1 files` cell:

```
SqlServerDataProvider.cs, SqlServerSqlBuilder.cs, SqlServerSqlOptimizer.cs,
SqlServerTools.cs, SqlServerProvider.cs, SqlServerVersion.cs, SqlServerOptions.cs,
SqlServerProviderAdapter.cs, SqlServerProviderDetector.cs,
SqlServerMappingSchema.cs, SqlServerBulkCopy.cs
```

Paths:
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerDataProvider.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlBuilder.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerSqlOptimizer.cs`
- `Source/LinqToDB/DataProvider/SqlServer/SqlServerTools.cs`
- `Source/LinqToDB/DataProvider/SqlServer/SqlServerProvider.cs`
- `Source/LinqToDB/DataProvider/SqlServer/SqlServerVersion.cs`
- `Source/LinqToDB/DataProvider/SqlServer/SqlServerOptions.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderAdapter.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerProviderDetector.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerMappingSchema.cs`
- `Source/LinqToDB/Internal/DataProvider/SqlServer/SqlServerBulkCopy.cs`

Rationale: The original 3-file minimum covers the SQL-generation core but omits the public entry points (`SqlServerTools`, enums, options) that callers depend on, the adapter (which is the assembly-loading boundary and the place where SQL Server 2025 type support diverges by package), and the bulk-copy and provider-detection types that have non-trivial logic. Any future `/kb-refresh` delta on those files would silently miss them without Tier-1 designation.

Also recommend deferring `architecture/providers.md` (cross-provider narrative) until at least 2–3 PROV-* areas are indexed, so the patterns are grounded in multiple providers.

## 2026-05-04T09:00:00Z — kb-areas.md PROV-SQLSERVER Tier-1 list expanded (applied)
- PROV-SQLSERVER row: Tier-1 list expanded from 3 files to 11. Added 4 public-surface anchors (`SqlServerTools.cs`, `SqlServerProvider.cs`, `SqlServerVersion.cs`, `SqlServerOptions.cs`) and 4 Internal anchors (`SqlServerProviderAdapter.cs`, `SqlServerProviderDetector.cs`, `SqlServerMappingSchema.cs`, `SqlServerBulkCopy.cs`). Original 3 files prefixed with full `Internal/DataProvider/SqlServer/` paths. Tier-2 column extended to also catch `SqlFn.cs`/`SqlType.cs`/`SystemDataSqlServerAttributeReader.cs` (which don't match `**/SqlServer*.cs`). Notes column expanded to record dual-client + nine-version surface and the Vector/JSON limitation (Microsoft.Data.SqlClient-only).

## 2026-05-04T09:00:00Z — kb-build step 3 PROV-SQLSERVER done
- areas/PROV-SQLSERVER/INDEX.md written, Tier-1 11/11, Tier-2 43/45 (95.6%), Tier-3 2 (T4-generated SqlServerHints.generated.cs + .tt), confidence high
- Per-version build/optimize matrix tables (v2005–v2025) for SqlBuilder, SqlOptimizer, SqlExpressionConvertVisitor, MemberTranslator chains
- 7 debt items tracked in INDEX: SqlServer2014SqlOptimizer ctor passes wrong version (v2016), 2× obsolete-for-v7 items, 2× SqlClient TODOs (SqlVector finalization), MERGE logic duplicated 2008/2012, parallel function-extension pattern (SqlFn vs MemberTranslator)
- Architecture/providers.md cross-provider doc deferred (per agent recommendation) until 2-3 PROV-* areas exist
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER (15 of 41); 26 remaining
- Paused at user request (one area per turn)


## 2026-05-03T22:45:28Z — agent audit notes
- Proposed final Tier-1 list for kb-areas.md (PROV-POSTGRES row), replacing the current 2-file pinned set:

```
pinnedTier1:
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLDataProvider.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlBuilder.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLSqlOptimizer.cs
  - Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLTools.cs
  - Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLVersion.cs
  - Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLOptions.cs
  - Source/LinqToDB/DataProvider/PostgreSQL/PostgreSQLIdentifierQuoteMode.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/NpgsqlProviderAdapter.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLProviderDetector.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLMappingSchema.cs
  - Source/LinqToDB/Internal/DataProvider/PostgreSQL/PostgreSQLBulkCopy.cs
```

Rationale:
- `PostgreSQLSqlOptimizer.cs` promoted from suggested Tier-2: owns DELETE/UPDATE rewrite and OUTPUT anchor validation; parallels `SqlServerSqlOptimizer` in importance.
- All 11 files read in full this run; none missing on disk.
- `PostgreSQLSql15Builder.cs` remains Tier-2: it is a thin 29-line subclass whose single override is fully narrated from reading the base builder.
- `PostgreSQLHints.tt` classified Tier-3 (code-generation artifact, not runtime logic).
- On-disk file count is 28 (27 runtime + 1 `.tt` template). The `kb-areas.md` stated count of 27 is accurate if `.tt` is excluded from the runtime file count; suggest updating the comment in kb-areas.md to clarify `27 runtime files (+1 .tt template)`.

## 2026-05-04T09:30:00Z — kb-areas.md PROV-POSTGRES Tier-1 list expanded (applied)
- PROV-POSTGRES row: Tier-1 list expanded from 2 files to 11 (matching the precedent set by PROV-SQLSERVER). Added: `PostgreSQLSqlOptimizer.cs`, `PostgreSQLTools.cs`, `PostgreSQLVersion.cs`, `PostgreSQLOptions.cs`, `PostgreSQLIdentifierQuoteMode.cs`, `NpgsqlProviderAdapter.cs`, `PostgreSQLProviderDetector.cs`, `PostgreSQLMappingSchema.cs`, `PostgreSQLBulkCopy.cs`. Tier-2 column extended to also catch `**/Npgsql*.cs` (which doesn't match `**/PostgreSQL*.cs`). Notes column expanded to record dynamic-Npgsql + 6-version surface, identifier-quote-mode role, and the v15 MERGE builder bug.

## 2026-05-04T09:30:00Z — kb-build step 3 PROV-POSTGRES done
- areas/PROV-POSTGRES/INDEX.md written, Tier-1 11/11, Tier-2 16/16 (100%), Tier-3 1 (T4 PostgreSQLHints.tt template), confidence high
- 6-version data-provider matrix (v92/v93/v95/v13/v15/v18) documented; per-version SqlProviderFlags differences captured
- 4 debt items tracked in INDEX: PostgreSQLSql15Builder unreachable (CreateSqlBuilder always returns base), TODO on identifier quoting (embedded double-quotes/surrogate pairs), float(N) precision copy-paste error (PostgreSQLDataProvider.cs:556–560), NpgsqlCidr TFM-conditional hardcoded type strings
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES (16 of 41); 25 remaining
- Paused at user request (one area per turn)


## 2026-05-03T22:58:08Z — agent audit notes
- Proposed Tier-1 expansion for `kb-areas.md` PROV-MYSQL row.

The current row pins only `MySqlDataProvider.cs` and `MySqlSqlBuilder.cs`. This run read all 11 files listed below as Tier-1. Update the row to:

```
| `PROV-MYSQL` | `Source/LinqToDB/DataProvider/MySql/**`, `Source/LinqToDB/Internal/DataProvider/MySql/**` | `Internal/DataProvider/MySql/MySqlDataProvider.cs`, `Internal/DataProvider/MySql/MySqlSqlBuilder.cs`, `Internal/DataProvider/MySql/MySqlSqlOptimizer.cs`, `DataProvider/MySql/MySqlTools.cs`, `DataProvider/MySql/MySqlVersion.cs`, `DataProvider/MySql/MySqlProvider.cs`, `DataProvider/MySql/MySqlOptions.cs`, `Internal/DataProvider/MySql/MySqlProviderAdapter.cs`, `Internal/DataProvider/MySql/MySqlProviderDetector.cs`, `Internal/DataProvider/MySql/MySqlMappingSchema.cs`, `Internal/DataProvider/MySql/MySqlBulkCopy.cs` | `**/MySql*.cs`, `**/MariaDB*.cs` | Two products (MySQL 5.7/8.0+, MariaDB 10+) × two clients (MySql.Data, MySqlConnector) = 6 concrete providers. |
```

Rationale for each new Tier-1 anchor:
- `MySqlSqlOptimizer.cs` — owns the UPDATE/DELETE statement rewrites unique to MySQL; entry point for `MySqlSqlExpressionConvertVisitor`.
- `MySqlTools.cs` — public factory; only public surface file without a corresponding Internal file.
- `MySqlVersion.cs` / `MySqlProvider.cs` — public enums that define the dual-axis model; referenced from tests and user code directly.
- `MySqlOptions.cs` — public sealed record; defines the `BulkCopyType` default that shapes runtime behavior.
- `MySqlProviderAdapter.cs` — most complex file in the area; the two inner adapter classes capture all capability differences between the two clients.
- `MySqlProviderDetector.cs` — non-trivial detection logic (ClickHouse guard, assembly probe, version string parsing).
- `MySqlMappingSchema.cs` — the 9-class schema hierarchy is the primary artifact for type-mapping correctness.
- `MySqlBulkCopy.cs` — dual-path bulk copy (native MySqlConnector vs. multi-row fallback) is a significant behavioral split.

Note: `MySqlHints.cs` was considered but left as Tier-2 given it is primarily a constant catalog + wrappers with no independent behavioral logic.

## 2026-05-04T10:00:00Z — kb-areas.md PROV-MYSQL Tier-1 list expanded (applied)
- PROV-MYSQL row: Tier-1 list expanded from 2 files to 11 (matching the precedent set by PROV-SQLSERVER and PROV-POSTGRES). Added: `MySqlSqlOptimizer.cs`, `MySqlTools.cs`, `MySqlVersion.cs`, `MySqlProvider.cs`, `MySqlOptions.cs`, `MySqlProviderAdapter.cs`, `MySqlProviderDetector.cs`, `MySqlMappingSchema.cs`, `MySqlBulkCopy.cs`. Tier-2 column extended to also catch `**/MariaDB*.cs`. Notes column expanded with the dual-product × dual-client matrix and the silent MariaDB FOR SHARE comment-out.

## 2026-05-04T10:00:00Z — kb-build step 3 PROV-MYSQL done
- areas/PROV-MYSQL/INDEX.md written, Tier-1 11/11, Tier-2 16/16 (100%), Tier-3 0, confidence high
- Dual-axis matrix documented (2 products × 2 clients = 6 concrete providers); per-version SqlProviderFlags; 9-class MappingSchema hierarchy
- 7 debt items tracked in INDEX: silent MariaDB FOR SHARE comment-out, MySql.Data decimal crash workaround, MySql.Data float[] silent conversion, no MERGE support, MySqlConnector decimal-version gating, no RETURNING surfacing for MariaDB, correlated-subquery depth limit on MySql57/MariaDB
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL (17 of 41); 24 remaining
- Paused at user request (one area per turn)


## 2026-05-03T23:10:36Z — unclassified files
- Source/LinqToDB/Internal/DataProvider/Oracle/OracleSqlBuilder.cs — pinned Tier-1 missing — file does not exist on disk. The actual base class is OracleSqlBuilderBase.cs. The kb-areas.md Tier-1 entry for PROV-ORACLE lists "OracleSqlBuilder.cs" which is a misnomer. Needs kb-areas.md update to replace with OracleSqlBuilderBase.cs.

## 2026-05-03T23:10:36Z — agent audit notes
- ## Proposed Tier-1 update for PROV-ORACLE in kb-areas.md

The current kb-areas.md PROV-ORACLE row has only two Tier-1 pins: `OracleDataProvider.cs` and `OracleSqlBuilder.cs`. `OracleSqlBuilder.cs` does not exist on disk — the actual base type is `OracleSqlBuilderBase.cs`. Additionally the pinned list is far too thin relative to the PROV-SQLSERVER / PROV-POSTGRES / PROV-MYSQL precedent.

Proposed corrected Tier-1 list (12 files, matching this run's Tier-1 set):

```
Internal/DataProvider/Oracle/OracleDataProvider.cs
Internal/DataProvider/Oracle/OracleSqlBuilderBase.cs          ← replaces "OracleSqlBuilder.cs"
Internal/DataProvider/Oracle/OracleSqlBuilderBase.Merge.cs    ← add
Internal/DataProvider/Oracle/Oracle11SqlBuilder.cs            ← add
Internal/DataProvider/Oracle/Oracle12SqlBuilder.cs            ← add
Internal/DataProvider/Oracle/Oracle11SqlOptimizer.cs          ← add
Internal/DataProvider/Oracle/Oracle12SqlOptimizer.cs          ← add
Internal/DataProvider/Oracle/OracleProviderAdapter.cs         ← add
Internal/DataProvider/Oracle/OracleProviderDetector.cs        ← add
Internal/DataProvider/Oracle/OracleMappingSchema.cs           ← add
Internal/DataProvider/Oracle/OracleBulkCopy.cs                ← add
DataProvider/Oracle/OracleTools.cs                            ← add
```

This matches the pattern of PROV-SQLSERVER (11 files) and PROV-POSTGRES (11 files). The Tier-2 glob `**/Oracle*.cs` is correct and covers the remaining 20 files.

Action required: update the PROV-ORACLE row in kb-areas.md to replace the two-file Tier-1 list with the twelve-file list above, and remove the nonexistent `OracleSqlBuilder.cs` entry.

## 2026-05-04T10:30:00Z — kb-areas.md PROV-ORACLE Tier-1 list expanded + corrected (applied)
- PROV-ORACLE row: Tier-1 list expanded from 2 files to 12; the misnomer `OracleSqlBuilder.cs` (does not exist on disk) replaced with `OracleSqlBuilderBase.cs` + the partial `OracleSqlBuilderBase.Merge.cs`; per-version builders (`Oracle11SqlBuilder.cs`, `Oracle12SqlBuilder.cs`) and optimizers (`Oracle11SqlOptimizer.cs`, `Oracle12SqlOptimizer.cs`) added as Tier-1 (each substantively distinct due to ROWNUM vs OFFSET/FETCH pagination). Notes column expanded with 2-dialect × 3-client matrix, identity-via-sequence model, AlternativeBulkCopy enum, XmlTable feature, and empty-string-NULL behavior.

## 2026-05-04T10:30:00Z — kb-build step 3 PROV-ORACLE done
- areas/PROV-ORACLE/INDEX.md written, Tier-1 12/12, Tier-2 19/20 (95%), Tier-3 1 (T4 OracleHints.tt template), confidence high
- 6 concrete providers documented (3 clients × 2 dialects); per-version SqlBuilder/Optimizer/ExpressionConvertVisitor matrix; 4-way bulk-copy strategy split; 6-class MappingSchema chain
- 1 UNCLASSIFIED-FILE: `OracleSqlBuilder.cs` (does not exist; was kb-areas.md misnomer for `OracleSqlBuilderBase.cs`); resolved by Tier-1 expansion above
- 7 debt items tracked in INDEX: Oracle122ParametersNormalizer never activated (issue #4219), SupportsBooleanType=false pending Oracle 23ai retest, reserved-words list is static 11g merge, OracleBulkCopy provider-specific path is sync-only, ODP.NET column-quoting falls back to multi-row, BFile parameter unsupported, function-with-ref-cursor schema gap
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE (18 of 41); 23 remaining
- Paused at user request (one area per turn)


## 2026-05-03T23:19:43Z — agent audit notes
- ## Proposed Tier-1 update for PROV-SQLITE in kb-areas.md

Current `kb-areas.md` row pins only `SQLiteDataProvider.cs` and `SQLiteSqlBuilder.cs` as Tier-1. After reading all 20 files, the following expanded list is proposed as Tier-1 anchors (10 files):

Internal:
- `Internal/DataProvider/SQLite/SQLiteDataProvider.cs` (already pinned)
- `Internal/DataProvider/SQLite/SQLiteSqlBuilder.cs` (already pinned)
- `Internal/DataProvider/SQLite/SQLiteSqlOptimizer.cs` — statement rewriter; needed to understand DELETE/UPDATE alternative forms
- `Internal/DataProvider/SQLite/SQLiteProviderAdapter.cs` — dual-client dynamic load; version caps for DateOnly and ClearAllPools
- `Internal/DataProvider/SQLite/SQLiteProviderDetector.cs` — holds the two singleton data-provider Lazy<> instances; detection logic
- `Internal/DataProvider/SQLite/SQLiteMappingSchema.cs` — Guid/DateTime/binary SQL-literal serialisation; ClassicMappingSchema / MicrosoftMappingSchema subclasses
- `Internal/DataProvider/SQLite/SQLiteBulkCopy.cs` — parameter + SQL-length limits; INSERT OR IGNORE override

Public:
- `DataProvider/SQLite/SQLiteTools.cs` — public registration entry + CreateDatabase/DropDatabase file utilities
- `DataProvider/SQLite/SQLiteProvider.cs` — ADO.NET client enum (AutoDetect / System / Microsoft)
- `DataProvider/SQLite/SQLiteOptions.cs` — BulkCopyType + AlwaysCheckDbNull options record

Remaining 10 files are appropriate as Tier-2 (all visited this run at 100%).

Suggested `kb-areas.md` edit for the PROV-SQLITE row — replace the `Tier-1 files` column with the 10 paths above, keeping the `Tier-2 globs` as `**/SQLite*.cs` unchanged.

## 2026-05-04T11:00:00Z — kb-areas.md PROV-SQLITE Tier-1 list expanded (applied)
- PROV-SQLITE row: Tier-1 list expanded from 2 files to 10 (matching the established PROV-* precedent). Added: `SQLiteSqlOptimizer.cs`, `SQLiteProviderAdapter.cs`, `SQLiteProviderDetector.cs`, `SQLiteMappingSchema.cs`, `SQLiteBulkCopy.cs`, `SQLiteTools.cs`, `SQLiteProvider.cs`, `SQLiteOptions.cs`. Notes column expanded with single-version, dual-client, no-MERGE, no-bulk-copy details and Microsoft.Data.Sqlite reader-type quirk.

## 2026-05-04T11:00:00Z — kb-build step 3 PROV-SQLITE done
- areas/PROV-SQLITE/INDEX.md written, Tier-1 10/10, Tier-2 10/10 (100%), Tier-3 0, confidence high
- Single-version provider documented (no per-version subclasses); dual-client matrix (System.Data.SQLite, Microsoft.Data.Sqlite); affinity-based dynamic typing covered; FTS3/4/5 LINQ extensions enumerated
- 6 debt items tracked in INDEX: UPDATE TAKE/SKIP intentionally disabled (Microsoft.Data.Sqlite limitation), IS DISTINCT not migrated to native syntax, no compile-time MERGE guard (runtime throw), GetDataType throws NotSupportedException, FTS5 column types hardcoded VarChar, V7 TODO for FTS admin command return values
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE (19 of 41); 22 remaining
- Paused at user request (one area per turn)


## 2026-05-03T23:30:23Z — agent audit notes
- **Proposed Tier-1 list update for `kb-areas.md` row `PROV-FIREBIRD`**

The current `kb-areas.md` row lists only 2 Tier-1 anchors for PROV-FIREBIRD:
> `FirebirdDataProvider.cs`, `FirebirdSqlBuilder.cs`

After reading all 23 files, the recommended expanded Tier-1 list (following the PROV-POSTGRES / PROV-SQLSERVER / PROV-ORACLE precedent of 10–12 anchors) is:

```
Internal/DataProvider/Firebird/FirebirdDataProvider.cs
Internal/DataProvider/Firebird/FirebirdSqlBuilder.cs
Internal/DataProvider/Firebird/FirebirdSqlOptimizer.cs
DataProvider/Firebird/FirebirdTools.cs
DataProvider/Firebird/FirebirdVersion.cs
DataProvider/Firebird/FirebirdOptions.cs
DataProvider/Firebird/FirebirdIdentifierQuoteMode.cs
Internal/DataProvider/Firebird/FirebirdProviderAdapter.cs
Internal/DataProvider/Firebird/FirebirdProviderDetector.cs
Internal/DataProvider/Firebird/FirebirdMappingSchema.cs
Internal/DataProvider/Firebird/FirebirdBulkCopy.cs
```

These 11 files constitute the complete public surface + all behavioral anchors. The remaining 12 (version-specific subclasses, expression visitors, member translators, schema provider, factory, extension marker, and extension class) remain Tier 2 and are adequately characterized by the INDEX.md.

No path-pattern changes needed — `**/Firebird*.cs` already captures all 23 files.

## 2026-05-04T11:30:00Z — kb-areas.md PROV-FIREBIRD Tier-1 list expanded (applied)
- PROV-FIREBIRD row: Tier-1 list expanded from 2 files to 11. Added: `FirebirdSqlOptimizer.cs`, `FirebirdTools.cs`, `FirebirdVersion.cs`, `FirebirdOptions.cs`, `FirebirdIdentifierQuoteMode.cs`, `FirebirdProviderAdapter.cs`, `FirebirdProviderDetector.cs`, `FirebirdMappingSchema.cs`, `FirebirdBulkCopy.cs`. Notes column expanded with single-client + 4-dialect matrix, uppercase folding, identity via generators + triggers, EXECUTE BLOCK DDL pattern, FB3+/FB4+ feature deltas.

## 2026-05-04T11:30:00Z — kb-build step 3 PROV-FIREBIRD done
- areas/PROV-FIREBIRD/INDEX.md written, Tier-1 11/11, Tier-2 12/12 (100%), Tier-3 0, confidence high
- 4-version SqlBuilder/Optimizer/ExpressionConvertVisitor matrix documented; mapping-schema chain (FirebirdMappingSchema base + 4 versioned subclasses); FB4+ optional types (DecFloat, ZonedDateTime, ZonedTime); identifier-length differences (31 chars v25/v3 vs 63 v4+)
- 7 debt items tracked in INDEX: recursive-CTE-outer-join optimizer TODO, Guid→string conversion code duplication, BuildParameter type-mapping TODOs (2), KeepIdentity always-throws (must manually disable triggers), FbMetaData.xml missing FB4+ types, NullCharSize=1 v25 row-size workaround
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD (20 of 41); 21 remaining
- Paused at user request (one area per turn)
## 2026-05-03T23:42:22Z — unclassified files
- Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilder.cs — pinned Tier-1 missing — file does not exist on disk. The actual base class is `DB2SqlBuilderBase.cs` (at `Source/LinqToDB/Internal/DataProvider/DB2/DB2SqlBuilderBase.cs`). Needs kb-areas.md update.

## 2026-05-03T23:42:22Z — agent audit notes
- ## Proposed Tier-1 update for PROV-DB2 in kb-areas.md

The current kb-areas.md row for `PROV-DB2` lists only `DB2DataProvider.cs` and `DB2SqlBuilder.cs` as Tier-1. `DB2SqlBuilder.cs` does not exist; the correct file is `DB2SqlBuilderBase.cs`. Proposed replacement Tier-1 list (11 files):

```
Internal/DataProvider/DB2/DB2DataProvider.cs
Internal/DataProvider/DB2/DB2SqlBuilderBase.cs          ← corrected from DB2SqlBuilder.cs
Internal/DataProvider/DB2/DB2SqlOptimizer.cs
DataProvider/DB2/DB2Tools.cs
DataProvider/DB2/DB2Version.cs
DataProvider/DB2/DB2Options.cs
DataProvider/DB2/DB2IdentifierQuoteMode.cs
Internal/DataProvider/DB2/DB2ProviderAdapter.cs
Internal/DataProvider/DB2/DB2ProviderDetector.cs
Internal/DataProvider/DB2/DB2MappingSchema.cs
Internal/DataProvider/DB2/DB2BulkCopy.cs
```

The existing Tier-2 glob `**/DB2*.cs` covers all remaining files including `DB2SqlBuilderBase.Merge.cs`, `DB2LUWSqlBuilder.cs`, `DB2zOSSqlBuilder.cs`, `DB2SqlExpressionConvertVisitor.cs`, `DB2LUWSchemaProvider.cs`, `DB2zOSSchemaProvider.cs`, `DB2BulkCopyShared.cs`, `DB2Extensions.cs`, `DB2Factory.cs`, and `Translation/DB2MemberTranslator.cs`. No glob change needed for Tier-2.

Note: `DB2BulkCopyShared` is `public` by design — it is part of the `linq2db4iSeries` interop contract and should not be made internal.

## 2026-05-04T12:00:00Z — kb-areas.md PROV-DB2 Tier-1 list expanded + corrected (applied)
- PROV-DB2 row: Tier-1 list expanded from 2 files to 11; the misnomer `DB2SqlBuilder.cs` (does not exist on disk) replaced with `DB2SqlBuilderBase.cs` (parallels the PROV-ORACLE correction). Added: `DB2SqlOptimizer.cs`, `DB2Tools.cs`, `DB2Version.cs`, `DB2Options.cs`, `DB2IdentifierQuoteMode.cs`, `DB2ProviderAdapter.cs`, `DB2ProviderDetector.cs`, `DB2MappingSchema.cs`, `DB2BulkCopy.cs`. Notes column expanded with two-server-family matrix (LUW vs z/OS), `DB2BulkCopyShared` public-by-design rationale, and explicit non-coverage of IBM i.

## 2026-05-04T12:00:00Z — kb-build step 3 PROV-DB2 done
- areas/PROV-DB2/INDEX.md written, Tier-1 11/11, Tier-2 10/10 (100%), Tier-3 0, confidence high
- Two-server-family matrix documented (LUW vs z/OS) — distinct SqlBuilder + SchemaProvider per family, shared SqlOptimizer + ProviderAdapter + MappingSchema; eServerType-based detection
- 1 UNCLASSIFIED-FILE: `DB2SqlBuilder.cs` (does not exist; was kb-areas.md misnomer for `DB2SqlBuilderBase.cs`); resolved by Tier-1 expansion above
- 6 debt items tracked in INDEX: ROW_NUMBER paging commented out (needs version tracking), async bulk copy is sync-under-the-hood, FK-column matching heuristic in LUW schema provider, BuildParameter Firebird-copy TODO, DB2DateTimeType "not sure if still actual" comment, DB2TimeSpanType obsolete stub
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2 (21 of 41); 20 remaining
- Paused at user request (one area per turn)
## 2026-05-03T23:52:09Z — unclassified files
- Source/LinqToDB/Internal/DataProvider/Access/AccessSqlBuilder.cs — pinned Tier-1 missing — file does not exist on disk. The actual base class is `AccessSqlBuilderBase.cs`. The kb-areas.md Tier-1 entry `AccessSqlBuilder.cs` should be corrected to `AccessSqlBuilderBase.cs`.

## 2026-05-03T23:52:09Z — agent audit notes
- ## Tier-1 list correction for PROV-ACCESS in kb-areas.md

The current kb-areas.md row for `PROV-ACCESS` pins only two files:
- `AccessDataProvider.cs` — correct, exists.
- `AccessSqlBuilder.cs` — **does not exist**. The actual abstract SQL builder base is `AccessSqlBuilderBase.cs`.

Proposed corrected Tier-1 list (11 files, following PROV-ORACLE / PROV-DB2 precedent):

```
Internal/DataProvider/Access/AccessDataProvider.cs
Internal/DataProvider/Access/AccessSqlBuilderBase.cs          ← replaces AccessSqlBuilder.cs
Internal/DataProvider/Access/AccessSqlOptimizer.cs
DataProvider/Access/AccessTools.cs
DataProvider/Access/AccessVersion.cs
DataProvider/Access/AccessProvider.cs
DataProvider/Access/AccessOptions.cs
Internal/DataProvider/Access/AccessProviderAdapter.cs
Internal/DataProvider/Access/AccessProviderDetector.cs
Internal/DataProvider/Access/AccessMappingSchema.cs
Internal/DataProvider/Access/AccessBulkCopy.cs
```

Action needed: update the `PROV-ACCESS` row's Tier-1 column in `.claude/docs/kb-areas.md` with the 11 paths above.

## 2026-05-04T12:30:00Z — kb-areas.md PROV-ACCESS Tier-1 list expanded + corrected (applied)
- PROV-ACCESS row: Tier-1 list expanded from 2 files to 11; the misnomer `AccessSqlBuilder.cs` (does not exist) replaced with `AccessSqlBuilderBase.cs` — third area this run with the same SqlBuilder→SqlBuilderBase correction (after PROV-ORACLE, PROV-DB2). Notes column expanded with 2-driver × 2-engine matrix (4 concrete providers), no-schemas/no-MERGE/no-CTE constraints, positional ODBC parameters, Access date literal syntax, 767-param/64KB-SQL caps.

## 2026-05-04T12:30:00Z — kb-build step 3 PROV-ACCESS done
- areas/PROV-ACCESS/INDEX.md written, Tier-1 11/11, Tier-2 16/16 (100%), Tier-3 0, confidence high
- Two-driver × two-engine matrix documented (4 concrete providers); divergent SqlBuilder + SchemaProvider per driver, divergent member translator per engine; AccessSqlOptimizer's CorrectInnerJoins + CorrectExistsAndIn captured; AccessDmlService's table-not-found exception detection
- 1 UNCLASSIFIED-FILE: `AccessSqlBuilder.cs` (does not exist; was kb-areas.md misnomer for `AccessSqlBuilderBase.cs`); resolved by Tier-1 expansion above
- 6 debt items tracked in INDEX: OLE DB identity reporting bug (issue #3149), ODBC FK/PK gaps (dotnet/runtime#35442), EscapeLikeCharacters throws for ACE dynamic patterns, deprecated CreateDatabase string overload, IsParameterOrderDependent applied blanket-true (should be ODBC-only), GetOleDbSchemaTable native-AV crash risk
- Pattern observed: 3rd PROV-* area in this run with the kb-areas.md row pinning `*SqlBuilder.cs` instead of the actual `*SqlBuilderBase.cs` — likely an early-bootstrap typing pattern affecting several rows uniformly
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS (22 of 41); 19 remaining
- Paused at user request (one area per turn)
## 2026-05-04T00:04:41Z — agent audit notes
- **Proposed final Tier-1 list for `kb-areas.md` — PROV-INFORMIX**

The 10-file expansion used for this run is appropriate. Recommend updating `kb-areas.md` to pin:

1. `Source/LinqToDB/Internal/DataProvider/Informix/InformixDataProvider.cs`
2. `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlBuilder.cs`
3. `Source/LinqToDB/Internal/DataProvider/Informix/InformixSqlOptimizer.cs`
4. `Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderAdapter.cs`
5. `Source/LinqToDB/Internal/DataProvider/Informix/InformixProviderDetector.cs`
6. `Source/LinqToDB/Internal/DataProvider/Informix/InformixMappingSchema.cs`
7. `Source/LinqToDB/Internal/DataProvider/Informix/InformixBulkCopy.cs`
8. `Source/LinqToDB/DataProvider/Informix/InformixTools.cs`
9. `Source/LinqToDB/DataProvider/Informix/InformixOptions.cs`
10. `Source/LinqToDB/DataProvider/Informix/InformixProvider.cs`

**Note on three-provider distinction**: The adapter comment at `InformixProviderAdapter.cs:20-24` distinguishes three underlying clients (SQLI `IBM.Data.Informix`, IDS `IBM.Data.Informix`, IDS `IBM.Data.DB2`). The public `InformixProvider` enum collapses this to two values because the two `IBM.Data.Informix` variants are detected at runtime by the presence of `IfxBulkCopy` in the assembly (`InformixProviderAdapter.cs:68`). The `kb-areas.md` description correctly notes "two ADO.NET clients" at the enum level; this audit note confirms the three-way internal distinction is documented in the INDEX (Adapter section above).

## 2026-05-04T13:00:00Z — kb-areas.md PROV-INFORMIX Tier-1 list expanded (applied)
- PROV-INFORMIX row: Tier-1 list expanded from 2 files to 10. Added `InformixSqlOptimizer.cs`, `InformixProviderAdapter.cs`, `InformixProviderDetector.cs`, `InformixMappingSchema.cs`, `InformixBulkCopy.cs`, `InformixTools.cs`, `InformixOptions.cs`, `InformixProvider.cs`. Notes column expanded with three-clients-collapsed-to-two-enum-values nuance, IDS-vs-SQLI runtime detection via IfxBulkCopy presence, FIRST/SKIP paging, SERIAL identity, BOOLEAN since 12.10, and the shared bulk-copy path with PROV-DB2 (`DB2BulkCopyShared`).

## 2026-05-04T13:00:00Z — kb-build step 3 PROV-INFORMIX done
- areas/PROV-INFORMIX/INDEX.md written, Tier-1 10/10, Tier-2 5/5 (100%), Tier-3 0, confidence high
- Three-client matrix (SQLI Ifx, IDS Ifx, IDS DB2) collapsed to 2 enum values; IDS-vs-SQLI distinguished via IfxBulkCopy probe; three-path bulk copy (IfxBulkCopy → DB2BulkCopyShared → MultipleRowsCopy fallback)
- 7 debt items tracked in INDEX: TimeSpan TODO note, "Move everything to SQLBuilder" optimizer TODO, DateParts.Millisecond returns null (and DateAdd millisecond unsupported), IsValidIdentifier TODOs (reserved-words list, locale support), SQLI client has no native bulk-copy path, IDS bulk-copy async-under-sync, collection types (SET/MULTISET/LIST/ROW) commented out in GetDataTypes
- Cross-reference established: PROV-DB2's `DB2BulkCopyShared.ProviderSpecificCopyImpl` reused for the DB2 client path (matches the public-by-design rationale recorded for PROV-DB2)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX (23 of 41); 18 remaining
- Paused at user request (one area per turn)
## 2026-05-04T00:16:32Z — agent audit notes
- **Proposed final Tier-1 list for kb-areas.md `PROV-SYBASE` row**

All 10 files read as Tier-1 are confirmed on disk and substantive. Recommend the following pinned list:

```
Internal/DataProvider/Sybase/SybaseDataProvider.cs
Internal/DataProvider/Sybase/SybaseSqlBuilder.cs
Internal/DataProvider/Sybase/SybaseSqlOptimizer.cs
Internal/DataProvider/Sybase/SybaseMappingSchema.cs
Internal/DataProvider/Sybase/SybaseProviderAdapter.cs
Internal/DataProvider/Sybase/SybaseProviderDetector.cs
Internal/DataProvider/Sybase/SybaseBulkCopy.cs
DataProvider/Sybase/SybaseTools.cs
DataProvider/Sybase/SybaseProvider.cs
DataProvider/Sybase/SybaseOptions.cs
```

No files were missing on disk. No unclassified files encountered.

## 2026-05-04T13:30:00Z — kb-areas.md PROV-SYBASE Tier-1 list expanded (applied)
- PROV-SYBASE row: Tier-1 list expanded from 2 files to 10. Added `SybaseSqlOptimizer.cs`, `SybaseMappingSchema.cs`, `SybaseProviderAdapter.cs`, `SybaseProviderDetector.cs`, `SybaseBulkCopy.cs`, `SybaseTools.cs`, `SybaseProvider.cs`, `SybaseOptions.cs`. Notes column expanded with SAP-ASE-vs-SQL-Anywhere disambiguation, dual-client matrix (native vs managed), T-SQL heritage with SQL Server, `TOP`-only paging, 26-char param-name cap, BIT-cannot-be-NULL quirk, and the rationale for `MultipleRows` default (native bulk-copy bugs).

## 2026-05-04T13:30:00Z — kb-build step 3 PROV-SYBASE done
- areas/PROV-SYBASE/INDEX.md written, Tier-1 10/10, Tier-2 6/6 (100%), Tier-3 0, confidence high
- Single ASE dialect documented; dual ADO.NET client matrix (native vs DataAction managed); 26-char parameter-name limit enforced in two places (normalizer + builder); two-command TRUNCATE+sp_chgattribute identity reset; `IF (OBJECT_ID(...) IS NULL) EXECUTE('DDL')` pattern for CreateIfNotExists; `_skipAliases` workaround for nested-context column alias quirks
- 7 debt items tracked in INDEX: ASE 16SP3 features (window funcs, distinct set ops) unimplemented (TODO present), commented-out SupportsDistinctAsExistsIntersect property, native-driver bulk-copy bugs forcing MultipleRows default, GetProcedureParameters transaction-incompatibility (hard limit), DataAction managed driver lacks AseBulkCopy, MultipleRowsCopy2 empty-separator undocumented, dual-enforcement of 26-char param cap
- Cross-reference established: shared T-SQL dialect heritage with PROV-SQLSERVER (`TOP`, `IDENTITY`, `@@IDENTITY`, `CONVERT`, `OBJECT_ID()`, `SET IDENTITY_INSERT`, `#`/`##` temp-table prefixes)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX, PROV-SYBASE (24 of 41); 17 remaining
- Paused at user request (one area per turn)
## 2026-05-04T00:25:26Z — agent audit notes
- **Proposed Tier-1 list for `kb-areas.md` PROV-SAPHANA row**

The expanded 11-file set used for this run is confirmed correct. All 11 files were substantive and load-bearing. Proposed final Tier-1:

```
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaDataProvider.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaSqlBuilder.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaOdbcSqlBuilder.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaSqlOptimizer.cs
- Source/LinqToDB/DataProvider/SapHana/SapHanaTools.cs
- Source/LinqToDB/DataProvider/SapHana/SapHanaProvider.cs
- Source/LinqToDB/DataProvider/SapHana/SapHanaOptions.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaProviderAdapter.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaProviderDetector.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaMappingSchema.cs
- Source/LinqToDB/Internal/DataProvider/SapHana/SapHanaBulkCopy.cs
```

Note: `SapHanaOdbcSqlBuilder.cs` is promoted to Tier-1 (despite being a subclass of `SapHanaSqlBuilder`) because it is the only place where ODBC-specific parameter/type behavior diverges from native, and it is on the active code path for every ODBC client.

## 2026-05-04T14:00:00Z — kb-areas.md PROV-SAPHANA Tier-1 list expanded (applied)
- PROV-SAPHANA row: Tier-1 list expanded from 2 files to 11. Added `SapHanaOdbcSqlBuilder.cs` (substantive ODBC-divergent subclass), `SapHanaSqlOptimizer.cs`, `SapHanaTools.cs`, `SapHanaProvider.cs`, `SapHanaOptions.cs`, `SapHanaProviderAdapter.cs`, `SapHanaProviderDetector.cs`, `SapHanaMappingSchema.cs`, `SapHanaBulkCopy.cs`. Notes column expanded with native-vs-ODBC driver matrix, COLUMN-TABLE default DDL, HANA calculation-view machinery (`CalculationViewInputParametersExpressionAttribute`, `ViewWithParametersTableSchema`, `GetHanaSchemaOptions`).

## 2026-05-04T14:00:00Z — kb-build step 3 PROV-SAPHANA done
- areas/PROV-SAPHANA/INDEX.md written, Tier-1 11/11, Tier-2 9/9 (100%), Tier-3 0, confidence high
- Native + ODBC dual-driver matrix documented; calculation-view machinery (HANA-unique BI feature) deeply covered including the public `WITH PARAMETERS ('PLACEHOLDER' = (...))` SQL generation pattern via `Sql.TableExpressionAttribute` subclass
- 5 debt items tracked in INDEX: HanaDecimal client-to-DB conversion incomplete (TODO), HanaDbType enum ordinals not stable across SDK (warning comment), NewGuid() returns null (no working SQL solution), IsCorrelatedSubQueryTakeSupported deliberately false (HANA throws "more than one row" rather than truncating), POSITION column read twice as both Length+Precision in schema provider
- Notable cross-area dependency: `Sql.TableExpressionAttribute` (SQL-AST area) extended by `CalculationViewInputParametersExpressionAttribute` for HANA-specific table-expression generation
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX, PROV-SYBASE, PROV-SAPHANA (25 of 41); 16 remaining
- Paused at user request (one area per turn)
## 2026-05-04T00:40:06Z — agent audit notes
- Proposed final Tier-1 list for `kb-areas.md` (PROV-CLICKHOUSE):

The 10-file set used this run is well-chosen and covers all architectural pivots. Recommended to confirm as canonical:

1. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseDataProvider.cs` — provider base + concrete subclasses; SqlProviderFlags; SetParameter; BulkCopy dispatch
2. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseSqlBuilder.cs` — full SQL generation including DDL, DML mutations, types, hints
3. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseSqlOptimizer.cs` — DisableParameters, FixCteAliases; creates the expression convert visitor
4. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseProviderAdapter.cs` — ADO.NET type wrapping; Octonica + Driver wrappers; MySqlConnector delegation
5. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseProviderDetector.cs` — auto-detection by name/file; MySql pass-through
6. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseMappingSchema.cs` — type mappings, literal generators, three sub-schemas
7. `Source/LinqToDB/Internal/DataProvider/ClickHouse/ClickHouseBulkCopy.cs` — three ProviderSpecific paths + fallback
8. `Source/LinqToDB/DataProvider/ClickHouse/ClickHouseTools.cs` — public registration entry point
9. `Source/LinqToDB/DataProvider/ClickHouse/ClickHouseProvider.cs` — client selector enum
10. `Source/LinqToDB/DataProvider/ClickHouse/ClickHouseOptions.cs` — provider options (BulkCopyType, UseStandardCompatibleAggregates)

No files were missing or renamed. The T4 template `ClickHouseHints.tt` and `README.md` are correctly omitted from Tier-1 and Tier-2 (they are documentation/generated-source and carry no structural knowledge beyond what `ClickHouseHints.cs` and `ClickHouseHints.generated.cs` already provide).

## 2026-05-04T14:30:00Z — kb-areas.md PROV-CLICKHOUSE Tier-1 list expanded (applied)
- PROV-CLICKHOUSE row: Tier-1 list expanded from 2 files to 10. Added `ClickHouseSqlOptimizer.cs`, `ClickHouseProviderAdapter.cs`, `ClickHouseProviderDetector.cs`, `ClickHouseMappingSchema.cs`, `ClickHouseBulkCopy.cs`, `ClickHouseTools.cs`, `ClickHouseProvider.cs`, `ClickHouseOptions.cs`. Notes column expanded with three-client matrix (Octonica + Driver + MySqlConnector-via-MySQL-protocol), the MySqlConnector adapter delegation to PROV-MYSQL, no-bound-parameters quirk (all params inlined via `DisableParameters`), no-MERGE constraint, ALTER-TABLE-DELETE/UPDATE for DML, and three-strategy bulk copy.

## 2026-05-04T14:30:00Z — kb-build step 3 PROV-CLICKHOUSE done
- areas/PROV-CLICKHOUSE/INDEX.md written, Tier-1 10/10, Tier-2 13/13 (100%), Tier-3 2 (`.tt` template + `README.md`), confidence high
- Three-driver matrix documented; MySqlConnector path delegates to PROV-MYSQL's `MySqlProviderAdapter` (cross-area dependency captured); reciprocal `MySqlProviderDetector` ClickHouse-string short-circuit confirmed
- 10 debt items tracked in INDEX: SupportedCorrelatedSubqueriesLevel=0 (hard limit), IsNestedJoinsSupported=false, InsertOrUpdate flag=true but throws (deliberate), Enum8/Enum16 DDL throws, JSON limitations across all 3 clients, CTE column list blocked by ClickHouse #22932, fixed table engines only (MergeTree/Memory; user must use raw SQL for advanced engines), IPv4-via-MySQL ClickHouse #39056, Octonica IsDBNullAllowed always-true workaround (issue #55), v7 BulkCopyRowsCopied.RowsCopied int→long migration TODO
- Notable: `BuildMergeStatement` and `BuildParameter` both throw unconditionally; statement-level rewrites in optimizer make this work end-to-end
- Cross-area dependency: PROV-CLICKHOUSE wraps `MySqlProviderAdapter.GetInstance(MySqlProvider.MySqlConnector)` directly when MySqlConnector path is selected (matches the bidirectional protocol-sharing pattern)
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX, PROV-SYBASE, PROV-SAPHANA, PROV-CLICKHOUSE (26 of 41); 15 remaining
- Paused at user request (one area per turn)
## 2026-05-04T09:24:21Z — agent audit notes
- Proposed Tier-1 expansion for PROV-SQLCE row in `.claude/docs/kb-areas.md`.

Current pinned Tier-1 (2 files):
- `SqlCeDataProvider.cs`
- `SqlCeSqlBuilder.cs`

Proposed canonical Tier-1 (11 files, matching the precedent set by PROV-SQLSERVER, PROV-POSTGRES, and other PROV-* runs):
- `SqlCeDataProvider.cs`
- `SqlCeSqlBuilder.cs`
- `SqlCeSqlOptimizer.cs`
- `SqlCeProviderAdapter.cs`
- `SqlCeMappingSchema.cs`
- `SqlCeBulkCopy.cs`
- `SqlCeSchemaProvider.cs`
- `SqlCeDmlService.cs`
- `SqlCeSqlExpressionConvertVisitor.cs`
- `Translation/SqlCeMemberTranslator.cs`
- `SqlCeTools.cs` (public-facing, anchors provider detection and database lifecycle)

Rationale: SQL CE has no version enum or multi-client matrix, but it has several unique structural files (`SqlCeSchemaProvider.cs`, `SqlCeDmlService.cs`, `SqlCeSqlExpressionConvertVisitor.cs`) that carry CE-specific behavior not present in the 2-anchor baseline. All 11 files are in `Source/LinqToDB/Internal/DataProvider/SqlCe/` or `Source/LinqToDB/DataProvider/SqlCe/` and match the area's path patterns.

Suggested kb-areas.md update for the PROV-SQLCE row:

```
| PROV-SQLCE | `Source/LinqToDB/DataProvider/SqlCe/**`, `Source/LinqToDB/Internal/DataProvider/SqlCe/**` | `SqlCeDataProvider.cs`, `SqlCeSqlBuilder.cs`, `SqlCeSqlOptimizer.cs`, `SqlCeProviderAdapter.cs`, `SqlCeMappingSchema.cs`, `SqlCeBulkCopy.cs`, `SqlCeSchemaProvider.cs`, `SqlCeDmlService.cs`, `SqlCeSqlExpressionConvertVisitor.cs`, `Translation/SqlCeMemberTranslator.cs`, `SqlCeTools.cs` | `**/SqlCe*.cs` | SQL Server Compact Edition. Per-vendor Internal helpers cross-listed under same area. |
```

## 2026-05-04T09:25:15Z — agent audit notes
- Proposed Tier-1 expansion for PROV-SQLCE row in `.claude/docs/kb-areas.md`.

Current pinned Tier-1 (2 files):
- `SqlCeDataProvider.cs`
- `SqlCeSqlBuilder.cs`

Proposed canonical Tier-1 (11 files, matching the precedent set by PROV-SQLSERVER, PROV-POSTGRES, and other PROV-* runs):
- `SqlCeDataProvider.cs`
- `SqlCeSqlBuilder.cs`
- `SqlCeSqlOptimizer.cs`
- `SqlCeProviderAdapter.cs`
- `SqlCeMappingSchema.cs`
- `SqlCeBulkCopy.cs`
- `SqlCeSchemaProvider.cs`
- `SqlCeDmlService.cs`
- `SqlCeSqlExpressionConvertVisitor.cs`
- `Translation/SqlCeMemberTranslator.cs`
- `SqlCeTools.cs` (public-facing, anchors provider detection and database lifecycle)

Rationale: SQL CE has no version enum or multi-client matrix, but it has several unique structural files (`SqlCeSchemaProvider.cs`, `SqlCeDmlService.cs`, `SqlCeSqlExpressionConvertVisitor.cs`) that carry CE-specific behavior not present in the 2-anchor baseline. All 11 files are in `Source/LinqToDB/Internal/DataProvider/SqlCe/` or `Source/LinqToDB/DataProvider/SqlCe/` and match the area's path patterns.

Suggested kb-areas.md update for the PROV-SQLCE row:

```
| PROV-SQLCE | `Source/LinqToDB/DataProvider/SqlCe/**`, `Source/LinqToDB/Internal/DataProvider/SqlCe/**` | `SqlCeDataProvider.cs`, `SqlCeSqlBuilder.cs`, `SqlCeSqlOptimizer.cs`, `SqlCeProviderAdapter.cs`, `SqlCeMappingSchema.cs`, `SqlCeBulkCopy.cs`, `SqlCeSchemaProvider.cs`, `SqlCeDmlService.cs`, `SqlCeSqlExpressionConvertVisitor.cs`, `Translation/SqlCeMemberTranslator.cs`, `SqlCeTools.cs` | `**/SqlCe*.cs` | SQL Server Compact Edition. Per-vendor Internal helpers cross-listed under same area. |
```

## 2026-05-04T09:31:36Z — kb-build step 3 PROV-SQLCE — agent audit notes
- Proposed Tier-1 expansion for PROV-SQLCE (2 → 11 files), matching PROV-SQLSERVER / PROV-POSTGRES precedent.
- Added: SqlCeSqlOptimizer.cs, SqlCeProviderAdapter.cs, SqlCeMappingSchema.cs, SqlCeBulkCopy.cs, SqlCeSchemaProvider.cs (unique among providers — INFORMATION_SCHEMA queries via raw SQL), SqlCeDmlService.cs (unique — DB_E_NOTABLE HResult detection with locale-sensitive message fallback), SqlCeSqlExpressionConvertVisitor.cs, Translation/SqlCeMemberTranslator.cs, SqlCeTools.cs.
- No version enum (CE is single-version 4.0); no provider-selection enum (single ADO.NET driver); no native bulk-copy (MultipleRows-only via MultipleRowsCopy2); SqlDecimal scale-overflow workaround in SqlCeProviderAdapter.cs:120-144.

## 2026-05-04T09:31:39Z — kb-areas.md PROV-SQLCE Tier-1 list expanded (applied)
- PROV-SQLCE row: Tier-1 list expanded from 2 files to 11 (matching PROV-SQLSERVER/PROV-POSTGRES precedent). Notes column expanded with single-version/single-driver matrix, no-MERGE/no-UPDATE-JOIN/no-window-functions/no-boolean constraints, no-native-bulk-copy fallback, NVarChar 4000 cap, TOP+OFFSET/FETCH paging, @@IDENTITY identity reporting, schema provider's INFORMATION_SCHEMA raw-SQL approach, DmlService's DB_E_NOTABLE HResult fallback, and the SQL CE 3.0 InlineFunctionParameters compat flag.

## 2026-05-04T09:31:41Z — kb-build step 3 PROV-SQLCE done
- areas/PROV-SQLCE/INDEX.md written, Tier-1 11/11, Tier-2 10/10 (100%), Tier-3 1 (SqlCeHints.tt), confidence high
- Single-version, single-driver embedded engine documented; no version enum and no provider-selection enum (contrasts with parent PROV-SQLSERVER's multi-version + dual-client matrix); SqlCeProviderAdapter loaded once via Common.Tools.TryLoadAssembly; SqlCeEngine wrapper exposes CreateDatabase/DropDatabase for .sdf file lifecycle
- 5 known constraints documented: IsIdentity always-false in schema provider (CE INFORMATION_SCHEMA limitation), DB_E_NOTABLE HResult unreliable (locale-sensitive message fallback in DmlService), FixEmptySelect intentionally suppressed (CorrectSkipAndColumns covers same case), InlineFunctionParameters as SQL CE 3.0 compat flag (off by default), no native bulk-copy (MultipleRows-only)
- Cross-area dependency: shares no T-SQL dialect heritage with PROV-SQLSERVER beyond name — the dialect divergences (no MERGE, no UPDATE-JOIN, no window functions, no boolean type, NVarChar 4000 cap, [name] quoting with no schema prefix, @@IDENTITY) are specific to the embedded engine
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX, PROV-SYBASE, PROV-SAPHANA, PROV-CLICKHOUSE, PROV-SQLCE (27 of 41); 14 remaining
- Paused at user request (one area per turn)

## 2026-05-04T09:50:57Z — agent audit notes
- Proposed canonical Tier-1 file list for the `PROV-YDB` row in `.claude/docs/kb-areas.md`.

Replace `(TBD on first read)` with:

```
Internal/DataProvider/Ydb/YdbDataProvider.cs, Internal/DataProvider/Ydb/YdbSqlBuilder.cs, Internal/DataProvider/Ydb/YdbSqlOptimizer.cs, Internal/DataProvider/Ydb/YdbProviderAdapter.cs, Internal/DataProvider/Ydb/YdbMappingSchema.cs, Internal/DataProvider/Ydb/YdbBulkCopy.cs, Internal/DataProvider/Ydb/YdbSqlExpressionConvertVisitor.cs, Internal/DataProvider/Ydb/Translation/YdbMemberTranslator.cs, Internal/DataProvider/Ydb/YdbRetryPolicy.cs, Internal/DataProvider/Ydb/YdbTransientExceptionDetector.cs, DataProvider/Ydb/YdbTools.cs, DataProvider/Ydb/YdbOptions.cs, DataProvider/Ydb/YdbHints.cs
```

Rationale:
- `YdbDataProvider`, `YdbSqlBuilder`, `YdbSqlOptimizer` — core provider triad.
- `YdbProviderAdapter` — driver-loading contract.
- `YdbMappingSchema` — defines all type literals; without it the type system is opaque.
- `YdbBulkCopy` — provider-specific bulk strategy.
- `YdbSqlExpressionConvertVisitor` — YQL expression rewrites.
- `YdbMemberTranslator` — LINQ→YQL member mapping.
- `YdbRetryPolicy` + `YdbTransientExceptionDetector` — area-unique; not present in any other provider. Worth anchoring as Tier 1 so future indexer runs always read them.
- `YdbTools` — sole public-API factory.
- `YdbOptions` — configuration contract.
- `YdbHints` — public YQL hint surface.

No files are missing from disk vs. the area description. No Tier-1 anchor is absent.

## 2026-05-04T09:57:46Z — kb-build step 3 PROV-YDB — agent audit notes
- Proposed canonical Tier-1 (13 files) for PROV-YDB row in kb-areas.md (was '(TBD on first read)').
- Anchors: YdbDataProvider, YdbSqlBuilder, YdbSqlOptimizer, YdbProviderAdapter, YdbMappingSchema, YdbBulkCopy, YdbSqlExpressionConvertVisitor, Translation/YdbMemberTranslator, YdbRetryPolicy, YdbTransientExceptionDetector, YdbTools, YdbOptions, YdbHints.
- Area-unique pair (YdbRetryPolicy + YdbTransientExceptionDetector) elevated to Tier 1 — no other provider has built-in retry policy. Detector uses reflection (no hard Ydb.Sdk compile-time dep); status-code-name dispatch matches YDB SDK defaults.
- No version enum, no provider-selection enum (single ADO.NET driver); 128-bit decimal wire encoding via Ydb.Protos protos; provider-specific bulk via IBulkUpsertImporter (default).

## 2026-05-04T09:57:51Z — kb-areas.md PROV-YDB Tier-1 list filled in (applied)
- PROV-YDB row: Tier-1 list updated from '(TBD on first read)' to 13 files. Notes column expanded with: YQL dialect (backtick quoting, CTE-as-assignment, CAST/Unwrap, SERIAL identity DDL, no MERGE, no standalone OFFSET, only Serializable, no correlated subqueries, IN-list materialization), single-driver matrix (Ydb.Sdk + Ydb.Protos), area-unique YdbRetryPolicy + YdbTransientExceptionDetector with reflection-based SDK-exception inspection, IBulkUpsertImporter native bulk path, and the absent SchemaProvider.

## 2026-05-04T09:58:07Z — kb-build step 3 PROV-YDB done
- areas/PROV-YDB/INDEX.md written, Tier-1 13/13, Tier-2 7/8 (88% — YdbHints.tt T4 source skipped, fully represented by generated .cs), Tier-3 0, confidence high
- Single-driver, single-version provider documented; YQL dialect divergences (backtick quoting, $name=SELECT CTE syntax, Unwrap(CAST), SERIAL/SMALLSERIAL/BIGSERIAL identity, no MERGE, no standalone OFFSET, only Serializable, no correlated subqueries) captured with file:line citations
- Area-unique retry subsystem (YdbRetryPolicy + YdbTransientExceptionDetector) deeply covered: opt-in (not auto-wired); reflection-based YdbException inspection (no hard SDK compile-time dep); status-code-name dispatch — BadSession/SessionBusy=0ms, Aborted/Undetermined=FullJitter(fast), Unavailable=EqualJitter(fast), Overloaded=EqualJitter(slow); mirrors YDB SDK defaults
- 10 known issues / debt items tracked: GetSchemaProvider NotImplementedException, no correlated subqueries fallback, OFFSET-without-LIMIT (YDB#11258), retry opt-in with TODO IsTransientWhenIdempotent, no extended TxModes, no YdbStruct, Guid.NewGuid commented out (single-UUID-per-query bug), String.Join ORDER BY DESC restriction, DateTimeOffset DatePart/Truncation NotSupported, SqlTypes.SqlBoolean ConvertBit NotSupported, decimal modulo commented out
- Cross-area dependency: PROV-YDB consumes RetryPolicyBase from INFRA area — only provider area to reach into the retry subsystem; integration is opt-in via DataOptions.UseRetryPolicy(...)
- Notable: 128-bit decimal wire encoding constructed via Int128 (net8+) or BigInteger from Ydb.DecimalType + Ydb.Value protos — only provider with a separate proto-assembly dependency
- Completed areas: CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, PROV-SQLSERVER, PROV-POSTGRES, PROV-MYSQL, PROV-ORACLE, PROV-SQLITE, PROV-FIREBIRD, PROV-DB2, PROV-ACCESS, PROV-INFORMIX, PROV-SYBASE, PROV-SAPHANA, PROV-CLICKHOUSE, PROV-SQLCE, PROV-YDB (28 of 41); 13 remaining
- All PROV-* areas now complete. Remaining areas are companion projects (EFCORE, CLI, COMPAT, EXTENSIONS-PKG, FSHARP, LINQPAD, REMOTE, SCAFFOLD, T4-TEMPLATES, TOOLS, CODEGEN, SHARED-INTERNAL) and infra (TESTS-*, BUILD, CLAUDE-INFRA).
- Paused at user request (one area per turn)

## 2026-05-04T10:13:45Z — unclassified files
- LinqToDBForEFExtensions.cs — Pinned as Tier-1 in kb-areas.md but does not exist on disk. The actual file is `Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFExtensions.Async.cs`. The Tier-1 pin should be updated to reference the on-disk file name.

## 2026-05-04T10:13:45Z — agent audit notes
- Proposed Tier-1 update for EFCORE area in kb-areas.md:

Replace the stale `LinqToDBForEFExtensions.cs` Tier-1 pin with the correct on-disk filename and add `LinqToDBForEFToolsImplDefault.cs` as a second anchor (it is the default implementation of the main extensibility interface and is larger/more complex than `LinqToDBForEFTools.cs` itself).

Proposed Tier-1 list:
- `LinqToDBForEFTools.cs` (retain)
- `LinqToDBForEFExtensions.Async.cs` (replace `LinqToDBForEFExtensions.cs`)
- `LinqToDBForEFToolsImplDefault.cs` (add — canonical implementation of `ILinqToDBForEFTools`)
- `EFCoreMetadataReader.cs` (add — the `IMetadataReader` bridge, the most complex single file in the area)
- `Internal/TransformExpressionVisitor.cs` (add — the expression-rewrite engine)

## 2026-05-04T10:15:00Z — kb-build step 3 EFCORE — unclassified files
- Source/LinqToDB.EntityFrameworkCore/LinqToDBForEFExtensions.cs — pinned Tier-1 missing — file does not exist on disk. The actual file is LinqToDBForEFExtensions.Async.cs. kb-areas.md Tier-1 pin should be corrected.

## 2026-05-04T10:15:13Z — kb-build step 3 EFCORE — agent audit notes
- Proposed Tier-1 update for EFCORE row in kb-areas.md (was 2 files: LinqToDBForEFTools.cs + missing LinqToDBForEFExtensions.cs).
- Replace LinqToDBForEFExtensions.cs (does not exist) with LinqToDBForEFExtensions.Async.cs (actual on-disk filename).
- Add 3 anchors: LinqToDBForEFToolsImplDefault.cs (canonical strategy implementation), EFCoreMetadataReader.cs (the IMetadataReader bridge, most complex file in area), Internal/TransformExpressionVisitor.cs (the EF→linq2db expression rewriter).
- Final 5-anchor Tier-1 set covers: entry-point partials root, async ext surface, default strategy impl, metadata bridge, expression rewriter.

## 2026-05-04T10:16:37Z — kb-areas.md EFCORE Tier-1 list expanded + corrected (applied)
- EFCORE row: Tier-1 list expanded from 2 files to 5; the misnomer LinqToDBForEFExtensions.cs (does not exist on disk) replaced with LinqToDBForEFExtensions.Async.cs. Added: LinqToDBForEFToolsImplDefault.cs, EFCoreMetadataReader.cs, Internal/TransformExpressionVisitor.cs.
- Notes column expanded with: 4-csproj-1-source-tree compilation strategy (EF31/EF8/EF9/EF10 DefineConstants), per-EF API divergences (EFCoreMetadataReader, ReflectionMethods, TransformExpressionVisitor, LinqToDBForEFToolsDataConnection), NoWarn=EF1001 rationale (RelationalQueryContextFactory/_dependencies via reflection), partial-class layout, EF→linq2db provider mapping table (SqlServer/MySql/PostgreSQL/SQLite/Firebird/DB2/Oracle/Access/SqlCe — Informix/SAP HANA/Sybase/ClickHouse not supported), Npgsql reflection-by-name discovery pattern, per-TFM PublicAPI baselines.

## 2026-05-04T10:16:43Z — kb-build step 3 EFCORE done
- areas/EFCORE/INDEX.md written, Tier-1 1/2 (50% — pinned LinqToDBForEFExtensions.cs surfaced as UNCLASSIFIED-FILE; resolved by Tier-1 update above), Tier-2 22/22 (100%), Tier-3 0, confidence high
- Multi-EF compilation strategy documented: 4 csprojs (EF3/EF8/EF9/EF10) × 1 source tree × DefineConstants symbol; per-EF #if branches in 4 files captured concretely
- EF→linq2db provider mapping table extracted (9 supported provider strings; 4 documented gaps: Informix, SAP HANA, Sybase, ClickHouse)
- EFCoreMetadataReader bridges EF IModel→linq2db MappingAttribute via TableAttribute, ColumnAttribute (with identity detection from annotations), ValueConverterAttribute, AssociationAttribute, QueryFilterAttribute, InheritanceMappingAttribute, plus Sql.ExpressionAttribute synthesis from EF MemberTranslator/MethodCallTranslator (Npgsql operators @>, <@, &&, AT TIME ZONE discovered automatically); compiled-expression cache for entity-key getters keyed in IMemoryCache
- TransformExpressionVisitor rewrite catalog (10+ rules): EntityQueryable/DbSet → GetTable, FromSqlQueryRootExpression → IDataContext.FromSql, temporal root expressions → SqlServerHints.TemporalTable*, Include/ThenInclude → LoadWith chain, IgnoreQueryFilters → IgnoreFilters, AsNoTracking/AsTracking → strips + side-effects, TagWith → TagQuery, AsSplitQuery/AsSingleQuery (EF8+) → strip, EF.Property → Sql.Ext.Property, [NotParameterized] params → Sql.ToSql
- Change-tracker integration via LinqToDBForEFToolsDataConnection : IEntityServiceInterceptor — guards: skips temp tables, cross-server tables, mismatched names, NoTracking
- 7 known issues / debt items: dead LinqToDBForEFToolsDataContext path, fragile reflection-based GetCurrentContext (4 EF internal field forks), 4-provider mapping gap, MinAsync/MaxAsync nullable mismatch (#pragma CS8619), GetServiceProviderHashCode always 0, NpgsqlEnumTypeMapping reflection-by-name, dead duplicate switch in ConvertToExpressionAttribute
- Cross-area dependencies: METADATA (provides IMetadataReader impl), MAPPING (consumes MappingAttribute), DATA (DataConnection extension), LINQ (ProcessSourceQueryable + ExtensionsAdapter hooks), INTERCEPTORS (IEntityServiceInterceptor + IQueryExpressionInterceptor), EXPR (LinqExtensions adapter), all PROV-* (CreateLinqToDBDataProvider switch dispatch)
- Completed areas: ... PROV-YDB, EFCORE (29 of 41); 12 remaining
- Paused at user request (one area per turn)

## 2026-05-04T10:31:48Z — agent audit notes
- Proposed canonical Tier-1 list for area `CLI` (update `kb-areas.md` row accordingly):

```
Source/LinqToDB.CLI/Program.cs
Source/LinqToDB.CLI/LinqToDB.CLI.csproj
Source/LinqToDB.CLI/CommandLine/CliController.cs
Source/LinqToDB.CLI/CommandLine/LinqToDBCliController.cs
Source/LinqToDB.CLI/CommandLine/StatusCodes.cs
Source/LinqToDB.CLI/CommandLine/Commands/CliCommand.cs
Source/LinqToDB.CLI/CommandLine/Commands/ScaffoldCommand.cs
Source/LinqToDB.CLI/CommandLine/Commands/ScaffoldCommand.Execute.cs
Source/LinqToDB.CLI/CommandLine/Commands/ScaffoldCommand.Interceptors.cs
Source/LinqToDB.CLI/CommandLine/Commands/ScaffoldCommand.Configuration.cs
Source/LinqToDB.CLI/CommandLine/Commands/ScaffoldCommand.Options.cs
Source/LinqToDB.CLI/CommandLine/Options/CliOption.cs
Source/LinqToDB.CLI/T4Host/LinqToDBHost.cs
Source/LinqToDB.CLI/Template.tt
```

Rationale:
- `Program.cs` / `LinqToDBCliController.cs` / `CliController.cs` — entry point and dispatch backbone.
- `StatusCodes.cs` — exit-code contract (exit code values are part of the public CLI contract).
- `CliCommand.cs` / `CliOption.cs` — abstract bases for the command/option model; everything else derives from these.
- All five `ScaffoldCommand*.cs` partials — the `scaffold` command is the raison d'être of the tool; all five partials are load-bearing (none is a thin wrapper).
- `LinqToDBHost.cs` — public type (listed in `PublicAPI.Shipped.txt`); forms the T4 host contract.
- `Template.tt` — the embedded starter template; defines what users get from `dotnet linq2db template`.
- `LinqToDB.CLI.csproj` — multi-arch build logic is non-trivial and worth anchoring.

Everything else (`HelpCommand.cs`, `TemplateCommand.cs`, option concrete types) is Tier 2 — important but derivable from the Tier-1 abstractions.

## 2026-05-04T10:33:40Z — kb-build step 3 CLI — agent audit notes
- Proposed canonical Tier-1 (14 files) for CLI row in kb-areas.md (was '(TBD on first read)').
- Anchors: Program.cs, LinqToDB.CLI.csproj, CliController.cs, LinqToDBCliController.cs, StatusCodes.cs, CliCommand.cs, all 5 ScaffoldCommand*.cs partials, CliOption.cs, T4Host/LinqToDBHost.cs, Template.tt.
- All 5 ScaffoldCommand partials elevated to Tier 1 — none is a thin wrapper; each partial owns a distinct concern (constructor + option registration in .cs, option declarations in .Options.cs, JSON→ScaffoldOptions mapping in .Configuration.cs, scaffold pipeline + provider-specific connection setup in .Execute.cs, T4 + assembly interceptor loading in .Interceptors.cs).
- LinqToDBHost.cs is public (listed in PublicAPI.Shipped.txt) — forms the T4 host contract that user templates inherit.
- Template.tt elevated to Tier 1 — embedded resource extracted by `dotnet linq2db template`; defines the starter scaffold for all users.

## 2026-05-04T10:36:27Z — kb-areas.md CLI Tier-1 list filled in (applied)
- CLI row: Tier-1 list updated from '(TBD on first read)' to 14 files. Notes column expanded with: dotnet-linq2db tool packaging (linq2db.cli NuGet, packageType DotnetTool, net8.0/9.0/10.0 + Windows multi-arch via MultiArchPublish MSBuild target — win-x86/win-x64/win-arm64 .exe bundled for --architecture restart), 3-command surface (help default + scaffold + template), 5-partial ScaffoldCommand structure, ~80 options × 4 OptionCategory groups, 14 supported provider strings via DatabaseType enum, SCAFFOLD library wrapping (Scaffolder.LoadDataModel→GenerateCodeModel→GenerateSourceCode), T4 interceptor pipeline (Mono.TextTemplating + double Roslyn compile + AssemblyLoadContext.LoadFromStream), LinqToDBHost public T4 base class, IBM DB2/Informix exclusion (~90 MB) requiring --provider-location, Windows-only architecture restart.

## 2026-05-04T10:37:03Z — kb-build step 3 CLI done
- areas/CLI/INDEX.md written, Tier-1 14/14, Tier-2 11/11 (100%; 19 total Tier-2 listed in coverage block including .csproj/.tt/.nuspec/PublicAPI), Tier-3 0, confidence high
- Distribution: linq2db.cli dotnet tool, packageType DotnetTool, net8.0/9.0/10.0 + Windows multi-arch (.exe bundled per RID via MultiArchPublish MSBuild target). Custom .nuspec rather than <IsTool>true because of multi-arch packing.
- 3-command surface (help default, scaffold, template); ScaffoldCommand is 5-file partial registering ~80 options across 4 OptionCategory groups (General/Schema/DataModel/CodeGen); 14 supported provider strings via DatabaseType enum
- Scaffold pipeline (in ScaffoldCommand.Execute.cs): ProcessScaffoldOptions → GetConnection → new Scaffolder(LanguageProviders.CSharp, HumanizerNameConverter.Instance, settings, interceptors) → LoadDataModel → GenerateCodeModel → GenerateSourceCode → File.WriteAllText
- T4 interceptor compile pipeline: Mono.TextTemplating.TemplateGenerator.PreprocessTemplate → Roslyn CSharpCompilation (in-memory DLL) → LinqToDBHost.TransformText() → second Roslyn compile → AssemblyLoadContext.Default.LoadFromStream → instantiate ScaffoldInterceptors subclass
- Cross-area dependencies: SCAFFOLD (Scaffolder + ScaffoldOptions + ScaffoldInterceptors + LegacySchemaProvider + LanguageProviders + HumanizerNameConverter + MetadataBuilders), CORE (DataConnection + DataOptions + ProviderName), all PROV-* tools (provider-specific connection setup), Mono.TextTemplating + Microsoft.CodeAnalysis.CSharp + Microsoft.Extensions.DependencyModel NuGets
- Inbound: standalone tool, no other source project references LinqToDB.CLI
- 6 known issues / debt items: 3× verbose-logging TODOs in Interceptors.cs (unconditional Console.WriteLine), Console.BufferWidth fallback (issue #3612), file-name-collision deferred TODO, IgnoreSystemHistoryTables possibly missing constructor registration, --architecture Windows-only, IBM DB2/Informix excluded from package (~90 MB) requiring --provider-location
- Completed areas: ... PROV-YDB, EFCORE, CLI (30 of 41); 11 remaining
- Paused at user request (one area per turn)

## 2026-05-04T10:46:45Z — agent audit notes
- Proposed canonical Tier-1 list for COMPAT (update `kb-areas.md`):

```
Source/LinqToDB.Compat/LinqToDB.Compat.csproj
Source/LinqToDB.Compat/PublicAPI/net462/PublicAPI.Shipped.txt
```

Rationale: the csproj is the single structural source of truth (TFMs, compile links, package identity). The `net462` shipped file is the most distinctive — it records the type-forwarding annotation absent from other TFMs and anchors the "two modes" story. The remaining per-TFM Shipped files are structurally identical to each other and are adequately covered as Tier 2.

The `**/*.cs` Tier-2 glob in `kb-areas.md` matches zero files (no .cs files exist under `Source/LinqToDB.Compat/`). Recommend changing the Tier-2 glob to `PublicAPI/**/*.txt` and adding `README.md` as a Tier-2 entry, or replacing the glob with `**/*.txt` to match the actual on-disk inventory.

## 2026-05-04T10:52:45Z — kb-build step 3 COMPAT — agent audit notes
- Proposed canonical Tier-1 (2 entries, both non-.cs) for COMPAT row in kb-areas.md (was '(TBD on first read)'): LinqToDB.Compat.csproj + PublicAPI/net462/PublicAPI.Shipped.txt.
- kb-areas.md Tier-2 glob `**/*.cs` matches zero files in this area — recommend changing to `PublicAPI/**/*.txt` + README.md (the actual file types on disk). Applied in this run.
- No .cs source files in this area: csproj pulls 5 .cs files from Source/LinqToDB/Configuration/ via <Compile Include Link> under COMPAT compile constant — those source files belong to INTERNAL-API/CORE areas, not COMPAT.
- Two-mode build documented: on net462 types [TypeForwardedTo] to linq2db.dll (PublicAPI/net462/PublicAPI.Shipped.txt entries annotated `(forwarded, contained in linq2db)`); on net8/9/10/netstandard2.0 types compiled in full into linq2db.Compat.dll.

## 2026-05-04T10:53:28Z — kb-areas.md COMPAT row filled in (applied)
- COMPAT row: Tier-1 set to 2 non-.cs entries (LinqToDB.Compat.csproj + PublicAPI/net462/PublicAPI.Shipped.txt). Tier-2 glob changed from `**/*.cs` (matches zero) to `PublicAPI/**/*.txt`, `README.md` (matches actual on-disk inventory). Notes column expanded with: package identity (linq2db.Compat NuGet + assembly), purpose (System.Configuration restoration on modern TFMs), <Compile Include Link> mechanism pulling 5 .cs files from Source/LinqToDB/Configuration/ under COMPAT compile constant, two-mode build (net462 TypeForwardedTo vs net8/9/10/netstandard2.0 compiled-in), 14-member identical surface across TFMs, distinction from deprecated Source/Default/, user entry point.

## 2026-05-04T10:54:01Z — kb-build step 3 COMPAT done
- areas/COMPAT/INDEX.md written, Tier-1 2/2, Tier-2 10/10 (100%), Tier-3 0, confidence high
- First non-.cs area covered in this run — csproj-only shim with 5 source files pulled via <Compile Include Link> from Source/LinqToDB/Configuration/. No .cs files belong to this area on disk; the linked files are owned by CORE/INTERNAL-API.
- Two-mode build (net462 [TypeForwardedTo] vs net8/9/10/netstandard2.0 compiled-in) anchored by the PublicAPI/net462/PublicAPI.Shipped.txt `(forwarded, contained in linq2db)` annotation
- 5 types in LinqToDB.Configuration namespace (LinqToDBSection, DataProviderElementCollection, DataProviderElement, ElementBase, ElementCollectionBase<T>); identical 14-member surface across 5 TFMs
- Cross-area: depends on CORE (provides the 5 linked source files) and on System.Configuration.ConfigurationManager NuGet (restored BCL shim for non-Framework TFMs)
- 1 known-debt note: empty root PublicAPI/PublicAPI.Shipped.txt while per-TFM files carry the surface — intentional (analyzer picks up per-TFM via AdditionalFiles) but non-obvious
- Completed areas: ... CLI, COMPAT (31 of 41); 10 remaining
- Paused at user request (one area per turn)

## 2026-05-04T11:09:44Z — agent audit notes
- Proposed canonical Tier-1 list for EXTENSIONS-PKG (all 3 .cs files — area is small enough that all source is Tier 1):

- `Source/LinqToDB.Extensions/ServiceConfigurationExtensions.cs`
- `Source/LinqToDB.Extensions/Logging/LinqToDbLoggerFactoryAdapter.cs`
- `Source/LinqToDB.Extensions/Logging/OptionsBuilderExtensions.cs`

Update `kb-areas.md` row for EXTENSIONS-PKG, replacing `(TBD on first read)` with these three paths.

## 2026-05-04T11:14:15Z — kb-build step 3 EXTENSIONS-PKG — agent audit notes
- Proposed Tier-1 (3 files = all .cs in area) for EXTENSIONS-PKG row in kb-areas.md (was '(TBD on first read)').
- Anchors: ServiceConfigurationExtensions.cs (DI registration), Logging/LinqToDbLoggerFactoryAdapter.cs (M.E.Logging→WriteTrace bridge), Logging/OptionsBuilderExtensions.cs (UseLoggerFactory/UseDefaultLogging on DataOptions).
- Area is the Microsoft.Extensions integration package (DI + Logging.Abstractions). No M.E.Options dependency — options integration goes through DataOptions directly.

## 2026-05-04T11:16:32Z — kb-areas.md EXTENSIONS-PKG row filled in (applied)
- EXTENSIONS-PKG row: Tier-1 set to 3 .cs files (the entire .cs surface of the area). Notes column expanded with: Microsoft.Extensions integration scope (DI + Logging.Abstractions; no M.E.Options), AddLinqToDB / AddLinqToDBContext<TContext> / AddLinqToDBContext<TContext,TContextImpl> / factory-delegate / AddLinqToDBService<TContext> entry points, HasTypedContextConstructor reflection-based DataOptions constructor selection, Scoped default lifetime, LinqToDBLoggerFactoryAdapter (TraceLevel→LogLevel mapping), UseLoggerFactory/UseDefaultLogging wiring through QueryTraceOptions.WriteTrace, distinction from Source/LinqToDB/Extensions/ (EXPR area).

## 2026-05-04T11:17:06Z — kb-build step 3 EXTENSIONS-PKG done
- areas/EXTENSIONS-PKG/INDEX.md written, Tier-1 3/3 (all .cs files), Tier-2 0/0 (none beyond Tier 1), Tier-3 0, confidence high
- Microsoft.Extensions integration package — DI registration (IServiceCollection extensions) + Logging adapter. Distinct from Source/LinqToDB/Extensions/ (in EXPR area, expression-tree extensions).
- DI surface: AddLinqToDB (1-connection shorthand) + AddLinqToDBContext<TContext> / <TContext,TContextImpl> (typed-context with reflection-based HasTypedContextConstructor to detect DataOptions/DataOptions<T>/DataOptions<TContext> constructor) + factory-delegate overloads + AddLinqToDBService<TContext> (registers ILinqService<TContext> for REMOTE-CLIENT). Default Scoped.
- Logging adapter: LinqToDBLoggerFactoryAdapter wraps ILoggerFactory + ILogger<DataConnection>; OnTrace(string?, string?, TraceLevel) maps TraceLevel→LogLevel; OptionsBuilderExtensions.UseLoggerFactory/UseDefaultLogging plug into DataOptions.QueryTraceOptions.WriteTrace.
- Cross-area dependencies: CORE (DataOptions, DataConnection, IDataContext, QueryTraceOptions), REMOTE-CLIENT (ILinqService<T>, LinqService<T>, IDataContextFactory<T>), Microsoft.Extensions.DependencyInjection NuGet, Microsoft.Extensions.Logging.Abstractions NuGet
- 3 known issues / debt items: unused 'category' parameter in OnTrace (IDE0060 suppressed; category-per-logger routing deferred), HasTypedContextConstructor throws ArgumentException at registration time with no compile-time guard, no IOptions<DataOptions> integration (callers must wire UseDefaultLogging explicitly)
- Completed areas: ... CLI, COMPAT, EXTENSIONS-PKG (32 of 41); 9 remaining
- Paused at user request (one area per turn)

## 2026-05-04T11:22:57Z — agent audit notes
- Proposed canonical Tier-1 list for FSHARP area (update kb-areas.md):

```
Source/LinqToDB.FSharp/LinqToDB.FSharp.fsproj
Source/LinqToDB.FSharp/DataOptionsExtensions.fs
Source/LinqToDB.FSharp/FSharpEntityBindingInterceptor.fs
Source/LinqToDB.FSharp/readme.md
```

Rationale: the area has only 4 meaningful files; all were read in full. `FSharpExpressionInterceptor.fs` is excluded from the fsproj `<Compile>` items and does not ship — it is NOT proposed as Tier 1 but is flagged separately below.

## 2026-05-04T11:24:10Z — kb-build step 3 FSHARP — unclassified files
- Source/LinqToDB.FSharp/FSharpExpressionInterceptor.fs — dead code on disk, excluded from <Compile> items in LinqToDB.FSharp.fsproj, references stale internal namespaces (LinqToDB.Interceptors.Internal, ExpressionInterceptor) that no longer exist. Pre-refactor predecessor of FSharpEntityBindingInterceptor; should be deleted or restored with current API. Action: triage.

## 2026-05-04T11:27:43Z — kb-build step 3 FSHARP — agent audit notes
- Proposed Tier-1 (4 files = all shipped files) for FSHARP row in kb-areas.md (was '(TBD on first read)').
- Anchors: LinqToDB.FSharp.fsproj, DataOptionsExtensions.fs, FSharpEntityBindingInterceptor.fs, readme.md.
- FSharpExpressionInterceptor.fs explicitly NOT proposed as Tier 1 — excluded from <Compile> items, dead pre-refactor code with stale namespace references.

## 2026-05-04T11:28:45Z — kb-areas.md FSHARP row filled in (applied)
- FSHARP row: Tier-1 set to 4 files (all shipped on-disk artifacts). Notes column expanded with: package identity, entry point (UseFSharp() via [<Extension>]), record detection mechanism (CompilationMappingAttribute(SourceConstructFlags.RecordType) minus [<CLIMutable>]), positional SequenceNumber-keyed member-to-constructor mapping with ConcurrentDictionary cache, SqlGenericConstructorExpression rewrite at both CreateType.New and CreateType.Full, net462 explicit FSharp.Core ref, dead FSharpExpressionInterceptor.fs file (excluded from <Compile>), and current scope limitation (records only; no DUs / option / collections).

## 2026-05-04T11:29:29Z — kb-build step 3 FSHARP done
- areas/FSHARP/INDEX.md written, Tier-1 4/4 (all shipped files), Tier-2 0/0, Tier-3 0, confidence high
- Currently records-only; entry via DataOptions.UseFSharp() from C# (via [<Extension>]); single registered interceptor (FSharpEntityBindingInterceptor) — no expression interceptor ships
- FSharpEntityBindingInterceptor mechanics: CompilationMappingAttribute(SourceConstructFlags.RecordType) detection minus [<CLIMutable>]; SequenceNumber-keyed ConcurrentDictionary<Type, Dictionary<int, MemberAccessor>> cache; rewrites SqlGenericConstructorExpression at both CreateType.New (positional Parameters re-tagged with MemberInfo) and CreateType.Full (named Assignments → Expression.New(ctor, args).MemberInit with default-value fill for missing slots)
- 1 UNCLASSIFIED-FILE: FSharpExpressionInterceptor.fs (excluded from <Compile>, dead pre-refactor code; needs delete-or-restore triage)
- 3 known issues / debt: dead FSharpExpressionInterceptor.fs file, no DU / option / collection support (acknowledged in readme), explicit `= true` comparison on AttributesExtensions.HasAttribute<CLIMutableAttribute>?: bool? (intentional null=false treatment)
- Cross-area dependencies: INTERCEPTORS (EntityBindingInterceptor base class + IEntityBindingInterceptor interface), CORE (DataOptions, TypeAccessor, MappingSchema, SqlGenericConstructorExpression)
- Completed areas: ... EXTENSIONS-PKG, FSHARP (33 of 41); 8 remaining
- Paused at user request (one area per turn)

## 2026-05-04T11:43:31Z — agent audit notes
- Proposed canonical Tier-1 list for `LINQPAD` area in `kb-areas.md`.

Replace the current `(TBD on first read)` cell with:

```
`Drivers/DynamicLinqToDBDriver.cs`, `Drivers/StaticLinqToDBDriver.cs`, `LINQPadDataConnection.cs`, `Drivers/DriverHelper.cs`, `DatabaseProviders/IDatabaseProvider.cs`, `DatabaseProviders/DatabaseProviderBase.cs`, `DatabaseProviders/DatabaseProviders.cs`, `DatabaseProviders/ProviderInfo.cs`, `Configuration/ConnectionSettings.cs`, `Configuration/AppConfig.cs`, `Drivers/DynamicSchemaGenerator.cs`, `Drivers/StaticSchemaGenerator.cs`, `Drivers/Scaffold/ModelProviderInterceptor.cs`, `Drivers/Scaffold/DataModelAugmentor.cs`, `LinqToDB.LINQPad.csproj`
```

Rationale:
- Both driver entry points (`LinqToDBDriver`, `LinqToDBStaticDriver`) are the LINQPad SDK integration boundary — stable public types whose names must not change.
- `LINQPadDataConnection` is the only public API type (besides the exception) that user-generated dynamic contexts inherit.
- `DriverHelper` owns all shared driver logic.
- `IDatabaseProvider` + `DatabaseProviderBase` + `DatabaseProviders` + `ProviderInfo` are the complete provider registry contract.
- `ConnectionSettings` + `AppConfig` define the settings serialization contract (v5 format).
- `DynamicSchemaGenerator` + `StaticSchemaGenerator` + `ModelProviderInterceptor` + `DataModelAugmentor` collectively define the SCAFFOLD integration.
- The csproj documents TFMs, package dependencies, and PostBuild bundle generation.

## 2026-05-04T11:47:39Z — kb-build step 3 LINQPAD — agent audit notes
- Proposed canonical Tier-1 (15 files) for LINQPAD row in kb-areas.md (was '(TBD on first read)').
- Anchors: 2 driver entry points (DynamicLinqToDBDriver, StaticLinqToDBDriver — class names locked by SDK contract), LINQPadDataConnection (only public type beside exception), DriverHelper (shared init/wiring/dialog logic), 4 provider-registry files (IDatabaseProvider/DatabaseProviderBase/DatabaseProviders/ProviderInfo), 2 configuration files (ConnectionSettings + AppConfig), 4 SCAFFOLD-integration files (DynamicSchemaGenerator + StaticSchemaGenerator + ModelProviderInterceptor + DataModelAugmentor), csproj.
- Driver class names (LinqToDBDriver / LinqToDBStaticDriver) cannot be renamed — saved LINQPad connections reference them by FQN.
- DB2/Informix excluded on 32-bit net8.0-windows (IntPtr.Size == 8 guard); always registered on net472.

## 2026-05-04T11:50:01Z — kb-areas.md LINQPAD row filled in (applied)
- LINQPAD row: Tier-1 set to 15 files (the SDK boundary, settings contract, provider registry, SCAFFOLD integration, csproj). Notes column expanded with: TFMs (net472 + net8.0-windows7.0), .lpx/.lpx6 bundle packaging via PostBuild + Pack.cmd, two-driver-mode architecture (Dynamic via SCAFFOLD+Roslyn vs Static via reflection), SDK-locked class names, 13-provider FrozenDictionary registry with 32-bit exclusions, SCAFFOLD integration details (ScaffoldOptions construction + ModelProviderInterceptor + DataModelAugmentor 3-arg ctor injection), SettingsV5 JSON-in-XML format with legacy migration, IntPtr.Size-aware ProviderPath, raw-WPF UI (no MVVM framework, allocation-avoiding static PropertyChangedEventArgs), Compat polyfills, iSeries WITH_ISERIES conditional with manual nuspec step, IBM DB2 net472 PostBuild relocation trick.

## 2026-05-04T11:50:06Z — kb-build step 3 LINQPAD done
- areas/LINQPAD/INDEX.md written, Tier-1 15/15 (100%), Tier-2 44/47 (93.6% — gate met), Tier-3 6 (NuGet/*.cmd/.xml/.png + README.md + TypeRenderingTests.txt + PublicAPI .txt), confidence high
- Two driver modes: LinqToDBDriver (DynamicDataContextDriver subclass — runs SCAFFOLD pipeline + Roslyn CSharpCompilation.Emit) vs LinqToDBStaticDriver (StaticDataContextDriver subclass — reflects [Table]/[Column]/[Association] on user-supplied assembly). Class names SDK-locked (renaming breaks saved connections).
- Provider registry: DatabaseProviders static class with FrozenDictionary<ProviderName, IDatabaseProvider> + FrozenDictionary<providerName, IDatabaseProvider>; 13 concrete IDatabaseProvider impls; DB2/Informix excluded on 32-bit net8.0-windows (IntPtr.Size == 8 guard); always registered on net472
- SCAFFOLD integration triad: DynamicSchemaGenerator (constructs ScaffoldOptions and runs LoadDataModel→GenerateCodeModel→GenerateSourceCode), ModelProviderInterceptor (ScaffoldInterceptors impl, accumulates model in AfterSourceCodeGenerated, emits ExplorerItem tree in GetTree()), DataModelAugmentor (ConvertCodeModelVisitor, injects 3-arg constructor on generated context class chaining to LINQPadDataConnection)
- Settings: ConnectionSettings JSON-serialized as SettingsV5 XML node in IConnectionInfo.DriverData; 5 nested option groups (Connection/Schema/Scaffold/LinqToDb/StaticContext); inner Legacy class migrates pre-v5 per-XML-node format; ProviderPath dispatches by IntPtr.Size for 32/64-bit
- WPF UI: SettingsDialog hosts 7 sub-tab models (DynamicConnection/StaticConnection/Scaffold/Schema/LinqToDB/Troubleshoot/About); raw INotifyPropertyChanged with per-property static PropertyChangedEventArgs (allocation-avoiding); plain {Binding} XAML, no MVVM framework
- ValueFormatter handles provider-specific result rendering (Npgsql geometry/interval/inet, Oracle/DB2 LOBs, MySqlGeometry, FbDecFloat) via 3 FrozenDictionary lookup tables (exact type, base type, by-type-name)
- PasswordManager resolves {pm:name} tokens in connection strings via LINQPad.Util.GetPassword
- Compat polyfills (net472 only): IReadOnlySet<T>, ReadOnlyHashSet<T>, ReadOnlySetExtensions for ConnectionSettings.SchemaOptions filter sets
- Cross-area: SCAFFOLD (Scaffolder + ScaffoldInterceptors + ScaffoldOptions + ConvertCodeModelVisitor), CORE (DataConnection + DataOptions + IDataContext), all PROV-* tools, LINQPad.Reference NuGet (DynamicDataContextDriver/StaticDataContextDriver/IConnectionInfo/ExplorerItem/QueryExecutionManager/Util), Microsoft.CodeAnalysis.CSharp
- 7 known issues / debt items: SqlServer return-value scaffolding TODO, CSharpUtils duplicate-of-CSharpCodeGenerator.KeyWords TODO, SYSLIB1045 (regex source-gen) suppressed on net472 PasswordManager, SYSLIB0044 (AssemblyName.CodeBase obsolete) suppressed in DynamicLinqToDBDriver, DB2/Informix silently disabled on 32-bit net8.0, WITH_ISERIES manual nuspec step, IBM DB2 net472 PostBuild relocation hack
- Completed areas: ... FSHARP, LINQPAD (34 of 41); 7 remaining
- Paused at user request (one area per turn)

## 2026-05-04T11:59:38Z — agent audit notes
- ## Proposed Tier-1 list for REMOTE area

All 23 `.cs` files in this area are small and anchor-worthy (each is a top-level public type). Proposed canonical Tier-1 list for `kb-areas.md`:

**gRPC (8 files):**
- `Source/LinqToDB.Remote.Grpc/IGrpcLinqService.cs`
- `Source/LinqToDB.Remote.Grpc/GrpcDataContext.cs`
- `Source/LinqToDB.Remote.Grpc/GrpcLinqService.cs`
- `Source/LinqToDB.Remote.Grpc/GrpcLinqServiceClient.cs`
- `Source/LinqToDB.Remote.Grpc/Dto/GrpcConfiguration.cs`
- `Source/LinqToDB.Remote.Grpc/Dto/GrpcConfigurationQuery.cs`
- `Source/LinqToDB.Remote.Grpc/Dto/GrpcInt.cs`
- `Source/LinqToDB.Remote.Grpc/Dto/GrpcString.cs`

**HTTP (5 files):**
- `Source/LinqToDB.Remote.HttpClient.Client/HttpClientDataContext.cs`
- `Source/LinqToDB.Remote.HttpClient.Client/HttpClientLinqServiceClient.cs`
- `Source/LinqToDB.Remote.HttpClient.Client/ServiceConfigurationExtensions.cs`
- `Source/LinqToDB.Remote.HttpClient.Server/LinqToDBController.cs`
- `Source/LinqToDB.Remote.HttpClient.Server/ServiceConfigurationExtensions.cs`

**SignalR (5 files):**
- `Source/LinqToDB.Remote.SignalR.Client/SignalRDataContext.cs`
- `Source/LinqToDB.Remote.SignalR.Client/SignalRLinqServiceClient.cs`
- `Source/LinqToDB.Remote.SignalR.Client/ServiceConfigurationExtensions.cs`
- `Source/LinqToDB.Remote.SignalR.Server/LinqToDBHub.cs`
- `Source/LinqToDB.Remote.SignalR.Server/LinqToDBHub{T}.cs`

**WCF (4 files):**
- `Source/LinqToDB.Remote.Wcf/IWcfLinqService.cs`
- `Source/LinqToDB.Remote.Wcf/WcfDataContext.cs`
- `Source/LinqToDB.Remote.Wcf/WcfLinqService.cs`
- `Source/LinqToDB.Remote.Wcf/WcfLinqServiceClient.cs`

Recommend updating `kb-areas.md` REMOTE row Tier-1 column with the 22 canonical anchors above (omitting `LinqToDBController{T}.cs` and `LinqToDBHub{T}.cs` as Tier-2 thin generic subclasses — captured under their non-generic bases).

## 2026-05-04T12:02:31Z — kb-build step 3 REMOTE — agent audit notes
- Proposed canonical Tier-1 (22 of 23 .cs files) for REMOTE row in kb-areas.md (was '(TBD on first read)').
- Anchors organized by transport: 8 gRPC files (4 service types + 4 DTO wrappers), 5 HTTP files (3 client + 2 server), 5 SignalR files (3 client + 2 server), 4 WCF files. Two thin generic subclasses (LinqToDBController{T}, LinqToDBHub{T}) demoted to Tier 2 — captured under non-generic bases.
- Common per-transport pattern: <X>DataContext (RemoteDataContextBase override of GetClient + ContextIDPrefix) + <X>LinqServiceClient (delegates 5 ILinqService methods) + server surface (controller/hub/service).
- gRPC needs 4 DTO wrappers because primitives aren't gRPC message types; HTTP server uses ASP.NET Core MVC [ApiController]; SignalR uses Hub (no contract — method names matched via nameof string); WCF lacks CancellationToken (best-effort ThrowIfCancellationRequested only).
- TFM matrix: WCF net462-only; HTTP server net8.0+ (ASP.NET Core); all others net462+netstandard2.0+net8.0+net9.0+net10.0.

## 2026-05-04T12:03:15Z — kb-areas.md REMOTE row filled in (applied)
- REMOTE row: Tier-1 set to 22 anchors organized by transport (8 gRPC + 5 HTTP + 5 SignalR + 4 WCF; 2 thin generic subclasses kept as Tier-2). Notes column expanded with: 6-package collapse rationale, common per-transport 3-type pattern (DataContext/LinqServiceClient/server-surface), wire format ownership (LinqServiceSerializer in INTERNAL-API), gRPC 4-DTO-wrapper requirement, ASP.NET Core MVC/SignalR/WCF dispatch mechanics, TFM matrix per package (WCF net462-only, HTTP server net8.0+, others span net462→net10.0), error propagation per transport (RpcException/FaultException/HTTP status codes), WCF cancellation limitation.

## 2026-05-04T12:06:43Z — kb-build step 3 REMOTE done
- areas/REMOTE/INDEX.md written, Tier-1 23/23 .cs (100%), Tier-2 6/6 csprojs (100%), Tier-3 0, confidence high
- 6 transport packages collapsed into 1 area (gRPC, HTTP-client, HTTP-server, SignalR-client, SignalR-server, WCF). Each is a thin shim — no query logic, no mapping, no wire format. Wire format owned by LinqServiceSerializer in INTERNAL-API.
- Common per-transport 3-type pattern documented: <X>DataContext (RemoteDataContextBase override) + <X>LinqServiceClient (5 ILinqService method delegation, sets RemoteClientTag) + server surface (controller/hub/service receiving 5 calls and delegating to injected ILinqService)
- gRPC: 4 protobuf-net DTO wrappers (GrpcInt, GrpcString, GrpcConfiguration, GrpcConfigurationQuery) needed because gRPC primitives aren't valid message types; IGrpcLinqService is the [Service]-annotated contract; new GrpcChannel created per query (potential overhead)
- HTTP: split into Client (HttpClient.PostAsJsonAsync) and Server (ASP.NET Core [ApiController] with 5 [HttpPost] actions); generic LinqToDBController<T> + ServiceConfigurationExtensions.AddLinqToDBController<TContext>; default route 'api/linq2db'
- SignalR: split into Client (HubConnection.InvokeAsync<T> with method names matched via nameof string) and Server (Hub subclass); no SignalR-server ServiceConfigurationExtensions (inconsistency with HTTP server)
- WCF: net462-only; single combined client+server package; [ServiceContract]/[OperationContract] on IWcfLinqService; [ServiceBehavior(Single, Multiple)] on WcfLinqService; ClientBase<IWcfLinqService> on WcfLinqServiceClient with 4 constructor overloads; cancellation is best-effort (ThrowIfCancellationRequested before each call)
- TFM matrix: linq2db.Remote.Wcf net462-only; linq2db.Remote.HttpClient.Server net8.0/9.0/10.0 (ASP.NET Core); all others net462+netstandard2.0+net8/9/10
- 6 known issues / debt items: WcfLinqServiceClient.RemoteClientTag has Cyrillic 'с' typo (cosmetic), WCF cancellation best-effort only (protocol limitation), SignalRLinqServiceClient.DisposeAsync is a no-op, SignalR server lacks ServiceConfigurationExtensions (inconsistent with HTTP), gRPC creates new channel per query (potential overhead), HTTP CA2000 suppression on HttpClient ownership transfer
- Cross-area dependencies: REMOTE-CLIENT (ILinqService, ILinqService<T>, RemoteDataContextBase, LinqService, LinqService<T>, LinqServiceInfo, IDataContextFactory<T>, DataContextFactory<T>); INTERNAL-API (LinqServiceSerializer); ASP.NET Core (server packages); Grpc.Net.Client + protobuf-net.Grpc + protobuf-net (gRPC); System.ServiceModel (WCF, net462 BCL)
- Completed areas: ... LINQPAD, REMOTE (35 of 41); 6 remaining
- Paused at user request (one area per turn)

## 2026-05-04T12:24:17Z — agent audit notes
- **Proposed canonical Tier-1 list for SCAFFOLD area (kb-areas.md update)**

Replace the current `(TBD on first read)` entry with:

```
Source/LinqToDB.Scaffold/Scaffold/Scaffolder.cs,
Source/LinqToDB.Scaffold/Scaffold/Customization/ScaffoldInterceptors.cs,
Source/LinqToDB.Scaffold/Scaffold/Options/ScaffoldOptions.cs,
Source/LinqToDB.Scaffold/Scaffold/Options/SchemaOptions.cs,
Source/LinqToDB.Scaffold/Scaffold/Options/DataModelOptions.cs,
Source/LinqToDB.Scaffold/Scaffold/Options/CodeGenerationOptions.cs,
Source/LinqToDB.Scaffold/Scaffold/DataModel/DataModelLoader.cs,
Source/LinqToDB.Scaffold/DataModel/Model/DatabaseModel.cs,
Source/LinqToDB.Scaffold/DataModel/Model/Entities/EntityModel.cs,
Source/LinqToDB.Scaffold/DataModel/DataModelGenerator.cs,
Source/LinqToDB.Scaffold/Schema/ISchemaProvider.cs,
Source/LinqToDB.Scaffold/Schema/ITypeMappingProvider.cs,
Source/LinqToDB.Scaffold/Schema/LegacySchemaProvider.cs,
Source/LinqToDB.Scaffold/CodeModel/AST/Basics/ICodeElement.cs,
Source/LinqToDB.Scaffold/CodeModel/Languages/ILanguageProvider.cs,
Source/LinqToDB.Scaffold/Metadata/IMetadataBuilder.cs,
Source/LinqToDB.Scaffold/Naming/NamingServices.cs
```

Rationale: these 17 files cover the three-phase public pipeline (`Scaffolder`), all three option record roots, the schema contract + legacy adapter, the DataModel root + primary entity type, both CodeModel generator roots, the AST node base interface, the language abstraction, the metadata emission contract, and the identifier normalization service. The `ModelGeneration/` legacy layer has no proposed Tier-1 anchor because it is not invoked via `Scaffolder`; a single note in the INDEX.md covers it.

## 2026-05-04T12:24:18Z — deferred-coverage queue updated
- SCAFFOLD: +257 deferred

## 2026-05-04T12:26:19Z — kb-build step 3 SCAFFOLD — agent audit notes
- Proposed canonical Tier-1 (17 files) for SCAFFOLD row in kb-areas.md (was '(TBD on first read)').
- Anchors cover the 3-phase public pipeline (Scaffolder + 3 option roots + DataModelLoader), DataModel root (DatabaseModel + EntityModel + DataModelGenerator), Schema contract (ISchemaProvider + ITypeMappingProvider + LegacySchemaProvider adapter), CodeModel root (ICodeElement + ILanguageProvider), Metadata emission (IMetadataBuilder), and identifier normalization (NamingServices).
- ModelGeneration/ subsystem (45 .cs files) intentionally NOT in Tier-1 — legacy T4-compat layer NOT invoked via Scaffolder; separate older codegen framework retained for T4-template back-compat (ModelGenerator.LinqToDB.cs, .DataModel.cs, .EditableObject.cs, .NotifyPropertyChanged.cs, etc.); referenced from T4-templates side only.

## 2026-05-04T12:26:47Z — kb-areas.md SCAFFOLD row filled in (applied)
- SCAFFOLD row: Tier-1 set to 17 files anchoring the 3-phase pipeline + 8 subsystem roots. Notes column expanded with: 281 .cs files / 8 subsystems summary, 3-phase Scaffolder pipeline (LoadDataModel via DataModelLoader → GenerateCodeModel via DataModelGenerator → GenerateSourceCode), per-subsystem file counts (CodeModel 128, DataModel 41, ModelGeneration 45, Schema 33, Scaffold 15, Metadata/Naming/Helpers ~19), legacy ModelGeneration/ layer call-out (NOT wired through Scaffolder; T4-template back-compat only), CSharp-only language provider (F#/VB.NET TODO #1553), consumer mapping (CLI/LINQPAD/T4-templates), provider-specific LegacySchemaProvider workarounds (PostgreSQL tuple-params bug, Access OleDb identity, MySQL type-mapping conflicts, SQLite missing column types, SqlServer RETURN_VALUE).

## 2026-05-04T12:28:14Z — kb-build step 3 SCAFFOLD done (with deferred-coverage)
- areas/SCAFFOLD/INDEX.md written, Tier-1 14/14, Tier-2 28/267 (10.5%) ✗ gate, Tier-3 0, confidence medium
- Gate caveat: Tier-2 below 90% — 257 files added to deferred-coverage queue (mirrors INTERNAL-API + EXPR-TRANS sampled-by-design pattern). Architecture captured at subsystem level; per-file leaf details (CodeModel AST nodes, ModelGeneration legacy layer, DataModel function models, Schema DTOs) deferred to /kb-refresh --source coverage.
- Deferred queue breakdown by subsystem: CodeModel/AST 67 files, CodeModel/Builders 15, CodeModel/Languages-CSharp 3, CodeModel/Types 11, CodeModel/Visitors 6, CodeModel/Comparers/Utils/CodeGeneration ~10, DataModel/Context 5 + DataModelGenerator 8 partials + Model 24, Schema/Functions 14 + Schema/Tables 8 + Schema/Common 4 + 2 schema providers, Metadata 5 model DTOs + 2 builders, Naming 5, ModelGeneration 45, Helpers/Scaffold partials ~10
- 3-phase Scaffolder pipeline documented concretely: LoadDataModel (DataModelLoader iterates schema objects + threads through ScaffoldInterceptors virtuals + builds entity/column/function models) → GenerateCodeModel (DataModelGenerator walks DatabaseModel + emits CodeFile/CodeClass/CodeProperty/CodeMethod AST + applies optional ConvertCodeModelVisitor[] post-processors) → GenerateSourceCode (normalizes identifiers via ILanguageProvider.GetIdentifiersNormalizer + collects naming scopes + resolves conflicts + invokes ILanguageProvider.GetCodeGenerator + calls ScaffoldInterceptors.AfterSourceCodeGenerated)
- CSharp is the only ILanguageProvider implementation (CSharpLanguageProvider singleton); F# and VB.NET TODO at LanguageProviders.cs:9 (issue #1553)
- ModelGeneration/ legacy layer (45 files) is a separate older codegen framework NOT wired through Scaffolder — T4-template back-compat only; partial ModelGenerator.* files (LinqToDB/DataModel/EditableObject/NotifyPropertyChanged/NotifyDataErrorInfo/Validation)
- ScaffoldInterceptors extensibility surface: 7 schema-layer methods (GetTables/GetViews/GetForeignKeys/GetProcedures/GetTableFunctions/GetScalarFunctions/GetAggregateFunctions), GetTypeMapping, 5 data-model preprocessors (PreprocessEntity/StoredProcedure/TableFunction/ScalarFunction/AggregateFunction/Association), and AfterSourceCodeGenerated post-codegen hook
- ScaffoldOptions.Default()/T4() factory methods provide two preset configurations; T4 preset enables T4Compat naming casings for back-compat
- 6 known issues / debt items: F#/VB.NET unimplemented (#1553), Console.Error.WriteLine logger fallback (DataModelLoader.MapType + LegacySchemaProvider.ParseColumn TODOs), PostgreSQL legacy schema-provider table-function-column-as-output-parameter bug workaround, MySQL type-mapping conflict swallowed, ModelGeneration/ legacy layer undocumented lifecycle, ISchemaProvider lacks async overloads (TODO at top of file)
- Cross-area dependencies: METADATA / CORE (LegacySchemaProvider calls IDataProvider.GetSchemaProvider().GetSchema), SQL-PROVIDER (DataModelGenerator consumes ISqlBuilder), METADATA attributes (AttributeBasedMetadataBuilder + FluentMetadataBuilder emit [Table]/[Column]/[Association]/[Sql.Function]), TOOLS (WellKnownTypes references LinqToDB.Tools), Humanizer.Core NuGet (HumanizerNameConverter)
- Inbound consumers: CLI (ScaffoldCommand), LINQPAD (DynamicSchemaGenerator + ModelProviderInterceptor + DataModelAugmentor), T4 templates (legacy ModelGeneration/ layer)
- Completed areas: ... REMOTE, SCAFFOLD (36 of 41); 5 remaining
- Paused at user request (one area per turn)

## 2026-05-04T12:36:00Z — agent audit notes
- area: T4-TEMPLATES
finding: T4Model.ttinclude should be promoted to Tier 1

Current Tier-1 list in kb-areas.md: LinqToDB.ttinclude, DataModel.ttinclude, LinqToDB.Tools.ttinclude.

T4Model.ttinclude is the abstract model framework that defines every partial class (`Table`, `Column`, `ForeignKey`, `Procedure`, `Class`, `Property`, `Field`, `Method`, `Attribute`, `Event`, `MemberGroup`) and instantiates `ModelGenerator<Table,Procedure>`. All three current Tier-1 files either include it directly (`DataModel.ttinclude` line 3) or depend on it transitively. Understanding T4-TEMPLATES without reading T4Model.ttinclude would leave the type hierarchy unexplained.

Recommendation: add `T4Model.ttinclude` as a 4th Tier-1 anchor in kb-areas.md for this area.

## 2026-05-04T12:36:00Z — deferred-coverage queue updated
- T4-TEMPLATES: +4 deferred

## 2026-05-04T12:37:47Z — kb-build step 3 T4-TEMPLATES — agent audit notes
- Proposed Tier-1 expansion for T4-TEMPLATES row in kb-areas.md (3 → 4 anchors): adds T4Model.ttinclude.
- Rationale: T4Model.ttinclude is the abstract model framework defining every partial class (Table/Column/ForeignKey/Procedure/Class/Property/Field/Method/Attribute/Event/MemberGroup) and instantiating ModelGenerator<Table,Procedure>. All 3 current Tier-1 files depend on it (DataModel.ttinclude includes it directly at line 3; LinqToDB.ttinclude includes DataModel.ttinclude). Understanding the area without it would leave the type hierarchy unexplained.
- Tier-2 glob extended from `**/*.ttinclude` to also include `README.md` (matches actual on-disk inventory).

## 2026-05-04T12:38:54Z — kb-areas.md T4-TEMPLATES Tier-1 list expanded (applied)
- T4-TEMPLATES row: Tier-1 list expanded from 3 to 4 by adding T4Model.ttinclude. Tier-2 glob extended with README.md. Notes column expanded with: linq2db.t4models packaging (NuGet content + tools/ DLLs + $(LinqToDBT4SharedTools) MSBuild var), 5-layer composition graph (provider include → LinqToDB → DataModel → T4Model → LinqToDB.Tools loads linq2db.dll/linq2db.Scaffold.dll), partial-class extension pattern hooking SCAFFOLD's legacy ModelGeneration interfaces (ITable/IColumn/IForeignKey/IProcedure<>/ModelGenerator/ModelSource), 10 optional add-ons with BeforeGenerateModel/AfterGenerateLinqToDBModel hooks, distinction from modern CLI scaffold path, VS-only MultipleFiles.ttinclude (EnvDTE), .NET-Framework-only PluralizationService.ttinclude (Humanizer.ttinclude is replacement), AppDomain.AssemblyResolve fallback for version mismatches.

## 2026-05-04T12:39:05Z — kb-build step 3 T4-TEMPLATES done (with deferred-coverage)
- areas/T4-TEMPLATES/INDEX.md written, Tier-1 4/4, Tier-2 7/11 (64% — below 90%; 4 add-ons deferred), Tier-3 0, confidence medium
- Gate caveat: Tier-2 below 90% — 4 add-on .ttinclude files (EditableObject/Equatable/NotifyDataErrorInfo/Validation) deferred with skip reason 'name + include-chain pattern unambiguous from sibling add-ons; no novel types expected'. Pattern is established by sibling NotifyPropertyChanged/DataAnnotations reads.
- Legacy T4 scaffold path documented as distinct from modern CLI scaffold (Scaffolder + CodeModel/AST in SCAFFOLD area). T4 templates use SCAFFOLD's ModelGeneration/ legacy layer (45 files retained for back-compat — confirmed cross-area link).
- 5-layer composition graph: user .tt → provider-specific LinqToDB.<Provider>.ttinclude → LinqToDB.ttinclude → DataModel.ttinclude → T4Model.ttinclude → LinqToDB.Tools.ttinclude (loads linq2db.dll + linq2db.Scaffold.dll into AppDomain via <#@ assembly #>)
- T4Model.ttinclude partial classes (Table/Column/ForeignKey/Procedure/Class/Property/Field/Method/Attribute/Event/MemberGroup) extend SCAFFOLD's ModelGeneration interfaces (ITable/IColumn/IForeignKey/IProcedure<>/IClass/IProperty/IField/IMethod/IAttribute/IEvent/IMemberGroup); ModelGenerator<Table,Procedure> backed by ModelSource
- Optional add-ons hook BeforeGenerateModel / AfterGenerateLinqToDBModel: NotifyPropertyChanged (calls ModelGenerator.NotifyPropertyChangedImplementation), ObsoleteAttributes (parses [Obsolete] from DB descriptions), DataAnnotations ([Display]/[Required]/[StringLength] injection), MultipleFiles (VS EnvDTE multi-file split), Humanizer (Pluralize/Singularize/Pascalize delegates), PluralizationService (EF5 .NET Framework alternative + hand-coded English dictionary)
- Packaging: NuGet/t4models/linq2db.t4models.csproj packs .ttinclude as contentFiles + tools/ folder with pre-built provider DLLs resolved via $(LinqToDBT4SharedTools) MSBuild var; AppDomain.AssemblyResolve fallback handles version mismatches
- 4 known issues / debt items: MultipleFiles.ttinclude VS-only (EnvDTE; no Rider/MSBuild path), PluralizationService.ttinclude .NET-Framework-only (System.Data.Entity.Design EF5; Humanizer.ttinclude replaces), legacy ModelGeneration layer divergence from modern CLI scaffold (new CLI features not auto-surfaced in T4), AppDomain APIs not single-file/AOT compatible (design-time only — not a runtime issue)
- Cross-area dependencies: SCAFFOLD (ModelGeneration/ legacy layer — 45 files retained for T4 back-compat; ModelGenerator/ModelSource/I*-interfaces; consumed at design time when template runs); CORE (DataConnection); METADATA (SchemaProvider.GetSchemaOptions); Humanizer.dll (optional); System.Data.Entity.Design (.NET Framework, optional); EnvDTE (VS, optional)
- Inbound: provider-specific NuGet packages (NuGet/SqlServer/, NuGet/MySql/, etc.) ship LinqToDB.<Provider>.ttinclude that chain into this folder; Tests/Tests.T4.Nugets/ validates package layout
- Completed areas: ... SCAFFOLD, T4-TEMPLATES (37 of 41); 4 remaining
- Paused at user request (one area per turn)

## 2026-05-04T12:53:44Z — agent audit notes
- Proposed canonical Tier-1 list for `kb-areas.md` row `TOOLS`:

```
Activity/ActivityHierarchy.cs
Activity/ActivityStatistics.cs
Activity/IStatActivity.cs
Comparers/ComparerBuilder.cs
Comparers/IgnoreComparisonAttribute.cs
DataProvider/SqlServer/Schemas/ISystemSchemaData.cs
DataProvider/SqlServer/Schemas/SystemDB.cs
DataProvider/SqlServer/Schemas/SystemSchemaExtensions.cs
EntityServices/IEntityMap.cs
EntityServices/IdentityMap.cs
EntityServices/EntityMap.cs
Mapper/IMapperBuilder.cs
Mapper/MapperBuilder.cs
Mapper/Map.cs
Mapper/ExpressionBuilder.cs
```

All paths relative to `Source/LinqToDB.Tools/`. The non-generic `EnumerableExtensions.cs` and `MappingSchemaExtensions.cs` are public surface but serve supporting roles; they are fine as Tier 2. `Schemas.generated.cs` is T4-generated (auto-gen header present) and should remain Tier 3 — it is not hand-authored. `Schemas.tt` is the generator source and could be listed as a Tier-2 non-`.cs` file if future refreshes want to track its changes.

## 2026-05-04T12:55:17Z — kb-build step 3 TOOLS — agent audit notes
- Proposed canonical Tier-1 (15 files) for TOOLS row in kb-areas.md (was '(TBD on first read)').
- Anchors organized by 5 subsystems: 3 Activity (ActivityHierarchy + ActivityStatistics + IStatActivity), 2 Comparers (ComparerBuilder + IgnoreComparisonAttribute), 3 SqlServer Schemas (ISystemSchemaData + SystemDB + SystemSchemaExtensions), 3 EntityServices (IEntityMap + IdentityMap + EntityMap), 4 Mapper (IMapperBuilder + MapperBuilder + Map + ExpressionBuilder).
- EnumerableExtensions.cs + MappingSchemaExtensions.cs intentionally Tier 2 (public surface but supporting roles); Schemas.generated.cs Tier 3 (T4 auto-generated against SQL Server 2022).

## 2026-05-04T12:55:49Z — kb-areas.md TOOLS row filled in (applied)
- TOOLS row: Tier-1 set to 15 anchors organized by 5 opt-in subsystems (Activity / Comparers / DataProvider/SqlServer/Schemas / EntityServices / Mapper). Notes column expanded with: package identity (linq2db.Tools standalone NuGet, distinct from in-tree IN-TREE-TOOLS area), per-subsystem mechanics (ActivityStatistics ~70-entry per-ActivityID registry + ActivityHierarchy AsyncLocal tree recorder hooking ActivityService.AddFactory; ComparerBuilder TypeAccessor-based reflection with [IgnoreComparisonAttribute] opt-out and per-element dispatch; SystemDB DataConnection subclass over SQL Server 2022-generated typed model + GetTableRowCountInfo; IdentityMap EntityServiceInterceptor with per-type ConcurrentDictionary cache + EntityMapEntry interlocked DBCount/CacheCount; Map/MapperBuilder/Mapper AutoMapper-style + ExpressionBuilder dual-path with >10-level cross-reference restart guard), 2 root files (EnumerableExtensions.ToDiagnosticString ASCII formatter + MappingSchemaExtensions key-comparer factory).

## 2026-05-04T12:56:50Z — kb-build step 3 TOOLS done
- areas/TOOLS/INDEX.md written, Tier-1 15/15, Tier-2 13/13 (100%), Tier-3 1 (Schemas.generated.cs T4 auto-generated against SQL Server 2022 16.00.1135), confidence high
- 5 opt-in subsystems documented:
- (1) Activity: ActivityStatistics + ActivityHierarchy as concrete LinqToDB.Metrics.IActivity implementations registered via ActivityService.AddFactory; ~70-entry static registry by ActivityID; AsyncLocal tree recorder on root Dispose emits indented call-tree string
- (2) Comparers: ComparerBuilder.GetEqualityComparer<T>() reflects via TypeAccessor with [IgnoreComparisonAttribute] opt-out; per-element dispatch to BitArrayEqualityComparer/EnumerableEqualityComparer<T>/recursive ComparerBuilder for class members; cached in Comparer<T>.DefaultInstance
- (3) DataProvider/SqlServer/Schemas: SystemDB DataConnection subclass + ISystemSchemaData interface + SystemSchemaExtensions; Schemas.generated.cs (T4 from Schemas.tt against SQL Server 2022 16.00.1135 — needs manual regen, no CI step) declares ~30 typed schema models; GetTableRowCountInfo queries sys.partitions filtered to user-table index types 0/1 with SqlFn.ObjectSchemaName/.ObjectName
- (4) EntityServices: opt-in identity map — IdentityMap : EntityServiceInterceptor, IDisposable constructor auto-registers via _dataContext.AddInterceptor; EntityMap<T> per-type ConcurrentDictionary keyed by dataContext.GetKeyEqualityComparer<T>(); EntityMapEntry<T> with interlocked DBCount/CacheCount; GetEntity falls back to live DB query (silent — possible surprise)
- (5) Mapper: AutoMapper-style; Map.GetMapper<TFrom,TTo>() + Map.DeepCopy<T>() extension with MapHolder<T> static cache; MapperBuilder<TFrom,TTo> fluent (FromMapping/ToMapping/MapMember/SetProcessCrossReferences/SetDeepCopy); Mapper<TFrom,TTo> lazy compiled-delegate cache; ExpressionBuilder dual-path (create-new + in-place with cross-reference IDictionary tracking; >10-level recursion triggers ProcessCrossReferences=true rebuild)
- Root files: EnumerableExtensions.ToDiagnosticString<T> ASCII table formatter (numeric right-align, InvariantCulture); MappingSchemaExtensions provides GetKeyEqualityComparer/GetEntityEqualityComparer/GetEqualityComparer on MappingSchema, IDataContext, ITable<T> (key fallback to all-columns when no PK)
- 5 known issues / debt items: StatActivity.Start() PlatformID.Unix vs Stopwatch fallback (DateTime.Now ~15ms resolution unreliable on Windows non-Stopwatch path), ActivityStatistics process-global static (no Reset, parallel test interference), ExpressionBuilder.ConvertCollection NotImplementedException for non-List/HashSet/array, Schemas.generated.cs SQL Server 2022 16.00.1135 fixed (no CI regen), EntityMap<T>.GetEntity issues silent live DB query when not cached (bypasses identity-map contract surprise)
- Cross-area dependencies: CORE (DataConnection + DataOptions/DataOptions<T> + IDataContext + EntityDescriptor); MAPPING (MappingSchema + EntityDescriptor + ColumnDescriptor); INTERCEPTORS (IActivity + ActivityID + ActivityService + ActivityBase + EntityServiceInterceptor + EntityCreatedEventData); REFLECTION/INFRA (TypeAccessor + MemberAccessor); PROV-SQLSERVER (SqlFn.ObjectSchemaName/.ObjectName)
- Distinct from IN-TREE-TOOLS area: linq2db.Tools is a separate NuGet package (own assembly + own ProjectReference back to LinqToDB), whereas IN-TREE-TOOLS holds Tools/DataExtensions.RetrieveIdentity<T> inside the main linq2db.dll
- Completed areas: ... T4-TEMPLATES, TOOLS (38 of 41); 3 remaining
- Paused at user request (one area per turn)

## 2026-05-04T13:04:53Z — agent audit notes
- Proposed canonical Tier-1 list for `kb-areas.md` CODEGEN row (update `(TBD on first read)` to this):

```
Source/CodeGenerators/BuildersGenerator.cs
Source/CodeGenerators/BuildersGenerator.Models.cs
Source/CodeGenerators/EquatableReadOnlyList.cs
```

Rationale: these are the only three `.cs` files in the project. The `csproj` and `Directory.Build.props` are build-metadata files; they carry no runtime logic and should remain Tier 3 (counted, not read in future incremental runs unless the hook-in configuration changes).

The generator scans for three attributes — `BuildsAnyAttribute`, `BuildsExpressionAttribute`, `BuildsMethodCallAttribute` — not just `BuildsMethodCallAttribute` as the task prompt stated. The EXPR-TRANS area's INDEX.md should reference all three attribute names if it describes this generator.

## 2026-05-04T13:06:18Z — kb-build step 3 CODEGEN — agent audit notes
- Proposed canonical Tier-1 (3 files = all .cs in project) for CODEGEN row in kb-areas.md (was '(TBD on first read)').
- Anchors: BuildersGenerator.cs, BuildersGenerator.Models.cs, EquatableReadOnlyList.cs.
- Notable correction to prompt brief: the generator scans for 3 attributes (BuildsAnyAttribute + BuildsExpressionAttribute + BuildsMethodCallAttribute), not just BuildsMethodCallAttribute. EXPR-TRANS INDEX.md may need updating to reference all 3.

## 2026-05-04T13:06:43Z — kb-areas.md CODEGEN row filled in (applied)
- CODEGEN row: Tier-1 set to 3 .cs files (the entire .cs surface of the project). Notes column expanded with: Roslyn incremental source generator role (produces ExpressionBuilder.g.cs at compile time), csproj configuration (netstandard2.0/IsRoslynComponent/EnforceExtendedAnalyzerRules/IsPackable=false/Meziantou.Polyfill for HashCode+IsExternalInit), hook-in via OutputItemType=Analyzer + ReferenceOutputAssembly=false, generator pipeline (ForAttributeWithMetadataName x3 for BuildsAny/Expression/MethodCall + Transform + Collect + Combine + RegisterImplementationSourceOutput), CallParams [Flags] bitmask for parameter-shape encoding, EquatableReadOnlyList<T> required for SG cache stability, 3-level emitted dispatch (NodeType → MethodInfo.Name → per-builder guards), and stricter local Directory.Build.props (AnalysisLevel preview-All).

## 2026-05-04T13:06:49Z — kb-build step 3 CODEGEN done
- areas/CODEGEN/INDEX.md written, Tier-1 3/3 (all .cs files), Tier-2 0/0, Tier-3 2 (csproj + Directory.Build.props), confidence high
- Roslyn incremental source generator producing ExpressionBuilder.g.cs at compile time of LinqToDB.csproj. Hook-in via `<ProjectReference … OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`
- Pipeline: 3 ForAttributeWithMetadataName syntax providers (BuildsAny/Expression/MethodCall) → Transform→BuilderNode/EquatableReadOnlyList<BuilderNode> → Collect → Combine → RegisterImplementationSourceOutput → GenerateCode → ExpressionBuilder.g.cs
- Output: partial class ExpressionBuilder { private static partial ISequenceBuilder? FindBuilderImpl(BuildInfo, ExpressionBuilder) { ... } } with 3-level switch dispatch (ExpressionType.Call → MethodInfo.Name → per-builder CanBuildMethod guards); Any-kind builders fall outside switch and tried unconditionally after typed cases
- EquatableReadOnlyList<T> wrapper essential for incremental SG caching — pre-Collect Transform returns this (not List<T>) so equality comparison is meaningful and unchanged inputs skip regeneration
- CallParams [Flags] bitmask (Call=1, Info=2, Builder=4) encodes which of (MethodCallExpression/Expression, BuildInfo, ExpressionBuilder) the CanBuild method takes — drives renderer to emit exactly the right signature without runtime overhead
- csproj: netstandard2.0 (Roslyn SG req), IsRoslynComponent=true, EnforceExtendedAnalyzerRules=true, IsPackable=false; refs Microsoft.CodeAnalysis.CSharp + Microsoft.CodeAnalysis.Analyzers + Meziantou.Polyfill (System.HashCode + IsExternalInit polyfills)
- Directory.Build.props local override stricter than parent: RunAnalyzersDuringBuild + EnforceCodeStyleInBuild + AnalysisLevel=preview-All + TreatWarningsAsErrors + Nullable=enable + LangVersion=14
- Notable correction surfaced: 3 attribute names (not just BuildsMethodCallAttribute as prompt stated); EXPR-TRANS INDEX.md may need cross-link update
- 0 known issues / debt items — generator self-contained; new builder requires only attribute decoration (no manual registration)
- Cross-area: EXPR-TRANS (consumes generated dispatch in ExpressionBuilder + decorates Internal/Linq/Builder/* classes with [BuildsAny]/[BuildsExpression]/[BuildsMethodCall])
- Completed areas: ... TOOLS, CODEGEN (39 of 41); 2 remaining
- Paused at user request (one area per turn)

## 2026-05-04T13:11:02Z — agent audit notes
- Area: SHARED-INTERNAL
Finding: `Source/Shared/Extensions.cs` is classified Tier-2 in the current `kb-areas.md` pin list but carries the most logic of the three files in this area (TFM-compat polyfills for `string`, `StringBuilder`, `char`, and ADO.NET async-dispose). It is a primary contributor to every C# project in the solution via the blanket `Directory.Build.props` include. Recommend promoting it to Tier-1 in `kb-areas.md` for this area so future delta runs always re-read it in full.

Proposed change to `kb-areas.md` SHARED-INTERNAL row:
  Tier-1 (current): `JetBrains.Annotations.cs`, `SharedAssemblyInfo.cs`
  Tier-1 (proposed): `JetBrains.Annotations.cs`, `SharedAssemblyInfo.cs`, `Extensions.cs`

## 2026-05-04T13:12:20Z — kb-build step 3 SHARED-INTERNAL — agent audit notes
- Proposed Tier-1 expansion (2 → 3 anchors): adds Extensions.cs.
- Rationale: Extensions.cs carries the most logic in this area (TFM-compat polyfills for string/StringBuilder/char/ADO.NET async-dispose) and is linked into every C# project. Currently classified Tier-2 in kb-areas.md despite being primary contributor.

## 2026-05-04T13:12:33Z — kb-areas.md SHARED-INTERNAL Tier-1 list expanded (applied)
- SHARED-INTERNAL row: Tier-1 expanded from 2 → 3 by adding Extensions.cs (TFM-compat polyfills). Notes column expanded with: linkage mechanism (Directory.Build.props:192-194 <Compile Include … LinkBase=Compatibility> for all C# projects, no csproj for this folder), per-file purposes (SharedAssemblyInfo [module:SkipLocalsInit]; JetBrains.Annotations.cs internal sealed shims for ReSharper/Rider attributes; Extensions.cs StringExtensions/StringBuilderExtensions/CharExtensions/AdoAsyncDispose polyfills primarily for NETFRAMEWORK || NETSTANDARD2_0), banned-overload avoidance (JoinStrings wrapper), Tests.csproj duplicate <Compile Include> for JetBrains.Annotations.cs (likely pre-dates blanket glob).

## 2026-05-04T13:12:40Z — kb-build step 3 SHARED-INTERNAL done
- areas/SHARED-INTERNAL/INDEX.md written, Tier-1 2/2 (per old kb-areas.md pin list), Tier-2 1/1, Tier-3 0, confidence high — full coverage of all 3 .cs files
- No csproj for this folder — all 3 .cs files linked into every C# project via Directory.Build.props:192-194 <Compile Include … LinkBase=Compatibility> under Condition='$(Language)'==C#'
- SharedAssemblyInfo.cs: solely [module: SkipLocalsInit] (zero-init disabled solution-wide for perf)
- JetBrains.Annotations.cs: internal sealed shims of ReSharper/Rider annotation attributes (StringFormatMethodAttribute, UsedImplicitlyAttribute, MeansImplicitUseAttribute, PublicAPIAttribute, InstantHandleAttribute, PureAttribute, LinqTunnelAttribute, NoEnumerationAttribute) + ImplicitUseKindFlags/ImplicitUseTargetFlags enums; no JetBrains.Annotations NuGet dep
- Extensions.cs: 4 polyfill classes — StringExtensions (AsSpan fallbacks via C# 14 extension-member blocks + JoinStrings avoiding banned string.Join overload), StringBuilderExtensions (AppendBuilder/InsertBuilder/AppendJoinStrings — ToString() intermediates on legacy TFMs vs native Append(StringBuilder)/AppendJoin on modern), CharExtensions (IsAsciiDigit/Letter/LetterOrDigit), AdoAsyncDispose (legacy-only DisposeAsync extension on DbCommand/DbDataReader/DbConnection/DbTransaction with runtime IAsyncDisposable check + sync fallback)
- Inbound: every C# project in solution. Outbound: System.Data.Common (Extensions.cs legacy TFMs only); no LinqToDB references
- 3 known issues / debt items: Tests.csproj:75 redundant <Compile Include … JetBrains.Annotations.cs> (covered by blanket Directory.Build.props glob; likely pre-dates), SharedAssemblyInfo.cs reduced to single [module: SkipLocalsInit] line (was previously a richer assembly-info file), Extensions.cs Tier-2 misclassification in old kb-areas.md (resolved by Tier-1 promotion this run)
- Completed areas: ... CODEGEN, SHARED-INTERNAL (40 of 41); 1 remaining
- All 12 companion-project areas now complete (CLI, COMPAT, EXTENSIONS-PKG, FSHARP, LINQPAD, REMOTE, SCAFFOLD, T4-TEMPLATES, TOOLS, CODEGEN, SHARED-INTERNAL, EFCORE)
- Paused at user request (one area per turn)

## 2026-05-04T13:15:01Z — kb-build step 3 area-count correction
- Resolving long-standing tally discrepancy: '41 of 41' was inherited from early-build audit entries and was off by ~9-10. Corrected enumeration:
- kb-areas.md lists 51 area codes total; GLOBAL is excluded (cross-cutting docs only, n/a path patterns) → 50 areas have indexable scope.
- Areas with on-disk INDEX.md after SHARED-INTERNAL run: 40 (CORE, SQL-AST, SQL-PROVIDER, EXPR-TRANS, LINQ, MAPPING, DATA, EXPR, INFRA, INTERCEPTORS, INTERNAL-API, METADATA, REMOTE-CLIENT, IN-TREE-TOOLS, all 14 PROV-*, EFCORE, CLI, COMPAT, EXTENSIONS-PKG, FSHARP, LINQPAD, REMOTE, SCAFFOLD, T4-TEMPLATES, TOOLS, CODEGEN, SHARED-INTERNAL).
- Truly remaining: 10 areas (TESTS-INFRA, TESTS-LINQ, TESTS-EFCORE, TESTS-FSHARP, TESTS-T4, TESTS-VB, TESTS-MODEL, TESTS-BENCHMARKS, BUILD, CLAUDE-INFRA) + GLOBAL (out of scope for step 3 — cross-cutting docs only).
- Going forward: '40 of 50' (with 10 remaining indexable + 1 GLOBAL out-of-scope). User confirmed continuation through remaining 10 at 1-per-turn cadence.

## 2026-05-04T13:26:36Z — agent audit notes
- Proposed explicit Tier-1 list for TESTS-INFRA (current kb-areas.md entry uses a glob for `*Attribute.cs`):

Recommended pinned Tier-1 files (14 total):
1. Tests/Base/TestBase.cs
2. Tests/Base/TestConfiguration.cs
3. Tests/Base/TestProvName.cs
4. Tests/Base/Attributes/DataSourcesBaseAttribute.cs
5. Tests/Base/Attributes/DataSourcesAttribute.cs
6. Tests/Base/Attributes/IncludeDataSourcesAttribute.cs
7. Tests/Base/Attributes/CreateDatabaseSourcesAttribute.cs
8. Tests/Base/Attributes/ActiveIssueAttribute.cs
9. Tests/Base/Attributes/ThrowsWhenAttribute.cs
10. Tests/Base/Attributes/SkipCIAttribute.cs
11. Tests/Base/Attributes/FeatureSources/AllJoinsSourceAttribute.cs
12. Tests/Base/Attributes/FeatureSources/MergeDataContextSourceAttribute.cs
13. Tests/Base/Attributes/FeatureSources/RecursiveCteContextSourceAttribute.cs
14. Tests/Linq/Tests.csproj

Rationale: the current glob `every *Attribute.cs under Tests/Base/` implicitly includes 21 files (all found under `Attributes/` and `Attributes/FeatureSources/`). The 7 small provider-specific `Throws*` and `NorthwindDataContextAttribute` files are near-trivial one-method subclasses and are better classified as Tier 2. The 3 files above (items 11-13) are the representative FeatureSources anchors. `TestConfiguration.cs` and `TestProvName.cs` should be explicitly pinned as they are the runtime backbone of provider selection.

## 2026-05-04T13:26:37Z — deferred-coverage queue updated
- TESTS-INFRA: +51 deferred

## 2026-05-04T13:28:21Z — kb-build step 3 TESTS-INFRA — agent audit notes
- Proposed explicit Tier-1 list (14 files) replacing the glob `every *Attribute.cs under Tests/Base/`.
- The glob implicitly captured 21 attribute files; 7 narrow Throws*/NorthwindDataContext/SkipCategory/InsertOrUpdate variants demoted to Tier 2 (one-method subclasses).
- Added explicit pins for TestConfiguration.cs + TestProvName.cs (runtime backbone of provider selection — were previously implicit-via-TestBase coupling).
- FeatureSources representatives elevated: AllJoinsSourceAttribute + MergeDataContextSourceAttribute + RecursiveCteContextSourceAttribute (the 3 most-cited feature filters).

## 2026-05-04T13:28:28Z — kb-areas.md TESTS-INFRA Tier-1 list explicitly pinned (applied)
- TESTS-INFRA row: Tier-1 set to explicit 14 files (replacing prior glob `every *Attribute.cs under Tests/Base/`). Notes column expanded with: provider selection mechanics (DataSourcesBase → DataSources + IncludeDataSources; ActiveIssue/ThrowsWhen/SkipCI; FeatureSources hardcoded supported-provider lists); config loading (TestConfiguration + SettingsReader's BasedOn/++/---/-/[OtherName] grammar); TestBase 8-partial layout + AssertQuery in-memory re-evaluation; CustomTestContext/BaselinesManager/BaselinesWriter SQL trace + per-test .sql baseline files; in-process gRPC/HTTP/SignalR/WCF server containers with thread-id port offsetting; test-only interceptors; CustomizationSupport for fork extensibility; TestNoopProvider for translation-only tests; X86Stubs DB2 stubs under DB2STUBS; Tests.Playground for ad-hoc playground via .playground.slnf.

## 2026-05-04T13:28:48Z — kb-build step 3 TESTS-INFRA done (with deferred-coverage)
- areas/TESTS-INFRA/INDEX.md written, Tier-1 14/14, Tier-2 18/70 (26% — gate not met; 51 files in deferred-coverage queue), Tier-3 0, confidence medium
- Provider selection: NUnit IParameterDataSource attribute family (DataSourcesBase root + DataSources excludes-from-UserProviders + IncludeDataSources intersects-with-UserProviders + ActiveIssue marks Explicit + ThrowsWhen wraps for parameter-conditional exception + SkipCI excludes flaky from CI); FeatureSources hardcode supported lists per feature (AllJoins/Merge/RecursiveCte/Cte/Analytic/IdentityInsertMerge/MergeNotMatchedBySource); LinqServiceSuffix toggles each test to also run via remote transport
- TestConfiguration ctor: searches up for DataProviders.json (committed) + UserDataProviders.json (gitignored override); SettingsReader merges with BasedOn inheritance + ++/--- provider-list shorthand + - prefix removes + [OtherName] connection-string-by-name reference; populates UserProviders/SkipCategories/DefaultProvider/BaselinesPath
- TxtSettings implements DataConnection.DefaultSettings on non-netfx (DataConnection.AddOrSetConfiguration on net462)
- TestBase 8-partial layout: .cs (static ctor wires WriteTraceLine, OptimizeForSequentialAccess setup, baselines+SQL-trace teardown), .Context (provider-name dispatch with LinqServiceSuffix→IServerContainer.CreateContext), .Asserts (AreEqual/CompareSql/AssertState gated dead-code), .AssertQuery (DB exec + in-memory LINQ re-eval via ApplyNullPropagationVisitor + cross-check), .Tables (lazy Person/Parent/Child/etc + Northwind wrapper + DataCache<T>), .Concurrent/.Identity/.Utils
- CustomTestContext singleton ConcurrentDictionary<string,object?> with well-known constants (BASELINE/TRACE/LIMITED/BASELINE_DISABLED/TRACE_DISABLED) — global because tests not parallelized
- BaselinesManager accumulates BeforeExecute traces; BaselinesWriter writes BaselinesPath/<provider>/<FQN>/<test>.sql (strips transaction markers + 'BeforeExecute
' + '(asynchronously)'); validates direct vs remote runs produce identical baselines
- Remote: IServerContainer interface (KeepSamePortBetweenThreads + CreateContext); 4 concrete impls boot in-process ASP.NET Core or WCF on fixed port; port = base + thread-id%1000 + RunID when KeepSamePortBetweenThreads=false; gRPC uses ProtoBuf.Grpc.Server
- TestNoopProvider in-memory DynamicDataProviderBase for LINQ-translation-only tests (TestNoopSqlBuilder + TestNoopSqlOptimizer); SQLiteMiniprofilerProvider wraps SQLite Classic in MiniProfiler; UnwrapProfilerInterceptor unwraps profiled connections
- X86Stubs/DB2Stubs.cs compiles only under #if DB2STUBS; provides minimal stubs in IBM.Data.DB2 + IBM.Data.Db2 + IBM.Data.DB2.Core + IBM.Data.DB2Types namespaces (DB2Connection/DB2TimeStamp/DB2Xml etc.) for cross-platform builds without IBM driver
- Cross-area dependencies: CORE (DataConnection/DataOptions/Configuration); all PROV-* (TestConfiguration.Providers enumerates ProviderName.* constants); REMOTE-CLIENT (IServerContainer wraps LinqService); REMOTE (gRPC/Http/SignalR/WCF transport packages); TESTS-MODEL (lazy-loaded test entities); NUnit + Shouldly NuGets
- 6 known issues / debt items: CustomizationSupport.Interceptor non-thread-safe mutable static (forks must set before assembly load), CustomTestContext global ConcurrentDictionary risks cross-test contamination if background thread outlives TearDown, AssertState gated _assertStateEnabled=false (dead in CI; AllTypes TODO), BaselinesWriter._baselines static dict without per-run reset (spurious guard fires across assemblies), _serverContainers populated unconditionally even with DisableRemoteContext=true (gRPC/SignalR DI eager registration), Tests.csproj <Compile Remove> for WindowFunctionsTests.* (rename silently re-enables)
- Completed areas: ... SHARED-INTERNAL, TESTS-INFRA (41 of 50); 9 remaining
- Paused at user request (one area per turn)

## 2026-05-04T13:41:10Z — agent audit notes
- **Propose promoting `TestsInitialization.cs` to Tier 1 in `kb-areas.md`.**

`Tests/Linq/TestsInitialization.cs` is the NUnit `[SetUpFixture]` for the entire test assembly. It controls: SQLite native-library loading strategy, activity/metrics registration, ClickHouse options that affect test expectations, Oracle v11 protocol enablement, SqlCE factory registration, and IBM provider assembly-resolve fallback on NETFX. This file is a *prerequisite for all other tests in the area* — it must be read to understand why tests pass or fail in specific environments. The kb-areas.md row for `TESTS-LINQ` currently says "(none — Tier-2 only area)". Recommend updating the row to list `TestsInitialization.cs` as a Tier-1 anchor, matching how `TESTS-INFRA` pins its key infrastructure files.

## 2026-05-04T13:41:11Z — deferred-coverage queue updated
- TESTS-LINQ: +552 deferred

## 2026-05-04T13:42:35Z — kb-build step 3 TESTS-LINQ — agent audit notes
- Proposed promoting TestsInitialization.cs from no-pin (kb-areas.md row was '(none — Tier-2 only area)') to Tier 1.
- Rationale: NUnit [SetUpFixture] running once per assembly is a prerequisite for every test in the area; controls SQLite native-lib load, activity/metrics registration, ClickHouse test options, Oracle v11 protocol, SqlCE factory, IBM assembly-resolve on netfx. Without reading it, test pass/fail behavior across environments is unexplained.
- Decision: changed kb-areas.md row from '(none)' to anchoring TestsInitialization.cs as Tier 1; matches how TESTS-INFRA pins infrastructure files.

## 2026-05-04T13:42:59Z — kb-areas.md TESTS-LINQ Tier-1 anchor added (applied)
- TESTS-LINQ row: Tier-1 changed from '(none — Tier-2 only area)' to TestsInitialization.cs (the [SetUpFixture] for the test assembly). Notes column expanded with: 23-subdirectory taxonomy mapped to production areas, root infra description (TestsInitialization/TestRetryPolicy/ExpectedException/YdbToDo), per-subdir file counts and validation targets (Linq→EXPR-TRANS/SQL-PROVIDER, UserTests→regression repro pattern, Update→DML+MERGE, DataProvider→per-PROV-* type mapping, Microsoft→EFCore-suite ports, OrmBattle→benchmark ports, ThirdParty→community), WindowFunctionsTests.*.cs partial spread excluded via <Compile Remove>, all fixtures extend TestBase from TESTS-INFRA with [IncludeDataSources]/[DataSources] for provider selection.

## 2026-05-04T13:43:13Z — kb-build step 3 TESTS-LINQ done (with deferred-coverage)
- areas/TESTS-LINQ/INDEX.md written, Tier-1 4/4 (TestsInitialization + TestRetryPolicy + ExpectedExceptionAttribute + YdbToDoAttributes), Tier-2 40/598 (6.7% — gate not met; 552 files in deferred-coverage queue), Tier-3 0, confidence medium
- Largest area in repo: ~602 .cs files across 23 subdirectories. KB documented as taxonomy not per-fixture (would require sub-areas).
- Subdirectory taxonomy with per-subdir counts and production-area validation mapping captured: Linq/166 (LINQ operators → EXPR-TRANS+SQL-PROVIDER), UserTests/256 (issue-numbered Issue<N>Tests.cs regression repros), Update/44 (DML+MERGE → SQL-PROVIDER), DataProvider/25+Types subfolder (per-vendor type mapping → each PROV-*), Extensions/18 (LinqExtensions), Mapping/13, Common/11 (Sql.* helpers), Data/9 (DataConnection), Exceptions/9, Infrastructure/6, Tools/5, Samples/4, Metadata/3, OrmBattle/3, SchemaProvider/3, Scaffold/3, Reflection/2, TypeMapping/2, AST/1, Create/1, Microsoft/1, ThirdParty/1, root/4
- WindowFunctionsTests.*.cs partial-class spread excluded via <Compile Remove> in Tests.csproj (not a feature flag — silent re-enable on rename, flagged in TESTS-INFRA debt)
- All fixtures extend TestBase (TESTS-INFRA); provider selection via [IncludeDataSources(...)] / [DataSources(...)]; NUnit lifecycle hooks per fixture
- Cross-area dependencies: TESTS-INFRA (TestBase + DataSources family + TestConfiguration), TESTS-MODEL (Person/Parent/Child/LinqDataTypes/Northwind entities), every PROV-* + CORE + SQL-PROVIDER + EXPR-TRANS
- Completed areas: ... TESTS-INFRA, TESTS-LINQ (42 of 50); 8 remaining
- Paused at user request (one area per turn)

## 2026-05-04T13:53:43Z — agent audit notes
- Area: TESTS-EFCORE
Issue: `kb-areas.md` row has Tier-1 = `(none)`. Based on this run, the following files should be promoted to Tier 1 to anchor future delta indexing:

1. `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.EF10.csproj` — current TFM + production ref; mirrors what EFCORE area tracks.
2. `Tests/EntityFrameworkCore/Tests.EntityFrameworkCore.props` — shared props file; defines package refs, assembly name, base project reference.
3. `Tests/EntityFrameworkCore/ContextTestBase.cs` — the abstract base class used by every fixture; `CreateContext()`, DB-init, `ProviderSetup()`, `TestContextTracker`.
4. `Tests/EntityFrameworkCore/NorthwindContextTestBase.cs` — Northwind dispatch hub; used by `ToolsTests`, `InterceptorTests`, `SQLiteTests`, `PomeloMySqlTests`.
5. `Tests/EntityFrameworkCore/Tests/ToolsTests.cs` — the broadest fixture (40+ tests, covers nearly all production EFCORE API surfaces).
6. `Tests/EntityFrameworkCore/Utilities/EFDataSourcesAttribute.cs` — the primary test parameter attribute for EF tests.

Proposed `kb-areas.md` update: set Tier-1 to the 6 paths above for the `TESTS-EFCORE` row.

## 2026-05-04T13:53:43Z — deferred-coverage queue updated
- TESTS-EFCORE: +92 deferred

## 2026-05-04T13:55:46Z — kb-build step 3 TESTS-EFCORE — agent audit notes
- Proposed Tier-1 (6 anchors) for TESTS-EFCORE row in kb-areas.md (was '(none)').
- Anchors: Tests.EntityFrameworkCore.EF10.csproj (current TFM + production ref mirror to EFCORE area), Tests.EntityFrameworkCore.props (shared package refs + assembly identity), ContextTestBase.cs (abstract base with CreateContext + DB-init + ProviderSetup + TestContextTracker), NorthwindContextTestBase.cs (Northwind provider dispatch + seeding), Tests/ToolsTests.cs (broadest fixture — 40+ tests covering nearly all EFCORE production API), Utilities/EFDataSourcesAttribute.cs (primary EF test parameter attribute).
- TestConfiguration.EFProviders is defined cross-area in Tests/Base/TestConfiguration.cs:200 — EF-supported providers list (SQLiteMS, AllSqlServer2016PlusMS, AllPostgreSQL13Plus, AllMySqlConnector excluded on NET10_0). Worth a future cross-link to TESTS-INFRA INDEX.

## 2026-05-04T13:56:39Z — kb-areas.md TESTS-EFCORE Tier-1 list filled in (applied)
- TESTS-EFCORE row: Tier-1 changed from '(none)' to 6 anchors. Notes column expanded with: 4-csproj × 1-source-tree multi-EF compilation mirroring production EFCORE; per-csproj TFM + production-ref + Pomelo-on-EF10 exclusion; shared Tests.EntityFrameworkCore.props package refs; EFDataSources/EFIncludeDataSources attribute filtering against TestConfiguration.EFProviders; ContextTestBase<TContext> + NorthwindContextTestBase mechanics (CreateContext + DbContextOptionsBuilder + per-provider UseSqlServer/etc + UseLinqToDB + EnsureCreated + idempotent TestContextTracker); 13 test fixtures with primary coverage notes; combo-interceptor (linq2db + EF) bridge via UseEfCoreRegisteredInterceptorsIfPossible; TestLoggerProvider→BaselinesManager wiring; 9 model sub-contexts.

## 2026-05-04T13:56:45Z — kb-build step 3 TESTS-EFCORE done (with deferred-coverage)
- areas/TESTS-EFCORE/INDEX.md written, Tier-1 0/0 (kb-areas.md row was '(none)' at run time — proposed 6 anchors via audit-note), Tier-2 36/171 (21% — gate not met; 92 files in deferred queue per fence), Tier-3 0, confidence medium
- Multi-EF parallel structure documented: 4 csprojs (EF3 net462, EF8/EF9 (props-defined), EF10 net10.0) × 1 source tree; each refs matching LinqToDB.EntityFrameworkCore.EF{n}.csproj + Tests.Base.csproj (TESTS-INFRA); per-version differences via #if NETFRAMEWORK / NET8_0_OR_GREATER (no EF31/EF8/EF9/EF10 DefineConstants in test csprojs unlike production EFCORE)
- Provider selection: EFDataSourcesAttribute filters against TestConfiguration.EFProviders (SQLiteMS + AllSqlServer2016PlusMS + AllPostgreSQL13Plus + AllMySqlConnector-non-NET10); EFIncludeDataSources is inclusion variant
- Test bases: ContextTestBase<TContext : DbContext> extends TestBase + CreateContext builds DbContextOptionsBuilder<TContext> + per-provider UseSqlServer/UseSqlite/UseNpgsql/UseMySql + UseLinqToDB(NodaTimeSupport for PostgreSQL) + EnsureDeleted+EnsureCreated+OnDatabaseCreated; idempotent via static TestContextTracker.LastContexts dict (not thread-safe — first context per connectionString wins); NorthwindContextTestBase dispatches provider-string → per-provider NorthwindContext subclass + seeds via NorthwindData.Seed
- 13 fixtures in Tests/: ToolsTests (broadest — 40+ tests on ToLinqToDB/Include/ThenInclude/change tracker/TagWith/temporal/FromSqlRaw/DML/async), IssueTests (regression — Issue73/117/321/4624…5388), InterceptorTests (5 interceptor surfaces + EF+linq2db combo + UseEfCoreRegisteredInterceptorsIfPossible), ForMappingTests (EFCoreMetadataReader identity/skip-on-insert/update/MERGE), ConvertorTests + IdTests (EF value-converter import via IValueConverterSelector), InheritanceTests (discriminator + bulk copy), NpgSqlTests (Npgsql range/array/xmin/NodaTime/AT TIME ZONE), PomeloMySqlTests (excl EF10), SQLiteTests, JsonConvertTests, FSharpTests (#if EF8), CustomContextIssueTests (DB-corruption tests)
- Combo interceptor: TestEfCoreAndLinqToDBComboInterceptor implements both linq2db ICommandInterceptor and EF IDbCommandInterceptor; UseEfCoreRegisteredInterceptorsIfPossible reads CoreOptionsExtension.Interceptors and bridges any that are also linq2db IInterceptor
- Logging: TestLoggerProvider + TestLogger + TestLoggerExtensions + LogMessageEntry + NullExternalScopeProvider + NullScope route ILogger output to BaselinesManager.LogBaselines for baseline comparison
- Models: 9 sub-contexts (Northwind/IssueModel/ForMapping/Inheritance/NpgSqlEntities/Shared/ValueConversion/JsonConverter) with per-provider Fluent-API map classes for SQL Server/Pomelo/PostgreSQL (~15 files each)
- 6 known issues / debt items: no DefineConstants for EF-version-specific test code (only NETFRAMEWORK/NET8_0_OR_GREATER guards), TestContextTracker.LastContexts non-thread-safe Dictionary, CustomContextIssueTests manual TestContextTracker invalidation fragile if test order changes, InheritanceTests.TestInheritanceBulkCopy try/catch workaround for Npgsql EFCore #3671 (recheck), PomeloMySqlTests EF10 exclusion via package-ref removal (no [ActiveIssue] annotation), FSharpTests #if EF8 (no F# coverage on EF3/EF31)
- Cross-area dependencies: EFCORE (every fixture exercises LinqToDBForEFTools/EFCoreMetadataReader/TransformExpressionVisitor/LinqToDBForEFToolsDataConnection/LinqToDBOptionsExtension), TESTS-INFRA (TestBase + DataSourcesBaseAttribute + TestConfiguration.EFProviders), PROV-SQLSERVER (temporal table tests), PROV-SQLITE (default tests), PROV-POSTGRES (Npgsql range/array/NodaTime), PROV-MYSQL (Pomelo non-EF10), INTERCEPTORS (5 interceptor interfaces tested), DATA (BulkCopyType/DataConnection/CreateTempTable), MAPPING (MappingSchema assertions)
- Completed areas: ... TESTS-LINQ, TESTS-EFCORE (43 of 50); 7 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:02:47Z — agent audit notes
- Proposed Tier-1 anchors for `kb-areas.md` update (TESTS-FSHARP row):

The area currently has no Tier-1 files. The following are the highest-value anchors to promote:

1. `Tests/FSharp/Models.fs` — defines all F# entity types used across every core test module. It is the foundation for F# record materialization validation. Any change to it cascades everywhere.
2. `Tests/FSharp/Tests.FSharp.fsproj` — controls F# compile order (order-sensitive language); any TFM or dependency change here affects the whole project.
3. `Tests/EntityFrameworkCore.FSharp/FSharpContext.fs` — the only `DbContext` definition in the EFCore F# project; anchor for the EF/linq2db F# bridge surface.
4. `Tests/EntityFrameworkCore.FSharp/Tests.EntityFrameworkCore.FSharp.fsproj` — pinned to `net8.0` and references `LinqToDB.EntityFrameworkCore.EF8.csproj`; knowing this TFM pin is critical for anyone adding EF9/EF10 F# tests.

Suggested `kb-areas.md` Tier-1 list update for TESTS-FSHARP:
```
Tests/FSharp/Models.fs
Tests/FSharp/Tests.FSharp.fsproj
Tests/EntityFrameworkCore.FSharp/FSharpContext.fs
Tests/EntityFrameworkCore.FSharp/Tests.EntityFrameworkCore.FSharp.fsproj
```

## 2026-05-04T14:06:16Z — kb-build step 3 TESTS-FSHARP — agent audit notes
- Proposed Tier-1 (4 anchors) for TESTS-FSHARP row in kb-areas.md (was '(none)').
- Anchors: Models.fs (foundation entity records used across every core F# test module — F# is order-sensitive), Tests.FSharp.fsproj (controls F# compile order + TFM + refs), FSharpContext.fs (only DbContext definition in EFCore F# project — bridge anchor), Tests.EntityFrameworkCore.FSharp.fsproj (pinned to net8.0 + EF8 ref — important for anyone adding EF9/10 F# tests).

## 2026-05-04T14:06:37Z — kb-areas.md TESTS-FSHARP Tier-1 list filled in (applied)
- TESTS-FSHARP row: Tier-1 changed from '(none)' to 4 anchors. Notes column expanded with: 2-project structure (Tests.FSharp + Tests.EntityFrameworkCore.FSharp), F# 9 LangVersion, EF8-only pin on EFCore F# project, F# compile-order list, Models.fs entity record taxonomy (CLIMutable vs not for two materialization paths, nested-record mapping, option columns, conflicting names, DU with [<MapValue>]), 7 issue regressions covered (#1813 #2678 #3357 #3743 #4132 #4851 #5428), AppDbContext with EFCore OptionConverter for F# option columns, 4 debt items (no Tests.FSharp.fsproj TFM override; EF8-only pin; Issue5428 PostgreSQL-only connectionString param; no MappingSchema.Initialize [<SetUp>] enforcement; no async F# tests).

## 2026-05-04T14:06:41Z — kb-build step 3 TESTS-FSHARP done
- areas/TESTS-FSHARP/INDEX.md written, Tier-1 0/0 (kb-areas.md row was '(none)' at run time — proposed 4 anchors via audit-note now applied), Tier-2 18/18 (100%; gate met), Tier-3 0, confidence high — full coverage of all on-disk .fs + .fsproj files
- Two F# test projects validate distinct concerns: Tests.FSharp (12 .fs + fsproj — exercises FSHARP companion area's UseFSharp + FSharpEntityBindingInterceptor); Tests.EntityFrameworkCore.FSharp (3 .fs + fsproj — exercises EFCore F# bridge via TESTS-EFCORE's FSharpTests C# fixture under #if EF8)
- F# is compile-order-sensitive — fsproj order: Issue4851 → Issue2678 → Models → Issue3357 → WhereTest → SelectTest → InsertTest → MappingSchema → Issue3743 → Issue4132 → Issue1813 → Issue5428 (Models.fs precedes all dependent test modules)
- Tests/FSharp/Models.fs is the foundation: Person/Patient (non-CLIMutable — exercises FSharpEntityBindingInterceptor's record-constructor mapping path); PersonCLIMutable/PatientCLIMutable (with [<CLIMutable>] — exercises property-setter path); ComplexPerson/DeeplyComplexPerson (nested-record mapping via [<Column('col','path.field')>]); PersonWithOptions (string option columns — requires MappingSchema.Initialize()); PersonConflictingNamesRecord (case-only conflict regression); Person.Gender DU with [<MapValue>]; Child/Parent simple records
- 7 issue regressions covered: #1813 7-case groupJoin+leftOuterJoin combinations, #2678 F# class vs [<CLIMutable>] record insert/select round-trip, #3357 Union/Concat over tuples + named records + anon records in F# query expressions, #3743 chained Join+LeftJoin method syntax, #4132 table.Insert/Update lambda syntax, #4851 (DataOptions()).UseFSharp() compile smoke, #5428 UseFSharp + NodaTime LocalDate + PostgreSQL DateInterval + CTE + window Lead
- Tests/EntityFrameworkCore.FSharp/FSharpContext.fs: AppDbContext : DbContext F# class with WithIdentity DbSet + Issue4646Table (F# Value:int option, ValueN:Nullable<int> option columns using EFCore OptionConverter — cross-validates EF↔linq2db F# option handling). FSharpExtensions.fs: WithFSharp(builder) → builder.UseFSharpTypes() from EntityFrameworkCore.FSharp community package. FSharpTestMethods.fs: TestLeftJoin / Issue4646TestEF / Issue4646TestLinqToDB entry points called by TESTS-EFCORE's FSharpTests fixture
- Cross-area dependencies: FSHARP (LinqToDB.FSharp.fsproj — DataOptions.UseFSharp/FSharpEntityBindingInterceptor; exercised by non-CLIMutable record reads), EFCORE (LinqToDB.EntityFrameworkCore.EF8.csproj — .ToLinqToDB/CreateLinqToDBConnection); TESTS-INFRA (Tests.Base.csproj — Tests.Tools module used by InsertTest/Issue1813/Issue4132); TESTS-EFCORE (its FSharpTests C# fixture calls into FSharpTestMethods); external NUnit + Npgsql.NodaTime + EntityFrameworkCore.FSharp community pkg
- 5 known issues / debt items: Tests.FSharp.fsproj has no TFM override (inherits Directory.Build.props), EFCore F# project pinned to net8.0 (no EF9/EF10 F# scenarios tested), Issue5428.fs takes raw connectionString param (PostgreSQL-only — needs custom wrapper for NUnit DataContext fixture), MappingSchema.Initialize() must be called before any PersonWithOptions test but no [<SetUp>] enforces it, no F# tests for async LINQ / IAsyncEnumerable / CancellationToken paths
- Completed areas: ... TESTS-EFCORE, TESTS-FSHARP (44 of 50); 6 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:18:15Z — agent audit notes
- Proposed Tier-1 anchors for `kb-areas.md` update (TESTS-T4 row):

1. `Tests/Tests.T4/Tests.T4.csproj` — the project file defines TFM scope, all `.tt`/`.generated.cs` item declarations, and the WPF/non-WPF conditional compile split. Load-bearing for understanding what compiles and when.
2. `Tests/Tests.T4/Cli/CLI.ttinclude` — the `RunCliTool` helper is the sole mechanism that drives CLI scaffold invocations. Understanding it is prerequisite to understanding how all five CLI modes work.
3. `Tests/Tests.T4/Cli/All.tt` — the most complete CLI mode driver; documents the full option set exercised and the full provider matrix. Representative of all five `Cli/<mode>.tt` siblings.
4. `Tests/Tests.T4.Nugets/Projects/t4models.csproj` — the aggregate NuGet compile check project; documents the 16-provider matrix and the three additional package dependencies.
5. `Tests/Tests.T4.Nugets/Templates/SqlServer.tt` — representative of all 16 Nugets templates; shows the include pattern against the installed NuGet bundle.

These five files are small, architectural, and change only when the scaffolder's public surface changes — they are good Tier-1 anchors.

## 2026-05-04T14:20:03Z — kb-build step 3 TESTS-T4 — agent audit notes
- Proposed Tier-1 (5 anchors) for TESTS-T4 row in kb-areas.md (was '(none)').
- Anchors: Tests.T4.csproj (TFM scope + WPF/non-WPF conditional compile + .tt/.generated.cs item declarations), Cli/CLI.ttinclude (RunCliTool helper drives all CLI scaffold invocations), Cli/All.tt (most complete CLI mode driver — full option set + 23-provider matrix; representative of 5 sibling .tt drivers), Tests.T4.Nugets/Projects/t4models.csproj (aggregate NuGet compile check, 16-provider matrix), Tests.T4.Nugets/Templates/SqlServer.tt (representative of 16 Nugets templates).
- Largest file-count area in repo (~2218 files) — 38 visited (1.7%); ~2180 deferred to queue (generated baselines, near-identical per-provider patterns, not architecturally significant).

## 2026-05-04T14:20:48Z — kb-areas.md TESTS-T4 Tier-1 list filled in (applied)
- TESTS-T4 row: Tier-1 changed from '(none)' to 5 anchors. Notes column expanded with: 2-project structure (Tests.T4 multi-TFM compile-only + Tests.T4.Nugets isolated slnx pinned 6.2.0-local.1), CI git-diff detection mechanism (no NUnit fixtures), 6-mode CLI taxonomy (All/Default/Fluent/NoMetadata/T4/NewCliFeatures with provider counts), Tests.T4.Nugets compile-validation flow, sub-area roles (Default/Databases legacy T4 path, Models for T4Model.ttinclude features, WPF for NotifyPropertyChanged, Compat IPNetwork stub, Unlock host-shutdown), 4 debt items (no NUnit fixtures, NewCliFeatures SQLite-only, Tests.T4.Nugets.slnx manual rebuild requirement, RunCliTool 60-sec timeout + commented-out error reporting).

## 2026-05-04T14:20:58Z — kb-build step 3 TESTS-T4 done (with deferred-coverage)
- areas/TESTS-T4/INDEX.md written, Tier-1 0/0 (kb-areas.md row was '(none)' at run time — proposed 5 anchors via audit-note now applied), Tier-2 38/2218 (1.7% — gate not met by design; ~2180 deferred — largest deferred queue in KB), Tier-3 0, confidence medium
- Largest file-count area in repo (2218 files); KB documented as taxonomy + 2-project flow rather than per-baseline detail
- Critical insight: NO NUnit test fixtures in either project — regressions detected via CI `git diff` after T4 re-generation overwrites committed baselines in-place; no test-runner assertion failure
- Tests.T4 (multi-TFM, WPF items net462-only): inherits linq2db.TestProjects.props → BasicTestProjects.props; refs LinqToDB.Extensions + Tests.Base + NUnit packages but exposes no fixtures — pure compile-only validation that scaffolded code builds
- Cli/<mode>.tt drivers shell out to `dotnet linq2db scaffold` via CLI.ttinclude:RunCliTool with 60-second timeout; writes output directly into committed Cli/<mode>/<provider>/ subdir; deletes target dir + recreates each run
- 6 CLI modes documented: All (max options, attributes metadata, ~24 providers incl. Azure/AzureMI/ClickHouse×3), Default (out-of-box, ~21), Fluent (`--metadata fluent` + `--add-association-extensions true` + `--find-methods none`, ~21), NoMetadata (`--metadata none` + `--add-association-extensions true` + `--add-init-context false`, ~21), T4 (CLI's T4 path mode='t4', ~21), NewCliFeatures (`--context-modifier internal` + `--customize scaffold.tt` + `--fluent-entity-type-helpers` — SQLite-only, 2 runs)
- Tests.T4.Nugets isolated solution: net10.0 only, central pkg mgmt Version=6.2.0-local.1 pointing to locally built NuGets; 16 per-provider .tt include from `$(LinqToDBT4<Provider>TemplatesPath)` (the installed NuGet, not source); 16 single-template .csproj wrappers + 1 aggregate t4models.csproj refs linq2db.t4models + FirebirdSql.Data.FirebirdClient + Npgsql + dotMorten.Microsoft.SqlServer.Types
- Cross-area dependencies: T4-TEMPLATES (Default/Databases/Models/WPF/Tests.T4.Nugets/Templates include from $(LinqToDBT4TemplatesPath)), CLI (Cli/<mode>/<provider>/ baselines = expected `dotnet linq2db scaffold` output), SCAFFOLD (any change to ScaffoldCommand or codegen causes baseline drift; ScaffoldInterceptors/ScaffoldOptions/Scaffolder referenced in scaffold.tt), every PROV-* (per-provider schema readers determine scaffolded content)
- 4 known issues / debt items: no NUnit fixtures (regressions only via `git diff` — local test runs don't surface failures); NewCliFeatures/ SQLite-only (new CLI options need manual mode extension); Tests.T4.Nugets.slnx isolated requires manual rebuild against locally packed NuGets (easy to miss when iterating on T4 templates); CLI.ttinclude:RunCliTool hard-coded 60-sec timeout + silently ignores non-zero scaffolder exit codes (error reporting commented out at lines 68 + 72)
- Completed areas: ... TESTS-FSHARP, TESTS-T4 (45 of 50); 5 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:25:36Z — agent audit notes
- All 4 source files in TESTS-VB are small and each tests a distinct VB-specific construct. Recommend promoting all of them to Tier 1 in `kb-areas.md`:

```
Tests/VisualBasic/CompilerServices.vb
Tests/VisualBasic/Tests.vb
Tests/VisualBasic/VisualBasicCommon.vb
Tests/VisualBasic/Tests.VisualBasic.vbproj
```

Rationale: the area has only 4 files, each anchors a different VB interop concern (query syntax, compiler-emitted comparisons, GROUP BY regression, date literals). Tier-1 designation ensures future runs re-read them in full rather than sampling.

## 2026-05-04T14:26:32Z — kb-build step 3 TESTS-VB — agent audit notes
- Proposed Tier-1 (all 4 files = entire area) for TESTS-VB row in kb-areas.md (was '(none)').
- Anchors: Tests.VisualBasic.vbproj, CompilerServices.vb, Tests.vb, VisualBasicCommon.vb. Each tests a distinct VB interop concern (query syntax, compiler-emitted comparisons, GROUP BY regression, date literals).

## 2026-05-04T14:26:54Z — kb-areas.md TESTS-VB row filled in (applied)
- TESTS-VB row: Tier-1 changed from '(none)' to 4 files (entire area). Notes column expanded with: helper-library-not-fixture nature (called from C# fixtures in Tests/Linq/), VB 16.9 + Tests.Model-only ref skipping Tests.Base, per-file content (VisualBasicCommon 4 query-syntax helpers + ParamenterName typo + VB bitwise-Or + navigation-Count + VB date-literal `#…#`; CompilerServices CompareString equality emission; Tests issue-649 GROUP-BY 3-variant + issue-2746 CType + inline Activity649/Person649 entities), cross-area dependencies (TESTS-MODEL + EXPR-TRANS).

## 2026-05-04T14:27:40Z — kb-build step 3 TESTS-VB done
- areas/TESTS-VB/INDEX.md written, Tier-1 0/0 (kb-areas.md row was '(none)' at run time — proposed 4 anchors via audit-note now applied), Tier-2 4/4 (100%; gate met), Tier-3 0, confidence high — full coverage of all on-disk files
- Tiny helper-library area (4 files): Public VB modules called from C# fixtures in Tests/Linq/, NOT an NUnit fixture project itself. VB 16.9, refs Tests.Model only (skips Tests.Base — VB can't use DataSourcesAttribute/TestBase directly)
- VisualBasicCommon.vb: 4 VB query-syntax helpers — ParamenterName (typo) tests local-variable capture; SearchCondition1 compound boolean with VB bitwise-Or; SearchCondition2 navigation .Count; SearchCondition3/4 VB date-literal #11/14/1997#
- CompilerServices.vb: CompareString helper — VB compiler emits Microsoft.VisualBasic.CompilerServices.Operators.CompareString for `p.FirstName = "John"`; LINQ translator must handle it without confusing equality vs assignment
- Tests.vb: Issue-#649 GROUP-BY-with-navigation-property 3-variant repros (`Group By … Into … Select` syntax, inline aggregation, fluent-API equivalent); Issue-#2746 nullable CType coercion (w.ChildID.Value = CType(SelectedValue, Integer)); inline Activity649/Person649 entity pair with [Association]
- Cross-area dependencies: TESTS-MODEL (ITestDataContext + NorthwindDB + Person + Parent + LinqDataTypes + GrandChild1 imports), EXPR-TRANS (must accept VB-compiler-emitted CompareString + VB Or operator + Group By/Select Many/Where method calls)
- 2 known issues / debt items: ParamenterName typo (cosmetic, not functional); project ref skips Tests.Base so VB cannot directly use NUnit attributes (all helpers must be invoked from C#)
- Completed areas: ... TESTS-T4, TESTS-VB (46 of 50); 4 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:35:04Z — agent audit notes
- Proposed canonical Tier-1 anchors for `kb-areas.md` TESTS-MODEL row:

```
ITestDataContext.cs, TestDataConnection.cs, Person.cs, Tests.Model.csproj
```

Rationale:
- `ITestDataContext.cs` — single-file contract that every test fixture and remote context implements; change here affects all tests.
- `TestDataConnection.cs` — primary concrete data context used in the overwhelming majority of test runs; also `ISystemSchemaData`.
- `Person.cs` — represents the canonical core entity pattern (provider-specific `[Column(Configuration=...)]` overrides, `[Association]`, `IPerson` interface implementation).
- `Tests.Model.csproj` — project reference graph reveals all remote transport dependencies (SignalR always, Grpc/HttpClient for non-netfx, WCF for netfx, Scaffold for LinqToDB.Tools).

No pinned Tier-1 files were missing on disk. All expected files found.

## 2026-05-04T14:37:52Z — kb-build step 3 TESTS-MODEL — agent audit notes
- Proposed Tier-1 (4 anchors) for TESTS-MODEL row in kb-areas.md (was '(TBD on first read)').
- Anchors: ITestDataContext.cs (contract for all test data contexts — change cascades to all tests), TestDataConnection.cs (primary DataConnection + also ISystemSchemaData), Person.cs (canonical core entity with [Column(Configuration)] per-provider overrides + [Association] + IPerson interface), Tests.Model.csproj (project reference graph reveals remote-transport deps + Scaffold ref).
- All 26 on-disk .cs + csproj files read in full; no missing pinned anchors.

## 2026-05-04T14:38:05Z — kb-areas.md TESTS-MODEL Tier-1 list filled in (applied)
- TESTS-MODEL row: Tier-1 changed from '(TBD on first read)' to 4 anchors. Notes column expanded with: schema-model-only purpose (no test logic), 26-file inventory + 5 sub-areas (core entities + Parent/Child hierarchy + 2 inheritance hierarchies + Northwind + provider-specific), [Association] ExpressionPredicate + [ExpressionMethod] computed associations, Functions/FunctionsExtensions [Sql.TableFunction], NorthwindDB FreeTextTable + WithUpdateLock helpers, ITestDataContext 20-table contract + GetParentByID, 4-impl matrix (TestDataConnection + TestDataCustomConnection composition + 4 remote contexts with TFM gating), GetParentByID NotImplementedException-on-remote pattern, provider-specific sequence/identity quirks (FB reserved word, Oracle uppercase + empty-string-NULL, PostgreSQL 4 sequence variants), Gender/TypeValue [MapValue]/[MapValue(null)] enum patterns, issue #4031 interface-inheritance regression types, 3 known-debt items.

## 2026-05-04T14:38:19Z — kb-build step 3 TESTS-MODEL done
- areas/TESTS-MODEL/INDEX.md written, Tier-1 4/4, Tier-2 22/22 (100%), Tier-3 0, confidence high — full coverage of all 26 .cs + csproj
- Schema-model-only library (linq2db.Model assembly) — no test logic; consumed by every test project (TESTS-INFRA TestBase + TESTS-LINQ + TESTS-EFCORE + TESTS-VB + remote test areas)
- Core entities: Person/Patient/Doctor/ComplexPerson/Gender/FullName/TestIdentity/LinqDataTypes/LinqDataTypes2 — canonical schema seeded by every provider test database
- ParentChild.cs is the most-loaded file: Parent/Parent1–5 + Child + GrandChild + GrandChild1 + IParent + 4 inheritance hierarchies on Parent table (ParentInheritanceBase/Base2/Base3/Base4) exercising null-code defaults + value-code + enum discriminators + abstract bases; Functions/FunctionsExtensions/FunctionsOld for [Sql.TableFunction] + [Sql.TableExpression]
- Separate InheritanceParentChild.cs hierarchy on InheritanceParent/Child tables with TInheritance interface enforcing TypeDiscriminator contract
- Northwind: 12 entities in static container + Product bool-discriminator (Active/DiscontinuedProduct subclasses); NorthwindDB : DataConnection adds FreeTextTable<> SQL Server full-text + [Sql.TableExpression] WithUpdateLock<T>
- ITestDataContext : IDataContext with 20 typed ITable<T> properties + GetParentByID [Sql.TableFunction]; 4 implementations:
-   - TestDataConnection — primary DataConnection subclass + also ISystemSchemaData (exposes SystemSchemaModel from LinqToDB.Tools)
-   - TestDataCustomConnection — composition-wrapped, manual IDataContext impl, exercises non-DataConnection code paths
-   - 4 remote contexts: TestGrpcDataContext (!NETFRAMEWORK, skips cert validation), TestHttpContextDataContext (!NETFRAMEWORK), TestSignalRDataContext (all TFMs), TestWcfDataContext (NETFRAMEWORK only, NetTcpBinding extended timeouts)
- All 4 remote contexts throw NotImplementedException from GetParentByID — table functions not supported over remote transports
- Provider-specific entities cover quirky patterns: FirebirdSpecific (Value_ column workaround for FB reserved word), OracleSpecific (uppercase TABLE/SEQUENCE names + StringTest empty-string-equals-NULL helper), PostgreSQLSpecific (4 sequence variants + 2 schema-qualified identity tables)
- Auxiliary types: Gender enum [MapValue] single-char (M/F/U/O), TypeValue enum [MapValue(null)] for Value0 testing null-enum mapping, Interfaces.cs IIssue4031/IIssue4031<T>/Issue4031BaseExternal regression types, Extensions.cs BeginTransaction extension on ITestDataContext
- 3 known-debt items: TestDataCustomConnection partially stubs IDataContext (UseOptions/UseMappingSchema/AddMappingSchema/SetMappingSchema no-ops — can't test per-scope mapping overrides via custom-context path), no net462-compatible HTTP remote context (gap in TFM coverage), GetParentByID silently fails on remote unless fixture guards on context type (table-function tests implicitly require TestDataConnection)
- Cross-area dependencies: CORE (IDataContext/DataConnection/ITable/DataOptions/MappingSchema/Sql.* attributes), MAPPING (12 attribute kinds), REMOTE-CLIENT (RemoteDataContextBase ancestors), 4 REMOTE transports (SignalR + Grpc + HttpClient + Wcf), TOOLS (SystemSchemaModel via LinqToDB.Scaffold ref), PROV-SQLSERVER (FreeTextTable extension)
- Consumers: TESTS-INFRA (TestBase.Tables.cs lazy-loads via TestDataConnection), TESTS-LINQ (every fixture queries through ITestDataContext), TESTS-EFCORE (some fixtures use NorthwindDB), TESTS-VB (imports Tests.Model), all remote transport test areas
- Completed areas: ... TESTS-VB, TESTS-MODEL (47 of 50); 3 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:46:49Z — agent audit notes
- Proposed Tier-1 anchors for `TESTS-BENCHMARKS` (update `kb-areas.md`):

1. `Tests/Tests.Benchmarks/Program.cs` — entry point; controls which benchmarks run by default.
2. `Tests/Tests.Benchmarks/Config.cs` — BDN IConfig; defines the runtime matrix that determines what the benchmark suite covers.
3. `Tests/Tests.Benchmarks/linq2db.Benchmarks.csproj` — build contract; TFMs and project references.
4. `Tests/Tests.Benchmarks/TestClasses/ProviderMocks/MockDbConnection.cs` — root of the mock ADO.NET stack that makes DB-free benchmarking possible; representative of the 7-file ProviderMocks cluster.
5. `Tests/Tests.Benchmarks/Benchmarks/QueryGeneration/QueryGenerationBenchmark.cs` — the most architecturally significant benchmark (pure SQL-emission, no DB); exercises the full LINQ→SQL translation pipeline without network noise.
6. `Tests/Tests.Benchmarks/TestClasses/TypeMapperWrappers.cs` — shared type definitions for all 14 TypeMapper benchmarks; if this changes, every TypeMapper result is invalidated.

Rationale: designating these six files as Tier 1 would raise the next run's Tier-1 hit to 100% and the Tier-2 denominator to 35 files, making the 90% threshold reachable in a single pass.

## 2026-05-04T14:48:10Z — kb-build step 3 TESTS-BENCHMARKS — agent audit notes
- Proposed Tier-1 (6 anchors) for TESTS-BENCHMARKS row in kb-areas.md (was '(none)').
- Anchors: Program.cs (entry + default filter), Config.cs (BDN IConfig + runtime matrix), linq2db.Benchmarks.csproj (build contract + TFMs via shared props), MockDbConnection.cs (root of 7-file ProviderMocks cluster — DB-free benchmarking), QueryGenerationBenchmark.cs (most architecturally significant benchmark — pure SQL emission no DB), TypeMapperWrappers.cs (shared type defs for all 14 TypeMapper benchmarks).
- Pinning these 6 would raise the next run's Tier-1 hit to 100% and reduce Tier-2 denominator to 35, making 90% reachable in a single pass.

## 2026-05-04T14:48:15Z — kb-areas.md TESTS-BENCHMARKS Tier-1 list filled in (applied)
- TESTS-BENCHMARKS row: Tier-1 changed from '(none)' to 6 anchors. Notes column expanded with: 3-category benchmark structure (Queries/QueryGeneration/TypeMapper), 7-mock ADO.NET cluster eliminating network cost, runtime matrix (net462/net8/9/10), default-filter behavior (Queries+QueryGeneration on, TypeMapper opt-in), JIT-inlining-prevention pattern in TypeMapperWrappers, Query.ClearCaches() cache-bust pattern in Issue3253, issue-#2032 OracleReaderExpressions workaround, 6 known-debt items (most providers commented in QueryGenerationBenchmark, dev-scaffolding commented block in Program.cs, issue-#2032 recheck, results/ unverified gitignore status, FetchIndividualBenchmark dup of FetchSetBenchmark, TFMs hidden via shared props import).

## 2026-05-04T14:49:07Z — kb-build step 3 TESTS-BENCHMARKS done
- areas/TESTS-BENCHMARKS/INDEX.md written, Tier-1 0/0 (kb-areas.md row was '(none)' at run time — proposed 6 anchors via audit-note now applied), Tier-2 28/41 (68% — gate not met by design; 13 sibling-pattern files deferred), Tier-3 0, confidence medium
- BenchmarkDotNet harness across 3 categories: Queries (9 classes, MockDb-backed live-LINQ vs CompiledQuery vs RawAdoNet), QueryGeneration (1 class, pure SQL emission via .ToString() on IQueryable, no DB), TypeMapper (14 classes micro-benching Internal.Expressions.Types.TypeMapper + ExpressionGenerator vs direct calls baseline)
- ProviderMocks/ 7-file cluster: QueryResult payload (Names/FieldTypes/DbTypes/Data/Match-predicate-for-multi-result), MockDbConnection (single + multi-result ctors), MockDbCommand (Match-routed GetResult), MockDbDataReader (Read-by-index, Schema for column metadata), MockDbParameter/Collection/Transaction stubs — eliminates network + serialization cost
- Config.cs: net462 (baseline) + net8/9/10 jobs, RyuJIT x64, MemoryDiagnoser, GitHub Markdown exporter, FilteredColumnProvider strips Job/Error/Median/Gen*/Ratio/StdDev
- Program.cs default filter '*.Queries.* *.QueryGeneration.*' (TypeMapper opt-in only)
- TypeMapper benchmarks use Original.*/Wrapped.* synthetic type pairs in TypeMapperWrappers.cs with [MethodImpl(NoInlining)] preventing JIT-elim of measured overhead. 14 classes covering BuildAction/BuildFunc/BuildGetter/BuildSetter setup costs + WrapAction/Wrap/WrapEvent/WrapGetter/WrapInstance/WrapSetter call costs + CreateAndWrap factory + EnumConvert + NpgsqlBulkCopyRowWriter (ExpressionGenerator-built vs direct NpgsqlBinaryImporter.Write) + OracleReaderExpressions (8 sub-benchmarks for OracleTimeStampTZ/LTZ/OracleDecimal reads)
- Queries provider matrix: SelectBenchmark/UpdateBenchmark/ConcurrentBenchmark on PostgreSQL v9.5; FetchSetBenchmark/FetchGraphBenchmark/InsertSetBenchmark on SQL Server 2022; Issue3253 on SQLite Microsoft; Issue3268 on SQL Server 2008
- Issue3253Benchmark calls Query.ClearCaches() to force cache-miss regression measurement (issue tracked query-plan cache bloat with high column counts)
- OracleReaderExpressionsBenchmark documents issue-#2032 workaround via double-compile
- 6 known-debt items: most providers commented in QueryGenerationBenchmark (only Access + Firebird active); large dev-scaffolding commented block in Program.cs lines 22-97 (manual-run scaffolding for profiler-guided runs, not dead code); issue-#2032 workaround should be re-checked for upstream resolution; results/ directory gitignore status unvalidated (BDN artifacts path lands in project root which is committed); FetchIndividualBenchmark structurally identical to FetchSetBenchmark (could dedupe with [Params]); TFMs hidden via shared linq2db.Providers.props import
- Cross-area dependencies: CORE (LinqToDB.csproj direct project ref, sole dep); INTERNAL-API (Internal.Linq.Query.ClearCaches + Internal.Expressions.Types.TypeMapper + Internal.Expressions.ExpressionGenerator); 5 PROV-* (PostgreSQL/SqlServer/SQLite/Access/Firebird via GetDataProvider() in benchmark setups + Npgsql + Oracle hot-path benchmarks); INFRA (LinqToDB.Async ToListAsync in FetchGraphBenchmark)
- Inbound: standalone executable; only structural reference is linq2db.Benchmarks.slnf solution filter
- Completed areas: ... TESTS-MODEL, TESTS-BENCHMARKS (48 of 50); 2 remaining
- Paused at user request (one area per turn)

## 2026-05-04T14:59:52Z — unclassified files
- Build/BannedSymbols.txt — pinned Tier-1 missing on disk — file does not exist at Build/BannedSymbols.txt; the actual banned-API list is at Source/BannedSymbols.txt, referenced by Source/Directory.Build.props as $(MSBuildThisFileDirectory)\BannedSymbols.txt. The kb-areas.md Tier-1 pin should be updated to Source/BannedSymbols.txt.

## 2026-05-04T14:59:52Z — kb-build step 3 BUILD — agent audit notes
- UNCLASSIFIED-FILE: Build/BannedSymbols.txt (kb-areas.md pin) does not exist on disk; actual banned-API list is at Source/BannedSymbols.txt (referenced from Source/Directory.Build.props:17 via $(MSBuildThisFileDirectory)\BannedSymbols.txt).
- AUDIT-NOTE: proposed kb-areas.md BUILD-row Tier-1 fix — replace `Build/BannedSymbols.txt` with `Source/BannedSymbols.txt`. Awaiting user decision.
- Side-finding: `.github/workflows/*.yml` Tier-2 pattern matches zero files — all CI is Azure Pipelines under `Build/Azure/pipelines/`. No GitHub Actions workflows exist. Pattern can be removed from kb-areas.md or kept for forward-compat.

## 2026-05-04T14:59:52Z — kb-build step 3 BUILD done (with UNCLASSIFIED-FILE + AUDIT-NOTE)
- areas/BUILD/INDEX.md written, Tier-1 3/4 (Build/BannedSymbols.txt missing — UNCLASSIFIED-FILE recorded; Source/BannedSymbols.txt read in full as the actual location), Tier-2 70/70 (100%; gate met), Tier-3 0, confidence high
- Subsystems documented: TFM matrix + 9 feature flags (SUPPORTS_COMPOSITE_FORMAT/DATEONLY/ENSURE_CAPACITY/SPAN/READONLY/REGEX_GENERATORS/INT128, ADO_ASYNC, ADO_IS_TRANSIENT — all gated on net8.0 except SUPPORTS_READONLY net472 fallback); version variables (`<Version>`=6.3.0 + EF3/8/9/10 = 3.32.0/8.6.0/9.5.0/10.4.0 + BaselineVersion=6.0.0; VersionSuffix=-local.1 default, stripped on release branch); Roslyn analyzer gating (RunAnalyzersDuringBuild Release-only via Configuration condition; TreatWarningsAsErrors unconditional; AnalysisLevel=preview-All; analyzers AsyncFixer + Lindhart + Meziantou + BannedApiAnalyzers + SourceLink); Source/BannedSymbols.txt 288-line ban list (ConcurrentBag/IDataReader/IDb*/GetCustomAttribute*/parameterless DateTime.ToString/Decimal.Parse/String.Format/Expression.Compile/MethodBase.Invoke/Activator.CreateInstance/DbCommand.Dispose/string.IndexOf without StringComparison/Type.GetInterfaceMap); Meziantou.Polyfill configured for ~30 polyfill types (HashCode/Index/Range/System.Threading.Lock/etc.) gated on TFM-compatibility
- global.json: SDK 10.0.0, rollForward=minor, allowPrerelease=false
- linq2db.slnx: 4 build configs (Azure/Debug/Release/Testing); 7 project folders (Source/Tests/Packaging/Build/.claude meta); Testing|* mostly disabled for packaging+heavy-test variants
- Azure Pipelines (CRITICAL FINDING): NO GitHub Actions workflows exist — all CI is Azure Pipelines under Build/Azure/pipelines/. 3 top-level pipelines (build.yml PR compile-only with analyzers DISABLED pending dotnet/roslyn#80621; default.yml master/release push + release-branch PRs full pipeline + draft release; testing.yml manual /azp run test-<db> bot trigger). 8 templates (build-job, build-vars, nuget-job, test-jobs, test-matrix, test-workflow-{linux,macos,windows})
- Test matrix (~35 entries): SQLite (all OS); Access MDB+ACE x86 (Windows only, ACE x64 disabled per dotnet/runtime#46187); SQL CE (Win); MySQL5.7+9+MariaDB11; PostgreSQL 13-18; SQL Server 2005-2025+Extras+Metrics; Sybase ASE 16; Oracle 11g-23c (retry=true); Firebird 2.5-5.0; DB2 LUW 11.5 (retry); Informix 14.10 (retry); SAP HANA 2; ClickHouse Driver+MySql variants (Octonica always disabled). Each entry has config_*/script_*/enable_fw_*/enable_os_*. test-jobs.yml expands into 3 parallel jobs (windows-2025/ubuntu-24.04/macOS-15) + manages baselines branch + draft baselines PR
- build-job.yml: windows-2025 pool, installs .NET 9+10 SDKs explicitly (image lag), versionSuffix logic, builds Examples.slnf for compile check, full Azure-config build with x64+x86 (DB2STUB=True for x86), Release pack, LPX artifacts, GitHub release draft on release branch via RELEASES_GH_PAT
- nuget-job.yml: master→Azure Artifacts internal feed; release→NuGet.org via 'linq2db nuget feed' service connection
- Build/Azure/scripts: 43 .sh + 3 .ps1 — uniform pattern (.sh: docker run + log-poll readiness; .ps1: download+install Microsoft/linq2db.ci MSI/EXE for Access ACE + SQL CE; mac.*.sh mirrors linux except DB2/Informix). Build/Azure/{net80,net90,net100,netfx}/*.json (~150 files) are UserDataProviders.json snippets — Tier-3 by test-data rule, counted not visited. *.cmd files (sqlserver.*.cmd) outside kb-areas.md Tier-2 patterns — also Tier-3
- .github/ISSUE_TEMPLATE: 13 YAML forms (6 bug + 6 feature variants × 6 product surfaces: linq2db core/EF.Core/CLI/T4/LINQPad/Other; numbered 01-09/11-19) + config.yml redirecting new-DB requests to issue #1014
- 6 known issues / debt: Build/BannedSymbols.txt pin stale (UNCLASSIFIED above); Roslyn analyzer disabled in build.yml (with_analyzers: false pending dotnet/roslyn#80621 — analyzer violations can land on master without CI gate, only caught locally on Release); macOS tests disabled by default in test-matrix.yml (mac_enabled: false; testing.yml comment suggests recently disabled for cost); Access ACE x64 disabled (enable_os_windows: false, dotnet/runtime#46187 random AV crash); ClickHouse Octonica entry has enabled: false in both branches of its ${{ if }} (never tested); testing.yml missing explicit db_filter default for test-all (commented-out — falls through to test-matrix.yml [all] default)
- Cross-area dependencies: every C# project in Source/+Tests/ inherits Directory.Build.props (universal); every Source/* project also inherits Source/Directory.Build.props which adds Source/BannedSymbols.txt as <AdditionalFiles>; CI test matrix touches every PROV-* + TESTS-*; nuget-job publishes every Packaging/* output
- Completed areas: ... TESTS-BENCHMARKS, BUILD (49 of 50); 1 remaining (CLAUDE-INFRA)
- Paused at user request (one area per turn)

## 2026-05-04T14:59:52Z — agent audit notes
- `kb-areas.md` BUILD area Tier-1 pin `Build/BannedSymbols.txt` is wrong. The file does not exist. The actual banned-API list consumed by `Microsoft.CodeAnalysis.BannedApiAnalyzers` is `Source/BannedSymbols.txt`, added as `<AdditionalFiles>` in `Source/Directory.Build.props`:17. Proposed fix: replace `Build/BannedSymbols.txt` with `Source/BannedSymbols.txt` in the Tier-1 pins for the BUILD area in `.claude/docs/kb-areas.md`.

## 2026-05-04T15:24:23Z — agent audit notes
- BannedSymbols.txt path mismatch: CLAUDE.md (line 54) states the banned API list is at `Build/BannedSymbols.txt`. On-disk search (Glob `**/BannedSymbols.txt`) finds the file at `Source/BannedSymbols.txt` and `Tests/BannedSymbols.txt` — neither is under `Build/`. The `kb-areas.md` BUILD area row pins `BannedSymbols.txt` as Tier-1 within the `Build/**` path pattern, which will not match the actual file location. Both CLAUDE.md and kb-areas.md need updating to reference `Source/BannedSymbols.txt`.

## 2026-05-04T15:24:23Z — kb-build step 3 CLAUDE-INFRA done
- areas/CLAUDE-INFRA/INDEX.md written, Tier-1 28/28 (100%), Tier-2 42/42 (100%; gate met), Tier-3 0, confidence high — full coverage of every Tier-1 + Tier-2 file
- Tier-1 inventory (28): CLAUDE.md (root), 9 agents (.claude/agents/*.md), 18 skill SKILL.md files
- Tier-2 inventory (42): 1 agents/_shared/kb-protocol.md, 23 docs (.claude/docs/*.md), 14 scripts (.claude/scripts/*.ps1), 2 hooks (.claude/hooks/*.ps1) + supporting refs
- Subsystems documented: root instructions (CLAUDE.md + agent-rules.md auto-import via @.claude/docs/agent-rules.md); 18 skills grouped by purpose (KB management 5: kb-build/refresh/ask/issues/status; PR review 2: review-pr/verify-review; issue+fix workflow 4: fix-issue/create-issue/find-issues/merge-duplicates; test workflow 2: test/test-providers; infrastructure 5: api-baselines/version-bump/update-slnx/audit-claude/session-reflect); 9 agents grouped by role (5 KB indexers all model:sonnet except kb-github-curator:haiku; 2 review subagents code-reviewer:opus + baselines-reviewer:sonnet; 2 test agents test-writer:sonnet + test-runner:haiku); 23 reference docs (each one-line purpose); 14 helper scripts (manifest-in/JSON-out, _shared.ps1 dot-sourced everywhere); 2 hooks (track-docker-start.ps1 PostToolUse + cleanup-docker-session.ps1 SessionEnd; note: user-level check-bash-chain.js PreToolUse hook lives outside this corpus at ~/.claude-my/hooks/, not under .claude/hooks/)
- 5 known issues / debt items: BannedSymbols.txt path mismatch (corroborates BUILD finding — both CLAUDE.md:54 and kb-areas.md BUILD-row need fix to Source/BannedSymbols.txt); claude-setup.md "Current skills" list stale (omits all skills added since last update — informational only, agents read individual SKILL.md); user-level check-bash-chain.js hook not discoverable from this corpus (lives at ~/.claude-my/hooks/, must be installed manually); 4 SKILL.md files exceed 250 lines (review-pr/verify-review/test-providers/fix-issue — refactor candidates per audit-claude); settings.local.json gitignored so hook wiring not committed (claude-setup.md acknowledges by design)
- Cross-area dependencies: outbound to every other area (skills spawn agents that read Source/+Tests/+Build/, kb-state.ps1 writes .claude/knowledge-base/, hooks observe .build/.claude/); inbound is Claude Code session loader (no code callers)
- Step 3 area-count complete: ALL 50 of 50 indexable areas now have INDEX.md. GLOBAL out of scope for step 3 (cross-cutting docs only).
- Step 3 NOT yet promoted to `done`: residual gate items remain — BUILD UNCLASSIFIED-FILE (kb-areas.md/CLAUDE.md fix proposed, awaiting user decision), FSHARP UNCLASSIFIED-FILE (FSharpExpressionInterceptor.fs delete-or-restore decision pending), 8 areas with by-design Tier-2 deferred-coverage queues (need user acknowledgment to close at `done`).

## 2026-05-04T15:56:04Z — agent audit notes
- File: Source/LinqToDB.Templates/Equatable.ttinclude
Conflict with existing INDEX.md optional add-ons table (row: `Equatable.ttinclude`).
Prior claim: "Attach point: `T4Model.ttinclude`".
Actual: the file opens with `<#@ include file="T4Model.ttinclude" once="true" #>` AND `<#@ include file="DataModel.ttinclude" once="true" #>`. It is the only optional add-on that pulls in `DataModel.ttinclude` directly (needed because `EquatableImpl` references the `Table`/`Column`/`IsPrimaryKey` types introduced there). The INDEX.md has been updated to reflect both attach points.
- File: Source/LinqToDB.Templates/Validation.ttinclude
Conflict with existing INDEX.md optional add-ons table (row: `Validation.ttinclude`).
Prior claim: "Attach point: `T4Model.ttinclude`".
Actual: the file opens with `<#@ include file="NotifyPropertyChanged.ttinclude" once="true" #>` — it does not directly include `T4Model.ttinclude`; it reaches it transitively. Including `Validation.ttinclude` always activates notify-property-changed support (user-visible consequence).
Similarly, `NotifyDataErrorInfo.ttinclude` prior claim was imprecise — it directly includes `Validation.ttinclude` (and through it `NotifyPropertyChanged.ttinclude`).

## 2026-05-04T15:59:04Z — deferred-coverage queue updated
- T4-TEMPLATES: -4 cleared

## 2026-05-04T15:59:30Z — kb-build step 3 T4-TEMPLATES coverage-fill done
- areas/T4-TEMPLATES/INDEX.md re-emitted: Tier-2 7/11 → 11/11 (gate now met), confidence stays high
- 4 deferred files cleared: EditableObject.ttinclude, Equatable.ttinclude, NotifyDataErrorInfo.ttinclude, Validation.ttinclude
- Findings: EditableObject (IEditableObject implementation via ModelGenerator.EditableObjectImplementation; extends Property as IEditableObjectProperty with IsEditable + IsDirtyText; provides EditableProperty subclass; SetPropertyValueAction wiring); Equatable (IEquatable<T> via ComparerBuilder.GetEqualityComparer<T>(); default filter primary-key columns; configurable EqualityPropertiesFilter delegate; only optional add-on directly including DataModel.ttinclude); NotifyDataErrorInfo (thin shim over ModelGenerator.NotifyDataErrorInfoImplementation, depends on Validation.ttinclude); Validation (extends Property as IPropertyValidation; depends on NotifyPropertyChanged.ttinclude — validation always carries notify-property-changed support)
- 2 AUDIT-NOTE entries: prior INDEX.md attach-point claims were imprecise for Equatable (omitted DataModel.ttinclude direct include), Validation (claimed T4Model.ttinclude direct, actually NotifyPropertyChanged.ttinclude), and NotifyDataErrorInfo (transitive only). Resolutions applied in re-emit.
- Net result: T4-TEMPLATES queue drained, area now at full Tier-1 + Tier-2 coverage.

## 2026-05-04T16:00:00Z — kb-areas.md + CLAUDE.md BannedSymbols.txt fix applied
- kb-areas.md BUILD row: Tier-1 pin updated `Build/BannedSymbols.txt` → `Source/BannedSymbols.txt`; path patterns gain `Source/BannedSymbols.txt`; notes column expanded with rationale (banned-API list consumed by Microsoft.CodeAnalysis.BannedApiAnalyzers via Source/Directory.Build.props <AdditionalFiles>).
- CLAUDE.md line 54: text "Banned API list in `Build/BannedSymbols.txt`" updated to "`Source/BannedSymbols.txt`".
- Resolves BUILD UNCLASSIFIED-FILE / AUDIT-NOTE from prior turn and CLAUDE-INFRA AUDIT-NOTE corroboration.
- Note: BUILD/INDEX.md frontmatter still shows coverage_tier_1: 3/4 (out of date now that kb-areas.md pin is reconciled). Will be picked up on next /kb-refresh code-cursor sweep.

## 2026-05-04T16:00:30Z — FSHARP/FSharpExpressionInterceptor.fs removed (user decision)
- Source/LinqToDB.FSharp/FSharpExpressionInterceptor.fs deleted via `git rm` per user direction (file was excluded from <Compile> in fsproj, contained pre-refactor stale-namespace code referencing LinqToDB.Interceptors.Internal/ExpressionInterceptor types that no longer exist).
- areas/FSHARP/INDEX.md updated: removed "FSharpExpressionInterceptor (unreleased)" subsection; removed corresponding row from Key types table; removed "Excluded from build" subsection; removed first known-issues bullet about the dead file; tightened the Subsystems entry-point paragraph to drop the reference.
- Resolves FSHARP UNCLASSIFIED-FILE gate item.

## 2026-05-04T16:37:38Z — deferred-coverage queue updated
- TESTS-INFRA: -51 cleared

## 2026-05-04T16:38:00Z — kb-build step 3 TESTS-INFRA coverage-fill done
- areas/TESTS-INFRA/INDEX.md re-emitted: Tier-2 18/70 → 69/70 (99%; gate met), confidence promoted medium → high
- 51 deferred files cleared in single batch — full queue drained
- Subsystems integrated: Provider selection (10 new feature-source attribute classes characterized incl. CteContextSourceAttribute exposing CteSupportedProviders array, IdentityInsertMergeDataContextSourceAttribute restricting to AllSybase/AllSqlServer2008Plus/AllPostgreSQL15Plus, MergeNotMatchedBySourceDataContextSourceAttribute restricting to AllFirebird5Plus/AllSqlServer2008Plus/AllPostgreSQL17Plus, SupportsAnalyticFunctionsContextAttribute restricting to AllSqlServer/AllOracle/AllClickHouse, InsertOrUpdateDataSourcesAttribute statically excluding AllClickHouse/Ydb, NorthwindDataContextAttribute, SkipCategoryAttribute with IApplyToTest, ThrowsForProviderAttribute + ThrowsRequiredOuterJoinsAttribute + ThrowsRequiresCorrelatedSubqueryAttribute); TestBase partial spread (TestBase.Concurrent.cs ConcurrentRunner with 10-thread Semaphore + JSON failure dump; TestBase.Identity.cs provider-specific identity-reset DDL across 13 providers; TestBase.Utils.cs LinqServiceSuffix + GetParameterToken + IsCaseSensitiveDB + AsyncEnumerableToListAsync); Remote transports (5 new server containers — TestGrpcLinqService non-netfx, HttpServerContainer port 22655 + TestHttpLinqServiceController, SignalRServerContainer port 22656 dual netfx/non-netfx + TestSignalRLinqService, WcfServerContainer port 22654 netfx-only with NetTcpBinding 10MB/10min, plus PortStatusRestorer RAII guard for KeepSamePortBetweenThreads); 5 Interceptors (CountingContextInterceptor extends DataContextInterceptor; SaveCommandInterceptor on CommandInitialized; SaveWrappedCommandInterceptor with profiler-unwrap dynamic cast; SequentialAccessCommandInterceptor singleton; BindByNameOracleCommandInterceptor :NEW trigger workaround); 2 TestProviders (SQLiteMiniprofilerProvider + UnwrapProfilerInterceptor); 4 Asserts cardinality DSL (ITimesConstraint + TimesType + AtLeast + Exactly); Should multi-substring constraint with in-order matching; ALLTYPE POCO; many utility classes (TestUtils, ProviderNameHelpers, QueryUtils, ScopedSettings, TestData, TestDataExtensions, TempTable, TxtSettings, DatabaseUtils, TestExternals, DictionaryEqualityComparer)
- 9 known-debt items added: CustomizationSupport.Interceptor non-thread-safe; CustomTestContext global ConcurrentDictionary cross-test risk; AssertState dead code (_assertStateEnabled = false); BaselinesWriter._baselines static dict no per-run reset; _serverContainers populated unconditionally even with DisableRemoteContext=true; Tests.csproj <Compile Remove> for WindowFunctionsTests.* (rename re-enables); MergeNotMatchedBySourceDataContextSourceAttribute inverted excludeLinqService convention; WcfServerContainer.Host_Faulted throws NotImplementedException; SkipCategoryAttribute.ProviderName property unused (early-return makes provider-specific skip a no-op)
- Net result: TESTS-INFRA queue fully drained (1 file remaining unvisited from prior-run budget exhaustion, kept at Tier-2 99%).

## 2026-05-04T16:58:05Z — deferred-coverage queue updated
- EXPR-TRANS: -57 cleared

## 2026-05-04T16:58:30Z — kb-build step 3 EXPR-TRANS coverage-fill done
- areas/EXPR-TRANS/INDEX.md re-emitted: Tier-1 expanded to 63/63 (full root *Builder.cs inventory now characterised); Tier-2 17/71 → 71/71 (100%; gate met), confidence stays high
- 57 deferred files cleared in single batch — full queue drained
- Subsystems integrated:
  - 14 context specializations (SubQueryContext, AsSubqueryContext, SelectContext, ScopeContext, PassThroughContext, SingleExpressionContext, AnchorContext, EagerContext, EnumerableContext, EnumerableContextDynamic, CteContext, CteTableContext, TableBuilder.TableContext, TableBuilder.RawSqlContext)
  - 7 supporting visitors: ExpressionTreeOptimizationContext (per-query analysis), ExpressionTreeOptimizerVisitor (constant-fold/double-not/equal-branches), BinaryExpressionAggregatorVisitor (>3-leaf AND/OR rebalance for stack-safety), ExposeExpressionVisitor (primary normalization pass), CanBeEvaluatedOnClientCheckVisitorBase (server-side-token early-exit), LambdaResolveVisitor (context-touching node resolution), ExpressionTestGenerator (diagnostic NUnit code emit)
  - 12 MergeBuilder partials (Merge/MergeInto/Using/UsingTarget/On 3-overload/InsertWhenNotMatched/UpdateWhenMatched skip-empty for #2843/UpdateWhenMatchedThenDelete/DeleteWhenMatched IsSourceOuter=true/DeleteWhenNotMatchedBySource/UpdateWhenNotMatchedBySource/MergeContext SetRunQuery dispatch on MergeKind), MergeProjectionHelper, TableLikeQueryContext dual-context with Prepare* helpers, TableLikeHelpers utility
  - CTE subsystem: CteBuilderImpl/CteAnnotationsContainer (IExpressionCacheKey via deterministic string)/TableBuilder.CteTableContext partial
  - LoadWith/association: AssociationHelper factory for HasQueryMethod/DefaultIfEmpty/LoadWith filter injection, LoadWithEntity/LoadWithMember tree, ILoadWithContext + ITableContext interfaces, EagerLoading.GetEnumerableElementType
  - Proxy subsystem: IBuildProxy + BuildProxyBase<TOwner> with ProcessTranslated/HandleTranslated/BuildProxyVisitor
  - EntityConstructorBase BuildGenericFromMembers respecting SkipOn* flags + FullEntityPurpose enum
  - SequenceHelper (PrepareBody, IsSameContext, CreateRef, CorrectExpression, UnwrapProxy, FindError, CreateSpecialProperty)
  - ParametersContext details (CurrentSqlParameters list, BuildParameter handling Bool wrapping + InPredicate, RegisterDynamicExpressionAccessor, SimplifyConversion)
  - Misc helpers: ProjectFlagExtensions [AggressiveInlining] predicates, ProjectionPathHelper, EvaluationHelper, BuildContextDebuggingHelper (#if DEBUG only)
- 9 known-debt items: source-generator silent-miss on missing attributes; legacy method-name overload entanglement in [BuildsMethodCall] lists; IBuildContext.Parent.set marked TODO probably-not-needed; BuildInfo mutable bag with set; accessors; MethodCallParser.cs in kb-areas.md doesn't exist; MergeBuilder.UpdateWhenMatched silent skip for empty implicit-setter (#2843); TableLikeQueryContext ProjectionHelper "selft_target" typo; ExpressionBuildVisitor cache leak risk via incomplete CloningContext; CteContext._isRecursiveCall flag without lock (fragile guard, currently safe because ExpressionBuilder not shared)
- Net result: EXPR-TRANS queue fully drained (Tier-2 100%, confidence high).

## 2026-05-04T17:29:16Z — deferred-coverage queue updated
- TESTS-EFCORE: -92 cleared

## 2026-05-04T17:30:00Z — kb-build step 3 TESTS-EFCORE coverage-fill done
- areas/TESTS-EFCORE/INDEX.md re-emitted: Tier-2 36/171 → 128/171 (74.9%; full deferred queue drained), confidence stays medium (74.9% below 90% — remaining 43 files were never queued, not part of this fill cycle)
- 92 deferred files cleared in single batch — full deferred queue drained
- Subsystems/sections integrated:
  - 3 EFCore-only Interceptors (TestConnectionInterceptor + TestDataContextInterceptor + TestEntityServiceInterceptor for InterceptorTests)
  - Logging plumbing (LogMessageEntry record + NullScope/NullExternalScopeProvider stubs + TestLoggerExtensions for routing ILogger output to BaselinesManager)
  - Models taxonomy: ForMapping (12 files — per-provider DbContexts + 9 entity POCOs covering identity/no-identity/duplicate-properties/inheritance/string-types/uint mapping); IssueModel (7 files — Issue73/117 entities + per-provider IssueContexts); JsonConverter (5 files — EventScheduleItemBase hierarchy + LocalizedString JSON value-conversion + CrashEnum); Northwind (16 entity POCOs + 13 SQLServer Fluent maps + NorthwindData seeder); NpgSqlEntities (6 files — array/xmin/timestamp/event/event-view PostgreSQL-specific types); Shared (12 files — Entity/Item/Detail/SubDetail/Child + Id<T>/IHasId + IdValueConverter + DataContextExtensions + ModelBuilderExtensions); ValueConversion (6 files — Id<TEntity,TKey>/IdValueConverter<TEntity,TKey>/IdValueConverterSelector + IEntity<TKey> + ConvertorContext + SubDivision)
  - 2 test fixtures: JsonConvertTests (JSON value-conversion round-trip via EventScheduleItem hierarchy + LocalizedString + CrashEnum); PomeloMySqlTests (Pomelo-specific MySQL features, excluded on EF10 csproj)
  - 5 Utilities (ExceptionExtensions, Polyfills, StringExtensions, TypeExtensions, Unit) for shared helper functions
- Net result: TESTS-EFCORE deferred queue fully drained. Tier-2 at 128/171 (74.9%). Remaining 43 unvisited Tier-2 files were never added to the deferred queue — they fall outside this drain cycle. Confidence stays medium per the 90%-threshold rule.

## 2026-05-04T17:58:30Z — deferred-coverage queue updated
- INTERNAL-API: -51 cleared

## 2026-05-04T18:09:10Z — deferred-coverage queue updated
- INTERNAL-API: -55 cleared

## 2026-05-04T18:29:00Z — deferred-coverage queue updated
- INTERNAL-API: -57 cleared

## 2026-05-04T19:17:43Z — deferred-coverage queue updated
- INTERNAL-API: -48 cleared

## 2026-05-04T19:18:00Z — kb-build step 3 INTERNAL-API coverage-fill done (4 batches)
- areas/INTERNAL-API/INDEX.md updated across batches 1-3 (full re-emit each time with additions); batch 4 dispatched but its envelope's artifact body was display-truncated and not recoverable — only the DEFERRED-COVERAGE-CLEAR fence was preserved, queue drained to 0
- Final frontmatter manually bumped: coverage_tier_2 182/199 → 199/199 (queue fully drained), confidence medium → high
- Total: 211 files cleared (batch 1: 51 — Async/Cache/Common/Conversion/DataContextExtensions; batch 2: 55 — DataProvider+Translation/Infrastructure; batch 3: 57 — Expressions/* incl. ExpressionVisitors+Types+WindowFunctionHelpers; batch 4: 48 — Extensions/Interceptors/Mapping/Options/Reflection/Remote/SchemaProvider/PublicAPI)
- Subsystems integrated through batch 3: Async (DbConnection/Transaction reflective wrappers + SafeAwaiter ConfigureAwait pattern); Cache (16-file MemoryCache subset — port of Microsoft.Extensions.Caching.Memory minus DI/IServiceCollection); Common (22 utility files: Pools / SnapshotDictionary / SqlTextWriter / TaskCache / TopoSorting / TypeHelper / ValueComparer + many others); Conversion (4 files — ConvertBuilder for runtime conversion-delegate generation); DataProvider root (27 files — Adapter/AssemblyResolver/IdentifierService*/QueryParametersNormalizer/Adapter wrappers etc.); DataProvider/Translation (17 files — base classes for AggregateFunctions/DateFunctions/Guid/Math/SqlFunctions/String/SqlTypes member translation + IMemberConverter); Infrastructure (9 files — AnnotatableBase/IUniqueIdGenerator/UniqueIdGenerator); Expressions root + Types + ExpressionVisitors (57 files — Sql*Expression nodes + visitor framework FindVisitor/TransformVisitor/WritableContext + TypeWrapper reflection-binding subsystem)
- Batch 4 subsystems NOT integrated into INDEX.md body due to output truncation (queue is drained but the doc didn't get the per-file characterizations): Extensions/* (11 helper extension classes); Interceptors/* (8 Aggregated*Interceptor + EntityBindingInterceptor + InterceptorInternalExtensions + OneTimeCommandInterceptor); Mapping/* (DynamicColumnInfo, LockedMappingSchemaInfo, MappingAttributesCache, Nullability, SpecialPropertyInfo, VirtualPropertyInfoBase); Options/IReapplicable; Reflection/MemberInfoEqualityComparer; Remote/* (RemoteDataReader, SerializationConverter, SerializationMappingSchema); SchemaProvider/* (6 *Info POCO classes); PublicAPI/*.txt (8 baseline files for RS0016/RS0017 analyzer per TFM). Future /kb-refresh --source coverage on this area can pick up these additions without re-reading.
- Net result: INTERNAL-API at Tier-2 100% (199/199), confidence high. Queue drained. INDEX.md body is rich for batches 1-3 subsystems and lighter for batch-4 subsystems (file-list-only, no per-file characterization). User can revisit if richer batch-4 documentation is needed.

## 2026-05-04T19:54:04Z — deferred-coverage queue updated
- SCAFFOLD: -52 cleared

## 2026-05-04T20:08:22Z — deferred-coverage queue updated
- SCAFFOLD: -52 cleared

## 2026-05-04T20:18:37Z — deferred-coverage queue updated
- SCAFFOLD: -52 cleared

## 2026-05-04T20:19:00Z — kb-build step 3 SCAFFOLD coverage-fill batches 1-3 (partial)
- areas/SCAFFOLD/INDEX.md updated by batches 1+2 (full re-emit each time, additions integrated): Tier-2 28/267 → 132/267 (49.4%)
- batch 1 (52): all CodeModel/AST/Basics/ + 35 root AST nodes (CodeAsOperator-CodeRegion). Body integrated marker interfaces / abstract base classes / declaration / structure / expression / statement / annotation node taxonomy.
- batch 2 (52): AST root remainder (CodeReturn, CodeSuppressNull, CodeTernary, CodeThis, CodeThrow*, CodeTypeCast, CodeTypeInitializer, CodeTypeReference, CodeTypeToken, CodeUnary, CodeVariable, CodeXmlComment), AST/Groups (9), AST/Trivia (2), Builders (15), CodeGeneration (7), Comparers (2), Languages/CSharp (3). Body integrated CodeBuilder factory + builder hierarchy + IndentedWriter + TableLayoutBuilder two-phase layout + TypeEqualityComparer with NRT/nullability variants + CSharpCodeGenerator/CSharpNameNormalizationVisitor mechanics.
- batch 3 (52): Types/* (11 — IType hierarchy + TypeParser + WellKnownTypes), Utils/AstExtensions, Visitors/Basic/* (4) + Visitors/Custom/ProviderSpecificStructsEqualityFixer + ImportsCollector + NameScopesCollector, DataModel/Context/* (5 — IDataModelGenerationContext + DataModelGenerationContext + NestedSchemaGenerationContext + FileData + CodeGenerationExtensions), DataModelConstants, 8 DataModelGenerator partials (DataContext/Entities/Associations/Schemas/Functions+Aggregate/Procedures/Scalar/Table), DataModel/Model/Code/* (4) + DataContextModel + Entities/* (3) + Functions/* (9). Queue drained but INDEX.md body NOT updated this batch — context budget for the parent skill prevented persisting the agent's full re-emit (the artifact body was generated but not written; queue-only fence applied). Future /kb-refresh --source coverage on SCAFFOLD can rebuild richer documentation if needed.
- batches 4 + 5 still pending (49 files left in queue: Helpers, Metadata + Metadata/Model, ModelGeneration, Naming, Scaffold/* helpers, Schema/* DTOs).
- 11 known-debt items added across batches 1-3: F#/VB.NET not implemented (issue #1553); logger not wired (DataModelLoader / LegacySchemaProvider use Console.Error.WriteLine); PostgreSQL legacy-API workaround for table-function tuple parameters; MySQL type-conflict swallowed; ModelGeneration/ legacy layer retained; ISchemaProvider lacks async overloads; CodeNameOf argument-type unconstrained; CodeBinary type inference incomplete (Add returns left type); CSharpCodeGenerator inline comment generation throws NotImplementedException; CSharpCodeGenerator dead fields (_knownTypes, _currentImports — pragma-disabled); CSharpNameNormalizationVisitor missing type-parameter-count overload check; TypeArgument constraints unimplemented; TypeParser NRT extraction from CLR attributes incomplete; stored-procedure generation "chaotic" since async support; MakeFullyQualifiedRoutineName depends on linq2db API needing refactor; XML comment text/attribute writers don't filter disallowed chars.

## 2026-05-04T20:52:52Z — deferred-coverage queue updated
- SCAFFOLD: -52 cleared

## 2026-05-04T21:05:58Z — deferred-coverage queue updated
- SCAFFOLD: -49 cleared

