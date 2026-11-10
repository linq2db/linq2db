<#
baselines-base.ps1 - dot-sourced helpers that resolve which baselines branch a PR's baselines build on.

A PR stacked on another PR (its base is the parent's head branch, not master) gets its baselines branch
cut from, rebased onto and its baselines PR opened against the nearest ancestor's baselines branch, so
the baselines PR shows only this layer's changes. The walk follows each PR's base branch to the open
PR whose head it is, and stops at the first ancestor with a baselines/pr_<n> branch; anything else -
a PR on master, a base that is no open PR's head, an ancestor chain without baselines - resolves to
baselines master.

Auth: gh reads GITHUB_TOKEN (= BASELINES_GH_PAT), which every caller already sets.
#>

function Get-BaselinesPrId([string] $Branch) {
    if ($Branch -match '^baselines/pr_(\d+)$') { return $Matches[1] }
    return ''
}

function Test-BaselinesBranch([string] $RepoUrl, [string] $Branch) {
    $refs = @(git -c http.proactiveAuth=basic ls-remote --heads $RepoUrl $Branch)
    if ($LASTEXITCODE -ne 0) {
        throw "ls-remote for '${Branch}' failed with code ${LASTEXITCODE}"
    }
    return [bool]($refs | Where-Object { $_ -match '^[0-9a-f]{40}\s' })
}

function Get-BaselinesBase {
    param(
        [string] $PrId,
        [Parameter(Mandatory)][string] $BaselinesMaster,
        [Parameter(Mandatory)][string] $RepoUrl,
        [string] $Org = 'linq2db',
        [string] $SourceRepo = 'linq2db'
    )

    $id = $PrId
    # a cycle is impossible on GitHub, the cap only bounds API calls on a pathological stack
    for ($depth = 0; $id -and $depth -lt 10; $depth++) {
        $baseRef = gh api "repos/${Org}/${SourceRepo}/pulls/${id}" --jq .base.ref
        if ($LASTEXITCODE -ne 0 -or -not $baseRef) {
            Write-Host "Cannot read base branch of #${id}, using ${BaselinesMaster}"
            return $BaselinesMaster
        }

        $parent = gh api -XGET "repos/${Org}/${SourceRepo}/pulls" -F state=open -F head="${Org}:${baseRef}" --jq '.[0].number // empty'
        if ($LASTEXITCODE -ne 0 -or -not $parent) {
            Write-Host "#${id} targets '${baseRef}', which is no open PR's head - baselines base is ${BaselinesMaster}"
            return $BaselinesMaster
        }

        $candidate = "baselines/pr_${parent}"
        if (Test-BaselinesBranch $RepoUrl $candidate) {
            Write-Host "#${id} is stacked on #${parent} - baselines base is ${candidate}"
            return $candidate
        }

        Write-Host "#${id} is stacked on #${parent}, which has no baselines branch - checking its base"
        $id = $parent
    }

    return $BaselinesMaster
}

# Retargets an existing open baselines PR whose base no longer matches - a parent that gained or lost
# its baselines branch, or a stack GitHub retargeted to master after the parent merged.
function Update-BaselinesPrBase {
    param(
        [Parameter(Mandatory)][string] $Branch,
        [Parameter(Mandatory)][string] $Base,
        [string] $Org = 'linq2db',
        [string] $BaselinesRepo = 'linq2db.baselines'
    )

    $pr = gh api -XGET "repos/${Org}/${BaselinesRepo}/pulls" -F state=open -F head="${Org}:${Branch}" --jq '.[0] | select(.) | "\(.number) \(.base.ref)"'
    if ($LASTEXITCODE -ne 0 -or -not $pr) {
        return
    }

    $number, $current = $pr -split ' ', 2
    if ($current -eq $Base) {
        return
    }

    Write-Host "Retargeting baselines PR #${number} from '${current}' to '${Base}'"
    $output = gh api -XPATCH "repos/${Org}/${BaselinesRepo}/pulls/${number}" -F base=$Base 2>&1
    if ($LASTEXITCODE -ne 0) {
        # Not fatal: GitHub refuses (422) a base the head has no commits over - a stacked PR whose baselines
        # match its parent's. Left set, the exit code fails a caller that ends here (ensure-baselines-branch.ps1).
        Write-Host "Failed to retarget baselines PR #${number}. Error code ${LASTEXITCODE}, output: ${output}"
        $global:LASTEXITCODE = 0
    }
}

function Test-RebaseInProgress {
    return (Test-Path (git rev-parse --git-path rebase-merge)) -or (Test-Path (git rev-parse --git-path rebase-apply))
}

# Rebases HEAD onto $Onto with every conflict resolved to $Onto's side. A PR's baselines conflict with its
# base when another PR's baselines merged there first; the run's legs then regenerate this PR's own changes
# on top (push-baselines.ps1 replays with -X theirs). Returns $false, with no rebase left in progress, when
# the rebase still cannot finish.
function Invoke-BaselinesRebase([Parameter(Mandatory)][string] $Onto) {
    # during a rebase "ours" is the branch being rebased onto
    git rebase -X ours $Onto | Out-Host
    if ($LASTEXITCODE -ne 0 -and -not (Test-RebaseInProgress)) {
        return $false
    }

    for ($step = 0; (Test-RebaseInProgress) -and $step -lt 1000; $step++) {
        # -X settles content conflicts only: a path modified on one side and deleted on the other stays unmerged
        $unmerged = @(git diff --name-only --diff-filter=U)
        foreach ($path in $unmerged) {
            git cat-file -e ":2:${path}" 2>$null
            if ($LASTEXITCODE -eq 0) {
                git checkout --ours -- $path | Out-Host
                git add -- $path | Out-Host
            } else {
                git rm -q -- $path | Out-Host
            }
            Write-Host "Conflict in '${path}' resolved to ${Onto}'s side"
        }

        git -c core.editor=true rebase --continue | Out-Host
        if ($LASTEXITCODE -ne 0 -and $unmerged.Count -eq 0) {
            # stopped on something other than a conflict: retrying cannot help
            break
        }
    }

    if (Test-RebaseInProgress) {
        git rebase --abort | Out-Host
        return $false
    }

    return $true
}
