---
area: T4-TEMPLATES
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 4/4
coverage_tier_2: 11/11
---

# T4-TEMPLATES

Design-time T4 scaffold templates shipped to NuGet consumers via the `linq2db.t4models` package (`NuGet/t4models/linq2db.t4models.csproj`). These files are `.ttinclude` fragments — they have no standalone entry point. A consumer creates a `.tt` file in their project that includes the relevant provider-specific root include (e.g., `LinqToDB.SqlServer.ttinclude` from `NuGet/SqlServer/`), which in turn chains into this folder's generic includes. At T4-host execution time (Visual Studio or Rider), the template runs synchronously and emits a `.generated.cs` file into the project tree.

This is the **legacy** scaffold path. The modern alternative is the `dotnet linq2db scaffold` CLI (see [SCAFFOLD area](../SCAFFOLD/INDEX.md)).

## Subsystems

### Core chain (required)

| File | Role |
|---|---|
| `LinqToDB.Tools.ttinclude` | Bottom layer. Loads `linq2db.dll`, `linq2db.Scaffold.dll`, and `Microsoft.Bcl.AsyncInterfaces.dll` into the T4 AppDomain via `<#@ assembly #>` directives; supplies `GetProviderToolsPath` and `LoadAssembly` helpers for provider-specific includes that need extra native DLLs (e.g., MySqlConnector). Handles assembly-resolve fallback for version mismatches (`AppDomain.CurrentDomain.AssemblyResolve`). |
| `T4Model.ttinclude` | Abstract model framework. Includes `LinqToDB.Tools.ttinclude`; imports `LinqToDB.Tools.ModelGeneration`; instantiates `ModelGenerator<Table,Procedure>` backed by a `ModelSource`. Defines the concrete partial classes (`Class`, `Property`, `Field`, `Event`, `Method`, `Attribute`, `Table`, `ForeignKey`, `Procedure`, `Namespace`) that the T4 host compiles into the transformation class. Exposes `GenerateModel()`, `BeforeGenerateModel`, `WriteProperty`, `WriteField`, `WriteEvent`, `WriteAttribute`, `SetPropertyValueAction`. |
| `DataModel.ttinclude` | Database-specific model surface. Includes `T4Model.ttinclude`; imports `LinqToDB.SchemaProvider`. Wraps `ModelGenerator` properties with C#-property surface for `NamespaceName`, `DataContextName`, `BaseDataContextClass`, `EnforceModelNullability`, pluralization flags, normalization hooks (`ToValidName`, `ConvertToCompilable`, `NormalizeName`), schema-load options (`GetSchemaOptions`), `LoadServerMetadata`, `LoadMetadata`, `GetTable`, `GetColumn`, `GetFK`, `GetProcedure`. Defines the `Column` partial extending `Property` and `IColumn`. |
| `LinqToDB.ttinclude` | Top-level entry for most provider includes. Includes `DataModel.ttinclude`; sets `BaseDataContextClass` default to `"LinqToDB.Data.DataConnection"`; hooks `BeforeGenerateModel` to call `GenerateTypesFromMetadata()`. Exposes generation flags (`GenerateDataOptionsConstructors`, `GenerateFindExtensions`, `GenerateSchemaAsType`, `GenerateViews`, `GenerateDbTypes`, `IsCompactColumns`, etc.) and provider-specific callbacks (`GenerateProviderSpecificTable`, `GenerateProcedureDbType`). |

### Optional add-ons

These are included by consumers on demand after including a provider root:

| File | Attach point | Role |
|---|---|---|
| `NotifyPropertyChanged.ttinclude` | `T4Model.ttinclude` | Hooks `BeforeGenerateModel`; calls `ModelGenerator.NotifyPropertyChangedImplementation<…>()`. Adds `IsNotifying` / `Dependents` to `Property` via `INotifyingPropertyProperty`. Exposes `ImplementNotifyPropertyChanging`, `SkipNotifyPropertyChangedImplementation`. |
| `ObsoleteAttributes.ttinclude` | `LinqToDB.ttinclude` | Hooks `BeforeGenerateLinqToDBModel` and `AfterGenerateLinqToDBModel` to parse `[Obsolete…]` markers in DB object descriptions and inject `[Obsolete]` attributes on generated table classes and their references in the DataContext. |
| `DataAnnotations.ttinclude` | `T4Model.ttinclude` | Hooks `BeforeGenerateModel`; walks the model tree and injects `[Display]`, `[Required]`, `[StringLength]` etc. on properties using `LinqToDB.Tools.ModelGeneration.ModelGenerator.ToStringLiteral`. |
| `MultipleFiles.ttinclude` | None (standalone, VS-only) | Provides `SaveOutput(string fileName)` and `SyncProject()` using `EnvDTE` to split generated output into multiple `.cs` files and keep the VS project item tree in sync. Not usable in Rider or MSBuild-driven T4. |
| `Humanizer.ttinclude` | None (post-include) | Replaces `ToPlural`/`ToSingular`/`ToValidName` delegates with Humanizer-backed implementations (`Pluralize`, `Singularize`, `Pascalize`); sets `NormalizeNames = true`. |
| `PluralizationService.ttinclude` | `DataModel.ttinclude` | Alternative to `Humanizer.ttinclude`; uses `System.Data.Entity.Design.PluralizationServices.PluralizationService` (EF5-era, .NET Framework only) plus a hand-coded English dictionary for edge cases. |
| `EditableObject.ttinclude` | `T4Model.ttinclude` | Hooks `BeforeGenerateModel` to call `ModelGenerator.EditableObjectImplementation<MemberGroup,Method,Property,Field>()`; extends `Property` as `IEditableObjectProperty` adding `IsEditable` (bool) and `IsDirtyText` (string, default `"{0} != {1}"`); provides `EditableProperty` subclass with `IsEditable = true`. Wires `SetPropertyValueAction` to handle the `"IsEditable"` property name dynamically. |
| `Equatable.ttinclude` | `T4Model.ttinclude`, `DataModel.ttinclude` | Hooks `BeforeGenerateModel`; calls `EquatableImpl()`, which walks all non-static `Class` nodes where `IsEquatable == true`, filters properties via `EqualityPropertiesFilter` (default: primary-key `Column` instances), and injects `IEquatable<T>` with a private static `IEqualityComparer<T>` field using `ComparerBuilder.GetEqualityComparer<T>()` from `LinqToDB.Tools.Comparers`, plus `Equals(T)`, `GetHashCode()` override, and `Equals(object)` override. Exposes `static bool DefaultEquatable = true` and a configurable `EqualityPropertiesFilter` delegate. Extends `Class` partial with `public bool IsEquatable`. |
| `NotifyDataErrorInfo.ttinclude` | `T4Model.ttinclude`, `Validation.ttinclude` | Thin shim: includes `T4Model.ttinclude` and `Validation.ttinclude` (which itself pulls in `NotifyPropertyChanged.ttinclude`); hooks `BeforeGenerateModel` to call `ModelGenerator.NotifyDataErrorInfoImplementation<MemberGroup,Method,Property,Field,Event,Attribute>()`. Generation logic lives entirely in `linq2db.Scaffold.dll`. |
| `Validation.ttinclude` | `NotifyPropertyChanged.ttinclude` | Includes `NotifyPropertyChanged.ttinclude` (not just `T4Model.ttinclude`); hooks `BeforeGenerateModel` to call `ModelGenerator.ValidationImplementation<Class,MemberGroup,Method,Field,Attribute>()`; extends `Property` as `IPropertyValidation` adding `CustomValidation` (bool), `ValidateProperty` (bool), and a write-only `Validate` setter that sets both. Depends on `NotifyPropertyChanged.ttinclude` — validation always carries notify-property-changed support. |

## Composition graph

```
[user .tt file]
    └── <#@ include #> NuGet/<Provider>/LinqToDB.<Provider>.ttinclude
            └── <#@ include #> LinqToDB.ttinclude
                    └── <#@ include #> DataModel.ttinclude
                            └── <#@ include #> T4Model.ttinclude
                                    └── <#@ include #> LinqToDB.Tools.ttinclude
                                            loads: linq2db.dll, linq2db.Scaffold.dll

[user .tt file] (optional add-ons)
    ├── <#@ include #> NotifyPropertyChanged.ttinclude  → includes T4Model.ttinclude
    ├── <#@ include #> ObsoleteAttributes.ttinclude     → includes LinqToDB.ttinclude
    ├── <#@ include #> DataAnnotations.ttinclude        → includes T4Model.ttinclude
    ├── <#@ include #> MultipleFiles.ttinclude          (standalone, VS EnvDTE)
    ├── <#@ include #> Humanizer.ttinclude              (standalone, requires Humanizer.dll)
    ├── <#@ include #> PluralizationService.ttinclude   → includes DataModel.ttinclude
    ├── <#@ include #> EditableObject.ttinclude         → includes T4Model.ttinclude
    ├── <#@ include #> Equatable.ttinclude              → includes T4Model.ttinclude + DataModel.ttinclude
    ├── <#@ include #> Validation.ttinclude             → includes NotifyPropertyChanged.ttinclude
    └── <#@ include #> NotifyDataErrorInfo.ttinclude    → includes T4Model.ttinclude + Validation.ttinclude
```

All provider-specific includes (e.g., `NuGet/SqlServer/LinqToDB.SqlServer.ttinclude`) sit outside this folder; they supply `Load<Provider>Metadata(…)` which calls through to `LinqToDB.SchemaProvider` via `ModelGenerator.LoadServerMetadata<ForeignKey,Column>(dataConnection)`.

## Key types

These types live in `LinqToDB.Tools.ModelGeneration` (in `linq2db.Scaffold.dll`), not in this folder. The T4 templates import the namespace and the partial classes in this folder extend the base generics:

| Type | Source (cross-area) | Role |
|---|---|---|
| `ModelGenerator<TTable,TProcedure>` | SCAFFOLD / `ModelGeneration/ModelGenerator.cs` | Orchestrator: loads schema, builds model tree, drives code emission. The templates expose its properties as top-level template members. |
| `ModelSource<,>` / `Namespace<>` | SCAFFOLD / `ModelGeneration/` | Root of the in-memory code model tree. |
| `ITable` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `T4Model.ttinclude`'s `Table` partial class. |
| `IColumn` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `DataModel.ttinclude`'s `Column` partial class. |
| `IForeignKey` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `T4Model.ttinclude`'s `ForeignKey` partial. |
| `IProcedure<>` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `T4Model.ttinclude`'s `Procedure` partial. |
| `GetSchemaOptions` | `LinqToDB.SchemaProvider` | Exposes table/view/procedure filter options; surfaced as `GetSchemaOptions` property in `DataModel.ttinclude`. |
| `IEditableObjectProperty` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `EditableObject.ttinclude`'s `Property` partial; requires `IsEditable` and `IsDirtyText`. |
| `IPropertyValidation` | SCAFFOLD / `ModelGeneration/` | Interface implemented by `Validation.ttinclude`'s `Property` partial; requires `CustomValidation` and `ValidateProperty`. |
| `ComparerBuilder` | `LinqToDB.Tools.Comparers` | Used by `Equatable.ttinclude` to generate `IEqualityComparer<T>` instances from property selectors. |

The `T4Model.ttinclude`'s concrete partial classes (`Table`, `Column`, `ForeignKey`, `Procedure`, `Class`, `Property`, `Field`, `Method`, `Attribute`, `Event`, `MemberGroup`) are the template-side extension points — they are `partial` so consumers can add members in their own `.tt` files.

## Files (Tier 1 / Tier 2)

**Tier 1** (read in full this run):

| File | Notes |
|---|---|
| `Source/LinqToDB.Templates/LinqToDB.ttinclude` | Top of the per-provider include chain |
| `Source/LinqToDB.Templates/DataModel.ttinclude` | DB model surface, schema loading hooks |
| `Source/LinqToDB.Templates/LinqToDB.Tools.ttinclude` | Assembly bootstrap layer |
| `Source/LinqToDB.Templates/T4Model.ttinclude` | Abstract model framework, all partial types |

**Tier 2** (sampled — all read):

| File | Read? | Notes |
|---|---|---|
| `Source/LinqToDB.Templates/NotifyPropertyChanged.ttinclude` | Yes | INotifyPropertyChanged support |
| `Source/LinqToDB.Templates/ObsoleteAttributes.ttinclude` | Yes | [Obsolete] injection from DB descriptions |
| `Source/LinqToDB.Templates/DataAnnotations.ttinclude` | Yes (partial) | [Display]/[Required] injection |
| `Source/LinqToDB.Templates/MultipleFiles.ttinclude` | Yes | VS EnvDTE multi-file split |
| `Source/LinqToDB.Templates/Humanizer.ttinclude` | Yes | Humanizer pluralization delegate swap |
| `Source/LinqToDB.Templates/PluralizationService.ttinclude` | Yes | EF5-era pluralization alternative |
| `Source/LinqToDB.Templates/README.md` | Yes | Consumer-facing usage doc |
| `Source/LinqToDB.Templates/EditableObject.ttinclude` | Yes | IEditableObject via `EditableObjectImplementation`; `EditableProperty` subclass; `SetPropertyValueAction` wiring |
| `Source/LinqToDB.Templates/Equatable.ttinclude` | Yes | IEquatable<T> via `ComparerBuilder`; default filter: PK columns; configurable via `EqualityPropertiesFilter` |
| `Source/LinqToDB.Templates/NotifyDataErrorInfo.ttinclude` | Yes | Thin shim over `NotifyDataErrorInfoImplementation`; depends on `Validation.ttinclude` |
| `Source/LinqToDB.Templates/Validation.ttinclude` | Yes | `ValidationImplementation`; `IPropertyValidation` on `Property`; depends on `NotifyPropertyChanged.ttinclude` |

## Inbound / outbound dependencies

**Inbound**: Provider-specific NuGet packages (`NuGet/SqlServer/`, `NuGet/MySql/`, etc.) ship provider-specific `.ttinclude` files that chain into this folder's core includes. `Tests/Tests.T4.Nugets/` validates the packaged templates end-to-end.

**Outbound**:
- `linq2db.Scaffold.dll` (built from SCAFFOLD area): `LinqToDB.Tools.ModelGeneration.ModelGenerator<,>`, `ModelSource`, all `I*` interfaces including `IEditableObjectProperty`, `IPropertyValidation`.
- `linq2db.dll`: `LinqToDB.Data.DataConnection`, `LinqToDB.SchemaProvider.GetSchemaOptions`, `LinqToDB.Internal.SqlProvider`, `LinqToDB.Internal.SqlQuery`.
- `LinqToDB.Tools.Comparers` (in `linq2db.Scaffold.dll`): `ComparerBuilder.GetEqualityComparer<T>()`, consumed by `Equatable.ttinclude`.
- `Humanizer.dll` (optional): consumed by `Humanizer.ttinclude`.
- `System.Data.Entity.Design` (.NET Framework only): consumed by `PluralizationService.ttinclude`.
- `EnvDTE` (VS SDK, optional): consumed by `MultipleFiles.ttinclude`.

## Packaging

`NuGet/t4models/linq2db.t4models.csproj` packs this folder's `.ttinclude` files as NuGet content (`contentFiles\any\any\LinqToDB.Templates\` and `content\LinqToDB.Templates\`). The `tools\` folder in the NuGet package carries pre-built provider DLLs (`linq2db.dll`, `linq2db.Scaffold.dll`, all provider clients) so the T4 host can resolve them without a full project build. The `$(LinqToDBT4SharedTools)` MSBuild property in the `<#@ assembly #>` directives resolves to this `tools\` path at template-expansion time.

## Known issues / debt

- `MultipleFiles.ttinclude` depends on `EnvDTE`, which is Visual Studio-only. It does not work under Rider's T4 host or MSBuild-driven T4. No Rider-compatible alternative is provided.
- `PluralizationService.ttinclude` references `System.Data.Entity.Design` — an EF5 artifact not available on .NET Core/.NET 5+. It is effectively .NET Framework only. `Humanizer.ttinclude` is the recommended cross-platform replacement.
- `Validation.ttinclude` has a hard dependency on `NotifyPropertyChanged.ttinclude`. Consumers wanting only validation without INotifyPropertyChanged cannot include `Validation.ttinclude` in isolation — they get the full notify-property-changed machinery as a side effect.
- The legacy ModelGeneration layer (`linq2db.Scaffold.dll`) is retained solely for T4 back-compat. The modern CLI scaffold path (`dotnet linq2db scaffold`) uses a separate code model. Any new capability added to the CLI scaffold is not automatically surfaced in T4 templates.
- Assembly resolution uses `AppDomain` APIs (`AssemblyResolve`, `Assembly.LoadFrom`), which are not supported in .NET single-file deployments or AOT contexts — this is by design since T4 templates run at design time, but limits future hosting options.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md) — modern CLI scaffold flow; `ModelGeneration/` subdirectory is the shared runtime that T4 templates also use.
- [architecture/overview.md](../../architecture/overview.md) — overall pipeline; T4 is the legacy design-time scaffold entry point.
- `NuGet/t4models/linq2db.t4models.csproj` — packaging definition.
- `Tests/Tests.T4.Nugets/` — integration tests for the NuGet content layout.
- [linq2db T4 Models docs](https://linq2db.github.io/articles/T4.htm) — external consumer reference (mirrored in `README.md`).

<details><summary>Coverage</summary>

Tier 1 (4/4): `LinqToDB.ttinclude`, `DataModel.ttinclude`, `LinqToDB.Tools.ttinclude`, `T4Model.ttinclude` — all read in full.

Tier 2 (11/11): `NotifyPropertyChanged.ttinclude` (read), `ObsoleteAttributes.ttinclude` (read), `DataAnnotations.ttinclude` (partial read — first 30 lines sufficient to confirm pattern), `MultipleFiles.ttinclude` (read), `Humanizer.ttinclude` (read), `PluralizationService.ttinclude` (read — first 20 lines), `README.md` (read in full).

Read (this run): `EditableObject.ttinclude` (read in full — hooks `EditableObjectImplementation`, extends `Property` as `IEditableObjectProperty`, provides `EditableProperty` subclass), `Equatable.ttinclude` (read in full — hooks `EquatableImpl`, injects `IEquatable<T>` with `ComparerBuilder`-backed comparer on PK-column filter, extends `Class` with `IsEquatable`), `NotifyDataErrorInfo.ttinclude` (read in full — thin shim over `NotifyDataErrorInfoImplementation`, depends on `Validation.ttinclude`), `Validation.ttinclude` (read in full — hooks `ValidationImplementation`, extends `Property` as `IPropertyValidation`, depends on `NotifyPropertyChanged.ttinclude`).

Tier 3: none.

</details>
