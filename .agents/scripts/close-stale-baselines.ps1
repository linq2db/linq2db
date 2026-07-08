<#
close-stale-baselines.ps1 — close the stale baselines PR for a linq2db PR and
delete its branch, then prune the local baselines clone.

Codifies .agents/docs/pr-and-push.md -> "After test renames / moves / deletes:
clean up stale baselines" (also the CONFLICTING-baselines case). GLUE ONLY — the
TRIGGER decision (a follow-up actually renamed/moved/deleted a test, or master
moved and the baselines PR went CONFLICTING) and the user go-ahead for this
destructive action on a sibling repo stay with the agent/skill.

  pwsh -NoProfile -File .agents/scripts/close-stale-baselines.ps1 -Pr 5503
  pwsh -NoProfile -File .agents/scripts/close-stale-baselines.ps1 -Pr 5503 -DryRun

Resolves the baselines PR on linq2db.baselines keyed to head `baselines/pr_<n>`,
closes it with an explanatory comment, deletes the branch ref (treating an
already-gone ref as success), and `git fetch --prune`s the local clone.
#>
param(
    [Parameter(Mandatory)][int]$Pr,
    [string]$Comment,
    [string]$Repo           = 'linq2db/linq2db.baselines',
    [string]$BaselinesClone = '../linq2db.baselines',
    [switch]$DryRun
)

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'close-stale-baselines'

$head = "baselines/pr_$Pr"
if (-not $Comment) {
    $Comment = "Closing — PR linq2db/linq2db#$Pr's follow-up commits renamed / moved test(s) (or master moved and these baselines became CONFLICTING); they are stale. The next CI baseline-regeneration run will produce fresh baselines under the up-to-date names."
}

$list = Invoke-GhJson @('pr', 'list', '--repo', $Repo, '--head', $head, '--state', 'all', '--json', 'number,url,state')
if (-not $list.ok) { Exit-WithError "gh pr list failed: $($list.error)" }
$prs = @($list.data)

if ($prs.Count -eq 0) {
    Write-JsonOutput ([pscustomobject]@{ ok = $true; pr = $Pr; head = $head; found = $false; action = 'none'; reason = 'no baselines PR for this head' })
    exit 0
}

if ($DryRun) {
    Write-JsonOutput ([pscustomobject]@{ ok = $true; pr = $Pr; head = $head; found = $true; dryRun = $true; baselinesPrs = $prs; comment = $Comment })
    exit 0
}

$results = @()
foreach ($bp in $prs) {
    $num = [long]$bp.number
    $entry = [pscustomobject]@{ number = $num; url = $bp.url; priorState = $bp.state; closed = $false; branchDeleted = $false; error = $null }

    if ($bp.state -eq 'OPEN') {
        $close = Invoke-Gh @('pr', 'close', "$num", '--repo', $Repo, '--comment', $Comment)
        if ($close.ok) { $entry.closed = $true } else { $entry.error = "close failed: $($close.error)" }
    } else {
        $entry.closed = $true   # already closed
    }

    $del = Invoke-Gh @('api', '-X', 'DELETE', "repos/$Repo/git/refs/heads/$head")
    if ($del.ok) { $entry.branchDeleted = $true }
    elseif ($del.error -match 'does not exist|Not Found|422') { $entry.branchDeleted = $true }   # already gone
    else { $entry.error = if ($entry.error) { "$($entry.error); branch delete failed: $($del.error)" } else { "branch delete failed: $($del.error)" } }

    $results += $entry
}

if (Test-Path -LiteralPath $BaselinesClone) {
    $p = Invoke-Git @('fetch', 'origin', '--prune') -WorkingDirectory $BaselinesClone
    $prune = [pscustomobject]@{ attempted = $true; ok = $p.ok; error = $p.error }
} else {
    $prune = [pscustomobject]@{ attempted = $false; reason = "local clone not found at $BaselinesClone" }
}

$allOk = -not ($results | Where-Object { $_.error })
Write-JsonOutput ([pscustomobject]@{ ok = $allOk; pr = $Pr; head = $head; found = $true; baselines = $results; prune = $prune })
