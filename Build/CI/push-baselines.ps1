<#
push-baselines.ps1 - commit a test leg's captured baselines, push them to the per-run branch, and open
the baselines PR if this leg is the first to get there.

Extracted verbatim from the two inline copies in test-workflow-linux.yml and test-workflow-windows.yml,
which were 124 lines each and differed on exactly one: the commit message's platform prefix. That is
now -Platform. The GitHub Actions legs need the same logic, and a third copy of a 124-line script whose
every branch encodes a reproduced CI failure was not worth having.

Run it with the baselines clone as the working directory. checkout-baselines.ps1 makes that clone
shallow and sparse (--depth 1 --single-branch --sparse), off the run branch when it already exists and
off baselines master when it does not - which is the case this script's push resolves, by creating the
branch.

Auth comes from GITHUB_TOKEN (= BASELINES_GH_PAT), used both for the push URL and by gh. Git identity
comes from EMAIL / GIT_AUTHOR_NAME / GIT_COMMITTER_NAME, as the callers already set.

Exit codes: 0 = pushed, or nothing to push, or the PR already existed. 1 = a real failure to record
baselines. Note the deliberate asymmetry at the end: failing to post the courtesy comment on the source
PR is a warning, not a failure.

Annotations: emits an Azure ##vso warning by default, or a GitHub ::warning:: with -GitHubAnnotations,
so a warning is visible on whichever CI is running it.
#>

[CmdletBinding()]
param(
    # 'Linux' or 'Windows' - the only thing that differed between the two inline copies.
    [Parameter(Mandatory)][string] $Platform,
    # The leg's display title, e.g. 'SQLite (all providers)'. Goes in the commit message.
    [Parameter(Mandatory)][string] $Title,
    # The per-run baselines branch, e.g. baselines/pr_5848.
    [Parameter(Mandatory)][string] $Branch,
    # The baselines repo's default branch, i.e. the PR base.
    [Parameter(Mandatory)][string] $BaselinesMaster,
    # Source PR number. Empty when the pipeline was triggered outside a PR.
    [string] $PrId = '',
    [string] $Org = 'linq2db',
    [string] $BaselinesRepo = 'linq2db.baselines',
    [string] $SourceRepo = 'linq2db',
    [switch] $GitHubAnnotations
)

$ErrorActionPreference = 'Continue'

function Write-CiWarning([string] $message) {
    if ($GitHubAnnotations) { Write-Host "::warning::$message" }
    else                    { Write-Host "##vso[task.logissue type=warning]$message" }
}

if (-not $Env:GITHUB_TOKEN) {
    Write-Host 'GITHUB_TOKEN is not set - it carries the baselines PAT, so nothing can be pushed'
    exit 1
}

Write-Host "Add baselines changes to commit (index)"
$output = git add --sparse -A
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to add baselines changes. Error code ${LASTEXITCODE}, output: ${output}"
    exit 1
}
Write-Host "Create commit"
$output = git commit -m "[$Platform / $Title] baselines"
if ($output -match "nothing to commit") {
    Write-Host "No baselines changes detected"
    exit 0
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create commit. Error code ${LASTEXITCODE}, output: ${output}"
    exit 1
}

# When the run branch did not exist the leg cloned master, so this push is what creates it, and only
# a leg that actually has baselines to push ever creates it. A run that changes none leaves nothing
# behind, which is what retired create_baselines_pr - a job that cost a fresh agent acquisition
# at the very end of the run (78 min on build 23009) for 0.3 min of work. Whichever leg commits
# first wins the ref; the rest are rejected and rebase onto it.
# x-access-token: rather than the bare token, per #5859 - git asks for the password it is missing if
# the credential is incomplete. The clone this runs in was made with http.proactiveAuth=basic, which
# is written into its config, so the fetch in the rebase below authenticates from that.
$repoUrl = "https://x-access-token:${Env:GITHUB_TOKEN}@github.com/${Org}/${BaselinesRepo}.git"
$pushed = $false
$attempts = 10
while ($attempts -gt 0) {
    Write-Host "Push baselines to ${Branch}"
    $output = git push $repoUrl HEAD:refs/heads/$Branch 2>&1
    if ($LASTEXITCODE -eq 0) {
        $pushed = $true
        break
    }
    Write-Host "Push rejected, rebasing onto ${Branch}. Output: ${output}"
    $output = git fetch origin "refs/heads/${Branch}" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to fetch ${Branch}. Error code ${LASTEXITCODE}, output: ${output}"
        exit 1
    }
    # -X theirs, which during a rebase means the commit being replayed - this leg's freshly captured
    # baseline - and not the branch it is going onto. Without it a leg whose clone is based on master
    # fails whenever its commit rewrites a file the branch already carries, which is every leg on the
    # second run of a PR whose SQL moved: the replay conflicts. Reproduced on a scratch repository
    # with this exact sequence; the fresh capture is always the one that should win, so resolving that
    # way is the answer rather than a papered-over conflict. A leg based on the run branch pushes a
    # fast-forward instead and only reaches this loop when a concurrent leg got there first.
    # --onto FETCH_HEAD HEAD~1 keeps that to this leg's own commit. A bare FETCH_HEAD replays
    # everything back to the merge base, and the clone is --depth 1: when baselines master advanced
    # after create_baselines_branch rebased the branch, the shallow root is unreachable from the
    # branch tip, so git reads it as an unrelated root commit and replays its whole tree - which
    # -X theirs then lets win, reverting every baseline another leg had already pushed. HEAD~1 is
    # exact because this step makes exactly one commit.
    $output = git rebase -X theirs --onto FETCH_HEAD HEAD~1 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to rebase onto ${Branch}. Error code ${LASTEXITCODE}, output: ${output}"
        Write-Host "Display conflict diff"
        git diff ORIG_HEAD FETCH_HEAD
        exit 1
    }
    $attempts = $attempts - 1
}
if (-not $pushed) {
    Write-Host "Failed to push baselines"
    exit 1
}

# Open the baselines PR if this leg is the first to push. Concurrent legs either find the PR the
# winner already opened, or lose the create - which GitHub answers with "A pull request already
# exists", not an error worth failing a green test leg over.
$output = gh api -XGET /repos/$Org/$BaselinesRepo/pulls -F state=open -F head="${Org}:${Branch}"
if ($LASTEXITCODE -ne 0) {
    Write-Host "PR search failed. Error code ${LASTEXITCODE}, output: ${output}"
    exit 1
}
if ($output -match "html_url") {
    Write-Host "Baselines PR already exists"
    exit 0
}
if ($PrId) {
    $sourcePrUrl = "https://github.com/${Org}/${SourceRepo}/pull/${PrId}"
    $prName = "Baselines for ${sourcePrUrl}"
    $prMessage = "Baselines for [#${PrId}](${sourcePrUrl})"
} else {
    $prName = "Baselines"
    $prMessage = "Not associated with any pull request (tests pipeline triggered from admin console?)"
}
$output = gh api /repos/$Org/$BaselinesRepo/pulls -F title="${prName}" -F head=$Branch -F base=$BaselinesMaster -F draft=true -F body="${prMessage}" 2>&1
if ($output -match "A pull request already exists") {
    Write-Host "Baselines PR was opened by a concurrent leg"
    exit 0
}
if (-not ($output -match "html_url")) {
    Write-Host "PR creation failed. Error code ${LASTEXITCODE}, output: ${output}"
    exit 1
}
Write-Host "Baselines PR created"
if (-not $PrId) {
    exit 0
}
Write-Host "Post notification to source PR about baselines PR creation"
$note = "Test baselines changed by this PR. Don't forget to merge/close baselines PR after this pr merged/closed."
$output = gh api /repos/$Org/$SourceRepo/issues/$PrId/comments -F body="$note"
if ($LASTEXITCODE -ne 0 -or -not ($output -match "html_url")) {
    # A warning, not a failure. The two exits above guard something real - the baselines were not
    # tracked - while by this point the PR exists and only the courtesy notification is missing.
    # Failing here would put a leg red over a comment, after its provider tests all passed, and a
    # re-run cannot repost it: the leg finds nothing to commit and exits before reaching this code.
    Write-CiWarning "Message posting failed. Error code ${LASTEXITCODE}, output: ${output}"
}
