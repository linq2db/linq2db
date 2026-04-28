#!/usr/bin/env pwsh
<#
Diff a pre-run baselines snapshot (from `snap-baselines.ps1`) against the
current state of the same paths — emit the set of files added, removed,
or whose SHA1 changed since the snapshot.

Why this wrapper exists
-----------------------
The `/test` skill's post-run flow compares the pre-snapshot hash map to
the current filesystem, so it can surface which baseline files the run
touched. Running that as raw PowerShell inside Bash needs multiple
allowlist entries (ConvertFrom-Json piping, Get-FileHash looping) and
makes per-file reporting awkward. This script answers the whole diff in
one call:

    Bash(pwsh -NoProfile -File .claude/scripts/diff-baselines.ps1:*)

Input (stdin, JSON)
-------------------
  {
    "preFile":  ".build/.claude/baselines-pre-<run>.json",  // required — produced by snap-baselines.ps1
    "paths":    ["c:/GitHub/linq2db.bls/Firebird.4", ...]   // required — same shape as snap-baselines paths[]
  }

Output (stdout, single JSON object):

  {
    "status":   "ok",
    "preFile":  "...",
    "counts":   { "changed": 15, "added": 2, "removed": 0, "unchanged": 1088 },
    "changed":  [{ "path": "...", "preHash": "...", "postHash": "..." }, ...],
    "added":    [{ "path": "...", "postHash": "..." }, ...],
    "removed":  [{ "path": "...", "preHash": "..." }, ...]
  }

Paths missing from the current filesystem that were present in the
snapshot go into `removed`. Paths present now that were not in the
snapshot go into `added`. Hash mismatches go into `changed`. Everything
else is counted in `counts.unchanged` but not listed.
#>

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'diff-baselines'

$manifest = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($manifest)) {
    Exit-WithError 'no manifest on stdin (expected JSON object with preFile and paths[])'
}

try {
    $cfg = $manifest | ConvertFrom-Json
} catch {
    Exit-WithError "manifest is not valid JSON: $($_.Exception.Message)"
}

if (-not $cfg.preFile) {
    Exit-WithError 'manifest.preFile is required'
}
if (-not (Test-Path -LiteralPath $cfg.preFile)) {
    Exit-WithError "manifest.preFile does not exist: $($cfg.preFile)"
}
if (-not $cfg.paths -or $cfg.paths.Count -eq 0) {
    Exit-WithError 'manifest.paths[] is required and must be non-empty'
}

$preJson = Get-Content -Raw -LiteralPath $cfg.preFile
try {
    $preObj = $preJson | ConvertFrom-Json
} catch {
    Exit-WithError "preFile is not valid JSON: $($_.Exception.Message)"
}

$preMap = @{}
foreach ($p in $preObj.PSObject.Properties) {
    $preMap[$p.Name] = [string]$p.Value
}

$changed   = @()
$added     = @()
$unchanged = 0
$seen      = @{}

foreach ($root in $cfg.paths) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -Recurse -File -LiteralPath $root | ForEach-Object {
        $seen[$_.FullName] = $true
        $cur = $null
        try {
            $cur = (Get-FileHash -Algorithm SHA1 -LiteralPath $_.FullName).Hash
        } catch {
            $cur = "UNHASHABLE:$($_.Exception.Message)"
        }

        if ($preMap.ContainsKey($_.FullName)) {
            if ($preMap[$_.FullName] -ne $cur) {
                $changed += [pscustomobject]@{
                    path     = $_.FullName
                    preHash  = $preMap[$_.FullName]
                    postHash = $cur
                }
            } else {
                $unchanged++
            }
        } else {
            $added += [pscustomobject]@{
                path     = $_.FullName
                postHash = $cur
            }
        }
    }
}

$removed = @()
foreach ($k in $preMap.Keys) {
    if (-not $seen.ContainsKey($k)) {
        $removed += [pscustomobject]@{
            path    = $k
            preHash = $preMap[$k]
        }
    }
}

[pscustomobject]@{
    status  = 'ok'
    preFile = $cfg.preFile
    counts  = [pscustomobject]@{
        changed   = $changed.Count
        added     = $added.Count
        removed   = $removed.Count
        unchanged = $unchanged
    }
    changed = $changed
    added   = $added
    removed = $removed
} | ConvertTo-Json -Depth 5 -Compress:$false
