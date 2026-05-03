#!/usr/bin/env pwsh
<#
Apply the in-place edit batch produced by `/verify-review` step 9 in one pwsh
process.

Why this wrapper exists
-----------------------
`/verify-review` decides per prior finding (see SKILL.md step 7 table):
  - fixed × line  → PATCH the comment body: append "— ✓ Fixed in <sha>"
  - fixed × file  → PATCH the comment body (same annotation)
  - fixed × body  → PUT the prior review body: flip `[ ]` → `[x]`
  - partial × body → PUT the prior review body: flip `[ ]` → `[~]`
  - plus optional per-thread GraphQL `resolveReviewThread` mutations

Done inline from the skill, this is 1 PATCH per fixed line/file comment + 1
PUT per edited review body + 1 GraphQL mutation per thread resolve. Each
unique command string triggers its own permission prompt. Done here, one
allowlist rule covers the whole batch:

    Bash(pwsh -NoProfile -File .claude/scripts/apply-verify-writes.ps1:*)

The script fetches the current comment body for each commentPatches[] entry
before PATCHing. That spares the caller from duplicating the original body
text in its manifest, and it keeps the append idempotent: if the exact
appendNote already appears anywhere in the current body, we skip the write
(and mark the entry `ok: true, skipped: "already annotated"`). Uniqueness
is carried by the run's HEAD SHA embedded in the note.

Input (stdin, JSON)
-------------------
  {
    "pr":            5414,                             // required, int
    "owner":         "linq2db",                        // optional, default "linq2db"
    "repo":          "linq2db",                        // optional, default "linq2db"
    "appendNote":    "— ✓ Fixed in 83d743bb",         // required when commentPatches[] is non-empty
    "commentPatches": [                                // optional — append `appendNote` to these comments
      3109629364, 3109629397                          //   bare integers OR { "commentId": N, "appendNote": "..." }
    ],                                                 //   (per-entry appendNote overrides the top-level default)
    "threadResolves": [                                // optional — resolveReviewThread mutations
      "PRRT_kwDOAB09RM58JUqw"                         //   bare strings OR { "threadId": "...", "label": "MIN001" }
    ],
    "reviewBodyEdits": [                               // optional — PUT prior review body
      { "reviewId": 4138820911, "newBody": "<full replacement body>" }
    ],
    "concurrency": 6                                   // optional, default 6
  }

Output (stdout, single JSON object):
  {
    "commentPatches":  [ { commentId, ok, skipped?, error? } ],
    "threadResolves":  [ { threadId, label?, ok, isResolved?, error? } ],
    "reviewBodyEdits": [ { reviewId, ok, storedLength?, error? } ]
  }

Exit codes
----------
  0 = every request succeeded (or was skipped as already-applied)
  1 = hard failure (bad input, missing required field, gh not on PATH)
  2 = at least one write failed; inspect the output for per-item `ok: false`
#>

$global:ScriptBaseName = 'apply-verify-writes'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'pr (positive integer) required' }
$pr = [int]$m.pr
$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$repoFull = "$owner/$repo"
$defaultAppendNote = if ($m.appendNote) { [string]$m.appendNote } else { '' }
$concurrency = if ((Test-IsInteger $m.concurrency) -and [long]$m.concurrency -gt 0) { [int]$m.concurrency } else { 6 }

# --- Normalise commentPatches input ---------------------------------------
$commentPatchJobs = @()
if ($m.commentPatches) {
    foreach ($entry in @($m.commentPatches)) {
        $cid = $null
        $note = $defaultAppendNote
        if (Test-IsInteger $entry) {
            $cid = [long]$entry
        } elseif ($entry -is [pscustomobject] -or $entry -is [hashtable]) {
            $obj = if ($entry -is [hashtable]) { [pscustomobject]$entry } else { $entry }
            if (Test-IsInteger $obj.commentId) { $cid = [long]$obj.commentId }
            if ($obj.appendNote) { $note = [string]$obj.appendNote }
        }
        if ($null -eq $cid) { Exit-WithError "commentPatches: each entry must be an integer id or { commentId, appendNote? }" }
        if (-not $note) { Exit-WithError "commentPatches[$cid]: no appendNote (set top-level appendNote or per-entry appendNote)" }
        $commentPatchJobs += [pscustomobject]@{ commentId = $cid; appendNote = $note }
    }
}

# --- Normalise threadResolves input ---------------------------------------
$threadResolveJobs = @()
if ($m.threadResolves) {
    foreach ($entry in @($m.threadResolves)) {
        $tid = $null
        $label = $null
        if ($entry -is [string]) {
            $tid = $entry
        } elseif ($entry -is [pscustomobject] -or $entry -is [hashtable]) {
            $obj = if ($entry -is [hashtable]) { [pscustomobject]$entry } else { $entry }
            if ($obj.threadId -is [string]) { $tid = [string]$obj.threadId }
            if ($obj.label) { $label = [string]$obj.label }
        }
        if (-not $tid) { Exit-WithError "threadResolves: each entry must be a string threadId or { threadId, label? }" }
        $threadResolveJobs += [pscustomobject]@{ threadId = $tid; label = $label }
    }
}

# --- Normalise reviewBodyEdits input --------------------------------------
$reviewBodyEditJobs = @()
if ($m.reviewBodyEdits) {
    foreach ($entry in @($m.reviewBodyEdits)) {
        $obj = if ($entry -is [hashtable]) { [pscustomobject]$entry } else { $entry }
        if (-not (Test-IsInteger $obj.reviewId)) { Exit-WithError "reviewBodyEdits: reviewId (integer) required" }
        if (-not ($obj.newBody -is [string])) { Exit-WithError "reviewBodyEdits[$($obj.reviewId)]: newBody (string) required" }
        $reviewBodyEditJobs += [pscustomobject]@{ reviewId = [long]$obj.reviewId; newBody = [string]$obj.newBody }
    }
}

$totalJobs = $commentPatchJobs.Count + $threadResolveJobs.Count + $reviewBodyEditJobs.Count
if ($totalJobs -eq 0) { Exit-WithError 'no work: at least one of commentPatches / threadResolves / reviewBodyEdits must be non-empty' }

$root = $PSScriptRoot

# --- Run commentPatches in parallel (GET current body, append, PATCH) -----
$commentPatchResults = @()
if ($commentPatchJobs.Count -gt 0) {
    $commentPatchResults = $commentPatchJobs | ForEach-Object -ThrottleLimit $concurrency -Parallel {
        . "$using:root/_shared.ps1"
        $job = $_
        $rf = $using:repoFull
        $cid = $job.commentId
        $note = $job.appendNote

        $getRes = Invoke-GhJson @('api',"repos/$rf/pulls/comments/$cid")
        if (-not $getRes.ok) {
            return [pscustomobject]@{ commentId = $cid; ok = $false; error = "GET failed: $($getRes.error)" }
        }
        $currentBody = [string]$getRes.data.body

        # Idempotence: skip the PATCH when the exact appendNote is already
        # anywhere in the body. The note embeds the run's HEAD SHA, so a match
        # proves this exact annotation was appended before; noise from a
        # different SHA falls through and is a genuine "re-append".
        if ($currentBody.Contains($note)) {
            return [pscustomobject]@{ commentId = $cid; ok = $true; skipped = 'already annotated' }
        }

        $newBody = $currentBody + "`n`n" + $note
        $payload = [pscustomobject]@{ body = $newBody } | ConvertTo-Json -Depth 3 -Compress
        $patchRes = Invoke-GhJson -ArgumentList @(
            'api','--method','PATCH',"repos/$rf/pulls/comments/$cid",'--input','-'
        ) -StdinInput $payload
        if (-not $patchRes.ok) {
            return [pscustomobject]@{ commentId = $cid; ok = $false; error = "PATCH failed: $($patchRes.error)" }
        }
        return [pscustomobject]@{ commentId = $cid; ok = $true }
    }
}
if ($null -eq $commentPatchResults) { $commentPatchResults = @() }

# --- Run threadResolves in parallel ---------------------------------------
$threadResolveResults = @()
if ($threadResolveJobs.Count -gt 0) {
    $resolveMutation = 'mutation($tid:ID!){resolveReviewThread(input:{threadId:$tid}){thread{isResolved}}}'
    $threadResolveResults = $threadResolveJobs | ForEach-Object -ThrottleLimit $concurrency -Parallel {
        . "$using:root/_shared.ps1"
        $job = $_
        $res = Invoke-GhJson @(
            'api','graphql',
            '-f',"query=$using:resolveMutation",
            '-F',"tid=$($job.threadId)"
        )
        if (-not $res.ok) {
            return [pscustomobject]@{ threadId = $job.threadId; label = $job.label; ok = $false; error = $res.error }
        }
        $thread = $res.data.data.resolveReviewThread.thread
        return [pscustomobject]@{ threadId = $job.threadId; label = $job.label; ok = $true; isResolved = [bool]$thread.isResolved }
    }
}
if ($null -eq $threadResolveResults) { $threadResolveResults = @() }

# --- Run reviewBodyEdits in parallel (PUT the full body per review) -------
$reviewBodyEditResults = @()
if ($reviewBodyEditJobs.Count -gt 0) {
    $reviewBodyEditResults = $reviewBodyEditJobs | ForEach-Object -ThrottleLimit $concurrency -Parallel {
        . "$using:root/_shared.ps1"
        $job = $_
        $rf = $using:repoFull
        $pr = $using:pr
        $rid = $job.reviewId
        $payload = [pscustomobject]@{ body = $job.newBody } | ConvertTo-Json -Depth 3 -Compress
        $putRes = Invoke-GhJson -ArgumentList @(
            'api','--method','PUT',"repos/$rf/pulls/$pr/reviews/$rid",'--input','-'
        ) -StdinInput $payload
        if (-not $putRes.ok) {
            return [pscustomobject]@{ reviewId = $rid; ok = $false; error = "PUT failed: $($putRes.error)" }
        }
        $storedBody = [string]$putRes.data.body
        return [pscustomobject]@{ reviewId = $rid; ok = $true; storedLength = $storedBody.Length }
    }
}
if ($null -eq $reviewBodyEditResults) { $reviewBodyEditResults = @() }

$out = [pscustomobject]@{
    commentPatches  = @($commentPatchResults)
    threadResolves  = @($threadResolveResults)
    reviewBodyEdits = @($reviewBodyEditResults)
}
Write-JsonOutput $out

$anyFailed = @(
    @($commentPatchResults)  | Where-Object { -not $_.ok }
    @($threadResolveResults) | Where-Object { -not $_.ok }
    @($reviewBodyEditResults) | Where-Object { -not $_.ok }
).Count -gt 0

if ($anyFailed) { exit 2 } else { exit 0 }
