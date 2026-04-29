---
area: MAPPING
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-04-27
last_verified_sha: a198cb803e80ae740cfc6afc7687536e78cd6cf2
coverage_tier_1: 3/3
coverage_tier_2: 43/43
---

# MAPPING — POCO ↔ table/column metadata + the per-context conversion graph

`Source/LinqToDB/Mapping/**` is the metadata layer that turns a user POCO into the column/table/association descriptors consumed by [EXPR-TRANS](../EXPR-TRANS/INDEX.md) (translation), [LINQ](../LINQ/INDEX.md) (materialization), and [DATA](../DATA/INDEX.md) (parameter binding). Two entangled responsibilities live here:

1. **Schema metadata.** `MappingSchema` is the per-context registry: type → `EntityDescriptor` → `ColumnDescriptor[]` + `AssociationDescriptor[]`, plus `InheritanceMapping`, dynamic-column accessors, query filters, fluent vs attribute-driven mapping. The attributes themselves (Column/Table/Association/PrimaryKey/Identity/MapValue/…) are also defined here — they're the public surface that user code annotates POCOs with, and the `Mapping/Builder/`-flavored fluent equivalent (`FluentMappingBuilder` → `EntityMappingBuilder<T>` → `PropertyMappingBuilder<TEntity,TProperty>`) builds the same attribute objects via lambdas.
2. **Type-conversion graph.** `MappingSchema` also owns the value-conversion pipeline: a multi-layer `Schemas[]` stack of `MappingSchemaInfo` records each holding a converter dictionary keyed by `(DbDataType from, DbDataType to, ConversionType)`. `GetConvertExpression` / `GetConverter` / `SetConvertExpression` / `SetConverter` walk that stack with type-simplification fallback (Simplify drops DbType → precision/scale → length → DataType). This subsumes both client-side enum/string conversions and the `T → DataParameter` lambdas used at parameter binding time. `IValueConverter` (and the `ValueConverterAttribute` per-column override) sits on top as the user-facing convert-on-this-column knob.

`MappingSchema` instances form a layered chain: a `MappingSchema(provider, baseSchemas)` snapshots its bases into `Schemas[]` (highest priority first), and lookups (`GetDataType`, `GetCanBeNull`, `GetConvertInfo`, `GetAttribute<T>`) walk that array. `MappingSchema.Default` is a `LockedMappingSchema` (defined in `Internal.Mapping`, out of this area) seeded with the built-in scalar types and the `ValueToSqlConverter` defaults. `CombineSchemas(ms1, ms2)` is the shared cache key the rest of the library uses to fold a connection-level schema into a provider-level schema without leaking N² instances.

## Key types

| Type | Role | File |
|---|---|---|
| `MappingSchema` | Per-context registry: attribute lookup, conversion graph, scalar-type table, `EntityDescriptor` cache | `MappingSchema.cs` |
| `EntityDescriptor` | Per-type table: name parts, columns, associations, inheritance, dynamic columns, query filter | `EntityDescriptor.cs` |
| `ColumnDescriptor` | Per-column: storage, DataType/DbType/Length/Precision/Scale, IsIdentity/IsPrimaryKey/CanBeNull, value converter, lambda factories for `GetDbValueLambda` / `GetDbParamLambda` / `GetProviderValue` | `ColumnDescriptor.cs` |
| `AssociationDescriptor` | Per-association: this/other keys, predicate or query expression, storage, alias, nullability inference | `AssociationDescriptor.cs` |
| `InheritanceMapping` | Discriminator value → concrete `Type` + reference to discriminator `ColumnDescriptor` | `InheritanceMapping.cs` |
| `MappingAttribute` (abstract) | Base class for every mapping attribute; carries `Configuration` (provider name filter) and `GetObjectID()` (mapping-schema config-id contributor) | `MappingAttribute.cs` |
| `FluentMappingBuilder` / `EntityMappingBuilder<T>` / `PropertyMappingBuilder<T,P>` | Lambda-driven attribute synthesis; `Build()` registers a `FluentMetadataReader` (in `Internal.Mapping`) on the schema | `FluentMappingBuilder.cs`, `EntityMappingBuilder.cs`, `PropertyMappingBuilder.cs` |
| `ValueToSqlConverter` | Per-type `Action<StringBuilder, SqlDataType, DataOptions, object>` table for inlining literals into SQL (used by SQL-PROVIDER builders for constants/scalars) | `ValueToSqlConverter.cs` |
| `IValueConverter` / `ValueConverter<TModel,TProvider>` / `ValueConverterFunc<,>` | User-defined per-column conversion lambdas, opt-in via `ValueConverterAttribute` | `IValueConverter.cs`, `ValueConverter.cs`, `ValueConverterFunc.cs`, `ValueConverterAttribute.cs` |
| `IEntityChangeDescriptor` / `IColumnChangeDescriptor` | Mutation surfaces handed to `EntityDescriptorCreatedCallback` so users (and EF interop) can rewrite descriptors after construction | `IEntityChangeDescriptor.cs`, `IColumnChangeDescriptor.cs` |
| `MapValue` / `MapValueAttribute` | Enum value ↔ database value bidirectional mapping; multiple per field allowed | `MapValue.cs`, `MapValueAttribute.cs` |
| `SkipBaseAttribute` / `SkipValuesByListAttribute` / `SkipValuesOnInsert/UpdateAttribute` / `SkipModification` | Abstract + value-list-driven skip-on-insert/update predicates, aggregated into `ColumnDescriptor.SkipModificationFlags` | `SkipBaseAttribute.cs`, `SkipValuesByListAttribute.cs`, `SkipValuesOn{Insert,Update}Attribute.cs`, `SkipModification.cs` |
| `OptimisticLockPropertyAttribute` / `OptimisticLockPropertyBaseAttribute` / `VersionBehavior` | Concurrency-column update strategies (`Auto` / `AutoIncrement` / `Guid`); used by `LinqToDB.Concurrency.ConcurrencyExtensions` | `OptimisticLockPropertyAttribute.cs`, `OptimisticLockPropertyBaseAttribute.cs`, `VersionBehavior.cs` |
| `DefaultValue` (static) / `DefaultValue<T>` | Per-type default-value table substituted for `null` reads; seeded for primitives, populated lazily for enums via `GetMapValues` | `DefaultValue.cs` |
| `IGenericInfoProvider` | Extensibility point for adding generic-type conversions (e.g. `IEnumerable<T> → ImmutableList<T>`); registered with `MappingSchema.SetGenericConvertProvider(typeof(MyProvider<>))` | `IGenericInfoProvider.cs` |
| `IToSqlConverter` | Marker interface for objects that know how to render themselves to `ISqlExpression` directly | `IToSqlConverter.cs` |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full, sets `area: MAPPING` ownership)

- `Source/LinqToDB/Mapping/MappingSchema.cs`
- `Source/LinqToDB/Mapping/EntityDescriptor.cs`
- `Source/LinqToDB/Mapping/ColumnDescriptor.cs`

### Tier 2 (read; mapping attributes + descriptors + builders + value-conversion plumbing)

- Descriptors / interfaces: `AssociationDescriptor.cs`, `InheritanceMapping.cs`, `IColumnChangeDescriptor.cs`, `IEntityChangeDescriptor.cs`, `IGenericInfoProvider.cs`, `IValueConverter.cs`, `IToSqlConverter.cs`, `MappingAttribute.cs`, `MapValue.cs`
- Mapping attributes: `TableAttribute.cs`, `ColumnAttribute.cs`, `ColumnAliasAttribute.cs`, `NotColumnAttribute.cs`, `PrimaryKeyAttribute.cs`, `IdentityAttribute.cs`, `AssociationAttribute.cs`, `InheritanceMappingAttribute.cs`, `DataTypeAttribute.cs`, `NullableAttribute.cs`, `NotNullAttribute.cs`, `ScalarTypeAttribute.cs`, `SequenceNameAttribute.cs`, `MapValueAttribute.cs`, `DynamicColumnsStoreAttribute.cs`, `DynamicColumnAccessorAttribute.cs`, `OptimisticLockPropertyAttribute.cs`, `OptimisticLockPropertyBaseAttribute.cs`, `QueryFilterAttribute.cs`, `ResultSetIndexAttribute.cs`, `ServerSideOnlyAttribute.cs`, `IsQueryableAttribute.cs`, `SkipBaseAttribute.cs`, `SkipValuesByListAttribute.cs`, `SkipValuesOnInsertAttribute.cs`, `SkipValuesOnUpdateAttribute.cs`, `SqlQueryDependentAttribute.cs`, `SqlQueryDependentParamsAttribute.cs`, `ValueConverterAttribute.cs`
- Fluent builder: `FluentMappingBuilder.cs`, `EntityMappingBuilder.cs`, `PropertyMappingBuilder.cs`
- Value layer: `ValueConverter.cs`, `ValueConverterFunc.cs`, `ValueToSqlConverter.cs`, `DefaultValue.cs`, `ConversionType.cs`, `SkipModification.cs`, `VersionBehavior.cs`

### Tier 3

None matched (no generated/build artifacts under `Source/LinqToDB/Mapping/`).

## Subsystems

### 1. `MappingSchema` registry + `Schemas[]` stack

Constructed via `new MappingSchema(string? configuration, params MappingSchema[]? schemas)` (`MappingSchema.cs:119`). The constructor flattens the bases into a deduped `MappingSchemaInfo[] Schemas` array (`MappingSchema.cs:158-181`); `Schemas[0]` is the schema's own writable layer, the rest are read-only ancestors. Every "get" method (`GetDefaultValue`, `GetCanBeNull`, `GetDataType`, `GetMapValues`, `GetAttribute<T>`, `GetConvertInfo`) walks the array front-to-back; every "set" method writes into `Schemas[0]` under `_syncRoot` and calls `ResetID()` so the cached `ConfigurationID` is recomputed (`MappingSchema.cs:1390-1403`). `CombineSchemas(ms1, ms2)` (`MappingSchema.cs:52-66`) memoises the (locked, locked) → combined schema, which is how the connection-level schema fuses with the provider's locked schema without exploding allocations across queries.

`MappingSchema.Default` is a singleton `LockedMappingSchema` (`Internal.Mapping`, out of this area) initialised in the nested `DefaultMappingSchema` ctor (`MappingSchema.cs:1446-1492`): it seeds the scalar-type table (`AddScalarType(typeof(string), DataType.NVarChar)`, etc.), registers `DBNull → object?` and `DateTime → string` converters, and primes `ValueToSqlConverter`. Provider-specific schemas (e.g. `SqlServerMappingSchema`) extend `LockedMappingSchema` and chain off `Default`.

### 2. `EntityDescriptor` construction

`EntityDescriptor.Init` (`EntityDescriptor.cs:164-297`) is the single hot path that produces an entity's column/association set:

1. Reads `[Table]` from the schema → seeds `Name = SqlObjectName(name, server, database, schema)`, `TableOptions`, `IsColumnAttributeRequired`.
2. Reads `[QueryFilter]` for a soft-delete-style ambient filter (`QueryFilterLambda` / `QueryFilterFunc`).
3. `InitializeDynamicColumnsAccessors` (`EntityDescriptor.cs:488-665`) inspects `[DynamicColumnsStore]` / `[DynamicColumnAccessor]` and synthesises three lambdas — getter (TryGetValue or user-supplied), setter (Item-indexer assign), and a storage-initializer that lazily news the `Dictionary<string,object>` on first write. These are persisted on `EntityDescriptor` so the LINQ side can compile them into materializers.
4. Iterates `TypeAccessor.Members` (concat with `MappingSchema.GetDynamicColumns(ObjectType)`):
   - `[Association]` → constructs `AssociationDescriptor` from the attribute's keys / predicate / query / storage / setter.
   - `[Column]` → for each `ColumnAttribute` with distinct `MemberName`, either constructs a `ColumnDescriptor` directly or queues a class-level redirect (the `attrs` list, used when `MemberName` resolves a nested path like `"Residence.Street"`). See the dot-path handling in `SetColumn` (`EntityDescriptor.cs:299-328`).
   - No column attribute, but `MappingSchema.IsScalarType(member.Type)` is true OR the member has `[Identity]` / `[PrimaryKey]` → still creates a `ColumnDescriptor` (the implicit-mapping path; suppressed by `IsColumnAttributeRequired`).
   - `[ColumnAlias]` → registered in `_aliases` so `entityDescriptor[memberName]` falls through to the aliased column.
   - `[ExpressionMethod(IsColumn = true)]` → tracked in `CalculatedMembers`.
5. Type-level `[Column]` attributes are applied last (`EntityDescriptor.cs:283-291`), letting class-level overrides win over member-level ones for the same path.
6. `InitInheritanceMapping` (`EntityDescriptor.cs:349-428`) collects `[InheritanceMapping]` attrs, recursively builds `EntityDescriptor`s for each child type via `MappingSchema.GetEntityDescriptor`, picks the column flagged `IsDiscriminator = true` as the shared discriminator, and sorts the array most-specific first so consumers can dispatch by `IsAssignableFrom` order.

`EntityDescriptor` instances are cached per `(Type, schemaConfigId)` in `EntityDescriptorsCache` (`MappingSchema.cs:1835-1857`). Cache invalidation is implicit — `ResetID` bumps `ConfigurationID`, and the next `GetEntityDescriptor` call sees a cache miss for the new key.

### 3. `ColumnDescriptor` construction + lambda compilation

Constructor (`ColumnDescriptor.cs:32-148`) resolves the column metadata from three sources in priority order: explicit `ColumnAttribute` properties, the schema's `GetDataType(MemberType)` / `GetUnderlyingDataType` for type-default values, then per-member fallbacks (`[DataType]`, `[Nullable]`, nullability annotations, `[PrimaryKey]`, `[Sequence]`, `[ValueConverter]`, `[Skip*]`).

The descriptor then exposes lazily-compiled lambda factories (`ColumnDescriptor.cs:507-802`):

- `GetOriginalValueLambda()` — `obj => obj.Member` (or storage member). The "raw" extractor without conversion.
- `GetDbParamLambda()` — full pipeline: extractor → discriminator-default substitution (only for `IsDiscriminator` columns with inheritance, generates `Expression.Condition(TypeIs(obj, mappedType), code, …)` chains) → `ApplyConversions(getterExpr, dbDataType, includingEnum: true)`. The `static ApplyConversions` (`ColumnDescriptor.cs:661-760`) is the canonical "convert from CLR value to provider value" pipeline: type-prep converter → user `IValueConverter` (with null guards if `!HandlesNulls`) → registered `T → DataParameter` lambda from the schema, with enum→underlying-type fallback when no explicit converter exists. The same logic is reused by EXPR-TRANS for inline expression rewrites that need column conversions.
- `GetDbValueLambda()` — same as `GetDbParamLambda` but unwraps `DataParameter.Value` if the pipeline ends in a `DataParameter`. This is what materialization paths use when they only need the raw value.
- `GetProviderValue(object)` — compiles `GetDbValueLambda` into a `Func<object,object>` and caches it in `_getter`. With inheritance, the compiled getter checks `obj is MemberAccessor.TypeAccessor.Type` first and falls back to the default value to avoid wrong-type access.

### 4. Type-conversion graph (`GetConverter` / `SetConvertExpression`)

`GetConverter(DbDataType from, DbDataType to, bool create, ConversionType conversionType)` (`MappingSchema.cs:894-1024`) is the workhorse. The lookup walks `Schemas[]`, simplifying both `from` and `to` via `Simplify` (drop `DbType` → drop `Precision/Scale` → drop `Length` → drop `DataType`) on misses. If still nothing matches and `create` is true, it tries nullable-unwrap fallbacks (`int? → byte` becomes `int → byte` then convert `int? → int`), then defers to `ConvertInfo.Default.Create` (in `Internal.Conversion`) for the auto-generated baseline conversion.

`SetConvertExpression(fromType, toType, expr, addNullCheck)` is the public registration entrypoint (`MappingSchema.cs:565-589`). When `addNullCheck` is set and `to ≠ DataParameter` and the user expression doesn't already use `DefaultValue` placeholders, the expression is wrapped with `AddNullCheck` (returns `default` for null source). The setter then calls `SetNullableConversion` to derive a `T? → DataParameter` companion converter on demand (lifts the `T → DataParameter` lambda with `null → DataParameter.ClearValue + SetType(fromNullable)`).

`ConversionType` (`ConversionType.cs`) splits the converter table into three bands: `Common` (used for both directions), `ToDatabase` (parameter binding side), `FromDatabase` (materialization side). The lookup tries the requested band first then falls through to `Common`.

### 5. `ValueToSqlConverter` — literal inlining

Distinct from the lambda conversion graph: `ValueToSqlConverter` (`ValueToSqlConverter.cs`) holds `Action<StringBuilder, DbDataType, DataOptions, object>` per type and is what the SQL-PROVIDER builders use when they need to inline a constant directly into the generated SQL (rather than parameterise it). `SetDefaults` registers integer/float/decimal/string/Guid/DateTime/`SqlByte`/`SqlInt32`/etc. converters that match each provider's literal syntax. Every `MappingSchema` clones from its parent's converter via `BaseConverters` so provider-specific overrides (e.g. SQL Server `bit` literal as `1`/`0`, Oracle `DATE` formatting) cascade properly.

### 6. Fluent builder

`FluentMappingBuilder` (`FluentMappingBuilder.cs`) accumulates `Type → List<MappingAttribute>` and `MemberInfo → List<MappingAttribute>` dictionaries; `Build()` packages them into a `FluentMetadataReader` (in `Internal.Mapping`) that's pushed onto the schema's reader chain via `MappingSchema.AddMetadataReader`. `EntityMappingBuilder<TEntity>` and `PropertyMappingBuilder<TEntity,TProperty>` are lambda-typed wrappers around it that synthesise `TableAttribute` / `ColumnAttribute` / `AssociationAttribute` / `PrimaryKeyAttribute` / `IdentityAttribute` / `InheritanceMappingAttribute` / `QueryFilterAttribute` / `DynamicColumnsStoreAttribute` from `Expression<Func<TEntity, TProperty>>` member-accessor expressions. `EntityMappingBuilder.SetAttribute` (`EntityMappingBuilder.cs:638-686`) is the merge operation: if an attribute already exists in the fluent-side cache, mutate it; if it exists only in the underlying schema, copy via `overrideAttribute` and re-register; otherwise new it.

## Interactions

- **METADATA → MAPPING.** `Source/LinqToDB/Metadata/**` provides `IMetadataReader` (attribute discovery from CLR attributes, `System.ComponentModel.DataAnnotations`, fluent registrations, XML files). `MappingSchema.AddMetadataReader` (`MappingSchema.cs:1162-1183`) prepends a reader to `Schemas[0].MetadataReader`, and `_cache` / `_firstOnlyCache` (`MappingSchema.cs:1210-1267`) memoise the per-`(type, member, attrType)` filtered-by-`ConfigurationList` results. This is the only inbound dep — MAPPING does not know how attributes were sourced, only that they implement `MappingAttribute`.
- **MAPPING → EXPR-TRANS.** Translation looks up entity descriptors via `dataContext.MappingSchema.GetEntityDescriptor(type)` to discover columns / associations during expression rewriting. `EntityDescriptor.this[memberName]` and `FindColumnDescriptor(MemberInfo)` are the two main lookup APIs.
- **MAPPING → LINQ.** Materialization compiles column readers from `ColumnDescriptor.GetDbDataType` (for the data reader call shape) plus the `FromDatabase` converters in the schema, and uses `EntityDescriptor.InheritanceMapping` for discriminator dispatch when constructing materialised entities.
- **MAPPING → DATA.** `DataConnection`/`DataParameter` paths call `ColumnDescriptor.GetDbParamLambda()` (or the static `ColumnDescriptor.ApplyConversions`) to convert a CLR value to the provider's parameter representation at execution time. `DefaultValue.GetValue` is what materialization substitutes when a non-nullable column reads NULL.
- **MAPPING → SQL-PROVIDER.** SQL builders consume `MappingSchema.ValueToSqlConverter` to inline literals, and `MappingSchema.GetDataType` / `ColumnDescriptor.DataType` / `DbType` to render column types in `CREATE TABLE` and parameter casts.

## Inbound / outbound dependencies

### Inbound (who reads MAPPING)

- `LinqToDB.Internal.Linq.Builder.*` — entity descriptor lookup, association resolution, column reader compilation.
- `LinqToDB.Data.*` — parameter binding via `ColumnDescriptor.GetDbParamLambda`.
- `LinqToDB.Internal.SqlProvider.*` — `ValueToSqlConverter` for literal inlining; `MappingSchema.GetDataType` for type rendering.
- `LinqToDB.Concurrency.ConcurrencyExtensions` — reads `OptimisticLockPropertyAttribute` via `MappingSchema.GetAttribute` to inject version-column updates.
- EF interop (`LinqToDB.EntityFrameworkCore.*`) — uses `IEntityChangeDescriptor` / `IColumnChangeDescriptor` and `EntityDescriptorCreatedCallback` to remap names / types in EF-driven schemas.

### Outbound (what MAPPING reads)

- `LinqToDB.Metadata.IMetadataReader` — attribute discovery.
- `LinqToDB.Internal.Conversion.ConvertInfo` / `Converter` — generated default conversions when no user converter exists.
- `LinqToDB.Internal.Mapping.MappingSchemaInfo` / `LockedMappingSchemaInfo` / `LockedMappingSchema` / `FluentMetadataReader` — internal storage and lock semantics for the schema layers.
- `LinqToDB.Reflection.TypeAccessor` / `MemberAccessor` — reflection caching used by `EntityDescriptor.TypeAccessor` and the dynamic-column setter generation.
- `LinqToDB.SqlQuery.SqlObjectName` / `SqlDataType` / `DbDataType` — value types for table identity and column type metadata.
- `LinqToDB.Common.Configuration` — `UseNullableTypesMetadata`, `IsStructIsScalarType`, `Linq.CacheSlidingExpiration` flags consumed during descriptor init.

## Known issues / debt

- **`ColumnAliasAttribute` admits cycles** (`ColumnAliasAttribute.cs:5-12`). The header comment flags two issues: alias-to-alias chains can loop into stack overflow, and the attribute can be applied with a null `MemberName` (which `EntityDescriptor.Init` later throws on lazily). No cycle detection in `EntityDescriptor.this[memberName]` — it just recurses through `_aliases.TryGetValue`.
- **`MappingSchema.GetMapValues` returns `null` for non-enum types** (`MappingSchema.cs:1766-1793`). Comment marks it as `TODO: v7: make it throw for non-enum type`. Several call sites (`GetUnderlyingDataType`, `GetDefaultValue`, `GetCanBeNull`) defensively `null!`-suppress on the return.
- **`EntityDescriptor.Init` mutation interleaving.** `_columns` / `_columnNames` / `_aliases` are appended to on a foreach over `members` while a separate type-level pass appends/replaces via `SetColumn`. The order is significant (member-level wins, then type-level overrides) but the duplication of "remove + re-add" logic in `SetColumn` (`EntityDescriptor.cs:299-328`) couples the two passes and would silently lose member ordering if the iteration order of `TypeAccessor.Members` ever became non-deterministic.
- **`InitializeDynamicColumnsAccessors` uses `member.Name` without composite-path handling** (`EntityDescriptor.cs:537`). The synthesised `DynamicColumnsStore` `ColumnDescriptor` uses `new ColumnAttribute(member.Name)` — fine because dynamic store members are simple properties, but the surrounding code in the same method sometimes synthesises `MemberAccessor`s for paths.
- **`ConfigurationID` recomputation cost.** `ResetID` (`MappingSchema.cs:1390-1403`) clears `_configurationID`; the next read recomputes by hashing every `Schemas[i].ConfigurationID` via `IdentifierBuilder`. Frequent `Set*` calls on a non-locked schema during application warm-up will repeatedly recompute, but this is O(layers) and bounded.
- **`SetDefaultValue` enum branch lazily mutates `Schemas[0]` from inside `GetDefaultValue`** (`MappingSchema.cs:243-274`). Memoising into `Schemas[0]` from a "get" path means a query against an unlocked schema can mutate it concurrently — the `_syncRoot` lock guards the write, but consumers that read `Schemas[0].GetDefaultValue` on another thread without going through the public method see torn state. Same pattern in `GetCanBeNull` (`MappingSchema.cs:300-331`).
- **Nullable null-check addition in `SetConvertExpression`** is opt-out only via `addNullCheck=false`; users who want explicit control over nullability must remember the flag, and the `SetNullableConversion` (`MappingSchema.cs:859-892`) auto-derivation can collide with a separately-registered explicit `T?` converter (it skips iff one already exists, but the order of registrations matters).

## See also

- [`architecture.md`](../../architecture.md) — pipeline overview (where MAPPING fits between Metadata and Linq.Builder).
- [`code-design.md`](../../code-design.md) — public-API contract that constrains everything in `Source/LinqToDB/Mapping/**` (no namespace changes, attribute equality must round-trip).
- [METADATA area](../METADATA/INDEX.md) — sources of `MappingAttribute` instances consumed by `MappingSchema`.
- [EXPR-TRANS area](../EXPR-TRANS/INDEX.md) — primary consumer of `EntityDescriptor` / `ColumnDescriptor` during translation.
- [LINQ area](../LINQ/INDEX.md) — materialization / column-reader compilation site.

## Pointers

- New mapping attribute? Inherit `MappingAttribute`, override `GetObjectID()` (must be deterministic across runs — it feeds `ConfigurationID`), apply `[AttributeUsage(..., AllowMultiple = true)]` if multiple-per-target makes sense, and pick up the `Configuration` filter pattern from existing attributes (`PrimaryKeyAttribute.cs:48-51` is a tight example).
- Adding a built-in conversion? Extend the `DefaultMappingSchema` ctor (`MappingSchema.cs:1446-1492`) with `SetConverter<TFrom,TTo>(...)` for the common direction; the `SetNullableConversion` machinery will derive the `T? → DataParameter` companion automatically.
- Adding a per-column knob? Add a property to `ColumnAttribute`, an explicit accessor on `ColumnDescriptor`, and read it during `ColumnDescriptor`'s ctor — the `Has*()` pattern (`HasLength`, `HasPrecision`, `HasIsIdentity`) is the convention for distinguishing "user set this explicitly" from "type default".
- Investigating why a column has the wrong DataType / nullability? Walk the precedence in `ColumnDescriptor` ctor: explicit `[Column].DataType` → schema `GetDataType(MemberType)` → `GetUnderlyingDataType(MemberType)` → `[DataType]` member attribute. For nullability: `[Column].CanBeNull` → `[Nullable]` → `Configuration.UseNullableTypesMetadata` + NRT analysis → `IsIdentity` → schema `GetCanBeNull` (`ColumnDescriptor.cs:150-166`).
- Touching `EntityDescriptor.Init`? The two-pass member-then-type ordering is load-bearing (class-level `[Column("col", "Member.Sub")]` must override member-level `[Column]` on `Member.Sub`). Keep `attrs` deferral, the `_columnNames.Remove` + `_columns.RemoveAll` pair in `SetColumn`, and the `IsColumnAttributeRequired` guard.

<details><summary>Coverage</summary>

Tier 1: 3/3 visited (read in full).
- `Source/LinqToDB/Mapping/MappingSchema.cs` — read 1965 lines in full (split across reads).
- `Source/LinqToDB/Mapping/EntityDescriptor.cs` — read 668 lines in full.
- `Source/LinqToDB/Mapping/ColumnDescriptor.cs` — read 805 lines in full.

Tier 2: 43/43 visited (≥90% target met). All Tier-2 files under `Source/LinqToDB/Mapping/**` were read.
- Two files were read partially because their bulk is repetitive registration code that doesn't change the architectural picture: `ValueToSqlConverter.cs` (first 120 lines covering `SetDefaults` + structure; remaining body is per-type `Build*` helpers and BaseConverters traversal — same pattern repeated), `PropertyMappingBuilder.cs` (first 120 lines covering ctor + the `HasAttribute` / `Property` / `Member` / `Association` shapes; remainder is per-attribute fluent helpers (`HasLength`, `HasPrecision`, `IsColumn`, `IsPrimaryKey`, …) that delegate to `_entity.HasAttribute(_memberInfo, new XAttribute { … })`).
- All other Tier-2 files were read in full.

Tier 3: 0/0 — no generated/build artifacts under `Source/LinqToDB/Mapping/`.

</details>
