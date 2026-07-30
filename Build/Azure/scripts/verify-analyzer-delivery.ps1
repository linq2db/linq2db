<#
verify-analyzer-delivery.ps1 — assert the packed nupkgs still deliver the linq2db.Analyzers rules.

linq2db ships its user-facing Roslyn rules as a package dependency: `linq2db` depends on
`linq2db.Analyzers`, and every satellite package leaves `analyzers` out of PrivateAssets on its own
`linq2db` / `linq2db.Tools` project reference so the analyzer subtree keeps flowing to that package's
consumers. That invariant lives in a dozen separate ProjectReference lines. A new satellite project
added with default metadata, or a later cleanup back to the default, cuts delivery at that hop.

Nothing at build time notices, and neither does a consumer smoke test: the .NET SDK currently hands
analyzers to csc even across a dependency edge that excludes them, so the rules keep arriving until
that behaviour changes — and only then does the mistake surface, potentially releases later. This
script asserts the shipped artifact rather than the mechanism: it reads the produced nuspecs, so it
stays valid across SDK / MSBuild changes and has no private build target to silently stop firing.

Checks:
  1. linq2db.nuspec declares a linq2db.Analyzers dependency in every dependency group.
  2. No package excludes 'Analyzers' on its linq2db / linq2db.Tools / linq2db.Analyzers dependency.
  3. The linq2db.Analyzers package carries both analyzer assemblies under analyzers/**/cs.

Check 2 skips the ids in $NoFlowRequired — packages nobody compiles against, so analyzer flow is
meaningless for them: linq2db.cli (a dotnet tool) and linq2db.LINQPad (a LINQPad driver). Their
project references intentionally keep NuGet's default metadata; the skip list is what records that.

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/verify-analyzer-delivery.ps1 -PackagesDir <dir>

  -PackagesDir   directory to scan recursively for *.nupkg (required)
  -NoAzdoLogs    suppress the Azure DevOps `##vso[task.logissue]` lines, which are on by default
                 (the script's primary caller is the AzDO publish pipeline). Use it for local
                 invocation. A switch rather than a [bool], because `pwsh -File` passes every
                 argument as a string and a [bool] parameter rejects strings outright.

Exit codes:
  0  every check passed
  1  a check failed — release-blocking; build should fail
  2  invalid args, no nupkgs found, or linq2db / linq2db.Analyzers absent from the drop
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDir,
    [switch] $NoAzdoLogs
)

$ErrorActionPreference = 'Stop'

$analyzerPackageId = 'linq2db.Analyzers'
$carrierPackageIds = @('linq2db', 'linq2db.Tools', 'linq2db.Analyzers')
$noFlowRequired    = @('linq2db.cli', 'linq2db.LINQPad')

function Get-GroupLabel {
    param([string] $TargetFramework)

    if ([string]::IsNullOrWhiteSpace($TargetFramework)) { return '(no targetFramework)' }
    return $TargetFramework
}

function Read-NupkgMetadata {
    param([string] $NupkgPath)

    $zip = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })

        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } | Select-Object -First 1
        if (-not $nuspecEntry) { return $null }

        $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
        try   { $xml = [xml] $reader.ReadToEnd() }
        finally { $reader.Dispose() }

        $idNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
        if (-not $idNode) { return $null }

        # nuspec carries either <dependencies><group targetFramework=..>..</group></dependencies> or a
        # flat <dependencies><dependency/></dependencies>; normalise both to a list of groups.
        $groups        = @()
        $dependenciesNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='dependencies']")
        if ($dependenciesNode) {
            $groupNodes = $dependenciesNode.SelectNodes("*[local-name()='group']")
            $hosts      = if ($groupNodes.Count -gt 0) { @($groupNodes) } else { @($dependenciesNode) }
            foreach ($h in $hosts) {
                $groups += [pscustomobject]@{
                    tfm  = $h.GetAttribute('targetFramework')
                    deps = @($h.SelectNodes("*[local-name()='dependency']") | ForEach-Object {
                        [pscustomobject]@{ id = $_.GetAttribute('id'); exclude = $_.GetAttribute('exclude') }
                    })
                }
            }
        }

        return [pscustomobject]@{
            id      = $idNode.InnerText
            groups  = $groups
            entries = $entries
            file    = [System.IO.Path]::GetFileName($NupkgPath)
        }
    }
    finally { $zip.Dispose() }
}

if (-not (Test-Path $PackagesDir)) {
    [Console]::Error.WriteLine("PackagesDir not found: $PackagesDir")
    exit 2
}

$pkgs = Get-ChildItem -Path $PackagesDir -Recurse -Filter '*.nupkg' -File
if (-not $pkgs -or $pkgs.Count -eq 0) {
    [Console]::Error.WriteLine("No .nupkg files found under: $PackagesDir")
    exit 2
}

$violations = @()
$parsed     = @()

foreach ($p in $pkgs) {
    $meta = Read-NupkgMetadata -NupkgPath $p.FullName
    if (-not $meta) {
        $violations += ('{0}: could not read a package id out of the nuspec' -f $p.Name)
        continue
    }
    $parsed += $meta
}

$linq2db     = $parsed | Where-Object { $_.id -eq 'linq2db' }          | Select-Object -First 1
$analyzerPkg = $parsed | Where-Object { $_.id -eq $analyzerPackageId } | Select-Object -First 1

if (-not $linq2db) {
    [Console]::Error.WriteLine("No linq2db package found under $PackagesDir — cannot verify analyzer delivery")
    exit 2
}
if (-not $analyzerPkg) {
    [Console]::Error.WriteLine("No $analyzerPackageId package found under $PackagesDir — cannot verify analyzer delivery")
    exit 2
}

# 1. linq2db must depend on the analyzer package on every framework it ships for.
if ($linq2db.groups.Count -eq 0) {
    $violations += ('linq2db: nuspec declares no dependencies at all — the {0} edge is gone, so the rules reach nobody' -f $analyzerPackageId)
}
else {
    foreach ($g in $linq2db.groups) {
        if (-not ($g.deps | Where-Object { $_.id -eq $analyzerPackageId })) {
            $violations += ('linq2db: dependency group {0} has no {1} dependency — consumers on that framework get no rules' -f (Get-GroupLabel $g.tfm), $analyzerPackageId)
        }
    }
}

# 2. No hop may exclude Analyzers: an intermediate edge that does suppresses the whole subtree.
$verified = @()
foreach ($p in $parsed) {
    if ($noFlowRequired -contains $p.id) { continue }

    foreach ($g in $p.groups) {
        foreach ($d in $g.deps) {
            if ($carrierPackageIds -notcontains $d.id) { continue }

            $excluded = @($d.exclude -split ',' | Where-Object { $_.Trim() -ieq 'Analyzers' })
            if ($excluded.Count -gt 0) {
                $violations += ('{0}: dependency {1} in group {2} has exclude={3} — that suppresses the whole {4} subtree for this package''s consumers; drop analyzers from PrivateAssets on the corresponding ProjectReference' -f $p.id, $d.id, (Get-GroupLabel $g.tfm), $d.exclude, $analyzerPackageId)
            }
            else {
                $verified += ('{0} -> {1}' -f $p.id, $d.id)
            }
        }
    }
}

# 3. The analyzer package must actually carry the assemblies (an empty analyzers/ folder packs fine).
foreach ($assembly in @('LinqToDB.Analyzers.dll', 'LinqToDB.Analyzers.CodeFixes.dll')) {
    $pattern = '^analyzers/.*/cs/' + [regex]::Escape($assembly) + '$'
    if (-not ($analyzerPkg.entries | Where-Object { $_ -match $pattern })) {
        $violations += ('{0}: no analyzers/**/cs/{1} entry — the package ships without that assembly, so its rules never load' -f $analyzerPackageId, $assembly)
    }
}

Write-Output ('Scanned {0} nupkg(s) for analyzer delivery. Violations: {1}' -f $pkgs.Count, $violations.Count)
Write-Output ''

foreach ($v in $violations) {
    Write-Output "  [FAIL]  $v"
    if (-not $NoAzdoLogs) {
        Write-Output ('##vso[task.logissue type=error]{0}' -f $v)
    }
}

if ($violations.Count -eq 0) {
    Write-Output 'Analyzer-flowing dependency edges:'
    $verified | Sort-Object -Unique | ForEach-Object { Write-Output "  $_" }
    Write-Output ''
    Write-Output ('Not required to flow analyzers (nothing compiles against them): {0}' -f ($noFlowRequired -join ', '))
    exit 0
}

exit 1
