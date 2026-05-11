---
area: TESTS-MODEL
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 4/4
coverage_tier_2: 22/22
---

# TESTS-MODEL

Shared POCO / mapping library (`linq2db.Model` assembly) consumed by every test project. Contains entity definitions, `ITestDataContext`, concrete `DataConnection` subclasses, and transport-specific remote data contexts. No test logic lives here — only the schema model.

## Subsystems

**Core entities** — `Person`, `Patient`, `Doctor`, `ComplexPerson`, `Gender`, `FullName`, `TestIdentity`, `LinqDataTypes`, `LinqDataTypes2`. These map to the canonical test database tables that every provider test schema seeds.

**Parent/Child hierarchy** — `ParentChild.cs` defines `Parent`, `Child`, `GrandChild` plus numbered variants (`Parent1`–`Parent5`, `GrandChild1`, `Parent3`, `IParent`). `Parent` carries multiple `[Association]` properties including `ExpressionPredicate` and `[ExpressionMethod]`-backed computed associations (`GrandChildren2`, `GrandChildrenByID`). The file also contains `Functions` / `FunctionsExtensions` / `FunctionsOld` demonstrating `[Sql.TableFunction]` and `[Sql.TableExpression]` usage for table-valued functions.

**Inheritance variants** — Two separate inheritance hierarchies:
- `ParentChild.cs` `#region Inheritance*`: four discriminator hierarchies (`ParentInheritanceBase`/`Base2`/`Base3`/`Base4`) all mapping to the `Parent` table, exercising null-code defaults, enum discriminators, and abstract base types.
- `InheritanceParentChild.cs`: separate `InheritanceParent` / `InheritanceChild` tables with their own `[InheritanceMapping]` chains; `TInheritance` interface enforces `TypeDiscriminator` contract.

**Northwind schema** — `Northwind.cs` static container class holding 12 entity types (`Category`, `Customer`, `Employee`, `Order`, `OrderDetail`, `Product`/`ActiveProduct`/`DiscontinuedProduct`, `Region`, `Shipper`, `Supplier`, `Territory`, `EmployeeTerritory`). `Product` uses bool-discriminator inheritance (`Discontinued` column). `NorthwindDB` is a `DataConnection` subclass exposing all 13 Northwind tables plus `FreeTextTable<>` (SQL Server full-text) and `[Sql.TableExpression]` `WithUpdateLock<T>`.

**ITestDataContext / concrete contexts** — `ITestDataContext` extends `IDataContext` with 20 typed `ITable<T>` properties and one `[Sql.TableFunction]` method (`GetParentByID`). Implemented by:
- `TestDataConnection` — direct `DataConnection` subclass; also implements `ISystemSchemaData` (exposes `SystemSchemaModel` from `LinqToDB.Tools`).
- `TestDataCustomConnection` — wraps `TestDataConnection` via composition, implementing `IDataContext` manually; used to test non-`DataConnection` code paths.
- Four remote contexts (see below).

**Remote data contexts** — Each subclasses the transport's base class and implements `ITestDataContext` by delegating `GetTable<T>()`. `GetParentByID` throws `NotImplementedException` on all remote contexts (table functions are not supported over remote transports).

| Class | Base | TFM |
|---|---|---|
| `TestGrpcDataContext` | `GrpcDataContext` | `!NETFRAMEWORK` |
| `TestHttpContextDataContext` | `HttpClientDataContext` | `!NETFRAMEWORK` |
| `TestSignalRDataContext` | `SignalRDataContext` | all |
| `TestWcfDataContext` | `WcfDataContext` | `NETFRAMEWORK` only |

**Provider-specific entities** — Static container classes with inner types covering sequence / identity patterns unique to each provider:
- `FirebirdSpecific.SequenceTest` — `[SequenceName("SequenceTestSeq")]` + column-name workaround (`Value_` avoids FB reserved word).
- `OracleSpecific.SequenceTest` — uppercase table/sequence names, `SEQUENCETESTSEQ`. `OracleSpecific.StringTest` — equality helper for Oracle empty-string-equals-NULL semantics.
- `PostgreSQLSpecific` — four sequence/identity variants (`SequenceTest1`–`3`, `SequenceCustomNamingTest`) covering `[SequenceName]` + schema-qualified sequences, plus `TestSchemaIdentity` / `TestSerialIdentity` for schema-prefixed tables.

**Auxiliary types** — `IPerson` interface (5 members); `Gender` enum with `[MapValue]` single-char mappings (`M/F/U/O`); `TypeValue` enum with `[MapValue(null)]` for `Value0` (tests null-enum mapping); `Interfaces.cs` (`IIssue4031`, `IIssue4031<T>`, `Issue4031BaseExternal`) for interface-inheritance mapping issue regression; `Extensions.cs` (`BeginTransaction` extension on `ITestDataContext`).

## Key types

| Type | File | Role |
|---|---|---|
| `ITestDataContext` | `ITestDataContext.cs` | Contract for all test data contexts; 20 `ITable<T>` + `GetParentByID` |
| `TestDataConnection` | `TestDataConnection.cs` | Primary `DataConnection` impl; also `ISystemSchemaData` |
| `TestDataCustomConnection` | `TestDataCustomConnection.cs` | Composition-based impl; exercises non-`DataConnection` `IDataContext` paths |
| `Person` | `Person.cs` | Core test entity; `[Column(Configuration=...)]` per-provider overrides, `[Association]` to `Patient` |
| `ComplexPerson` | `ComplexPerson.cs` | Nested-record mapping (`[Column("FirstName","Name.FirstName")]`) via `FullName` value object |
| `LinqDataTypes` | `LinqDataTypes.cs` | Type-coverage table: `decimal`, `DateTime`, `bool`, `Guid`, `Binary`, `short`, `string` with provider-specific `DataType` overrides |
| `ParentInheritanceBase` | `ParentChild.cs:413` | Abstract root of `Parent`-table inheritance; null-code + value-code + default mappings |
| `InheritanceParentBase` | `InheritanceParentChild.cs:14` | Root of `InheritanceParent` table hierarchy; `TInheritance` interface |
| `NorthwindDB` | `NorthwindDB.cs` | `DataConnection` for Northwind; includes `FreeTextTable<>` and `WithUpdateLock<T>` helpers |
| `TestGrpcDataContext` | `Remote/Grpc/TestGrpcDataContext.cs` | gRPC remote context; skips cert validation for local test servers |
| `TestWcfDataContext` | `Remote/WCF/TestWcfDataContext.cs` | WCF remote context (`net462` only); `NetTcpBinding` with extended timeouts |

## Files (Tier 1 / Tier 2)

**Tier 1** (canonical anchors):

| File | Reason |
|---|---|
| `ITestDataContext.cs` | Defines the shared data-context contract for all test fixtures |
| `TestDataConnection.cs` | Primary concrete `DataConnection` used in 95%+ of tests |
| `Person.cs` | Core entity; exercises `[Column(Configuration)]`, `[Association]`, `IPerson` |
| `Tests.Model.csproj` | Project references reveal all remote transport dependencies |

**Tier 2** (all read, 22/22):

`Patient.cs`, `Doctor.cs`, `ParentChild.cs`, `InheritanceParentChild.cs`, `Northwind.cs`, `NorthwindDB.cs`, `LinqDataTypes.cs`, `ComplexPerson.cs`, `FullName.cs`, `Gender.cs`, `TypeValue.cs`, `TestIdentity.cs`, `IPerson.cs`, `Interfaces.cs`, `Extensions.cs`, `FirebirdSpecific.cs`, `OracleSpecific.cs`, `PostgreSQLSpecific.cs`, `TestDataCustomConnection.cs`, `Remote/Grpc/TestGrpcDataContext.cs`, `Remote/HttpContext/TestHttpContextDataContext.cs`, `Remote/SignalR/TestSignalRDataContext.cs`, `Remote/WCF/TestWcfDataContext.cs`

## Inbound / outbound dependencies

**Outbound (this area depends on):**
- `LinqToDB` (core) — `IDataContext`, `DataConnection`, `ITable<T>`, `DataOptions`, `MappingSchema`, `Sql.*` attributes
- `LinqToDB.Mapping` — `[Table]`, `[Column]`, `[PrimaryKey]`, `[Identity]`, `[Association]`, `[InheritanceMapping]`, `[SequenceName]`, `[MapValue]`, `[NotColumn]`, `[Nullable]`, `[NotNull]`
- `LinqToDB.Remote.SignalR` / `LinqToDB.Remote.SignalR.Client` — `SignalRDataContext`
- `LinqToDB.Remote.Grpc` — `GrpcDataContext` (`!NETFRAMEWORK`)
- `LinqToDB.Remote.HttpClient.Client` — `HttpClientDataContext` (`!NETFRAMEWORK`)
- `LinqToDB.Remote.Wcf` — `WcfDataContext` (`NETFRAMEWORK`)
- `LinqToDB.Scaffold` — referenced in csproj; `TestDataConnection` uses `SystemSchemaModel` from `LinqToDB.Tools`
- `LinqToDB.DataProvider.SqlServer` — `SqlServerExtensions.FreeTextTable` in `NorthwindDB`

**Inbound (consumers):**
- TESTS-INFRA (`Tests.Base`) — `TestBase` uses `ITestDataContext`; `TestBase.Tables.cs` seeds via `TestDataConnection`
- TESTS-LINQ (`Tests/Tests.Linq`) — every fixture queries through `ITestDataContext` / `TestDataConnection`
- TESTS-EFCORE — some fixtures use `NorthwindDB` for Northwind tests
- TESTS-VB — imports `Tests.Model` for VB LINQ tests
- All remote transport test areas (REMOTE-*) — construct `Test<X>DataContext` from `Remote/` subfolder

## Known issues / debt

- `TestDataCustomConnection` partially stubs `IDataContext` — `UseOptions`, `UseMappingSchema`, `AddMappingSchema`, `SetMappingSchema` are no-ops (return `null` / do nothing). This means tests using `TestDataCustomConnection` cannot exercise per-scope mapping overrides.
- `TestWcfDataContext` and `FirebirdSpecific` are `NETFRAMEWORK`-only; the rest of the remote contexts are `!NETFRAMEWORK`. There is no `net462`-compatible HTTP remote context variant.
- `GetParentByID` on all four remote contexts throws `NotImplementedException` — table-valued function tests implicitly require `TestDataConnection` and will silently skip on remote test runs unless the fixture guards on context type.

## See also

- [TESTS-INFRA INDEX](../TESTS-INFRA/INDEX.md) — `TestBase`, `DataProvider` helpers, seed-data scripts
- [TESTS-LINQ INDEX](../TESTS-LINQ/INDEX.md) — fixtures consuming these entities
- [REMOTE-CLIENT INDEX](../REMOTE-CLIENT/INDEX.md) — `RemoteDataContextBase` ancestry of the four remote contexts
- [MAPPING INDEX](../MAPPING/INDEX.md) — attribute resolution pipeline these entities exercise

<details><summary>Coverage</summary>

Tier 1 (4/4 read):
- `ITestDataContext.cs` — read in full
- `TestDataConnection.cs` — read in full
- `Person.cs` — read in full
- `Tests.Model.csproj` — read in full

Tier 2 (22/22 read):
- `Patient.cs` — read in full
- `Doctor.cs` — read in full
- `ParentChild.cs` — read in full (includes Parent, Parent1–5, Child, GrandChild, GrandChild1, ParentInheritanceBase*1–4, Functions/FunctionsExtensions/FunctionsOld)
- `InheritanceParentChild.cs` — read in full
- `Northwind.cs` — read in full
- `NorthwindDB.cs` — read in full
- `LinqDataTypes.cs` — read in full (LinqDataTypes + LinqDataTypes2)
- `ComplexPerson.cs` — read in full (ComplexPerson, ComplexPerson2, ComplexPerson3)
- `FullName.cs` — read in full
- `Gender.cs` — read in full
- `TypeValue.cs` — read in full
- `TestIdentity.cs` — read in full
- `IPerson.cs` — read in full
- `Interfaces.cs` — read in full (IIssue4031, IIssue4031<T>, Issue4031BaseExternal)
- `Extensions.cs` — read in full
- `FirebirdSpecific.cs` — read in full
- `OracleSpecific.cs` — read in full
- `PostgreSQLSpecific.cs` — read in full
- `TestDataCustomConnection.cs` — read in full
- `Remote/Grpc/TestGrpcDataContext.cs` — read in full
- `Remote/HttpContext/TestHttpContextDataContext.cs` — read in full
- `Remote/SignalR/TestSignalRDataContext.cs` — read in full
- `Remote/WCF/TestWcfDataContext.cs` — read in full

Tier 3: none
</details>
