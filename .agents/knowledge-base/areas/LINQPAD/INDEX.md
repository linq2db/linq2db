---
area: LINQPAD
kind: area-index
sources: [code]
confidence: high
last_verified: 2026-07-05
last_verified_sha: 36ee4f82f06eaf242b052ade8c87121d251a6165
coverage_tier_1: 15/15
coverage_tier_2: 46/49
---

# LINQPAD

LINQPad driver for linq2db. Exposes two driver modes (dynamic and static) to LINQPad 5/6/7 via the `LINQPad.Extensibility.DataContext` SDK. TFMs: `net472` (LINQPad 5) and `net8.0-windows7.0` (LINQPad 6/7). Assembly name: `linq2db.LINQPad`. Packing into `.lpx` (net472) / `.lpx6` (net8.0-windows) bundles is triggered by MSBuild `PostBuild` targets that call `NuGet/Pack.cmd`. NuGet packaging is handled by the dedicated sibling `LinqToDB.LINQPad.Pack.csproj` (see Subsystems -- Build / Packaging below).

## Subsystems

### Driver layer (`Drivers/`)

Two public driver classes, both prefixed `LinqToDB` as required by the LINQPad SDK -- renaming them would break all existing saved connections:

- **`LinqToDBDriver`** (`DynamicLinqToDBDriver.cs`): subclasses `DynamicDataContextDriver`. Implements `GetSchemaAndBuildAssembly` -- queries the SCAFFOLD library via `DynamicSchemaGenerator.GetModel`, receives back a C# source string and an `ExplorerItem` tree, then Roslyn-compiles the generated source into the output assembly. On net8.0-windows it applies a runtime-token fallback to resolve provider assembly references.
- **`LinqToDBStaticDriver`** (`StaticLinqToDBDriver.cs`): subclasses `StaticDataContextDriver`. Implements `GetSchema` -- reflects the caller-supplied assembly type using `StaticSchemaGenerator.GetSchema`.
- **`DriverHelper`** (`Drivers/DriverHelper.cs`): `internal static` shared across both drivers. Handles `Init()`, `InitializeContext` (wires linq2db's `TraceInfo` to `QueryExecutionManager.SqlTranslationWriter`), `ShowConnectionDialog`, `HandleException`, `GetAssembliesToAdd`.
- **`PasswordManager`** (`Drivers/PasswordManager.cs`): resolves `{pm:name}` tokens via `LINQPad.Util.GetPassword`.
- **`ValueFormatter`** (`Drivers/ValueFormatter.cs`): converts provider-specific value types (Npgsql geometry/interval/inet, OracleClob/Blob/XmlType, DB2 types, MySqlGeometry, FbDecFloat) into LINQPad-renderable equivalents via three `FrozenDictionary` lookup tables.

### Schema generation (`Drivers/DynamicSchemaGenerator.cs`, `Drivers/StaticSchemaGenerator.cs`, `Drivers/Scaffold/`)

- **`DynamicSchemaGenerator.GetModel`**: bridges `ConnectionSettings` to the SCAFFOLD library. Constructs a `ScaffoldOptions`, creates a `Scaffolder`, runs `LoadDataModel -> GenerateCodeModel -> GenerateSourceCode`. Injects `DataModelAugmentor` to add the three-parameter `LINQPadDataConnection` constructor to the generated context class.
- **`ModelProviderInterceptor`**: implements `ScaffoldInterceptors`. Accumulates the full logical model in `AfterSourceCodeGenerated`, then converts it to an `ExplorerItem` tree in `GetTree()`.
- **`DataModelAugmentor`**: `ConvertCodeModelVisitor` subclass. Identifies the generated `DataContext` class, injects a public `(string provider, string? assemblyPath, string connectionString)` constructor.
- **`StaticSchemaGenerator.GetSchema`**: reflects the custom context type's `IQueryable<T>` properties and reads `[Table]`/`[Column]`/`[Association]` attributes.

### Provider registry (`DatabaseProviders/`)

`DatabaseProviders` (static) holds two `FrozenDictionary` tables: `Providers` (keyed by `ProviderName` generic DB string) and `ProvidersByProviderName` (keyed by specific provider name string). On net8.0-windows, DB2 and Informix are registered only on 64-bit processes; `DuckDBProvider` and `YdbProvider` are registered unconditionally on net8.0-windows (outside the 64-bit guard, `DatabaseProviders.cs:36-37`); on net472 both DuckDB and YDB are absent (`YdbProvider.cs` is wrapped in `#if !NETFRAMEWORK`, matching `DuckDBProvider`).

`IDatabaseProvider` defines the contract: `Database`, `Description`, `Providers`, `GetDataProvider`, `GetProviderFactory`, `GetAdditionalReferences`, `IsProviderPathSupported`, `RegisterProviderFactory`, `AutomaticProviderSelection`, `GetProviderByConnectionString`, `GetLastSchemaUpdate`, `ClearAllPools`.

`DatabaseProviderBase` provides virtual no-op defaults; concrete providers override only what they need.

15 concrete provider classes:
- `SqlServerProvider`: on net8.0-windows overrides `GetDataProvider` to hardcode `Microsoft.Data.SqlClient`.
- `AccessProvider`: `AutomaticProviderSelection = true`; `SupportsSecondaryConnection = true`.
- **`DuckDBProvider`**: net8.0-windows only (`#if !NETFRAMEWORK`). Single `ProviderInfo` entry (`ProviderName.DuckDB`, display name `"DuckDB"`, `IsDefault = true`). Overrides `GetProviderFactory` to return `DuckDBClientFactory.Instance`; `GetLastSchemaUpdate` returns `null`; `ClearAllPools` is a no-op.
- **`YdbProvider`** (`DatabaseProviders/YdbProvider.cs`): net8.0-windows only (`#if !NETFRAMEWORK`), same exemption from the 64-bit guard as `DuckDBProvider`. Single `ProviderInfo` entry (`ProviderName.Ydb`, display name `"YDB"`, `IsDefault = true`). `GetProviderFactory` returns `YdbProviderFactory.Instance`; `ClearAllPools` calls `YdbConnection.ClearAllPools().GetAwaiter().GetResult()` (async API bridged synchronously); `GetLastSchemaUpdate` returns `null` (unimplemented, same as DuckDB).
- **`PostgreSQLProvider`** (`DatabaseProviders/PostgreSQLProvider.cs`): exposes eight dialect-detection `ProviderInfo` entries -- `ProviderName.PostgreSQL` (auto-detect, default), `PostgreSQL92/93/95/13/15/18/19` (`PostgreSQL19` added this delta, `"PostgreSQL 19 Dialect"`, `PostgreSQLProvider.cs:20`). `GetProviderFactory` returns `NpgsqlFactory.Instance`; `ClearAllPools` calls `NpgsqlConnection.ClearAllPools()`; `GetLastSchemaUpdate` returns `null`.

`ProviderInfo` record: `Name`, `DisplayName`, `IsDefault`, `IsHidden`, `Troubleshoot`.

### Configuration (`Configuration/`)

**`ConnectionSettings`**: root settings object, serialized as JSON inside LINQPad's `IConnectionInfo.DriverData`. Contains `ConnectionOptions`, `SchemaOptions`, `ScaffoldOptions`, `LinqToDbOptions`, `StaticContextOptions`.

`ConnectionOptions.ProviderPath` is computed: dispatches to `ProviderPathx86` or `ProviderPathx64` based on `IntPtr.Size`.

**`AppConfig`**: `ILinqToDBSettings` implementation. Parses `appsettings.json` or `app.config` XML.

### WPF UI (`UI/`)

Dialog hosted as `SettingsDialog` (XAML `Window`). `DataContext` is `SettingsModel`, which aggregates seven sub-models. `ModelBase` provides a pattern for raised-per-property static `PropertyChangedEventArgs` instances.

**`AboutModel`** (`UI/Model/AboutModel.cs`): singleton (`Instance` property). Reads the driver version at runtime via `typeof(AboutModel).Assembly.GetName().Version` (three-part) -- no hardcoded version string. Constructs `Logo` from a `pack://` URI pointing to `Resources/Logo.png` embedded in the assembly; guards against `UriParser` initialization order with an `Application()` constructor call when `"pack"` is not yet a known scheme. Exposes `Project` (display string), `Copyright` (from `AssemblyCopyrightAttribute`), `RepositoryUri`, and `ReportsUri`. Bound by `AboutTab.xaml` (`UI/Settings/AboutTab.xaml`).

**`DynamicConnectionTab`** (`UI/Settings/DynamicConnectionTab.xaml`): `UserControl` with design-time `DataContext` of type `DynamicConnectionModel`. Renders: Database Type `ComboBox` (bound `Databases`/`Database`), Provider `ComboBox` (bound `Providers`/`Provider`, visibility-gated), provider-path `TextBox` + Select button (visibility-gated), connection string `TextBox` (multiline, with `{pm:name}` tooltip), secondary connection string panel (MS Access only, visibility-gated), Encrypt connection strings `CheckBox`, and command timeout `TextBox` (via `CommandTimeoutConverter`).

### Compat (`Compat/`)

`IReadOnlySet<T>`, `ReadOnlyHashSet<T>`, `ReadOnlySetExtensions` -- polyfills guarded with `#if NETFRAMEWORK`. On net8.0-windows the BCL's native `IReadOnlySet<T>` is used.

### Build / Packaging

Two project files govern the area:

- **`LinqToDB.LINQPad.csproj`** (`Microsoft.NET.Sdk.WindowsDesktop`): the WPF driver assembly, TFMs `net472` + `net8.0-windows7.0`. `UseWPF=true`, `IsPackable=false`. `.lpx`/`.lpx6` generation gated by `GenerateLpxArtifacts` property (default `true`); set to `false` from the Pack project to avoid coupling `dotnet pack` to 7-Zip / `Pack.cmd`. `<Page Update>` items for all nine XAML files (including `AboutTab.xaml`, `DynamicConnectionTab.xaml`) are declared here. Non-net472 `<ItemGroup>` carries both `DuckDB.NET.Data.Full` and `Ydb.Sdk` (line 69, added this delta) `PackageReference`s.
- **`LinqToDB.LINQPad.Pack.csproj`** (`Microsoft.NET.Sdk`): packaging-only, single TFM `net8.0`. Does NOT recompile the driver. References `LinqToDB.LINQPad.csproj` with `ReferenceOutputAssembly=false` for build ordering, then pulls the `net8.0-windows7.0` driver assembly into `lib/net8.0/` via the `_AddLinqPadDriverToPackage` MSBuild target (calls `GetTargetPath` on the sibling). Motivation: LINQPad on macOS/Linux rejected a `net8.0-windows7.0`-only package ("No compatible assemblies found", issue #5497); packing under plain `net8.0` restores the pre-#5279 layout. `EnableDefaultItems=false`, `IncludeBuildOutput=false`. Now also carries a `Ydb.Sdk` `PackageReference` (line 79, added this delta) alongside `DuckDB.NET.Data.Full`. `<Description>` (line 40) still lists 14 databases and omits both DuckDB and YDB -- known issue.

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
| `YdbProvider` | `DatabaseProviders/YdbProvider.cs` | net8.0-windows YDB provider |
| `ProviderInfo` | `DatabaseProviders/ProviderInfo.cs` | Per-dialect descriptor |
| `ConnectionSettings` | `Configuration/ConnectionSettings.cs` | Root settings DTO |
| `AppConfig` | `Configuration/AppConfig.cs` | `ILinqToDBSettings` for static contexts |
| `DynamicSchemaGenerator` | `Drivers/DynamicSchemaGenerator.cs` | SCAFFOLD bridge |
| `StaticSchemaGenerator` | `Drivers/StaticSchemaGenerator.cs` | Reflection-based schema tree |
| `ModelProviderInterceptor` | `Drivers/Scaffold/ModelProviderInterceptor.cs` | `ScaffoldInterceptors` impl |
| `DataModelAugmentor` | `Drivers/Scaffold/DataModelAugmentor.cs` | `ConvertCodeModelVisitor` |
| `SettingsModel` | `UI/Model/SettingsModel.cs` | Root ViewModel |
| `AboutModel` | `UI/Model/AboutModel.cs` | About-tab ViewModel; runtime version read |
| `ValueFormatter` | `Drivers/ValueFormatter.cs` | Provider-specific type rendering |
| `PasswordManager` | `Drivers/PasswordManager.cs` | Resolves `{pm:...}` tokens |
| `LinqToDBLinqPadException` | `LinqToDBLinqPadException.cs` | Public domain exception |

## Files (Tier 1 / Tier 2)

**Tier 1 (canonical anchors) -- 15 files:** Drivers (Dynamic/Static/DriverHelper), LINQPadDataConnection, IDatabaseProvider, DatabaseProviderBase, DatabaseProviders, ProviderInfo, ConnectionSettings, AppConfig, DynamicSchemaGenerator, StaticSchemaGenerator, Scaffold/ModelProviderInterceptor, Scaffold/DataModelAugmentor, csproj.

**Tier 2 -- visited 46 of 49 files:**
- Concrete providers (15 files, `YdbProvider.cs` added this delta): all sampled; Access, SqlServer, DuckDB, YDB, PostgreSQL read in full.
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
- `DuckDB.NET.Data.Full` NuGet -- used by `DuckDBProvider.GetProviderFactory` (net8.0-windows only); present in both `LinqToDB.LINQPad.csproj` (non-net472 ItemGroup) and `LinqToDB.LINQPad.Pack.csproj`.
- `Ydb.Sdk` NuGet -- used by `YdbProvider.GetProviderFactory` (`YdbProviderFactory.Instance`) and `ClearAllPools` (`YdbConnection.ClearAllPools`); net8.0-windows only; added this delta to both `LinqToDB.LINQPad.csproj` (non-net472 ItemGroup, line 69) and `LinqToDB.LINQPad.Pack.csproj` (line 79).

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
- `YdbProvider.GetLastSchemaUpdate` always returns `null` -- same unimplemented schema-change-detection limitation as DuckDB (added this delta).
- `LinqToDB.LINQPad.Pack.csproj` `<Description>` (line 40) still lists 14 named databases and omits both DuckDB and YDB despite both being registered providers.

## See also

- [SCAFFOLD area](../SCAFFOLD/INDEX.md)
- [PROV-SQLSERVER area](../PROV-SQLSERVER/INDEX.md)
- [architecture/overview.md](../../architecture/overview.md)

<details><summary>Coverage</summary>

- Tier 1: 15/15 done
- Tier 2: 46/49 done (93.9%)
- Tier 3 (skipped, logged): 6

**Delta read (prior run -- PR #5451 DuckDB additions):**
- `DatabaseProviders/DuckDBProvider.cs` -- new file; DuckDB provider impl with `DuckDBClientFactory.Instance` factory.
- `DatabaseProviders/DatabaseProviders.cs` -- `DuckDBProvider` registration added outside 64-bit guard.
- `LinqToDB.LINQPad.csproj` -- `DuckDB.NET.Data.Full` added to net8.0-windows ItemGroup.

**Read (this run -- delta, sha 2e67bafc9):**
- `Source/LinqToDB.LINQPad/UI/Model/AboutModel.cs` -- no behavioral change; reads version at runtime from `Assembly.GetName().Version`; constructs `pack://` logo URI with initialization-order guard; exposes `RepositoryUri` / `ReportsUri`. Surfaced in delta as packaging churn; `AboutModel` entry added to Key types and WPF UI subsection.
- `Source/LinqToDB.LINQPad/UI/Settings/AboutTab.xaml` -- pure XAML layout bound to `AboutModel`; `<Page Update>` entry added to `LinqToDB.LINQPad.Pack.csproj` in this delta. No content change.
- `Source/LinqToDB.LINQPad/LinqToDB.LINQPad.Pack.csproj` -- `DuckDB.NET.Data.Full` confirmed present (line 62); `AboutTab.xaml` added to `<Page Update>` block (lines 94-97); `<Description>` still omits DuckDB (known-issue bullet cites line 20).


**Read (this run -- delta, sha b3340aa9):**
- `Source/LinqToDB.LINQPad/LinqToDB.LINQPad.Pack.csproj` -- now a packaging-only project (`Microsoft.NET.Sdk`, single TFM `net8.0`, `EnableDefaultItems=false`, `IncludeBuildOutput=false`); driver assembly pulled into `lib/net8.0/` via `_AddLinqPadDriverToPackage` MSBuild target; split motivated by macOS/Linux NuGet compatibility issue #5497 (pre-#5279 layout restored). `<Description>` (line 40) still omits DuckDB. `DuckDB.NET.Data.Full` PackageReference present (line 78).
- `Source/LinqToDB.LINQPad/LinqToDB.LINQPad.csproj` -- SDK changed to `Microsoft.NET.Sdk.WindowsDesktop`; `IsPackable=false` added; `GenerateLpxArtifacts` property gate added for PostBuild `.lpx`/`.lpx6` targets; `<Page Update>` block for all nine XAML files (including `AboutTab.xaml`, `DynamicConnectionTab.xaml`) declared here; `DuckDB.NET.Data.Full` in non-net472 `<ItemGroup>` (line 68).
- `Source/LinqToDB.LINQPad/UI/Settings/DynamicConnectionTab.xaml` -- `UserControl`, design-time `DataContext` `DynamicConnectionModel`; renders Database Type / Provider combo boxes, provider path TextBox + Select button, connection string TextBox (multiline, `{pm:name}` tooltip), secondary connection string panel (MS Access, visibility-gated), Encrypt checkBox, command timeout TextBox (`CommandTimeoutConverter`). No structural change; surfaced for first-time explicit coverage.

**Read (this run -- delta, sha 36ee4f82):**
- `Source/LinqToDB.LINQPad/DatabaseProviders/DatabaseProviders.cs` -- registers new `YdbProvider` unconditionally inside `#if !NETFRAMEWORK` (outside the 64-bit guard, line 37), alongside `DuckDBProvider`.
- `Source/LinqToDB.LINQPad/DatabaseProviders/PostgreSQLProvider.cs` -- added `ProviderName.PostgreSQL19` (`"PostgreSQL 19 Dialect"`) to the dialect-detection `ProviderInfo` list (now eight entries, line 20).
- `Source/LinqToDB.LINQPad/DatabaseProviders/YdbProvider.cs` -- new file; `DatabaseProviderBase` subclass for YDB, `#if !NETFRAMEWORK`-gated, mirrors `DuckDBProvider`'s shape (`GetProviderFactory` -> `YdbProviderFactory.Instance`, `GetLastSchemaUpdate` -> `null`, `ClearAllPools` -> `YdbConnection.ClearAllPools().GetAwaiter().GetResult()`).
- `Source/LinqToDB.LINQPad/LinqToDB.LINQPad.Pack.csproj` -- added `PackageReference Include="Ydb.Sdk"` to the net8.0 dependency group (line 79); `<Description>` (line 40) not updated, still omits DuckDB and now also YDB.
- `Source/LinqToDB.LINQPad/LinqToDB.LINQPad.csproj` -- added `PackageReference Include="Ydb.Sdk"` to the non-net472 `<ItemGroup>` (line 69), alongside existing `DuckDB.NET.Data.Full`.

</details>
