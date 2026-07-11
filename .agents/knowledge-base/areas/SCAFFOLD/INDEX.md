---
area: SCAFFOLD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-06-01
last_verified_sha: 2e67bafc9bfc8ae8ba573b93bde8671d9920c95d
coverage_tier_1: 14/14
coverage_tier_2: 267/267
---

# SCAFFOLD

Model scaffolding library (`linq2db.Scaffold` NuGet package, assembly `linq2db.Scaffold`). Converts a live database connection into C# source code representing the data model. Consumers: [CLI](../CLI/INDEX.md) (`ScaffoldCommand`), [LINQPAD](../LINQPAD/INDEX.md) (`DynamicSchemaGenerator`), and T4 templates (via `ScaffoldOptions.T4()` preset).

TFMs: `net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. Depends on `LinqToDB.Tools` (via `ProjectReference`) and `Humanizer.Core` (pluralization). Root namespace `LinqToDB`; assembly name `linq2db.Scaffold`.

## Pipeline

The three-phase public entry point lives in `Scaffolder` (`Source/LinqToDB.Scaffold/Scaffold/Scaffolder.cs`):

1. **`LoadDataModel(ISchemaProvider, ITypeMappingProvider) -> DatabaseModel`** -- instantiates `DataModelLoader` and calls `LoadSchema()`.
2. **`GenerateCodeModel(ISqlBuilder, DatabaseModel, IMetadataBuilder?, params ConvertCodeModelVisitor[]) -> CodeFile[]`** -- instantiates `DataModelGenerator` and calls `ConvertToCodeModel()`. Emits CodeModel AST nodes for entities, associations, functions, constructors. Each optional `ConvertCodeModelVisitor` post-processes via `Visit()`.
3. **`GenerateSourceCode(DatabaseModel, params CodeFile[]) -> SourceCodeFile[]`** -- normalizes identifier names, collects naming scopes / import requirements, resolves conflicts, then invokes `ILanguageProvider.GetCodeGenerator(...)` to emit source text. Calls `ScaffoldInterceptors.AfterSourceCodeGenerated(FinalDataModel)` once at the end.

## Subsystems

### `Scaffold/` -- Public entry point (15 files)
`Scaffolder` orchestrator, `ScaffoldOptions` (Default/T4 factories with three sub-options: SchemaOptions / DataModelOptions / CodeGenerationOptions), `ScaffoldInterceptors` abstract base, `NoOpScaffoldInterceptors` (internal sealed singleton null-object), `DataModelLoader` (5-file partial), `FinalDataModel`, `SourceCodeFile` `(string FileName, string Code)` final output record, `NameGenerationServices` static helper isolating association-name generation logic for unit-testability.

**`DataModelLoader` partial files:**
- `DataModelLoader.DataContext.cs` -- `BuildDataContext()`: creates `DataContextModel` with class name from options or `{DatabaseName}DB`, sets base type to `DataConnection`, populates constructors, adds optional XML-doc summary.
- `DataModelLoader.Entities.cs` -- `BuildEntity()` / `BuildEntityColumns()` / `BuildAssociations()`: maps schema -> DataModel; computes cardinality from PK/FK overlap; delegates name generation to `NameGenerationServices.GenerateAssociationName()`.
- `DataModelLoader.Functions.cs` -- `BuildAggregateFunction()`/`BuildScalarFunction()`/`BuildTableFunction()`/`BuildStoredProcedure()`: each converts a schema `CallableObject` descendant into its DataModel counterpart. Multi-result stored procedures throw `NotImplementedException`.
- `DataModelLoader.Generic.cs` -- `ProcessObjectName()` strips default schema/database; `GetOrAddAdditionalSchema()` lazily creates `AdditionalSchemaModel` wrapper.

### `Schema/` -- DB schema discovery contracts and adapters (33 files)
`ISchemaProvider` primary contract; `ITypeMappingProvider` for type mapping; `LegacySchemaProvider` adapter over the legacy `IDataProvider.GetSchemaProvider().GetSchema()` API; `MergedAccessSchemaProvider` for multi-file Access (uses OLE DB as primary source, patches `COUNTER` column nullability + identity from ODBC); `AggregateTypeMappingsProvider` chains multiple `ITypeMappingProvider` instances (first non-null wins).

**Common DTOs (`Schema/Common/`):** `DatabaseType` `(string? Name, int? Length, int? Precision, int? Scale)`; `TypeMapping` `(IType CLRType, DataType? DataType)`; `Sequence` `(SqlObjectName? Name)` -- sequence load not yet implemented (TODO).

**`DatabaseOptions`** base -- `ScalarFunctionSchemaRequired` (default `false`); `SqlServerDatabaseOptions` overrides to `true`. `DatabaseOptions.Default` singleton.

**Table DTOs (`Schema/Tables/`):** `TableLikeObject` abstract base `(SqlObjectName Name, string? Description, IReadOnlyCollection<Column> Columns, Identity? Identity, PrimaryKey? PrimaryKey)`; `Table`/`View` extend it; `Column` `(Name, Description, DatabaseType Type, bool Nullable, bool Insertable, bool Updatable, int? Ordinal)`; `ForeignKey` `(Name, Source, Target, IReadOnlyList<ForeignKeyColumnMapping> Relation)`; `ForeignKeyColumnMapping` `(SourceColumn, TargetColumn)`; `Identity` `(Column, Sequence?)`; `PrimaryKey` `(Name?, IReadOnlyCollection<string> Columns)` with `GetColumnPositionInKey(Column)`.

**Function DTOs (`Schema/Functions/`):** `CallableObject` base `(CallableKind Kind, SqlObjectName Name, string? Description, IReadOnlyCollection<Parameter> Parameters)`; `CallableKind` enum (ScalarFunction/AggregateFunction/TableFunction/StoredProcedure); `AggregateFunction` with `ScalarResult`; `ScalarFunction` with `Result`; `TableFunction` with `SchemaError?` + `IReadOnlyCollection<ResultColumn>?`; `StoredProcedure` with `SchemaError?` + `IReadOnlyList<IReadOnlyList<ResultColumn>>? ResultSets` + `Result`; `Parameter` `(Name, Description, DatabaseType Type, bool Nullable, ParameterDirection Direction)`; `Result`/`ResultKind` (Void/Tuple/Scalar; Dynamic commented out); `ScalarResult`; `TupleResult` (PostgreSQL tuple-returning); `VoidResult` (-> `object?`); `ResultColumn`.

### `DataModel/` -- Logical model layer (41 files)
`DatabaseModel` root; `DataContextModel`; `EntityModel`; function model hierarchy (`FunctionModelBase` -> `ScalarFunctionModelBase` -> `ScalarFunctionModel`/`AggregateFunctionModel`; `TableFunctionModelBase` -> `TableFunctionModel`/`StoredProcedureModel`); `TupleModel`+`TupleFieldModel`; `SchemaModelBase`; `DataModelGenerator` (9-file partial); `IDataModelGenerationContext`.

#### DataModel / Context (5 files)
`IDataModelGenerationContext` threaded through all generator partials. `DataModelGenerationContext` is root-context impl; `NestedSchemaGenerationContext` wraps for additional-schema contexts. `FileData` is a `sealed record(CodeFile File, Dictionary<string, ClassGroup> ClassesPerNamespace)` -- carries the per-output-file CodeModel AST node together with a map from namespace name to the `ClassGroup` within that file, used by generator partials to route entity/function class definitions to the correct file and namespace bucket. `CodeGenerationExtensions` provides static AST-builder bridge helpers.

#### DataModel / Entity generation (`DataModelGenerator.Entities.cs`)
`DataModelGenerator` (partial) entity-generation methods -- all private static, called from `ConvertToCodeModel`:
- **`BuildEntities(context, entities, defineEntityClass)`** -- iterates `EntityModel` list; registers each entity's `ClassBuilder` via `context.RegisterEntityBuilder`, delegates to `BuildEntity`.
- **`BuildEntity(context, entity)`** -- calls metadata builder, emits column properties via `context.DefineProperty`, registers each via `context.RegisterColumnProperty`, then calls `BuildEntityIEquatable` / `BuildEntityContextProperty` / `BuildFindExtensions`.
- **`BuildEntityIEquatable(context, entity)`** -- conditional on `entity.ImplementsIEquatable` + PK columns. Generates a `private static readonly IEqualityComparer<TEntity>` via `ComparerBuilder.GetEqualityComparer(keySelectors[])`. Emits `IEquatable<T>.Equals`, `object.Equals` override, `object.GetHashCode` override in a `DataModelConstants.ENTITY_IEQUATABLE_REGION`.
- **`BuildEntityContextProperty(context, model)`** -- skipped when `model.ContextProperty == null`. Emits an `ITable<TEntity>` property on the data context whose getter calls `DataExtensions.GetTable<TEntity>(this)`.
- **`BuildFindExtensions(context, model)`** -- skipped when no PK. Dispatches to `BuildFindExtension` for all 12 `FindTypes` flag combinations (Find/FindAsync/FindQuery x ByPk/ByRecord x OnTable/OnContext).
- **`BuildFindExtension(context, model, methodType)`** -- generates a single `public static extension` method. PK parameter order governed by `OrderFindParametersByColumnOrdinal` (ordinal) vs alphabetical. Filter lambda typed `Expression<Func<TEntity, bool>>`; comparisons ordered by `PrimaryKeyOrder`.

### `CodeModel/AST/` -- Language-agnostic code AST (~80 files)
All nodes implement `ICodeElement`. Marker interfaces: `ICodeExpression`, `ICodeStatement`, `ILValue`, `ITopLevelElement`, `ITypedName`. Abstract bases: `AttributeOwner`, `TypeBase`, `MethodBase`, `CodeTypedName`, `CodeAssignmentBase`, `CodeCallBase`, `CodeThrowBase`, `CodeElementList<T>`. Enums: `BinaryOperation`, `UnaryOperation`, `Modifiers`, `PragmaType`, `CodeParameterDirection`.

Concrete nodes: Declaration (CodeClass/CodeMethod/CodeConstructor/CodeTypeInitializer/CodeProperty/CodeField/CodeParameter/CodeVariable); Structure (CodeFile/CodeNamespace/CodeBlock/CodeRegion); Expressions (CodeConstant/CodeDefault/CodeBinary/CodeUnary/CodeTernary/CodeAsOperator/CodeTypeCast/CodeSuppressNull/CodeAwaitExpression/CodeCallExpression/CodeNew/CodeNewArray/CodeLambda/CodeMember/CodeReference/CodeAssignmentExpression/CodeIndex/CodeNameOf/CodeThis/CodeTypeReference/CodeTypeToken/CodeIdentifier/CodeExternalPropertyOrField); Statements (CodeAssignmentStatement/CodeAwaitStatement/CodeCallStatement/CodeReturn/CodeThrowStatement/CodeThrowExpression); Annotation (CodeAttribute/CodeImport/CodePragma/CodeComment/CodeXmlComment/CodeEmptyLine).

Notable: `CodeUnary` only supports `Not`; `CodeIdentifier` is mutable with `OnChange` event; `TypeBase.ChangeHandler` wired to `IType.SetNameChangeHandler`; `SimpleTrivia` enum attached via `Before`/`After`.

#### AST / Groups (9 files)
`IMemberGroup` (`IsEmpty`); `MemberGroup<TMember>` base; concrete `ClassGroup`, `ConstructorGroup`, `FieldGroup`, `MethodGroup`, `PropertyGroup`, `RegionGroup`, `PragmaGroup`. `Field/Property/MethodGroup.TableLayout` bool drives column-aligned output.

### `CodeModel/Builders/` -- Fluent AST construction (15 files)
`CodeBuilder` central factory. `TypeBuilder<TBuilder,TType>` -> `ClassBuilder`; `MethodBaseBuilder<...>` -> `MethodBuilder`/`ConstructorBuilder`/`LambdaMethodBuilder`/`TypeInitializerBuilder`; `PropertyBuilder`, `FieldBuilder`, `BlockBuilder`, `AttributeBuilder`, `XmlDocBuilder`, `NamespaceBuilder`, `RegionBuilder`.

### `CodeModel/CodeGeneration/` -- Emission infrastructure (7 files)
`IndentedWriter` (StringBuilder-backed, indent-tracked); `NameFixOptions` + `NameFixType`; `TableLayoutBuilder` (4-file partial) two-phase Layout+Data column-aligned generator.

### `CodeModel/Comparers/` (2 files)
`CodeIdentifierComparer`; `TypeEqualityComparer` (configurable ignoreNRT/ignoreNullability).

### `CodeModel/Languages/CSharp/` (3 files)
`CSharpLanguageProvider` singleton; `CSharpCodeGenerator` extends `CodeGenerationVisitor<>`, static `KeyWords` set (104); `CSharpNameNormalizationVisitor` fixes `CodeIdentifier` instances in-place.

### `CodeModel/Languages/FSharp/` (3 files, #1553)
`FSharpLanguageProvider` singleton (`NRTSupported=false`, `FileExtension="fs"`, F# type aliases, case-sensitive comparers); `FSharpCodeGenerator` extends `CodeGenerationVisitor<>` and emits idiomatic F# — `namespace rec` single-file output, records with attribute-decorated fields, `'T option` for nullable scalar columns, module-per-schema for additional schemas, computed schema-accessor properties (no `InitSchemas` — avoids `member val` recursive-init), `task {}` async bodies, `byref<>` out/ref params, `Func<>`-wrapped mapper lambdas + explicit `QueryProc<T>` type args (F# overload-resolution needs both); `FSharpNameNormalizationVisitor`. `DataModelGenerator` gates C#-shaped features off for F# via `ReferenceEquals(LanguageProvider, LanguageProviders.FSharp)`. The `Modifiers.Record`/`Modifiers.Module` AST bits are F#-only markers (C# ignores them).

### `CodeModel/Visitors/` (8 files)
`CodeModelVisitor` 42-case dispatch; `NoopCodeModelVisitor`; `CodeGenerationVisitor`; `ConvertCodeModelVisitor`. Concrete: `ImportsCollector`, `NameScopesCollector`, `ProviderSpecificStructsEqualityFixer`.

### `CodeModel/Types/` (11 files)
`IType` hierarchy: `RegularType`/`GenericType`/`OpenGenericType`/`ArrayType`/`TypeArgument`. `ITypeParser`/`TypeParser`. `WellKnownTypes` registry. `TypeExtensions.SetNameChangeHandler` walks the type graph.

### `CodeModel/Utils/` (1 file)
`AstExtensions` -- recursive `EnumerateMemberGroups<TGroup>` / `EnumerateMembers<...>`.

### `Naming/` -- Identifier normalization (8 files)
`NamingServices` core. `NameCasing` enum (None/Pascal/CamelCase/SnakeCase/LowerCase/UpperCase/T4CompatPluralized/T4CompatNonPluralized). `Pluralization` enum. `NameTransformation` enum (None/SplitByUnderscore/Association). `NormalizationOptions` (optional-override `_set` pattern, `MergeInto`, `.None` identity). `NameConverterBase` (`NormalizeName`/`GetLastWord` via `StringUtilities.EnumerateCharacters`). `HumanizerNameConverter` (Humanizer.Core Singularize/Pluralize; `"all"` uncountable).

### `Helpers/` (1 file)
`StringUtilities.EnumerateCharacters` yields `(string codePoint, UnicodeCategory)` (surrogate-aware).

### `Metadata/` -- Mapping attribute/fluent generation (10 files)
`IMetadataBuilder`; `MetadataSource` enum (None/Attributes/FluentMapping); `AttributeBasedMetadataBuilder` (eager); `FluentMetadataBuilder` (stateful, emits `FluentMappingBuilder` chain in `Complete`). DTOs: `EntityMetadata`, `ColumnMetadata`, `AssociationMetadata` (legacy `Alias`/`Storage` planned obsolete in v4), `FunctionMetadata`, `TableFunctionMetadata`.

### `ModelGeneration/` -- Legacy T4-compat generation layer (45 files)
Older framework predating `DataModelGenerator` + CodeModel AST. **Not called by `Scaffolder`**; consumed by T4 templates directly. Namespace `LinqToDB.Tools.ModelGeneration`. See [T4-TEMPLATES](../T4-TEMPLATES/INDEX.md).

19 interfaces (`ITree`, `IClassMember`, `ITypeBase`, `IClass`, `ITable`, `IMemberBase`, `IMemberGroup`, `IField`, `IEvent`, `IMethod`, `IProcedure<TTable>`, `IProperty`, `IColumn`, `IForeignKey`, `IEditableObjectProperty`, `INotifyingPropertyProperty`, `IPropertyValidation`, `IModelSource`, `INamespace`, `IAttribute`); enums `AccessModifier`, `AssociationType`.

Concrete generics: `MemberBase`, `MemberGroup<T>`, `Attribute<T>`, `Class<T>`, `Event<T>`, `Field<T>`, `Method<T>`, `ForeignKey<T>` (extends `Property<T>`), `ModelSource<TModel,TNamespace>`; plus `ModelType`, `NameChangedArgs` `(string OldName, string? NewName)`, `Namespace<T>`, `Parameter` (T4-layer SP parameter; distinct from `Schema/Functions/Parameter.cs`), `Property<T>`, `TableContext<TTable,TProcedure>`, `TypeBase`.

`ModelGenerator` (7 files: base + 6 partials), abstract partial `ModelGenerator` + `ModelGenerator<TTable,TProcedure>`:
- `ModelGenerator.cs` -- base; `GenerateModel()`; static `KeyWords` (80) + replaceable delegates.
- `ModelGenerator.DataModel.cs` -- `LoadServerMetadata` + `LoadMetadata`.
- `ModelGenerator.LinqToDB.cs` -- `GenerateTypesFromMetadata` main entry. Public property defaults: `GenerateDataOptionsConstructors`=true, `GenerateFindExtensions`=true, `IsCompactColumns`=true, `IsCompactColumnAliases`=true, `GenerateViews`=true, `GenerateProceduresOnTypedContext`=true, `GenerateNameOf`=true, `GenerateTableRegion`=true, `GenerateSchemaAsType`=false, `SchemaNameSuffix`="Schema", `SchemaDataContextTypeName`="DataContext", `PrefixTableMappingWithSchema`=true, `PrefixTableMappingForDefaultSchema`=false. `BuildColumnComparison` replaceable `Func<...>`; `GetConstructors` replaceable factory.
- `ModelGenerator.NotifyPropertyChanged.cs` -- `INotifyPropertyChanged`/`INotifyPropertyChanging`.
- `ModelGenerator.EditableObject.cs` -- `IEditableObject`.
- `ModelGenerator.NotifyDataErrorInfo.cs` -- `INotifyDataErrorInfo`. **WPF `Application.Current.Dispatcher` baked into generated body.**
- `ModelGenerator.Validation.cs` -- DataAnnotations-based validation.

## Key types

| Type | File | Role |
|---|---|---|
| `Scaffolder` | `Scaffold/Scaffolder.cs:18` | Public 3-method orchestrator |
| `ScaffoldOptions` | `Scaffold/Options/ScaffoldOptions.cs:8` | Root options (Default/T4 factories) |
| `ScaffoldInterceptors` | `Scaffold/Customization/ScaffoldInterceptors.cs:19` | Abstract extensibility base |
| `NoOpScaffoldInterceptors` | `Scaffold/Customization/NoOpScaffoldInterceptors.cs` | Internal null-object default |
| `DataModelLoader` | `Scaffold/DataModel/DataModelLoader.cs:18` | Schema -> DataModel (5-part partial) |
| `NameGenerationServices` | `Scaffold/NameGenerationServices.cs` | Static association-name generation |
| `SourceCodeFile` | `Scaffold/SourceCodeFile.cs` | Final output `(FileName, Code)` |
| `DatabaseModel`/`DataContextModel`/`EntityModel`/`SchemaModelBase` | `DataModel/Model/` | DataModel root + containers |
| `TableFunctionModel`/`TupleModel`/`TupleFieldModel` | `DataModel/Model/Functions/` | Function model variants |
| `ISchemaProvider`/`LegacySchemaProvider` | `Schema/` | DB schema contract + adapter |
| `MergedAccessSchemaProvider` | `Schema/MergedAccessSchemaProvider.cs` | OLE DB + ODBC Access merge |
| `AggregateTypeMappingsProvider` | `Schema/AggregateTypeMappingsProvider.cs` | Chained type-mapping fallback |
| `TableLikeObject`/`Table`/`View`/`Column`/`ForeignKey`/`PrimaryKey`/`Identity` | `Schema/Tables/` | Table schema DTOs |
| `CallableObject`/`ScalarFunction`/`TableFunction`/`StoredProcedure`/`AggregateFunction` | `Schema/Functions/` | Function schema DTOs |
| `Result`/`ScalarResult`/`TupleResult`/`VoidResult`/`ResultColumn` | `Schema/Functions/` | Function result descriptors |
| `DatabaseType`/`TypeMapping`/`DatabaseOptions`/`SqlServerDatabaseOptions` | `Schema/Common/`, `Schema/` | Type and DB option DTOs |
| `DataModelGenerator` | `DataModel/DataModelGenerator.cs:20` | DataModel -> CodeModel AST |
| `FileData` | `DataModel/Context/FileData.cs:12` | Per-output-file context: `sealed record(CodeFile File, Dictionary<string, ClassGroup> ClassesPerNamespace)` |
| `IDataModelGenerationContext`/`DataModelGenerationContext`/`NestedSchemaGenerationContext` | `DataModel/Context/` | Generator context interfaces |
| `ICodeElement`/`CodeIdentifier`/`CodeClass`/`CodeMethod`/`CodeProperty`/`CodeFile` | `CodeModel/AST/` | AST node hierarchy |
| `MemberGroup<TMember>` and concrete groups | `CodeModel/AST/Groups/` | Typed member-group wrappers |
| `CodeBuilder`/`TableLayoutBuilder`/`IndentedWriter` | `CodeModel/Builders+CodeGeneration/` | AST factory + emission |
| `CSharpLanguageProvider`/`CSharpCodeGenerator`/`CSharpNameNormalizationVisitor` | `CodeModel/Languages/CSharp/` | C# language services + emitter |
| `ConvertCodeModelVisitor`/`CodeModelVisitor`/`NoopCodeModelVisitor`/`ImportsCollector`/`NameScopesCollector`/`ProviderSpecificStructsEqualityFixer` | `CodeModel/Visitors/` | AST traversal + rewrite |
| `IType` and impls / `TypeParser` / `WellKnownTypes` | `CodeModel/Types/` | Type system |
| `ILanguageProvider` | `CodeModel/Languages/` | Language services |
| `NamingServices` | `Naming/NamingServices.cs` | Identifier normalization orchestrator |
| `HumanizerNameConverter`/`NameConverterBase` | `Naming/` | Pluralization via Humanizer.Core |
| `NormalizationOptions` | `Naming/NormalizationOptions.cs` | Per-element normalization config |
| `NameCasing`/`Pluralization`/`NameTransformation` | `Naming/` | Naming enums |
| `IMetadataBuilder`/`AttributeBasedMetadataBuilder`/`FluentMetadataBuilder` | `Metadata/` | Mapping emission strategies |
| `ModelGenerator`/`ModelGenerator<TTable,TProcedure>` | `ModelGeneration/` | Legacy T4-compat generation root |
| `ModelType`/`TypeBase`/`Namespace<T>`/`Property<T>`/`Parameter`/`TableContext<...>`/`NameChangedArgs` | `ModelGeneration/` | Legacy T4-compat concrete types |

## Files (Tier 1 / Tier 2)

**Tier 1 (14 files -- read in full):** Scaffolder.cs, ScaffoldInterceptors.cs, ScaffoldOptions.cs, SchemaOptions.cs, DataModelOptions.cs (sampled), DataModelLoader.cs, DatabaseModel.cs, EntityModel.cs, DataModelGenerator.cs, ISchemaProvider.cs, LegacySchemaProvider.cs, ICodeElement.cs, CodeElementType.cs, ILanguageProvider.cs.

**Tier 2 (267 / 267 -- 100%):** All files read across batches 1-5. See Coverage block.

## Inbound / outbound dependencies

**Consumers (inbound):**
- [CLI](../CLI/INDEX.md) -- `ScaffoldCommand` constructs `Scaffolder` -> 3-method pipeline.
- [LINQPAD](../LINQPAD/INDEX.md) -- `DynamicSchemaGenerator` constructs `Scaffolder`; `ModelProviderInterceptor` implements `ScaffoldInterceptors`; `DataModelAugmentor` implements `ConvertCodeModelVisitor`.
- T4 templates -- use `ModelGeneration/` layer (legacy, separate from `Scaffolder`).

**Dependencies (outbound):**
- [METADATA / CORE](../METADATA/INDEX.md) -- `LegacySchemaProvider` calls legacy `IDataProvider.GetSchemaProvider().GetSchema()`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) -- `Scaffolder.GenerateCodeModel` and `DataModelGenerator` accept `ISqlBuilder` for full function-name generation.
- [METADATA attributes](../METADATA/INDEX.md) -- `AttributeBasedMetadataBuilder`/`FluentMetadataBuilder` emit attributes / fluent calls.
- `LinqToDB.Tools` (ProjectReference); `Humanizer.Core` (PackageReference); `System.Windows` (WPF) for `ModelGenerator.NotifyDataErrorInfo.cs` generated body.

## Known issues / debt

- **F# implemented (#1553); VB.NET not implemented** (`LanguageProviders.cs`). `CSharpLanguageProvider` and `FSharpLanguageProvider` exist; VB.NET remains the only unimplemented `ILanguageProvider`. F# is CLI/AST-path only (no T4). Some C#-shaped features are gated off for F# pending incremental parity: `IEquatable` comparer (records are value-equal), association *extension* methods, partial types, `InitDataContext`/`StaticInitDataContext` partial hooks.
- **Logger not yet wired.** `DataModelLoader.MapType:215` and `LegacySchemaProvider.ParseColumn` fall back to `Console.Error.WriteLine`.
- **PostgreSQL legacy API bug workaround** in `LegacySchemaProvider.ParseSchema:94`.
- **MySQL schema type-conflict swallowed** in `LegacySchemaProvider.RegisterType`.
- **`ModelGeneration/` legacy layer retained.** 45 files not wired through `Scaffolder`. `NotifyDataErrorInfo.cs` has WPF dependency baked into generated body.
- **`ISchemaProvider` lacks async overloads** (`ISchemaProvider.cs:6`).
- **`CodeNameOf` argument type not constrained** (`CodeNameOf.cs:13`).
- **`CodeBinary` type inference incomplete** for `Add` (returns left operand type).
- **Inline comment generation unimplemented** in `CSharpCodeGenerator.Visit(CodeComment)`.
- **Dead fields in `CSharpCodeGenerator`** (`_knownTypes`, `_currentImports`) pragma-disabled.
- **`CSharpNameNormalizationVisitor` missing type-parameter-count overload check** (`Visit(CodeMethod):94`).
- **`AssociationMetadata.Alias`/`Storage`** retained for legacy T4 compat, planned obsolete in v4.
- **`Sequence` load not implemented** in schema API.
- **Multi-result stored procedures not supported** -- `DataModelLoader.BuildStoredProcedure` throws `NotImplementedException` for `ResultSets.Count > 1`.
- **`ResultKind.Dynamic`** commented out -- not yet supported.
- **`MergedAccessSchemaProvider.GetProcedures`** has commented-out `|| safeSchemaOnly` (line 53).
- **`BuildFindExtension` duplicate sort block** (`DataModelGenerator.Entities.cs:289`, DI-0783): the `OrderFindParametersByColumnOrdinal` ordinal-sort is applied twice with no intervening by-name path executed; the second `OrderBy` is a no-op residue from an earlier two-sort design.
- **`NormalizeStringName` kept without `[Obsolete]`** (`ModelGenerator.LinqToDB.cs:179`, DI-0784): public method explicitly annotated `// unused: left for backward API compatibility`.

## See also

- [CLI INDEX.md](../CLI/INDEX.md) -- `ScaffoldCommand` usage of this library
- [LINQPAD INDEX.md](../LINQPAD/INDEX.md) -- dynamic driver usage
- [METADATA INDEX.md](../METADATA/INDEX.md) -- `ISchemaProvider` (core-side), `DatabaseSchema` DTOs
- [SQL-PROVIDER INDEX.md](../SQL-PROVIDER/INDEX.md) -- `ISqlBuilder` consumed by `DataModelGenerator`
- [T4-TEMPLATES INDEX.md](../T4-TEMPLATES/INDEX.md) -- T4 templates using `ModelGeneration/` layer

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 14 / 14
- Tier 2 (visited / total): 267 / 267 (100%)

Read across batches 1-5: full `Source/LinqToDB.Scaffold/` source tree -- Scaffold/* + Schema/* + DataModel/* + CodeModel/* + Naming/* + Helpers/* + Metadata/* + ModelGeneration/* (legacy T4-compat layer).

- Tier 3 (skipped, logged): 0

Read (this run -- delta):
- `DataModel/Context/FileData.cs` -- confirmed `sealed record(CodeFile File, Dictionary<string, ClassGroup> ClassesPerNamespace)`; added field-level detail to `## DataModel / Context` and `## Key types`.
- `DataModel/DataModelGenerator.Entities.cs` -- entity-generation partial; documented `BuildEntities`/`BuildEntity`/`BuildEntityIEquatable`/`BuildEntityContextProperty`/`BuildFindExtensions`/`BuildFindExtension` in new `#### DataModel / Entity generation` subsection; flagged duplicate-sort debt (DI-0783).
- `ModelGeneration/ModelGenerator.LinqToDB.cs` -- `GenerateTypesFromMetadata` main entry; added public property default values; flagged `NormalizeStringName` dead-code (DI-0784).
- `PublicAPI/PublicAPI.Shipped.txt` -- release-promotion churn (>256 KB); not read in full. No structural changes to INDEX.md body warranted.
</details>
