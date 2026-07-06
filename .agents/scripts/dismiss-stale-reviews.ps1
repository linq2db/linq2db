<#
dismiss-stale-reviews.ps1 — dismiss lingering CHANGES_REQUESTED reviews on a PR
after follow-up commits addressed them (when GitHub's auto-dismissal didn't fire).

Codifies .agents/docs/pr-and-push.md -> "Stale CHANGES_REQUESTED reviews after
follow-up commits". Finds the numeric REST review ids (GraphQL ids don't work for
the PUT) and dismisses each with a non-empty message (empty -> HTTP 422). GLUE
ONLY — dismissing is a visible action on someone else's review, so the
ask-the-user gate stays with the agent/skill; run this only after confirmation.

  pwsh -NoProfile -File .agents/scripts/dismiss-stale-reviews.ps1 -Pr 5503
  pwsh -NoProfile -File .agents/scripts/dismiss-stale-reviews.ps1 -Pr 5503 -DryRun
#>
param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Repo    = 'linq2db/linq2db',
    [string]$Message = 'Stale',
    [switch]$DryRun
)

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'dismiss-stale-reviews'

if (-not $Message -or -not $Message.Trim()) { Exit-WithError 'message must be non-empty (GitHub rejects empty dismissal messages with HTTP 422)' }

$reviews = Invoke-GhJson @('api', "repos/$Repo/pulls/$Pr/reviews", '--paginate')
if (-not $reviews.ok) { Exit-WithError "gh api reviews failed: $($reviews.error)" }

$stale = @($reviews.data |
    Where-Object { $_.state -eq 'CHANGES_REQUESTED' } |
    ForEach-Object { [pscustomobject]@{ id = [long]$_.id; user = $_.user.login; commit_id = $_.commit_id; submitted_at = $_.submitted_at } })

if ($stale.Count -eq 0) {
    Write-JsonOutput ([pscustomobject]@{ ok = $true; pr = $Pr; found = 0; dismissed = @(); note = 'no CHANGES_REQUESTED reviews' })
    exit 0
}

if ($DryRun) {
    Write-JsonOutput ([pscustomobject]@{ ok = $true; pr = $Pr; found = $stale.Count; dryRun = $true; staleReviews = $stale })
    exit 0
}

$results = @()
foreach ($rv in $stale) {
    $d = Invoke-Gh @('api', '-X', 'PUT', "repos/$Repo/pulls/$Pr/reviews/$($rv.id)/dismissals", '-f', "message=$Message", '-f', 'event=DISMISS')
    $results += [pscustomobject]@{ id = $rv.id; user = $rv.user; dismissed = $d.ok; error = $d.error }
}

$allOk = -not ($results | Where-Object { -not $_.dismissed })
Write-JsonOutput ([pscustomobject]@{ ok = $allOk; pr = $Pr; found = $stale.Count; dismissed = $results })
