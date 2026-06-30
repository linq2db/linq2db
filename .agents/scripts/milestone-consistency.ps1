#!/usr/bin/env pwsh
<#
milestone-consistency.ps1 — keep a PR and the issues it closes on the same
milestone. A merged PR carries a milestone; the issues it closes sometimes
don't, which skews the release milestone audit and the release-notes coverage
check. This script detects the laggards and (on request) assigns the PR's
milestone to them.

Milestone is metadata, so assignment is exempt from the "never edit content
authored by others" rule — but it's a visible action, so the skill confirms
with the user before calling `assign`.

Actions
-------

  check    Read-only. Report the PR's milestone vs each closed issue's.
           In:  -Pr <n> [-Repo <owner/repo>]
           Out: { ok, pr, prMilestone: {number,title}|null,
                  closedIssues[]: { number, title, milestone, milestoneState,
                                    matches, relation, likelyIntentional },
                  laggards[]:    { ...same fields... } }
           `relation` = none|earlier|later|same|non-version (issue milestone
           version vs PR's). `likelyIntentional` = true when the issue is on an
           earlier or already-closed milestone (its fix shipped in a past
           release; this PR is a follow-up such as a test-enable), so it should
           be left alone rather than reassigned.

  assign   Assign the PR's milestone to each laggard issue (REST PATCH by
           numeric milestone id — works for closed milestones too, unlike
           `gh issue edit --milestone`). Verifies after each PATCH. Skips
           `likelyIntentional` laggards unless -IncludeReleased is passed.
           In:  -Pr <n> [-Repo] [-DryRun] [-IncludeReleased]
           Out: { ok, pr, milestone, results[]: { issue, from, to, ok },
                  skipped[]: { issue, from, relation, milestoneState, reason } }

Conventions: `.agents/docs/script-authoring.md`,
`.agents/docs/github-authoring.md` (PATCH-verify, numeric-id milestone).
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('check','assign')]
    [string]$Action,

    [Parameter(Mandatory)][int]$Pr,
    [string]$Repo = 'linq2db/linq2db',
    [switch]$DryRun,
    # By default `assign` skips laggards whose issue sits on an earlier or
    # already-closed milestone (a fix that shipped in a past release; this PR is
    # a follow-up such as a test-enable). Pass -IncludeReleased to assign those too.
    [switch]$IncludeReleased
)

$global:ScriptBaseName = 'milestone-consistency'
. "$PSScriptRoot/_shared.ps1"

# -- gather ------------------------------------------------------------------

# Compare an issue's milestone title to the PR's, as versions. Returns one of:
#   none        — issue has no milestone title
#   earlier     — issue milestone version < PR milestone version
#   later       — issue milestone version > PR milestone version
#   same        — equal (won't be a laggard)
#   non-version — one/both titles don't parse as a version (e.g. "Backlog")
function Get-MilestoneRelation {
    param([string]$PrTitle, [string]$IssueTitle)
    if (-not $IssueTitle) { return 'none' }
    [version]$prV = $null
    [version]$isV = $null
    if (-not [version]::TryParse($PrTitle, [ref]$prV) -or -not [version]::TryParse($IssueTitle, [ref]$isV)) {
        return 'non-version'
    }
    if ($isV -lt $prV) { return 'earlier' }
    if ($isV -gt $prV) { return 'later' }
    return 'same'
}

function Get-PrCore {
    param([int]$PrNumber, [string]$RepoFull)
    $r = Invoke-GhJson -ArgumentList @(
        'pr','view',"$PrNumber",'--repo',$RepoFull,
        '--json','number,title,milestone,body,closingIssuesReferences'
    )
    if (-not $r.ok) { Exit-WithError "gh pr view $PrNumber failed: $($r.error)" }
    return $r.data
}

# Milestone of an issue (PRs are issues too). Returns {number,title}|null.
function Get-IssueMilestone {
    param([int]$IssueNumber, [string]$RepoFull)
    $r = Invoke-GhJson -ArgumentList @('api', "repos/$RepoFull/issues/$IssueNumber", '--jq', '{number: .number, title: .title, milestone: .milestone}')
    if (-not $r.ok) { Exit-WithError "gh api issues/$IssueNumber failed: $($r.error)" }
    return $r.data
}

# Collect the set of issues the PR closes: closingIssuesReferences (numbers +
# titles) plus body-regex refs (numbers only — title fetched on demand).
function Get-ClosedIssueNumbers {
    param($PrData)
    $set = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($ci in @($PrData.closingIssuesReferences)) {
        if ($ci.number) { [void]$set.Add([int]$ci.number) }
    }
    foreach ($n in (Get-IssueRefsFromBody -Body $PrData.body)) { [void]$set.Add([int]$n) }
    return @($set | Sort-Object)
}

function Build-Report {
    param([int]$PrNumber, [string]$RepoFull)
    $pr = Get-PrCore -PrNumber $PrNumber -RepoFull $RepoFull
    $prMs = $null
    if ($pr.milestone -and $pr.milestone.number) {
        $prMs = [ordered]@{ number = [int]$pr.milestone.number; title = [string]$pr.milestone.title }
    }
    $closed = New-Object 'System.Collections.Generic.List[object]'
    $laggards = New-Object 'System.Collections.Generic.List[object]'
    foreach ($n in (Get-ClosedIssueNumbers -PrData $pr)) {
        $iss = Get-IssueMilestone -IssueNumber $n -RepoFull $RepoFull
        $issMs = $null
        $issMsState = $null
        if ($iss.milestone -and $iss.milestone.number) {
            $issMs = [ordered]@{ number = [int]$iss.milestone.number; title = [string]$iss.milestone.title }
            $issMsState = [string]$iss.milestone.state
        }
        $matches = ($prMs -and $issMs -and ([int]$issMs.number -eq [int]$prMs.number))
        $relation = if ($prMs) { Get-MilestoneRelation -PrTitle $prMs.title -IssueTitle ($issMs.title) } else { 'none' }
        # An issue on an earlier or already-closed milestone is most likely a
        # legitimate cross-milestone case (it shipped earlier; this PR is a
        # follow-up such as a test-enable), not a milestone that needs fixing.
        $likelyIntentional = ($relation -eq 'earlier') -or ($issMsState -eq 'closed')
        $row = [ordered]@{
            number            = [int]$n
            title             = [string]$iss.title
            milestone         = $issMs
            milestoneState    = $issMsState
            matches           = $matches
            relation          = $relation
            likelyIntentional = [bool]$likelyIntentional
        }
        $closed.Add($row) | Out-Null
        if ($prMs -and -not $matches) { $laggards.Add($row) | Out-Null }
    }
    return [pscustomobject]@{ pr = $PrNumber; title = [string]$pr.title; prMilestone = $prMs; closedIssues = $closed.ToArray(); laggards = $laggards.ToArray() }
}

# -- actions -----------------------------------------------------------------

function Do-Check {
    $rep = Build-Report -PrNumber $Pr -RepoFull $Repo
    Write-JsonOutput ([ordered]@{
        ok = $true; action = 'check'; pr = $rep.pr; title = $rep.title
        prMilestone = $rep.prMilestone; closedIssues = $rep.closedIssues; laggards = $rep.laggards
    })
}

function Do-Assign {
    $rep = Build-Report -PrNumber $Pr -RepoFull $Repo
    if (-not $rep.prMilestone) { Exit-WithError "PR #$Pr has no milestone — nothing to assign" }
    $msNum = [int]$rep.prMilestone.number
    $results = New-Object 'System.Collections.Generic.List[object]'
    $skipped = New-Object 'System.Collections.Generic.List[object]'
    foreach ($lag in @($rep.laggards)) {
        $issNum = [int]$lag.number
        $from = if ($lag.milestone) { [int]$lag.milestone.number } else { $null }
        # Skip likely-intentional cross-milestone cases unless explicitly overridden —
        # reassigning would corrupt the milestone of a fix that already shipped.
        if ($lag.likelyIntentional -and -not $IncludeReleased) {
            $skipped.Add([ordered]@{ issue = $issNum; from = $from; relation = $lag.relation; milestoneState = $lag.milestoneState; reason = 'likely-intentional (issue on earlier/closed milestone) — pass -IncludeReleased to assign anyway' }) | Out-Null
            continue
        }
        if ($DryRun) {
            $results.Add([ordered]@{ issue = $issNum; from = $from; to = $msNum; ok = $true; dryRun = $true }) | Out-Null
            continue
        }
        $p = Invoke-Gh -ArgumentList @('api','--method','PATCH', "repos/$Repo/issues/$issNum", '-F', "milestone=$msNum")
        if (-not $p.ok) { Exit-WithError "PATCH milestone on issue #$issNum failed: $($p.error)" }
        # verify
        $v = Invoke-Gh -ArgumentList @('api', "repos/$Repo/issues/$issNum", '--jq', '.milestone.number')
        $ok = $v.ok -and ($v.stdout.Trim() -eq "$msNum")
        $results.Add([ordered]@{ issue = $issNum; from = $from; to = $msNum; ok = $ok }) | Out-Null
    }
    $allOk = (@($results | Where-Object { -not $_.ok }).Count -eq 0)
    Write-JsonOutput ([ordered]@{
        ok = $allOk; action = 'assign'; pr = $Pr; dryRun = [bool]$DryRun; includeReleased = [bool]$IncludeReleased
        milestone = $rep.prMilestone; results = $results.ToArray(); skipped = $skipped.ToArray()
    })
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'check'  { Do-Check }
    'assign' { Do-Assign }
    default  { Exit-WithError "unknown action: $Action" }
}
