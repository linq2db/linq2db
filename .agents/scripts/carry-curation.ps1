<#
carry-curation.ps1 — carry `.agents/` curation across a branch switch, or verify
a push range carries no `.agents/` diff.

Codifies .agents/docs/worktree.md -> "Carrying `.agents/` curation across branch
switches" (the fetch -> checkout glue, and the push-time verify). GLUE ONLY — the
DECISION to carry (this is a working branch, not master/release) and the
explicit-pathspec staging discipline stay with the agent/skill.

  pwsh -NoProfile -File .agents/scripts/carry-curation.ps1            # carry
  pwsh -NoProfile -File .agents/scripts/carry-curation.ps1 -Verify    # push-time check

Carry mode: fetch origin <curation>, `git checkout origin/<curation> -- .agents/`,
then unstage so the carried files are working-tree-only modifications (never
staged, per the rule). Refuses on master/release (they reflect merged state).
Verify mode: report any commits in origin/<branch>..HEAD that touch `.agents/`
and any staged `.agents/` paths — both violate "never commit curation on a
working branch".
#>
param(
    [switch]$Verify,
    [string]$Branch,
    [string]$Curation = 'infra/agents-curation',
    [string]$Remote   = 'origin'
)

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'carry-curation'

function Get-CurrentBranch {
    $r = Invoke-Git @('rev-parse', '--abbrev-ref', 'HEAD')
    if (-not $r.ok) { Exit-WithError "cannot resolve current branch: $($r.error)" }
    return $r.stdout.Trim()
}

if (-not $Branch) { $Branch = Get-CurrentBranch }

if ($Verify) {
    $range = "$Remote/$Branch..HEAD"
    $log = Invoke-Git @('log', '--oneline', $range, '--', '.agents/')
    $commits = @()
    $rangeNote = $null
    if ($log.ok) {
        if ($log.stdout.Trim()) { $commits = @($log.stdout.Trim() -split "`n") }
    } else {
        $rangeNote = "could not evaluate range ($($log.error.Trim())) — branch likely not pushed yet"
    }

    $staged = Invoke-Git @('diff', '--cached', '--name-only', '--', '.agents/')
    $stagedPaths = @()
    if ($staged.ok -and $staged.stdout.Trim()) { $stagedPaths = @($staged.stdout.Trim() -split "`n") }

    $clean = ($commits.Count -eq 0 -and $stagedPaths.Count -eq 0)
    Write-JsonOutput ([pscustomobject]@{
        ok                     = $true
        mode                   = 'verify'
        branch                 = $Branch
        range                  = $range
        clean                  = $clean
        committedAgentsChanges = $commits
        stagedAgentsPaths      = $stagedPaths
        rangeNote              = $rangeNote
    })
    exit 0
}

# carry mode
if ($Branch -eq 'master' -or $Branch -eq 'release') {
    Write-JsonOutput ([pscustomobject]@{
        ok = $true; mode = 'carry'; branch = $Branch; action = 'skipped'
        reason = 'master/release reflect merged state — curation is not carried here'
    })
    exit 0
}

$fetch = Invoke-Git @('fetch', $Remote, $Curation)
if (-not $fetch.ok) { Exit-WithError "git fetch $Remote $Curation failed: $($fetch.error)" }

$checkout = Invoke-Git @('checkout', "$Remote/$Curation", '--', '.agents/')
if (-not $checkout.ok) { Exit-WithError "git checkout $Remote/$Curation -- .agents/ failed: $($checkout.error)" }

# `git checkout <tree> -- <path>` stages the result; the rule wants the carry to
# be working-tree-only (unstaged) modifications, so reset the index for .agents/.
$unstage = Invoke-Git @('restore', '--staged', '--', '.agents/')

$status = Invoke-Git @('status', '--porcelain', '--', '.agents/')
$changed = @()
if ($status.ok -and $status.stdout.Trim()) {
    $changed = @($status.stdout.Trim() -split "`n" | ForEach-Object { $_.Trim() })
}

Write-JsonOutput ([pscustomobject]@{
    ok           = $true
    mode         = 'carry'
    branch       = $Branch
    action       = 'carried'
    source       = "$Remote/$Curation"
    unstaged     = $unstage.ok
    changedCount = $changed.Count
    changed      = $changed
})
