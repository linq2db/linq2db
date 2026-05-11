---
area: LINQPAD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-05-11
last_verified_sha: 4a478ff148cfc4aa21e7b23b91f5a8c2f3b407b7
coverage_tier_1: 15/15
coverage_tier_2: 45/48
---

# LINQPAD

LINQPad driver for linq2db. Exposes two driver modes (dynamic and static) to LINQPad 5/6/7 via the `LINQPad.Extensibility.DataContext` SDK. TFMs: `net472` (LINQPad 5) and `net8.0-windows7.0` (LINQPad 6/7). Assembly name: `linq2db.LINQPad`. Packing into `.lpx` (net472) / `.lpx6` (net8.0-windows) bundles is triggered by MSBuild `PostBuild` targets that call `NuGet/Pack.cmd`.

## Subsystems

### Driver layer (`Drivers/`)

Two public driver classes, both prefixed `LinqToDB` as required by the LINQPad SDK -- renaming them would break all existing saved connections:

- **`LinqToDBDriver`** (`DynamicLinqToDBDriver.cs`): subclasses `DynamicDataContextDriver`. Implements `GetSchemaAndBuildAssembly` -- queries the SCAFFOLD library via `DynamicSchemaGenerator.GetModel`, receives back a C# source string and an `ExplorerItem` tree, then Roslyn-compiles the generated source into the output assembly. On net8.0-windows it applies a runtime-token fallback to resolve provider assembly references.
- **`LinqToDBStaticDriver`** (`StaticLinqToDBDriver.cs`): subclasses `StaticDataContextDriver`. Implements `GetSchema` -- reflects the caller-supplied assembly type using `StaticSchemaGenerator.GetSchema`.
- **`DriverHelper`** (`Drivers/DriverHelper.cs`): `internal static` shared across both drivers. Handles `Init()`, `InitializeContext` (wires linq2db's `TraceInfo` to `QueryExecutionManager.SqlTranslationWriter`), `ShowConnectionDialog`, `HandleException`, `GetAssembliesToAdd`.
- **`PasswordManager`** (`Drivers/PasswordManager.cs`): resolves `{pm:name}` tokens via `LINQPad.Util.GetPassword`.
- **`ValueFormatter`** (`Drivers/ValueFormatter.cs`): converts provider-specific value types (Npgsql geometry/interval/inet, OracleClob/Blob/XmlType, DB2 types, MySqlGeometry, FbDecFloat) into LINQPad-renderable equivalents via three `FrozenDictionary` lookup tables.

### Schema generation (`Drivers/DynamicSchemaGenerator.cs`, `Drivers/StaticSchemaGenerator.cs`, `Drivers/Scaffold/`)

- **`DynamicSchemaGenerator.GetModel`**: bridges `ConnectionSettings` to the SCAFFOLD library. Constructs a `ScaffoldOptions` instance, creates a `Scaffolder`, runs `LoadDataModel -> GenerateCodeModel -> GenerateSourceCode`. Injects `DataModelAugmentor` to add the three-parameter `LINQPadDataConnection` constructor to the generated context class.
- **`ModelProviderInterceptor`**: implements `ScaffoldInterceptors`. Accumulates the full logical model in `AfterSourceCodeGenerated`, then converts it to an `ExplorerItem` tree in `GetTree()`.
- **`DataModelAugmentor`**: `ConvertCodeModelVisitor` subclass. Identifies the generated `DataContext` class, injects a public `(string provider, string? assemblyPath, string connectionString)` constructor.
- **`StaticSchemaGenerator.GetSchema`**: reflects the custom context type's `IQueryable<T>` properties and reads `[Table]`/`[Column]`/`[Association]` attributes.

### Provider registry (`DatabaseProviders/`)

`DatabaseProviders` (static) holds two `FrozenDictionary` tables: `Providers` (keyed by `ProviderName` generic DB string) and `ProvidersByProviderName` (keyed by specific provider name string). On net8.0-windows, DB2 and Informix are registered only on 64-bit processes; `DuckDBProvider` is registered unconditionally on net8.0-windows (outside the 64-bit guard); on net472 DuckDB is absent.

`IDatabaseProvider` defines the contract: `Database`, `Description`, `Providers`, `GetDataProvider`, `GetProviderFactory`, `GetAdditionalReferences`, `IsProviderPathSupported`, `RegisterProviderFactory`, `AutomaticProviderSelection`, `GetProviderByConnectionString`, `GetLastSchemaUpdate`, `ClearAllPools`.

`DatabaseProviderBase` provides virtual no-op defaults; concrete providers override only what they need.

14 concrete provider classes:
- `SqlServerProvider`: on net8.0-windows overrides `GetDataProvider` to hardcode `Microsoft.Data.SqlClient`.
- `AccessProvider`: `AutomaticProviderSelection = true`; `SupportsSecondaryConnection = true`.
- **`DuckDBProvider`**: net8.0-windows only (`#if !NETFRAMEWORK`). Single `ProviderInfo` entry (`ProviderName.DuckDB`, display name `"DuckDB"`, `IsDefault = true`). Overrides `GetProviderFactory` to return `DuckDBClientFactory.Instance`; `GetLastSchemaUpdate` returns `null`; `ClearAllPools` is a no-op.

`ProviderInfo` record: `Name`, `DisplayName`, `IsDefault`, `IsHidden`, `Troubleshoot`.

### Configuration (`Configuration/`)

**`ConnectionSettings`**: root settings object, serialized as JSON inside LINQPad's `IConnectionInfo.DriverData`. Contains `ConnectionOptions`, `SchemaOptions`, `ScaffoldOptions`, `LinqToDbOptions`, `StaticContextOptions`.

`ConnectionOptions.ProviderPath` is computed: dispatches to `ProviderPathx86` or `ProviderPathx64` based on `IntPtr.Size`.

**`AppConfig`**: `ILinqToDBSettings` implementation. Parses `appsettings.json` or `app.config` XML.

### WPF UI (`UI/`)

Dialog hosted as `SettingsDialog` (XAML `Window`). `DataContext` is `SettingsModel`, which aggregates seven sub-models. `ModelBase` provides a pattern for raised-per-property static `PropertyChangedEventArgs` instances.

### Compat (`Compat/`)

`IReadOnlySet<T>`, `ReadOnlyHashSet<T>`, `ReadOnlySetExtensions` -- polyfills guarded with `#if NETFRAMEWORK`. On net8.0-windows the BCL's native `IReadOnlySet<T>` is used.

## Key types

| Type | File | Role |
|---|---|---|
| `LinqToDBDriver` | `Drivers/DynamicLinqToDBDriver.cs` | Dynamic mode entry |
| `LinqToDBStaticDriver` | `Drivers/StaticLinqToDBDriver.cs` | Static mode entry |
| `LINQPadDataConnection` | `LINQPadDataConnection.cs` | Public `DataConnection` subclass for generated contexts |
| `DriverHelper` | `Drivers/DriverHelper.cs` | Shared driver logic |
| `IDatabaseProvider` | `DatabaseProviders/IDatabaseProvider.cs` | Provider abstraction contract |
| `DatabaseProviderBase` | `DatabaseProviders/DatabaseProviderBase.cs` | Default-virtual base |
| `DatabaseProviders` | `DatabaseProviders/DatabaseProviders.cs` | `FrozenDictionary`-backed static registry |
| `DuckDBProvider` | `DatabaseProviders/DuckDBProvider.cs` | net8.0-windows DuckDB provider |
| `ProviderInfo` | `DatabaseProviders/ProviderInfo.cs` | Per-dialect descriptor |
| `ConnectionSettings` | `Configuration/ConnectionSettings.cs` | Root settings DTO |
| `AppConfig` | `Configuration/AppConfig.cs` | `ILinqToDBSettings` for static contexts |
| `DynamicSchemaGenerator` | `Drivers/DynamicSchemaGenerator.cs` | SCAFFOLD bridge |
| `StaticSchemaGenerator` | `Drivers/StaticSchemaGenerator.cs` | Reflection-based schema tree |
| `ModelProviderInterceptor` | `Drivers/Scaffold/ModelProviderInterceptor.cs` | `ScaffoldInterceptors` impl |
| `DataModelAugmentor` | `Drivers/Scaffold/DataModelAugmentor.cs` | `ConvertCodeModelVisitor` |
| `SettingsModel` | `UI/Model/SettingsModel.cs` | Root ViewModel |
| `ValueFormatter` | `Drivers/ValueFormatter.cs` | Provider-specific type rendering |
| `PasswordManager` | `Drivers/PasswordManager.cs` | Resolves `{pm:...}` tokens |
| `LinqToDBLinqPadException` | `LinqToDBLinqPadException.cs` | Public domain exception |

## Files (Tier 1 / Tier 2)

**Tier 1 (canonical anchors) -- 15 files:** Drivers (Dynamic/Static/DriverHelper), LINQPadDataConnection, IDatabaseProvider, DatabaseProviderBase, DatabaseProviders, ProviderInfo, ConnectionSettings, AppConfig, DynamicSchemaGenerator, StaticSchemaGenerator, Scaffold/ModelProviderInterceptor, Scaffold/DataModelAugmentor, csproj.

**Tier 2 -- visited 45 of 48 files:**
- Concrete providers (14 files): all sampled; Access, SqlServer, DuckDB read in full.
- UI Models (12 files): sample read in full; pattern confirmed.
- UI Settings code-behind (11 files): `SettingsDialog.xaml.cs` read; pattern confirmed.
- Compat (3 files): `IReadOnlySet.cs` read; others structurally implied.
- Other: `CSharpUtils.cs`, `ValueFormatter.cs`, `PasswordManager.cs`, `Notification.cs`, `LinqToDBLinqPadException.cs`, `GlobalSuppressions.cs`, `Configuration/CustomSerializers/IReadOnlySetConverter.cs`.

## Inbound / outbound dependencies

**Outbound:**
- [SCAFFOLD](../SCAFFOLD/INDEX.md) -- `LinqToDB.Scaffold` project reference.
- [CORE](../CORE/INDEX.md) -- `DataConnection`, `DataOptions`, `IDataContext`.
- All [PROV-*](../PROV-SQLSERVER/INDEX.md) -- each `IDatabaseProvider` impl delegates to `DataConnection.GetDataProvider` or `<X>Tools.GetDataProvider`.
- `LINQPad.Reference` NuGet -- `DynamicDataContextDriver`, etc.
- `Microsoft.CodeAnalysis.CSharp` NuGet -- Roslyn compilation.
- `DuckDB.NET.Data.Full` NuGet -- used by `DuckDBProvider.GetProviderFactory` (net8.0-windows only).

**Inbound:** standalone driver project; nothing inside `Source/LinqToDB/` depends on this area.

## Known issues / debt

- `DynamicSchemaGenerator.cs:44` -- `// TODO: disabled due to generation bug in current scaffolder` (`options.Schema.EnableSqlServerReturnValue = false` commented).
- `CSharpUtils.cs` -- `// TODO: move to linq2db.Tools`; keyword list duplicates `CSharpCodeGenerator.KeyWords`.
- `SYSLIB1045` suppressed in `PasswordManager.cs` on net472.
- `SYSLIB0044` (`AssemblyName.CodeBase` obsolete) suppressed in `DynamicLinqToDBDriver.cs:175`.
- DB2 and Informix silently excluded on 32-bit net8.0-windows processes.
- `WITH_ISERIES` conditional: iSeries support compiled in by default but the nuspec packaging step is manual.
- IBM DB2 on net472 requires a PostBuild trick.
- `DuckDBProvider.GetLastSchemaUpdate` always returns `null` -- schema change detection not implemented for DuckDB.
- DuckDB is absent from `LinqToDB.LINQPad.Pack.csproj` description string (still lists only 14 named databases).

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md)
- [PROV-SQLSERVER area](../PROV-SQLSERVER/INDEX.md)
- [architecture/overview.md](../../architecture/overview.md)

<details><summary>Coverage</summary>

- Tier 1: 15/15 done
- Tier 2: 45/48 done (93.75%)
- Tier 3 (skipped, logged): 6

**Delta read (this run -- PR #5451 DuckDB additions):**
- `DatabaseProviders/DuckDBProvider.cs` -- new file; DuckDB provider impl with `DuckDBClientFactory.Instance` factory.
- `DatabaseProviders/DatabaseProviders.cs` -- `DuckDBProvider` registration added outside 64-bit guard.
- `LinqToDB.LINQPad.csproj` -- `DuckDB.NET.Data.Full` added to net8.0-windows ItemGroup.
- `LinqToDB.LINQPad.Pack.csproj` -- read for delta; confirms `DuckDB.NET.Data.Full` added; description string not updated.

</details>
