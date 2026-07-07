<#
analyzer-profile-build.ps1 — full Release rebuild with Roslyn analyzer
performance reports enabled, used by `/profile-analyzers` (and optionally
chained from `/release-deps` after an analyzer-package bump).

Invokes `dotnet build -t:Rebuild` with the four flags required to make
`/reportanalyzer` output reach the MSBuild log:

  -p:RunAnalyzersDuringBuild=true   # opt in to analyzer execution
  -p:ReportAnalyzer=true            # ask csc.exe to print the report
  -p:UseSharedCompilation=false     # bypass VBCSCompiler so report reaches stdout
  -v:detailed                        # MSBuild filters MessageImportance.Low at -v:normal

`-t:Rebuild` (instead of plain `Build`) is mandatory: incremental build
skips `CoreCompile` for up-to-date projects and emits no analyzer report
for them, leaving the ranking lopsided.

`-c Release` is mandatory too: Release is where `Directory.Build.props`
gates `RunAnalyzersDuringBuild=true` + `EnforceCodeStyleInBuild=true`.
Without it, code-style analyzers (IDE0039 etc.) don't fire and any
errors they would have surfaced ship to CI unnoticed.

Wall-clock cost on linq2db.slnx: 10-25 minutes. The skill's contract is
explicit user-confirmation before invoking; do not auto-launch.

Output (single JSON object on stdout):
  { ok, logPath, exitCode, elapsedMs }

Conventions: `.agents/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [string]   $SolutionPath = 'linq2db.slnx',
    [Parameter(Mandatory)][string] $LogPath,
    [string]   $Target = 'Rebuild',
    [string[]] $ExtraArgs = @()
)

$global:ScriptBaseName = 'analyzer-profile-build'
. (Join-Path $PSScriptRoot '_shared.ps1')

if (-not (Test-Path -LiteralPath $SolutionPath)) {
    Exit-WithError "Solution not found: $SolutionPath (CWD: $(Get-Location))"
}

$logDir = Split-Path -Parent $LogPath
if ($logDir -and -not (Test-Path -LiteralPath $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

# Shut down build servers so csc.exe runs fresh and the analyzer report
# reliably reaches the MSBuild log. VBCSCompiler reuses processes by default
# and may swallow the report into its compile-response object instead of
# writing it to stdout.
[Console]::Error.WriteLine("Shutting down build servers...")
& dotnet build-server shutdown | Out-Null

$buildArgs = @(
    'build', $SolutionPath,
    "-t:$Target",
    '-c', 'Release',
    '-p:RunAnalyzersDuringBuild=true',
    '-p:ReportAnalyzer=true',
    '-p:UseSharedCompilation=false',
    '-v:detailed'
) + $ExtraArgs

[Console]::Error.WriteLine("Starting build: dotnet $($buildArgs -join ' ')")
[Console]::Error.WriteLine("Log file:       $LogPath")
[Console]::Error.WriteLine("Expected wall-clock: 10-25 minutes on linq2db.slnx")

$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @buildArgs *> $LogPath
$exit = $LASTEXITCODE
$sw.Stop()

$status = if ($exit -eq 0) { 'succeeded' } else { "FAILED (exit $exit)" }
[Console]::Error.WriteLine(("Build {0} in {1}. Log: {2}" -f $status, $sw.Elapsed.ToString('hh\:mm\:ss'), $LogPath))

Write-JsonOutput @{
    ok        = ($exit -eq 0)
    action    = 'build'
    logPath   = (Resolve-Path -LiteralPath $LogPath).Path
    exitCode  = $exit
    elapsedMs = [int64]$sw.Elapsed.TotalMilliseconds
}
