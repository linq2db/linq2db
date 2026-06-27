<#
ensure-baselines-branch.ps1 — idempotently ensure the per-run baselines branch
exists on the linq2db.baselines repository.

Shared by two callers in the test pipeline:

  * create_baselines_branch (central, runs once): -Rebase -EmitOutputs.
    Creates the branch if missing, rebases it onto baselines master when it
    already exists but is behind, and exports the branch name / head hash /
    new-branch flag as task output variables.

  * test_{windows,linux,macos}_job (self-heal, before their clone): ensure-only.
    Re-creates the branch at the recorded -BaseHash if a previous completed run
    deleted it (empty branch cleanup) — without this a "rerun failed jobs"
    restart fails because create_baselines_branch is not re-run, so its branch
    was already removed by create_baselines_pr (see build 21555).

The branch creation is race-tolerant: when several test jobs self-heal a missing
branch in parallel, only one wins the ref creation; the losers detect the branch
now exists (by re-querying) and proceed.

Reads the auth token from the GITHUB_TOKEN environment variable (= BASELINES_GH_PAT).
The rebase path also needs git identity via EMAIL / GIT_AUTHOR_NAME / GIT_COMMITTER_NAME.

Usage:

  central:   pwsh ... -PrId "$(source_pr_id)" -BaselinesMaster "$(baselines_master)" -Rebase -EmitOutputs
  self-heal: pwsh ... -Branch "$(baselines_branch)" -BaselinesMaster "$(baselines_master)" -BaseHash "$(baselines_head)"

Parameters:
  -Branch           resolved branch name (e.g. baselines/pr_1234). When empty it is derived from -PrId.
  -PrId             source pull request number; empty => baselines/default branch.
  -BaselinesMaster  baselines repo default branch (master). Required.
  -BaseHash         hash to create the branch from when it is missing. Empty => baselines master HEAD.
  -Rebase           rebase an existing branch onto baselines master when it is behind (central job only).
  -EmitOutputs      export baselines_branch / baselines_head / baselines_new_branch task outputs (central job only).
#>

param(
    [string] $Branch = '',
    [string] $PrId = '',
    [Parameter(Mandatory = $true)][string] $BaselinesMaster,
    [string] $BaseHash = '',
    [switch] $Rebase,
    [switch] $EmitOutputs
)

$orgName       = "linq2db"
$baselinesRepo = "linq2db.baselines"
$baselinesRepoUrl = "https://${Env:GITHUB_TOKEN}@github.com/${orgName}/${baselinesRepo}.git"

# Resolve branch name (derive from PR id when not passed explicitly)
if (-not $Branch) {
    if ($PrId) {
        $Branch = "baselines/pr_${PrId}"
    } else {
        $Branch = "baselines/default"
    }
}
Write-Host "Baselines branch name: ${Branch}"

function Get-RemoteHash([string]$ref, [switch]$Heads) {
    if ($Heads) {
        $out = git ls-remote --heads $baselinesRepoUrl $ref
    } else {
        $out = git ls-remote $baselinesRepoUrl $ref
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ls-remote for '${ref}' failed with code ${LASTEXITCODE}"
        exit 1
    }
    if ($out.Length -lt 40) {
        return ''
    }
    return ($out -split '\s+')[0]
}

$branchHash  = Get-RemoteHash $Branch -Heads
$newBranch   = 0

if (-not $branchHash) {
    Write-Host "Baselines branch not found, creating it"

    # Create from the recorded base hash when supplied (keeps create_baselines_pr's
    # no-new-commits detection valid across restarts even if master advanced),
    # otherwise from current baselines master HEAD.
    if ($BaseHash) {
        $createHash = $BaseHash
    } else {
        $createHash = Get-RemoteHash $BaselinesMaster
        if (-not $createHash) {
            Write-Host "Baselines repo HEAD not found for '${BaselinesMaster}'"
            exit 1
        }
    }

    Write-Host "Creating new baselines branch ${Branch} at ${createHash}..."
    $output = gh api /repos/$orgName/$baselinesRepo/git/refs -i -F ref=refs/heads/$Branch -F sha=$createHash
    Write-Host "Create command output: ${output}"

    if ($LASTEXITCODE -eq 0 -and ($output -match "201 Created")) {
        Write-Host "Baselines branch created"
        $newBranch  = 1
        $branchHash = $createHash
    } else {
        # Creation failed — a sibling job may have created the branch concurrently.
        Write-Host "Branch creation did not return 201, re-checking whether it now exists"
        $branchHash = Get-RemoteHash $Branch -Heads
        if (-not $branchHash) {
            Write-Host "Failed to create branch and it does not exist. Error code ${LASTEXITCODE}"
            exit 1
        }
        Write-Host "Baselines branch already exists (created by a concurrent job)"
    }
} else {
    Write-Host "Baselines branch already exists"

    if ($Rebase) {
        Write-Host "Checking if rebase required"
        $masterHash = Get-RemoteHash $BaselinesMaster
        if (-not $masterHash) {
            Write-Host "Baselines repo HEAD not found for '${BaselinesMaster}'"
            exit 1
        }
        if ($branchHash -eq $masterHash) {
            Write-Host "Baselines branch already based on HEAD, no rebase required"
        } else {
            Write-Host "Baselines head is ${branchHash}, but master is ${masterHash}, trying to rebase on current HEAD"
            git clone $baselinesRepoUrl baselines
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to clone baselines repository. Error code ${LASTEXITCODE}"
                exit 1
            }
            cd baselines
            git checkout origin/$Branch
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to checkout baselines branch origin/${Branch}. Error code ${LASTEXITCODE}"
                exit 1
            }
            git rebase origin/$BaselinesMaster
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to rebase baselines PR on origin/${BaselinesMaster}. Delete branch and re-run tests. Error code ${LASTEXITCODE}"
                exit 1
            }
            git push -f origin HEAD:$Branch
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to push rebased baselines. Error code ${LASTEXITCODE}"
                exit 1
            }
            Write-Host "Baselines PR was rebased on HEAD"
            cd ..
            $branchHash = $masterHash
        }
    }
}

Write-Host "Baselines branch head hash: ${branchHash}"

if ($EmitOutputs) {
    echo "##vso[task.setvariable variable=baselines_branch;isOutput=true]${Branch}"
    echo "##vso[task.setvariable variable=baselines_head;isOutput=true]${branchHash}"
    echo "##vso[task.setvariable variable=baselines_new_branch;isOutput=true]${newBranch}"
}
