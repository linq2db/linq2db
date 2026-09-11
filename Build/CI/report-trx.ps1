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

# --retry-failed-tests writes each attempt as its own .trx under Retries/<id>/<attempt>/, holding only
# the tests it re-ran, next to the full run at the top of the results directory. Summing them all
# counts a test that failed and then passed as a failure, so every retry leg went red while its own
# test steps exited 0 - the three Oracle legs on GitHub run 33946658182.
#
# So group by file name, which is suite+TFM, and replay the attempts in order: a test's outcome is the
# one from the last attempt that contains it. Attempt order is the numeric directory under Retries/,
# with the top-level file first.
$attemptOrder = {
    param($file)
    if ($file.FullName -match '[/\\]Retries[/\\][^/\\]+[/\\](\d+)[/\\]') { [int] $Matches[1] } else { 0 }
}

foreach ($group in $trx | Group-Object Name) {
    $outcome = [ordered]@{}   # test name -> last outcome seen
    $message = @{}            # test name -> failure message from that attempt
    $baseCounters = $null

    foreach ($file in $group.Group | Sort-Object @{ Expression = $attemptOrder }) {
        [xml] $doc = Get-Content -LiteralPath $file.FullName -Raw
        # MTP writes the VSTest schema, so counters are on ResultSummary/Counters.
        if ($null -eq $baseCounters) { $baseCounters = $doc.TestRun.ResultSummary.Counters }

        foreach ($r in @($doc.TestRun.Results.UnitTestResult)) {
            $outcome[$r.testName] = $r.outcome
            $message[$r.testName] = ($r.Output.ErrorInfo.Message -join ' ').Trim()
        }
    }

    # Anything that is not Passed and not NotExecuted counts as a failure - Error, Timeout, Aborted,
    # Inconclusive and the rest. Classifying by outcome rather than deriving passed as a remainder,
    # which would have folded every one of those into the passed column and exited 0.
    $stillFailed = @($outcome.Keys | Where-Object { $outcome[$_] -notin @('Passed', 'NotExecuted') })
    $skipped     = @($outcome.Keys | Where-Object { $outcome[$_] -eq 'NotExecuted' }).Count
    $total       = $outcome.Count

    # The counters summarise the same rows, so a disagreement means one of the two is being read
    # wrongly - say so rather than quietly reporting a different number from Azure's.
    if ($null -ne $baseCounters -and [int] $baseCounters.total -ne $total) {
        Write-Host "::warning::report-trx: $($group.Name) lists $total results but its counters say $([int] $baseCounters.total)"
    }

    $rows += [pscustomobject]@{
        File     = $group.Name
        Attempts = $group.Count
        Total    = $total
        Passed   = $total - $skipped - $stillFailed.Count
        Failed   = $stillFailed.Count
        Skipped  = $skipped
    }

    foreach ($name in $stillFailed) {
        $failures += [pscustomobject]@{ Test = $name; Message = "[$($outcome[$name])] $($message[$name])".Trim() }
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
$md.Add('| Result file | Attempts | Total | Passed | Failed | Skipped |')
$md.Add('|---|---:|---:|---:|---:|---:|')
foreach ($r in $rows) {
    $md.Add("| $($r.File) | $($r.Attempts) | $($r.Total) | $($r.Passed) | $($r.Failed) | $($r.Skipped) |")
}
$md.Add("| **total** | | **$($totals.Total)** | **$($totals.Passed)** | **$($totals.Failed)** | **$($totals.Skipped)** |")

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

# A workflow command's data and its property values need escaping, and property values need more of
# it: an unescaped comma ends the property, so a name like Method("a","b") - any multi-argument
# TestCase - would silently truncate or void the annotation.
function Get-EscapedData([string] $value) {
    $value -replace '%', '%25' -replace "`r", '%0D' -replace "`n", '%0A'
}
function Get-EscapedProperty([string] $value) {
    (Get-EscapedData $value) -replace ':', '%3A' -replace ',', '%2C'
}

foreach ($f in @($failures | Select-Object -First $MaxAnnotations)) {
    $msg = ($f.Message -replace '\s+', ' ')
    if ($msg.Length -gt 300) { $msg = $msg.Substring(0, 300) + '...' }
    Write-Host "::error title=$(Get-EscapedProperty $f.Test)::$(Get-EscapedData $msg)"
}
if ($failures.Count -gt $MaxAnnotations) {
    Write-Host "::warning::$($failures.Count - $MaxAnnotations) further failures are in the summary table and the log"
}

if ($totals.Failed -gt 0) { exit 1 }
exit 0
