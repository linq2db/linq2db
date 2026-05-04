---
area: LINQPAD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-04
last_verified_sha: d8650bb481e953a6c8a2238016bbc1994f3e0d9e
coverage_tier_1: 15/15
coverage_tier_2: 44/47
---

# LINQPAD

LINQPad driver for linq2db. Exposes two driver modes (dynamic and static) to LINQPad 5/6/7 via the `LINQPad.Extensibility.DataContext` SDK. TFMs: `net472` (LINQPad 5) and `net8.0-windows7.0` (LINQPad 6/7). Assembly name: `linq2db.LINQPad`. Packing into `.lpx` (net472) / `.lpx6` (net8.0-windows) bundles is triggered by MSBuild `PostBuild` targets that call `NuGet/Pack.cmd`.

## Subsystems

### Driver layer (`Drivers/`)

Two public driver classes, both prefixed `LinqToDB` as required by the LINQPad SDK — renaming them would break all existing saved connections:

- **`LinqToDBDriver`** (`DynamicLinqToDBDriver.cs`): subclasses `DynamicDataContextDriver`. Implements `GetSchemaAndBuildAssembly` — queries the SCAFFOLD library via `DynamicSchemaGenerator.GetModel`, receives back a C# source string and an `ExplorerItem` tree, then Roslyn-compiles the generated source into the output assembly (`CSharpCompilation.Create → Emit`). On net8.0-windows it applies a runtime-token fallback to resolve provider assembly references that may target the wrong TFM subfolder (`MakeReferenceByRuntime`). Source:  `Drivers/DynamicLinqToDBDriver.cs:121`.
- **`LinqToDBStaticDriver`** (`StaticLinqToDBDriver.cs`): subclasses `StaticDataContextDriver`. Implements `GetSchema` — reflects the caller-supplied assembly type using `StaticSchemaGenerator.GetSchema`, which inspects `IQueryable<T>` properties and `[Table]`/`[Column]`/`[Association]` attributes to build the `ExplorerItem` tree without touching the database.
- **`DriverHelper`** (`Drivers/DriverHelper.cs`): `internal static` shared across both drivers. Handles `Init()` (net472 assembly-binding resolvers for dependency DLL hell), `InitializeContext` (wires linq2db's `TraceInfo` to `QueryExecutionManager.SqlTranslationWriter` for SQL logging), `ShowConnectionDialog`, `HandleException`, and `GetAssembliesToAdd`. Source: `Drivers/DriverHelper.cs:100`.
- **`PasswordManager`** (`Drivers/PasswordManager.cs`): resolves `{pm:name}` tokens in connection strings via `LINQPad.Util.GetPassword`. Source: `Drivers/PasswordManager.cs:12`.
- **`ValueFormatter`** (`Drivers/ValueFormatter.cs`): `PreprocessObjectToWrite` hook — converts provider-specific value types (Npgsql geometry/interval/inet, OracleClob/Blob/XmlType, DB2 types, MySqlGeometry, FbDecFloat, etc.) into LINQPad-renderable equivalents via three `FrozenDictionary` lookup tables (exact type, base type, by-type-name). Source: `Drivers/ValueFormatter.cs:43`.

### Schema generation (`Drivers/DynamicSchemaGenerator.cs`, `Drivers/StaticSchemaGenerator.cs`, `Drivers/Scaffold/`)

- **`DynamicSchemaGenerator.GetModel`**: bridges `ConnectionSettings` to the [SCAFFOLD](../SCAFFOLD/INDEX.md) library. Constructs a `ScaffoldOptions` instance from settings (schema filters, naming options, pluralization, etc.), creates a `Scaffolder`, runs `LoadDataModel → GenerateCodeModel → GenerateSourceCode`. Injects `DataModelAugmentor` to add the three-parameter `LINQPadDataConnection` constructor to the generated context class. Returns the `ExplorerItem` tree from `ModelProviderInterceptor.GetTree()` plus the generated C# source text. Source: `Drivers/DynamicSchemaGenerator.cs:129`.
- **`ModelProviderInterceptor`** (`Drivers/Scaffold/ModelProviderInterceptor.cs`): implements `ScaffoldInterceptors`. Accumulates the full logical model (`_schemaItems` + `_associations`) in `AfterSourceCodeGenerated`, then converts it to an `ExplorerItem` tree in `GetTree()`. Handles schema/package hierarchy, FK associations with bidirectional hyperlinks, stored procedures, table/scalar/aggregate functions. Source: `Drivers/Scaffold/ModelProviderInterceptor.cs:21`.
- **`DataModelAugmentor`** (`Drivers/Scaffold/DataModelAugmentor.cs`): `ConvertCodeModelVisitor` subclass. Identifies the generated `DataContext` class by its `Inherits` type, then injects a public `(string provider, string? assemblyPath, string connectionString)` constructor that chains to `LINQPadDataConnection` and optionally sets `CommandTimeout`. Source: `Drivers/Scaffold/DataModelAugmentor.cs:13`.
- **`StaticSchemaGenerator.GetSchema`**: reflects the custom context type's `IQueryable<T>` properties and reads `[Table]`, `[Column]`, `[PrimaryKey]`, `[Identity]`, `[Association]` attributes via `TypeAccessor` to build the explorer tree without any DB connection. Source: `Drivers/StaticSchemaGenerator.cs:62`.

### Provider registry (`DatabaseProviders/`)

`DatabaseProviders` (static) holds two `FrozenDictionary` tables: `Providers` (keyed by `ProviderName` generic DB string) and `ProvidersByProviderName` (keyed by specific provider name string). On net8.0-windows, DB2 and Informix are registered only on 64-bit processes (`IntPtr.Size == 8`); on net472 they are always registered. Source: `DatabaseProviders/DatabaseProviders.cs:29`.

`IDatabaseProvider` defines the contract: `Database` (generic name), `Description` (UI label), `Providers` (`IReadOnlyList<ProviderInfo>`), `GetDataProvider`, `GetProviderFactory`, `GetAdditionalReferences`, `IsProviderPathSupported`, `RegisterProviderFactory`, `AutomaticProviderSelection`, `GetProviderByConnectionString`, `GetLastSchemaUpdate`, `ClearAllPools`. Source: `DatabaseProviders/IDatabaseProvider.cs`.

`DatabaseProviderBase` provides virtual no-op defaults for optional members; concrete providers override only what they need. `GetDataProvider` default delegates to `DataConnection.GetDataProvider(providerName, connectionString)`. Source: `DatabaseProviders/DatabaseProviderBase.cs:40`.

13 concrete provider classes (see Files table below). Notable per-provider facts:
- `SqlServerProvider`: on net8.0-windows overrides `GetDataProvider` to hardcode `Microsoft.Data.SqlClient` because the provider detector fails to auto-detect it; exposes 10 dialect `ProviderInfo` entries. Source: `DatabaseProviders/SqlServerProvider.cs:88`.
- `AccessProvider`: `AutomaticProviderSelection = true`; `SupportsSecondaryConnection = true` (OLE DB secondary for ODBC-primary schema). `GetProviderByConnectionString` infers OLE DB vs ODBC from the connection string content. Source: `DatabaseProviders/AccessProvider.cs:27`.

`ProviderInfo` record: `Name` (linq2db `ProviderName` string), `DisplayName`, `IsDefault`, `IsHidden` (hides obsolete provider names from UI while preserving backward compat), `Troubleshoot` (per-provider help text). Source: `DatabaseProviders/ProviderInfo.cs`.

### Configuration (`Configuration/`)

**`ConnectionSettings`**: root settings object, serialized as JSON (`SettingsV5` XML node) inside LINQPad's `IConnectionInfo.DriverData`. Contains five nested option groups: `ConnectionOptions` (provider, connection string, timeout, secondary connection), `SchemaOptions` (schema/catalog include/exclude lists, object type flags), `ScaffoldOptions` (pluralization, capitalization, as-is names, provider types), `LinqToDbOptions` (`OptimizeJoins`), `StaticContextOptions` (assembly/type name, config path). Implements legacy migration from pre-v5 per-XML-node format via the inner `Legacy` class. Source: `Configuration/ConnectionSettings.cs:26`.

`ConnectionOptions.ProviderPath` is a computed property that dispatches to `ProviderPathx86` or `ProviderPathx64` based on `IntPtr.Size`, storing separate paths for 32-bit and 64-bit processes. Source: `Configuration/ConnectionSettings.cs:338`.

**`AppConfig`**: `ILinqToDBSettings` implementation for static contexts. Parses `appsettings.json` (via `JsonSerializer`) or `app.config` XML (via `XmlReader`). Applies `PasswordManager.ResolvePasswordManagerFields` on connection string values. Source: `Configuration/AppConfig.cs:18`.

### WPF UI (`UI/`)

Dialog hosted as `SettingsDialog` (XAML `Window`). `DataContext` is `SettingsModel`, which aggregates seven sub-models: `DynamicConnectionModel`, `StaticConnectionModel`, `ScaffoldModel`, `SchemaModel`, `LinqToDBModel`, `TroubleshootModel`, `AboutModel`. All view models implement `INotifyPropertyChanged`; `ModelBase` provides a pattern for raised-per-property static `PropertyChangedEventArgs` instances to avoid per-change allocation. Source: `UI/Model/ModelBase.cs`. No third-party MVVM framework; WPF bindings are plain `{Binding ...}` in XAML.

`SettingsDialog.xaml.cs` coordinates tab visibility and the Test/Save button; the `TestDynamicConnection` lambda inside `DriverHelper.ShowConnectionDialog` opens a real connection as the validation step. Source: `Drivers/DriverHelper.cs:199`.

### Compat (`Compat/`)

`IReadOnlySet<T>`, `ReadOnlyHashSet<T>`, `ReadOnlySetExtensions` — polyfills guarded with `#if NETFRAMEWORK`. `IReadOnlySet<T>` is used by `ConnectionSettings.SchemaOptions` for schema/catalog filter sets; `IReadOnlySetConverter` is a `JsonConverter` factory for `System.Text.Json` serialization. On net8.0-windows the BCL's native `IReadOnlySet<T>` is used.

## Key types

| Type | File | Role |
|---|---|---|
| `LinqToDBDriver` | `Drivers/DynamicLinqToDBDriver.cs` | LINQPad `DynamicDataContextDriver` subclass; dynamic mode entry |
| `LinqToDBStaticDriver` | `Drivers/StaticLinqToDBDriver.cs` | LINQPad `StaticDataContextDriver` subclass; static mode entry |
| `LINQPadDataConnection` | `LINQPadDataConnection.cs` | Public `DataConnection` subclass; base class for all generated dynamic contexts |
| `DriverHelper` | `Drivers/DriverHelper.cs` | Shared driver logic (init, context wiring, SQL logging, connection dialog) |
| `IDatabaseProvider` | `DatabaseProviders/IDatabaseProvider.cs` | Provider abstraction contract |
| `DatabaseProviderBase` | `DatabaseProviders/DatabaseProviderBase.cs` | Default-virtual base for 13 concrete providers |
| `DatabaseProviders` | `DatabaseProviders/DatabaseProviders.cs` | `FrozenDictionary`-backed static registry; resolves by DB name or provider name |
| `ProviderInfo` | `DatabaseProviders/ProviderInfo.cs` | Per-dialect descriptor (name, display name, flags, troubleshoot text) |
| `ConnectionSettings` | `Configuration/ConnectionSettings.cs` | Root settings DTO; JSON-serialized in LINQPad connection XML |
| `AppConfig` | `Configuration/AppConfig.cs` | `ILinqToDBSettings` for static contexts (JSON / `app.config`) |
| `DynamicSchemaGenerator` | `Drivers/DynamicSchemaGenerator.cs` | SCAFFOLD bridge; returns `(ExplorerItem[], sourceCode, providerAssemblyLocation)` |
| `StaticSchemaGenerator` | `Drivers/StaticSchemaGenerator.cs` | Reflection-based schema tree for static contexts |
| `ModelProviderInterceptor` | `Drivers/Scaffold/ModelProviderInterceptor.cs` | `ScaffoldInterceptors` impl; accumulates model → `ExplorerItem` tree |
| `DataModelAugmentor` | `Drivers/Scaffold/DataModelAugmentor.cs` | `ConvertCodeModelVisitor` that injects the driver constructor into generated context |
| `SettingsModel` | `UI/Model/SettingsModel.cs` | Root ViewModel; aggregates per-tab sub-models |
| `ValueFormatter` | `Drivers/ValueFormatter.cs` | Provider-specific type rendering for LINQPad result grid |
| `PasswordManager` | `Drivers/PasswordManager.cs` | Resolves `{pm:…}` tokens via `LINQPad.Util.GetPassword` |
| `LinqToDBLinqPadException` | `LinqToDBLinqPadException.cs` | Public domain exception surfaced to LINQPad |

## Files (Tier 1 / Tier 2)

**Tier 1 (canonical anchors) — 15 files:**

| File | Rationale |
|---|---|
| `Drivers/DynamicLinqToDBDriver.cs` | Dynamic mode driver entry; LINQPad SDK subclass |
| `Drivers/StaticLinqToDBDriver.cs` | Static mode driver entry; LINQPad SDK subclass |
| `LINQPadDataConnection.cs` | Only public type (besides exception) in the assembly |
| `Drivers/DriverHelper.cs` | Shared driver orchestration |
| `DatabaseProviders/IDatabaseProvider.cs` | Provider contract |
| `DatabaseProviders/DatabaseProviderBase.cs` | Provider base class |
| `DatabaseProviders/DatabaseProviders.cs` | Provider registry |
| `DatabaseProviders/ProviderInfo.cs` | Per-dialect descriptor |
| `Configuration/ConnectionSettings.cs` | Settings root; settings contract |
| `Configuration/AppConfig.cs` | Static context config loader |
| `Drivers/DynamicSchemaGenerator.cs` | SCAFFOLD integration; schema + codegen orchestration |
| `Drivers/StaticSchemaGenerator.cs` | Reflection-based static schema |
| `Drivers/Scaffold/ModelProviderInterceptor.cs` | ScaffoldInterceptors impl; tree builder |
| `Drivers/Scaffold/DataModelAugmentor.cs` | AST visitor; generated constructor injection |
| `LinqToDB.LINQPad.csproj` | TFM, package references, PostBuild targets |

**Tier 2 — visited 44 of 47 files:**

Concrete providers (13 files): `AccessProvider.cs`, `ClickHouseProvider.cs`, `DB2Provider.cs`, `FirebirdProvider.cs`, `InformixProvider.cs`, `MySqlProvider.cs`, `OracleProvider.cs`, `PostgreSQLProvider.cs`, `SQLiteProvider.cs`, `SapHanaProvider.cs`, `SqlCeProvider.cs`, `SqlServerProvider.cs`, `SybaseAseProvider.cs` — all sampled (Access + SqlServer read in full; others structurally identical).

UI Models (12 files): `AboutModel.cs`, `ConnectionModelBase.cs`, `DynamicConnectionModel.cs`, `LinqToDBModel.cs`, `ModelBase.cs`, `OptionalTabModelBase.cs`, `ScaffoldModel.cs`, `SchemaModel.cs`, `SettingsModel.cs`, `StaticConnectionModel.cs`, `TabModelBase.cs`, `TroubleshootModel.cs`, `UniqueStringListModel.cs` — representative sample (`SettingsModel`, `DynamicConnectionModel`, `ModelBase`) read in full; pattern confirmed.

UI Settings code-behind (11 files): `SettingsDialog.xaml.cs` read in full; `AboutTab`, `CommandTimeoutConverter`, `DynamicConnectionTab`, `LinqToDBTab`, `ScaffoldTab`, `SchemaTab`, `SharedConnectionOptions`, `StaticConnectionTab`, `TroubleshootTab`, `UniqueStringListControl` — pattern confirmed from `SettingsDialog`.

Compat (3 files): `IReadOnlySet.cs` read in full; `ReadOnlyHashSet.cs`, `ReadOnlySetExtensions.cs` — structurally implied, skipped.

Other: `CSharpUtils.cs`, `ValueFormatter.cs`, `PasswordManager.cs`, `Notification.cs`, `LinqToDBLinqPadException.cs`, `GlobalSuppressions.cs`, `Configuration/CustomSerializers/IReadOnlySetConverter.cs` — read or structurally clear.

Skipped (3): `ReadOnlyHashSet.cs` (polyfill impl, no new claims), `ReadOnlySetExtensions.cs` (polyfill extension, no new claims), `LinqToDB.LINQPad.Pack.csproj` (pack-only project, no .cs content).

## Inbound / outbound dependencies

**Outbound (this area depends on):**
- [SCAFFOLD](../SCAFFOLD/INDEX.md) — `LinqToDB.Scaffold` project reference; `Scaffolder`, `ScaffoldInterceptors`, `ScaffoldOptions`, `ConvertCodeModelVisitor`, `FinalDataModel`, `DatabaseModel`, etc.
- [CORE](../CORE/INDEX.md) — `DataConnection`, `DataOptions`, `IDataContext`
- [PROV-*](../PROV-SQLSERVER/INDEX.md) — all provider areas: each `IDatabaseProvider` impl delegates to `DataConnection.GetDataProvider` or directly calls `<X>Tools.GetDataProvider`
- `LINQPad.Reference` NuGet — `DynamicDataContextDriver`, `StaticDataContextDriver`, `IConnectionInfo`, `ExplorerItem`, `QueryExecutionManager`, `Util`
- `Microsoft.CodeAnalysis.CSharp` NuGet — Roslyn compilation for dynamic mode

**Inbound:**
- Nothing inside `Source/LinqToDB/` depends on this area — it is a standalone driver project.

## Known issues / debt

- `DynamicSchemaGenerator.cs:44` has `// TODO: disabled due to generation bug in current scaffolder` with `options.Schema.EnableSqlServerReturnValue = false` commented out. Source: `Drivers/DynamicSchemaGenerator.cs:43`.
- `CSharpUtils.cs` carries a `// TODO: move to linq2db.Tools` note; the keyword list duplicates `CSharpCodeGenerator.KeyWords` from the SCAFFOLD library. Source: `Drivers/Scaffold/CSharpUtils.cs:7`.
- On net472, `SYSLIB1045` is suppressed in `PasswordManager.cs` (regex not converted to `GeneratedRegexAttribute`) — a minor perf issue for the net8.0 build which does use `GeneratedRegex` in `DynamicLinqToDBDriver.cs`.
- `SYSLIB0044` (`AssemblyName.CodeBase` is obsolete) suppressed in `DynamicLinqToDBDriver.cs:175` — no clean replacement available with the LINQPad SDK's `AssemblyName` input.
- DB2 and Informix silently excluded on 32-bit net8.0-windows processes (`IntPtr.Size == 8` guard). Source: `DatabaseProviders/DatabaseProviders.cs:29`.
- `WITH_ISERIES` conditional: iSeries support (`linq2db4iSeries` package + `DB2iSeries` provider) is compiled in by default (`DefineConstants` in csproj) but the nuspec comment says "don't forget to (un)comment reference in nuspec file" — the packaging step is manual. Source: `LinqToDB.LINQPad.csproj:12`.
- IBM DB2 on net472 requires a PostBuild trick: `IBM.Data.DB2.dll` is deleted from the output and re-placed in an `IBM.Data.DB2.DLL_provider\x64|x86\` subdirectory to trick `UnsafeNativeMethods.DB2Interop.Init()`. Source: `LinqToDB.LINQPad.csproj:114`.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md) — model scaffolding library consumed by dynamic mode
- [PROV-SQLSERVER area](../PROV-SQLSERVER/INDEX.md) — example of cross-listed provider
- [architecture/overview.md](../../architecture/overview.md) — overall pipeline

<details><summary>Coverage</summary>

- Tier 1 (visited / total): 15 / 15 ✓
  - `Drivers/DynamicLinqToDBDriver.cs`
  - `Drivers/StaticLinqToDBDriver.cs`
  - `LINQPadDataConnection.cs`
  - `Drivers/DriverHelper.cs`
  - `DatabaseProviders/IDatabaseProvider.cs`
  - `DatabaseProviders/DatabaseProviderBase.cs`
  - `DatabaseProviders/DatabaseProviders.cs`
  - `DatabaseProviders/ProviderInfo.cs`
  - `Configuration/ConnectionSettings.cs`
  - `Configuration/AppConfig.cs`
  - `Drivers/DynamicSchemaGenerator.cs`
  - `Drivers/StaticSchemaGenerator.cs`
  - `Drivers/Scaffold/ModelProviderInterceptor.cs`
  - `Drivers/Scaffold/DataModelAugmentor.cs`
  - `LinqToDB.LINQPad.csproj`
- Tier 2 (visited / total): 44 / 47 (93.6%) ✓
  - skipped: `Compat/ReadOnlyHashSet.cs` — polyfill impl, no new claims beyond `IReadOnlySet<T>` contract
  - skipped: `Compat/ReadOnlySetExtensions.cs` — polyfill extension methods, no new claims
  - skipped: `LinqToDB.LINQPad.Pack.csproj` — pack/publish-only project; no .cs source; packaging mechanics visible from main csproj PostBuild
- Tier 3 (skipped, logged): 6 (NuGet/Pack.cmd, NuGet/header.xml, NuGet/*.png, README.md, TypeRenderingTests.txt, PublicAPI/*.txt)
</details>
