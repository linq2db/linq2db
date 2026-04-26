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

