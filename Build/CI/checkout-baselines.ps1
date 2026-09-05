<#
checkout-baselines.ps1 - clone the baselines repository into a test leg's working directory.

Run it from the directory the clone should land in; every leg's configuration expects ./baselines
(AzureConnectionStrings sets BaselinesPath relative to the test assembly).

The clone is shallow, single-branch and sparse. The leg needs the index, not the tree:
BaselinesWriter creates its own directories and always writes whole files, so nothing here is ever
read back from the repository. --sparse lays out the two root files instead of 343541, and
--depth 1 --single-branch skips ~420 Mb of history for a tree the leg only appends to. The commit
step pairs this with git add --sparse.

The base is the run branch when it exists and baselines master when it does not, and which one it is
decides what the leg's commit can contain: git stages a delta against whatever was checked out. On
master, a baseline this PR changed in an earlier run and has since changed back is an empty delta -
it enters no commit, so the earlier run's version stays on the run branch and the baselines PR goes
on showing SQL the branch no longer emits. Against the run branch the same file is a staged revert.
Master stays the base when the branch does not exist, because the first leg's push is what creates
it, and a branch that does not exist has nothing to revert.

http.proactiveAuth is what makes the token in the url count: git sends credentials only after a 401
challenge, and github never challenges an anonymous read of a public repository - so without it the
clone is unauthenticated and github's unauthenticated-download throttle kills the leg (three legs on
build 23238). The credential also has to be complete - user:token, not token alone, or git asks for
the password it is missing. -c is written to the clone's config, so push-baselines.ps1's fetch
authenticates from it too.

Auth comes from GITHUB_TOKEN (= BASELINES_GH_PAT).

Exit codes: 0 = cloned. 1 = a real failure to check the branch out.
#>

[CmdletBinding()]
param(
    # The per-run baselines branch, e.g. baselines/pr_5848. Cloned when it already exists.
    [Parameter(Mandatory)][string] $Branch,
    # The baselines repo's default branch, i.e. the fallback base and the baselines PR's target.
    [Parameter(Mandatory)][string] $BaselinesMaster,
    # Directory the clone lands in, relative to the working directory.
    [string] $Path = 'baselines',
    [string] $Org = 'linq2db',
    [string] $BaselinesRepo = 'linq2db.baselines'
)

$ErrorActionPreference = 'Continue'

# Named, because an empty token clones as x-access-token:@... and git then fails on the password it
# is missing rather than on the thing that is actually wrong.
if (-not $Env:GITHUB_TOKEN) {
    Write-Host 'GITHUB_TOKEN is not set - it carries the baselines PAT, so nothing could be cloned or pushed'
    exit 1
}

$repoUrl = "https://x-access-token:${Env:GITHUB_TOKEN}@github.com/${Org}/${BaselinesRepo}.git"

# @(...) keeps a single-line answer an array. Without it PowerShell hands back a bare string for one
# match and a string[] for several, so a length test means "characters" in the first case and "lines"
# in the second - and a legitimate multi-ref answer reads as "not found".
$refs = @(git -c http.proactiveAuth=basic ls-remote --heads $repoUrl $Branch)
if ($LASTEXITCODE -ne 0) {
    Write-Host "ls-remote for '${Branch}' failed with code ${LASTEXITCODE}"
    exit 1
}

$base = if ($refs | Where-Object { $_ -match '^[0-9a-f]{40}\s' }) { $Branch } else { $BaselinesMaster }
Write-Host "Cloning baselines from '${base}'"

$output = git clone --depth 1 --single-branch --sparse -c http.proactiveAuth=basic --branch $base $repoUrl $Path 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to clone baselines from '${base}'. Error code ${LASTEXITCODE}, output: ${output}"
    exit 1
}
