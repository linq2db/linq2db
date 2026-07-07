<#
.SYNOPSIS
  Normalize + apply a batch of KB-INDEXER envelope files (the /kb-refresh capture loop).

.DESCRIPTION
  For each file matching the manifest `glob`, this script:
    1. Normalizes the stray closing marker `=== END KB-INDEXER OUTPUT v1 ===`
       -> `=== END KB-INDEXER OUTPUT ===` (indexer agents frequently emit the
       `v1` variant, which kb-state.ps1 apply-fences rejects as "envelope not found").
    2. Runs `kb-state.ps1 apply-fences` on it.
    3. Collects a per-file result line { file, ok, artifacts, patches, gate }.

  Applies SEQUENTIALLY by design: issues/prs/discussions INDEX-PATCH envelopes all
  target one shared `github/*-index.json`, and concurrent apply races/loses patches.
  apply-fences itself is cheap (the agents are the slow part), so sequential apply of
  a whole batch costs seconds and sidesteps the shared-index race entirely. Per-area
  ARTIFACT envelopes (INDEX.md / issues.md) would be parallel-safe, but sequential is
  used uniformly for simplicity and safety.

  Invoked by hand ~7x during a single full /kb-refresh sweep before being codified.

.INPUT (JSON manifest via -ManifestFile or stdin)
  { "glob": ".build/.agents/kb-refresh-arch-*.txt" }   # required
  { "glob": "...", "deleteApplied": true }              # optional: rm each file after ok apply

.OUTPUT (JSON)
  { "ok": <all applied cleanly>, "count": N, "results": [ {file, ok, artifacts, patches, gate, gateFailures?}, ... ] }
#>
param([string]$ManifestFile)

$ErrorActionPreference = 'Stop'

if ($ManifestFile) {
    $raw = [System.IO.File]::ReadAllText($ManifestFile, [System.Text.UTF8Encoding]::new($false))
} else {
    $raw = [Console]::In.ReadToEnd()
}
$m = $raw | ConvertFrom-Json
if (-not $m.glob) { Write-Output (@{ ok = $false; error = 'glob required' } | ConvertTo-Json -Compress); exit 1 }

$stateScript = Join-Path $PSScriptRoot 'kb-state.ps1'
if (-not (Test-Path $stateScript)) { Write-Output (@{ ok = $false; error = "kb-state.ps1 not found next to this script" } | ConvertTo-Json -Compress); exit 1 }

$files = @(Get-ChildItem -Path $m.glob -File -ErrorAction SilentlyContinue | Sort-Object Name)
$results = @()
$allOk = $true

foreach ($f in $files) {
    # 1. normalize the stray v1 closing marker in place
    $content = [System.IO.File]::ReadAllText($f.FullName, [System.Text.UTF8Encoding]::new($false))
    $normalized = $content.Replace('=== END KB-INDEXER OUTPUT v1 ===', '=== END KB-INDEXER OUTPUT ===')
    if ($normalized -ne $content) {
        [System.IO.File]::WriteAllText($f.FullName, $normalized, [System.Text.UTF8Encoding]::new($false))
    }

    # 2. apply-fences (sequential — shared-index safe)
    $applyManifest = @{ op = 'apply-fences'; agentOutputFile = $f.FullName } | ConvertTo-Json -Compress
    $out = $applyManifest | & pwsh -NoProfile -File $stateScript 2>&1 | Out-String

    try {
        $r = $out | ConvertFrom-Json
        $gate = if ($r.gateFailures) { @($r.gateFailures).Count } else { 0 }
        $arts = if ($r.artifacts) { @($r.artifacts).Count } else { 0 }
        $pat = if ($r.indexPatches) { @($r.indexPatches).Count } else { 0 }
        $entry = [ordered]@{ file = $f.Name; ok = [bool]$r.ok; artifacts = $arts; patches = $pat; gate = $gate }
        if ($gate -gt 0) { $entry.gateFailures = @($r.gateFailures) }
        if (-not $r.ok) { $allOk = $false }
        if ($r.ok -and $m.deleteApplied) { Remove-Item $f.FullName -Force }
    } catch {
        $entry = [ordered]@{ file = $f.Name; ok = $false; error = "parse failed: $($out.Substring(0, [Math]::Min(300, $out.Length)))" }
        $allOk = $false
    }
    $results += [pscustomobject]$entry
}

Write-Output ([pscustomobject]@{ ok = $allOk; count = $files.Count; results = $results } | ConvertTo-Json -Depth 8)
