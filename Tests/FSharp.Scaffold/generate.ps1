#!/usr/bin/env pwsh
# Regenerates the F# CLI-scaffolder baselines under Tests/FSharp.Scaffold/ (issue #1553).
#
# One .fs file per provider, each in its own namespace (Tests.FSharp.Scaffold.<Provider>) so the
# generated TestDataDB context names don't collide in the single test assembly. The committed files
# are the golden output: after any change to the F# code generator, rerun this and commit the diff;
# CI's compile of Tests.FSharp.Scaffold.fsproj is the regression gate.
#
# Requires the provider databases to be reachable (the same docker containers the test suite uses).
# Connection strings default to the local test values (see UserDataProviders.json) and can be
# overridden per provider.

[CmdletBinding()]
param(
    [string] $SQLiteConnection     = "Data Source=$PSScriptRoot/../../Data/TestData.sqlite",
    [string] $PostgreSQLConnection = 'Server=localhost;Port=5416;Database=testdata;User Id=postgres;Password=Password12!;Pooling=true;MinPoolSize=10;MaxPoolSize=100;',
    [string] $SqlServerConnection  = 'Server=localhost,1422;Database=TestData;User Id=sa;Password=Password12!;Encrypt=true;TrustServerCertificate=true',
    # build configuration used to locate/build the CLI
    [string] $Configuration        = 'Release'
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

# options shared by every provider baseline. --nrt and the partial/init-context options are C#-only
# (the CLI rejects them for F#); association-extensions / IEquatable / init methods are forced off for
# F# internally, so they're omitted here rather than passed-and-overridden.
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

Invoke-Scaffold -Provider 'SQLite'     -Connection $SQLiteConnection     -Namespace 'Tests.FSharp.Scaffold.SQLite'     -OutputSubfolder 'SQLite'
Invoke-Scaffold -Provider 'PostgreSQL' -Connection $PostgreSQLConnection -Namespace 'Tests.FSharp.Scaffold.PostgreSQL' -OutputSubfolder 'PostgreSQL'
Invoke-Scaffold -Provider 'SQLServer'  -Connection $SqlServerConnection  -Namespace 'Tests.FSharp.Scaffold.SqlServer'  -OutputSubfolder 'SqlServer' `
    -ExtraOptions @('--mssql-enable-return-value-parameter', 'true')

Write-Host 'Done. Review the git diff and build Tests.FSharp.Scaffold.fsproj to validate.'
