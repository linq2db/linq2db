<#
release-test-cli-scaffold.ps1 — run the linq2db CLI scaffold matrix in parallel.

Mirrors the `RunCliTool()` calls in `Tests/Tests.T4/Cli/*.tt` but invokes
`dotnet-linq2db.dll` directly so the matrix can be exercised outside Visual
Studio and across all providers in parallel. Owned by `/release-test-matrix`
track 4.7 (CLI scaffold).

Layout (mirrored from CLI.ttinclude + each `<Template>.tt`):

  Template  | Mode     | Rows | extraOptions
  ----------+----------+------+-------------------------------------------------
  Default   | default  |  22  | (none)
  All       | default  |  24  | --prefer-provider-types true ... (full set)
  Fluent    | default  |  22  | --metadata fluent ...
  NoMetadata| default  |  22  | --metadata none ...
  T4        | t4       |  22  | (none — mode='t4' is the differentiator)
  NewCli-   | default  |   2  | (per-row, SQLite only; tests new CLI features)
   Features |          |      |

  Total raw: 114 invocations. After default -SkipProviders (Azure variants
  + SqlCe by default), ~107.

Default behaviour:

  1. RepoRoot defaults to the script's `..\..` (two levels up from
     `.agents/scripts/`), i.e. the repo containing this `.agents` folder.
     Override with `-RepoRoot` when running from a different worktree.
  2. CliDll defaults to `<RepoRoot>/.build/publish/LinqToDB.CLI/Debug/net9.0/win-x64/dotnet-linq2db.dll`.
     The script does NOT build the CLI — caller must have a successful
     `dotnet build linq2db.slnx -c Debug` first (release-test-matrix 4.0).
  3. Connection strings for docker-backed providers are merged from
     `<RepoRoot>/DataProviders.json` -> `LocalConnectionStrings.Connections`.
     File-based providers (Access, DuckDB, SQLite, SqlCe) compose their CN
     in-script from `<RepoRoot>/.build/bin/NuGet/Debug/net462/Database/`.
  4. Azure + Azure.MI providers are skipped by default (they need an
     externally-hosted Azure SQL DB).

Parameters:

  -RepoRoot       Path to linq2db repo (auto-detected from script location).
  -CliDll         Path to dotnet-linq2db.dll (auto-derived from RepoRoot).
  -Templates      Array of template names to run.
                  Default: Default, All, Fluent, NewCliFeatures, NoMetadata, T4.
  -Providers      Filter to specific scaffold keys (e.g. 'SQLite','SqlServer2025').
                  Empty = no filter.
  -SkipProviders  Skip-list. Default: Azure variants.
  -DryRun         Print the inferred command lines, run nothing.
  -Parallelism    Max simultaneous CLI invocations. Default 6.
  -TimeoutSec     Per-invocation timeout. Default 120.
  -OutputJson     Emit a JSON result blob to stdout (skill consumption).

Usage:

  # Dry-run the full matrix.
  pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 -DryRun

  # Sanity check on SQLite only (no docker).
  pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 `
       -Templates Default -Providers SQLite

  # Skip extra providers (no SapHana running).
  pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 `
       -SkipProviders SqlServer.Azure,SqlServer.Azure.MI,SapHana,SqlCe

  # Run full matrix, machine-readable result.
  pwsh -NoProfile -File .agents/scripts/release-test-cli-scaffold.ps1 -OutputJson
#>

param(
    [string]   $RepoRoot,
    [string]   $CliDll,
    [string[]] $Templates    = @('Default','All','Fluent','NewCliFeatures','NoMetadata','T4'),
    [string[]] $Providers    = @(),
    [string[]] $SkipProviders = @('SqlServer.Azure','SqlServer.Azure.MI'),
    [switch]   $DryRun,
    [int]      $Parallelism  = 6,
    [int]      $TimeoutSec   = 120,
    [switch]   $OutputJson
)

$ErrorActionPreference = 'Stop'

# --- Defaults derived from script location ------------------------------------------------------
if (-not $RepoRoot) {
    # Script is at <repo>/.agents/scripts/release-test-cli-scaffold.ps1
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
if (-not $CliDll) {
    $CliDll = Join-Path $RepoRoot '.build\publish\LinqToDB.CLI\Debug\net9.0\win-x64\dotnet-linq2db.dll'
}

if (-not (Test-Path $CliDll)) {
    [Console]::Error.WriteLine("CliDll not found: $CliDll")
    [Console]::Error.WriteLine("Run track 4.0 first: dotnet build linq2db.slnx -c Debug")
    exit 2
}
$dpJsonPath = Join-Path $RepoRoot 'DataProviders.json'
if (-not (Test-Path $dpJsonPath)) {
    [Console]::Error.WriteLine("DataProviders.json not found at: $dpJsonPath (override -RepoRoot)")
    exit 2
}

# --- Path resolution (mirrors CLI.ttinclude's MSBuild property expansion) -----------------------
$databasesPath = Join-Path $RepoRoot '.build\bin\NuGet\Debug\net462\Database'
$artifactsPath = Join-Path $RepoRoot '.build'
$cliOutRoot    = Join-Path $RepoRoot 'Tests\Tests.T4\Cli'

# Pre-flight: the file-based providers (SQLite, Access ODBC/OleDb, DuckDB, SqlCe)
# expect their test DBs at $databasesPath. That folder is populated as a side
# effect of packing linq2db.t4models — but a fresh worktree (or one that hasn't
# pack'd in this configuration) won't have it. The script otherwise fails with
# every file-based provider reporting "file not found", which is confusing.
# Surface the missing-files case explicitly with a one-line fix hint.
$expectedDbs = @('TestData.sqlite','Northwind.sqlite','TestData.ODBC.mdb','TestData.mdb','TestData.duckdb')
$missingDbs = $expectedDbs | Where-Object { -not (Test-Path -LiteralPath (Join-Path $databasesPath $_)) }
if ($missingDbs.Count -gt 0 -and (-not $Providers -or ($Providers | Where-Object { $_ -match '^(SQLite|SQLiteNorthwind|Access|DuckDB|SqlCe)' }))) {
    [Console]::Error.WriteLine("Database/ files missing under: $databasesPath")
    [Console]::Error.WriteLine("Missing: $($missingDbs -join ', ')")
    [Console]::Error.WriteLine("Quick fix (file-based providers only — DB2/Firebird/etc. unaffected):")
    [Console]::Error.WriteLine("  New-Item -ItemType Directory -Force -Path '$databasesPath' | Out-Null")
    [Console]::Error.WriteLine("  Copy-Item '$RepoRoot\Data\TestData.sqlite','$RepoRoot\Data\Northwind.sqlite','$RepoRoot\Data\TestData.ODBC.mdb','$RepoRoot\Data\TestData.mdb','$RepoRoot\Data\TestData.duckdb' -Destination '$databasesPath'")
    [Console]::Error.WriteLine("Or pack linq2db.t4models in Debug to populate it via the normal pipeline.")
    exit 2
}
# DB2 provider DLL must match the CLI's TFM (net8 / net9 / net10) — derive from CliDll path.
$cliTfm = if ($CliDll -match '\\(net\d+\.\d+)\\') { $Matches[1] } else { 'net9.0' }
$db2ProvLoc    = Join-Path $artifactsPath "bin\Tests\Debug\$cliTfm\IBM.Data.Db2.dll"
$sqlCeProvLoc  = 'c:\Program Files\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll'

# --- File-based connection strings (no docker) --------------------------------------------------
$accessOdbcCN  = "Driver={Microsoft Access Driver (*.mdb, *.accdb)};Dbq=$databasesPath\TestData.ODBC.mdb;ExtendedAnsiSQL=1"
$accessOleDbCN = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=$databasesPath\TestData.mdb;Locale Identifier=1033;Persist Security Info=True"
$sqliteCN      = "Data Source=$databasesPath\TestData.sqlite"
$sqliteNwCN    = "Data Source=$databasesPath\Northwind.sqlite"
$sqlceCN       = "Data Source=$databasesPath\TestData.sdf"
$duckdbCN      = "Data Source=$databasesPath\TestData.duckdb"

# --- Connection-string resolution -----------------------------------------------------------------
# Mirrors what SettingsReader.Deserialize() does in CLI.ttinclude / Tests.Base: walks the BasedOn
# chain from the user file's overrides first, then falls back to the base file's defaults.
# Priority order: user MyConnectionStrings.Connections -> base LocalConnectionStrings.Connections
#                 -> base CommonConnectionStrings.Connections. Missing keys throw.
$dpJson = Get-Content $dpJsonPath -Raw | ConvertFrom-Json
$userJsonPath = Join-Path $RepoRoot 'UserDataProviders.json'
$userConns = $null
if (Test-Path $userJsonPath) {
    $userJson = Get-Content $userJsonPath -Raw | ConvertFrom-Json
    if ($userJson.MyConnectionStrings -and $userJson.MyConnectionStrings.Connections) {
        $userConns = $userJson.MyConnectionStrings.Connections
    }
}
function CN([string]$name) {
    if ($userConns -and $userConns.PSObject.Properties.Match($name).Count -gt 0) {
        $c = $userConns.$name
        if ($c -and $c.ConnectionString) { return $c.ConnectionString }
    }
    $c = $dpJson.LocalConnectionStrings.Connections.$name
    if ($c -and $c.ConnectionString) { return $c.ConnectionString }
    $c = $dpJson.CommonConnectionStrings.Connections.$name
    if ($c -and $c.ConnectionString) { return $c.ConnectionString }
    throw "Connection '$name' missing in MyConnectionStrings/LocalConnectionStrings/CommonConnectionStrings"
}

# --- Template extra-options (mirrors each .tt's `var options = ...` block) ----------------------
$optionsAll = @(
    '--prefer-provider-types true','--ignore-duplicate-fk false','--safe-schema-only false',
    '--load-sproc-schema true','--include-datatype true','--include-db-type true',
    '--include-length true','--include-precision true','--include-scale true',
    '--add-association-extensions true','--add-db-type-to-procedures true','--equatable-entities true',
    '--mssql-enable-return-value-parameter true',
    '--objects table,view,foreign-key,stored-procedure,scalar-function,table-function,aggregate-function',
    '--find-methods sync-pk-table,async-pk-table,query-pk-table,sync-pk-context,async-pk-context,query-pk-context,sync-entity-table,async-entity-table,query-entity-table,sync-entity-context,async-entity-context,query-entity-context',
    '--add-options-ctor true'
) -join ' '
$optionsAllNoTypes = ($optionsAll -replace '--prefer-provider-types true','').Trim()

$optionsFluent = @(
    '--metadata fluent','--safe-schema-only false','--load-sproc-schema true',
    '--add-association-extensions true','--find-methods none',
    '--objects table,view,foreign-key,stored-procedure,scalar-function,table-function,aggregate-function'
) -join ' '

$optionsNoMetadata = @(
    '--metadata none','--safe-schema-only false','--load-sproc-schema true',
    '--add-association-extensions true','--add-init-context false','--find-methods none',
    '--objects table,view,foreign-key,stored-procedure,scalar-function,table-function,aggregate-function'
) -join ' '

function GetExtraOptions([string]$tpl, [string]$scaffoldKey) {
    switch ($tpl) {
        'Default'        { return '' }
        'All'            { if ($scaffoldKey -match '^MariaDB$|^MySql$') { return $optionsAllNoTypes } else { return $optionsAll } }
        'Fluent'         { return $optionsFluent }
        'NewCliFeatures' { return '' }   # per-row options handled separately
        'NoMetadata'     { return $optionsNoMetadata }
        'T4'             { return '' }
        default          { throw "Unknown template '$tpl'" }
    }
}
function GetMode([string]$tpl) {
    if ($tpl -eq 'T4') { return 't4' } else { return 'default' }
}

# --- Matrix: each row mirrors a RunCliTool() call in the .tt files ------------------------------
$matrix = @(
    # 'ns' is the namespace suffix (mirrors the .tt's namespaceName argument); when unset, falls back to 'key'.
    # Access variants use a dotted namespace ('Access.Odbc') but a flat dir name ('AccessOdbc').
    @{ p='Access';          cn=$null;                 key='AccessOdbc';         ns='Access.Odbc';  cs=$accessOdbcCN;  pl=$null;        add=$null         }
    @{ p='Access';          cn=$null;                 key='AccessOleDb';        ns='Access.OleDb'; cs=$accessOleDbCN; pl=$null;        add=$null         }
    @{ p='Access';          cn=$null;                 key='AccessBoth';         ns='Access.Both';  cs=$accessOleDbCN; pl=$null;        add=$accessOdbcCN }
    @{ p='DuckDB';          cn=$null;                 key='DuckDB';             cs=$duckdbCN;      pl=$null;        add=$null         }
    @{ p='DB2';             cn='DB2';                 key='DB2';                cs=$null;          pl=$db2ProvLoc;  add=$null         }
    @{ p='Firebird';        cn='Firebird.5';          key='Firebird';           cs=$null;          pl=$null;        add=$null         }
    @{ p='Informix';        cn='Informix.DB2';        key='Informix';           cs=$null;          pl=$db2ProvLoc;  add=$null         }
    @{ p='MySQL';           cn='MariaDB.11';          key='MariaDB';            cs=$null;          pl=$null;        add=$null         }
    @{ p='MySQL';           cn='MySqlConnector.8.0';  key='MySql';              cs=$null;          pl=$null;        add=$null         }
    @{ p='Oracle';          cn='Oracle.11.Managed';   key='Oracle';             cs=$null;          pl=$null;        add=$null         }
    @{ p='PostgreSQL';      cn='PostgreSQL.10';       key='PostgreSQL';         cs=$null;          pl=$null;        add=$null         }
    @{ p='SapHana';         cn='SapHana.Native';      key='SapHana';            cs=$null;          pl=$null;        add=$null         }
    @{ p='SqlCe';           cn=$null;                 key='SqlCe';              cs=$sqlceCN;       pl=$sqlCeProvLoc;add=$null         }
    @{ p='SQLite';          cn=$null;                 key='SQLiteNorthwind';    cs=$sqliteNwCN;    pl=$null;        add=$null         }
    @{ p='SQLite';          cn=$null;                 key='SQLite';             cs=$sqliteCN;      pl=$null;        add=$null         }
    @{ p='SQLServer';       cn='SqlServer.Northwind'; key='SqlServerNorthwind'; cs=$null;          pl=$null;        add=$null         }
    @{ p='SQLServer';       cn='SqlServer.2017';      key='SqlServer';          cs=$null;          pl=$null;        add=$null         }
    @{ p='SQLServer';       cn='SqlServer.2025';      key='SqlServer2025';      cs=$null;          pl=$null;        add=$null         }
    @{ p='Sybase';          cn='Sybase.Managed';      key='Sybase';             cs=$null;          pl=$null;        add=$null         }
    @{ p='ClickHouseMySql'; cn='ClickHouse.MySql';    key='ClickHouse.MySql';   cs=$null;          pl=$null;        add=$null         }
    @{ p='ClickHouseHttp';  cn='ClickHouse.Driver';   key='ClickHouse.Driver';  cs=$null;          pl=$null;        add=$null         }
    @{ p='ClickHouseTcp';   cn='ClickHouse.Octonica'; key='ClickHouse.Octonica';cs=$null;          pl=$null;        add=$null         }
    # All-only extras (Azure variants); AllOnly => Default/Fluent/NoMetadata/T4 skip these
    @{ p='SQLServer';       cn='SqlServer.Azure';     key='SqlServer.Azure';    cs=$null;          pl=$null;        add=$null; AllOnly=$true }
    @{ p='SQLServer';       cn='SqlServer.Azure.MI';  key='SqlServer.Azure.MI'; cs=$null;          pl=$null;        add=$null; AllOnly=$true }
)

# --- NewCliFeatures has its own 2-row mini-matrix (both SQLite, different options) --------------
$scaffoldTtPath = Join-Path $cliOutRoot 'scaffold.tt'
$newCliRows = @(
    # 'ns' is the namespace suffix from each .tt's namespaceName argument; defaults to 'key' otherwise.
    @{ key='SQLite';        ns='SQLite';       cs=$sqliteCN; extra="--context-modifier internal --add-static-init-context true --customize $scaffoldTtPath" }
    @{ key='SQLite.Fluent'; ns='FluentSQLite'; cs=$sqliteCN; extra='--metadata fluent --fluent-entity-type-helpers SpecificTypeHelper,AllTypesHelper'        }
)

# --- Build the invocation list ------------------------------------------------------------------
$jobs = @()
foreach ($tpl in $Templates) {
    if ($tpl -eq 'NewCliFeatures') {
        foreach ($r in $newCliRows) {
            if ($SkipProviders -contains $r.key) { continue }
            if ($Providers.Count -gt 0 -and -not ($Providers -contains $r.key)) { continue }
            $nsSuffix  = if ($r.ContainsKey('ns') -and $r.ns) { $r.ns } else { $r.key.Replace('.','_') }
            $namespace = "Cli.NewCliFeatures.$nsSuffix"
            $targetDir = Join-Path $cliOutRoot (Join-Path 'NewCliFeatures' $r.key)
            $cliArgs = @('scaffold','-o',$targetDir,'-p','SQLite','-c',$r.cs,'-t','default','--nrt','true','-n',$namespace,'--context-name','TestDataDB')
            $cliArgs += ($r.extra -split ' ' | Where-Object { $_ })
            $jobs += [pscustomobject]@{ Template='NewCliFeatures'; Key=$r.key; Provider='SQLite'; CnName=$null; TargetDir=$targetDir; CliArgs=$cliArgs }
        }
        continue
    }
    foreach ($row in $matrix) {
        if ($row.AllOnly -and $tpl -ne 'All') { continue }
        if ($SkipProviders -contains $row.key) { continue }
        if ($Providers.Count -gt 0 -and -not ($Providers -contains $row.key)) { continue }

        $cs        = if ($row.cs) { $row.cs } else { CN $row.cn }
        $nsSuffix  = if ($row.ContainsKey('ns') -and $row.ns) { $row.ns } else { $row.key }
        $namespace = "Cli.$tpl.$nsSuffix"
        $targetDir = Join-Path $cliOutRoot (Join-Path $tpl $row.key)
        $extra     = if ($row.ContainsKey('noExtras') -and $row.noExtras) { '' } else { GetExtraOptions $tpl $row.key }
        $mode      = GetMode $tpl

        $cliArgs = @(
            'scaffold','-o',$targetDir,'-p',$row.p,'-c',$cs,
            '-t',$mode,'--nrt','true','-n',$namespace,'--context-name','TestDataDB'
        )
        if ($row.pl) { $cliArgs += @('-l', $row.pl) }
        if ($row.add) { $cliArgs += @('--additional-connection', $row.add) }
        if ($extra)   { $cliArgs += ($extra -split ' ' | Where-Object { $_ }) }

        $jobs += [pscustomobject]@{
            Template = $tpl; Key=$row.key; Provider=$row.p; CnName=$row.cn
            TargetDir = $targetDir; CliArgs = $cliArgs
        }
    }
}

if (-not $OutputJson) {
    Write-Output ("Planned: {0} CLI invocations across {1} template(s) (RepoRoot: {2})" -f $jobs.Count, $Templates.Count, $RepoRoot)
}

if ($DryRun) {
    if ($OutputJson) {
        $jobs | ForEach-Object {
            [pscustomobject]@{ template=$_.Template; key=$_.Key; provider=$_.Provider; targetDir=$_.TargetDir; args=$_.CliArgs }
        } | ConvertTo-Json -Depth 4
    } else {
        foreach ($j in $jobs) {
            $shown = ($j.CliArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
            Write-Output ("[{0,-14} {1}]  dotnet {2} {3}" -f $j.Template, $j.Key, (Split-Path $CliDll -Leaf), $shown)
        }
    }
    return
}

# --- Run in parallel ----------------------------------------------------------------------------
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()
$results = $jobs | ForEach-Object -Parallel {
    $j = $_
    if (Test-Path $j.TargetDir) { Remove-Item -Recurse -Force $j.TargetDir }
    New-Item -ItemType Directory -Force -Path $j.TargetDir | Out-Null

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'dotnet'
    $psi.ArgumentList.Add($using:CliDll) | Out-Null
    foreach ($a in $j.CliArgs) { $psi.ArgumentList.Add($a) | Out-Null }
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError  = $true
    $psi.UseShellExecute        = $false
    $psi.CreateNoWindow         = $true

    $sw   = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    $exited = $proc.WaitForExit($using:TimeoutSec * 1000)
    if (-not $exited) { try { $proc.Kill() } catch {} }
    $sw.Stop()

    [pscustomobject]@{
        Template = $j.Template; Key = $j.Key; Provider = $j.Provider; CnName = $j.CnName
        TimedOut = -not $exited
        ExitCode = if ($exited) { $proc.ExitCode } else { -1 }
        Seconds  = [math]::Round($sw.Elapsed.TotalSeconds,1)
        Stdout   = $stdoutTask.Result
        Stderr   = $stderrTask.Result
    }
} -ThrottleLimit $Parallelism
$swTotal.Stop()

$pass = ($results | Where-Object { $_.ExitCode -eq 0 }).Count
$fail = $results.Count - $pass

if ($OutputJson) {
    [pscustomobject]@{
        ok = ($fail -eq 0)
        totalSeconds = [math]::Round($swTotal.Elapsed.TotalSeconds, 1)
        passed = $pass
        failed = $fail
        results = $results | ForEach-Object {
            [pscustomobject]@{
                template = $_.Template; key = $_.Key; provider = $_.Provider; connection = $_.CnName
                exitCode = $_.ExitCode; timedOut = $_.TimedOut; seconds = $_.Seconds
                stderr = ($_.Stderr -split "`r?`n" | Select-Object -First 30) -join "`n"
            }
        }
    } | ConvertTo-Json -Depth 5
    return
}

Write-Output ""
Write-Output ("Done in {0:n1}s — {1} ok, {2} failed" -f $swTotal.Elapsed.TotalSeconds, $pass, $fail)
Write-Output ""
$results | Sort-Object Template,Key | ForEach-Object {
    $tag = if ($_.TimedOut) { 'TIMEOUT' } elseif ($_.ExitCode -eq 0) { 'OK    ' } else { ('FAIL ' + $_.ExitCode) }
    Write-Output ("  [{0}] {1,-14} {2,-26} {3}s" -f $tag, $_.Template, $_.Key, $_.Seconds)
}

$failed = $results | Where-Object { $_.ExitCode -ne 0 }
if ($failed) {
    Write-Output ""
    Write-Output "=== Failures (first 30 lines stderr each) ==="
    foreach ($f in $failed) {
        Write-Output ""
        Write-Output ("--- {0} / {1} ({2}, exit {3}) ---" -f $f.Template, $f.Key, $f.Provider, $f.ExitCode)
        $err = if ($f.Stderr) { $f.Stderr } else { $f.Stdout }
        ($err -split "`r?`n" | Select-Object -First 30) | ForEach-Object { Write-Output ("  " + $_) }
    }
}
