#!/usr/bin/env pwsh
<#
verify-analyzer-consumption.ps1 - prove, from real packages, that the linq2db analyzer rules reach a
consumer, that the documented opt-outs work, and that no analyzer assembly reaches runtime output.

Why a script
------------
`Build/Azure/scripts/verify-analyzer-delivery.ps1` (the CI gate) inspects the *packed nuspecs* - it can
prove the dependency edges and package layout are right, but not that a consumer's `csc` actually gets
the rules, nor that an opt-out works. Only a real pack + restore + build can. That harness has been
rebuilt by hand from scratch in three separate sessions (#5720 twice), each time re-deriving the same
non-obvious scaffolding, so it lives here now.

`/dogfood-analyzer` is a different thing: it validates a *rule's behaviour* (report mode / code-fix mode)
over the real test corpus. This validates *packaging and configuration*.

What it asserts (default matrix)
--------------------------------
  direct        PackageReference linq2db                     -> rule fires        (direct delivery)
  direct-off    + EnableLinqToDBAnalyzers=false              -> silent, build OK  (MSBuild opt-out)
  tools         PackageReference linq2db.Tools only           -> rule fires        (transitive delivery)
  tools-off     + EnableLinqToDBAnalyzers=false              -> silent, build OK  (opt-out two hops out,
                                                                 i.e. buildTransitive really flows)
  by-category   .editorconfig sets ONLY the category severity -> rule fires        (declared category is
                                                                 what the readmes document)

Every case also asserts no `LinqToDB.Analyzers*` assembly lands in the consumer's `bin/` - analyzer
assemblies must never reach an application's runtime output.

Scaffolding that is easy to get wrong (each of these cost a run)
---------------------------------------------------------------
- **`-p:EnablePackageValidation=false` on the pack.** Mid-cycle the branch's API surface does not match
  the last shipped baseline, so `dotnet pack` otherwise dies on `[Baseline]` CP0011/CP0012/CP0021 errors
  that have nothing to do with what is being measured. (Reconciling those is the release flow's job.)
- **Empty `Directory.Build.props` / `.targets` in the consumer dir**, plus a `Directory.Packages.props`
  that sets `ManagePackageVersionsCentrally=false` - otherwise the repo root's MSBuild state and central
  package management leak into the throwaway consumer and it fails on NU1008 instead of building.
- **`packageSourceMapping` with `<clear/>`** in the consumer `nuget.config`. The repo root maps `*` to
  nuget.org, so without the clear the local folder feed is "not considered" and restore can't find the
  freshly packed packages at all.
- **An isolated `globalPackagesFolder`.** Re-packing the same version otherwise serves the stale cached
  extraction from `~/.nuget/packages` and the run measures the previous build.

Usage:

  pwsh -NoProfile -File .agents/scripts/verify-analyzer-consumption.ps1 -Root <repo-or-worktree>

  -Root       repo root / worktree to pack out of (required)
  -Work       scratch dir; default .build/.agents/analyzer-consumption
  -Version    package version to pack and reference; default 6.4.0-verify<random>
  -RuleId     diagnostic id expected to fire; default L2DB1001
  -Category   analyzer category for the by-category case; default LinqToDB
  -Trigger    C# body line that must raise the rule; default is the legacy Sql.Ext window chain
  -KeepWork   don't delete the scratch dir at the end (default: kept, it holds the per-case build logs)

Output (stdout, JSON): { version, feed, cases: [ { name, exit, fired, expected, binLeak, log } ],
                         failures: [ ... ], passed: bool }

Exit codes:
  0  every case behaved as expected
  1  a case failed, or a pack / scaffolding step failed
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $Root,
    [string] $Work     = '.build/.agents/analyzer-consumption',
    [string] $Version,
    [string] $RuleId   = 'L2DB1001',
    [string] $Category = 'LinqToDB',
    [string] $Trigger  = 'long M(int x) => Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue();',
    [switch] $KeepWork
)

$global:ScriptBaseName = 'verify-analyzer-consumption'
. "$PSScriptRoot/_shared.ps1"

if (-not (Test-Path -LiteralPath $Root)) {
    Exit-WithError "root not found: $Root" -NextAction 'pass -Root pointing at the repo or worktree to pack out of'
}
if (-not $Version) { $Version = '6.4.0-verify{0}' -f (Get-Random -Maximum 9999) }

$Root = (Resolve-Path -LiteralPath $Root).Path
$feed = Join-Path $Work 'feed'
$gpf  = Join-Path $Work 'gpf'

if (Test-Path -LiteralPath $Work) { Remove-Item -Recurse -Force -LiteralPath $Work }
New-Item -ItemType Directory -Force -Path $feed, $gpf | Out-Null
$feed = (Resolve-Path -LiteralPath $feed).Path
$gpf  = (Resolve-Path -LiteralPath $gpf).Path

# --- pack ------------------------------------------------------------------------------------------

$projects = @(
    'Source/LinqToDB.Analyzers.CodeFixes/LinqToDB.Analyzers.CodeFixes.csproj'
    'Source/LinqToDB/LinqToDB.csproj'
    'Source/LinqToDB.Tools/LinqToDB.Tools.csproj'
)

foreach ($rel in $projects) {
    $proj = Join-Path $Root $rel
    if (-not (Test-Path -LiteralPath $proj)) { Exit-WithError "project not found: $proj" }

    $log = & dotnet pack $proj -c Testing -p:PackageVersion=$Version -p:EnablePackageValidation=false -o $feed -v:m 2>&1
    if ($LASTEXITCODE -ne 0) {
        $logPath = Join-Path $Work ('pack-{0}.log' -f [System.IO.Path]::GetFileNameWithoutExtension($proj))
        Set-Content -LiteralPath $logPath -Value ($log | Out-String) -Encoding UTF8
        Exit-WithError "pack failed: $rel" -NextAction "read $logPath"
    }
}

# --- consumer scaffolding --------------------------------------------------------------------------

$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
	<config>
		<add key="globalPackagesFolder" value="$gpf" />
	</config>
	<packageSources>
		<clear />
		<add key="local-verify" value="$feed" />
		<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
	</packageSources>
	<packageSourceMapping>
		<clear />
		<packageSource key="local-verify">
			<package pattern="linq2db*" />
		</packageSource>
		<packageSource key="nuget.org">
			<package pattern="*" />
		</packageSource>
	</packageSourceMapping>
</configuration>
"@

$editorConfigByRule     = "root = true`n`n[*.cs]`ndotnet_diagnostic.$RuleId.severity = error`n"
$editorConfigByCategory = "root = true`n`n[*.cs]`ndotnet_analyzer_diagnostic.category-$Category.severity = error`n"

$source = "using LinqToDB;`n`nclass C`n{`n`t$Trigger`n}`n"

function New-Consumer {
    param([string] $Name, [string] $PackageId, [bool] $OptOut, [string] $EditorConfig)

    $dir = Join-Path $Work $Name
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    $optOutLine = if ($OptOut) { "`t`t<EnableLinqToDBAnalyzers>false</EnableLinqToDBAnalyzers>`n" } else { '' }

    $csproj = @"
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<Nullable>disable</Nullable>
$optOutLine	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="$PackageId" Version="$Version" />
	</ItemGroup>

</Project>
"@

    $blockers = @'
<Project>
	<PropertyGroup>
		<ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
	</PropertyGroup>
</Project>
'@

    Set-Content -LiteralPath (Join-Path $dir "$Name.csproj")             -Value $csproj       -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir 'nuget.config')             -Value $nugetConfig  -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir '.editorconfig')            -Value $EditorConfig -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir 'Query.cs')                 -Value $source       -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir 'Directory.Build.props')     -Value '<Project />' -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir 'Directory.Build.targets')   -Value '<Project />' -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $dir 'Directory.Packages.props')  -Value $blockers     -Encoding UTF8

    return $dir
}

# --- run the matrix --------------------------------------------------------------------------------

$cases = @(
    @{ name = 'direct';      package = 'linq2db';       optOut = $false; expect = $true;  editorConfig = $editorConfigByRule }
    @{ name = 'direct-off';  package = 'linq2db';       optOut = $true;  expect = $false; editorConfig = $editorConfigByRule }
    @{ name = 'tools';       package = 'linq2db.Tools'; optOut = $false; expect = $true;  editorConfig = $editorConfigByRule }
    @{ name = 'tools-off';   package = 'linq2db.Tools'; optOut = $true;  expect = $false; editorConfig = $editorConfigByRule }
    @{ name = 'by-category'; package = 'linq2db';       optOut = $false; expect = $true;  editorConfig = $editorConfigByCategory }
)

$failures = @()
$results  = @()

foreach ($c in $cases) {
    $dir = New-Consumer -Name $c.name -PackageId $c.package -OptOut $c.optOut -EditorConfig $c.editorConfig

    $out     = & dotnet build $dir -c Release -v:m 2>&1
    $code    = $LASTEXITCODE
    $text    = ($out | Out-String)
    $logPath = Join-Path $Work ("{0}.log" -f $c.name)
    Set-Content -LiteralPath $logPath -Value $text -Encoding UTF8

    $fired = [bool]($text -match [regex]::Escape($RuleId))

    if ($fired -ne $c.expect) {
        $failures += ("{0}: expected {1} {2}, got {3} (exit {4}) - see {5}" -f $c.name, $RuleId, $c.expect, $fired, $code, $logPath)
    }
    if (-not $c.expect -and $code -ne 0) {
        $failures += ("{0}: build failed (exit {1}) though no diagnostic was expected - see {2}" -f $c.name, $code, $logPath)
    }

    $leaked = @(Get-ChildItem -LiteralPath $dir -Recurse -Filter 'LinqToDB.Analyzers*' -File -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '[\\/]bin[\\/]' })
    if ($leaked.Count -gt 0) {
        $failures += ("{0}: analyzer assemblies reached bin/: {1}" -f $c.name, (($leaked | ForEach-Object { $_.Name }) -join ', '))
    }

    $results += [pscustomobject]@{
        name     = $c.name
        exit     = $code
        fired    = $fired
        expected = $c.expect
        binLeak  = $leaked.Count
        log      = $logPath
    }
}

if (-not $KeepWork) {
    Remove-Item -Recurse -Force -LiteralPath $feed, $gpf -ErrorAction SilentlyContinue
}

Write-JsonOutput ([pscustomobject]@{
    version  = $Version
    feed     = $feed
    cases    = $results
    failures = $failures
    passed   = ($failures.Count -eq 0)
})

if ($failures.Count -eq 0) { exit 0 }
exit 1
