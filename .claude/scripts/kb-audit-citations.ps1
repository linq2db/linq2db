#!/usr/bin/env pwsh
<#
KB citation auditor — random-sample K KB files, parse their Source/Tests
citations, verify each line still exists and the cited construct is still
recognizable. Returns a hit/miss list so /kb-refresh can demote confidence
on stale files.

One permission rule:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-audit-citations.ps1:*)

Input (stdin, JSON):
  {
    "k":         5,            // sample size; default 5
    "files":     ["areas/.../INDEX.md", ...],  // optional explicit list — bypasses random sample
    "scope":     ["areas/", "architecture/"],  // optional restriction for sampling
    "tokenWindow": 3            // ± lines around cited line to look for the cited token
  }

Output (stdout, JSON):
  {
    "ok": true,
    "audited": [
      {
        "file": "areas/PROV-ORACLE/INDEX.md",
        "citations_total": 12,
        "hits":   8,
        "misses": 4,
        "miss_details": [
          {"target":"Source/LinqToDB/DataProvider/Oracle/OracleSqlBuilder.cs:142",
           "reason":"line missing or token not found",
           "expected_token":"BuildSelectClause"},
          ...
        ],
        "verdict": "stale"  // "ok" | "stale" | "deleted"
      },
      ...
    ],
    "summary": { "files_audited": 5, "files_stale": 2 }
  }
#>

$global:ScriptBaseName = 'kb-audit-citations'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson
$k = if (Test-IsInteger $m.k) { [int]$m.k } else { 5 }
$tokenWindow = if (Test-IsInteger $m.tokenWindow) { [int]$m.tokenWindow } else { 3 }

$repoRoot = Resolve-Path "$PSScriptRoot/../.." | Select-Object -ExpandProperty Path
$kbRoot   = Join-Path $repoRoot '.claude/knowledge-base'

if (-not (Test-Path $kbRoot)) {
    Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $true; audited = @(); summary = @{ files_audited = 0; files_stale = 0 } })
    exit 0
}

# Determine candidate files
$explicit = @()
if ($m.files) { foreach ($f in $m.files) { $explicit += [string]$f } }

$candidateFiles = @()
if ($explicit.Count -gt 0) {
    foreach ($rel in $explicit) {
        $abs = Join-Path $kbRoot $rel
        if (Test-Path $abs) { $candidateFiles += [pscustomobject]@{ rel = $rel; abs = $abs } }
    }
} else {
    $scopeDirs = if ($m.scope) {
        @($m.scope | ForEach-Object { Join-Path $kbRoot ([string]$_) })
    } else {
        @(
            (Join-Path $kbRoot 'areas')
            (Join-Path $kbRoot 'architecture')
            (Join-Path $kbRoot 'conventions')
            (Join-Path $kbRoot 'history/decisions')
        )
    }
    foreach ($d in $scopeDirs) {
        if (-not (Test-Path $d)) { continue }
        foreach ($f in (Get-ChildItem -Path $d -Recurse -File -Filter '*.md' -ErrorAction SilentlyContinue)) {
            $rel = ($f.FullName.Replace('\', '/')).Substring($kbRoot.Replace('\', '/').Length + 1)
            $candidateFiles += [pscustomobject]@{ rel = $rel; abs = $f.FullName }
        }
    }
    # Random sample
    if ($candidateFiles.Count -gt $k) {
        $candidateFiles = $candidateFiles | Get-Random -Count $k
    }
}

$rxCitation = '\b((?:Source|Tests|Build)/[\w./-]+\.(?:cs|csproj|md|json|ps1)):(\d+)\b'

$audited = @()
$staleCount = 0

foreach ($file in $candidateFiles) {
    $body = $null
    try { $body = [System.IO.File]::ReadAllText($file.abs, [System.Text.UTF8Encoding]::new($false)) } catch { continue }
    $cites = @()
    foreach ($mm in [regex]::Matches($body, $rxCitation)) {
        $cites += [pscustomobject]@{
            target = $mm.Groups[1].Value + ':' + $mm.Groups[2].Value
            path   = $mm.Groups[1].Value
            line   = [int]$mm.Groups[2].Value
            offset = $mm.Index
        }
    }
    $hits = 0; $misses = 0
    $missDetails = @()
    foreach ($c in $cites) {
        $codeAbs = Join-Path $repoRoot $c.path
        if (-not (Test-Path $codeAbs)) {
            $misses++
            $missDetails += [pscustomobject]@{ target = $c.target; reason = 'cited file missing'; expected_token = $null }
            continue
        }
        $codeLines = $null
        try { $codeLines = [System.IO.File]::ReadAllLines($codeAbs, [System.Text.UTF8Encoding]::new($false)) } catch {
            $misses++
            $missDetails += [pscustomobject]@{ target = $c.target; reason = "read failed: $($_.Exception.Message)"; expected_token = $null }
            continue
        }
        $idx = $c.line - 1
        if ($idx -lt 0 -or $idx -ge $codeLines.Length) {
            $misses++
            $missDetails += [pscustomobject]@{ target = $c.target; reason = "line $($c.line) out of range (file has $($codeLines.Length))"; expected_token = $null }
            continue
        }
        # Extract a candidate token from KB body around the citation: grab the
        # nearest preceding identifier-shaped word (simple heuristic).
        $kbBefore = $body.Substring([Math]::Max(0, $c.offset - 200), [Math]::Min(200, $c.offset))
        $token = $null
        $idMatches = [regex]::Matches($kbBefore, '`([A-Za-z_][A-Za-z0-9_]+)`')
        if ($idMatches.Count -gt 0) {
            $token = $idMatches[$idMatches.Count - 1].Groups[1].Value
        }
        # Verify token presence in ± tokenWindow
        $loIdx = [Math]::Max(0, $idx - $tokenWindow)
        $hiIdx = [Math]::Min($codeLines.Length - 1, $idx + $tokenWindow)
        $window = ($codeLines[$loIdx..$hiIdx] -join "`n")
        if ($token -and ($window -notmatch [regex]::Escape($token))) {
            $misses++
            $missDetails += [pscustomobject]@{ target = $c.target; reason = 'token not in line window'; expected_token = $token }
        } else {
            $hits++
        }
    }
    $verdict = 'ok'
    if ($cites.Count -gt 0 -and $misses -ge 1) {
        $verdict = if ($missDetails | Where-Object { $_.reason -eq 'cited file missing' }) { 'deleted' } else { 'stale' }
        $staleCount++
    }
    $audited += [pscustomobject]@{
        file            = $file.rel
        citations_total = $cites.Count
        hits            = $hits
        misses          = $misses
        miss_details    = $missDetails
        verdict         = $verdict
    }
}

Write-JsonOutput -InputObject ([pscustomobject]@{
    ok      = $true
    audited = $audited
    summary = [pscustomobject]@{
        files_audited = $audited.Count
        files_stale   = $staleCount
    }
})
