#!/usr/bin/env pwsh
<#
Snapshot a set of baseline-tree paths by SHA1-hashing every file.

Why this wrapper exists
-----------------------
The `/test` skill needs a before-snapshot of `BaselinesPath/<Provider>/…`
so it can diff the post-run state and surface which baseline files the
run touched. Walking the tree and hashing every file is a small job, but
doing it through raw Bash fires one permission prompt per directory
traversal and forces the caller to stream hashes back through stdout
parsing. This script answers the whole job in one call:

    Bash(pwsh -NoProfile -File .claude/scripts/snap-baselines.ps1:*)

Input (stdin, JSON)
-------------------
  {
    "paths":   ["c:/GitHub/linq2db.bls/Firebird.4", ...],  // required, non-empty
    "outFile": ".build/.claude/baselines-pre-<run>.json"   // required — hash map written here
  }

Each entry in `paths` is either a directory (walked recursively for files)
or a single file. Missing paths are emitted in the output under `missing`
rather than failing the call — callers often pass a list that includes
optional providers.

Output (stdout, single JSON object):

  {
    "status":    "ok",
    "outFile":   "...",
    "fileCount": 1234,
    "rootCount": 2,
    "missing":   ["c:/GitHub/linq2db.bls/NonExistent"]
  }

The `outFile` receives a flat `{ "<absolute-path>": "<sha1-hex>", ... }`
map that `diff-baselines.ps1` consumes.
#>

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'snap-baselines'

$manifest = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($manifest)) {
    Exit-WithError 'no manifest on stdin (expected JSON object with paths[] and outFile)'
}

try {
    $cfg = $manifest | ConvertFrom-Json
} catch {
    Exit-WithError "manifest is not valid JSON: $($_.Exception.Message)"
}

if (-not $cfg.paths -or $cfg.paths.Count -eq 0) {
    Exit-WithError 'manifest.paths[] is required and must be non-empty'
}
if (-not $cfg.outFile) {
    Exit-WithError 'manifest.outFile is required'
}

$hashMap  = [ordered]@{}
$missing  = @()
$rootSeen = 0

foreach ($p in $cfg.paths) {
    if (-not (Test-Path -LiteralPath $p)) {
        $missing += $p
        continue
    }
    $rootSeen++
    $item = Get-Item -LiteralPath $p
    if ($item.PSIsContainer) {
        Get-ChildItem -Recurse -File -LiteralPath $p | ForEach-Object {
            try {
                $h = (Get-FileHash -Algorithm SHA1 -LiteralPath $_.FullName).Hash
                $hashMap[$_.FullName] = $h
            } catch {
                $hashMap[$_.FullName] = "UNHASHABLE:$($_.Exception.Message)"
            }
        }
    } else {
        try {
            $h = (Get-FileHash -Algorithm SHA1 -LiteralPath $item.FullName).Hash
            $hashMap[$item.FullName] = $h
        } catch {
            $hashMap[$item.FullName] = "UNHASHABLE:$($_.Exception.Message)"
        }
    }
}

$outDir = Split-Path -Parent $cfg.outFile
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    [void](New-Item -ItemType Directory -Path $outDir -Force)
}

$hashMap | ConvertTo-Json -Depth 3 | Set-Content -Encoding utf8 -LiteralPath $cfg.outFile

[pscustomobject]@{
    status    = 'ok'
    outFile   = $cfg.outFile
    fileCount = $hashMap.Count
    rootCount = $rootSeen
    missing   = $missing
} | ConvertTo-Json -Depth 3 -Compress:$false
