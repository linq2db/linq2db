---
area: SCAFFOLD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 14/14
coverage_tier_2: 267/267
---

# SCAFFOLD

Model scaffolding library (`linq2db.Scaffold` NuGet package, assembly `linq2db.Scaffold`). Converts a live database connection into C# source code representing the data model. Consumers: [CLI](../CLI/INDEX.md) (`ScaffoldCommand`), [LINQPAD](../LINQPAD/INDEX.md) (`DynamicSchemaGenerator`), and T4 templates (via `ScaffoldOptions.T4()` preset).

TFMs: `net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. Depends on `LinqToDB.Tools` (via `ProjectReference`) and `Humanizer.Core` (pluralization). Root namespace `LinqToDB`; assembly name `linq2db.Scaffold`.

## Pipeline

The three-phase public entry point lives in `Scaffolder` (`Source/LinqToDB.Scaffold/Scaffold/Scaffolder.cs`):

1. **`LoadDataModel(ISchemaProvider, ITypeMappingProvider) → DatabaseModel`** — instantiates `DataModelLoader` and calls `LoadSchema()`.
2. **`GenerateCodeModel(ISqlBuilder, DatabaseModel, IMetadataBuilder?, params ConvertCodeModelVisitor[]) → CodeFile[]`** — instantiates `DataModelGenerator` and calls `ConvertToCodeModel()`. Emits CodeModel AST nodes for entities, associations, functions, constructors. Each optional `ConvertCodeModelVisitor` post-processes via `Visit()`.
3. **`GenerateSourceCode(DatabaseModel, params CodeFile[]) → SourceCodeFile[]`** — normalizes identifier names, collects naming scopes / import requirements, resolves conflicts, then invokes `ILanguageProvider.GetCodeGenerator(...)` to emit source text. Calls `ScaffoldInterceptors.AfterSourceCodeGenerated(FinalDataModel)` once at the end.

## Subsystems

### `Scaffold/` — Public entry point (15 files)
`Scaffolder` orchestrator, `ScaffoldOptions` (Default/T4 factories with three sub-options: SchemaOptions / DataModelOptions / CodeGenerationOptions), `ScaffoldInterceptors` abstract base, `NoOpScaffoldInterceptors` (internal sealed singleton null-object), `DataModelLoader` (5-file partial), `FinalDataModel`, `SourceCodeFile` `(string FileName, string Code)` final output record, `NameGenerationServices` static helper isolating association-name generation logic for unit-testability.

**`DataModelLoader` partial files:**
- `DataModelLoader.DataContext.cs` — `BuildDataContext()`: creates `DataContextModel` with class name from options or `{DatabaseName}DB`, sets base type to `DataConnection`, populates constructors, adds optional XML-doc summary.
- `DataModelLoader.Entities.cs` — `BuildEntity()` / `BuildEntityColumns()` / `BuildAssociations()`: maps schema → DataModel; computes cardinality from PK/FK overlap; delegates name generation to `NameGenerationServices.GenerateAssociationName()`.
- `DataModelLoader.Functions.cs` — `BuildAggregateFunction()`/`BuildScalarFunction()`/`BuildTableFunction()`/`BuildStoredProcedure()`: each converts a schema `CallableObject` descendant into its DataModel counterpart. Multi-result stored procedures throw `NotImplementedException`.
- `DataModelLoader.Generic.cs` — `ProcessObjectName()` strips default schema/database; `GetOrAddAdditionalSchema()` lazily creates `AdditionalSchemaModel` wrapper.

### `Schema/` — DB schema discovery contracts and adapters (33 files)
`ISchemaProvider` primary contract; `ITypeMappingProvider` for type mapping; `LegacySchemaProvider` adapter over the legacy `IDataProvider.GetSchemaProvider().GetSchema()` API; `MergedAccessSchemaProvider` for multi-file Access (uses OLE DB as primary source, patches `COUNTER` column nullability + identity from ODBC); `AggregateTypeMappingsProvider` chains multiple `ITypeMappingProvider` instances (first non-null wins).

**Common DTOs (`Schema/Common/`):**
- `DatabaseType` sealed record `(string? Name, int? Length, int? Precision, int? Scale)`.
- `TypeMapping` sealed record `(IType CLRType, DataType? DataType)`.
- `Sequence` sealed record `(SqlObjectName? Name)` — sequence load not yet implemented (TODO).

**`DatabaseOptions`** base — `ScalarFunctionSchemaRequired` (default `false`); `SqlServerDatabaseOptions` overrides to `true`. `DatabaseOptions.Default` singleton for providers needing no overrides.

**Table DTOs (`Schema/Tables/`):**
- `TableLikeObject` abstract record base `(SqlObjectName Name, string? Description, IReadOnlyCollection<Column> Columns, Identity? Identity, PrimaryKey? PrimaryKey)`.
- `Table` / `View` sealed records extending `TableLikeObject`.
- `Column` `(Name, Description, DatabaseType Type, bool Nullable, bool Insertable, bool Updatable, int? Ordinal)`.
- `ForeignKey` `(Name, Source, Target, IReadOnlyList<ForeignKeyColumnMapping> Relation)`.
- `ForeignKeyColumnMapping` `(SourceColumn, TargetColumn)`.
- `Identity` `(Column, Sequence?)`.
- `PrimaryKey` `(Name?, IReadOnlyCollection<string> Columns)` with `GetColumnPositionInKey(Column)`.

**Function DTOs (`Schema/Functions/`):**
- `CallableObject` abstract record base `(CallableKind Kind, SqlObjectName Name, string? Description, IReadOnlyCollection<Parameter> Parameters)`.
- `CallableKind` enum: `ScalarFunction` / `AggregateFunction` / `TableFunction` / `StoredProcedure`.
- `AggregateFunction` `: CallableObject` with `ScalarResult`. `ScalarFunction` with `Result`. `TableFunction` with `SchemaError?` + `IReadOnlyCollection<ResultColumn>?`. `StoredProcedure` with `SchemaError?` + `IReadOnlyList<IReadOnlyList<ResultColumn>>? ResultSets` + `Result`.
- `Parameter` `(Name, Description, DatabaseType Type, bool Nullable, ParameterDirection Direction)`. `ParameterDirection`: Input/Output/InputOutput.
- `Result` abstract base; `ResultKind` enum: `Void`/`Tuple`/`Scalar` (`Dynamic` commented out — not supported).
- `ScalarResult` `(Name?, DatabaseType Type, bool Nullable)`.
- `TupleResult` `(IReadOnlyCollection<ScalarResult> Fields, bool Nullable)` — for PostgreSQL tuple-returning functions.
- `VoidResult` — `DataModelLoader` sets return type to `object?` when encountered.
- `ResultColumn` `(Name?, DatabaseType Type, bool Nullable)`.

### `DataModel/` — Logical model layer (41 files)
`DatabaseModel` root; `DataContextModel`; `EntityModel`; function model hierarchy (`FunctionModelBase` → `ScalarFunctionModelBase` → `ScalarFunctionModel`/`AggregateFunctionModel`; `TableFunctionModelBase` → `TableFunctionModel`/`StoredProcedureModel`); `TupleModel`+`TupleFieldModel`; `SchemaModelBase`; `DataModelGenerator` (9-file partial); `IDataModelGenerationContext`.

#### DataModel / Context (5 files)
`IDataModelGenerationContext` interface threaded through all generator partials. `DataModelGenerationContext` is root-context impl; `NestedSchemaGenerationContext` wraps for additional-schema contexts. `FileData` is per-output-file record. `CodeGenerationExtensions` provides static AST-builder bridge helpers.

### `CodeModel/AST/` — Language-agnostic code AST (≈80 files)

All nodes implement `ICodeElement`. Marker interfaces: `ICodeExpression`, `ICodeStatement`, `ILValue`, `ITopLevelElement`, `ITypedName`. Abstract base classes: `AttributeOwner`, `TypeBase`, `MethodBase`, `CodeTypedName`, `CodeAssignmentBase`, `CodeCallBase`, `CodeThrowBase`, `CodeElementList<T>`. Enums: `BinaryOperation`, `UnaryOperation`, `Modifiers`, `PragmaType`, `CodeParameterDirection`.

**Concrete node classes**: Declaration (CodeClass / CodeMethod / CodeConstructor / CodeTypeInitializer / CodeProperty / CodeField / CodeParameter / CodeVariable); Structure (CodeFile / CodeNamespace / CodeBlock / CodeRegion); Expressions (CodeConstant / CodeDefault / CodeBinary / CodeUnary / CodeTernary / CodeAsOperator / CodeTypeCast / CodeSuppressNull / CodeAwaitExpression / CodeCallExpression / CodeNew / CodeNewArray / CodeLambda / CodeMember / CodeReference / CodeAssignmentExpression / CodeIndex / CodeNameOf / CodeThis / CodeTypeReference / CodeTypeToken / CodeIdentifier / CodeExternalPropertyOrField); Statements (CodeAssignmentStatement / CodeAwaitStatement / CodeCallStatement / CodeReturn / CodeThrowStatement / CodeThrowExpression); Annotation (CodeAttribute / CodeImport / CodePragma / CodeComment / CodeXmlComment / CodeEmptyLine).

Notable: `CodeUnary` only supports `Not`; `CodeIdentifier` is mutable reference object with `OnChange` event; `TypeBase.ChangeHandler` mechanism wired to `IType.SetNameChangeHandler`; `SimpleTrivia` enum (NewLine/Padding) attached via `Before`/`After` lists.

#### AST / Groups (9 files)
`IMemberGroup` exposes `IsEmpty`. `MemberGroup<TMember>` generic base. Concrete: `ClassGroup`, `ConstructorGroup`, `FieldGroup`, `MethodGroup`, `PropertyGroup`, `RegionGroup`, `PragmaGroup`. `Field/Property/MethodGroup.TableLayout` bool drives column-aligned output.

### `CodeModel/Builders/` — Fluent AST construction (15 files)
`CodeBuilder` central factory. Builder hierarchy: `TypeBuilder<TBuilder,TType>` → `ClassBuilder`; `MethodBaseBuilder<TBuilder,TMethod>` → `MethodBuilder` / `ConstructorBuilder` / `LambdaMethodBuilder` / `TypeInitializerBuilder`; `PropertyBuilder`, `FieldBuilder`, `BlockBuilder`, `AttributeBuilder`, `XmlDocBuilder`, `NamespaceBuilder`, `RegionBuilder`.

### `CodeModel/CodeGeneration/` — Emission infrastructure (7 files)
`IndentedWriter` `StringBuilder`-backed writer with indent-level tracking. `NameFixOptions` + `NameFixType` enum. `TableLayoutBuilder` (4-file partial) two-phase Layout + Data column-aligned text generator.

### `CodeModel/Comparers/` (2 files)
`CodeIdentifierComparer` (StringComparer-wrapped, sequence-aware). `TypeEqualityComparer` (configurable ignoreNRT/ignoreNullability).

### `CodeModel/Languages/CSharp/` (3 files)
`CSharpLanguageProvider` singleton. `CSharpCodeGenerator` extends `CodeGenerationVisitor<>`; static `KeyWords` set (104 entries). `CSharpNameNormalizationVisitor` walks AST once and fixes all `CodeIdentifier` instances in-place.

### `CodeModel/Visitors/` (8 files)
`CodeModelVisitor` 42-case dispatch base; `NoopCodeModelVisitor` recurse-by-default; `CodeGenerationVisitor` adds IndentedWriter; `ConvertCodeModelVisitor` AST rewrite. Concrete: `ImportsCollector`, `NameScopesCollector`, `ProviderSpecificStructsEqualityFixer`.

### `CodeModel/Types/` (11 files)
`IType` hierarchy: `RegularType` / `GenericType` / `OpenGenericType` / `ArrayType` / `TypeArgument`. `ITypeParser` / `TypeParser`. `WellKnownTypes` registry. `TypeExtensions.SetNameChangeHandler` walks full type graph.

### `CodeModel/Utils/` (1 file)
`AstExtensions` — recursive `EnumerateMemberGroups<TGroup>` / `EnumerateMembers<TGroup,TElement>` helpers traversing `RegionGroup` nesting.

### `Naming/` — Identifier normalization (8 files)

`NamingServices` core normalization. `NameCasing` enum (None/Pascal/CamelCase/SnakeCase/LowerCase/UpperCase/T4CompatPluralized/T4CompatNonPluralized — last two replicate original T4 casing behavior). `Pluralization` enum (None/Singular/Plural/PluralIfLongerThanOne). `NameTransformation` enum (None/SplitByUnderscore/Association — `Association` includes `SplitByUnderscore` plus T4-compat FK-name stripping).

**`NormalizationOptions`** sealed class with optional-override pattern — each property carries `_set` bool so `MergeInto(baseOptions)` can distinguish "not set" from "set to default". Static `NormalizationOptions.None` is the identity transform.

**`NameConverterBase`** abstract base for `INameConversionProvider`. `NormalizeName(string)` extracts non-letter trailing suffix, then `GetLastWord()` walks right-to-left identifying the last word respecting upper/lower transitions. Both use `StringUtilities.EnumerateCharacters()` for Unicode-correct code-point iteration.

**`HumanizerNameConverter`** sealed singleton `INameConversionProvider`. Wraps `Humanizer.Core` `Singularize()`/`Pluralize()`. Registers `"all"` as uncountable in static ctor. `GetConverter(Pluralization)` returns identity / Singularize / Pluralize / PluralIfLongerThanOne (>1 char only).

### `Helpers/` (1 file)
`StringUtilities.EnumerateCharacters(this string)` yields `(string codePoint, UnicodeCategory category)` tuples per Unicode code point (surrogate-pair-aware).

### `Metadata/` — Mapping attribute/fluent generation (10 files)
`IMetadataBuilder`. `MetadataSource` enum: None / Attributes / FluentMapping. `AttributeBasedMetadataBuilder` singleton (eager attribute emission). `FluentMetadataBuilder` stateful (accumulates → emits `FluentMappingBuilder` chain in `Complete`). Metadata model DTOs: `EntityMetadata`, `ColumnMetadata`, `AssociationMetadata` (with legacy `Alias`/`Storage` planned obsolete in v4), `FunctionMetadata`, `TableFunctionMetadata`.

### `ModelGeneration/` — Legacy T4-compat generation layer (45 files)

Older code-generation framework predating `DataModelGenerator` + CodeModel AST. **Not called by `Scaffolder`**; consumed by T4 templates directly. Namespace `LinqToDB.Tools.ModelGeneration`. See [T4-TEMPLATES](../T4-TEMPLATES/INDEX.md).

#### Interface taxonomy (19 interfaces)
`ITree`, `IClassMember`, `ITypeBase`, `IClass`, `ITable`, `IMemberBase`, `IMemberGroup`, `IField`, `IEvent`, `IMethod`, `IProcedure<TTable>`, `IProperty`, `IColumn`, `IForeignKey`, `IEditableObjectProperty`, `INotifyingPropertyProperty`, `IPropertyValidation`, `IModelSource`, `INamespace`, `IAttribute`. Enums: `AccessModifier`, `AssociationType`.

#### Concrete generic implementations
Each is self-referentially generic. `MemberBase` abstract; `MemberGroup<TMemberGroup>` IsCompact-aware rendering with column alignment; `Attribute<T>`, `Class<T>`, `Event<T>`, `Field<T>`, `Method<T>`; `ForeignKey<T>` extends `Property<T>` with AssociationType setter that propagates mirror type to BackReference; `ModelSource<TModel,TNamespace>` model root.

**Remaining concrete types (batch 5):**
- `ModelType` — runtime type descriptor for T4 layer (distinct from CodeModel `IType`). `ToTypeName()` renders C# type name; respects `ModelGenerator.EnableNullableReferenceTypes` for NRT `?` suffix.
- `NameChangedArgs` — `record (string OldName, string? NewName)` event args fired by `TypeBase.OnNameChanged`.
- `Namespace<T>` — generic `INamespace` impl; `Render(tt)` emits namespace header + usings + child types.
- `Parameter` — T4-layer stored-procedure parameter: SchemaName/SchemaType/IsIn/IsOut/IsResult/Size/ParameterName/ParameterType/IsNullable/SystemType/DataType. `Type` synthesizes a `ModelType` from `ParameterType`+`IsNullable`. (Distinct from `Schema/Functions/Parameter.cs`.)
- `Property<T>` — concrete `IProperty` extending `MemberBase`. Lazy `GetBodyBuilders`/`SetBodyBuilders` lists. Single-statement getter normalization to `return X;` form.
- `TableContext<TTable,TProcedure>` — fluent `Column()` and `FK()` customization helpers in T4 layer; delegate to `ModelGenerator<TTable,TProcedure>` lookup methods.
- `TypeBase` — abstract base for T4-layer type declarations. `Name` setter fires `OnNameChanged` event. `BeginConditional`/`EndConditional` emit `#if`/`#endif` guards.

#### `ModelGenerator` (7 files: base + 6 partials)
Abstract partial class `ModelGenerator` + `ModelGenerator<TTable,TProcedure>`. Public action delegates replaceable for customization.
- `ModelGenerator.cs` — base; `GenerateModel()` emits header + pragma + optional `#nullable enable` + `Model.Render(this)`. Static `KeyWords` (80) and replaceable static delegates.
- `ModelGenerator.DataModel.cs` — `LoadServerMetadata` + `LoadMetadata`.
- `ModelGenerator.LinqToDB.cs` — `GenerateTypesFromMetadata` main entry.
- `ModelGenerator.NotifyPropertyChanged.cs` — `INotifyPropertyChanged`/`INotifyPropertyChanging` injection.
- `ModelGenerator.EditableObject.cs` — `IEditableObject` injection.
- `ModelGenerator.NotifyDataErrorInfo.cs` — `INotifyDataErrorInfo`. **WPF `System.Windows.Application.Current.Dispatcher` dependency baked into generated body.**
- `ModelGenerator.Validation.cs` — `System.ComponentModel.DataAnnotations`-based validation.

## Key types

| Type | File | Role |
|---|---|---|
| `Scaffolder` | `Scaffold/Scaffolder.cs:18` | Public 3-method orchestrator |
| `ScaffoldOptions` | `Scaffold/Options/ScaffoldOptions.cs:8` | Root options (Default/T4 factories) |
| `ScaffoldInterceptors` | `Scaffold/Customization/ScaffoldInterceptors.cs:19` | Abstract extensibility base |
| `NoOpScaffoldInterceptors` | `Scaffold/Customization/NoOpScaffoldInterceptors.cs` | Internal null-object default |
| `DataModelLoader` | `Scaffold/DataModel/DataModelLoader.cs:18` | Schema → DataModel conversion (5-part partial) |
| `NameGenerationServices` | `Scaffold/NameGenerationServices.cs` | Static association-name generation (unit-testable) |
| `SourceCodeFile` | `Scaffold/SourceCodeFile.cs` | Final output: `(FileName, Code)` record |
| `DatabaseModel` / `DataContextModel` / `EntityModel` / `SchemaModelBase` | `DataModel/Model/` | DataModel root + container types |
| `TableFunctionModel` / `TupleModel` / `TupleFieldModel` | `DataModel/Model/Functions/` | Function model variants |
| `ISchemaProvider` / `LegacySchemaProvider` | `Schema/` | DB schema contract + adapter |
| `MergedAccessSchemaProvider` | `Schema/MergedAccessSchemaProvider.cs` | OLE DB + ODBC Access schema merge |
| `AggregateTypeMappingsProvider` | `Schema/AggregateTypeMappingsProvider.cs` | Chained type-mapping fallback |
| `TableLikeObject` / `Table` / `View` / `Column` / `ForeignKey` / `PrimaryKey` / `Identity` | `Schema/Tables/` | Table schema DTOs |
| `CallableObject` / `ScalarFunction` / `TableFunction` / `StoredProcedure` / `AggregateFunction` | `Schema/Functions/` | Function schema DTOs |
| `Result` / `ScalarResult` / `TupleResult` / `VoidResult` / `ResultColumn` | `Schema/Functions/` | Function result descriptors |
| `DatabaseType` / `TypeMapping` / `DatabaseOptions` / `SqlServerDatabaseOptions` | `Schema/Common/`, `Schema/` | Type and DB option DTOs |
| `DataModelGenerator` | `DataModel/DataModelGenerator.cs:20` | DataModel → CodeModel AST |
| `IDataModelGenerationContext` / `DataModelGenerationContext` / `NestedSchemaGenerationContext` | `DataModel/Context/` | Generator context interfaces |
| `ICodeElement` / `CodeIdentifier` / `CodeClass` / `CodeMethod` / `CodeProperty` / `CodeFile` | `CodeModel/AST/` | AST node hierarchy |
| `MemberGroup<TMember>` and concrete groups | `CodeModel/AST/Groups/` | Typed member-group wrappers |
| `CodeBuilder` / `TableLayoutBuilder` / `IndentedWriter` | `CodeModel/Builders+CodeGeneration/` | AST factory + emission helpers |
| `CSharpLanguageProvider` / `CSharpCodeGenerator` / `CSharpNameNormalizationVisitor` | `CodeModel/Languages/CSharp/` | C# language services + emitter |
| `ConvertCodeModelVisitor` / `CodeModelVisitor` / `NoopCodeModelVisitor` / `ImportsCollector` / `NameScopesCollector` / `ProviderSpecificStructsEqualityFixer` | `CodeModel/Visitors/` | AST traversal + rewrite |
| `IType` and impls / `TypeParser` / `WellKnownTypes` | `CodeModel/Types/` | Type system |
| `ILanguageProvider` | `CodeModel/Languages/` | Language services |
| `NamingServices` | `Naming/NamingServices.cs` | Identifier normalization orchestrator |
| `HumanizerNameConverter` / `NameConverterBase` | `Naming/` | Pluralization via Humanizer.Core |
| `NormalizationOptions` | `Naming/NormalizationOptions.cs` | Per-element normalization config (merge-capable) |
| `NameCasing` / `Pluralization` / `NameTransformation` | `Naming/` | Naming enums |
| `IMetadataBuilder` / `AttributeBasedMetadataBuilder` / `FluentMetadataBuilder` | `Metadata/` | Mapping emission strategies |
| `ModelGenerator` / `ModelGenerator<TTable,TProcedure>` | `ModelGeneration/` | Legacy T4-compat generation root |
| `ModelType` / `TypeBase` / `Namespace<T>` / `Property<T>` / `Parameter` / `TableContext<TTable,TProcedure>` / `NameChangedArgs` | `ModelGeneration/` | Legacy T4-compat concrete types |

## Files (Tier 1 / Tier 2)

**Tier 1 (14 files — read in full):** Scaffolder.cs, ScaffoldInterceptors.cs, ScaffoldOptions.cs, SchemaOptions.cs, DataModelOptions.cs (sampled), DataModelLoader.cs, DatabaseModel.cs, EntityModel.cs, DataModelGenerator.cs, ISchemaProvider.cs, LegacySchemaProvider.cs, ICodeElement.cs, CodeElementType.cs, ILanguageProvider.cs.

**Tier 2 (267 / 267 — 100%):** All files read across batches 1–5. See Coverage block.

## Inbound / outbound dependencies

**Consumers (inbound):**
- [CLI](../CLI/INDEX.md) — `ScaffoldCommand` constructs `Scaffolder` → calls 3-method pipeline.
- [LINQPAD](../LINQPAD/INDEX.md) — `DynamicSchemaGenerator` constructs `Scaffolder`; `ModelProviderInterceptor` implements `ScaffoldInterceptors`; `DataModelAugmentor` implements `ConvertCodeModelVisitor`.
- T4 templates — use `ModelGeneration/` layer (legacy, separate from `Scaffolder`).

**Dependencies (outbound):**
- [METADATA / CORE](../METADATA/INDEX.md) — `LegacySchemaProvider` calls legacy `IDataProvider.GetSchemaProvider().GetSchema()`.
- [SQL-PROVIDER](../SQL-PROVIDER/INDEX.md) — `Scaffolder.GenerateCodeModel` and `DataModelGenerator` accept `ISqlBuilder` for full function-name generation.
- [METADATA attributes](../METADATA/INDEX.md) — `AttributeBasedMetadataBuilder`/`FluentMetadataBuilder` emit attributes / fluent calls.
- `LinqToDB.Tools` (ProjectReference); `Humanizer.Core` (PackageReference); `System.Windows` (WPF) for `ModelGenerator.NotifyDataErrorInfo.cs` generated body.

## Known issues / debt

- **F# and VB.NET not implemented** (`LanguageProviders.cs:9`, issue #1553). `CSharpLanguageProvider` is the only `ILanguageProvider`.
- **Logger not yet wired.** `DataModelLoader.MapType:215` and `LegacySchemaProvider.ParseColumn` fall back to `Console.Error.WriteLine`.
- **PostgreSQL legacy API bug workaround** in `LegacySchemaProvider.ParseSchema:94`.
- **MySQL schema type-conflict swallowed** in `LegacySchemaProvider.RegisterType`.
- **`ModelGeneration/` legacy layer retained.** 45 files of older code-generation framework not wired through `Scaffolder`. `NotifyDataErrorInfo.cs` partial has WPF dependency baked into generated body — non-WPF targets fail at runtime.
- **`ISchemaProvider` lacks async overloads** (`ISchemaProvider.cs:6`).
- **`CodeNameOf` argument type not constrained** (`CodeNameOf.cs:13`).
- **`CodeBinary` type inference incomplete** for `Add` (returns left operand type).
- **Inline comment generation unimplemented** in `CSharpCodeGenerator.Visit(CodeComment)`.
- **Dead fields in `CSharpCodeGenerator`** (`_knownTypes`, `_currentImports`) pragma-disabled.
- **`CSharpNameNormalizationVisitor` missing type-parameter-count overload check** (`Visit(CodeMethod):94`).
- **`AssociationMetadata.Alias`/`Storage`** retained for legacy T4 compat, planned obsolete in v4.
- **`Sequence` load not implemented** in schema API (`Schema/Common/Sequence.cs` TODO).
- **Multi-result stored procedures not supported** — `DataModelLoader.BuildStoredProcedure` throws `NotImplementedException` for `ResultSets.Count > 1`.
- **`ResultKind.Dynamic`** commented out — not yet supported by any schema provider.
- **`MergedAccessSchemaProvider.GetProcedures`** has a commented-out `|| safeSchemaOnly` condition (line 53), suggesting the merge logic for safe-schema-only mode is incomplete.

## See also

- [CLI INDEX.md](../CLI/INDEX.md) — `ScaffoldCommand` usage of this library
- [LINQPAD INDEX.md](../LINQPAD/INDEX.md) — dynamic driver usage of this library
- [METADATA INDEX.md](../METADATA/INDEX.md) — `ISchemaProvider` (core-side), `DatabaseSchema` DTOs
- [SQL-PROVIDER INDEX.md](../SQL-PROVIDER/INDEX.md) — `ISqlBuilder` consumed by `DataModelGenerator`
- [T4-TEMPLATES INDEX.md](../T4-TEMPLATES/INDEX.md) — T4 templates using `ModelGeneration/` layer

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 14 / 14 ✓
- Tier 2 (visited / total): 267 / 267 (100%) ✓

Read across batches 1-5: full `Source/LinqToDB.Scaffold/` source tree — Scaffold/* + Schema/* + DataModel/* + CodeModel/* + Naming/* + Helpers/* + Metadata/* + ModelGeneration/* (legacy T4-compat layer).

- Tier 3 (skipped, logged): 0
</details>
