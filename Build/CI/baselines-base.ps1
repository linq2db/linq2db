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
    $null = gh api -XPATCH "repos/${Org}/${BaselinesRepo}/pulls/${number}" -F base=$Base
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to retarget baselines PR #${number}. Error code ${LASTEXITCODE}"
    }
}
