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
  4. The linq2db.Analyzers package carries the EnableLinqToDBAnalyzers opt-out targets in build/ and
     buildTransitive/ — the readme documents the property, and only the packed file implements it.
  5. Every shipped rule id appears in the packed readme of both linq2db and linq2db.Analyzers. The two
     diagnostics tables are duplicates maintained by hand and nothing else compares them to anything, so
     a new rule can ship fully green and undocumented. The id list comes from AnalyzerReleases.*.md,
     which RS2000/RS2001 already tie to the DiagnosticDescriptors — so the chain is
     descriptor -> release-tracking file -> both readmes, with a build gate on each hop.

Check 2 skips the ids in $NoFlowRequired — packages nobody compiles against, so analyzer flow is
meaningless for them: linq2db.cli (a dotnet tool) and linq2db.LINQPad (a LINQPad driver). Their
project references intentionally keep NuGet's default metadata; the skip list is what records that.

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/verify-analyzer-delivery.ps1 -PackagesDir <dir>

  -PackagesDir   directory to scan recursively for *.nupkg (required)
  -RepoRoot      repository root, used by check 5 to read the AnalyzerReleases.*.md rule ids. Defaults
                 to this script's own location walked up three levels; pass it explicitly when running
                 the script from a copy, or the default repoints and the run exits 2 rather than 1.
  -NoAzdoLogs    suppress the Azure DevOps `##vso[task.logissue]` lines, which are on by default
                 (the script's primary caller is the AzDO publish pipeline). Use it for local
                 invocation. A switch rather than a [bool], because `pwsh -File` passes every
                 argument as a string and a [bool] parameter rejects strings outright.

Exit codes:
  0  every check passed
  1  a check failed — release-blocking; build should fail
  2  invalid args, no nupkgs found, linq2db / linq2db.Analyzers absent from the drop, or the
     AnalyzerReleases.*.md inputs for check 5 could not be read
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDir,
    [string] $RepoRoot = (Join-Path $PSScriptRoot '..' '..' '..'),
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

        # The readme is what check 5 reads. Prefer the path the nuspec declares; fall back to a root-level
        # readme.md so a package that ships one without declaring it is still checked rather than skipped.
        $readme     = $null
        $readmeNode = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='readme']")
        $readmePath = if ($readmeNode) { $readmeNode.InnerText.Replace('\', '/') } else { 'readme.md' }

        $readmeEntry = $zip.Entries | Where-Object { $_.FullName -ieq $readmePath } | Select-Object -First 1
        if ($readmeEntry) {
            $readmeReader = New-Object System.IO.StreamReader($readmeEntry.Open())
            try   { $readme = $readmeReader.ReadToEnd() }
            finally { $readmeReader.Dispose() }
        }

        return [pscustomobject]@{
            id         = $idNode.InnerText
            groups     = $groups
            entries    = $entries
            readme     = $readme
            readmePath = $readmePath
            file       = [System.IO.Path]::GetFileName($NupkgPath)
        }
    }
    finally { $zip.Dispose() }
}

function Get-ShippedRuleIds {
    param([string] $AnalyzerProjectDir)

    $ids = @()

    foreach ($name in @('AnalyzerReleases.Shipped.md', 'AnalyzerReleases.Unshipped.md')) {
        $path = Join-Path $AnalyzerProjectDir $name
        if (-not (Test-Path $path)) { return $null }

        # Release-tracking table rows are `<id> | <category> | <severity> | <notes>`; the header and the
        # `;`-prefixed preamble never match, so no row filtering beyond the id shape is needed.
        foreach ($line in Get-Content -LiteralPath $path) {
            if ($line -match '^\s*(L2DB\d+)\s*\|') { $ids += $Matches[1] }
        }
    }

    return @($ids | Sort-Object -Unique)
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

# 4. The opt-out documented in both readmes is implemented solely by the packed targets file: build/ for
#    a direct reference to this package, buildTransitive/ for the normal path through linq2db. Losing
#    either folder turns EnableLinqToDBAnalyzers into a property that silently does nothing.
foreach ($folder in @('build', 'buildTransitive')) {
    $entry = '{0}/linq2db.Analyzers.targets' -f $folder
    if ($analyzerPkg.entries -notcontains $entry) {
        $violations += ('{0}: no {1} entry — the documented EnableLinqToDBAnalyzers opt-out is not in the package' -f $analyzerPackageId, $entry)
    }
}

# 5. Both packages' readmes must document every shipped rule. Nothing else compares those two tables to
#    anything, so an undocumented rule ships green; the ids come from the release-tracking files, which
#    RS2000/RS2001 already hold against the descriptors.
$analyzerProjectDir = Join-Path $RepoRoot 'Source/LinqToDB.Analyzers'
$ruleIds            = Get-ShippedRuleIds -AnalyzerProjectDir $analyzerProjectDir

if ($null -eq $ruleIds) {
    [Console]::Error.WriteLine("AnalyzerReleases.*.md not found under $analyzerProjectDir — pass -RepoRoot explicitly when running from a copy of this script")
    exit 2
}

foreach ($pkg in @($linq2db, $analyzerPkg)) {
    if ($null -eq $pkg.readme) {
        $violations += ('{0}: no {1} entry in the package — the diagnostics table ships with nothing in it' -f $pkg.id, $pkg.readmePath)
        continue
    }

    foreach ($ruleId in $ruleIds) {
        if ($pkg.readme -notmatch [regex]::Escape($ruleId)) {
            $violations += ('{0}: {1} is missing from the packed {2} — the rule ships undocumented for this package''s consumers' -f $pkg.id, $ruleId, $pkg.readmePath)
        }
    }
}

Write-Output ('Scanned {0} nupkg(s) for analyzer delivery, against {1} shipped rule id(s): {2}. Violations: {3}' -f $pkgs.Count, $ruleIds.Count, ($ruleIds -join ', '), $violations.Count)
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
