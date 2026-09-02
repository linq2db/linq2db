<#
report-trx.ps1 - summarise MTP .trx results for a GitHub Actions run.

Stands in for Azure's PublishTestResults@2, which has no GitHub equivalent. Deliberately not a
third-party action: the repo's Actions policy allows GitHub-owned actions only, and a test reporter is
a poor place to widen that.

Writes a table to $GITHUB_STEP_SUMMARY and one ::error:: annotation per failed test, so failures are
visible on the run without opening a log.

Mirrors PublishTestResults@2's failTaskOnMissingResultsFile: no .trx at all exits non-zero. That guard
earns its keep - a leg whose test executable never ran produces no .trx, and without it the leg is
green having tested nothing. The same class of silent pass that an empty test matrix produces.

Exit codes: 0 = every test passed, 1 = at least one failed, 2 = no .trx found (or a bad argument).
The caller decides whether a failed test fails the step; the exit code just reports it.

Usage:
    pwsh -NoProfile -File Build/CI/report-trx.ps1 -ResultsDirectory TestResults -Title 'Lin s_SQLite'
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ResultsDirectory,
    # Prefixes the summary heading, so several legs in one run stay distinguishable.
    [string] $Title = 'Tests',
    # Cap on emitted annotations. GitHub drops annotations past ~10 per step anyway, and a mass failure
    # should not bury the summary table under hundreds of them.
    [int] $MaxAnnotations = 10
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ResultsDirectory)) {
    Write-Host "::error::report-trx: results directory '$ResultsDirectory' does not exist - no test run produced output"
    exit 2
}

$trx = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx' -Recurse -File)
if ($trx.Count -eq 0) {
    Write-Host "::error::report-trx: no .trx files under '$ResultsDirectory' - the test executable produced no results, so this leg tested nothing"
    exit 2
}

$rows = @()
$failures = @()

foreach ($file in $trx) {
    [xml] $doc = Get-Content -LiteralPath $file.FullName -Raw

    # MTP writes the VSTest schema, so counters are on ResultSummary/Counters.
    $counters = $doc.TestRun.ResultSummary.Counters
    $rows += [pscustomobject]@{
        File    = $file.Name
        Total   = [int] $counters.total
        Passed  = [int] $counters.passed
        Failed  = [int] $counters.failed
        Skipped = ([int] $counters.total - [int] $counters.executed)
    }

    foreach ($r in @($doc.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq 'Failed' })) {
        $failures += [pscustomobject]@{
            Test    = $r.testName
            Message = ($r.Output.ErrorInfo.Message -join ' ').Trim()
        }
    }
}

$totals = [pscustomobject]@{
    Total   = ($rows | Measure-Object Total   -Sum).Sum
    Passed  = ($rows | Measure-Object Passed  -Sum).Sum
    Failed  = ($rows | Measure-Object Failed  -Sum).Sum
    Skipped = ($rows | Measure-Object Skipped -Sum).Sum
}

$md = [System.Collections.Generic.List[string]]::new()
$md.Add("### $Title")
$md.Add('')
$md.Add('| Result file | Total | Passed | Failed | Skipped |')
$md.Add('|---|---:|---:|---:|---:|')
foreach ($r in $rows) {
    $md.Add("| $($r.File) | $($r.Total) | $($r.Passed) | $($r.Failed) | $($r.Skipped) |")
}
$md.Add("| **total** | **$($totals.Total)** | **$($totals.Passed)** | **$($totals.Failed)** | **$($totals.Skipped)** |")

if ($failures.Count -gt 0) {
    $md.Add('')
    $md.Add("#### Failed ($($failures.Count))")
    $md.Add('')
    foreach ($f in $failures) {
        # One line each, message truncated - the log has the full text.
        $msg = $f.Message -replace '\s+', ' '
        if ($msg.Length -gt 300) { $msg = $msg.Substring(0, 300) + '...' }
        $md.Add("- ``$($f.Test)`` - $msg")
    }
}

if ($Env:GITHUB_STEP_SUMMARY) {
    $md -join "`n" | Out-File -FilePath $Env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
$md -join "`n" | Write-Host

foreach ($f in @($failures | Select-Object -First $MaxAnnotations)) {
    $msg = ($f.Message -replace '\s+', ' ')
    if ($msg.Length -gt 300) { $msg = $msg.Substring(0, 300) + '...' }
    # ::error:: takes no newlines, hence the collapse above.
    Write-Host "::error title=$($f.Test)::$msg"
}
if ($failures.Count -gt $MaxAnnotations) {
    Write-Host "::warning::$($failures.Count - $MaxAnnotations) further failures are in the summary table and the log"
}

if ($totals.Failed -gt 0) { exit 1 }
exit 0
