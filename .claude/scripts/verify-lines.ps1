#!/usr/bin/env pwsh
<#
Batch line-number verifier for code-reviewer findings.

Why this wrapper exists
-----------------------
GitHub's review API silently converts `line` + `side` into a diff `position`
and discards `line`. A wrong-but-in-hunk line number is accepted and the
comment lands on unrelated code with no error signal. The only defence is
to verify each finding's line number against the PR-head file AND against
the right-side hunk boundaries before posting.

    Bash(pwsh -NoProfile -File .claude/scripts/verify-lines.ps1:*)

Input (stdin, JSON)
-------------------
  {
    "pr":       5414,                 // optional — sets default headRef
    "headRef":  "origin/pr/5414",     // optional, default "origin/pr/<pr>"
    "baseRef":  "origin/master",      // optional
    "findings": [
      {
        "id":       "BLK001",
        "file":     "Source/...cs",
        "line":     42,
        "line_end": 47,
        "snippet":  "verbatim code…"
      }
    ]
  }

Output (stdout, single JSON object): per-finding { id, file, line, line_end,
ok, snippetMatched, inHunk, correctedLine, correctedLineEnd, reason }.

Verification semantics
----------------------
  - `snippetMatched=true` when the content at `[line, line_end ?? line]` in
    the head file equals `snippet` modulo trailing whitespace on each line
    (tabs/indentation are load-bearing — trailing whitespace only is trimmed).
  - `inHunk=true` when `[line, line_end ?? line]` overlaps a right-side hunk.
  - `ok = snippetMatched && inHunk`.
  - When `snippetMatched=false` but the exact multi-line snippet appears
    elsewhere in the file, `correctedLine`/`correctedLineEnd` carry the
    first occurrence; ambiguity flagged in `reason`.
  - A finding with no `snippet` skips the content check; only the hunk check
    runs. Rarely useful — callers should supply snippets whenever possible.

Exit codes
----------
  0 = success (regardless of per-finding verdicts)
  1 = hard failure (invalid stdin, git failed, etc.)
#>

$global:ScriptBaseName = 'verify-lines'
. "$PSScriptRoot/_shared.ps1"

function Format-LineNormalised { param([string]$s) return ($s -replace '[ \t]+$', '') }

function Format-BlockNormalised {
    param([string]$text)
    return (($text -split "`n") | ForEach-Object { Format-LineNormalised $_ }) -join "`n"
}

# Check whether [line, lineEnd] (1-indexed) in $content matches the normalised
# $snippet. Returns @{ match; actualText }.
function Test-Snippet {
    param([string]$content, [int]$line, [int]$lineEnd, [string]$snippet)
    if (-not $snippet) { return @{ match = $null; actualText = '' } }
    $allLines = $content -split "`n"
    $a = [Math]::Max(1, $line)
    $b = [Math]::Max($a, $lineEnd)
    if ($b -gt $allLines.Length) { return @{ match = $false; actualText = '' } }
    $actual = ($allLines[($a - 1)..($b - 1)] -join "`n")
    return @{ match = ((Format-BlockNormalised $actual) -eq (Format-BlockNormalised $snippet)); actualText = $actual }
}

# If the snippet is not at the expected line, search the whole file for a
# matching block. Returns @{ line; lineEnd; multiple } or $null.
function Find-SnippetElsewhere {
    param([string]$content, [string]$snippet)
    if (-not $snippet) { return $null }
    $normSnippet = Format-BlockNormalised $snippet
    $lines = ($content -split "`n") | ForEach-Object { Format-LineNormalised $_ }
    $snipLines = $normSnippet -split "`n"
    $count = $snipLines.Length
    if ($count -eq 0 -or $lines.Length -lt $count) { return $null }

    $firstHit = $null
    $multiple = $false
    for ($i = 0; $i -le ($lines.Length - $count); $i++) {
        $ok = $true
        for ($j = 0; $j -lt $count; $j++) {
            if ($lines[$i + $j] -ne $snipLines[$j]) { $ok = $false; break }
        }
        if ($ok) {
            if ($null -eq $firstHit) {
                $firstHit = @{ line = $i + 1; lineEnd = $i + $count }
            } else {
                $multiple = $true
                break
            }
        }
    }

    if (-not $firstHit) { return $null }
    return @{ line = $firstHit.line; lineEnd = $firstHit.lineEnd; multiple = $multiple }
}

$m = Read-StdinJson

$findings = @()
if ($m.findings) { $findings = @($m.findings) }
$headRef = if ($m.headRef) { [string]$m.headRef } elseif (Test-IsInteger $m.pr) { "origin/pr/$([int]$m.pr)" } else { $null }
if (-not $headRef) { Exit-WithError 'headRef (or pr to derive it) required' }
$baseRef = if ($m.baseRef) { [string]$m.baseRef } else { 'origin/master' }

# Group findings by file so each file is read once.
$byFile = @{}
foreach ($f in $findings) {
    if (-not $f -or -not ($f.file -is [string]) -or -not $f.file) { continue }
    if (-not $byFile.ContainsKey($f.file)) { $byFile[$f.file] = @() }
    $byFile[$f.file] += $f
}
$filesList = @($byFile.Keys)

# Batch hunk parse — one git diff for all files.
$hunksByFile = @{}
if ($filesList.Count -gt 0) {
    $argsList = @('diff','--unified=0',"$baseRef...$headRef",'--') + $filesList
    $r = Invoke-Git -ArgumentList $argsList
    if (-not $r.ok) { Exit-WithError "git diff failed: $($r.error)" }
    $hunksByFile = ConvertFrom-UnifiedDiffHunks -DiffText $r.stdout
}

# Fan-out content reads.
$root = $PSScriptRoot
$fileData = @{}
if ($filesList.Count -gt 0) {
    $fetchResults = $filesList | ForEach-Object -ThrottleLimit 8 -Parallel {
        . "$using:root/_shared.ps1"
        $path = $_
        $r = Invoke-Git @('show', "$using:headRef`:$path")
        if (-not $r.ok) {
            return [pscustomobject]@{ path = $path; content = $null; error = $r.error }
        }
        return [pscustomobject]@{ path = $path; content = $r.stdout; error = $null }
    }
    foreach ($fr in $fetchResults) { $fileData[$fr.path] = $fr }
}

$out = foreach ($f in $findings) {
    $base = [ordered]@{
        id = $f.id
        file = $f.file
        line = $f.line
        line_end = $f.line_end
        ok = $false
        snippetMatched = $null
        inHunk = $false
        correctedLine = $null
        correctedLineEnd = $null
        reason = $null
    }

    if (-not $f -or -not ($f.file -is [string]) -or -not $f.file) {
        $base.reason = 'missing file'
        [pscustomobject]$base
        continue
    }
    if (-not (Test-IsInteger $f.line) -or [long]$f.line -le 0) {
        $base.reason = 'missing or invalid line'
        [pscustomobject]$base
        continue
    }

    $lineInt = [int]$f.line
    $hunks = @()
    if ($hunksByFile.ContainsKey($f.file)) { $hunks = @($hunksByFile[$f.file]) }
    $lineEnd = if ((Test-IsInteger $f.line_end) -and [long]$f.line_end -ge $lineInt) { [int]$f.line_end } else { $lineInt }
    $base.inHunk = Test-RangeInHunks -Hunks $hunks -Line $lineInt -LineEnd $lineEnd

    $fd = $fileData[$f.file]
    if (-not $fd -or $fd.content -isnot [string]) {
        $errMsg = if ($fd -and $fd.error) { $fd.error } else { 'missing' }
        $base.reason = "could not read file: $errMsg"
        [pscustomobject]$base
        continue
    }

    if ($f.snippet -is [string] -and $f.snippet.Length -gt 0) {
        $chk = Test-Snippet -content $fd.content -line $lineInt -lineEnd $lineEnd -snippet $f.snippet
        $base.snippetMatched = $chk.match
        if ($chk.match -eq $false) {
            $elsewhere = Find-SnippetElsewhere -content $fd.content -snippet $f.snippet
            if ($elsewhere) {
                $base.correctedLine = $elsewhere.line
                $base.correctedLineEnd = $elsewhere.lineEnd
                if ($elsewhere.multiple) {
                    $base.reason = 'snippet found at multiple locations — manual review needed'
                } else {
                    $base.reason = 'snippet found at a different location; consider correctedLine'
                }
            } else {
                $base.reason = 'snippet not found anywhere in file'
            }
        }
    }

    $base.ok = $base.inHunk -and ($base.snippetMatched -ne $false)
    if (-not $base.ok -and -not $base.reason) {
        if (-not $base.inHunk) { $base.reason = 'line is outside any changed hunk' }
    }

    [pscustomobject]$base
}

Write-JsonOutput ([pscustomobject]@{
    headRef = $headRef
    baseRef = $baseRef
    findings = @($out)
})
