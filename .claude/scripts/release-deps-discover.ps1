<#
release-deps-discover.ps1 — NuGet dependency-update discovery for the
release-prep workflow. Parses Directory.Packages.props + VersionOverride
sites, queries nuget.org for available versions, applies the project's
shipping-vs-test policy, and emits a plan JSON the orchestrator renders
to the user as a numbered update table.

Action:

  discover    Walk Directory.Packages.props (and Tests/Tests.T4.Nugets/
              Directory.Packages.props if present); grep VersionOverride
              sites; fetch nuget.org versions for each unique package id;
              compute proposed update per row; write plan to
              .build/.claude/release-<ver>-deps-plan.json.
              Inputs:  -Version <ver>
                       [-NoFetch]               # use cache only, never hit network
                       [-RefreshCache]          # force re-fetch even if cached
                       [-MaxParallel <n>]       # nuget query fan-out (default 8)
              Output:  { ok, planFile, totalPackages, withUpdates,
                         blockedByPolicy, fetchErrors, packages[] }

Plan-file schema (per `packages[]` entry):
  {
    "id":               "<PackageId>",
    "current":          "<resolved current version>",
    "source":           "Directory.Packages.props" | "VersionOverride" | "Tests.T4.Nugets",
    "file":             "<path>",
    "line":             <int>,
    "condition":        "<Condition attribute value if any>",
    "isOverride":       <bool>,
    "shipping":         <bool>,    # referenced by any Source/**/*.csproj
    "policy":           <null|"runtime-pin"|"shipping-prerelease"|"analyzer-allowed-prerelease">,
    "latestRelease":    "<x.y.z|null>",
    "latestPrerelease": "<x.y.z-...|null>",
    "proposed":         "<x.y.z|null>",
    "blocked":          <bool>,
    "blockReason":      "<text|null>",
    "fetchError":       "<text|null>"
  }

Conventions: `.claude/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('discover')]
    [string]$Action,

    [Parameter(Mandatory)][string]$Version,

    [switch]$NoFetch,
    [switch]$RefreshCache,
    [int]$MaxParallel = 8
)

$global:ScriptBaseName = 'release-deps-discover'
. (Join-Path $PSScriptRoot '_shared.ps1')

# -- paths -------------------------------------------------------------------

function Get-WorkDir {
    $dir = Join-Path (Get-Location) '.build/.claude'
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    return $dir
}

function Get-CacheDir {
    param([string]$Ver)
    $d = Join-Path (Get-WorkDir) "release-$Ver-deps-cache"
    if (-not (Test-Path -LiteralPath $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
    return $d
}

function Get-PlanFilePath {
    param([string]$Ver)
    return (Join-Path (Get-WorkDir) "release-$Ver-deps-plan.json")
}

# -- version comparison ------------------------------------------------------

# Parse a NuGet version into a tuple usable for comparison. Handles:
#   - 1.2.3            -> components=[1,2,3,0], prerelease=$null
#   - 1.2.3.4          -> components=[1,2,3,4], prerelease=$null
#   - 1.2.3-rc.1       -> components=[1,2,3,0], prerelease='rc.1'
#   - 1.2.3-rc.1+meta  -> meta stripped (NuGet ignores build metadata)
# Non-numeric components default to 0 — defensive against weird tags.
function ConvertTo-NuGetTuple {
    param([string]$V)
    if (-not $V) { return $null }
    # Strip build metadata (+...) — NuGet ignores it for ordering.
    $clean = $V -replace '\+.+$',''
    $base, $pre = $clean -split '-', 2
    $parts = @($base -split '\.' | ForEach-Object {
        try { [int]$_ } catch { 0 }
    })
    while ($parts.Count -lt 4) { $parts += 0 }
    return [pscustomobject]@{
        components = $parts
        prerelease = $pre
        raw        = $V
    }
}

# Compare two SemVer 2.0.0 prerelease identifier strings. Returns -1, 0, +1.
# Spec (semver.org §11.4):
#   - Identifiers consisting of only digits are compared numerically.
#   - Identifiers with letters or hyphens are compared lexically in ASCII order.
#   - Numeric identifiers always have lower precedence than alphanumeric ones.
#   - A larger set of pre-release fields has higher precedence than a smaller
#     set, if all preceding identifiers are equal.
# Plain ordinal compare misorders e.g. `rc.10` vs `rc.2` (ordinal puts `rc.10`
# before `rc.2`), so we cannot use [string]::Compare here.
function Compare-PrereleaseString {
    param([string]$A, [string]$B)
    $aIds = $A -split '\.'
    $bIds = $B -split '\.'
    $n = [Math]::Min($aIds.Count, $bIds.Count)
    for ($i = 0; $i -lt $n; $i++) {
        $ai = $aIds[$i]
        $bi = $bIds[$i]
        $aIsNum = $ai -match '^\d+$'
        $bIsNum = $bi -match '^\d+$'
        if ($aIsNum -and $bIsNum) {
            $av = [int]$ai
            $bv = [int]$bi
            if ($av -lt $bv) { return -1 }
            if ($av -gt $bv) { return 1 }
        } elseif ($aIsNum) {
            return -1
        } elseif ($bIsNum) {
            return 1
        } else {
            $cmp = [string]::Compare($ai, $bi, [System.StringComparison]::Ordinal)
            if ($cmp -lt 0) { return -1 }
            if ($cmp -gt 0) { return 1 }
        }
    }
    if ($aIds.Count -lt $bIds.Count) { return -1 }
    if ($aIds.Count -gt $bIds.Count) { return 1 }
    return 0
}

# Compare two NuGet versions. Returns -1, 0, +1.
function Compare-NuGetVersion {
    param([string]$A, [string]$B)
    $ta = ConvertTo-NuGetTuple $A
    $tb = ConvertTo-NuGetTuple $B
    if (-not $ta -and -not $tb) { return 0 }
    if (-not $ta) { return -1 }
    if (-not $tb) { return 1 }
    for ($i = 0; $i -lt 4; $i++) {
        if ($ta.components[$i] -lt $tb.components[$i]) { return -1 }
        if ($ta.components[$i] -gt $tb.components[$i]) { return 1 }
    }
    # Base components equal. Per SemVer: pre-release sorts before release.
    if (-not $ta.prerelease -and -not $tb.prerelease) { return 0 }
    if (-not $ta.prerelease) { return 1 }
    if (-not $tb.prerelease) { return -1 }
    return Compare-PrereleaseString $ta.prerelease $tb.prerelease
}

function Test-IsPrerelease {
    param([string]$V)
    if (-not $V) { return $false }
    return ($V -match '-')
}

# -- props parsing -----------------------------------------------------------

# Parse Directory.Packages.props (or any MSBuild *.props file) into a
# property hash + an array of PackageVersion entries. Resolves $(Var)
# references in versions using the file's <PropertyGroup>.
#
# Returns:
#   @{
#     properties = @{ Name = 'Value' ... }
#     entries    = @( @{ id; version; condition; line; file } ... )
#   }
#
# Skips entries inside HTML comments (<!-- ... -->). Multi-line comments
# are detected by line-by-line state tracking.
function Read-PackagesPropsFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    $lines = $text -split "`r?`n"

    $props = @{}
    $entries = New-Object 'System.Collections.Generic.List[object]'
    $inComment = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        if ($inComment) {
            if ($line -match '-->') { $inComment = $false; $line = ($line -split '-->',2)[1] }
            else { continue }
        }
        # Single-line and multi-line comment handling. Strip inline <!-- ... -->.
        while ($line -match '<!--') {
            $startIdx = $line.IndexOf('<!--')
            $endIdx   = $line.IndexOf('-->', $startIdx)
            if ($endIdx -lt 0) {
                $line = $line.Substring(0, $startIdx)
                $inComment = $true
                break
            } else {
                $line = $line.Substring(0, $startIdx) + $line.Substring($endIdx + 3)
            }
        }
        if (-not $line.Trim()) { continue }

        # Property: <PropName>value</PropName> (single-line only — sufficient
        # for the simple PropertyGroups in Directory.Packages.props).
        if ($line -match '^\s*<(?<name>[A-Za-z_][\w.]*)>(?<val>[^<]*)</\1>\s*$') {
            $props[$Matches['name']] = $Matches['val']
            continue
        }

        # PackageVersion entry.
        if ($line -match '<PackageVersion\s+') {
            $id = $null
            $ver = $null
            $cond = $null
            if ($line -match 'Include\s*=\s*"([^"]+)"')   { $id = $Matches[1] }
            if ($line -match 'Version\s*=\s*"([^"]+)"')   { $ver = $Matches[1] }
            if ($line -match 'Condition\s*=\s*"([^"]+)"') { $cond = $Matches[1] }
            if ($id -and $ver) {
                $entries.Add(@{
                    id        = $id
                    version   = $ver
                    condition = $cond
                    line      = $lineNum
                    file      = $Path
                }) | Out-Null
            }
        }
    }

    return @{ properties = $props; entries = $entries.ToArray() }
}

# Resolve $(Name) references in $Value using $Properties. Multiple passes
# until stable or a max-iterations cap (defensive — MSBuild allows nested
# property references). Returns the resolved string.
function Resolve-MsBuildProperty {
    param([string]$Value, [hashtable]$Properties)
    $out = $Value
    for ($i = 0; $i -lt 8; $i++) {
        $prev = $out
        $out = [regex]::Replace($out, '\$\((?<n>[A-Za-z_][\w.]*)\)', {
            param($m)
            $name = $m.Groups['n'].Value
            if ($Properties.ContainsKey($name)) { return $Properties[$name] }
            return $m.Value
        })
        if ($out -eq $prev) { break }
    }
    return $out
}

# -- VersionOverride discovery -----------------------------------------------

# Paths to exclude when scanning the working tree. `.build/` carries cached
# copies of csproj files from past PR reviews (see diff-reader.ps1); `.git`,
# `bin`, `obj`, `node_modules`, `packages` carry build / vcs artefacts.
# Without this filter, the same csproj is reported multiple times and
# triggers duplicate VersionOverride / shipping detections.
#
# Implemented as a regex against the path's normalized form (forward slashes)
# so behavior is identical on Windows / Linux / macOS. Anchored on `/<dir>/`
# patterns so an unrelated file like `obj-foo.cs` won't match `/obj/`.
function Test-IsExcludedPath {
    param([string]$Path)
    if (-not $Path) { return $false }
    $norm = ($Path -replace '\\','/') + '/'
    return ($norm -match '/(\.build|\.git|bin|obj|node_modules|packages)/')
}

function Find-VersionOverrideSites {
    param([string]$Root)
    # Limit to .csproj and .props to keep the grep narrow and fast.
    # `-LiteralPath -Recurse -Include` combination is unreliable in PowerShell
    # (Include filter sometimes returns no results with LiteralPath). Use
    # plain -Path + -Recurse -Filter pattern: two calls, one per extension.
    $files = @()
    $files += @(Get-ChildItem -Path $Root -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-IsExcludedPath $_.FullName) })
    $files += @(Get-ChildItem -Path $Root -Recurse -Filter '*.props' -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-IsExcludedPath $_.FullName) })
    $sites = New-Object 'System.Collections.Generic.List[object]'
    foreach ($f in $files) {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        if ($text -notmatch 'VersionOverride') { continue }
        $lines = $text -split "`r?`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            # Both Include= and Update= forms are valid in csproj. Cover both
            # orderings of the attribute pair (Id/VersionOverride).
            if ($line -match '<PackageReference\s+[^>]*(?:Include|Update)\s*=\s*"(?<id>[^"]+)"[^>]*VersionOverride\s*=\s*"(?<v>[^"]+)"' -or
                $line -match '<PackageReference\s+[^>]*VersionOverride\s*=\s*"(?<v>[^"]+)"[^>]*(?:Include|Update)\s*=\s*"(?<id>[^"]+)"') {
                $sites.Add(@{
                    id      = $Matches['id']
                    version = $Matches['v']
                    file    = $f.FullName
                    line    = $i + 1
                }) | Out-Null
            }
        }
    }
    return $sites.ToArray()
}

# -- shipping detection ------------------------------------------------------

# A package is "shipping" if any csproj under Source/ has a
# <PackageReference Include="<id>" /> for it. Per the repo's central-
# package-management convention, source projects reference packages
# without a Version attribute — version comes from Directory.Packages.props.
function Find-ShippingPackageIds {
    param([string]$Root)
    $sourceDir = Join-Path $Root 'Source'
    if (-not (Test-Path -LiteralPath $sourceDir)) { return @() }
    $files = @(Get-ChildItem -Path $sourceDir -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-IsExcludedPath $_.FullName) })
    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($f in $files) {
        $text = [System.IO.File]::ReadAllText($f.FullName)
        foreach ($m in [regex]::Matches($text, '<PackageReference\s+[^>]*Include\s*=\s*"(?<id>[^"]+)"')) {
            [void]$set.Add($m.Groups['id'].Value)
        }
    }
    return @($set)
}

# -- nuget query -------------------------------------------------------------

function Get-NugetCachedVersions {
    param([string]$PackageId, [string]$CacheDir)
    $lcid = $PackageId.ToLowerInvariant()
    $path = Join-Path $CacheDir "$lcid.json"
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try {
        $data = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
        return @{ versions = @($data.versions); cached = $true }
    } catch {
        return $null
    }
}

function Save-NugetCache {
    param([string]$PackageId, [string]$CacheDir, [string[]]$Versions)
    $lcid = $PackageId.ToLowerInvariant()
    $path = Join-Path $CacheDir "$lcid.json"
    $obj = @{ id = $PackageId; versions = $Versions; fetched_at = (Get-Date).ToString('o') }
    [System.IO.File]::WriteAllText(
        $path,
        ($obj | ConvertTo-Json -Depth 10),
        [System.Text.UTF8Encoding]::new($false)
    )
}

function Get-NugetVersionsLive {
    param([string]$PackageId)
    $lcid = $PackageId.ToLowerInvariant()
    $url = "https://api.nuget.org/v3-flatcontainer/$lcid/index.json"
    try {
        $resp = Invoke-RestMethod -Uri $url -TimeoutSec 30 -UseBasicParsing
        return @{ versions = @($resp.versions); error = $null }
    } catch {
        return @{ versions = @(); error = $_.Exception.Message }
    }
}

# -- policy ------------------------------------------------------------------

# Pin-rule package patterns (shipping packages that should stick to their
# initial .NET version unless flagged vulnerable). The trailing ones cover
# AspNetCore-related runtime libs that get pulled in as transitive deps of
# the same .NET version line.
$script:RuntimePinPatterns = @(
    '^System\.',
    '^Microsoft\.Extensions\.',
    '^Microsoft\.AspNetCore\.',
    '^Microsoft\.Bcl\.',
    '^Microsoft\.CSharp$',
    '^Microsoft\.NETCore\.'
)

function Test-IsRuntimePinCandidate {
    param([string]$Id)
    foreach ($p in $script:RuntimePinPatterns) {
        if ($Id -match $p) { return $true }
    }
    return $false
}

# -- discover (main) ---------------------------------------------------------

function Do-Discover {
    $root = Get-Location | Select-Object -ExpandProperty Path
    $cacheDir = Get-CacheDir -Ver $Version

    # 1. Parse main + (optional) test-isolated props
    $main = Read-PackagesPropsFile -Path (Join-Path $root 'Directory.Packages.props')
    if (-not $main) { Exit-WithError "Directory.Packages.props not found at $root" }

    $t4Path = Join-Path $root 'Tests\Tests.T4.Nugets\Directory.Packages.props'
    $t4 = if (Test-Path -LiteralPath $t4Path) { Read-PackagesPropsFile -Path $t4Path } else { $null }

    # 2. Find VersionOverride sites
    $overrides = Find-VersionOverrideSites -Root $root

    # 3. Identify shipping packages
    $shippingIds = Find-ShippingPackageIds -Root $root
    $shippingSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$shippingIds, [System.StringComparer]::OrdinalIgnoreCase)

    # 4. Build the row list
    $rows = New-Object 'System.Collections.Generic.List[object]'

    foreach ($e in $main.entries) {
        $resolved = Resolve-MsBuildProperty -Value $e.version -Properties $main.properties
        $rows.Add(@{
            id          = $e.id
            current     = $resolved
            rawVersion  = $e.version
            condition   = $e.condition
            source      = 'Directory.Packages.props'
            file        = $e.file
            line        = $e.line
            isOverride  = $false
            shipping    = $shippingSet.Contains($e.id)
        }) | Out-Null
    }
    if ($t4) {
        foreach ($e in $t4.entries) {
            $resolved = Resolve-MsBuildProperty -Value $e.version -Properties $t4.properties
            $rows.Add(@{
                id          = $e.id
                current     = $resolved
                rawVersion  = $e.version
                condition   = $e.condition
                source      = 'Tests.T4.Nugets'
                file        = $e.file
                line        = $e.line
                isOverride  = $false
                shipping    = $false   # by definition, T4-nugets central props is test-only
            }) | Out-Null
        }
    }
    foreach ($o in $overrides) {
        # VersionOverride values commonly reference $()-style properties
        # defined in Directory.Packages.props (e.g. $(OracleManagedLinqPad
        # Version)). Resolve against the main props' property table.
        $resolved = Resolve-MsBuildProperty -Value $o.version -Properties $main.properties
        $rows.Add(@{
            id          = $o.id
            current     = $resolved
            rawVersion  = $o.version
            condition   = $null
            source      = 'VersionOverride'
            file        = $o.file
            line        = $o.line
            isOverride  = $true
            shipping    = $shippingSet.Contains($o.id)
        }) | Out-Null
    }

    # 5. Fetch nuget versions for each unique id
    $uniqueIds = @($rows | ForEach-Object { $_.id } | Sort-Object -Unique)
    $idToVersions = @{}
    $idToError = @{}

    # Populate cached first
    $needFetch = New-Object 'System.Collections.Generic.List[string]'
    foreach ($id in $uniqueIds) {
        if ($RefreshCache) {
            $needFetch.Add($id) | Out-Null
            continue
        }
        $cached = Get-NugetCachedVersions -PackageId $id -CacheDir $cacheDir
        if ($cached) { $idToVersions[$id] = $cached.versions }
        else { $needFetch.Add($id) | Out-Null }
    }

    if (-not $NoFetch -and $needFetch.Count -gt 0) {
        # Parallel fan-out. Use ForEach-Object -Parallel; do not call gh/git
        # inside (no shared helpers needed beyond Invoke-RestMethod).
        $results = $needFetch | ForEach-Object -ThrottleLimit $MaxParallel -Parallel {
            $id = $_
            $lcid = $id.ToLowerInvariant()
            $url = "https://api.nuget.org/v3-flatcontainer/$lcid/index.json"
            try {
                $resp = Invoke-RestMethod -Uri $url -TimeoutSec 30 -UseBasicParsing
                [pscustomobject]@{ id = $id; versions = @($resp.versions); error = $null }
            } catch {
                [pscustomobject]@{ id = $id; versions = @(); error = $_.Exception.Message }
            }
        }
        foreach ($r in $results) {
            if ($r.error) {
                $idToError[$r.id] = $r.error
                $idToVersions[$r.id] = @()
            } else {
                $idToVersions[$r.id] = $r.versions
                Save-NugetCache -PackageId $r.id -CacheDir $cacheDir -Versions $r.versions
            }
        }
    }

    # 6. Compute proposed + policy per row
    $totalWithUpdates = 0
    $totalBlocked = 0
    $packages = New-Object 'System.Collections.Generic.List[object]'
    foreach ($row in $rows) {
        $vs = @($idToVersions[$row.id])
        $err = $idToError[$row.id]

        $latestRelease = $null
        $latestPre = $null
        foreach ($v in $vs) {
            $isPre = Test-IsPrerelease $v
            if ($isPre) {
                if (-not $latestPre -or (Compare-NuGetVersion $v $latestPre) -gt 0) { $latestPre = $v }
            } else {
                if (-not $latestRelease -or (Compare-NuGetVersion $v $latestRelease) -gt 0) { $latestRelease = $v }
            }
        }

        $policy = $null
        $blocked = $false
        $blockReason = $null
        $proposed = $null

        $currentIsPre = Test-IsPrerelease $row.current

        # Runtime-pin only applies to shipping packages and only to the
        # canonical pinned form (e.g. 8.0.0 / 9.0.0 / 10.0.0). If the
        # current is already off-pin (e.g. 8.0.25), the user has knowingly
        # bumped past pin — don't re-flag.
        if ($row.shipping -and (Test-IsRuntimePinCandidate $row.id) -and ($row.current -match '^\d+\.0\.0$')) {
            $policy = 'runtime-pin'
            if ($latestRelease -and (Compare-NuGetVersion $latestRelease $row.current) -gt 0) {
                $blocked = $true
                $blockReason = 'policy:runtime-pin (use current pinned version unless flagged vulnerable)'
            }
        }

        if (-not $blocked) {
            # Default: propose latest-release if it's newer than current.
            if ($latestRelease -and (Compare-NuGetVersion $latestRelease $row.current) -gt 0) {
                $proposed = $latestRelease
            }
            # If current is already a prerelease and a newer prerelease exists,
            # also surface that as a viable target.
            if ($currentIsPre -and $latestPre -and (Compare-NuGetVersion $latestPre $row.current) -gt 0) {
                if (-not $proposed -or (Compare-NuGetVersion $latestPre $proposed) -gt 0) {
                    $proposed = $latestPre
                }
            }
            # Shipping packages: prerelease-proposed targets are blocked
            # unless current is already prerelease (rare; analyzer cases).
            if ($row.shipping -and $proposed -and (Test-IsPrerelease $proposed) -and -not $currentIsPre) {
                $blocked = $true
                $blockReason = 'policy:shipping-prerelease (latest is prerelease; shipping packages must not use prereleases)'
                $policy = 'shipping-prerelease'
                # Surface the release version anyway if it exists and is newer.
                if ($latestRelease -and (Compare-NuGetVersion $latestRelease $row.current) -gt 0 -and -not (Test-IsPrerelease $latestRelease)) {
                    $proposed = $latestRelease
                    $blocked = $false
                    $blockReason = $null
                    $policy = $null
                }
            }
        }

        if ($blocked) { $totalBlocked++ }
        elseif ($proposed) { $totalWithUpdates++ }

        $packages.Add([ordered]@{
            id               = $row.id
            current          = $row.current
            rawVersion       = $row.rawVersion
            source           = $row.source
            file             = $row.file
            line             = $row.line
            condition        = $row.condition
            isOverride       = $row.isOverride
            shipping         = $row.shipping
            latestRelease    = $latestRelease
            latestPrerelease = $latestPre
            proposed         = $proposed
            policy           = $policy
            blocked          = $blocked
            blockReason      = $blockReason
            fetchError       = $err
        }) | Out-Null
    }

    $planPath = Get-PlanFilePath -Ver $Version
    [System.IO.File]::WriteAllText(
        $planPath,
        (@{ packages = $packages.ToArray() } | ConvertTo-Json -Depth 100),
        [System.Text.UTF8Encoding]::new($false)
    )

    $fetchErrors = @($packages | Where-Object { $_.fetchError } | ForEach-Object { @{ id = $_.id; error = $_.fetchError } })

    Write-JsonOutput @{
        ok               = $true
        action           = 'discover'
        planFile         = $planPath
        totalPackages    = $packages.Count
        uniquePackages   = $uniqueIds.Count
        withUpdates      = $totalWithUpdates
        blockedByPolicy  = $totalBlocked
        fetchErrors      = $fetchErrors
        overrideSites    = @($overrides).Count
        packages         = $packages.ToArray()
    }
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'discover' { Do-Discover }
    default    { Exit-WithError "unknown action: $Action" }
}
