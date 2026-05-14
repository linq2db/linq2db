<#
wait-for-review.ps1 — poll `gh api /repos/.../pulls/<n>/reviews` until a new
review from a matching bot login appears, or until -MaxWaitSec elapses.

Used by `/copilot-loop` step 11 to bridge between fix rounds without burning
agent context on per-poll Bash calls. One foreground invocation; the
helper's internal `Start-Sleep` handles the polling cadence.

Inputs:

  -Pr <int>                                  (required) target PR number
  -SinceSubmittedAt <iso8601>                (required) last seen review timestamp;
                                             a review with submittedAt > this counts as new
  -BotLoginRegex <string>                    (required) case-insensitive regex on .user.login
                                             (typical: 'copilot' — matches both
                                             'copilot-pull-request-reviewer[bot]' and 'Copilot')
  -Owner <string>                            optional, default 'linq2db'
  -Repo <string>                             optional, default 'linq2db'
  -MaxWaitSec <int>                          optional, default 600 (10 minutes)
  -PollIntervalSec <int>                     optional, default 30
  -ManifestFile <path>                       alternative — same fields as JSON manifest

Output (stdout, JSON, exit 0 either way):
  {
    ok: true,
    found: true|false,
    waitedSec: <int>,
    polled: <int>,
    newReview: {
      id, submittedAt, state, commitId, user
    } | null
  }

Exit codes:
  0  found OR timed out cleanly (caller inspects `found`)
  1  bad input / fatal error

Permission profile: one allowlist match for the script invocation;
internal `gh api` calls go through Invoke-GhJson (same as pr-context.ps1).

Conventions: `.claude/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [int]$Pr = 0,
    [string]$SinceSubmittedAt,
    [string]$BotLoginRegex,
    [string]$Owner,
    [string]$Repo,
    [int]$MaxWaitSec = 0,
    [int]$PollIntervalSec = 0,
    [string]$ManifestFile
)

$global:ScriptBaseName = 'wait-for-review'
. "$PSScriptRoot/_shared.ps1"

# Accept either named parameters or a manifest. Named takes precedence when set.
$m = if ($Pr -gt 0) {
    [PSCustomObject]@{
        pr                = $Pr
        sinceSubmittedAt  = $SinceSubmittedAt
        botLoginRegex     = $BotLoginRegex
        owner             = $Owner
        repo              = $Repo
        maxWaitSec        = $MaxWaitSec
        pollIntervalSec   = $PollIntervalSec
    }
} else {
    Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
}

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) {
    Exit-WithError 'pr (positive integer) required'
}
$pr = [int]$m.pr

if (-not $m.sinceSubmittedAt) {
    Exit-WithError 'sinceSubmittedAt (ISO-8601 timestamp) required'
}
$sinceSubmittedAt = [string]$m.sinceSubmittedAt
# Parse to DateTimeOffset for comparison. GitHub returns Z-suffixed UTC.
$sinceDto = $null
try {
    $sinceDto = [System.DateTimeOffset]::Parse($sinceSubmittedAt, $null,
        [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
        [System.Globalization.DateTimeStyles]::AdjustToUniversal)
} catch {
    Exit-WithError "sinceSubmittedAt must be a parseable ISO-8601 timestamp (got '$sinceSubmittedAt')"
}

if (-not $m.botLoginRegex) {
    Exit-WithError 'botLoginRegex (regex string) required'
}
$botLoginRegex = [string]$m.botLoginRegex

$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$repoFull = "$owner/$repo"

$maxWait = if ((Test-IsInteger $m.maxWaitSec) -and [long]$m.maxWaitSec -gt 0) {
    [int]$m.maxWaitSec
} else { 600 }
$pollInterval = if ((Test-IsInteger $m.pollIntervalSec) -and [long]$m.pollIntervalSec -gt 0) {
    [int]$m.pollIntervalSec
} else { 30 }
if ($pollInterval -gt $maxWait) { $pollInterval = $maxWait }

# Poll loop. Each iteration: fetch the PR's reviews, pick the latest one
# matching the bot regex, compare submittedAt against sinceDto.
$start = [System.Diagnostics.Stopwatch]::StartNew()
$polled = 0
$found = $null
$lastError = $null

while ($start.Elapsed.TotalSeconds -lt $maxWait) {
    $polled++
    try {
        $resp = Invoke-GhJson @(
            'api',"repos/$repoFull/pulls/$pr/reviews",'--paginate'
        )
    } catch {
        # Transient gh / network errors don't end the loop — just record and retry.
        $lastError = $_.Exception.Message
        Start-Sleep -Seconds $pollInterval
        continue
    }
    if (-not $resp.ok) {
        $lastError = $resp.error
        Start-Sleep -Seconds $pollInterval
        continue
    }

    # Filter to bot-authored reviews with submittedAt > sinceDto.
    $candidates = @()
    foreach ($r in @($resp.data)) {
        $login = if ($r.user -and $r.user.login) { [string]$r.user.login } else { '' }
        if (-not $login) { continue }
        if ($login -inotmatch $botLoginRegex) { continue }
        $submittedRaw = [string]$r.submitted_at
        if (-not $submittedRaw) { continue }
        try {
            $sub = [System.DateTimeOffset]::Parse($submittedRaw, $null,
                [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
                [System.Globalization.DateTimeStyles]::AdjustToUniversal)
        } catch { continue }
        if ($sub -gt $sinceDto) {
            $candidates += [pscustomobject]@{ raw = $r; submittedAt = $sub }
        }
    }
    if ($candidates.Count -gt 0) {
        $pick = $candidates | Sort-Object -Property submittedAt -Descending | Select-Object -First 1
        $r = $pick.raw
        # ConvertFrom-Json auto-converts ISO-8601 timestamp strings to [DateTime],
        # which then stringifies in host-locale format. Re-emit as round-trip
        # ISO-8601 ("o" format) so consumers can pass it back to -SinceSubmittedAt
        # on the next round.
        $submittedDto = $pick.submittedAt
        $found = [ordered]@{
            id           = [long]$r.id
            submittedAt  = $submittedDto.UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ssZ')
            state        = [string]$r.state
            commitId     = [string]$r.commit_id
            user         = [string]$r.user.login
        }
        break
    }

    # Compute remaining time. Sleep min(pollInterval, remaining).
    $remaining = $maxWait - [int]$start.Elapsed.TotalSeconds
    if ($remaining -le 0) { break }
    $sleepFor = [Math]::Min($pollInterval, $remaining)
    Start-Sleep -Seconds $sleepFor
}

$start.Stop()

Write-JsonOutput @{
    ok        = $true
    found     = ($null -ne $found)
    waitedSec = [int]$start.Elapsed.TotalSeconds
    polled    = $polled
    newReview = $found
    error     = $lastError
}
