#!/usr/bin/env pwsh
# test-status.ps1 — print a one-line summary of an in-progress (or finished) linq2db test run.
#
# Reads the JSON heartbeat written by Tests/Base/TestProgressReporter.cs when a run is launched with
# the LINQ2DB_TEST_PROGRESS environment variable set. By default it picks the most recently updated
# .build/.claude/test-progress.*.json file (the active run); pass -Path to target a specific file.
#
# Usage:
#   pwsh -NoProfile -File .claude/scripts/test-status.ps1
#   pwsh -NoProfile -File .claude/scripts/test-status.ps1 -Path .build/.claude/test-progress.net10.0.1234.json
#   pwsh -NoProfile -File .claude/scripts/test-status.ps1 -Raw      # emit the raw JSON instead of a summary

[CmdletBinding()]
param(
	[string] $Path,
	[switch] $Raw
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..' '..')

if (-not $Path) {
	$dir = Join-Path $repoRoot '.build/.claude'
	if (-not (Test-Path $dir)) {
		Write-Output "No test-progress directory yet ($dir). Start a run with LINQ2DB_TEST_PROGRESS=1."
		return
	}

	$latest = Get-ChildItem -Path $dir -Filter 'test-progress.*.json' -File -ErrorAction SilentlyContinue |
		Sort-Object LastWriteTimeUtc -Descending |
		Select-Object -First 1

	if (-not $latest) {
		Write-Output "No test-progress.*.json found in $dir. Start a run with LINQ2DB_TEST_PROGRESS=1."
		return
	}

	$Path = $latest.FullName
}

if (-not (Test-Path $Path)) {
	Write-Output "File not found: $Path"
	return
}

# Reader may catch a write in flight; retry briefly on a parse failure.
$p = $null
for ($i = 0; $i -lt 5; $i++) {
	try { $p = Get-Content -Raw -Path $Path | ConvertFrom-Json; break }
	catch { Start-Sleep -Milliseconds 100 }
}

if (-not $p) {
	Write-Output "Could not parse $Path (still being written?)."
	return
}

if ($Raw) {
	Get-Content -Raw -Path $Path
	return
}

function Format-Duration([double] $seconds) {
	$ts = [TimeSpan]::FromSeconds($seconds)
	if ($ts.TotalHours -ge 1) { return ('{0}h{1:00}m' -f [int]$ts.TotalHours, $ts.Minutes) }
	if ($ts.TotalMinutes -ge 1) { return ('{0}m{1:00}s' -f [int]$ts.TotalMinutes, $ts.Seconds) }
	return ('{0:0}s' -f $ts.TotalSeconds)
}

$total     = [long]$p.total
$completed = [long]$p.completed
$pct       = if ($total -gt 0) { [math]::Round(100.0 * $completed / $total, 1) } else { $null }
$state     = if ($p.done) { 'DONE' } else { 'RUNNING' }
$elapsed   = Format-Duration ([double]$p.elapsedSec)
$eta       = if ($null -ne $p.etaSec) { Format-Duration ([double]$p.etaSec) } else { 'n/a' }
$rate      = [math]::Round([double]$p.testsPerSec, 1)

$progress  = if ($null -ne $pct) { "$completed/$total ($pct%)" } else { "$completed/?" }
$current   = if ($p.currentTest) { $p.currentTest } else { '-' }

$line = "[$state $($p.tfm)] $progress " +
		"| pass $($p.passed) / fail $($p.failed) / skip $($p.skipped) " +
		"| $rate t/s | elapsed $elapsed | eta $eta " +
		"| now: $current"

Write-Output $line

if (([long]$p.failed) -gt 0 -and $p.recentFailures) {
	Write-Output "recent failures:"
	foreach ($f in $p.recentFailures) {
		$msg = ($f.message -replace '\s+', ' ')
		if ($msg.Length -gt 120) { $msg = $msg.Substring(0, 120) + '...' }
		Write-Output "  - $($f.test): $msg"
	}
}
