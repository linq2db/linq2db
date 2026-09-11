<#
third-party-notices.ps1 — generate, and enforce, the third-party license notices that ship inside
every linq2db artifact which physically redistributes a binary linq2db does not own.

Most linq2db packages declare their dependencies and ship only their own assemblies; NuGet then hands
the consumer each dependency under that dependency's own license, and there is nothing to disclose.
Three artifact families are different, because the foreign binary is *inside* the artifact:

  * linq2db.cli       - PackAsTool + ToolPackageRuntimeIdentifiers, so `dotnet pack` publishes the whole
                        dependency closure into tools/<tfm>/<rid>/. That closure is 90 NuGet packages
                        and 158 assemblies per RID, against the 22 PackageReference lines in the csproj.
  * the 14 T4 packages- NuGet/*/linq2db.*.csproj hand-list provider clients into tools/, including the
                        SQL CE runtime and the SAP HANA client out of Redist/, and IBM's 223-file
                        clidriver tree.
  * linq2db.LINQPad.lpx - Pack.cmd archives the entire net472 output directory (7z -r a "%OUTDIR%*.*"),
                        so every driver and native asset in that folder ships in the LINQPad 5 plugin.

All of them declare PackageLicenseExpression=MIT, which is true of linq2db's own code and silent about
the rest. This script is what closes that gap and keeps it closed. See linq2db/linq2db#5731.

Actions
-------

  -Action generate   Render Build/licenses/generated/THIRD-PARTY-NOTICES.<artifact>.txt from
                     Build/licenses/components.json + Build/licenses/texts/. The generated files are
                     tracked in git so the exact text that ships is reviewable in the PR that changes
                     it - the same reasoning that keeps CompatibilitySuppressions.xml checked in.

  -Action check      Re-render to a temp directory and compare bytes against the tracked files. Fails
                     on a hand-edit or on a manifest edit that was never regenerated.

  -Action verify     Open the produced artifacts (-PackagesDir / -LpxDir / -PublishDir), enumerate the
                     binaries they actually contain, and assert that every one maps to a component
                     scoped to that artifact, that the notices file is present, and that packages which
                     are supposed to bundle nothing bundle nothing. This reads the *shipped artifact*
                     rather than the csproj on purpose: the csproj cannot tell you what a transitive
                     dependency dragged in, which is the case the gate exists for.

  -Action harvest    Read restore graphs (project.assets.json) and/or a published deps.json, look each
                     package up in the local NuGet cache for its license metadata, and print what would
                     change in components.json. Author-side only: it proposes, it never writes the
                     manifest. Run after a dependency bump - /release-deps step 5b does this.

Determinism
-----------

`check` compares bytes, and it runs on a developer box and on windows-2025. Three things therefore
matter and are deliberate, not incidental:

  * ordinal sorting everywhere (PowerShell's Sort-Object is culture-sensitive by default),
  * "`n" line endings written unconditionally - .gitattributes pins Build/licenses/** to eol=lf so the
    working tree matches the repository on every platform,
  * UTF-8 with no BOM.

Usage
-----

  pwsh -NoProfile -File Build/Azure/scripts/third-party-notices.ps1 -Action generate
  pwsh -NoProfile -File Build/Azure/scripts/third-party-notices.ps1 -Action check
  pwsh -NoProfile -File Build/Azure/scripts/third-party-notices.ps1 -Action verify -PackagesDir .build/package/release -LpxDir .build/lpx
  pwsh -NoProfile -File Build/Azure/scripts/third-party-notices.ps1 -Action harvest -AssetsFile .build/obj/LinqToDB.CLI/project.assets.json

  -RepoRoot      repository root; defaults to the directory three levels above this script
  -PackagesDir   directory scanned recursively for *.nupkg (verify)
  -LpxDir        directory scanned for *.lpx (verify)
  -PublishDir    one or more `dotnet publish` output directories to check as a tool payload (verify)
  -AssetsFile    one or more project.assets.json / deps.json paths (harvest)
  -NoAzdoLogs    suppress the Azure DevOps `##vso[task.logissue]` lines, which are on by default. A
                 switch rather than a [bool], because `pwsh -File` passes every argument as a string
                 and a [bool] parameter rejects strings outright.

Exit codes
----------

  0  clean
  1  a violation - drift, an unmapped binary, a missing notices file, a bundled binary in a package
     that must not have one. Release-blocking; the build should fail.
  2  invalid args, missing manifest, or an artifact that could not be enumerated at all (which would
     otherwise pass vacuously - see Test-ArtifactSanity).
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('harvest', 'generate', 'check', 'verify')]
    [string]   $Action,
    [string]   $RepoRoot,
    [string]   $PackagesDir,
    [string]   $LpxDir,
    [string[]] $PublishDir,
    [string[]] $AssetsFile,
    [switch]   $NoAzdoLogs
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

# --------------------------------------------------------------------------------------------------
# Paths
# --------------------------------------------------------------------------------------------------

if (-not $RepoRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}
if (-not (Test-Path -LiteralPath $RepoRoot)) {
    [Console]::Error.WriteLine("RepoRoot not found: $RepoRoot")
    exit 2
}

$licensesDir  = Join-Path $RepoRoot 'Build\licenses'
$manifestPath = Join-Path $licensesDir 'components.json'
$textsDir     = Join-Path $licensesDir 'texts'
$generatedDir = Join-Path $licensesDir 'generated'

$script:violations = [System.Collections.Generic.List[string]]::new()
$script:notes      = [System.Collections.Generic.List[string]]::new()

function Add-Violation([string] $Message) { $script:violations.Add($Message) | Out-Null }
function Add-Note([string] $Message)      { $script:notes.Add($Message)      | Out-Null }

# Ordinal sort. Sort-Object is culture-sensitive and would make `check` machine-dependent.
#
# The leading comma on every `return` is load-bearing: PowerShell unrolls a one-element array on return,
# so without it a single-item result comes back as a bare string and `.Count` on it throws under
# Set-StrictMode. That is not hypothetical here - most components resolve to exactly one version.
function Sort-Ordinal {
    param([string[]] $Values)
    if (-not $Values -or $Values.Count -eq 0) { return , @() }
    $copy = [string[]]::new($Values.Count)
    [Array]::Copy($Values, $copy, $Values.Count)
    [Array]::Sort($copy, [System.StringComparer]::Ordinal)
    return , $copy
}

function Write-TextFile {
    param([string] $Path, [string] $Content)
    $dir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    # Content is assembled with "`n" throughout; WriteAllText does not translate it.
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Read-TextFile {
    param([string] $Path)
    # Normalise on read so a stray CRLF in a hand-added license text cannot make the output
    # platform-dependent. The .gitattributes pin is the primary guard; this is belt and braces.
    return ([System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n")
}

# --------------------------------------------------------------------------------------------------
# Manifest
# --------------------------------------------------------------------------------------------------

function Get-Manifest {
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        [Console]::Error.WriteLine("Manifest not found: $manifestPath")
        exit 2
    }
    $m = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

    foreach ($required in @('firstPartyAssemblies', 'ignore', 'artifacts', 'components')) {
        if (-not ($m.PSObject.Properties.Name -contains $required)) {
            [Console]::Error.WriteLine("Manifest is missing the '$required' property: $manifestPath")
            exit 2
        }
    }
    return $m
}

function Get-Artifact {
    param($Manifest, [string] $Key)
    return $Manifest.artifacts | Where-Object { $_.key -eq $Key } | Select-Object -First 1
}

function Get-ComponentsForArtifact {
    param($Manifest, [string] $Key)
    $hits = @($Manifest.components | Where-Object { $_.artifacts -contains $Key })
    $ids  = Sort-Ordinal ([string[]]($hits | ForEach-Object { $_.id }))
    return , @($ids | ForEach-Object { $id = $_; $hits | Where-Object { $_.id -eq $id } | Select-Object -First 1 })
}

# Version rendering. `versions` is a TFM -> version map, because the CLI resolves several packages to a
# different version per target framework (Microsoft.Extensions.Hosting is 8.0.0 / 9.0.0 / 10.0.0), and a
# scalar would be a lie on two of the three payloads inside every RID package.
function Format-ComponentVersions {
    param($Component)
    $names = Sort-Ordinal ([string[]]($Component.versions.PSObject.Properties.Name))
    $vals  = [System.Collections.Generic.List[string]]::new()
    foreach ($n in $names) { $vals.Add($Component.versions.$n) | Out-Null }
    $distinct = Sort-Ordinal ([string[]]($vals | Select-Object -Unique))
    if ($distinct.Count -eq 1) { return $distinct[0] }
    return (($names | ForEach-Object { "$_`: $($Component.versions.$_)" }) -join ', ')
}

# --------------------------------------------------------------------------------------------------
# generate
# --------------------------------------------------------------------------------------------------

$separator = ('-' * 98)

function New-NoticesText {
    param($Manifest, $Artifact)

    $components = Get-ComponentsForArtifact -Manifest $Manifest -Key $Artifact.key

    $sb = [System.Text.StringBuilder]::new()
    $null = $sb.Append("$($Artifact.title) - THIRD-PARTY NOTICES`n")
    $null = $sb.Append(('=' * "$($Artifact.title) - THIRD-PARTY NOTICES".Length))
    $null = $sb.Append("`n`n")
    $null = $sb.Append("GENERATED FILE - DO NOT EDIT.`n")
    $null = $sb.Append("Source:     Build/licenses/components.json and Build/licenses/texts/`n")
    $null = $sb.Append("Regenerate: pwsh -NoProfile -File Build/Azure/scripts/third-party-notices.ps1 -Action generate`n")
    $null = $sb.Append("`n")
    $null = $sb.Append("The LINQ to DB code in this artifact is licensed under the MIT license, which is what the`n")
    $null = $sb.Append("package license expression refers to. The components listed below are third-party software`n")
    $null = $sb.Append("redistributed inside this artifact; they are NOT covered by that MIT license and remain`n")
    $null = $sb.Append("subject to their own terms, reproduced in full below.`n")
    $null = $sb.Append("`n")
    $null = $sb.Append("Artifact:   $($Artifact.title)`n")
    $null = $sb.Append("Components: $($components.Count)`n")

    # Texts are emitted once each, in a section of their own, and referenced by number. Without this the
    # same 76 KB .NET runtime notice is repeated per Microsoft component and the CLI's notices file comes
    # out at 4 MB - which is not a size problem so much as an unreadable one.
    $textOrder = [System.Collections.Generic.List[string]]::new()
    foreach ($c in $components) {
        foreach ($t in @($c.licenseTexts)) {
            if (-not $t) { continue }
            if (-not $textOrder.Contains($t)) { $textOrder.Add($t) | Out-Null }
        }
    }

    $null = $sb.Append("`n$separator`n")
    $null = $sb.Append("COMPONENTS`n")
    $null = $sb.Append("$separator`n")

    $index = 0
    foreach ($c in $components) {
        $index++
        $null = $sb.Append("`n$index. $($c.displayName) $(Format-ComponentVersions -Component $c)`n")
        if ($c.packageId -and $c.packageId -ne $c.displayName) {
            $null = $sb.Append("   NuGet package: $($c.packageId)`n")
        }
        if ($c.projectUrl) { $null = $sb.Append("   $($c.projectUrl)`n") }

        $refs = @(@($c.licenseTexts) | Where-Object { $_ } | ForEach-Object { '[' + ($textOrder.IndexOf($_) + 1) + ']' })
        if ($refs.Count -gt 0) { $null = $sb.Append("   License: $($c.license)  see $($refs -join ' ')`n") }
        else                   { $null = $sb.Append("   License: $($c.license)`n") }

        if ($c.copyright) { $null = $sb.Append("   $($c.copyright)`n") }
        if ($c.redistribution -ne 'permitted') {
            $null = $sb.Append("   Redistribution: $($c.redistribution)`n")
        }
        if (($c.PSObject.Properties.Name -contains 'notes') -and $c.notes) {
            $null = $sb.Append("   $($c.notes)`n")
        }
    }

    if ($textOrder.Count -gt 0) {
        $null = $sb.Append("`n$separator`n")
        $null = $sb.Append("LICENSE AND NOTICE TEXTS`n")
        $null = $sb.Append("$separator`n")

        $n = 0
        foreach ($t in $textOrder) {
            $n++
            $users = @($components | Where-Object { @($_.licenseTexts) -contains $t } | ForEach-Object { $_.displayName })
            $textPath = Join-Path $textsDir $t
            if (-not (Test-Path -LiteralPath $textPath)) {
                Add-Violation ("texts/{0} is referenced by {1} but does not exist" -f $t, ($users -join ', '))
                continue
            }
            $null = $sb.Append("`n[$n] $t`n")
            $null = $sb.Append("    Applies to: $((Sort-Ordinal ([string[]]$users)) -join ', ')`n")
            $null = $sb.Append("`n")
            $null = $sb.Append(((Read-TextFile -Path $textPath).TrimEnd("`n")) + "`n")
            $null = $sb.Append("`n$separator`n")
        }
    }
    else {
        $null = $sb.Append("`n$separator`n")
    }

    return $sb.ToString()
}

function Invoke-Generate {
    param([string] $TargetDir)

    $manifest = Get-Manifest
    if (-not (Test-Path -LiteralPath $TargetDir)) { New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null }

    $written = [System.Collections.Generic.List[string]]::new()
    foreach ($a in $manifest.artifacts) {
        $text = New-NoticesText -Manifest $manifest -Artifact $a
        Write-TextFile -Path (Join-Path $TargetDir $a.notices) -Content $text
        $written.Add($a.notices) | Out-Null
    }
    return (Sort-Ordinal ([string[]]$written))
}

# --------------------------------------------------------------------------------------------------
# check
# --------------------------------------------------------------------------------------------------

function Invoke-Check {
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ("l2db-notices-" + [System.Guid]::NewGuid().ToString('n'))
    try {
        $expected = Invoke-Generate -TargetDir $temp

        $actualFiles = @()
        if (Test-Path -LiteralPath $generatedDir) {
            $actualFiles = @(Get-ChildItem -LiteralPath $generatedDir -Filter '*.txt' -File | ForEach-Object { $_.Name })
        }
        $actual = Sort-Ordinal ([string[]]$actualFiles)

        foreach ($name in $expected) {
            $tracked = Join-Path $generatedDir $name
            if (-not (Test-Path -LiteralPath $tracked)) {
                Add-Violation ("generated/{0} is missing - run -Action generate" -f $name)
                continue
            }
            $a = [System.IO.File]::ReadAllBytes($tracked)
            $b = [System.IO.File]::ReadAllBytes((Join-Path $temp $name))
            if (-not [System.Linq.Enumerable]::SequenceEqual([byte[]]$a, [byte[]]$b)) {
                Add-Violation ("generated/{0} does not match the manifest ({1} bytes tracked, {2} expected) - it was hand-edited, or components.json changed without -Action generate" -f $name, $a.Length, $b.Length)
            }
        }
        foreach ($name in $actual) {
            if ($expected -notcontains $name) {
                Add-Violation ("generated/{0} is not produced by the manifest - a removed artifact leaves an orphan; delete it" -f $name)
            }
        }

        # texts/ is the other direction the byte-compare cannot see: a text no component references any
        # more still sits there, and a removed component takes its licence body out of every notices
        # file while leaving the file behind. Neither shows up as drift.
        $manifest = Get-Manifest
        $referenced = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($c in $manifest.components) {
            foreach ($t in @($c.licenseTexts)) { if ($t) { $referenced.Add($t) | Out-Null } }
        }
        $onDisk = @()
        if (Test-Path -LiteralPath $textsDir) {
            $onDisk = @(Get-ChildItem -LiteralPath $textsDir -File | ForEach-Object { $_.Name })
        }
        foreach ($t in (Sort-Ordinal ([string[]]$onDisk))) {
            if (-not $referenced.Contains($t)) {
                Add-Violation ("texts/{0} is referenced by no component - delete it, or add the component that needs it" -f $t)
            }
        }
        foreach ($t in (Sort-Ordinal ([string[]]@($referenced)))) {
            if ($onDisk -notcontains $t) {
                Add-Violation ("texts/{0} is referenced by components.json but does not exist" -f $t)
            }
        }

        Add-Note ("compared {0} generated notices file(s); {1} licence text(s), all referenced" -f $expected.Count, $onDisk.Count)
    }
    finally {
        if (Test-Path -LiteralPath $temp) { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

# --------------------------------------------------------------------------------------------------
# verify
# --------------------------------------------------------------------------------------------------

function Test-GlobMatch {
    param([string] $Path, [string] $Pattern)
    # Package entries use '/'; manifest globs are written with '/' too.
    $rx = '^' + [regex]::Escape($Pattern).Replace('\*\*/', '(?:.*/)?').Replace('\*\*', '.*').Replace('\*', '[^/]*').Replace('\?', '.') + '$'
    return [regex]::IsMatch($Path, $rx, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

function Test-Ignored {
    param($Manifest, [string] $Path)
    foreach ($g in $Manifest.ignore) { if (Test-GlobMatch -Path $Path -Pattern $g) { return $true } }
    return $false
}

function Test-FirstParty {
    param($Manifest, [string] $FileName)
    # Exact assembly names only. A `linq2db*` prefix rule would silently classify the third-party
    # `linq2db4iSeries` package (which ships in the .lpx, and whose namespace is LinqToDB.*) as ours.
    foreach ($n in $Manifest.firstPartyAssemblies) {
        if ([string]::Equals($n, $FileName, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

# Satellite resources belong to their parent assembly's component: <culture>/Foo.resources.dll -> Foo.dll.
function Resolve-EffectiveName {
    param([string] $Path)
    $leaf = [System.IO.Path]::GetFileName($Path)
    if ($leaf -like '*.resources.dll') {
        return ($leaf -replace '\.resources\.dll$', '.dll')
    }
    return $leaf
}

function Resolve-Component {
    param($Components, [string] $Path, [string] $EffectiveName)

    $exact = @($Components | Where-Object { $c = $_; @($c.files | Where-Object { $_ -eq $EffectiveName }).Count -gt 0 })
    if ($exact.Count -eq 1) { return $exact[0] }
    if ($exact.Count -gt 1) {
        Add-Violation ("'{0}' is claimed by {1} components exactly ({2}) - a file may belong to one component only" -f $EffectiveName, $exact.Count, (($exact | ForEach-Object { $_.id }) -join ', '))
        return $exact[0]
    }

    $globbed = @($Components | Where-Object {
        $c = $_
        @($c.files | Where-Object { $_ -ne $EffectiveName -and (Test-GlobMatch -Path $Path -Pattern $_) }).Count -gt 0
    })
    if ($globbed.Count -eq 1) { return $globbed[0] }
    if ($globbed.Count -gt 1) {
        Add-Violation ("'{0}' is claimed by {1} components by glob ({2}) - make one of them an exact entry" -f $Path, $globbed.Count, (($globbed | ForEach-Object { $_.id }) -join ', '))
        return $globbed[0]
    }
    return $null
}

function Get-NupkgInfo {
    param([string] $NupkgPath)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })
        $nuspec  = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' -and $_.FullName -notlike '*/*' } | Select-Object -First 1
        $id      = $null
        if ($nuspec) {
            $reader = [System.IO.StreamReader]::new($nuspec.Open())
            try   { $xml = [xml] $reader.ReadToEnd() } finally { $reader.Dispose() }
            $node = $xml.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='id']")
            if ($node) { $id = $node.InnerText }
        }
        return [pscustomobject]@{ id = $id; entries = $entries; file = [System.IO.Path]::GetFileName($NupkgPath) }
    }
    finally { $zip.Dispose() }
}

function Get-ZipEntries {
    param([string] $ArchivePath)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try { return , @($zip.Entries | ForEach-Object { $_.FullName }) } finally { $zip.Dispose() }
}

function Get-ZipEntryText {
    param([string] $ArchivePath, [string] $EntryPath)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        $e = $zip.Entries | Where-Object { $_.FullName -eq $EntryPath } | Select-Object -First 1
        if (-not $e) { return $null }
        $r = [System.IO.StreamReader]::new($e.Open())
        try { return $r.ReadToEnd() } finally { $r.Dispose() }
    }
    finally { $zip.Dispose() }
}

# Version check, for the one artifact family that states its own versions.
#
# A tool payload carries a deps.json naming every package and version it was published with, so the
# manifest can be held to it. The check is per target framework and not per package, because several
# packages resolve to a different version per TFM - Microsoft.Extensions.Hosting is 8.0.0 / 9.0.0 /
# 10.0.0, since Directory.Packages.props conditions those entries on $(TargetFramework) - and each of
# the payloads inside one RID package has its own deps.json. Comparing a single manifest version
# against all three would fail two of them by construction.
#
# T4 packages and the .lpx carry no such manifest, so their versions are refreshed by `-Action harvest`
# during release prep rather than gated here. That limit is deliberate; inferring a package version from
# an assembly's FileVersion produces false failures (Microsoft.SqlServer.Types 170.1000.7,
# Net.IBM.Data.Db2 9.0.0.400).
function Test-PayloadVersions {
    param($Manifest, [string] $Label, [string] $Tfm, [string] $DepsJson)

    if (-not $DepsJson) { return }
    try { $deps = $DepsJson | ConvertFrom-Json } catch { Add-Violation ("{0}: could not parse deps.json for {1}" -f $Label, $Tfm); return }
    if (-not ($deps.PSObject.Properties.Name -contains 'libraries')) { return }

    $checked = 0
    foreach ($p in $deps.libraries.PSObject.Properties) {
        if ($p.Value.type -ne 'package') { continue }
        $id, $ver = $p.Name -split '/', 2

        $c = $Manifest.components | Where-Object { $_.packageId -eq $id } | Select-Object -First 1
        if (-not $c) { continue }   # unmapped packages are reported by the file-level pass

        $have = $c.versions.PSObject.Properties | Where-Object { $_.Name -eq $Tfm } | Select-Object -First 1
        if (-not $have) {
            Add-Violation ("{0}: component '{1}' has no '{2}' entry in its versions map, but {2} ships it at {3} - add it (run -Action harvest)" -f $Label, $c.id, $Tfm, $ver)
            continue
        }
        if ($have.Value -ne $ver) {
            Add-Violation ("{0}: component '{1}' says {2}={3}, but the {2} payload ships {4} - refresh the manifest (run -Action harvest)" -f $Label, $c.id, $Tfm, $have.Value, $ver)
        }
        $checked++
    }
    Add-Note ("{0}: {1} package versions checked against the {2} payload" -f $Label, $checked, $Tfm)
}

# An optional manifest field has to be probed, not read: Set-StrictMode turns a missing property on a
# PSCustomObject into a terminating error, and only the tool artifact carries packageIdPrefixes. Reading
# it directly means the first package belonging to *no* artifact kills the run - and that is every
# package the manifest deliberately excludes, so the failure cannot appear against a directory holding
# only the notices-bearing artifacts. It needs a full solution pack to show up, which is exactly what
# CI produces and a hand-assembled local check does not.
function Get-OptionalProperty {
    param($Object, [string] $Name)
    if ($null -eq $Object) { return $null }
    if ($Object.PSObject.Properties.Name -notcontains $Name) { return $null }
    return $Object.$Name
}

function Resolve-ArtifactForPackageId {
    param($Manifest, [string] $PackageId)
    if (-not $PackageId) { return $null }
    foreach ($a in $Manifest.artifacts) {
        $ids = Get-OptionalProperty -Object $a -Name 'packageIds'
        if ($ids -and ($ids -contains $PackageId)) { return $a }
    }
    foreach ($a in $Manifest.artifacts) {
        $prefixes = Get-OptionalProperty -Object $a -Name 'packageIdPrefixes'
        if (-not $prefixes) { continue }
        foreach ($p in $prefixes) {
            if ($PackageId.StartsWith($p, [System.StringComparison]::OrdinalIgnoreCase)) { return $a }
        }
    }
    return $null
}

# An enumeration that matches nothing passes every other assertion in this script. Each artifact class
# therefore has to prove it was actually read: implementation-bearing artifacts must contain at least one
# known first-party assembly, and the linq2db.cli pointer package - which carries no implementation at
# all - must contain its DotnetToolSettings.xml and no assemblies whatsoever.
function Test-ArtifactSanity {
    param($Manifest, [string] $Label, [string[]] $Entries, [string] $Kind)

    $binaries = @($Entries | Where-Object { $_ -match '(?i)\.(dll|exe|so|dylib)$' })

    if ($Kind -eq 'pointer') {
        if (-not ($Entries | Where-Object { $_ -match '(?i)tools/any/any/DotnetToolSettings\.xml$' })) {
            [Console]::Error.WriteLine("$Label`: pointer package has no tools/any/any/DotnetToolSettings.xml - the artifact was not read as expected")
            return $false
        }
        if ($binaries.Count -gt 0) {
            Add-Violation ("{0}: pointer package carries {1} assembl(y|ies) - it is supposed to carry none" -f $Label, $binaries.Count)
        }
        return $true
    }

    $firstParty = @($binaries | Where-Object { Test-FirstParty -Manifest $Manifest -FileName (Resolve-EffectiveName -Path $_) })
    if ($firstParty.Count -eq 0) {
        [Console]::Error.WriteLine("$Label`: no known first-party assembly found among $($binaries.Count) binaries - the artifact was not enumerated as expected, so its result would be vacuous")
        return $false
    }
    return $true
}

function Test-Artifact {
    param($Manifest, $Artifact, [string] $Label, [string[]] $Entries, [string] $Kind)

    if (-not (Test-ArtifactSanity -Manifest $Manifest -Label $Label -Entries $Entries -Kind $Kind)) {
        return $false
    }

    $components = Get-ComponentsForArtifact -Manifest $Manifest -Key $Artifact.key

    $noticesPresent = @($Entries | Where-Object { [System.IO.Path]::GetFileName($_) -eq 'THIRD-PARTY-NOTICES.txt' })
    if ($noticesPresent.Count -eq 0 -and $components.Count -gt 0) {
        Add-Violation ("{0}: no THIRD-PARTY-NOTICES.txt, but it ships {1} third-party component(s)" -f $Label, $components.Count)
    }

    $unmapped = [System.Collections.Generic.List[string]]::new()
    foreach ($e in $Entries) {
        if ($e.EndsWith('/')) { continue }
        if (Test-Ignored -Manifest $Manifest -Path $e) { continue }

        $effective = Resolve-EffectiveName -Path $e
        if (Test-FirstParty -Manifest $Manifest -FileName $effective) { continue }

        $c = Resolve-Component -Components $components -Path $e -EffectiveName $effective
        if (-not $c) { $unmapped.Add($e) | Out-Null }
    }

    foreach ($u in (Sort-Ordinal ([string[]]$unmapped))) {
        Add-Violation ("{0}: '{1}' maps to no component in the notices for this artifact - add it to Build/licenses/components.json (run -Action harvest for a proposal)" -f $Label, $u)
    }

    Add-Note ("{0}: {1} entries, {2} components, {3} unmapped" -f $Label, $Entries.Count, $components.Count, $unmapped.Count)
    return $true
}

# A package that is not in the manifest is one of ours, and the assembly it exists to ship is normally
# named after it - linq2db.Remote.Grpc ships linq2db.Remote.Grpc.dll. Treating that as first-party by
# *shape* rather than by name keeps the check correct as satellite packages come and go; enumerating
# them instead means every new one turns this gate red for no reason. The explicit allow-list stays for
# the assemblies whose name differs from their package id (linq2db.Analyzers ships LinqToDB.Analyzers.dll,
# linq2db.cli ships dotnet-linq2db).
#
# It does not weaken the exact-name rule that U-5 exists for: the exemption is only ever the package's
# *own* id, so a bundled dependency - linq2db4iSeries above all, whose id starts with linq2db and whose
# namespace starts with LinqToDB - can never match it.
function Test-ExcludedPackage {
    param($Manifest, [string] $Label, [string[]] $Entries, [string] $PackageId)

    $ownAssembly = if ($PackageId) { $PackageId + '.dll' } else { $null }

    $foreign = [System.Collections.Generic.List[string]]::new()
    foreach ($e in $Entries) {
        if ($e.EndsWith('/')) { continue }
        if ($e -notmatch '(?i)\.(dll|exe|so|dylib)$') { continue }
        if (Test-Ignored -Manifest $Manifest -Path $e) { continue }
        $effective = Resolve-EffectiveName -Path $e
        if (Test-FirstParty -Manifest $Manifest -FileName $effective) { continue }
        if ($ownAssembly -and [string]::Equals($effective, $ownAssembly, [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        $foreign.Add($e) | Out-Null
    }

    if ($foreign.Count -gt 0) {
        foreach ($f in (Sort-Ordinal ([string[]]$foreign))) {
            Add-Violation ("{0}: ships third-party binary '{1}', but this package is expected to declare its dependencies rather than bundle them - either stop bundling it, or add the package to components.json as a notices-bearing artifact" -f $Label, $f)
        }
    }
}

function Invoke-Verify {
    $manifest = Get-Manifest
    $inspected = 0

    if ($PackagesDir) {
        if (-not (Test-Path -LiteralPath $PackagesDir)) {
            [Console]::Error.WriteLine("PackagesDir not found: $PackagesDir")
            exit 2
        }
        $pkgs = @(Get-ChildItem -LiteralPath $PackagesDir -Recurse -Filter '*.nupkg' -File)
        if ($pkgs.Count -eq 0) {
            [Console]::Error.WriteLine("No .nupkg files found under: $PackagesDir")
            exit 2
        }
        foreach ($p in $pkgs) {
            $info = Get-NupkgInfo -NupkgPath $p.FullName
            if (-not $info.id) {
                Add-Violation ("{0}: could not read a package id out of the nuspec" -f $p.Name)
                continue
            }
            $artifact = Resolve-ArtifactForPackageId -Manifest $manifest -PackageId $info.id
            if ($artifact) {
                $kind = if ($artifact.kind -eq 'tool' -and $info.id -eq $artifact.key) { 'pointer' } else { $artifact.kind }
                if (-not (Test-Artifact -Manifest $manifest -Artifact $artifact -Label $info.id -Entries $info.entries -Kind $kind)) { exit 2 }

                if ($kind -eq 'tool') {
                    # tools/<tfm>/<rid>/<tool>.deps.json - one per target framework in the RID package.
                    foreach ($d in @($info.entries | Where-Object { $_ -match '^tools/[^/]+/[^/]+/[^/]+\.deps\.json$' })) {
                        $tfm  = ($d -split '/')[1]
                        $text = Get-ZipEntryText -ArchivePath $p.FullName -EntryPath $d
                        Test-PayloadVersions -Manifest $manifest -Label $info.id -Tfm $tfm -DepsJson $text
                    }
                }
                $inspected++
            }
            else {
                Test-ExcludedPackage -Manifest $manifest -Label $info.id -Entries $info.entries -PackageId $info.id
                $inspected++
            }
        }
    }

    if ($LpxDir) {
        if (-not (Test-Path -LiteralPath $LpxDir)) {
            [Console]::Error.WriteLine("LpxDir not found: $LpxDir")
            exit 2
        }
        $lpxs = @(Get-ChildItem -LiteralPath $LpxDir -Recurse -Filter '*.lpx' -File)
        if ($lpxs.Count -eq 0) {
            [Console]::Error.WriteLine("No .lpx files found under: $LpxDir")
            exit 2
        }
        foreach ($l in $lpxs) {
            $artifact = Get-Artifact -Manifest $manifest -Key 'linq2db.LINQPad.lpx'
            if (-not $artifact) {
                [Console]::Error.WriteLine("Manifest has no 'linq2db.LINQPad.lpx' artifact")
                exit 2
            }
            $entries = Get-ZipEntries -ArchivePath $l.FullName
            if (-not (Test-Artifact -Manifest $manifest -Artifact $artifact -Label $l.Name -Entries $entries -Kind 'lpx')) { exit 2 }
            $inspected++
        }
    }

    foreach ($d in @($PublishDir)) {
        if (-not $d) { continue }
        if (-not (Test-Path -LiteralPath $d)) {
            [Console]::Error.WriteLine("PublishDir not found: $d")
            exit 2
        }
        $artifact = Get-Artifact -Manifest $manifest -Key 'linq2db.cli'
        $root     = (Resolve-Path -LiteralPath $d).Path
        $entries  = @(Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object { $_.FullName.Substring($root.Length + 1).Replace('\', '/') })
        $label    = "publish:" + (Split-Path -Leaf $root)
        if (-not (Test-Artifact -Manifest $manifest -Artifact $artifact -Label $label -Entries $entries -Kind 'tool')) { exit 2 }

        # A publish directory holds one framework, and its deps.json names which: the `targets` map is
        # keyed by the moniker (".NETCoreApp,Version=v10.0" plus a RID-qualified twin).
        foreach ($d in @(Get-ChildItem -LiteralPath $root -Filter '*.deps.json' -File)) {
            $text = [System.IO.File]::ReadAllText($d.FullName)
            $tfm  = $null
            try {
                $rt = ($text | ConvertFrom-Json).runtimeTarget.name
                if ($rt -match '^\.NETCoreApp,Version=v(\d+)\.(\d+)') { $tfm = "net$($Matches[1]).$($Matches[2])" }
            } catch { }
            if ($tfm) { Test-PayloadVersions -Manifest $manifest -Label $label -Tfm $tfm -DepsJson $text }
            else      { Add-Note ("{0}: could not derive a target framework from {1}; version check skipped" -f $label, $d.Name) }
        }
        $inspected++
    }

    if ($inspected -eq 0) {
        [Console]::Error.WriteLine("Nothing to verify - pass -PackagesDir, -LpxDir and/or -PublishDir")
        exit 2
    }
    Add-Note ("inspected {0} artifact(s)" -f $inspected)
}

# --------------------------------------------------------------------------------------------------
# harvest
# --------------------------------------------------------------------------------------------------

function Get-NuGetCacheRoot {
    if ($env:NUGET_PACKAGES) { return $env:NUGET_PACKAGES }
    return (Join-Path $HOME '.nuget\packages')
}

function Get-PackageLicenseMetadata {
    param([string] $Id, [string] $Version)

    $dir = Join-Path (Get-NuGetCacheRoot) ($Id.ToLowerInvariant() + '\' + $Version)
    $result = [ordered]@{
        packageId = $Id; version = $Version
        license = 'UNKNOWN'; licenseKind = 'none'; licenseFile = ''
        copyright = ''; projectUrl = ''
    }
    if (-not (Test-Path -LiteralPath $dir)) { $result.licenseKind = 'not-restored'; return [pscustomobject]$result }

    $nuspec = Get-ChildItem -LiteralPath $dir -Filter '*.nuspec' -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $nuspec) { $result.licenseKind = 'no-nuspec'; return [pscustomobject]$result }

    [xml]$x = Get-Content -LiteralPath $nuspec.FullName -Raw
    $meta = $x.package.metadata
    if ($meta.PSObject.Properties.Name -contains 'copyright')  { $result.copyright  = $meta.copyright }
    if ($meta.PSObject.Properties.Name -contains 'projectUrl') { $result.projectUrl = $meta.projectUrl }

    if ($meta.PSObject.Properties.Name -contains 'license' -and $meta.license) {
        $result.licenseKind = $meta.license.type
        if ($meta.license.type -eq 'expression') { $result.license = $meta.license.'#text' }
        else {
            $result.license     = 'see license file'
            $result.licenseFile = $meta.license.'#text'
        }
    }
    elseif ($meta.PSObject.Properties.Name -contains 'licenseUrl' -and $meta.licenseUrl) {
        $result.licenseKind = 'url'
        $result.license     = $meta.licenseUrl
    }
    return [pscustomobject]$result
}

# Returns id/version pairs per TFM out of a project.assets.json or a *.deps.json.
#
# A restore graph contains every package the build touched, which is not the same set as the packages
# that ship: analyzers (AsyncFixer, Meziantou.Analyzer), source generators, SourceLink and
# DotNet.ReproducibleBuilds are all `PrivateAssets="All"` and contribute no file to the output. They are
# distinguished here by the shape of their target entry rather than by an id list, which would rot:
# a package that ships something has a `runtime`, `runtimeTargets` or `native` section, and a
# build-only one has only `build` / `analyzers`. Without this filter the CLI harvest reports 109
# packages against the 90 that the publish output actually contains.
function Get-GraphPackages {
    param([string] $Path)

    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $rows = [System.Collections.Generic.List[object]]::new()

    if ($json.PSObject.Properties.Name -contains 'targets') {
        foreach ($t in $json.targets.PSObject.Properties) {
            # project.assets.json has both "net10.0" and "net10.0/win-x64"; the bare TFM is enough,
            # the RID-qualified targets resolve the same package versions.
            $tfm = $t.Name
            if ($tfm -match '/') { continue }
            foreach ($p in $t.Value.PSObject.Properties) {
                $id, $ver = $p.Name -split '/', 2
                $type = if ($p.Value.PSObject.Properties.Name -contains 'type') { $p.Value.type } else { 'package' }
                if ($type -ne 'package') { continue }

                $names   = $p.Value.PSObject.Properties.Name
                $ships   = ($names -contains 'runtime') -or ($names -contains 'runtimeTargets') -or ($names -contains 'native')
                if (-not $ships) { continue }

                # A `runtime` section holding only the _._ placeholder means "nothing for this TFM".
                if (($names -contains 'runtime') -and -not ($names -contains 'runtimeTargets') -and -not ($names -contains 'native')) {
                    $assets = @($p.Value.runtime.PSObject.Properties.Name)
                    if ($assets.Count -eq 1 -and $assets[0] -match '(^|/)_\._$') { continue }
                }

                $rows.Add([pscustomobject]@{ tfm = $tfm; id = $id; version = $ver }) | Out-Null
            }
        }
    }
    if ($json.PSObject.Properties.Name -contains 'libraries' -and $rows.Count -eq 0) {
        foreach ($p in $json.libraries.PSObject.Properties) {
            if ($p.Value.type -ne 'package') { continue }
            $id, $ver = $p.Name -split '/', 2
            $rows.Add([pscustomobject]@{ tfm = '(deps)'; id = $id; version = $ver }) | Out-Null
        }
    }
    return $rows
}

function Invoke-Harvest {
    $manifest = $null
    if (Test-Path -LiteralPath $manifestPath) { $manifest = Get-Manifest }

    $files = @($AssetsFile)
    if (-not $files -or $files.Count -eq 0 -or -not $files[0]) {
        $files = @(
            (Join-Path $RepoRoot '.build\obj\LinqToDB.CLI\project.assets.json')
            (Join-Path $RepoRoot '.build\obj\NuGet\project.assets.json')
            (Join-Path $RepoRoot '.build\obj\LinqToDB.LINQPad\project.assets.json')
        )
        Add-Note 'no -AssetsFile given; using the three default graph-owning projects'
    }

    # Only the CLI's graph equals a shipped set: PackAsTool publishes the whole closure. The T4 packages
    # pack a hand-picked subset of NuGet.csproj's output and the .lpx takes the net472 output directory,
    # so those graphs contain plenty that ships nowhere - net462/net472 facades (System.IO, System.Runtime),
    # and the LINQPad project's net8.0-windows7.0 target, whose output no artifact carries. Proposing
    # those as new components would have the release-prep walk adding entries with no artifact.
    #
    # So a package absent from the manifest is proposed as [new] only when the CLI graph has it; from the
    # other graphs it is reported as [note], because a genuinely new *shipped* file there requires someone
    # editing a csproj's <None Include> list, which `verify` catches against the packed artifact at once.
    $all      = [System.Collections.Generic.List[object]]::new()
    $cliIds   = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($f in $files) {
        if (-not (Test-Path -LiteralPath $f)) {
            Write-Output ("  [skip]  {0} - not restored" -f $f)
            continue
        }
        $rows = Get-GraphPackages -Path $f
        $isCli = $f -match '(?i)LinqToDB\.CLI'
        foreach ($r in $rows) {
            $all.Add($r) | Out-Null
            if ($isCli) { $cliIds.Add($r.id) | Out-Null }
        }
    }

    if ($all.Count -eq 0) {
        [Console]::Error.WriteLine('No restore graphs could be read; run `dotnet restore` on the graph-owning projects first')
        exit 2
    }

    $byId = $all | Group-Object id
    $ids  = Sort-Ordinal ([string[]]($byId | ForEach-Object { $_.Name }))

    Write-Output ("Harvested {0} distinct package(s) from {1} graph row(s)." -f $ids.Count, $all.Count)
    Write-Output ''

    $newOnes   = [System.Collections.Generic.List[string]]::new()
    $changed   = [System.Collections.Generic.List[string]]::new()
    $noted     = [System.Collections.Generic.List[string]]::new()

    foreach ($id in $ids) {
        $rows     = @($byId | Where-Object { $_.Name -eq $id } | Select-Object -ExpandProperty Group)
        $versions = [ordered]@{}
        foreach ($tfm in (Sort-Ordinal ([string[]]($rows | ForEach-Object { $_.tfm } | Select-Object -Unique)))) {
            $versions[$tfm] = (@($rows | Where-Object { $_.tfm -eq $tfm })[0]).version
        }
        $rendered = (($versions.Keys | ForEach-Object { "$_=$($versions[$_])" }) -join ' ')

        $existing = $null
        if ($manifest) { $existing = $manifest.components | Where-Object { $_.packageId -eq $id } | Select-Object -First 1 }

        if (-not $existing) {
            $meta = Get-PackageLicenseMetadata -Id $id -Version (@($rows)[0].version)
            $line = ("{0}  {1}  license={2} ({3}) copyright='{4}'" -f $id, $rendered, $meta.license, $meta.licenseKind, $meta.copyright)
            if ($cliIds.Contains($id)) { $newOnes.Add("  [new]      $line") | Out-Null }
            else                       { $noted.Add("  [note]     $line") | Out-Null }
        }
        else {
            # Compare only the frameworks both sides know about. The manifest is the union of three
            # graphs, so harvesting one of them would otherwise report every entry that carries a TFM
            # the current run cannot see - a difference in coverage, not a change in versions.
            foreach ($tfm in $versions.Keys) {
                $have = $existing.versions.PSObject.Properties | Where-Object { $_.Name -eq $tfm } | Select-Object -First 1
                if (-not $have) {
                    $changed.Add(("  [version]  {0}  {1}: absent from the manifest, resolved to {2}" -f $id, $tfm, $versions[$tfm])) | Out-Null
                }
                elseif ($have.Value -ne $versions[$tfm]) {
                    $changed.Add(("  [version]  {0}  {1}: manifest {2}, resolved {3}" -f $id, $tfm, $have.Value, $versions[$tfm])) | Out-Null
                }
            }
        }
    }

    if ($manifest) {
        $graphIds = $ids
        foreach ($c in $manifest.components) {
            if ($c.packageId -and ($graphIds -notcontains $c.packageId) -and ($c.artifacts -contains 'linq2db.cli')) {
                $changed.Add(("  [gone]     {0} - in the manifest for linq2db.cli but not in the restore graph" -f $c.packageId)) | Out-Null
            }
        }
    }

    # Components carrying a `revisit` marker are decisions taken on a condition that will change - an
    # upstream package we are waiting on, a vendor question we have raised. They are reported on every
    # harvest so they resurface each release cycle rather than resting in a file nobody re-reads.
    if ($manifest) {
        $pending = @($manifest.components | Where-Object { ($_.PSObject.Properties.Name -contains 'revisit') -and $_.revisit })
        if ($pending.Count -gt 0) {
            Write-Output ("{0} component(s) carry a revisit marker - check whether the condition has been met:" -f $pending.Count)
            foreach ($c in $pending) {
                Write-Output ("  [revisit]  {0}" -f $c.displayName)
                Write-Output ("             {0}" -f $c.revisit)
            }
            Write-Output ''
        }
    }

    if ($newOnes.Count -eq 0 -and $changed.Count -eq 0) {
        Write-Output 'components.json agrees with the restore graphs. Nothing to propose.'
        if ($noted.Count -gt 0) {
            Write-Output ''
            Write-Output ("{0} package(s) are in the net462/net472 build graphs but ship in no artifact; they need no entry unless a csproj starts packing them:" -f $noted.Count)
            foreach ($l in (Sort-Ordinal ([string[]]$noted))) { Write-Output $l }
        }
        return
    }
    foreach ($l in (Sort-Ordinal ([string[]]$newOnes))) { Write-Output $l }
    foreach ($l in (Sort-Ordinal ([string[]]$changed))) { Write-Output $l }
    if ($noted.Count -gt 0) {
        Write-Output ''
        Write-Output ("plus {0} package(s) in the net462/net472 build graphs that ship in no artifact - no entry needed unless a csproj starts packing them (run with -AssetsFile to see them):" -f $noted.Count)
        foreach ($l in (Sort-Ordinal ([string[]]$noted))) { Write-Output $l }
    }
    Write-Output ''
    Write-Output 'harvest proposes only - edit Build/licenses/components.json by hand, then run -Action generate.'
}

# --------------------------------------------------------------------------------------------------
# Dispatch
# --------------------------------------------------------------------------------------------------

switch ($Action) {
    'generate' {
        $written = Invoke-Generate -TargetDir $generatedDir
        if ($script:violations.Count -eq 0) {
            Write-Output ("Generated {0} notices file(s) into Build/licenses/generated/." -f $written.Count)
            foreach ($w in $written) { Write-Output "  $w" }
        }
    }
    'check'   { Invoke-Check }
    'verify'  { Invoke-Verify }
    'harvest' { Invoke-Harvest; exit 0 }
}

foreach ($n in $script:notes) { Write-Output "  $n" }

if ($script:violations.Count -gt 0) {
    Write-Output ''
    foreach ($v in $script:violations) {
        Write-Output "  [FAIL]  $v"
        if (-not $NoAzdoLogs) { Write-Output ('##vso[task.logissue type=error]{0}' -f $v) }
    }
    Write-Output ''
    Write-Output ("{0} violation(s)." -f $script:violations.Count)
    exit 1
}

Write-Output 'OK'
exit 0
