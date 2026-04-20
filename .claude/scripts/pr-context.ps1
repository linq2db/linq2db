#!/usr/bin/env pwsh
<#
One-shot PR context loader for the review skills.

Why this wrapper exists
-----------------------
`/review-pr` and `/verify-review` both open with a ~10-call parallel batch of
`gh api` + `git` queries (PR metadata, reviews, review comments, issue
comments, closing-issues GraphQL, fetch, diff --stat, diff --name-status,
log, log --format=%B). Each unique command string is its own permission
prompt, so the allowlist rules didn't amortise. This script folds the whole
batch into one pwsh process with one allowlist rule:

    Bash(pwsh -NoProfile -File .claude/scripts/pr-context.ps1:*)

It also performs the one-level linked-issue scan (regex across PR body,
commit messages, conversation comments, review bodies, review comments)
and fan-out fetches the linked issues + their comments in parallel.

Input (stdin, JSON)
-------------------
  {
    "pr":        5414,            // required, int
    "owner":     "linq2db",       // optional, default "linq2db"
    "repo":      "linq2db",       // optional, default "linq2db"
    "baseRef":   "origin/master", // optional
    "fetchHead": true,            // optional, default true — skips `git fetch` for both the PR head and the base branch when false
    "linkedConcurrency": 6        // optional, default 6
  }

Output (stdout, single JSON object): see the review skills' expected shape —
  { pr, currentUser, reviews, reviewComments, issueComments, reviewThreads,
    closingIssues, linkedRefs, linkedIssues, diffStat, nameStatus, commits,
    baseRef, headRef, headSha }

`reviewThreads[]` is the databaseId → thread.id map needed by `/verify-review`
step 7 (which action to take per prior line/file comment based on thread state).
Each entry is `{ threadId, isResolved, firstCommentId }`, matching the GraphQL
`reviewThreads(first:100).nodes[*]` shape.

Exit codes
----------
  0 = success
  1 = hard failure (invalid stdin, gh/git command failed, etc.)
#>

$global:ScriptBaseName = 'pr-context'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'pr (positive integer) required' }
$pr = [int]$m.pr
$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$repoFull = "$owner/$repo"
$baseRef = if ($m.baseRef) { [string]$m.baseRef } else { 'origin/master' }
$headRef = "origin/pr/$pr"
$fetchHead = ($null -eq $m.fetchHead) -or ([bool]$m.fetchHead)
$linkedConcurrency = if ((Test-IsInteger $m.linkedConcurrency) -and [long]$m.linkedConcurrency -gt 0) { [int]$m.linkedConcurrency } else { 6 }

# Everything except the git work that depends on the fetch can run in parallel.
# Fire them all as thread jobs, including the fetch itself.

$root = $PSScriptRoot

$jobs = @{}
$jobs.fetch = if ($fetchHead) {
    Start-ThreadJob -ScriptBlock {
        . "$using:root/_shared.ps1"
        Invoke-Git @('fetch', 'origin', "refs/pull/$using:pr/head:refs/remotes/origin/pr/$using:pr")
    }
} else { $null }

# Keep the base ref fresh. Without this, a stale `origin/master` produces a wrong
# baseRef...headRef diff (inflated file list, wrong hunks), and the caller ends
# up re-running this script after a manual `git fetch origin master`.
$baseBranch = if ($baseRef -match '^origin/(.+)$') { $Matches[1] } else { $null }
$jobs.fetchBase = if ($fetchHead -and $baseBranch) {
    Start-ThreadJob -ScriptBlock {
        . "$using:root/_shared.ps1"
        Invoke-Git @('fetch', 'origin', $using:baseBranch)
    }
} else { $null }

$jobs.prMeta = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-GhJson @(
        'pr','view',"$using:pr",
        '--repo',$using:repoFull,
        '--json','number,title,body,baseRefName,headRefName,headRefOid,milestone,labels,state,isDraft,url,mergeable,additions,deletions,changedFiles,author'
    )
}
$jobs.reviews = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-GhJson @('api',"repos/$using:repoFull/pulls/$using:pr/reviews",'--paginate')
}
$jobs.reviewComments = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-GhJson @('api',"repos/$using:repoFull/pulls/$using:pr/comments",'--paginate')
}
$jobs.issueComments = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-GhJson @('api',"repos/$using:repoFull/issues/$using:pr/comments",'--paginate')
}
$jobs.user = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Gh @('api','user','--jq','.login')
}
$jobs.closingIssues = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    # Pass owner/repo as GraphQL variables rather than splicing them into the
    # query string — defends against any future non-default owner/repo value
    # containing quotes or backslashes.
    $query = 'query($o:String!,$r:String!,$n:Int!){ repository(owner:$o,name:$r){ pullRequest(number:$n){ closingIssuesReferences(first:20){ nodes{ number } } } } }'
    Invoke-GhJson @('api','graphql','-F',"o=$using:owner",'-F',"r=$using:repo",'-F',"n=$using:pr",'-f',"query=$query")
}
$jobs.reviewThreads = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    # databaseId → thread.id map. `/verify-review` step 7 decides per-thread
    # resolve actions against `isResolved`; pairing it with the first comment's
    # databaseId lets the caller go REST comment_id → GraphQL thread_id in one
    # lookup. `first:100` matches the upper bound observed on linq2db PRs.
    $query = 'query($o:String!,$r:String!,$n:Int!){ repository(owner:$o,name:$r){ pullRequest(number:$n){ reviewThreads(first:100){ nodes{ id isResolved comments(first:1){ nodes{ databaseId } } } } } } }'
    Invoke-GhJson @('api','graphql','-F',"o=$using:owner",'-F',"r=$using:repo",'-F',"n=$using:pr",'-f',"query=$query")
}

$fetchRes         = if ($jobs.fetch) { Receive-Job $jobs.fetch -Wait; Remove-Job $jobs.fetch } else { [pscustomobject]@{ ok = $true; stdout = '' } }
$fetchBaseRes     = if ($jobs.fetchBase) { Receive-Job $jobs.fetchBase -Wait; Remove-Job $jobs.fetchBase } else { [pscustomobject]@{ ok = $true; stdout = '' } }
$prMetaRes        = Receive-Job $jobs.prMeta -Wait;        Remove-Job $jobs.prMeta
$reviewsRes       = Receive-Job $jobs.reviews -Wait;       Remove-Job $jobs.reviews
$reviewCommentsRes= Receive-Job $jobs.reviewComments -Wait;Remove-Job $jobs.reviewComments
$issueCommentsRes = Receive-Job $jobs.issueComments -Wait; Remove-Job $jobs.issueComments
$userRes          = Receive-Job $jobs.user -Wait;          Remove-Job $jobs.user
$closingRes       = Receive-Job $jobs.closingIssues -Wait; Remove-Job $jobs.closingIssues
$reviewThreadsRes = Receive-Job $jobs.reviewThreads -Wait; Remove-Job $jobs.reviewThreads

if (-not $fetchRes.ok)          { Exit-WithError "git fetch failed: $($fetchRes.error)" }
if (-not $fetchBaseRes.ok)      { Exit-WithError "git fetch $baseRef failed: $($fetchBaseRes.error)" }
if (-not $prMetaRes.ok)         { Exit-WithError "gh pr view failed: $($prMetaRes.error)" }
if (-not $reviewsRes.ok)        { Exit-WithError "gh pulls/reviews failed: $($reviewsRes.error)" }
if (-not $reviewCommentsRes.ok) { Exit-WithError "gh pulls/comments failed: $($reviewCommentsRes.error)" }
if (-not $issueCommentsRes.ok)  { Exit-WithError "gh issues/comments failed: $($issueCommentsRes.error)" }
if (-not $userRes.ok)           { Exit-WithError "gh api user failed: $($userRes.error)" }
if (-not $closingRes.ok)        { Exit-WithError "closing-issues GraphQL failed: $($closingRes.error)" }
if (-not $reviewThreadsRes.ok)  { Exit-WithError "review-threads GraphQL failed: $($reviewThreadsRes.error)" }

$prMeta = $prMetaRes.data
$reviewsRaw = @($reviewsRes.data)
$reviewCommentsRaw = @($reviewCommentsRes.data)
$issueCommentsRaw = @($issueCommentsRes.data)
$currentUser = $userRes.stdout.Trim()

$closingIssues = @()
$nodes = $closingRes.data.data.repository.pullRequest.closingIssuesReferences.nodes
if ($nodes) {
    foreach ($n in $nodes) {
        if (Test-IsInteger $n.number) { $closingIssues += [int]$n.number }
    }
}

$reviewThreads = @()
$threadNodes = $reviewThreadsRes.data.data.repository.pullRequest.reviewThreads.nodes
if ($threadNodes) {
    foreach ($t in $threadNodes) {
        $firstId = $null
        if ($t.comments -and $t.comments.nodes -and $t.comments.nodes.Count -gt 0) {
            $firstId = $t.comments.nodes[0].databaseId
        }
        $reviewThreads += [pscustomobject]@{
            threadId       = [string]$t.id
            isResolved     = [bool]$t.isResolved
            firstCommentId = $firstId
        }
    }
}

# Git queries that depend on the fetch completing.
$gitJobs = @{}
$gitJobs.stat = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Git @('diff','--stat',"$using:baseRef...$using:headRef")
}
$gitJobs.nameStatus = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Git @('diff','--name-status',"$using:baseRef...$using:headRef")
}
$gitJobs.log = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Git @('log','--no-merges','--format=%H%x1f%ai%x1f%an%x1f%s',"$using:baseRef..$using:headRef")
}
$gitJobs.logBody = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Git @('log','--no-merges','--format=%H%x1e%B%x1d',"$using:baseRef..$using:headRef")
}
$gitJobs.headSha = Start-ThreadJob -ScriptBlock {
    . "$using:root/_shared.ps1"
    Invoke-Git @('rev-parse',$using:headRef)
}

$statRes       = Receive-Job $gitJobs.stat -Wait;       Remove-Job $gitJobs.stat
$nameStatusRes = Receive-Job $gitJobs.nameStatus -Wait; Remove-Job $gitJobs.nameStatus
$logRes        = Receive-Job $gitJobs.log -Wait;        Remove-Job $gitJobs.log
$logBodyRes    = Receive-Job $gitJobs.logBody -Wait;    Remove-Job $gitJobs.logBody
$headShaRes    = Receive-Job $gitJobs.headSha -Wait;    Remove-Job $gitJobs.headSha

if (-not $statRes.ok)       { Exit-WithError "git diff --stat failed: $($statRes.error)" }
if (-not $nameStatusRes.ok) { Exit-WithError "git diff --name-status failed: $($nameStatusRes.error)" }
if (-not $logRes.ok)        { Exit-WithError "git log failed: $($logRes.error)" }
if (-not $logBodyRes.ok)    { Exit-WithError "git log (bodies) failed: $($logBodyRes.error)" }
if (-not $headShaRes.ok)    { Exit-WithError "git rev-parse $headRef failed: $($headShaRes.error)" }

$diffStat = $statRes.stdout.TrimEnd()

$nameStatus = @()
foreach ($line in ($nameStatusRes.stdout -split "`n")) {
    if (-not $line) { continue }
    $parts = $line -split "`t"
    $status = $parts[0]
    if ($status.StartsWith('R') -or $status.StartsWith('C')) {
        $nameStatus += [pscustomobject]@{ status = $status; oldPath = $parts[1]; path = $parts[2] }
    } else {
        $nameStatus += [pscustomobject]@{ status = $status; path = $parts[1] }
    }
}

$commits = @()
foreach ($line in ($logRes.stdout -split "`n")) {
    if (-not $line) { continue }
    $fields = $line -split [char]0x1f
    $commits += [pscustomobject]@{
        sha     = $fields[0]
        date    = $fields[1]
        author  = $fields[2]
        subject = if ($fields.Length -gt 3) { $fields[3] } else { '' }
        body    = ''
    }
}

# `$logBodyRes.stdout` is a sequence of `<sha>\x1e<body>\x1d` records.
$bodyMap = @{}
foreach ($entry in ($logBodyRes.stdout -split [char]0x1d)) {
    $e = $entry.Trim()
    if (-not $e) { continue }
    $sepIdx = $e.IndexOf([char]0x1e)
    if ($sepIdx -lt 0) { continue }
    $bodyMap[$e.Substring(0, $sepIdx)] = $e.Substring($sepIdx + 1)
}
foreach ($c in $commits) {
    if ($bodyMap.ContainsKey($c.sha)) { $c.body = $bodyMap[$c.sha] } else { $c.body = $c.subject }
}

$headSha = $headShaRes.stdout.Trim()

# Regex-scan for referenced issues/PRs (one level deep).
$textBlobs = @()
$textBlobs += [string]($prMeta.body)
foreach ($c in $commits) { $textBlobs += [string]($c.body) }
foreach ($c in $issueCommentsRaw) { $textBlobs += [string]($c.body) }
foreach ($r in $reviewsRaw) { $textBlobs += [string]($r.body) }
foreach ($c in $reviewCommentsRaw) { $textBlobs += [string]($c.body) }

$refRegex = [regex]'(?i)(?:linq2db/linq2db)?#(\d+)|https?://github\.com/linq2db/linq2db/(?:issues|pull)/(\d+)'
$refsFound = [System.Collections.Generic.HashSet[int]]::new()
foreach ($blob in $textBlobs) {
    if (-not $blob) { continue }
    foreach ($match in $refRegex.Matches($blob)) {
        $val = if ($match.Groups[1].Success) { $match.Groups[1].Value } else { $match.Groups[2].Value }
        $n = 0
        if ([int]::TryParse($val, [ref]$n)) { [void]$refsFound.Add($n) }
    }
}
foreach ($n in $closingIssues) { [void]$refsFound.Add($n) }
[void]$refsFound.Remove($pr)
$linkedRefs = @($refsFound | Sort-Object)

# Fan-out linked issue fetches.
$linkedIssues = @()
if ($linkedRefs.Count -gt 0) {
    $linkedIssues = $linkedRefs | ForEach-Object -ThrottleLimit $linkedConcurrency -Parallel {
        . "$using:root/_shared.ps1"
        $num = $_
        $rf = $using:repoFull
        # Across linkedRefs we are already parallel; within a single issue the
        # two gh calls run sequentially — nested `$using:` into a child
        # Start-ThreadJob is not supported.
        $issueRes    = Invoke-GhJson @('api',"repos/$rf/issues/$num")
        $commentsRes = Invoke-GhJson @('api',"repos/$rf/issues/$num/comments",'--paginate')

        if (-not $issueRes.ok) {
            return [pscustomobject]@{ number = $num; error = $issueRes.error }
        }
        $iss = $issueRes.data
        $labels = @()
        if ($iss.labels) {
            foreach ($l in $iss.labels) {
                if ($l -is [string]) { $labels += $l } elseif ($l.name) { $labels += $l.name }
            }
        }
        $comments = @()
        if ($commentsRes.ok -and $commentsRes.data) {
            foreach ($c in $commentsRes.data) {
                $comments += [pscustomobject]@{
                    user = $c.user.login
                    created_at = $c.created_at
                    body = [string]$c.body
                }
            }
        }
        [pscustomobject]@{
            number = $num
            title  = [string]$iss.title
            body   = [string]$iss.body
            state  = [string]$iss.state
            labels = $labels
            milestone = if ($iss.milestone) { $iss.milestone.title } else { $null }
            isPullRequest = [bool]($iss.pull_request)
            comments = $comments
            error = if ($commentsRes.ok) { $null } else { "comments: $($commentsRes.error)" }
        }
    }
}

$reviews = foreach ($r in $reviewsRaw) {
    [pscustomobject]@{
        id = $r.id
        node_id = $r.node_id
        user = $r.user.login
        state = $r.state
        submitted_at = $r.submitted_at
        commit_id = $r.commit_id
        body = [string]$r.body
    }
}

$reviewComments = foreach ($c in $reviewCommentsRaw) {
    [pscustomobject]@{
        id = $c.id
        user = $c.user.login
        path = $c.path
        line = $c.line
        original_line = $c.original_line
        side = $c.side
        position = $c.position
        commit_id = $c.commit_id
        pull_request_review_id = $c.pull_request_review_id
        subject_type = if ($c.subject_type) { $c.subject_type } else { 'line' }
        body = [string]$c.body
    }
}

$issueComments = foreach ($c in $issueCommentsRaw) {
    [pscustomobject]@{
        user = $c.user.login
        created_at = $c.created_at
        body = [string]$c.body
    }
}

Write-JsonOutput ([pscustomobject]@{
    pr = $prMeta
    currentUser = $currentUser
    reviews = @($reviews)
    reviewComments = @($reviewComments)
    issueComments = @($issueComments)
    reviewThreads = @($reviewThreads)
    closingIssues = @($closingIssues)
    linkedRefs = @($linkedRefs)
    linkedIssues = @($linkedIssues)
    diffStat = $diffStat
    nameStatus = @($nameStatus)
    commits = @($commits)
    baseRef = $baseRef
    headRef = $headRef
    headSha = $headSha
})
