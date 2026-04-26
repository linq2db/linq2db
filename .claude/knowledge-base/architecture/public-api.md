---
area: GLOBAL
kind: architecture
sources: [code]
confidence: medium
last_verified: 2026-04-26
last_verified_sha: 3727a580c828e4f983da2de934d4cfc12d0cb255
coverage_tier_1: 4/4
coverage_tier_2: 3/3
---

# Public API surface

linq2db treats every type, method signature, and observable generated SQL outside the `LinqToDB.Internal.*` namespace as a stability contract for downstream consumers. ApiCompat baselines under `Source/**/CompatibilitySuppressions.xml` enforce this — see [`code-design.md`](../../docs/code-design.md) → **Public API is a contract**.

## Stability commitment by namespace

| Namespace root | Kind | Stability | Pointers |
|---|---|---|---|
| `LinqToDB` | top-level surface — `IDataContext`, `DataContext`, `DataOptions`, `DataExtensions`, `ITable<T>`, `CompiledQuery`, `ProviderName`, `LinqOptions`, `SqlOptions`, `TableOptions`, `DataType`, `DbDataType`, `LinqToDBException`, `RawSqlString`, `MergeOperationType`, `SqlJoinType`, etc. | **Stable**. Breaking change requires next-major milestone. | [CORE](../areas/CORE/INDEX.md) |
| `LinqToDB.Data` | persistent connection (`DataConnection`), `BulkCopy*`, `CommandInfo`, `DataParameter`, `DataReaderAsync`, `DataConnectionTransaction`, `TraceInfo`, `ConflictAction`, retry-policy types under `LinqToDB.Data.RetryPolicy`. | **Stable**. | [DATA](../areas/DATA/INDEX.md) |
| `LinqToDB.Mapping` | `MappingSchema`, `EntityDescriptor`, `ColumnDescriptor`, `[Table]`, `[Column]`, `[Association]`, `FluentMappingBuilder`, `IGenericInfoProvider`. | **Stable**. | [MAPPING](../areas/MAPPING/INDEX.md) |
| `LinqToDB.DataProvider` | `IDataProvider`, `IDataProviderFactory`. | **Stable interface contracts.** Breaking changes require next-major. | [CORE](../areas/CORE/INDEX.md) |
| `LinqToDB.DataProvider.<X>` | per-provider tools + dialect enums + hints + provider-specific options (`SqlServerTools`, `PostgreSQLOptions`, `OracleVersion`, etc.). One namespace per provider — `Access`, `ClickHouse`, `DB2`, `Firebird`, `Informix`, `MySql`, `Oracle`, `PostgreSQL`, `SQLite`, `SapHana`, `SqlCe`, `SqlServer`, `Sybase`, `Ydb`. | **Stable.** | [PROV-*](../areas/PROV-SQLSERVER/INDEX.md) |
| `LinqToDB.Sql` | `Sql` static class with provider-translatable functions (`Sql.CurrentTimestamp`, `Sql.Cast`, `Sql.Lower`, `Sql.Property`, `Sql.AsSql`, etc.), plus `[Sql.Function]`, `[Sql.Expression]`, `[Sql.Extension]`, `[Sql.TableFunction]`, `[Sql.QueryExtension]`, the `Sql.Row` / `Sql.GroupBy` / `Sql.Window` / `Sql.Between` / `Sql.Collate` / `Sql.Aggregate` / `Sql.Special` / `Sql.Strings` / `Sql.DateTime` / `Sql.DateOnly` / `Sql.DateTimeOffset` / `Sql.Analytic` helper subtypes. | **Stable** — extension API for downstream consumers writing custom translatable methods. | [CORE](../areas/CORE/INDEX.md) |
| `LinqToDB.LinqExtensions` | `LoadWith`, `ThenLoad`, `AsCte`, `AsSubQuery`, `Concurrency`, `Insert`, `Update`, `Delete`, `Merge`, `MultiInsert`, `Take`/`Skip` hints, `TableHint`, `IndexHint`, `JoinHint`. The bulk of `IQueryable<T>` extensions live here. | **Stable.** | [CORE](../areas/CORE/INDEX.md) |
| `LinqToDB.Configuration` | `ILinqToDBSettings`, `LinqToDBSettings`, `LinqToDBSection`, `IConnectionStringSettings`, `ConnectionStringSettings`, `IDataProviderSettings`, `NamedValue`, `DataProviderElement(Collection)`. App-config / json-driven configuration. | **Stable.** | n/a — sub-namespace of CORE area |
| `LinqToDB.Linq` | `IExpressionInfo`, `LinqException`, `NoLinqCache`, `Tools`, the `IMergeable*` / `IUpdatable` / `*Insertable` interfaces. | **Stable** — small surface, used by downstream extensions. | [LINQ](../areas/LINQ/INDEX.md) |
| `LinqToDB.Linq.Translation` | `IMemberTranslator`, `ISqlExpressionTranslator`, `TranslationModifier`, `TranslationContext`. | **Stable** — extension hook for custom member translation. | [EXPR-TRANS](../areas/EXPR-TRANS/INDEX.md) |
| `LinqToDB.Interceptors` | `IInterceptor`, `ICommandInterceptor`, `IConnectionInterceptor`, `IDataContextInterceptor`, `IEntityServiceInterceptor`, `IExceptionInterceptor`, `IQueryExpressionInterceptor`, `IUnwrapDataObjectInterceptor`. | **Stable.** | [INTERCEPTORS](../areas/INTERCEPTORS/INDEX.md) |
| `LinqToDB.Metadata` | `IMetadataReader`, `MetadataInfo`, attribute / fluent metadata reader contracts. | **Stable.** | [METADATA](../areas/METADATA/INDEX.md) |
| `LinqToDB.SchemaProvider` | `ISchemaProvider`, `GetSchemaOptions`, `TableSchema`, `ColumnSchema`, schema-introspection contracts. | **Stable.** | [METADATA](../areas/METADATA/INDEX.md) |
| `LinqToDB.Common` | `Configuration` (the static-flag root, distinct from `LinqToDB.Configuration`), `Converter`, `ConvertTo<T>`, `Convert<TFrom,TTo>`, `Compilation`, `Array<T>`, `Option<T>`, `LinqToDBConvertException`. | **Stable.** | [INFRA](../areas/INFRA/INDEX.md) |
| `LinqToDB.Async` | `AsyncExtensions` — `IQueryable<T>.ToListAsync()`, `FirstAsync()`, `SingleAsync()`, etc. on linq2db queries, plus generated overloads in `AsyncExtensions.generated.cs`. | **Stable.** | [INFRA](../areas/INFRA/INDEX.md) |
| `LinqToDB.Concurrency` | `ConcurrencyExtensions` — optimistic-concurrency `UpdateConcurrent` / `DeleteConcurrent` / `Insert OrUpdate` helpers. | **Stable.** | sub-namespace of CORE |
| `LinqToDB.Expressions` | `IExpressionEvaluator`, `MemberHelper`, `ExpressionVisitorEx` — extension hooks for callers that work directly with expression trees. | **Stable.** | [EXPR](../areas/EXPR/INDEX.md) |
| `LinqToDB.Extensions` | `AttributesExtensions`, `MemberInfoExtensions`, etc. | **Stable.** | [EXPR](../areas/EXPR/INDEX.md) |
| `LinqToDB.Reflection` | `IObjectFactory` and reflection helpers consumers may reference from custom mapping. | **Stable.** | [INFRA](../areas/INFRA/INDEX.md) |
| `LinqToDB.Remote` | `ILinqService`, `LinqServiceClient`, `LinqServiceQuery`, `LinqServiceResult` — the in-tree client/server contracts used by all `LinqToDB.Remote.*` transport packages. | **Stable.** | [REMOTE-CLIENT](../areas/REMOTE-CLIENT/INDEX.md) |
| `LinqToDB.Tools` | in-tree user-facing tooling helpers (distinct from the `Source/LinqToDB.Tools/` companion package). | **Stable.** | [IN-TREE-TOOLS](../areas/IN-TREE-TOOLS/INDEX.md) |
| `LinqToDB.Mapping` (sub-namespaces under it) | extends the core mapping surface — interfaces like `IGenericInfoProvider`, `IToSqlConverter`. | **Stable.** | [MAPPING](../areas/MAPPING/INDEX.md) |
| `LinqToDB.Metrics` | `IActivity`, `ActivityService`, `ActivityID` — observability hooks emitted from the pipeline (`ActivityID.Build`, `BuildQuery`, `ExecuteNonQuery`, `Materialization`, etc.). | **Stable.** | sub-namespace of INFRA |
| `LinqToDB.SqlQuery` | **Legacy public AST namespace** — holds a small set of SQL-AST types (`SqlDataType`, `SqlObjectName`, `Precedence`, `SqlExtendedFunction`, `SqlFrameClause`, `SqlFrameBoundary`, `SqlFunctionArgument`, `SqlWindowOrderItem`, `MultiInsertType`, `DefaultNullable`, `ISqlExpressionExtensions`, `NoneExtensionBuilder`). Carries `// TODO: v7 - move to internal namespace` markers per `code-design.md`. | **Stable on the public-API contract** but **technical debt** — new AST types must go in `LinqToDB.Internal.SqlQuery`; existing ones move on next major. | [SQL-AST](../areas/SQL-AST/INDEX.md) |
| `LinqToDB.Internal.*` | All build-internal mechanics: `LinqToDB.Internal.SqlQuery` (full AST: `SqlStatement`, `SelectQuery`, `SqlField`, `SqlBinaryExpression`, all visitors), `LinqToDB.Internal.SqlProvider` (`BasicSqlBuilder`, `BasicSqlOptimizer`, `ISqlBuilder`, `ISqlOptimizer`), `LinqToDB.Internal.Linq` (`Query`, `Query<T>`, `IQueryRunner`, `QueryRunner`, `ExpressionQuery`), `LinqToDB.Internal.Linq.Builder` (every `*Builder` and `IBuildContext`), `LinqToDB.Internal.DataProvider`, `LinqToDB.Internal.Expressions`, `LinqToDB.Internal.Common`, `LinqToDB.Internal.Mapping`, `LinqToDB.Internal.Cache`, `LinqToDB.Internal.Async`, `LinqToDB.Internal.Conversion`, `LinqToDB.Internal.Extensions`, `LinqToDB.Internal.Infrastructure`, `LinqToDB.Internal.Interceptors`, `LinqToDB.Internal.Logging`, `LinqToDB.Internal.Metrics`, `LinqToDB.Internal.Options`, `LinqToDB.Internal.Reflection`, `LinqToDB.Internal.Remote`, `LinqToDB.Internal.SchemaProvider`. | **Mutable.** Public-only-because-providers-need-it; not covered by the back-compat contract; types in `Internal` may change between minor versions. New AST types **must** land here per `code-design.md` → SQL AST types live in `LinqToDB.Internal.SqlQuery`. | [INTERNAL-API](../areas/INTERNAL-API/INDEX.md) |

## ApiCompat enforcement

`Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt` (~2.2 MB) holds the symbol-level ledger of the shipped public surface; per-TFM additions live in `PublicAPI/<tfm>/PublicAPI.Shipped.txt`. The ApiCompat tool runs on `dotnet pack` and writes/checks `Source/LinqToDB/CompatibilitySuppressions.xml` — see [`code-design.md`](../../docs/code-design.md) → public-API contract and the `api-baselines` skill. Hand-editing those files is forbidden (`agent-rules.md` → Agent Guardrails).

## Ban list (`Source/BannedSymbols.txt`)

A complementary internal discipline — the file lists APIs the codebase forbids itself from calling, regardless of whether they're public on the BCL side: ADO.NET interfaces (`IDbConnection`, `IDbCommand` — must use `DbConnection` / `DbCommand`, `Source/BannedSymbols.txt:1–13`), `[ThreadStatic]` (`BannedSymbols.txt:14`), `ConcurrentBag<T>` (`BannedSymbols.txt:1`), reflection `Invoke` calls that swallow `TargetInvocationException` (`BannedSymbols.txt:247–273` — use the `InvokeExt` extension), culture-sensitive `ToString` / `Parse` overloads on every primitive numeric and date type (`BannedSymbols.txt:92–233`), `Expression<T>.Compile()` (`BannedSymbols.txt:239` — must use the `CompileExpression` extension), `DbCommand.DisposeAsync` (`BannedSymbols.txt:276` — must go through `IDataProvider.DisposeCommandAsync`), and a comprehensive replacement of `*.GetCustomAttribute*` / `*.GetCustomAttributes*` with the cached `AttributesExtensions.GetAttribute<T>` / `GetAttributes<T>` extensions (`BannedSymbols.txt:21–82`). Any new code calling these triggers a Roslyn error in Release builds.

## Pointers

- Design invariants: [`.claude/docs/code-design.md`](../../docs/code-design.md)
- Per-area public surface: see each `[area]/INDEX.md` under `../areas/`
- Public API surface text: `Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt`
- Banned API list: `Source/BannedSymbols.txt`

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 4 / 4 ✓
  - Source/BannedSymbols.txt
  - Source/LinqToDB/PublicAPI/PublicAPI.Shipped.txt (head + namespace probe)
  - Source/LinqToDB/IDataContext.cs (public-surface anchor)
  - Source/LinqToDB/LinqToDB.csproj (PublicAPI ItemGroup at :151–153)
- Tier 2 (visited / total): 3 / 3 ✓
  - Directory.Build.props (RunAnalyzersDuringBuild + warnings-as-errors discipline)
  - Source/LinqToDB/SqlQuery/ folder file list (legacy public AST inventory)
  - Source/LinqToDB/Sql/ folder file list (`Sql.*` public extension surface)
- Tier 3 (skipped, logged): 0
</details>
