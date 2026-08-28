<#
ensure-baselines-branch.ps1 - resolve the per-run baselines branch name, and rebase
that branch onto baselines master when an earlier run left it behind.

Called once per run by create_baselines_branch, as:

  pwsh ... -PrId "$(source_pr_id)" -BaselinesMaster "$(baselines_master)" -Rebase -EmitOutputs

It does not create the branch. A test leg creates it by pushing, and only if it has
baselines to push, so a run that changes none leaves no branch behind. That is what
removed the end-of-run create_baselines_pr job, whose only remaining duty would have
been deleting the empty branch and which cost a fresh agent acquisition - 78 min on
build 23009 - for 0.3 min of work.

What is left here is the one case the legs cannot handle: a branch left by an earlier
run of the same PR that has fallen behind baselines master. The legs clone master and
would rebase their commit onto that stale base, so it is rebased onto master here,
once, before any leg starts.

Reads the auth token from the GITHUB_TOKEN environment variable (= BASELINES_GH_PAT).
The rebase path also needs git identity via EMAIL / GIT_AUTHOR_NAME / GIT_COMMITTER_NAME.

Parameters:
  -PrId             source pull request number; empty => baselines/default branch.
  -BaselinesMaster  baselines repo default branch (master). Required.
  -Rebase           rebase an existing branch onto baselines master when it is behind.
  -EmitOutputs      export the baselines_branch task output the test jobs read.
#>

param(
    [string] $PrId = '',
    [Parameter(Mandatory = $true)][string] $BaselinesMaster,
    [switch] $Rebase,
    [switch] $EmitOutputs
)

$orgName       = "linq2db"
$baselinesRepo = "linq2db.baselines"
$baselinesRepoUrl = "https://${Env:GITHUB_TOKEN}@github.com/${orgName}/${baselinesRepo}.git"

# Resolve the branch name from the PR id
if ($PrId) {
    $Branch = "baselines/pr_${PrId}"
} else {
    $Branch = "baselines/default"
}
Write-Host "Baselines branch name: ${Branch}"

function Get-RemoteHash([string]$ref) {
    # @(...) keeps a single-line answer an array. Without it PowerShell hands back a bare string for
    # one match and a string[] for several, so a length test means "characters" in the first case and
    # "lines" in the second — and a legitimate multi-ref answer reads as "not found".
    $out = @(git ls-remote --heads $baselinesRepoUrl $ref)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ls-remote for '${ref}' failed with code ${LASTEXITCODE}"
        exit 1
    }
    $line = $out | Where-Object { $_ -match '^[0-9a-f]{40}\s' } | Select-Object -First 1
    if (-not $line) {
        return ''
    }
    return ($line -split '\s+')[0]
}

$branchHash = Get-RemoteHash $Branch

if (-not $branchHash) {
    Write-Host "Baselines branch does not exist - the first test leg with baselines to push creates it"
} else {
    Write-Host "Baselines branch already exists"

    if ($Rebase) {
        Write-Host "Checking if rebase required"
        # Ask whether the branch is already on top of master, rather than comparing the two head
        # hashes: a branch carrying even one baselines commit is by construction ahead of master, so
        # the hashes always differ and the clone below ran on every re-run of a baseline-carrying PR.
        # That clone is the whole repository - 343541 files, ~420 Mb - and it sits in the serial
        # prefix every test leg waits on: 2.3 min for the job on build 23050 against 0.25 min on
        # 23068, where no branch existed. "ahead" means there is nothing to rebase onto.
        $status = gh api /repos/$orgName/$baselinesRepo/compare/${BaselinesMaster}...${Branch} --jq .status
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Compare request for '${BaselinesMaster}...${Branch}' failed with code ${LASTEXITCODE}"
            exit 1
        }
        Write-Host "Baselines branch is '${status}' relative to ${BaselinesMaster}"
        if ($status -eq 'ahead' -or $status -eq 'identical') {
            Write-Host "Baselines branch already based on ${BaselinesMaster}, no rebase required"
        } else {
            Write-Host "Baselines head is ${branchHash} and the branch is '${status}', trying to rebase on current HEAD"
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
            $branchHash = git rev-parse HEAD
            cd ..
        }
    }
}

Write-Host "Baselines branch head hash: ${branchHash}"

if ($EmitOutputs) {
    echo "##vso[task.setvariable variable=baselines_branch;isOutput=true]${Branch}"
}
