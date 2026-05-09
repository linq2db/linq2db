#!/usr/bin/env pwsh
<#
Post replies to N PR review-comment threads and resolve each thread, in one
allowlisted pwsh call. Two API hops per item:

  1. POST repos/{o}/{r}/pulls/{n}/comments/{commentId}/replies   — the reply
  2. GraphQL `resolveReviewThread(input:{threadId})`             — the resolve

Why this wrapper exists
-----------------------
Bulk thread cleanup (a typical Copilot review with N stale claims) otherwise
requires 2N raw `gh api` calls plus per-call body-encoding handling. Routing
each reply through `--input <json-file>` avoids the `gh api -f body=@<file>`
literal-string trap (and the stdin-pipe encoding mangling that `_shared.ps1`
documents). The script also gracefully reports per-item failures so a partial
outage doesn't lose track of what landed.

Contract
--------

Input (stdin, JSON):
  {
    "owner": "linq2db",                       // optional, default "linq2db"
    "repo":  "linq2db",                       // optional, default "linq2db"
    "pr":    5451,                            // required, positive int
    "items": [
      {
        "threadId":  "PRRT_kwDOAB09RM549FlA", // required, GraphQL node ID
        "commentId": 3037904273,              // required, REST comment ID (positive int)
        "body":      "Fixed at HEAD - ...",   // required, non-empty markdown
        "resolve":   true                     // optional, default true; set false to only reply
      },
      ...
    ]
  }

Output (stdout, JSON):
  {
    "ok": true,                               // false if any item failed
    "items": [
      {
        "threadId":  "...",
        "commentId": 3037904273,
        "ok":        true,
        "replyId":   3213692186,              // present only on successful reply
        "resolved":  true,                    // present only on successful resolve (or false if requested-but-failed)
        "error":     "..."                    // present only when ok=false
      },
      ...
    ],
    "summary": { "total": 11, "ok": 11, "failed": 0 }
  }

Exit codes:
  0 = every item posted reply + (optional) resolve cleanly
  1 = hard error (bad input)
  2 = at least one item failed; output identifies which so the caller can retry
#>

$global:ScriptBaseName = 'post-pr-thread-replies'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson

# --- input validation ---

$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) {
    Exit-WithError 'pr (positive integer) required'
}
$pr = [int]$m.pr

if (-not $m.items -or $m.items.Count -eq 0) {
    Exit-WithError 'items (non-empty array) required'
}

$workDir = '.build/.claude'
[void][System.IO.Directory]::CreateDirectory($workDir)

$resolveQuery = 'mutation($threadId: ID!) { resolveReviewThread(input: {threadId: $threadId}) { thread { isResolved } } }'

$results = New-Object System.Collections.Generic.List[object]
$failedCount = 0

for ($i = 0; $i -lt $m.items.Count; $i++) {
    $item = $m.items[$i]
    $idx  = $i + 1

    # Per-item validation; record but don't throw — keep going so a single bad
    # entry doesn't abort the rest of the batch.
    if (-not (Test-IsInteger $item.commentId) -or [long]$item.commentId -le 0) {
        $results.Add([pscustomobject]@{
            ok = $false; error = "items[$idx]: commentId (positive integer) required"
        })
        $failedCount++
        continue
    }
    if (-not $item.threadId -or -not ($item.threadId -is [string]) -or $item.threadId.Length -eq 0) {
        $results.Add([pscustomobject]@{
            ok = $false; commentId = [int]$item.commentId
            error = "items[$idx]: threadId (non-empty string) required"
        })
        $failedCount++
        continue
    }
    if (-not $item.body -or -not ($item.body -is [string]) -or $item.body.Length -eq 0) {
        $results.Add([pscustomobject]@{
            ok = $false; commentId = [int]$item.commentId; threadId = [string]$item.threadId
            error = "items[$idx]: body (non-empty string) required"
        })
        $failedCount++
        continue
    }

    $commentId = [int]$item.commentId
    $threadId  = [string]$item.threadId
    $body      = [string]$item.body
    $shouldResolve = if ($null -eq $item.resolve) { $true } else { [bool]$item.resolve }

    # 1) POST reply via JSON file (avoids -f body=@file trap and stdin mangling).
    $payloadPath = Join-Path $workDir "post-pr-thread-replies-$pr-$commentId.json"
    $payloadJson = ([pscustomobject]@{ body = $body } | ConvertTo-Json -Depth 100 -Compress)
    $utf8NoBom   = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($payloadPath, $payloadJson, $utf8NoBom)

    $endpoint = "repos/$owner/$repo/pulls/$pr/comments/$commentId/replies"
    $replyResult = Invoke-GhJson -ArgumentList @('api', '--method', 'POST', $endpoint, '--input', $payloadPath)
    if (-not $replyResult.ok) {
        $results.Add([pscustomobject]@{
            ok = $false; commentId = $commentId; threadId = $threadId
            error = "reply failed: $($replyResult.error)"
        })
        $failedCount++
        continue
    }
    $replyId = if ($replyResult.data.id) { [int]$replyResult.data.id } else { 0 }

    # 2) Resolve thread via GraphQL (when requested).
    if (-not $shouldResolve) {
        $results.Add([pscustomobject]@{
            ok = $true; commentId = $commentId; threadId = $threadId; replyId = $replyId
            resolved = $false
        })
        continue
    }

    $resolveResult = Invoke-GhJson -ArgumentList @('api', 'graphql', '-f', "query=$resolveQuery", '-F', "threadId=$threadId")
    if (-not $resolveResult.ok) {
        $results.Add([pscustomobject]@{
            ok = $false; commentId = $commentId; threadId = $threadId; replyId = $replyId
            resolved = $false
            error = "resolve failed: $($resolveResult.error)"
        })
        $failedCount++
        continue
    }

    $isResolved = $false
    try {
        $isResolved = [bool]$resolveResult.data.data.resolveReviewThread.thread.isResolved
    } catch { }

    $results.Add([pscustomobject]@{
        ok = $true; commentId = $commentId; threadId = $threadId; replyId = $replyId
        resolved = $isResolved
    })
}

$totalCount = $results.Count
$okCount    = $totalCount - $failedCount

Write-JsonOutput ([pscustomobject]@{
    ok      = ($failedCount -eq 0)
    items   = $results
    summary = [pscustomobject]@{ total = $totalCount; ok = $okCount; failed = $failedCount }
})

if ($failedCount -gt 0) { exit 2 }
exit 0
