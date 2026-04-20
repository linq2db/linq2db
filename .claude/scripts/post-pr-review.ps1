#!/usr/bin/env pwsh
<#
Post a PENDING pull request review on linq2db/linq2db in one go.

Why this wrapper exists
-----------------------
`gh api --method POST repos/.../pulls/<n>/reviews` creates the review and its
line-level comments in a single call, but it cannot carry **file-level**
comments (DraftPullRequestReviewComment schema lacks subject_type, 422) and
it cannot carry **replies to existing threads** (no in_reply_to_id in the
comments[] schema). File-level comments must be attached after creation via
the GraphQL `addPullRequestReviewThread` mutation; reply-to-thread comments
attached to a pending review go through `addPullRequestReviewComment`
scoped by `pullRequestReviewId` + `inReplyTo`. That means a review with K
file-level findings and R reply follow-ups needs 1 + K + R separate
gh invocations, each of which re-triggers the permission prompt if the
exact command isn't allowlisted.

This script does all of that in one pwsh process:
  1. Read the review manifest (body, line / file / reply comments).
  2. POST the pending review (line comments inside, no `event` field).
  3. Attach each file-level finding via GraphQL, in parallel.
  4. Attach each thread reply via GraphQL (scoped to the pending review), in parallel.
  5. Emit a single JSON result to stdout.

Single permission rule suffices:
  Bash(pwsh -NoProfile -File .claude/scripts/post-pr-review.ps1:*)

Two ways to supply the manifest
-------------------------------
  (1) `-ManifestScript <path>` — a PowerShell script that returns a hashtable
      (or pscustomobject) with the fields below. This is the preferred path
      for `/review-pr` and `/verify-review`: the caller writes one `.ps1`
      with here-string comment bodies (no JSON escaping), and the wrapper
      dot-sources it. Compared to the old "one .md file per comment +
      bodyFile refs" pattern, this collapses 1+N+M file writes down to 1
      manifest-script file for a review of N line + M file/reply comments.
  (2) JSON on stdin — legacy path, still accepted. Useful for shell
      heredocs or when the manifest is already serialised.

Manifest schema
---------------
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
  "replyComments": [
    // inReplyTo must be the GraphQL node ID of the existing review comment
    // (e.g. PRRC_kwDOxxx...). The REST /pulls/<n>/comments endpoint returns
    // it as `node_id`. Replies are scoped to the new pending review so they
    // stay hidden until the user submits the draft.
    { "inReplyTo": "PRRC_kwDO...", "body": "…" }
  ],
  "concurrency": 5,
  "verify":     true                          // optional, default false
}

When `verify` is true, after posting the script fetches the stored review
body and line comments back from GitHub and byte-compares each against
what was sent. Mismatches surface in the output's `verify` block and
trigger exit code 2. Useful for catching encoding round-trips that
silently corrupt non-ASCII content (e.g. the stdin-UTF-8 bug that posted
`ΓÇö` instead of `—`). File-level comments and thread replies are not
verified — they travel via GraphQL `-f`/`-F` flags whose encoding is
handled by `gh` itself and was never part of the stdin path.

Output (stdout, single JSON object): { reviewId, nodeId, url, lineComments[],
fileThreads[], replyComments[], verify? }.

Exit codes
----------
  0 = review created; every file thread + reply attached; verify (if requested) clean
  1 = hard failure (review creation failed, bad input, gh missing, etc.)
  2 = review created but >=1 file thread / reply attach failed, or verify found a mismatch
#>

param([string]$ManifestScript)

$global:ScriptBaseName = 'post-pr-review'
. "$PSScriptRoot/_shared.ps1"

function Read-Manifest {
    param([string]$ManifestScript)
    if ($ManifestScript) {
        if (-not (Test-Path -LiteralPath $ManifestScript)) {
            Exit-WithError "ManifestScript not found: $ManifestScript"
        }
        try {
            $obj = & $ManifestScript
        } catch {
            Exit-WithError "ManifestScript execution failed: $($_.Exception.Message)"
        }
        # `&` on a .ps1 may emit multiple objects when the script has extra
        # statements; take the last one, which is the manifest hashtable.
        if ($obj -is [array]) { $obj = $obj[-1] }
        if ($obj -is [hashtable]) { $obj = [pscustomobject]$obj }
        if (-not ($obj -is [pscustomobject])) {
            Exit-WithError "ManifestScript must return a hashtable or pscustomobject; got $($obj.GetType().FullName)"
        }
        return $obj
    }
    return Read-StdinJson
}

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

$m = Read-Manifest -ManifestScript $ManifestScript

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
$replyComments = @()
if ($m.replyComments) { $replyComments = @($m.replyComments) }

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

# Attach each thread-reply follow-up as a pending comment scoped to the new
# review. `addPullRequestReviewComment` with both `pullRequestReviewId` (the
# new pending review) and `inReplyTo` (the existing comment) is the only
# mechanism that keeps the reply invisible until the user submits the draft
# — the REST /reviews comments[] schema has no in_reply_to_id field, and
# the dedicated /replies endpoint posts immediately (outside any draft).
$replyMutation = @'
mutation($rid:ID!, $cid:ID!, $body:String!) {
  addPullRequestReviewComment(input:{
    pullRequestReviewId: $rid,
    inReplyTo: $cid,
    body: $body
  }) {
    comment {
      id
      databaseId
    }
  }
}
'@

$replyCommentResults = @()
if ($replyComments.Count -gt 0) {
    $replyPayloads = @()
    for ($i = 0; $i -lt $replyComments.Count; $i++) {
        $rc = $replyComments[$i]
        if (-not ($rc.inReplyTo -is [string]) -or $rc.inReplyTo.Length -eq 0) {
            $replyCommentResults += [pscustomobject]@{ inReplyTo = $rc.inReplyTo; ok = $false; error = "replyComments[$i]: missing inReplyTo (GraphQL node ID, e.g. PRRC_kwDO...)" }
            continue
        }
        $b = Resolve-Body -Item $rc -Label "replyComments[$i]"
        $replyPayloads += [pscustomobject]@{ inReplyTo = [string]$rc.inReplyTo; body = $b }
    }

    $results = $replyPayloads | ForEach-Object -ThrottleLimit $concurrency -Parallel {
        . "$using:root/_shared.ps1"
        $rc = $_
        $res = Invoke-GhJson @(
            'api','graphql',
            '-f',"query=$using:replyMutation",
            '-F',"rid=$using:nodeId",
            '-f',"cid=$($rc.inReplyTo)",
            '-f',"body=$($rc.body)"
        )
        if (-not $res.ok) {
            return [pscustomobject]@{ inReplyTo = $rc.inReplyTo; ok = $false; error = $res.error }
        }
        $comment = $res.data.data.addPullRequestReviewComment.comment
        $cid = $comment.id
        $dbId = $comment.databaseId
        if (-not $cid) {
            return [pscustomobject]@{ inReplyTo = $rc.inReplyTo; ok = $false; error = "unexpected response: $($res.data | ConvertTo-Json -Depth 10 -Compress)" }
        }
        return [pscustomobject]@{ inReplyTo = $rc.inReplyTo; ok = $true; commentId = $cid; databaseId = $dbId }
    }
    $replyCommentResults += $results
}

# Optional post-publish round-trip verification. We already have the sent body
# and the resolved per-comment bodies in $comments[*].body — re-fetch from
# GitHub and compare so encoding regressions can't sneak by silently.
# Line endings are normalised to LF before compare: GitHub stores bodies with
# CRLF regardless of what we send, so an exact byte-compare would always fail.
$verifyResult = $null
if ($m.verify -eq $true) {
    function Test-BodyEquals {
        param([string]$A, [string]$B)
        return (($A -replace "`r`n", "`n") -ceq ($B -replace "`r`n", "`n"))
    }

    $verifyBody = $null
    $verifyLineComments = @()
    $verifyOk = $true

    $bodyFetch = Invoke-GhJson -ArgumentList @(
        'api', "repos/$owner/$repo/pulls/$pr/reviews/$reviewId"
    )
    if (-not $bodyFetch.ok) {
        $verifyBody = [pscustomobject]@{ ok = $false; error = "fetch failed: $($bodyFetch.error)" }
        $verifyOk = $false
    } else {
        $storedBody = [string]$bodyFetch.data.body
        $bodyOk = Test-BodyEquals $storedBody $body
        $verifyBody = [pscustomobject]@{
            ok = $bodyOk
            sentLength = $body.Length
            storedLength = $storedBody.Length
        }
        if (-not $bodyOk) { $verifyOk = $false }
    }

    if ($comments.Count -gt 0) {
        # `/reviews/<id>/comments` returns `line: null` for every comment — only
        # `position` is populated there, which we'd have to translate from
        # source lines. `/pulls/<n>/comments` returns real `line` values; filter
        # client-side to this review.
        # `--paginate` walks every page (gh merges results into a single JSON
        # array). Without it the fetch caps at the default 100 and verification
        # silently fails on PRs with more existing review comments.
        $commentFetch = Invoke-GhJson -ArgumentList @(
            'api', '--paginate', "repos/$owner/$repo/pulls/$pr/comments?per_page=100"
        )
        if (-not $commentFetch.ok) {
            $verifyLineComments = @(foreach ($c in $comments) {
                [pscustomobject]@{ path = $c.path; line = $c.line; ok = $false; error = "fetch failed: $($commentFetch.error)" }
            })
            $verifyOk = $false
        } else {
            $thisReview = @($commentFetch.data | Where-Object { $_.pull_request_review_id -eq $reviewId })
            # Bucket stored comments by (path, line-or-original_line) so duplicates
            # on the same line match positionally instead of all colliding.
            $buckets = @{}
            foreach ($s in $thisReview) {
                $ln = if ($null -ne $s.line) { [int]$s.line }
                       elseif ($null -ne $s.original_line) { [int]$s.original_line }
                       else { -1 }
                $key = "$($s.path)|$ln"
                if (-not $buckets.ContainsKey($key)) { $buckets[$key] = [System.Collections.Queue]::new() }
                [void]$buckets[$key].Enqueue($s)
            }
            $verifyLineComments = foreach ($c in $comments) {
                $key = "$($c.path)|$([int]$c.line)"
                if ($buckets.ContainsKey($key) -and $buckets[$key].Count -gt 0) {
                    $s = $buckets[$key].Dequeue()
                    $stBody = [string]$s.body
                    $cmpOk = Test-BodyEquals $stBody $c.body
                    [pscustomobject]@{
                        path = $c.path
                        line = $c.line
                        ok = $cmpOk
                        sentLength = $c.body.Length
                        storedLength = $stBody.Length
                    }
                } else {
                    [pscustomobject]@{
                        path = $c.path
                        line = $c.line
                        ok = $false
                        error = 'no matching stored comment'
                    }
                }
            }
            if (@($verifyLineComments | Where-Object { -not $_.ok }).Count -gt 0) { $verifyOk = $false }
        }
    }

    $verifyResult = [pscustomobject]@{
        ok = $verifyOk
        body = $verifyBody
        lineComments = @($verifyLineComments)
    }
}

$outputProps = [ordered]@{
    reviewId = $reviewId
    nodeId = $nodeId
    url = $url
    lineComments = @($lineCommentResults)
    fileThreads = @($fileThreadResults)
    replyComments = @($replyCommentResults)
}
if ($null -ne $verifyResult) { $outputProps.verify = $verifyResult }
$output = [pscustomobject]$outputProps

Write-JsonOutput $output

$fileThreadsFailed = @($fileThreadResults | Where-Object { -not $_.ok }).Count -gt 0
$replyCommentsFailed = @($replyCommentResults | Where-Object { -not $_.ok }).Count -gt 0
$anyFailed = $fileThreadsFailed -or $replyCommentsFailed
$verifyFailed = $null -ne $verifyResult -and -not $verifyResult.ok
if ($anyFailed -or $verifyFailed) { exit 2 } else { exit 0 }
