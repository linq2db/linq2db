#!/usr/bin/env pwsh
<#
KB search — grep across .claude/knowledge-base/ with structured JSON output.
Used by kb-research to batch its own searches.

One permission rule:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-search.ps1 *)

Input (stdin, JSON):
  {
    "query":      "<regex>",
    "queries":    ["<regex>", ...],   // alternative to query — multiple terms, OR
    "scope":      ["areas/PROV-ORACLE", "architecture/"],  // optional path prefixes
    "include":    ["*.md"],            // optional file globs
    "exclude":    ["github/wiki/*"],
    "context":    2,                   // lines before/after each hit
    "maxHits":    100,
    "caseInsensitive": true
  }

Output (stdout, JSON):
  {
    "ok": true,
    "query":  "<resolved query>",
    "hits":   [
      {
        "file":     "areas/PROV-ORACLE/INDEX.md",
        "line":     42,
        "match":    "Oracle identity column generation uses sequences",
        "context_before": [...], "context_after": [...]
      },
      ...
    ],
    "files":  ["..."],     // unique file paths in hits, ordered by frequency
    "truncated": false
  }
#>

param([string]$ManifestFile)

$global:ScriptBaseName = 'kb-search'
. "$PSScriptRoot/_shared.ps1"

$m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile

$queries = @()
if ($m.query) { $queries += [string]$m.query }
if ($m.queries) { foreach ($q in $m.queries) { $queries += [string]$q } }
if ($queries.Count -eq 0) { Exit-WithError 'query or queries required' }

$scope    = if ($m.scope)    { @($m.scope    | ForEach-Object { [string]$_ }) } else { @() }
$include  = if ($m.include)  { @($m.include  | ForEach-Object { [string]$_ }) } else { @('*.md', '*.json') }
$exclude  = if ($m.exclude)  { @($m.exclude  | ForEach-Object { [string]$_ }) } else { @() }
$ctx      = if (Test-IsInteger $m.context) { [int]$m.context } else { 2 }
$maxHits  = if (Test-IsInteger $m.maxHits) { [int]$m.maxHits } else { 100 }
$caseI    = ($null -eq $m.caseInsensitive) -or ([bool]$m.caseInsensitive)

$repoRoot = Resolve-Path "$PSScriptRoot/../.." | Select-Object -ExpandProperty Path
$kbRoot   = Join-Path $repoRoot '.claude/knowledge-base'

if (-not (Test-Path $kbRoot)) {
    Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $true; query = ($queries -join '|'); hits = @(); files = @(); truncated = $false })
    exit 0
}

# Determine search roots
$searchRoots = @()
if ($scope.Count -gt 0) {
    foreach ($s in $scope) {
        $abs = Join-Path $kbRoot $s
        if (Test-Path $abs) { $searchRoots += $abs }
    }
} else {
    $searchRoots = @($kbRoot)
}

# Enumerate candidate files
$candidateFiles = @()
foreach ($root in $searchRoots) {
    if ((Get-Item $root).PSIsContainer) {
        foreach ($pat in $include) {
            $candidateFiles += @(Get-ChildItem -Path $root -Recurse -File -Filter $pat -ErrorAction SilentlyContinue)
        }
    } else {
        $candidateFiles += Get-Item -Path $root
    }
}
$candidateFiles = $candidateFiles | Sort-Object FullName -Unique

# Apply exclude patterns
if ($exclude.Count -gt 0) {
    $candidateFiles = $candidateFiles | Where-Object {
        $rel = ($_.FullName.Replace('\', '/')).Substring($kbRoot.Replace('\', '/').Length + 1)
        foreach ($ex in $exclude) {
            if ($rel -like $ex) { return $false }
        }
        return $true
    }
}

$pattern = ($queries | ForEach-Object { [regex]::Escape($_) }) -join '|'
# But we wanted these as regex — switch: don't escape, treat as regex literals from caller.
$pattern = $queries -join '|'
$rxOpts = if ($caseI) { [System.Text.RegularExpressions.RegexOptions]::IgnoreCase } else { [System.Text.RegularExpressions.RegexOptions]::None }
try { $rx = [regex]::new($pattern, $rxOpts) } catch { Exit-WithError "invalid regex: $($_.Exception.Message)" }

$hits = @()
$truncated = $false
$fileCounts = @{}

foreach ($f in $candidateFiles) {
    if ($hits.Count -ge $maxHits) { $truncated = $true; break }
    $rel = ($f.FullName.Replace('\', '/')).Substring($kbRoot.Replace('\', '/').Length + 1)
    $lines = $null
    try { $lines = [System.IO.File]::ReadAllLines($f.FullName, [System.Text.UTF8Encoding]::new($false)) } catch { continue }
    for ($i = 0; $i -lt $lines.Length; $i++) {
        if ($rx.IsMatch($lines[$i])) {
            $cb = @()
            $ca = @()
            for ($j = [Math]::Max(0, $i - $ctx); $j -lt $i; $j++) { $cb += $lines[$j] }
            for ($j = $i + 1; $j -lt [Math]::Min($lines.Length, $i + 1 + $ctx); $j++) { $ca += $lines[$j] }
            $hits += [pscustomobject]@{
                file           = $rel
                line           = $i + 1
                match          = $lines[$i]
                context_before = $cb
                context_after  = $ca
            }
            if ($fileCounts.ContainsKey($rel)) { $fileCounts[$rel] += 1 } else { $fileCounts[$rel] = 1 }
            if ($hits.Count -ge $maxHits) { $truncated = $true; break }
        }
    }
}

$files = @($fileCounts.GetEnumerator() | Sort-Object -Property Value -Descending | ForEach-Object { $_.Key })

Write-JsonOutput -InputObject ([pscustomobject]@{
    ok        = $true
    query     = $pattern
    hits      = $hits
    files     = $files
    truncated = $truncated
})
