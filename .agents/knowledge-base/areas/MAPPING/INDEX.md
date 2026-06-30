---
area: MAPPING
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-14
last_verified_sha: b3340aa9ded15ffc626983fd202e6399daa081ca
coverage_tier_1: 3/3
coverage_tier_2: 43/43
---

# MAPPING -- POCO <-> table/column metadata + the per-context conversion graph

`Source/LinqToDB/Mapping/**` is the metadata layer that turns a user POCO into the column/table/association descriptors consumed by [EXPR-TRANS](../EXPR-TRANS/INDEX.md) (translation), [LINQ](../LINQ/INDEX.md) (materialization), and [DATA](../DATA/INDEX.md) (parameter binding). Two entangled responsibilities live here:

1. **Schema metadata.** `MappingSchema` is the per-context registry: type -> `EntityDescriptor` -> `ColumnDescriptor[]` + `AssociationDescriptor[]`, plus `InheritanceMapping`, dynamic-column accessors, query filters, fluent vs attribute-driven mapping. The attributes themselves (Column/Table/Association/PrimaryKey/Identity/MapValue/...) are also defined here -- they are the public surface that user code annotates POCOs with, and the `Mapping/Builder/`-flavored fluent equivalent (`FluentMappingBuilder` -> `EntityMappingBuilder<T>` -> `PropertyMappingBuilder<TEntity,TProperty>`) builds the same attribute objects via lambdas.
2. **Type-conversion graph.** `MappingSchema` also owns the value-conversion pipeline: a multi-layer `Schemas[]` stack of `MappingSchemaInfo` records each holding a converter dictionary keyed by `(DbDataType from, DbDataType to, ConversionType)`. `GetConvertExpression` / `GetConverter` / `SetConvertExpression` / `SetConverter` walk that stack with type-simplification fallback (Simplify drops DbType -> precision/scale -> length -> DataType). This subsumes both client-side enum/string conversions and the `T -> DataParameter` lambdas used at parameter binding time. `IValueConverter` (and the `ValueConverterAttribute` per-column override) sits on top as the user-facing convert-on-this-column knob.

`MappingSchema` instances form a layered chain: a `MappingSchema(provider, baseSchemas)` snapshots its bases into `Schemas[]` (highest priority first), and lookups (`GetDataType`, `GetCanBeNull`, `GetConvertInfo`, `GetAttribute<T>`) walk that array. `MappingSchema.Default` is a `LockedMappingSchema` (defined in `Internal.Mapping`, out of this area) seeded with the built-in scalar types and the `ValueToSqlConverter` defaults. `CombineSchemas(ms1, ms2)` is the shared cache key the rest of the library uses to fold a connection-level schema into a provider-level schema without leaking N^2 instances.

`MappingSchema` implements `IEquatable<MappingSchema>`; equality is based on `ConfigurationID` (`MappingSchema.cs:1941-1963`). `ValueToSqlConverter` similarly implements `IEquatable<ValueToSqlConverter>` (`ValueToSqlConverter.cs:331-351`).

## Key types

| Type | Role | File |
|---|---|---|
| `MappingSchema` | Per-context registry: attribute lookup, conversion graph, scalar-type table, `EntityDescriptor` cache | `MappingSchema.cs` |
| `EntityDescriptor` | Per-type table: name parts, columns, associations, inheritance, dynamic columns, query filter | `EntityDescriptor.cs` |
| `ColumnDescriptor` | Per-column: storage, DataType/DbType/Length/Precision/Scale, IsIdentity/IsPrimaryKey/CanBeNull, value converter, lambda factories for `GetDbValueLambda` / `GetDbParamLambda` / `GetProviderValue` | `ColumnDescriptor.cs` |
| `AssociationDescriptor` | Per-association: this/other keys, predicate or query expression, storage, alias, nullability inference | `AssociationDescriptor.cs` |
| `InheritanceMapping` | Discriminator value -> concrete `Type` + reference to discriminator `ColumnDescriptor` | `InheritanceMapping.cs` |
| `MappingAttribute` (abstract) | Base class for every mapping attribute; carries `Configuration` (provider name filter) and `GetObjectID()` (mapping-schema config-id contributor) | `MappingAttribute.cs` |
| `FluentMappingBuilder` / `EntityMappingBuilder<T>` / `PropertyMappingBuilder<T,P>` | Lambda-driven attribute synthesis; `Build()` registers a `FluentMetadataReader` (in `Internal.Mapping`) on the schema | `FluentMappingBuilder.cs`, `EntityMappingBuilder.cs`, `PropertyMappingBuilder.cs` |
| `ValueToSqlConverter` | Per-type `Action<StringBuilder, DbDataType, DataOptions, object>` table for inlining literals into SQL (used by SQL-PROVIDER builders for constants/scalars); implements `IEquatable<ValueToSqlConverter>` | `ValueToSqlConverter.cs` |
| `IValueConverter` / `ValueConverter<TModel,TProvider>` / `ValueConverterFunc<,>` | User-defined per-column conversion lambdas, opt-in via `ValueConverterAttribute` | `IValueConverter.cs`, `ValueConverter.cs`, `ValueConverterFunc.cs`, `ValueConverterAttribute.cs` |
| `IEntityChangeDescriptor` / `IColumnChangeDescriptor` | Mutation surfaces handed to `EntityDescriptorCreatedCallback` so users (and EF interop) can rewrite descriptors after construction | `IEntityChangeDescriptor.cs`, `IColumnChangeDescriptor.cs` |
| `MapValue` / `MapValueAttribute` | Enum value <-> database value bidirectional mapping; multiple per field allowed | `MapValue.cs`, `MapValueAttribute.cs` |
| `SkipBaseAttribute` / `SkipValuesByListAttribute` / `SkipValuesOnInsert/UpdateAttribute` / `SkipModification` | Abstract + value-list-driven skip-on-insert/update predicates, aggregated into `ColumnDescriptor.SkipModificationFlags` | `SkipBaseAttribute.cs`, `SkipValuesByListAttribute.cs`, `SkipValuesOn{Insert,Update}Attribute.cs`, `SkipModification.cs` |
| `OptimisticLockPropertyAttribute` / `OptimisticLockPropertyBaseAttribute` / `VersionBehavior` | Concurrency-column update strategies (`Auto` / `AutoIncrement` / `Guid`); used by `LinqToDB.Concurrency.ConcurrencyExtensions` | `OptimisticLockPropertyAttribute.cs`, `OptimisticLockPropertyBaseAttribute.cs`, `VersionBehavior.cs` |
| `DefaultValue` (static) / `DefaultValue<T>` | Per-type default-value table substituted for `null` reads; seeded for primitives, populated lazily for enums via `GetMapValues` | `DefaultValue.cs` |
| `IGenericInfoProvider` | Extensibility point for adding generic-type conversions (e.g. `IEnumerable<T> -> ImmutableList<T>`); registered with `MappingSchema.SetGenericConvertProvider(typeof(MyProvider<>))` | `IGenericInfoProvider.cs` |
| `IToSqlConverter` | Marker interface for objects that know how to render themselves to `ISqlExpression` directly | `IToSqlConverter.cs` |
| `SqlQueryDependentParamsAttribute` | **Deprecated (v7 removal).** Subclass of `SqlQueryDependentAttribute` applied to parameters of custom SQL function methods; forces per-parameter client-side expression evaluation before SQL generation. Superseded by the default structural cache-compare path. | `SqlQueryDependentParamsAttribute.cs` |

## Files (Tier 1 / Tier 2)

### Tier 1 (read in full, sets `area: MAPPING` ownership)

- `Source/LinqToDB/Mapping/MappingSchema.cs`
- `Source/LinqToDB/Mapping/EntityDescriptor.cs`
- `Source/LinqToDB/Mapping/ColumnDescriptor.cs`

### Tier 2 (read; mapping attributes + descriptors + builders + value-conversion plumbing)

- Descriptors / interfaces: `AssociationDescriptor.cs`, `InheritanceMapping.cs`, `IColumnChangeDescriptor.cs`, `IEntityChangeDescriptor.cs`, `IGenericInfoProvider.cs`, `IValueConverter.cs`, `IToSqlConverter.cs`, `MappingAttribute.cs`, `MapValue.cs`
- Mapping attributes: `TableAttribute.cs`, `ColumnAttribute.cs`, `ColumnAliasAttribute.cs`, `NotColumnAttribute.cs`, `PrimaryKeyAttribute.cs`, `IdentityAttribute.cs`, `AssociationAttribute.cs`, `InheritanceMappingAttribute.cs`, `DataTypeAttribute.cs`, `NullableAttribute.cs`, `NotNullAttribute.cs`, `ScalarTypeAttribute.cs`, `SequenceNameAttribute.cs`, `MapValueAttribute.cs`, `DynamicColumnsStoreAttribute.cs`, `DynamicColumnAccessorAttribute.cs`, `OptimisticLockPropertyAttribute.cs`, `OptimisticLockPropertyBaseAttribute.cs`, `QueryFilterAttribute.cs`, `ResultSetIndexAttribute.cs`, `ServerSideOnlyAttribute.cs`, `IsQueryableAttribute.cs`, `SkipBaseAttribute.cs`, `SkipValuesByListAttribute.cs`, `SkipValuesOnInsertAttribute.cs`, `SkipValuesOnUpdateAttribute.cs`, `SqlQueryDependentAttribute.cs`, `SqlQueryDependentParamsAttribute.cs` (**[Obsolete] as of PR #5526 -- scheduled for v7 removal**), `ValueConverterAttribute.cs`
- Fluent builder: `FluentMappingBuilder.cs`, `EntityMappingBuilder.cs`, `PropertyMappingBuilder.cs`
- Value layer: `ValueConverter.cs`, `ValueConverterFunc.cs`, `ValueToSqlConverter.cs`, `DefaultValue.cs`, `ConversionType.cs`, `SkipModification.cs`, `VersionBehavior.cs`

### Tier 3

None matched (no generated/build artifacts under `Source/LinqToDB/Mapping/`).

## Subsystems

### 1. `MappingSchema` registry + `Schemas[]` stack

Constructed via `new MappingSchema(string? configuration, params MappingSchema[]? schemas)` (`MappingSchema.cs:119`). The constructor flattens the bases into a deduped `MappingSchemaInfo[] Schemas` array (`MappingSchema.cs:158-183`); `Schemas[0]` is the schema own writable layer, the rest are read-only ancestors. The single-base fast path uses a C# 14 list pattern (`case [var ms]:`) and array spread (`.. ms.Schemas`) (`MappingSchema.cs:141-156`). Every get method (`GetDefaultValue`, `GetCanBeNull`, `GetDataType`, `GetMapValues`, `GetAttribute<T>`, `GetConvertInfo`) walks the array front-to-back; every set method writes into `Schemas[0]` under `_syncRoot` (a `System.Threading.Lock`, net9+, `MappingSchema.cs:190`) and calls `ResetID()` so the cached `ConfigurationID` is recomputed (`MappingSchema.cs:1391-1403`). `CombineSchemas(ms1, ms2)` (`MappingSchema.cs:52-66`) memoises the (locked, locked) -> combined schema, which is how the connection-level schema fuses with the provider locked schema without exploding allocations across queries.

`ConfigurationList` and `ColumnNameComparer` both use the C# 14 `field` keyword for lazy initialisation of the backing field (`MappingSchema.cs:1411`, `MappingSchema.cs:1808`).

`MappingSchema.Default` is a singleton `LockedMappingSchema` (`Internal.Mapping`, out of this area) initialised in the nested `DefaultMappingSchema` ctor (`MappingSchema.cs:1446-1492`): it seeds the scalar-type table (`AddScalarType(typeof(string), DataType.NVarChar)`, etc.), registers `DBNull -> object?` and `DateTime -> string` converters, and primes `ValueToSqlConverter`. Provider-specific schemas (e.g. `SqlServerMappingSchema`) extend `LockedMappingSchema` and chain off `Default`.

### 2. `EntityDescriptor` construction

`EntityDescriptor.Init` (`EntityDescriptor.cs:164-297`) is the single hot path that produces an entity column/association set:

1. Reads `[Table]` from the schema -> seeds `Name = SqlObjectName(name, server, database, schema)`, `TableOptions`, `IsColumnAttributeRequired`.
2. Reads `[QueryFilter]` for a soft-delete-style ambient filter (`QueryFilterLambda` / `QueryFilterFunc`).
3. `InitializeDynamicColumnsAccessors` (`EntityDescriptor.cs:488-665`) inspects `[DynamicColumnsStore]` / `[DynamicColumnAccessor]` and synthesises three lambdas -- getter, setter, and a storage-initializer that lazily news the `Dictionary<string,object>` on first write. These are persisted on `EntityDescriptor` so the LINQ side can compile them into materializers.
4. Iterates `TypeAccessor.Members` (concat with `MappingSchema.GetDynamicColumns(ObjectType)`):
   - `[Association]` -> constructs `AssociationDescriptor` from the attribute keys / predicate / query / storage / setter.
   - `[Column]` -> for each `ColumnAttribute` with distinct `MemberName`, either constructs a `ColumnDescriptor` directly or queues a class-level redirect (the `attrs` list, used when `MemberName` resolves a nested path like "Residence.Street"). See `SetColumn` (`EntityDescriptor.cs:299-328`).
   - No column attribute, but `MappingSchema.IsScalarType(member.Type)` is true OR the member has `[Identity]` / `[PrimaryKey]` -> still creates a `ColumnDescriptor` (implicit-mapping path; suppressed by `IsColumnAttributeRequired`).
   - `[ColumnAlias]` -> registered in `_aliases`.
   - `[ExpressionMethod(IsColumn = true)]` -> tracked in `CalculatedMembers`.
5. Type-level `[Column]` attributes are applied last (`EntityDescriptor.cs:283-291`), letting class-level overrides win over member-level ones for the same path.
6. `InitInheritanceMapping` (`EntityDescriptor.cs:349-428`) collects `[InheritanceMapping]` attrs, recursively builds child `EntityDescriptor`s via `MappingSchema.GetEntityDescriptor`, picks the column flagged `IsDiscriminator = true`, and sorts most-specific first.

`EntityDescriptor` instances are cached per `(Type, schemaConfigId)` in `EntityDescriptorsCache` (`MappingSchema.cs:1835-1857`). Cache invalidation is implicit -- `ResetID` bumps `ConfigurationID`, and the next `GetEntityDescriptor` call sees a cache miss.

### 3. `ColumnDescriptor` construction + lambda compilation

Constructor (`ColumnDescriptor.cs:32-148`) resolves column metadata from three sources in priority order: explicit `ColumnAttribute` properties, the schema `GetDataType(MemberType)` / `GetUnderlyingDataType`, then per-member fallbacks (`[DataType]`, `[Nullable]`, nullability annotations, `[PrimaryKey]`, `[Sequence]`, `[ValueConverter]`, `[Skip*]`).

Lazily-compiled lambda factories (`ColumnDescriptor.cs:507-802`):

- `GetOriginalValueLambda()` -- `obj => obj.Member`, the raw extractor without conversion.
- `GetDbParamLambda()` -- full pipeline: extractor -> discriminator-default substitution -> `ApplyConversions(getterExpr, dbDataType, includingEnum: true)`. The `static ApplyConversions` (`ColumnDescriptor.cs:661-760`) is the canonical CLR value -> provider value pipeline (type-prep converter -> user `IValueConverter` -> registered `T -> DataParameter` lambda, with enum->underlying fallback). Reused by EXPR-TRANS for inline expression rewrites.
- `GetDbValueLambda()` -- same but unwraps `DataParameter.Value`.
- `GetProviderValue(object)` -- compiles `GetDbValueLambda` into a `Func<object,object>`, cached in `_getter`.

### 4. Type-conversion graph (`GetConverter` / `SetConvertExpression`)

`GetConverter(DbDataType from, DbDataType to, bool create, ConversionType conversionType)` (`MappingSchema.cs:895-1025`) walks `Schemas[]` via the inner local function `TryFindExistingConversion` (`MappingSchema.cs:1003-1024`), simplifying both endpoints via `Simplify` (drop `DbType` -> drop `Precision/Scale` -> drop `Length` -> drop `DataType`) on misses, then nullable-unwrap fallbacks, then defers to `ConvertInfo.Default.Create`.

`SetConvertExpression(fromType, toType, expr, addNullCheck)` (`MappingSchema.cs:565-589`) wraps with `AddNullCheck` when set and `to != DataParameter`, then `SetNullableConversion` derives a `T? -> DataParameter` companion on demand.

`ConversionType` splits the converter table into `Common` / `ToDatabase` / `FromDatabase`; lookups try the requested band then fall through to `Common`.

Two additional helper methods on `MappingSchema` support expression-level conversion:
- `GenerateSafeConvert(Type fromType, Type type)` (`MappingSchema.cs:780-813`) -- builds a null-safe conversion lambda for use in materializers.
- `GenerateConvertedValueExpression(object? value, Type type)` (`MappingSchema.cs:815-829`) -- produces a constant-or-converted `Expression` for a given runtime value.

### 5. `ValueToSqlConverter` -- literal inlining

`ValueToSqlConverter` holds `Action<StringBuilder, DbDataType, DataOptions, object>` per type; SQL-PROVIDER builders use it to inline constants. `SetDefaults` (`ValueToSqlConverter.cs:41-77`) registers: `bool`, `char`, all signed/unsigned integer types (`sbyte`..`ulong`), `float`, `double`, `decimal`, `DateTime`, `string`, `Guid`, the full `SqlTypes` suite (`SqlBoolean`, `SqlByte`, `SqlInt16`, `SqlInt32`, `SqlInt64`, `SqlSingle`, `SqlDouble`, `SqlDecimal`, `SqlMoney`, `SqlDateTime`, `SqlString`, `SqlChars`, `SqlGuid`), and `DateOnly` (under `SUPPORTS_DATEONLY`). Provider overrides cascade via `BaseConverters`.

Dispatch in `TryConvertImpl` (`ValueToSqlConverter.cs:188-239`) is fast-path: for primitive types (`TypeCode.Boolean`..`TypeCode.String`) cached per-type delegate fields (`_booleanConverter`, `_int32Converter`, etc.) are used without a dictionary lookup. `SetConverter(Type, ConverterType?)` accepts `null` to remove an existing converter (`ValueToSqlConverter.cs:254-289`).

`CanConvert(DbDataType, DataOptions, object?)` (`ValueToSqlConverter.cs:183-186`) provides a check-only path without writing to a `StringBuilder`.

### 6. Fluent builder

`FluentMappingBuilder` accumulates `Type -> List<MappingAttribute>` and `MemberInfo -> List<MappingAttribute>`; `Build()` packages them into a `FluentMetadataReader` pushed onto the schema reader chain via `MappingSchema.AddMetadataReader`. `EntityMappingBuilder<TEntity>` / `PropertyMappingBuilder<TEntity,TProperty>` synthesise attribute objects from member-accessor expressions. `EntityMappingBuilder.SetAttribute` (`EntityMappingBuilder.cs:638-686`) is the merge operation.

## Interactions

- **METADATA -> MAPPING.** `Source/LinqToDB/Metadata/**` provides `IMetadataReader`. `MappingSchema.AddMetadataReader` (`MappingSchema.cs:1162-1183`) prepends a reader; `_cache`/`_firstOnlyCache` memoise per-`(type, member, attrType)` filtered-by-`ConfigurationList` results.
- **MAPPING -> EXPR-TRANS.** Translation looks up `dataContext.MappingSchema.GetEntityDescriptor(type)`; `EntityDescriptor.this[memberName]` and `FindColumnDescriptor(MemberInfo)` are the lookup APIs.
- **MAPPING -> LINQ.** Materialization compiles column readers from `ColumnDescriptor.GetDbDataType` + `FromDatabase` converters, and uses `EntityDescriptor.InheritanceMapping` for discriminator dispatch.
- **MAPPING -> DATA.** `DataConnection`/`DataParameter` paths call `ColumnDescriptor.GetDbParamLambda()` (or static `ApplyConversions`) at execution time.
- **MAPPING -> SQL-PROVIDER.** SQL builders consume `MappingSchema.ValueToSqlConverter` and `GetDataType` / `ColumnDescriptor.DataType` / `DbType`.

## Inbound / outbound dependencies

### Inbound (who reads MAPPING)

- `LinqToDB.Internal.Linq.Builder.*` -- entity descriptor lookup, association resolution, column reader compilation.
- `LinqToDB.Data.*` -- parameter binding via `ColumnDescriptor.GetDbParamLambda`.
- `LinqToDB.Internal.SqlProvider.*` -- `ValueToSqlConverter`; `GetDataType` for type rendering.
- `LinqToDB.Concurrency.ConcurrencyExtensions` -- `OptimisticLockPropertyAttribute`.
- EF interop -- `IEntityChangeDescriptor` / `IColumnChangeDescriptor` + `EntityDescriptorCreatedCallback`.

### Outbound (what MAPPING reads)

- `LinqToDB.Metadata.IMetadataReader`.
- `LinqToDB.Internal.Conversion.ConvertInfo` / `Converter`.
- `LinqToDB.Internal.Mapping.MappingSchemaInfo` / `LockedMappingSchemaInfo` / `LockedMappingSchema` / `FluentMetadataReader`.
- `LinqToDB.Reflection.TypeAccessor` / `MemberAccessor`.
- `LinqToDB.SqlQuery.SqlObjectName` / `SqlDataType` / `DbDataType`.
- `LinqToDB.Common.Configuration` flags.

## Known issues / debt

- **`ColumnAliasAttribute` admits cycles** (`ColumnAliasAttribute.cs:5-12`). Alias-to-alias chains can loop into stack overflow; null `MemberName` throws lazily in `EntityDescriptor.Init`. No cycle detection in `EntityDescriptor.this[memberName]`.
- **`MappingSchema.GetMapValues` returns `null` for non-enum types** (`MappingSchema.cs:1766-1793`). `TODO: v7: make it throw for non-enum type`. Several call sites defensively `null!`-suppress.
- **`EntityDescriptor.Init` mutation interleaving.** Member-level then type-level passes; `SetColumn` remove+re-add couples the passes.
- **`ConfigurationID` recomputation cost.** `ResetID` clears `_configurationID`; the next read re-hashes every layer (O(layers)).
- **`SetDefaultValue` enum branch lazily mutates `Schemas[0]` from inside `GetDefaultValue`** (`MappingSchema.cs:243-274`); same pattern in `GetCanBeNull` (`:300-331`).
- **`SqlQueryDependentParamsAttribute` is deprecated** (`SqlQueryDependentParamsAttribute.cs:27`). Marked `[Obsolete]` in PR #5526; scheduled for removal in v7. Its `ExpressionsEqual<TContext>` / `SplitExpression` overrides are unsafe when the parameter expression captures outer-scope transparent identifiers in multi-level eager-loaded projections (issue #5154). The default structural cache-compare path now covers the intended use cases.

## See also

- [`architecture.md`](../../architecture.md) -- pipeline overview.
- [`code-design.md`](../../code-design.md) -- public-API contract.
- [METADATA area](../METADATA/INDEX.md) -- sources of `MappingAttribute` instances.
- [EXPR-TRANS area](../EXPR-TRANS/INDEX.md) -- primary consumer of `EntityDescriptor` / `ColumnDescriptor`.
- [LINQ area](../LINQ/INDEX.md) -- materialization / column-reader compilation site.

## Pointers

- New mapping attribute? Inherit `MappingAttribute`, override `GetObjectID()` (deterministic across runs), apply `[AttributeUsage(..., AllowMultiple = true)]` if multiple-per-target makes sense, pick up the `Configuration` filter pattern (`PrimaryKeyAttribute.cs:48-51`).
- Adding a built-in conversion? Extend the `DefaultMappingSchema` ctor (`MappingSchema.cs:1446-1492`) with `SetConverter<TFrom,TTo>(...)`; `SetNullableConversion` derives the `T? -> DataParameter` companion.
- Adding a per-column knob? Add a property to `ColumnAttribute`, an accessor on `ColumnDescriptor`, read it during the ctor -- the Has*() pattern distinguishes user-set from type-default.
- Wrong DataType / nullability? Walk the precedence in `ColumnDescriptor` ctor; for nullability `[Column].CanBeNull` -> `[Nullable]` -> NRT analysis -> `IsIdentity` -> schema `GetCanBeNull` (`ColumnDescriptor.cs:150-166`).
- Touching `EntityDescriptor.Init`? The two-pass member-then-type ordering is load-bearing.

<details><summary>Coverage</summary>

Tier 1: 3/3 visited (read in full): `MappingSchema.cs`, `EntityDescriptor.cs`, `ColumnDescriptor.cs`.

Tier 2: 43/43 visited. Two files read partially (repetitive registration code): `ValueToSqlConverter.cs`, `PropertyMappingBuilder.cs`. All others read in full.

Tier 3: 0/0.

Read (prior run -- delta):
- `Source/LinqToDB/Mapping/SqlQueryDependentParamsAttribute.cs` -- `[Obsolete]` annotation added at class level (`:27`) with message scheduling removal in v7 and pointing to issue #5154; XML `<remarks>` documenting the deprecation reason (unsafe transparent-identifier capture in multi-level eager-loaded projections). No logic changes -- `ExpressionsEqual<TContext>` / `SplitExpression` overrides unchanged.

Read (this run -- delta):
- `Source/LinqToDB/Mapping/MappingSchema.cs` -- `MappingSchema` now implements `IEquatable<MappingSchema>` (equality by `ConfigurationID`, `MappingSchema.cs:1941-1963`); `_syncRoot` field changed from `object` to `System.Threading.Lock` (net9+, `:190`); `ConfigurationList` and `ColumnNameComparer` use C# 14 `field` keyword for lazy init (`:1411`, `:1808`); constructor single-base fast-path uses list pattern + array spread (`:141-156`); `GetConverter` inner lookup extracted to local function `TryFindExistingConversion` (`:1003-1024`); public helpers `GenerateSafeConvert` (`:780-813`) and `GenerateConvertedValueExpression` (`:815-829`) confirmed present.
- `Source/LinqToDB/Mapping/ValueToSqlConverter.cs` -- `ValueToSqlConverter` now implements `IEquatable<ValueToSqlConverter>` (`:331-351`); `SetDefaults` covers the full primitive + SqlTypes suite including `bool`/`char`/all numerics/`SqlBoolean`..`SqlGuid`/`DateOnly` (`:41-77`), broader than prior description; `SetConverter` accepts `null` to remove a converter (`:254-289`); `CanConvert(DbDataType, DataOptions, object?)` check-only overload added (`:183-186`); `TryConvertImpl` dispatches primitives via cached per-type delegate fields for zero-dictionary-lookup fast path (`:200-224`).

</details>
