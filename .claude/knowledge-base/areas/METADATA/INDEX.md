---
area: METADATA
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-03
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 9/9
coverage_tier_2: 11/11
---

# METADATA

Two distinct sub-systems live here:

1. **Attribute/Fluent metadata reading** (`Source/LinqToDB/Metadata/`) — runtime mechanism by which `MappingSchema` discovers how POCO types map to database objects. Consumed by [MAPPING](../MAPPING/INDEX.md).
2. **Database schema discovery** (`Source/LinqToDB/SchemaProvider/`) — scaffolding-time introspection of an existing database structure. Entry point is `ISchemaProvider`; the template-and-scaffold pipeline consumes `DatabaseSchema` to generate model code. Implementation base lives in [INTERNAL-API](../INTERNAL-API/INDEX.md) as `SchemaProviderBase`.

These two sub-systems share a namespace root but have no runtime dependency on each other.

---

## Subsystems

### Metadata reading

`IMetadataReader` (`Source/LinqToDB/Metadata/IMetadataReader.cs:8`) exposes three methods:

- `GetAttributes(Type)` — class-level `MappingAttribute[]`
- `GetAttributes(Type, MemberInfo)` — member-level `MappingAttribute[]`
- `GetDynamicColumns(Type)` — `MemberInfo[]` for dynamic-column properties
- `GetObjectID()` — stable cache key string

`MetadataReader` (`Source/LinqToDB/Metadata/MetadataReader.cs:17`) is the composite implementation. It holds an `IMetadataReader[]` and fans out each call, concatenating results via `MappingAttributesCache` (`Internal/Mapping/MappingAttributesCache.cs`, owned by INTERNAL-API). The static `MetadataReader.Default` is initialized with a single `AttributeReader` and is the reader installed in `MappingSchema.Default` (`Source/LinqToDB/Mapping/MappingSchema.cs:1500`).

`MappingSchema.AddMetadataReader` (`Source/LinqToDB/Mapping/MappingSchema.cs:1162`) is the entry point for composing readers at runtime. It wraps the incoming reader with any existing schema-chain readers inside a new `MetadataReader`. The `MetadataReader.GetRegisteredTypes()` method (`Source/LinqToDB/Metadata/MetadataReader.cs:104`) traverses nested `FluentMetadataReader` and `MetadataReader` instances to collect all fluent-mapped types — used by scaffolding and diagnostics.

### Concrete readers

| Reader | Source | TFM guard | Notes |
|---|---|---|---|
| `AttributeReader` | CLR reflection (`ICustomAttributeProvider.GetCustomAttributes`) | none | Default; uses a static `MappingAttributesCache` shared across all instances |
| `FluentMetadataReader` | Pre-built attribute dictionaries from `FluentMappingBuilder` | none | Constructed by `FluentMappingBuilder.Build()` (`Source/LinqToDB/Mapping/FluentMappingBuilder.cs:54`); holds `_types`, `_members`, `_dynamicColumns` as `ConcurrentDictionary`; `GetObjectID()` hashes type/member counts and attribute IDs |
| `XmlAttributeReader` | XML document (file, embedded resource, or stream) | none | Parses `<Type Name="…"><Member Name="…"><Attr />` schema; attribute instantiation via compiled `Expression.Lambda`; throws `MetadataException` on malformed XML |
| `SystemComponentModelDataAnnotationsSchemaAttributeReader` | `[System.ComponentModel.DataAnnotations.Schema.Table]` / `[Column]` | none | Translates `TableAttribute.Name` with schema-dot notation; maps to linq2db `TableAttribute` / `ColumnAttribute` |
| `SystemDataLinqAttributeReader` | `[System.Data.Linq.Mapping.Table]`, `[Column]`, `[Association]`, `[Database]` | `#if NETFRAMEWORK` | Legacy LINQ-to-SQL attribute interop; schema/name handling mirrors `SystemComponentModel…` reader |

### Schema discovery

`ISchemaProvider.GetSchema(DataConnection, GetSchemaOptions?)` (`Source/LinqToDB/SchemaProvider/ISchemaProvider.cs:9`) is the public contract. Callers note that several providers (MySQL, SQL Server, Sybase, DB2) must be called outside transactions.

`GetSchemaOptions` (`Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs:9`) governs what is read: `GetTables`, `GetForeignKeys`, `GetProcedures` (all `true` by default), inclusion/exclusion filter arrays for schemas and catalogs, `LoadTable` / `LoadProcedure` callbacks, and `ProcedureLoadingProgress` reporting.

`DatabaseSchema` (`Source/LinqToDB/SchemaProvider/DatabaseSchema.cs:9`) is the result aggregate:
- `Tables` (`List<TableSchema>`) — tables and views
- `Procedures` (`List<ProcedureSchema>`) — stored procedures and functions
- `DataTypesSchema` (`DataTable?`) — raw ADO.NET type-map table
- `ProviderSpecificTypeNamespace` — e.g. `"Oracle.DataAccess.Types"`

---

## Key types

### Metadata namespace (`LinqToDB.Metadata`)

| Type | Kind | Role |
|---|---|---|
| `IMetadataReader` | interface | Root contract; 4 methods |
| `MetadataReader` | class | Composite fan-out; installed in `MappingSchema` |
| `AttributeReader` | class | CLR-reflection backed; the default |
| `FluentMetadataReader` | class | Pre-built from `FluentMappingBuilder` dictionaries |
| `XmlAttributeReader` | class | XML-file/stream backed; supports file and embedded resources |
| `SystemComponentModelDataAnnotationsSchemaAttributeReader` | class | `[DataAnnotations.Schema.*]` interop |
| `SystemDataLinqAttributeReader` | class | `[System.Data.Linq.Mapping.*]` interop; `NETFRAMEWORK` only |
| `MetadataException` | class | Thrown by XML reader on malformed input or missing file |

### SchemaProvider namespace (`LinqToDB.SchemaProvider`)

| Type | Kind | Role |
|---|---|---|
| `ISchemaProvider` | interface | Single method: `GetSchema(DataConnection, GetSchemaOptions?)` |
| `DatabaseSchema` | class | Top-level result: tables, procedures, type map, provider namespace |
| `GetSchemaOptions` | class | Discovery toggles, inclusion/exclusion filters, callbacks |
| `TableSchema` | class | One table or view: columns, foreign keys, flags |
| `ColumnSchema` | class | One column: type, PK/identity/nullable flags, linq2db `DataType` |
| `ProcedureSchema` | class | One procedure/function: parameters, result table, load errors |
| `ParameterSchema` | class | One procedure parameter: direction flags, type info |
| `ForeignKeySchema` | class | One FK: this/other table/columns, `BackReference`, `AssociationType` |
| `AssociationType` | enum | `Auto`, `OneToOne`, `OneToMany`, `ManyToOne` |
| `LoadTableData` | struct (readonly) | Thin wrapper over `TableInfo`; passed to `GetSchemaOptions.LoadTable` filter delegate |
| `TableInfo` | class | Internal raw row from provider query; wraps `LoadTableData` |

---

## Files (Tier 1 / Tier 2)

**Tier 1 — read in full:**

- `Source/LinqToDB/Metadata/IMetadataReader.cs`
- `Source/LinqToDB/Metadata/MetadataReader.cs`
- `Source/LinqToDB/Metadata/AttributeReader.cs`
- `Source/LinqToDB/Metadata/FluentMetadataReader.cs`
- `Source/LinqToDB/SchemaProvider/ISchemaProvider.cs`
- `Source/LinqToDB/SchemaProvider/DatabaseSchema.cs`
- `Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs`
- `Source/LinqToDB/SchemaProvider/TableSchema.cs`
- `Source/LinqToDB/SchemaProvider/ColumnSchema.cs`

**Tier 2 — all visited:**

| File | Contribution |
|---|---|
| `Metadata/MetadataException.cs` | Exception type thrown by `XmlAttributeReader` on file-not-found or malformed XML; inherits `LinqToDBException`; parameterless ctor is `[Obsolete]` to discourage use |
| `Metadata/XmlAttributeReader.cs` | XML-document backed `IMetadataReader`; resolves file/resource in four probe order steps; instantiates attributes via compiled lambda (`Expression.MemberInit`); only supports linq2db-assembly-defined `MappingAttribute` subtypes |
| `Metadata/SystemComponentModelDataAnnotationsSchemaAttributeReader.cs` | Translates `[DataAnnotations.Schema.Table]` → `TableAttribute` (with schema-dot parsing) and `[DataAnnotations.Schema.Column]` → `ColumnAttribute`; covers the `Name`/`Schema`/`DbType` fields |
| `Metadata/SystemDataLinqAttributeReader.cs` | `NETFRAMEWORK`-only LINQ-to-SQL interop; translates `[Table]`, `[Column]`, `[Association]`, `[Database]` to linq2db equivalents; maps `IsDbGenerated` → `IsIdentity` |
| `SchemaProvider/AssociationType.cs` | Four-value enum for FK cardinality; `ForeignKeySchema.AssociationType` setter mirrors the inverse to `BackReference` |
| `SchemaProvider/ForeignKeySchema.cs` | FK descriptor: this/other table and column lists, `BackReference` pointer, `AssociationType` with self-mirroring setter |
| `SchemaProvider/LoadTableData.cs` | Read-only struct exposing `TableInfo` fields to the `GetSchemaOptions.LoadTable` filter; internal ctor ensures only `SchemaProviderBase` can create it |
| `SchemaProvider/ParameterSchema.cs` | Procedure/function parameter: `IsIn`/`IsOut`/`IsResult` direction flags, `DataType`, provider-specific type string |
| `SchemaProvider/ProcedureSchema.cs` | Procedure/function descriptor: `IsFunction`/`IsTableFunction`/`IsAggregateFunction` kind flags, `ResultTable`, `ResultException`, `SimilarTables`, parameter list |
| `SchemaProvider/TableInfo.cs` | Internal raw row from provider discovery query; wrapped by `LoadTableData` for public exposure; not part of the public `DatabaseSchema` result graph |

---

## Inbound / outbound dependencies

**Inbound (consumers of this area):**

- [MAPPING](../MAPPING/INDEX.md) — `MappingSchema` holds `IMetadataReader` per schema-info node (`MappingSchemaInfo.MetadataReader`); calls `GetAttributes<T>`, `GetDynamicColumns` during `EntityDescriptor`/`ColumnDescriptor` construction.
- `FluentMappingBuilder` (MAPPING) — constructs `FluentMetadataReader` from built attribute dictionaries and calls `MappingSchema.AddMetadataReader`.
- Scaffolding (CLI, SCAFFOLD, T4-TEMPLATES areas) — calls `ISchemaProvider.GetSchema` to produce `DatabaseSchema` that drives model code generation.

**Outbound (what this area depends on):**

- `LinqToDB.Mapping.MappingAttribute` — all reader contracts return subtypes of this (MAPPING owns the attribute hierarchy).
- `Internal/Mapping/MappingAttributesCache` — used by `MetadataReader`, `AttributeReader`, `FluentMetadataReader` to cache attribute lookups; owned by [INTERNAL-API](../INTERNAL-API/INDEX.md).
- `Internal/SchemaProvider/SchemaProviderBase` — abstract base that implements `ISchemaProvider.GetSchema` by wiring the `GetTables`/`GetColumns`/… abstract methods; per-vendor schema providers derive from it; owned by [INTERNAL-API](../INTERNAL-API/INDEX.md).
- `LinqToDB.Data.DataConnection` — `ISchemaProvider.GetSchema` parameter (DATA area).

---

## See also

- [MAPPING area](../MAPPING/INDEX.md) — `MappingSchema.AddMetadataReader`, `EntityDescriptor`, `ColumnDescriptor`.
- [INTERNAL-API area](../INTERNAL-API/INDEX.md) — `SchemaProviderBase`, `MappingAttributesCache`, `MappingSchemaInfo`.

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 9 / 9 ✓
  - Source/LinqToDB/Metadata/IMetadataReader.cs
  - Source/LinqToDB/Metadata/MetadataReader.cs
  - Source/LinqToDB/Metadata/AttributeReader.cs
  - Source/LinqToDB/Metadata/FluentMetadataReader.cs
  - Source/LinqToDB/SchemaProvider/ISchemaProvider.cs
  - Source/LinqToDB/SchemaProvider/DatabaseSchema.cs
  - Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs
  - Source/LinqToDB/SchemaProvider/TableSchema.cs
  - Source/LinqToDB/SchemaProvider/ColumnSchema.cs
- Tier 2 (visited / total): 11 / 11 (100%) ✓
  - Source/LinqToDB/Metadata/MetadataException.cs
  - Source/LinqToDB/Metadata/XmlAttributeReader.cs
  - Source/LinqToDB/Metadata/SystemComponentModelDataAnnotationsSchemaAttributeReader.cs
  - Source/LinqToDB/Metadata/SystemDataLinqAttributeReader.cs
  - Source/LinqToDB/SchemaProvider/AssociationType.cs
  - Source/LinqToDB/SchemaProvider/ForeignKeySchema.cs
  - Source/LinqToDB/SchemaProvider/LoadTableData.cs
  - Source/LinqToDB/SchemaProvider/ParameterSchema.cs
  - Source/LinqToDB/SchemaProvider/ProcedureSchema.cs
  - Source/LinqToDB/SchemaProvider/TableInfo.cs
- Tier 3 (skipped, logged): 0
</details>
