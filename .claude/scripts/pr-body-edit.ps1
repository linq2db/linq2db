#!/usr/bin/env pwsh
<#
Insert new content into a GitHub PR body at one or more ASCII anchors, in a
single allowlisted pwsh call. Solves two recurring problems when editing a PR
body from a Claude Code session:

  1. **Encoding.** `gh pr view` / `gh api … --jq '.body'` stdout arriving via
     Git Bash + pwsh gets decoded through the Windows console code page (cp850
     on most non-English locales), so any non-ASCII character in the body
     (emoji, em-dash, accented letters) becomes a 3–4 character garble in the
     pwsh string. Subsequent string matching / replacement then silently fails
     or — worse — pushes the garbled bytes back to GitHub.
  2. **Permission friction.** A scratch "fetch body, mutate, push" pwsh loop
     is typically 3–5 iterations because each round hits a fresh encoding /
     stringification surprise. Every retry is a new `Bash(pwsh -File ...)`
     command string that the allowlist evaluates afresh.

This script sidesteps both by:

  - Using `Invoke-Gh` from `_shared.ps1` (UTF-8 stdio on the process pipes).
  - Reading the current body via `gh api` into a temp file and then loading
    it with `[System.IO.File]::ReadAllText(path, UTF8NoBom)`.
  - Writing the new body back the same way.
  - Presenting a single allowlist rule:
        Bash(pwsh -NoProfile -File .claude/scripts/pr-body-edit.ps1:*)

Contract
--------

Input (stdin, JSON):
  {
    "pr":      5479,                       // required, int
    "owner":   "linq2db",                  // optional, default "linq2db"
    "repo":    "linq2db",                  // optional, default "linq2db"
    "insertions": [                        // required, non-empty
      {
        "anchor":   "## Test plan",        // ASCII literal, must match exactly once (or matchCount: "first"/"last" when multiple allowed)
        "position": "before",              // "before" | "after"; default "before"
        "text":     "…full text block…"    // inserted verbatim; caller is responsible for leading/trailing blank lines
      },
      {
        "anchor":   "Generated with [Claude Code]",
        "position": "before",
        "text":     "…"
      }
    ],
    "dryRun":  false,                      // optional, default false — when true, compute and write the candidate body but don't call `gh pr edit`
    "workDir": ".build/.claude"            // optional — where to stage the before/after body files; default ".build/.claude"
  }

Rules for `anchor`:
  - Must be ASCII (any UTF-8 is technically accepted but defeats the whole point — the script will reject non-ASCII anchors with a clear error).
  - Must appear in the body. By default must appear EXACTLY ONCE; callers that want the first or last of many duplicates can pass `"matchCount": "first"` or `"matchCount": "last"` on the entry.
  - String-literal match, not regex.

Output (stdout, JSON):
  {
    "pr":          5479,
    "url":         "https://github.com/linq2db/linq2db/pull/5479",
    "applied":     true,                   // false when dryRun
    "bodyBefore":  ".build/.claude/pr5479-body-before.txt",
    "bodyAfter":   ".build/.claude/pr5479-body-after.txt",
    "insertions": [
      { "anchor": "## Test plan", "position": "before", "ok": true, "insertedAt": 3912 },
      { "anchor": "Generated with [Claude Code]", "position": "before", "ok": true, "insertedAt": 7013 }
    ],
    "diffStat": { "charsBefore": 8012, "charsAfter": 8741 }
  }

Exit codes:
  0 = success (all insertions applied, optional `gh pr edit` succeeded)
  1 = hard failure (bad input, anchor not found, gh error, etc.)
#>

$global:ScriptBaseName = 'pr-body-edit'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson

if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'pr (positive integer) required' }
$pr    = [int]$m.pr
$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo  } else { 'linq2db' }
$repoFull = "$owner/$repo"
$dryRun   = [bool]$m.dryRun
$workDir  = if ($m.workDir) { [string]$m.workDir } else { '.build/.claude' }

if (-not $m.insertions -or @($m.insertions).Count -eq 0) {
    Exit-WithError 'insertions (non-empty array) required'
}

# --- Normalise insertion entries ------------------------------------------
$entries = @()
$idx = 0
foreach ($ins in @($m.insertions)) {
    $idx++
    if (-not $ins.anchor -or -not ([string]$ins.anchor)) { Exit-WithError "insertions[$idx].anchor is required" }
    $anchor = [string]$ins.anchor
    if ($anchor -cmatch '[^\x00-\x7F]') {
        Exit-WithError "insertions[$idx].anchor contains non-ASCII characters; anchors must be ASCII to survive round-tripping through native-command stdout"
    }
    $position = if ($ins.position) { [string]$ins.position } else { 'before' }
    if ($position -ne 'before' -and $position -ne 'after') {
        Exit-WithError "insertions[$idx].position must be 'before' or 'after'"
    }
    if ($null -eq $ins.text) { Exit-WithError "insertions[$idx].text is required" }
    $text = [string]$ins.text
    $matchCount = if ($ins.matchCount) { [string]$ins.matchCount } else { 'one' }
    if ($matchCount -notin @('one','first','last')) {
        Exit-WithError "insertions[$idx].matchCount must be 'one', 'first', or 'last'"
    }
    $entries += [pscustomobject]@{
        anchor     = $anchor
        position   = $position
        text       = $text
        matchCount = $matchCount
    }
}

# --- Fetch current body via file roundtrip (avoids console code-page decoding) -----
[void][System.IO.Directory]::CreateDirectory($workDir)
$bodyBeforePath = Join-Path $workDir "pr$pr-body-before.txt"
$bodyAfterPath  = Join-Path $workDir "pr$pr-body-after.txt"

$getResult = Invoke-Gh -ArgumentList @('api', "repos/$repoFull/pulls/$pr", '--jq', '.body')
if (-not $getResult.ok) { Exit-WithError "gh api pulls/$pr failed: $($getResult.error)" }
$body = $getResult.stdout
# `gh api --jq '.body'` appends a trailing newline; strip it so we don't grow the body on every edit
if ($body.EndsWith("`n")) { $body = $body.Substring(0, $body.Length - 1) }
# Normalise CRLF → LF so anchor matching and inserted text are EOL-agnostic
$body = $body.Replace("`r`n", "`n")

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
# Use GetFullPath against pwsh's current location so relative workDir values
# resolve under the repo root (not against .NET's Environment.CurrentDirectory,
# which can diverge from Get-Location), while absolute workDir values pass
# through unchanged.
$bodyBeforeAbs = [System.IO.Path]::GetFullPath($bodyBeforePath, (Get-Location).Path)
$bodyAfterAbs  = [System.IO.Path]::GetFullPath($bodyAfterPath,  (Get-Location).Path)
[System.IO.File]::WriteAllText($bodyBeforeAbs, $body, $utf8NoBom)

# --- Apply each insertion sequentially -------------------------------------
$results = @()
foreach ($e in $entries) {
    $indices = @()
    $from = 0
    while ($true) {
        $pos = $body.IndexOf($e.anchor, $from, [System.StringComparison]::Ordinal)
        if ($pos -lt 0) { break }
        $indices += $pos
        $from = $pos + 1
    }
    if ($indices.Count -eq 0) {
        Exit-WithError "anchor not found in PR body: $($e.anchor)"
    }
    if ($indices.Count -gt 1 -and $e.matchCount -eq 'one') {
        Exit-WithError "anchor appears $($indices.Count) times in PR body; set matchCount to 'first' or 'last' if duplicates are expected: $($e.anchor)"
    }
    $target = switch ($e.matchCount) {
        'last'  { $indices[-1] }
        default { $indices[0] }
    }
    $insertAt = if ($e.position -eq 'before') { $target } else { $target + $e.anchor.Length }
    # $body is rebuilt here so subsequent insertions search against the already-updated text.
    # Consequence: anchor offsets returned in the result are into the FINAL body, not the original.
    $body = $body.Substring(0, $insertAt) + $e.text + $body.Substring($insertAt)
    $results += [pscustomobject]@{
        anchor     = $e.anchor
        position   = $e.position
        ok         = $true
        insertedAt = $insertAt
    }
}

# --- Collapse triple+ blank-line runs introduced by concatenation -----------
# Not always desired, but common enough that we do it by default. Callers who
# need exact whitespace preservation should shape their `text` payloads to
# avoid leading/trailing blank lines beyond one each.
$body = $body -replace "\n{3,}", "`n`n"

[System.IO.File]::WriteAllText($bodyAfterAbs, $body, $utf8NoBom)

$applied = $false
if (-not $dryRun) {
    $editResult = Invoke-Gh -ArgumentList @('pr', 'edit', "$pr", '--repo', $repoFull, '--body-file', $bodyAfterPath)
    if (-not $editResult.ok) { Exit-WithError "gh pr edit $pr failed: $($editResult.error)" }
    $applied = $true
}

Write-JsonOutput ([pscustomobject]@{
    pr         = $pr
    url        = "https://github.com/$repoFull/pull/$pr"
    applied    = $applied
    dryRun     = $dryRun
    bodyBefore = $bodyBeforePath
    bodyAfter  = $bodyAfterPath
    insertions = $results
    diffStat   = [pscustomobject]@{
        charsBefore = (Get-Content -LiteralPath $bodyBeforePath -Raw -Encoding utf8).Length
        charsAfter  = $body.Length
    }
})
