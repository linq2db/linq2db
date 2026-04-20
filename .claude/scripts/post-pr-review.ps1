#!/usr/bin/env pwsh
<#
Post a PENDING pull request review on linq2db/linq2db in one go.

Why this wrapper exists
-----------------------
`gh api --method POST repos/.../pulls/<n>/reviews` creates the review and its
line-level comments in a single call, but it cannot carry **file-level**
comments — the DraftPullRequestReviewComment schema lacks subject_type and
the request is rejected with 422 (see .claude/docs/github-review-api.md).
File-level comments must be attached after creation via the GraphQL
`addPullRequestReviewThread` mutation, one call per file. That means a
`review-pr` run with K file-level findings needs K+1 separate Bash / gh
invocations, each of which re-triggers the permission prompt if the exact
command isn't allowlisted.

This script does all of that in one pwsh process:
  1. Read the review manifest (body, line comments, file comments) from stdin.
  2. POST the pending review (line comments inside, no `event` field).
  3. Attach each file-level finding via GraphQL, in parallel.
  4. Emit a single JSON result to stdout.

Single permission rule suffices:
  Bash(pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1:*)

Manifest schema (stdin, JSON)
-----------------------------
{
  "pr":       5414,                          // required, int
  "commitId": "24bb14ee9d...",               // required, full PR head SHA
  "owner":    "linq2db",                     // optional, default "linq2db"
  "repo":     "linq2db",                     // optional, default "linq2db"
  "body":     "…review body markdown…",      // required — or bodyFile instead
  "bodyFile": ".build/.claude/review-5414.md",
  "lineComments": [
    { "path": "Source/...cs", "line": 42, "side": "RIGHT", "startLine": 40, "startSide": "RIGHT", "body": "…" }
  ],
  "fileComments": [
    { "path": "...", "body": "…" }
  ],
  "concurrency": 5
}

Output (stdout, single JSON object): { reviewId, nodeId, url, lineComments[],
fileThreads[] }.

Exit codes
----------
  0 = review created; every file thread attached
  1 = hard failure (review creation failed, bad input, gh missing, etc.)
  2 = review created but >=1 file thread attach failed
#>

$global:ScriptBaseName = 'post-pr-review'
. "$PSScriptRoot/_shared.ps1"

function Resolve-Body {
    param($Item, [string]$Label)
    if ($Item.body -is [string] -and $Item.body.Length -gt 0) { return $Item.body }
    if ($Item.bodyFile -is [string] -and $Item.bodyFile.Length -gt 0) {
        try {
            return (Get-Content -Raw -Encoding utf8 -LiteralPath $Item.bodyFile)
        } catch {
            Exit-WithError "${Label}: failed to read bodyFile=$($Item.bodyFile): $($_.Exception.Message)"
        }
    }
    Exit-WithError "${Label}: missing body (provide either `"body`" or `"bodyFile`")"
}

$m = Read-StdinJson

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'pr (integer) required' }
$pr = [int]$m.pr
$commitId = $m.commitId
if (-not ($commitId -is [string]) -or $commitId.Length -lt 7) { Exit-WithError 'commitId (string) required' }

$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$concurrency = if ((Test-IsInteger $m.concurrency) -and [long]$m.concurrency -gt 0) { [int]$m.concurrency } else { 5 }

$body = Resolve-Body -Item $m -Label 'review body'
$lineComments = @()
if ($m.lineComments) { $lineComments = @($m.lineComments) }
$fileComments = @()
if ($m.fileComments) { $fileComments = @($m.fileComments) }

# Build the REST payload. Line comments go inside; file comments attach via GraphQL after.
$comments = @()
for ($i = 0; $i -lt $lineComments.Count; $i++) {
    $lc = $lineComments[$i]
    if (-not ($lc.path -is [string])) { Exit-WithError "lineComments[$i]: missing path" }
    if (-not (Test-IsInteger $lc.line)) { Exit-WithError "lineComments[$i]: missing line (integer)" }
    $b = Resolve-Body -Item $lc -Label "lineComments[$i]"
    $side = if ($lc.side) { [string]$lc.side } else { 'RIGHT' }
    $c = [ordered]@{
        path = [string]$lc.path
        line = [int]$lc.line
        side = $side
        body = $b
    }
    if (Test-IsInteger $lc.startLine) {
        $c.start_line = [int]$lc.startLine
        $c.start_side = if ($lc.startSide) { [string]$lc.startSide } else { $side }
    }
    $comments += [pscustomobject]$c
}

$payload = [pscustomobject]@{
    commit_id = $commitId
    body = $body
    comments = $comments
}

$payloadJson = $payload | ConvertTo-Json -Depth 100 -Compress

$createRes = Invoke-GhJson -ArgumentList @(
    'api','--method','POST',"repos/$owner/$repo/pulls/$pr/reviews",'--input','-'
) -StdinInput $payloadJson
if (-not $createRes.ok) { Exit-WithError "failed to create review: $($createRes.error)" }

$review = $createRes.data
$reviewId = $review.id
$nodeId = $review.node_id
$url = $review.html_url
if (-not $reviewId -or -not $nodeId) {
    Exit-WithError "review response missing id/node_id:`n$(($review | ConvertTo-Json -Depth 100))"
}

$lineCommentResults = foreach ($lc in $lineComments) {
    [pscustomobject]@{ path = [string]$lc.path; line = [int]$lc.line; ok = $true }
}

# `foreach` over a zero-element list yields $null, not @(). Coerce for output.
if ($null -eq $lineCommentResults) { $lineCommentResults = @() }

# Attach each file-level finding as its own thread via GraphQL.
$mutation = @'
mutation($rid:ID!, $path:String!, $body:String!) {
  addPullRequestReviewThread(input:{
    pullRequestReviewId: $rid,
    subjectType: FILE,
    path: $path,
    body: $body
  }) {
    thread {
      id
      comments(first:1) { nodes { databaseId } }
    }
  }
}
'@

$root = $PSScriptRoot
$fileThreadResults = @()
if ($fileComments.Count -gt 0) {
    $payloads = @()
    for ($i = 0; $i -lt $fileComments.Count; $i++) {
        $fc = $fileComments[$i]
        if (-not ($fc.path -is [string])) {
            $fileThreadResults += [pscustomobject]@{ path = $fc.path; ok = $false; error = "fileComments[$i]: missing path" }
            continue
        }
        $b = Resolve-Body -Item $fc -Label "fileComments[$i]"
        $payloads += [pscustomobject]@{ path = [string]$fc.path; body = $b }
    }

    $results = $payloads | ForEach-Object -ThrottleLimit $concurrency -Parallel {
        . "$using:root/_shared.ps1"
        $fc = $_
        $res = Invoke-GhJson @(
            'api','graphql',
            '-f',"query=$using:mutation",
            '-F',"rid=$using:nodeId",
            '-f',"path=$($fc.path)",
            '-f',"body=$($fc.body)"
        )
        if (-not $res.ok) {
            return [pscustomobject]@{ path = $fc.path; ok = $false; error = $res.error }
        }
        $thread = $res.data.data.addPullRequestReviewThread.thread
        $tid = $thread.id
        $dbId = if ($thread.comments.nodes -and $thread.comments.nodes.Count -gt 0) { $thread.comments.nodes[0].databaseId } else { $null }
        if (-not $tid) {
            return [pscustomobject]@{ path = $fc.path; ok = $false; error = "unexpected response: $($res.data | ConvertTo-Json -Depth 10 -Compress)" }
        }
        return [pscustomobject]@{ path = $fc.path; ok = $true; threadId = $tid; databaseId = $dbId }
    }
    $fileThreadResults += $results
}

$output = [pscustomobject]@{
    reviewId = $reviewId
    nodeId = $nodeId
    url = $url
    lineComments = @($lineCommentResults)
    fileThreads = @($fileThreadResults)
}

Write-JsonOutput $output

$anyFailed = @($fileThreadResults | Where-Object { -not $_.ok }).Count -gt 0
if ($anyFailed) { exit 2 } else { exit 0 }
