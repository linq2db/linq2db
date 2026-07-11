#!/usr/bin/env pwsh
# Regenerates the F# CLI-scaffolder baselines under Tests/FSharp.Scaffold/ (issue #1553).
#
# One .fs file per provider, each in its own namespace (Tests.FSharp.Scaffold.<Provider>) so the
# generated TestDataDB context names don't collide in the single test assembly. The committed files
# are the golden output: after any change to the F# code generator, rerun this and commit the diff;
# CI's compile of Tests.FSharp.Scaffold.fsproj is the regression gate.
#
# Requires the provider databases to be reachable (the same docker containers the test suite uses).
# Docker-backed connection strings (PostgreSQL, SQL Server) are resolved by name from
# DataProviders.json / UserDataProviders.json - the same lookup the CLI baseline matrix uses - not
# hardcoded. SQLite uses the on-disk test DB file (config's SQLite.Classic is an in-memory DB, unusable
# for scaffolding), matching how the C# release-test-cli-scaffold.ps1 composes it.

[CmdletBinding()]
param(
    # connection names looked up in DataProviders.json / UserDataProviders.json
    [string] $PostgreSQLConnectionName = 'PostgreSQL.16',
    [string] $SqlServerConnectionName  = 'SqlServer.2022',
    # explicit full connection-string overrides (skip config lookup when set)
    [string] $PostgreSQLConnection,
    [string] $SqlServerConnection,
    [string] $SQLiteConnection,
    # build configuration used to locate/build the CLI
    [string] $Configuration            = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot/../.."
$cliProj  = Join-Path $repoRoot 'Source/LinqToDB.CLI/LinqToDB.CLI.csproj'
$cliDll   = Join-Path $repoRoot ".build/bin/LinqToDB.CLI/$Configuration/net10.0/dotnet-linq2db.dll"

if (-not (Test-Path $cliDll)) {
    Write-Host "CLI not found at $cliDll - building..."
    dotnet build $cliProj -c $Configuration -f net10.0
    if ($LASTEXITCODE -ne 0) { throw "CLI build failed" }
}

# --- Connection-string resolution (mirrors release-test-cli-scaffold.ps1's CN()) ------------------
# Priority: UserDataProviders.json MyConnectionStrings -> DataProviders.json LocalConnectionStrings
#           -> DataProviders.json CommonConnectionStrings. Missing keys throw.
$dpJsonPath = Join-Path $repoRoot 'DataProviders.json'
if (-not (Test-Path $dpJsonPath)) { throw "DataProviders.json not found at $dpJsonPath" }
$dpJson = Get-Content $dpJsonPath -Raw | ConvertFrom-Json

$userConns = $null
$userJsonPath = Join-Path $repoRoot 'UserDataProviders.json'
if (Test-Path $userJsonPath) {
    $userJson = Get-Content $userJsonPath -Raw | ConvertFrom-Json
    if ($userJson.MyConnectionStrings -and $userJson.MyConnectionStrings.Connections) {
        $userConns = $userJson.MyConnectionStrings.Connections
    }
}

function CN([string]$name) {
    if ($userConns -and $userConns.PSObject.Properties.Match($name).Count -gt 0 -and $userConns.$name.ConnectionString) {
        return $userConns.$name.ConnectionString
    }
    $c = $dpJson.LocalConnectionStrings.Connections.$name
    if ($c -and $c.ConnectionString) { return $c.ConnectionString }
    $c = $dpJson.CommonConnectionStrings.Connections.$name
    if ($c -and $c.ConnectionString) { return $c.ConnectionString }
    throw "Connection '$name' missing in MyConnectionStrings / LocalConnectionStrings / CommonConnectionStrings"
}

if (-not $SQLiteConnection)     { $SQLiteConnection     = "Data Source=" + (Join-Path $repoRoot 'Data/TestData.sqlite') }
if (-not $PostgreSQLConnection) { $PostgreSQLConnection = CN $PostgreSQLConnectionName }
if (-not $SqlServerConnection)  { $SqlServerConnection  = CN $SqlServerConnectionName }

# options shared by every provider baseline. The partial/init-context options are C#-only (the CLI
# rejects them for F#); association-extensions / IEquatable / init methods are forced off for F#
# internally, so they're omitted here rather than passed-and-overridden. --nrt is set per mode below.
$commonOptions = @(
    '--objects', 'table,view,foreign-key,stored-procedure,scalar-function,table-function,aggregate-function',
    '--find-methods', 'sync-pk-table,async-pk-table,query-pk-table,sync-pk-context,async-pk-context,query-pk-context,sync-entity-table,async-entity-table,query-entity-table,sync-entity-context,async-entity-context,query-entity-context',
    '--include-datatype', 'true',
    '--include-db-type', 'true',
    '--include-length', 'true',
    '--include-precision', 'true',
    '--include-scale', 'true',
    '--load-sproc-schema', 'true',
    '--add-db-type-to-procedures', 'true',
    '--prefer-provider-types', 'true'
)

function Invoke-Scaffold {
    param(
        [string]   $Provider,
        [string]   $Connection,
        [string]   $Namespace,
        [string]   $OutputSubfolder,
        [string[]] $ExtraOptions = @()
    )

    $output = Join-Path $PSScriptRoot $OutputSubfolder
    Write-Host "Scaffolding $Provider -> $OutputSubfolder"

    & dotnet $cliDll scaffold `
        -p $Provider `
        -c $Connection `
        --target-language 'f#' `
        -n $Namespace `
        --context-name TestDataDB `
        -o $output `
        --overwrite true `
        @commonOptions @ExtraOptions

    if ($LASTEXITCODE -ne 0) { throw "Scaffolding failed for $Provider" }
}

# each provider is scaffolded twice - once per nullness mode - for both-mode coverage:
#   --nrt true  -> <Provider>/       (ns Tests.FSharp.Scaffold.<Provider>)       compiled by Tests.FSharp.Scaffold.fsproj      (<Nullable>enable</Nullable>)
#   --nrt false -> NoNrt/<Provider>/ (ns Tests.FSharp.Scaffold.NoNrt.<Provider>) compiled by Tests.FSharp.Scaffold.NoNrt.fsproj (<Nullable>disable</Nullable>)
$providers = @(
    @{ Provider = 'SQLite';     Sub = 'SQLite';     Connection = $SQLiteConnection;     Extra = @() }
    @{ Provider = 'PostgreSQL'; Sub = 'PostgreSQL'; Connection = $PostgreSQLConnection; Extra = @() }
    @{ Provider = 'SQLServer';  Sub = 'SqlServer';  Connection = $SqlServerConnection;  Extra = @('--mssql-enable-return-value-parameter', 'true') }
)

foreach ($p in $providers) {
    Invoke-Scaffold -Provider $p.Provider -Connection $p.Connection `
        -Namespace "Tests.FSharp.Scaffold.$($p.Sub)" -OutputSubfolder $p.Sub `
        -ExtraOptions (@('--nrt', 'true') + $p.Extra)

    Invoke-Scaffold -Provider $p.Provider -Connection $p.Connection `
        -Namespace "Tests.FSharp.Scaffold.NoNrt.$($p.Sub)" -OutputSubfolder (Join-Path 'NoNrt' $p.Sub) `
        -ExtraOptions (@('--nrt', 'false') + $p.Extra)
}

Write-Host 'Done. Review the git diff and build Tests.FSharp.Scaffold.fsproj + Tests.FSharp.Scaffold.NoNrt.fsproj to validate.'
