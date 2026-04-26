# Architect Notes: AI/LLM Documentation Initiative

> Role: Architect & Critic  
> Branch: `llm-architecture-support`  
> Last updated: 2025-07 (session 5)

---

## Objective

Make linq2db usable by AI agents (LLM-backed IDE assistants, CLI agents, MCP-connected tools) with minimal friction after inclusion as a NuGet package.

Agents consume the library via:
- **IntelliSense / XML docs** (`linq2db.xml` in NuGet package)
- **NuGet package files** bundled at pack time: `readme.md`, `docs/ai-tags.md`, `docs/architecture.md`
- **Direct file access** (CLI agents with filesystem access, MCP tools)

---

## What Has Been Done

### 1. `LinqToDBArchitecture.cs`
- Dedicated `[EditorBrowsable(EditorBrowsableState.Never)]` static class
- Exists purely for IntelliSense / XML doc discoverability
- Contains: core identity, translation pipeline, execution model, "what is not provided", entry points, links to package docs
- Nested members for pipeline stages, execution model, mapping model

### 2. `docs/architecture.md` (included in NuGet)
- Plain-text architecture overview
- Covers: core identity, translation pipeline, execution model, mapping, provider model, what is NOT provided, entry points
- Has machine-readable section on AI-Tags
- **Confirmed present in csproj `<None Include="../../docs/architecture.md" .../>`**

### 3. `docs/ai-tags.md` (included in NuGet)
- Full specification of AI-Tags format
- Controlled vocabulary for all keys: Group, Execution, Composability, Affects, Pipeline, Provider
- Authoring rules, defaults merge rules, scope guidance
- **Confirmed present in csproj `<None Include="../../docs/ai-tags.md" .../>`**

### 4. `Source/LinqToDB/readme.md` (NuGet readme)
- Standard developer readme with usage examples
- Has AI/LLM section at the **end** with links to architecture.md, ai-tags.md, and LinqToDBArchitecture class
- **Confirmed present in csproj `<None Include="readme.md" .../>`**

### 5. AI-Tags in `DataExtensions.cs`
- Class-level `AI-Tags-Defaults: Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`
- Every public method has AI-Tags in `<remarks>`
- Coverage: full for this file

### 6. AI-Tags in `LinqExtensions/*.cs` (audited 2025-07; Groups updated 2025-07 session 2)
- `LinqExtensions.cs` — class-level `AI-Tags: Groups=QueryDirectives,NavigationLoading,DML,Merge,Hints,Configuration,Helpers; Pipeline=...; Provider=...;`
  Groups updated to include `Hints` (from `LinqExtensions.Hints.cs`) and `Configuration` (from `LinqExtensions.TableHelpers.cs`).
  Uses `AI-Tags` (aggregate form), NOT `AI-Tags-Defaults`. Per ai-tags.md rule #9, each method repeats `Pipeline` and `Provider` keys.
- `LinqExtensions.Insert.cs` — method-level AI-Tags present
- `LinqExtensions.Update.cs` — method-level AI-Tags present
- `LinqExtensions.Delete.cs` — method-level AI-Tags present
- `LinqExtensions.Hints.cs` — method-level AI-Tags present
- `LinqExtensions.Merge.cs` — method-level AI-Tags present; includes FSM-style state-transition diagram for Merge API — high quality
- `LinqExtensions.TableHelpers.cs` — method-level AI-Tags present
- `LinqExtensions.LoadWith.cs` — method-level AI-Tags present; includes call graph diagram
- **Coverage: substantially complete.** Minor efficiency gap: no `AI-Tags-Defaults` means per-method repetition of `Pipeline`/`Provider` keys.

### 7. AI-Tags in `DataConnection.cs` and `DataContext.cs` (audited 2025-07; seealso added 2025-07 session 2)
- Both have class-level `AI-Tags: Group=Connection; Affects=ExecutionContext; Pipeline=...; Provider=...;`
- `<seealso cref="LinqToDBArchitecture"/>` added to both — discovery path for IntelliSense-only agents established

### 8. `ITable<T>.cs` (audited 2025-07; seealso added 2025-07 session 2)
- Has complete XML docs describing query root semantics, deferred execution, no change tracking
- `<seealso cref="LinqToDBArchitecture"/>` added

### 9. `docs/agent-antipatterns.md` (created 2025-07 session 2)
- Created; covers: `MappingSchema` per-request, `TransactionScope`+`DataConnection` timing, thread safety, no client-eval fallback
- Linked from `docs/architecture.md` Additional Documentation section
- Included in NuGet package via `LinqToDB.csproj`

### 10. `Source/LinqToDB/Mapping/MappingSchema.cs` (updated 2025-07 session 2)
- Replaced minimal "Mapping schema." summary with full `<remarks>` block
- Covers: what MappingSchema is, performance-critical lifetime warning, `// WRONG:` per-request anti-pattern, correct `static readonly` + `DataOptions` preset pattern

### 11. `Source/LinqToDB/DataOptions.cs` (updated 2025-07 session 3)
- Existing docs already had "create once, reuse" performance guidance
- Added: temporary per-connection override section documenting `using var _ = db.UseOptions(...)` / `db.UseMappingSchema(...)` pattern
- Documents: `IDisposable` return fully restores prior options and internal connection state on dispose; `null` return = no-op optimization

### 12. `Source/LinqToDB/LinqToDBArchitecture.cs` (updated 2025-07 session 2)
- Fixed all 6 "shape" → "structure" occurrences (G6)
- Expanded `ExtensionModel` nested class: BCL hundreds of methods, `Sql` class SQL-specific functions, provider difference handling

### 13. `Source/LinqToDB/readme.md` (rewritten 2025-07 session 2)
- Full rewrite: user-first, practical quick-start, AI/LLM section at end (intentional user-first design)
- "Rich built-in SQL translation" and "Extensible SQL mapping" split into two distinct feature bullets

### 14. `Source/LinqToDB/Data/DataContextExtensions.cs` and `BulkCopyOptions.cs` (updated 2025-07 session 6)
- `DataContextExtensions.cs`: replaced minimal one-line summary with full `<remarks>` block describing the two API surfaces (raw SQL / SetCommand fluent builder, and BulkCopy DML); class-level `AI-Tags: Groups=RawSQL,DML` + `AI-Tags-Defaults`
- Tagged principal `SetCommand(IDataContext, string)` as entry point for raw SQL builder pattern: `Group=RawSQL; Pipeline=SqlText; Execution=Deferred; Composability=Composable`
- Tagged principal `BulkCopy<T>(IDataContext, BulkCopyOptions, IEnumerable<T>)` and `BulkCopyAsync` with `Group=DML; Execution=Immediate; Composability=Terminal; Pipeline=BulkInsert`
- `BulkCopyOptions.cs`: added `<remarks>` block covering strategy selection (`BulkCopyType`), provider support caveats, and `AI-Tags: Group=DML; Pipeline=BulkInsert`
- `docs/ai-tags.md`: extended vocabulary with `RawSQL` group; added authoring rules 10 and 11 for `Pipeline=SqlText` and `Pipeline=BulkInsert`

### 15. `Source/LinqToDB/Data/CommandInfo.cs` (updated 2025-07 session 7)
- Replaced "Provides database connection command abstraction." with full `<remarks>` block: fluent builder pattern description, terminal method list (Query, Execute, ExecuteScalar, ExecuteReader), stored-procedure note, no-LINQ-translation note
- `AI-Tags-Defaults: Group=RawSQL; Pipeline=SqlText; Provider=ProviderDefined;` established for all members
- Principal methods tagged: `Query<T>()` (Deferred/Terminal/QueryResult), `Execute()` (Immediate/Terminal/Data), `Execute<T>()` (Immediate/Terminal/QueryResult), `ExecuteReader()` (Immediate/Terminal/QueryResult)

### 16. `Source/LinqToDB/SchemaProvider/ISchemaProvider.cs` (updated 2025-07 session 7)
- Added class-level `<remarks>`: metadata retrieval overview, access pattern (`DataConnection.DataProvider.GetSchemaProvider()`), transaction-safety warning
- `AI-Tags: Group=Schema; Execution=Immediate; Composability=Terminal; Pipeline=SqlText; Provider=ProviderDefined;`
- `GetSchema` method: transaction warning preserved in method docs; AI-Tags repeated at method level
- `docs/ai-tags.md`: added `Schema` group to controlled vocabulary

---

## Gaps and Issues

### G1. AI-Tags coverage is incomplete
- `DataExtensions.cs` is tagged — confirmed
- `LinqExtensions/*.cs` — coverage unknown, to be audited
- `Data/DataConnection.cs`, `DataContext.cs` — coverage unknown
- `TempTable.cs`, `BulkCopy`, `SchemaProvider` APIs — likely untagged
- **Priority: audit and tag all high-value public API surfaces**

### G2. `LinqToDBArchitecture` discoverability for agents
- The class is `EditorBrowsable(Never)` — correct for human IDE use, but CLI agents doing symbol search may miss it
- readme.md says "see XML documentation for class `LinqToDBArchitecture`" but does NOT name the namespace
- A Copilot/agent doing `get_symbols_by_name("LinqToDBArchitecture")` would find it, but a naive text search won't
- **Suggestion: add namespace-qualified reference in readme.md AI section**

### G3. readme.md AI/LLM section is minimal and buried at the end
- Located after MiniProfiler section, just three bullet links
- No quick-start guidance for agents
- No description of what AI-Tags ARE at the readme level
- An agent reading only the first N tokens of readme.md will miss it
- **Suggestion: move AI/LLM section higher or add a short paragraph at the top**

### G4. No "anti-patterns / common mistakes" document
- Critical omission: agents generate wrong code when they don't know failure modes
- Known failure modes:
  - Creating `MappingSchema` per-request (serious performance issue)
  - Using non-translatable LINQ expressions without materialization
  - Mixing `TransactionScope` with `DataConnection` (ambiguous behavior documented in readme but not tagged)
  - Reusing `DataConnection` across threads
  - Calling client-side methods inside `Select` (no client-eval fallback unlike EF Core)
- **Suggestion: create `docs/agent-antipatterns.md` or embed in architecture.md**

### G5. No provider capability matrix
- AI agents cannot know which SQL features are available per provider
- Examples: MERGE not on SQLite, CTE with recursion requires support, `RETURNING` only on some providers
- A machine-readable capability table would help agents generate provider-aware code
- **Suggestion: `docs/provider-capabilities.md` with a simple markdown table; or extend AI-Tags with `Provider=ProviderSpecific` + reference**

### G6. `docs/architecture.md` uses the word "shape"
- User instruction (copilot-instructions): avoid "shape" in documentation; use "structure" instead
- **`docs/architecture.md` is CLEAN — no "shape" occurrences found (audited 2025-07)**
- **`LinqToDBArchitecture.cs` has 6 occurrences** (lines 21, 204, 436, 631, 639, 646):
  - Line 21: "think in terms of SQL intent and shape"
  - Line 204: "Resulting SQL shape:"
  - Line 436: "Query shape determines how associations are translated."
  - Line 631: "LoadWith modifies query shape to include associated data"
  - Line 639: "LINQ-shaped constructs"
  - Line 646: "SQL shape of these features depend on provider capabilities"
- All 6 must be replaced with "structure" or restructured

### G7. `LinqExtensions.cs` AI-Tags coverage unknown
- **STATUS: SUBSTANTIALLY RESOLVED (audited 2025-07)**
- All major sub-files (Insert, Update, Delete, Hints, Merge, TableHelpers, LoadWith) have method-level AI-Tags
- Merge.cs has particularly high quality: includes FSM-style state diagram and Merge call graph for AI agent navigation
- Remaining minor gap: `LinqExtensions.cs` class uses `AI-Tags` (aggregate form) instead of `AI-Tags-Defaults`; per-method tags repeat `Pipeline`/`Provider` keys unnecessarily (ai-tags.md rule #9)
- **Action**: optionally add `AI-Tags-Defaults: Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;` to the `LinqExtensions` class alongside the existing class-level `AI-Tags`

### G8. No agent-oriented "getting started" entry point in XML docs
- `LinqToDBArchitecture` is good but assumes the agent already knows to look there
- **Critical for IntelliSense-only agents**: if an agent has no filesystem access and can only
  browse types via IntelliSense/XML docs, there is CURRENTLY NO PATH from any naturally-discovered
  type (`DataConnection`, `DataContext`, `ITable{T}`) to `LinqToDBArchitecture`
- `LinqToDBArchitecture` is `EditorBrowsable(Never)` — hidden from auto-complete
- CLI agents using `get_symbols_by_name("LinqToDBArchitecture")` CAN find it; IntelliSense-only agents CANNOT
- **Suggestion: add `<seealso cref="LinqToDBArchitecture"/>` on `DataConnection`, `DataContext`, `ITable{T}`**
  This creates the missing discovery link for IntelliSense-only agents

### G9. `AI-Tags-Defaults` pattern not applied uniformly
- Only `DataExtensions.cs` has class-level `AI-Tags-Defaults` so far
- `LinqExtensions.cs` uses class-level `AI-Tags` (aggregate form with `Groups=`) — this is correct for aggregate docs
  but does NOT establish `AI-Tags-Defaults` for per-member inheritance
- Per ai-tags.md rule #9, `Pipeline` and `Provider` should be declared once in `AI-Tags-Defaults` and omitted from per-member tags
- Currently, every method in `LinqExtensions` sub-files explicitly repeats `Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;`
- **Suggestion**: add `AI-Tags-Defaults: Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;` to `LinqExtensions.cs` class docs
  alongside the existing `AI-Tags: Groups=...` line — the two are complementary and non-conflicting

### G10. `MappingSchema` per-request anti-pattern not tagged
- readme.md has a prominent warning in the fluent mapping section: "IMPORTANT: configure mapping schema instance only once..."
- This is one of the most serious performance pitfalls (internal caching is per-schema instance)
- Not expressed in AI-Tags or in any machine-readable form; agents reading only method-level docs won't see it
- `MappingSchema` class itself has no XML doc warning visible to agents
- **Suggestion**: add warning in `MappingSchema` class XML docs and/or in `docs/agent-antipatterns.md`

### G11. `TransactionScope` + `DataConnection` interaction not tagged
- readme.md has a detailed explanation with code example of the `TransactionScope`+`DataConnection` timing issue
- This is a common source of bugs (ambient transaction attached at connection open, not at scope creation)
- Not expressed in AI-Tags or flagged in `DataConnection` class XML docs
- Content already exists in readme.md — needs to be surfaced in class-level docs or antipatterns doc

---

## Open Questions

1. **Should `docs/` folder be navigable by agents?**  
   Currently the only discovery path for agents without filesystem access is:  
   readme.md → "see docs/architecture.md" → but the file is only available on disk, not via symbol search.  
   For IntelliSense-only agents (e.g. VS Copilot with no file read), the only path is `LinqToDBArchitecture`.  
   This is fine architecturally but should be explicit in our assumptions.

2. **Is `AI-Tags` the right mechanism for provider capability hints?**  
   Current `Provider=ProviderDefined` vs `Provider=ProviderAgnostic` is too coarse.  
   For merge, CTEs, window functions, the answer is "depends on specific provider".  
   Extending vocabulary vs a separate capability document — TBD.

3. **Should AI-Tags be parseable at build time?**  
   A source generator or build step that extracts and validates AI-Tags from XML docs would:  
   - Catch vocabulary violations early  
   - Produce a standalone `ai-index.json` in the NuGet package for agents with JSON parsing  
   - This is a larger investment; decide if warranted.

4. **`LinqToDBArchitecture` nested members — are they sufficient or over-engineered?**  
   The class has nested summary members for pipeline stages etc.  
   If agents only read top-level `<summary>` of the class itself, the nested detail is invisible.  
   May need to collapse into the main class docs.

---

## Prioritized Next Steps

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 1 | Fix "shape" → "structure" in `LinqToDBArchitecture.cs` | Low | **DONE** | All 6 occurrences replaced |
| 2 | Add `<seealso cref="LinqToDBArchitecture"/>` to `DataConnection`, `DataContext`, `ITable{T}` | **High** | **DONE** | Discovery path for IntelliSense-only agents established |
| 3 | Add namespace to `LinqToDBArchitecture` reference in readme.md | Low | **DONE** | Included in readme.md full rewrite |
| 4 | Move or expand AI/LLM section in readme.md | Medium | **DONE** | readme.md fully rewritten; AI section intentionally last (user-first design) |
| 5 | Create `docs/agent-antipatterns.md` | Medium | **DONE** | Created; linked from `docs/architecture.md` Additional Documentation |
| 6 | Add `AI-Tags-Defaults` to `LinqExtensions.cs` | Low | **DONE** | Added alongside existing `AI-Tags: Groups=...` line; establishes class-level defaults for all sub-files |
| 7 | Add `MappingSchema` per-request warning to `MappingSchema` class XML docs | Medium | **DONE** | Full `<remarks>`: performance warning + `// WRONG:` anti-pattern + correct `static readonly` pattern |
| 8 | Audit `DataConnection`/`DataContext` method-level AI-Tags | Low | **DONE** | Async transaction methods tagged in `DataConnection.Async.cs` (BeginTransactionAsync ×2, CommitTransactionAsync, RollbackTransactionAsync, DisposeTransactionAsync) and `DataContext.cs` (BeginTransactionAsync ×2) |
| 9 | Design provider capability document | Low | OPEN | See Open Question #2 |
| 10 | Document `DataOptions` lifetime + `UseOptions` scoped override pattern | Medium | **DONE** | `static readonly` preset + `using var _ = db.UseOptions(...)` full-restore idiom |
| 11 | Add Hints + Configuration groups to `LinqExtensions.cs` `Groups=` | Low | **DONE** | Groups now: `QueryDirectives,NavigationLoading,DML,Merge,Hints,Configuration,Helpers` |
| 12 | Expand `ExtensionModel` in `LinqToDBArchitecture.cs` | Low | **DONE** | BCL hundreds + `Sql` class + provider differences bullets added |
| 13 | Add class-level docs + AI-Tags + `seealso` to `TempTable<T>` | Medium | **DONE** | Full `<remarks>`: lifecycle, construction patterns, ITable{T} composability, lifetime guidance |
| 14 | Tag `DataContextExtensions.cs` (BulkCopy + raw SQL) + `BulkCopyOptions.cs` | Medium | **DONE** | Class-level remarks + Groups=RawSQL,DML; principal BulkCopy overloads tagged; `RawSQL` group added to ai-tags.md vocabulary |
| 15 | Tag `CommandInfo.cs` (raw SQL fluent builder) | Medium | **DONE** | Class-level remarks (builder pattern, terminal methods list); principal Query/Execute/ExecuteReader overloads tagged; AI-Tags-Defaults established |
| 16 | Tag `ISchemaProvider.cs` | Low | **DONE** | Class-level remarks (access pattern, transaction warning); GetSchema method tagged; `Schema` group added to ai-tags.md vocabulary |

---

## Session Log

- **2025-07 (initial)**: Analyzed existing docs, identified gaps G1–G9, created this notes file.
- **2025-07 (audit)**: Full audit of LinqExtensions sub-files, DataConnection, DataContext, ITable<T>, docs/. Updated G6 (precise locations), G7 (resolved), G8 (critical gap confirmed — IntelliSense-only agents have zero path to LinqToDBArchitecture). Added G10 (MappingSchema anti-pattern), G11 (TransactionScope timing). Confirmed docs/architecture.md is clean. Updated priority table: G8's `seealso` links promoted to High priority.
- **2025-07 (session 2)**: Completed items 1–5, 7, 11, 12. Fixed all "shape"→"structure" in LinqToDBArchitecture.cs. Added `<seealso cref="LinqToDBArchitecture"/>` to DataConnection, DataContext, ITable{T}. Rewrote readme.md (user-first; AI section at end). Created docs/agent-antipatterns.md; added to architecture.md Additional Documentation and LinqToDB.csproj NuGet files. Added MappingSchema.cs full remarks block (performance warning + anti-pattern). Added Hints+Configuration to LinqExtensions.cs Groups=. Expanded ExtensionModel in LinqToDBArchitecture.cs.
- **2025-07 (session 3)**: Completed item 10. Added `DataOptions` temporary override section to class XML docs: documents `using var _ = db.UseOptions(...)` / `db.UseMappingSchema(...)` pattern, IDisposable full-restore semantics, and null-return optimization. Updated this notes file to reflect all completed work from sessions 2–3.
- **2025-07 (session 4)**: Completed items 6 and 13. Added `AI-Tags-Defaults` to `LinqExtensions.cs` (alongside existing `AI-Tags: Groups=...`). Added full `<remarks>` block + AI-Tags + `<seealso>` to `TempTable<T>` class: lifecycle, construction patterns (empty / BulkCopy / INSERT-SELECT / async factory), ITable{T} composability, disposal guarantee.
- **2025-07 (session 5)**: Documentation consistency review. Two issues found and fixed:
  1. **`TempTable<T>` semantic correction** — the original docs stated the class "uses a regular physical table; database-native temporary table DDL is not used," which was factually wrong. Corrected to document the lifecycle-vs-table-kind distinction as two independent orthogonal concepts: lifecycle guarantee (CREATE on ctor, DROP on Dispose) + table kind controlled by `TableOptions` (default `IsTemporary` from `CreateTempTableOptions`). Added note that the default `TempTable<T>` constructor passes `TableOptions.NotSet`, so mapping/provider defaults determine table kind unless explicitly overridden.
  2. **`DataOptions.cs` fabricated method reference** — the "Temporary per-context overrides" code example used `o.UseQueryTraces(true)`, a method that does not exist anywhere in the codebase. Fixed to `o.UseCommandTimeout(30)` (confirmed real method in `DataOptionsExtensions.cs`).
  Cross-reference audit results: `TableOptions.None/NotSet/IsTemporary` ✅, `CreateTempTableOptions.TableOptions` ✅, `IDataContext.UseOptions`/`UseMappingSchema` ✅, `LinqToDBArchitecture` type ✅, `CreateAsync` static in `TempTable<T>` ✅, `DataConnection`/`DataContext` AI-Tags ✅, `DataExtensions.cs` AI-Tags-Defaults ✅.
- **2025-07 (session 6)**: Completed item 14. Extended `docs/ai-tags.md` vocabulary: added `RawSQL` group; added authoring rules 10 (`Pipeline=SqlText` for raw SQL) and 11 (`Pipeline=BulkInsert` for BulkCopy). Added class-level remarks to `DataContextExtensions.cs` describing the two API surfaces (raw SQL and BulkCopy); tagged principal `SetCommand` and `BulkCopy`/`BulkCopyAsync` overloads. Added `<remarks>` + AI-Tags to `BulkCopyOptions.cs`: strategy selection guide, provider support caveats, `BulkCopyType` list.
- **2025-07 (session 7)**: Completed items 15-16. `CommandInfo.cs`: replaced one-line summary with full `<remarks>` (fluent builder pattern, terminal methods, no-LINQ note); AI-Tags-Defaults established; principal `Query<T>`, `Execute`, `Execute<T>`, `ExecuteReader` overloads tagged. `ISchemaProvider.cs`: class-level remarks (metadata retrieval, access pattern via `DataConnection.DataProvider.GetSchemaProvider()`, transaction-safety warning); `GetSchema` tagged; `Schema` group added to ai-tags.md. **Bug caught and fixed**: fabricated `SchemaProviderExtensions` reference removed (same class of error as `UseQueryTraces` in session 5).

