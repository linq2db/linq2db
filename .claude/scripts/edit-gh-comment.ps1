#!/usr/bin/env pwsh
<#
PATCH a GitHub issue / PR-issue comment body in one allowlisted pwsh call,
with built-in byte-compare verification.

Why this wrapper exists
-----------------------
`gh api --method PATCH repos/.../issues/comments/<id> -f body=@<file>` does
NOT read the file — it stores the literal string `@<file>` as the body. Same
trap as `gh ... --body @-` (already banned in `agent-rules.md`). The right
shape is `--input <json-file>` with a JSON wrapper `{"body": "..."}` whose
string value is properly escaped.

Building that wrapper inline is fiddly enough that even careful agent runs
get it wrong (motivating session: PR #5467 release-notes-draft PATCH on
2026-05-06 stored the literal `@.build/.claude/pr5467-release-notes-draft.md`
as the comment body). This script does the wrapper construction, the PATCH,
and a byte-compare verification in one allowlisted call:

    Bash(pwsh -NoProfile -File .claude/scripts/edit-gh-comment.ps1 *)

Contract
--------

Input (stdin, JSON):
  {
    "commentId": 4389710243,                // required, positive int
    "owner":     "linq2db",                 // optional, default "linq2db"
    "repo":      "linq2db",                 // optional, default "linq2db"
    "kind":      "issue",                   // optional, default "issue".
                                            // Currently only "issue" is supported (covers issue + PR-issue
                                            // comments — both use repos/<o>/<r>/issues/comments/<id>).
                                            // Future: "review-comment", "review-body".
    "bodyFile":  ".build/.claude/foo.md",   // OR
    "body":      "...inline markdown..."    // — exactly one of these must be set
  }

Reads `bodyFile` as UTF-8 (no BOM expected; if present it's stripped before send,
so the PATCH never produces a garbled byte at the start of the comment).

Output (stdout, JSON):
  {
    "ok":        true,
    "commentId": 4389710243,
    "url":       "https://github.com/<owner>/<repo>/issues/.../#issuecomment-...",
    "verify": {
      "ok":           true,                 // false if storedLength != sentLength or content differs
      "sentLength":   14214,
      "storedLength": 14214
    }
  }

Exit codes:
  0 = PATCH succeeded and verify is clean
  1 = hard failure (bad input, gh error, etc.)
  2 = PATCH succeeded but verify found a mismatch — investigate before relying on the edit
#>

param([string]$ManifestFile)

$global:ScriptBaseName = 'edit-gh-comment'
. "$PSScriptRoot/_shared.ps1"

$m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile

if (-not (Test-IsInteger $m.commentId) -or [long]$m.commentId -le 0) {
    Exit-WithError 'commentId (positive integer) required'
}
$commentId = [int]$m.commentId
$owner     = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo      = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$kind      = if ($m.kind)  { [string]$m.kind }  else { 'issue' }
$repoFull  = "$owner/$repo"

if ($kind -ne 'issue') {
    Exit-WithError "kind must be 'issue' (only issue / PR-issue comments are supported today; got '$kind')"
}

$hasBody     = ($m.body     -is [string]) -and ($m.body.Length     -gt 0)
$hasBodyFile = ($m.bodyFile -is [string]) -and ($m.bodyFile.Length -gt 0)
if ($hasBody -and $hasBodyFile) {
    Exit-WithError "exactly one of 'body' or 'bodyFile' must be set, not both"
}
if (-not $hasBody -and -not $hasBodyFile) {
    Exit-WithError "exactly one of 'body' or 'bodyFile' must be set"
}

if ($hasBodyFile) {
    if (-not (Test-Path -LiteralPath $m.bodyFile)) {
        Exit-WithError "bodyFile not found: $($m.bodyFile)"
    }
    try {
        # ReadAllText with explicit UTF-8 strips BOM if present and decodes correctly regardless of console codepage.
        $body = [System.IO.File]::ReadAllText($m.bodyFile, [System.Text.UTF8Encoding]::new($false))
    } catch {
        Exit-WithError "failed to read bodyFile=$($m.bodyFile): $($_.Exception.Message)"
    }
} else {
    $body = [string]$m.body
}

# Stage the JSON payload as a file so we route through `gh api --input <path>`,
# which is the only reliable shape for a non-trivial body. Stdin would also work
# but adds Bash-pipe encoding surface on Windows that we want to keep avoiding.
$workDir = '.build/.claude'
[void][System.IO.Directory]::CreateDirectory($workDir)
$payloadPath = Join-Path $workDir "edit-gh-comment-$commentId.json"
$payloadAbs  = [System.IO.Path]::GetFullPath($payloadPath, (Get-Location).Path)

$payloadJson = ([pscustomobject]@{ body = $body } | ConvertTo-Json -Depth 100 -Compress)
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($payloadAbs, $payloadJson, $utf8NoBom)

# Endpoint depends on kind. Today only "issue" is supported.
$endpoint = "repos/$repoFull/issues/comments/$commentId"

$patchResult = Invoke-GhJson -ArgumentList @('api', '--method', 'PATCH', $endpoint, '--input', $payloadPath)
if (-not $patchResult.ok) { Exit-WithError "gh api PATCH $endpoint failed: $($patchResult.error)" }

$resp = $patchResult.data
$url  = if ($resp.html_url) { [string]$resp.html_url } else { '' }

# Verify: re-fetch the comment body and byte-compare to what we sent.
$getResult = Invoke-Gh -ArgumentList @('api', $endpoint, '--jq', '.body')
$verifyOk        = $false
$storedLen       = -1
$sentLen         = [System.Text.Encoding]::UTF8.GetByteCount($body)
if ($getResult.ok) {
    $stored = $getResult.stdout
    if ($stored.EndsWith("`n")) { $stored = $stored.Substring(0, $stored.Length - 1) }
    # Normalise CRLF → LF on the stored side; we send LF-only via JSON, GitHub stores LF, but
    # `gh api --jq` round-trip can introduce CR on Windows.
    $stored = $stored.Replace("`r`n", "`n")
    $storedLen = [System.Text.Encoding]::UTF8.GetByteCount($stored)
    $verifyOk = ($stored -ceq $body)
}

$exitCode = if ($verifyOk) { 0 } else { 2 }

Write-JsonOutput ([pscustomobject]@{
    ok        = $verifyOk
    commentId = $commentId
    url       = $url
    verify    = [pscustomobject]@{
        ok           = $verifyOk
        sentLength   = $sentLen
        storedLength = $storedLen
    }
})

exit $exitCode
