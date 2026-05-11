#!/usr/bin/env pwsh
<#
KB coverage queue backfill — one-shot script that seeds
`state/deferred-coverage.json` for areas whose `INDEX.md` predates the
`=== DEFERRED-COVERAGE ===` fence mechanism.

For each area in the manifest:
  1. Glob the area's Tier-2 file set from its path patterns
     (subtracting Tier-1 anchors and Tier-3 generated patterns).
  2. Read `areas/<area>/INDEX.md` and extract every file name / path
     mentioned inside the `<details><summary>Coverage</summary>` block.
  3. Every Tier-2 file NOT mentioned there is queued with
     `reason: "verify-coverage"`. Mentioned files are presumed already
     covered (the `coverage-fill` agent will re-confirm on next visit).

Invocation:
    pwsh -NoProfile -File .claude/scripts/kb-coverage-backfill.ps1 <<'EOF'
    {
      "areas": [
        {
          "code": "CORE",
          "pathPatterns": ["Source/LinqToDB/*.cs", "Source/LinqToDB/Configuration/**/*.cs"],
          "tier1": ["Source/LinqToDB/IDataContext.cs", "Source/LinqToDB/DataContext.cs", "Source/LinqToDB/Data/DataConnection.cs", "Source/LinqToDB/LinqToDB.csproj", "Source/LinqToDB/Configuration/LinqToDBSection.cs", "Source/LinqToDB/Configuration/ILinqToDBSettings.cs"]
        }
      ],
      "deferredAtSha": "<sha>"
    }

Output: per-area summary `{area, tier2_total, mentioned, queued}`.

The script does NOT modify any artifact under `.claude/knowledge-base/`
beyond `state/deferred-coverage.json` (via `kb-state.ps1 set-deferred-area`).
#>

param([string]$ManifestFile)

$global:ScriptBaseName = 'kb-coverage-backfill'
. "$PSScriptRoot/_shared.ps1"

$RepoRoot     = Resolve-Path "$PSScriptRoot/../.." | Select-Object -ExpandProperty Path
$KbRoot       = Join-Path $RepoRoot '.claude/knowledge-base'

# Tier-3 patterns from kb-coverage-tiers.md — generated / build-output files
# that are counted but never visited and so never belong on the queue.
$Tier3Patterns = @(
    '*.g.cs', '*.Designer.cs', '*.Generated.cs', '*.generated.cs'
)
$Tier3Dirs = @('/bin/', '/obj/', '/.vs/', '/.idea/', '/TestResults/')

function Test-Tier3 {
    param([string]$Path)
    $name = Split-Path -Leaf $Path
    foreach ($g in $Tier3Patterns) {
        if ($name -like $g) { return $true }
    }
    $norm = $Path -replace '\\','/'
    foreach ($d in $Tier3Dirs) {
        if ($norm -like "*$d*") { return $true }
    }
    return $false
}

function Expand-Pattern {
    param([string]$Pattern)
    $p = ($Pattern -replace '\\','/').TrimStart('/')
    if ($p -match '^(.+?)/\*\*/\*\.(\w+)$') {
        $base = Join-Path $RepoRoot $Matches[1]
        $ext  = $Matches[2]
        if (-not (Test-Path $base)) { return @() }
        return Get-ChildItem -Path $base -Filter "*.$ext" -Recurse -File -ErrorAction SilentlyContinue
    }
    if ($p -match '^(.+?)/\*\*$') {
        $base = Join-Path $RepoRoot $Matches[1]
        if (-not (Test-Path $base)) { return @() }
        return Get-ChildItem -Path $base -Recurse -File -ErrorAction SilentlyContinue
    }
    if ($p -match '^(.+?)/\*\.(\w+)$') {
        $base = Join-Path $RepoRoot $Matches[1]
        $ext  = $Matches[2]
        if (-not (Test-Path $base)) { return @() }
        return Get-ChildItem -Path $base -Filter "*.$ext" -File -ErrorAction SilentlyContinue
    }
    # Treat as a single concrete file path.
    $abs = Join-Path $RepoRoot $p
    if (Test-Path $abs -PathType Leaf) { return @(Get-Item $abs) }
    return @()
}

function Resolve-AreaTier2 {
    param([string[]]$Patterns, [string[]]$Tier1, [string[]]$ExtFilter)
    $tier1Set = @{}
    foreach ($t in $Tier1) { $tier1Set[($t -replace '\\','/')] = $true }
    $allowExt = @{}
    foreach ($e in $ExtFilter) { $allowExt[$e.ToLowerInvariant()] = $true }
    $matches = @{}
    foreach ($p in $Patterns) {
        foreach ($f in (Expand-Pattern -Pattern $p)) {
            $rel = $f.FullName.Substring($RepoRoot.Length + 1) -replace '\\','/'
            if ($tier1Set.ContainsKey($rel)) { continue }
            if (Test-Tier3 $rel) { continue }
            if ($allowExt.Count -gt 0) {
                $ext = ([System.IO.Path]::GetExtension($rel)).TrimStart('.').ToLowerInvariant()
                if (-not $allowExt.ContainsKey($ext)) { continue }
            }
            $matches[$rel] = $true
        }
    }
    return @($matches.Keys | Sort-Object)
}

function Read-CoverageBlock {
    param([string]$IndexMdPath)
    if (-not (Test-Path $IndexMdPath)) { return '' }
    $raw = [System.IO.File]::ReadAllText($IndexMdPath, [System.Text.UTF8Encoding]::new($false))
    $start = $raw.IndexOf('<details><summary>Coverage</summary>')
    if ($start -lt 0) { return '' }
    $end = $raw.IndexOf('</details>', $start)
    if ($end -lt 0) { return $raw.Substring($start) }
    return $raw.Substring($start, $end - $start)
}

function Get-MentionedFiles {
    param([string]$CoverageBlock)
    $mentioned = @{}
    if (-not $CoverageBlock) { return $mentioned }
    # Match full paths first: "Source/.../foo.cs" or "Tests/.../foo.cs"
    foreach ($m in [regex]::Matches($CoverageBlock, '\b((?:Source|Tests|Build)/[\w./{}-]+\.(?:cs|csproj|fs|fsproj|vb|tt|ttinclude|md|json|ps1))\b')) {
        $mentioned[($m.Groups[1].Value -replace '\\','/')] = $true
    }
    # Match bare filenames: "Foo.cs", "Foo.Bar.cs", "Foo{T}.cs" (handle the
    # generic-arg notation used in INDEX.md). Do NOT match cross-area pointers.
    foreach ($m in [regex]::Matches($CoverageBlock, '\b([A-Z][\w.{}]*\.(?:cs|csproj|fs|fsproj|vb))\b')) {
        $name = $m.Groups[1].Value
        $mentioned['__BARENAME__' + $name] = $true
    }
    return $mentioned
}

function Test-IsMentioned {
    param([string]$RelPath, $Mentioned)
    $norm = $RelPath -replace '\\','/'
    if ($Mentioned.ContainsKey($norm)) { return $true }
    $bare = Split-Path -Leaf $norm
    if ($Mentioned.ContainsKey('__BARENAME__' + $bare)) { return $true }
    return $false
}

# ---------- main ----------

$m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
if (-not $m.areas) { Exit-WithError 'areas[] required' }
$deferredSha = if ($m.deferredAtSha) { [string]$m.deferredAtSha } else { '' }
$today = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')

$results = @()
foreach ($a in $m.areas) {
    $code = [string]$a.code
    if (-not $code) { continue }
    $patterns = @()
    if ($a.pathPatterns) { foreach ($p in $a.pathPatterns) { $patterns += [string]$p } }
    $tier1 = @()
    if ($a.tier1) { foreach ($t in $a.tier1) { $tier1 += [string]$t } }
    if ($patterns.Count -eq 0) { Exit-WithError "area ${code}: pathPatterns required" }

    $extFilter = @()
    if ($a.extensions) { foreach ($e in $a.extensions) { $extFilter += [string]$e } }
    if ($extFilter.Count -eq 0) { $extFilter = @('cs') }
    $tier2 = Resolve-AreaTier2 -Patterns $patterns -Tier1 $tier1 -ExtFilter $extFilter
    $coverage = Read-CoverageBlock -IndexMdPath (Join-Path $KbRoot "areas/$code/INDEX.md")
    $mentioned = Get-MentionedFiles -CoverageBlock $coverage
    $queued = @()
    foreach ($f in $tier2) {
        if (-not (Test-IsMentioned -RelPath $f -Mentioned $mentioned)) {
            $queued += [pscustomobject]@{ path = $f; reason = 'verify-coverage' }
        }
    }

    # Apply via kb-state.ps1 set-deferred-area
    $payload = @{
        op = 'set-deferred-area'
        area = $code
        deferred_at = $today
        deferred_at_sha = $deferredSha
        files = $queued
    } | ConvertTo-Json -Depth 100
    $r = Invoke-Process -FilePath 'pwsh' `
        -ArgumentList @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'kb-state.ps1')) `
        -StdinInput $payload
    if (-not $r.ok) { Exit-WithError "set-deferred-area $code failed: $($r.error)" }

    $results += [pscustomobject]@{
        area         = $code
        tier2_total  = $tier2.Count
        mentioned    = $tier2.Count - $queued.Count
        queued       = $queued.Count
    }
}

Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $true; areas = $results })
