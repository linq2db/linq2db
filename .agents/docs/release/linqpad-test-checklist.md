# LINQPad smoke + targeted-change checklist

What to actually click in LINQPad 5 / 7+ when testing a release. Filled as the skill learns the structure of each release's test surface.

## T4 build prerequisite

Before any T4 / NuGet-T4 / CLI track in `/release-test-matrix`, the solution must be built in Debug targeting net462 to populate the T4 binaries under `.build`. Exact command:

```
dotnet build linq2db.slnx -c Debug
```

Solution-level Debug build emits net462 outputs alongside other TFMs (the slnx rejects single-TFM `-f net462`). Recorded on 6.3.0.

## Tests.T4.Nugets version property

`Tests/Tests.T4.Nugets/Directory.Packages.props` has a single `<Version>` property at the top of its `<PropertyGroup>`. Every `<PackageVersion>` for the linq2db family (`linq2db`, `linq2db.SqlServer`, `linq2db.t4models`, etc.) references it as `Version="$(Version)"`. Update that one property to the just-built local nuget version before `dotnet restore` of `Tests.T4.Nugets.slnx`.

Plain release version (e.g. `6.3.0` matching `dotnet pack`'s default output) works as the value when iterating once; for multiple iterations use `<version>-local.<N>` and bump N + re-pack with `-p:Version=<version>-local.<N>` to invalidate the NuGet cache.

Recorded on 6.3.0.

## T4 nuget pack: dynamic content via Target injection

Some content in the `tools/` directory of a scaffold nuget (notably `clidriver/` for `linq2db.DB2` and `linq2db.t4models`) is populated by a dependent build (`NuGet.csproj` via `IBM.Data.DB.Provider`'s build/restore step), **after** the consuming `linq2db.DB2.csproj` / `linq2db.t4models.csproj`'s static `<None Include>` globs are evaluated at project-load time.

**The trap:** a declaration like

```xml
<None Include="$(ToolsPath)/clidriver/**" Pack="true" PackagePath="tools/clidriver" />
```

evaluates the glob at project-load (when `$(ToolsPath)/clidriver/` doesn't exist yet), matches zero files, and silently ships an empty section. `dotnet pack` reports success — but the nupkg is missing the entire native folder. Symptom (downstream): `DllNotFoundException: db2app64.dll` when the IBM provider is instantiated.

**The fix:** inject items into Pack's internal `_PackageFiles` collection from a Target that runs after the build but before pack's content gathering:

```xml
<Target Name="IncludeClidriverInPack" BeforeTargets="_GetPackageFiles">
    <ItemGroup>
        <_PackageFiles Include="$(ToolsPath)\clidriver\**\*">
            <BuildAction>None</BuildAction>
            <PackagePath>tools\clidriver\%(RecursiveDir)%(Filename)%(Extension)</PackagePath>
        </_PackageFiles>
    </ItemGroup>
</Target>
```

`BeforeTargets="_GetPackageFiles"` is the canonical hook — `_GetPackageFiles` is what runs the content collection. `_PackageFiles` is Pack's internal item; populating it inside a Target gets dynamic evaluation. The static `<None>` approach with `Pack="true"` doesn't work even from inside a Target — by the time the Target runs, Pack's collection is already closed for `<None>` items, but `_PackageFiles` is still open.

Verify post-pack:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead('<nupkg-path>')
($z.Entries | Where-Object FullName -like '*clidriver*').Count
$z.Dispose()
```

Pattern broadly applicable to any pack-time content populated by a dependent build, not just clidriver. Recorded on 6.3.0 (commit 1c7d083c9 applied this fix to `NuGet/DB2/linq2db.DB2.csproj` + `NuGet/t4models/linq2db.t4models.csproj`).

## LINQPad NuGet driver must ship `lib/net8.0`, not `lib/net8.0-windows7.0`

The `linq2db.LINQPad` driver is a WPF assembly (compiled `net8.0-windows7.0`), but LINQPad installs drivers via NuGet on **every** OS. A `net8.0-windows7.0`-only package is rejected by NuGet's compatibility resolver on macOS/Linux → *"No compatible assemblies found"* (issue #5497; broke in 6.2.0 when #5279 replaced the hand-authored nuspec with csproj `dotnet pack`, which honors the project's `net8.0-windows7.0` TFM).

The package must present the platform-neutral `net8.0` moniker in **both** the lib folder and the dependency group. That's why `LinqToDB.LINQPad.Pack.csproj` is a **`net8.0` packaging-only** project: it references the `net8.0-windows7.0` build of `LinqToDB.LINQPad.csproj` for build-ordering only (`ReferenceOutputAssembly=false`, `SetTargetFramework`) and packs that built assembly into `lib/net8.0/` from a `BeforeTargets="_GetPackageFiles"` target. **Do not** collapse it back into a single `net8.0-windows7.0` pack project — that reintroduces #5497.

Why a single project can't just relabel the folder: NuGet derives the **dependency-group** TFM from the restore graph (`project.assets.json`), not a free-text label. A `net8.0-windows7.0` project can move the lib folder via `_BuildOutputInPackage` metadata, but its deps group stays `net8.0-windows7.0` (→ `NU5128`, and unresolved dependencies on a macOS consumer). Hence the compile/pack split. Trade-off: the package no longer declares the WPF `<frameworkReferences>` (a net8.0 project can't) — no practical effect on LINQPad's driver loading.

Post-pack verification — the produced nupkg must have a `net8.0` lib folder, not `net8.0-windows7.0`:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead('<linq2db.LINQPad nupkg path>')
$z.Entries.FullName | Where-Object { $_ -like 'lib/*' }   # expect lib/net8.0/..., NOT lib/net8.0-windows7.0/...
$z.Dispose()
```

Recorded on 6.4.0 (issue #5497, fix in PR #5571).

## LINQPad driver install automation

Neither LINQPad 5 nor LINQPad 9 exposes a CLI for driver install/update — both are UI-only by default. File-copy workarounds:

### LINQPad 5 (.lpx, .NET Framework)

Extract the `.lpx` (a renamed zip) directly into LINQPad 5's per-user driver folder. Close LINQPad 5 first — it locks DLLs while running.

```powershell
$target = Join-Path "$env:LOCALAPPDATA\LINQPad\Drivers\DataContext\4.6" 'linq2db.LINQPad (no-strong-name)'
if (Test-Path $target) { Remove-Item -Recurse -Force $target }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory('.build/lpx/linq2db.LINQPad.lpx', $target)
```

The `(no-strong-name)` suffix is LINQPad 5's convention for unsigned drivers (linq2db's .lpx is unsigned).

### LINQPad 9 (.lpx6 / NuGet driver)

LINQPad 9 reads NuGet sources from a `NuGetSources.xml` next to `LINQPad9.exe` (or `LPRun9.exe`); after configuring it the UI's NuGet Manager shows the local feed's versions:

```xml
<NuGetSources><Source Name="local" URI="<path-or-feed-url>"/></NuGetSources>
```

For full no-UI install/update, drop the `.nupkg`'s `lib/<TFM>/` contents into LINQPad 9's driver-cache folder (close LINQPad 9 first):

```
%LocalAppData%\LINQPad\Drivers\DataContext\NetCore\linq2db.LINQPad\
```

LINQPad 9 doesn't poll the feed for new versions — manual click "Update" in NuGet Manager OR re-do the file-copy when iterating with bumped `-local.N` versions.

## LINQPad 5 (.lpx) smoke

Default checklist (extended on first run):

- [ ] LINQPad starts with no error dialog.
- [ ] linq2db connection wizard appears under Add Connection.
- [ ] Connect to one provider (default: SQL Server) — schema browsable, sample query runs.
- [ ] Run a simple LINQ query → expected results.
- [ ] Run a more complex query that touches the changed surface (release-specific).

## LINQPad 7+ (nugets) smoke

Same as above plus:

- [ ] Nuget installs from the local test feed (recorded path in [`external-repos.md`](./external-repos.md)).
- [ ] Schema browser does not throw for any enabled provider.

## Targeted-change rows

Filled in per release when changes touch the LINQPad driver, scaffold library, or provider surface.

### Release <version>

<!-- entries appended by /release-test-matrix 4.8 on a per-release basis -->
