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

Conventions: `.agents/docs/script-authoring.md`.
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
# Normalise sinceSubmittedAt to a UTC DateTimeOffset for comparison.
# ConvertFrom-Json may hand us [DateTime] (auto-converted from an ISO-8601-
# looking string, Kind usually Unspecified or Local) or [DateTimeOffset]; the
# named-param caller hands us [string]. Casting [DateTime] to [string] uses
# the current culture, which then makes Parse(... $null ...) format-sensitive
# on non-en-US hosts. Handle each shape explicitly and use InvariantCulture
# for the string path.
$rawSince = $m.sinceSubmittedAt
$sinceDto = $null
if ($rawSince -is [System.DateTimeOffset]) {
    $sinceDto = $rawSince.ToUniversalTime()
} elseif ($rawSince -is [System.DateTime]) {
    # Kind-sensitive: Local must convert via ToUniversalTime() (apply host
    # offset). Utc is already correct. Unspecified is the typical
    # ConvertFrom-Json result for a Z-suffixed string — treat as UTC since
    # that's the GitHub API convention.
    $sinceDto = if ($rawSince.Kind -eq [System.DateTimeKind]::Local) {
        [System.DateTimeOffset]::new($rawSince).ToUniversalTime()
    } else {
        [System.DateTimeOffset]::new(
            [System.DateTime]::SpecifyKind($rawSince, [System.DateTimeKind]::Utc))
    }
} else {
    $sinceStr = [string]$rawSince
    try {
        $sinceDto = [System.DateTimeOffset]::Parse(
            $sinceStr,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
            [System.Globalization.DateTimeStyles]::AdjustToUniversal)
    } catch {
        Exit-WithError "sinceSubmittedAt must be a parseable ISO-8601 timestamp (got '$sinceStr')"
    }
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

function Invoke-ClampedSleep {
    param([int]$Interval, [int]$Elapsed, [int]$Max)
    $remaining = $Max - $Elapsed
    if ($remaining -le 0) { return $false }
    Start-Sleep -Seconds ([Math]::Min($Interval, $remaining))
    return $true
}

while ($start.Elapsed.TotalSeconds -lt $maxWait) {
    $polled++
    try {
        $resp = Invoke-GhJson @(
            'api',"repos/$repoFull/pulls/$pr/reviews",'--paginate'
        )
    } catch {
        # Transient gh / network errors don't end the loop — record, sleep
        # (clamped to remaining time so a late-cycle error can't overshoot
        # MaxWaitSec by up to one interval), retry.
        $lastError = $_.Exception.Message
        if (-not (Invoke-ClampedSleep -Interval $pollInterval -Elapsed ([int]$start.Elapsed.TotalSeconds) -Max $maxWait)) { break }
        continue
    }
    if (-not $resp.ok) {
        $lastError = $resp.error
        if (-not (Invoke-ClampedSleep -Interval $pollInterval -Elapsed ([int]$start.Elapsed.TotalSeconds) -Max $maxWait)) { break }
        continue
    }

    # Filter to bot-authored reviews with submittedAt > sinceDto.
    # ConvertFrom-Json may materialise submitted_at as [DateTime] (Kind
    # usually Unspecified or Local) rather than the raw [string], so casting
    # straight to [string] and parsing with $null IFormatProvider is
    # locale-dependent. Handle each shape explicitly; use InvariantCulture on
    # the string path.
    $candidates = @()
    foreach ($r in @($resp.data)) {
        $login = if ($r.user -and $r.user.login) { [string]$r.user.login } else { '' }
        if (-not $login) { continue }
        if ($login -inotmatch $botLoginRegex) { continue }
        $rawTs = $r.submitted_at
        if (-not $rawTs) { continue }
        $sub = $null
        if ($rawTs -is [System.DateTimeOffset]) {
            $sub = $rawTs.ToUniversalTime()
        } elseif ($rawTs -is [System.DateTime]) {
            # Same Kind-sensitive handling as sinceSubmittedAt above —
            # SpecifyKind alone misreads Local wall-clock as UTC.
            $sub = if ($rawTs.Kind -eq [System.DateTimeKind]::Local) {
                [System.DateTimeOffset]::new($rawTs).ToUniversalTime()
            } else {
                [System.DateTimeOffset]::new(
                    [System.DateTime]::SpecifyKind($rawTs, [System.DateTimeKind]::Utc))
            }
        } else {
            $rawStr = [string]$rawTs
            if (-not $rawStr) { continue }
            try {
                $sub = [System.DateTimeOffset]::Parse(
                    $rawStr,
                    [System.Globalization.CultureInfo]::InvariantCulture,
                    [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
                    [System.Globalization.DateTimeStyles]::AdjustToUniversal)
            } catch { continue }
        }
        if ($sub -gt $sinceDto) {
            $candidates += [pscustomobject]@{ raw = $r; submittedAt = $sub }
        }
    }
    if ($candidates.Count -gt 0) {
        $pick = $candidates | Sort-Object -Property submittedAt -Descending | Select-Object -First 1
        $r = $pick.raw
        # Re-emit as round-trip ISO-8601 ("o" format on the UTC DateTime gives
        # Z-suffix with sub-second precision) so consumers can pass it back to
        # -SinceSubmittedAt on the next round without losing precision —
        # otherwise the next poll could miss reviews that landed within the
        # same second as the previous round's pick.
        $submittedDto = $pick.submittedAt
        $found = [ordered]@{
            id           = [long]$r.id
            submittedAt  = $submittedDto.UtcDateTime.ToString('o', [System.Globalization.CultureInfo]::InvariantCulture)
            state        = [string]$r.state
            commitId     = [string]$r.commit_id
            user         = [string]$r.user.login
        }
        break
    }

    if (-not (Invoke-ClampedSleep -Interval $pollInterval -Elapsed ([int]$start.Elapsed.TotalSeconds) -Max $maxWait)) { break }
}

$start.Stop()

Write-JsonOutput @{
    ok        = $true
    found     = ($null -ne $found)
    waitedSec = [int]$start.Elapsed.TotalSeconds
    polled    = $polled
    newReview = $found
    # Surface the last transient-error context only when the loop ended in
    # the timeout / not-found path. On a successful find, an earlier
    # transient failure is no longer relevant — emit null so callers don't
    # mistake a recovered run for a partially-failed one.
    error     = if ($null -ne $found) { $null } else { $lastError }
}
