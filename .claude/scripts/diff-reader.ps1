#!/usr/bin/env pwsh
<#
Batch file content + diff + hunk reader for the `code-reviewer` subagent.

Why this wrapper exists
-----------------------
The reviewer needs three things per changed file:
  - the current PR-head content (for reading and snippet verification)
  - a unified=0 diff vs master (to know exactly which lines changed)
  - the parsed right-side hunk ranges (to reject findings that fall outside hunks)

Doing this with raw Bash fires one `git show` per file + one `git diff` per
file = 2N permission prompts. This script takes the full file list up front
and answers for all of them in one shot.

    Bash(pwsh -NoProfile -File .claude/scripts/diff-reader.ps1:*)

Input (stdin, JSON)
-------------------
  {
    "pr":           5414,                     // optional — sets default head/base
    "headRef":      "origin/pr/5414",         // optional, default "origin/pr/<pr>"
    "baseRef":      "origin/master",          // optional
    "files":        ["Source/...", "Tests/…"],// required — explicit, caller knows the list
    "include": {
      "content":   true,                      // optional, default true — inline head content
      "diff":      true,                      // optional, default true
      "hunks":     true,                      // optional, default true
      "base":      false,                     // optional, default false — also dump the base-ref
                                              // version of each file next to the head dump
                                              // (requires writeDir). Useful for structural
                                              // before/after comparison beyond the diff hunks.
      "styleScan": false                      // optional, default false — run cheap style checks
                                              // over the full head body and emit `styleFindings[]`
                                              // per entry. Currently detects:
                                              //   trailing_whitespace   — line ending in spaces/tabs
                                              //   three_plus_blank_lines — run of 3+ blank lines
                                              //   mixed_indent          — leading whitespace with
                                              //                           spaces-then-tabs (tabs-then-
                                              //                           spaces is legitimate column
                                              //                           alignment and is skipped).
                                              // Scope is the whole file, not just hunks — preexisting
                                              // nits are worth surfacing alongside PR-introduced ones.
    },
    "maxContentBytes": 200000,                // optional — truncate inline content; omit for no limit
    "writeDir":   ".build/.claude/pr5414"     // optional — when set, full per-file content is
                                              // written to <writeDir>/<source-path> (directory
                                              // structure preserved) and `contentPath` is emitted
                                              // per-entry so the caller can Read/Grep on disk
                                              // instead of keeping the whole blob in context.
                                              // When include.base is also true, base versions land
                                              // at <writeDir>/_base/<source-path> and `basePath`
                                              // is emitted per-entry.
  }

Output (stdout, single JSON object): per-file { path, content, contentTruncated,
contentBytes, contentPath, basePath, diff, diffPath, hunks, lineCount,
styleFindings, exists }. The `exists` flag is false for deleted files — content
is null, but `diff` and `hunks` are still populated. `contentPath` is only
present when `writeDir` was set and the file exists at `headRef`. `basePath` is
only present when `writeDir` + `include.base` are set and the file exists at
`baseRef` (absent for files newly added in the PR). `diffPath` is only present
when `writeDir` is set and the file has a non-empty per-file diff; the body
lives at `<writeDir>/_diff/<source-path>.diff` so callers can `Read` / `Grep`
it without paging through the full JSON output. `styleFindings` is only present
when `include.styleScan` is true; each entry is `{kind, line, lineEnd?, snippet?}`.

Exit codes
----------
  0 = success
  1 = hard failure
#>

$global:ScriptBaseName = 'diff-reader'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson

$files = @()
if ($m.files) { foreach ($f in $m.files) { if ($f -is [string] -and $f.Length -gt 0) { $files += $f } } }
if ($files.Count -eq 0) { Exit-WithError 'files (non-empty string array) required' }

$headRef = if ($m.headRef) { [string]$m.headRef } elseif (Test-IsInteger $m.pr) { "origin/pr/$([int]$m.pr)" } else { $null }
if (-not $headRef) { Exit-WithError 'headRef (or pr to derive it) required' }
$baseRef = if ($m.baseRef) { [string]$m.baseRef } else { 'origin/master' }

$inc = $m.include
$wantContent = if ($null -ne $inc -and $null -ne $inc.content) { [bool]$inc.content } else { $true }
$wantDiff    = if ($null -ne $inc -and $null -ne $inc.diff)    { [bool]$inc.diff }    else { $true }
$wantHunks   = if ($null -ne $inc -and $null -ne $inc.hunks)   { [bool]$inc.hunks }   else { $true }
$wantBase    = if ($null -ne $inc -and $null -ne $inc.base)    { [bool]$inc.base }    else { $false }
$wantStyleScan = if ($null -ne $inc -and $null -ne $inc.styleScan) { [bool]$inc.styleScan } else { $false }

$maxContentBytes = if ((Test-IsInteger $m.maxContentBytes) -and [long]$m.maxContentBytes -gt 0) { [int]$m.maxContentBytes } else { 0 }

$writeDir = if ($m.writeDir) { [string]$m.writeDir } else { $null }
if ($writeDir) {
    [void][System.IO.Directory]::CreateDirectory($writeDir)
}

$diffBodies = @{}
$hunksByFile = @{}
# Run the git diff whenever any of {inline diff, hunks, on-disk diff} is wanted.
# diffPath is tied to writeDir — having writeDir alone implies we should write
# per-file diffs to disk so callers don't have to re-parse the JSON blob.
$wantDiffPath = [bool]$writeDir
if ($wantDiff -or $wantHunks -or $wantDiffPath) {
    $argsList = @('diff','--unified=0',"$baseRef...$headRef",'--') + $files
    $r = Invoke-Git -ArgumentList $argsList
    if (-not $r.ok) { Exit-WithError "git diff failed: $($r.error)" }
    if ($wantDiff -or $wantDiffPath) { $diffBodies  = Split-DiffByFile -DiffText $r.stdout }
    if ($wantHunks)                  { $hunksByFile = ConvertFrom-UnifiedDiffHunks -DiffText $r.stdout }
}

$diffPaths = @{}
if ($wantDiffPath -and $diffBodies.Count -gt 0) {
    $diffRoot = Join-Path $writeDir '_diff'
    foreach ($path in $files) {
        if (-not $diffBodies.ContainsKey($path)) { continue }
        $body = $diffBodies[$path]
        if (-not $body) { continue }
        $target = Join-Path $diffRoot ($path + '.diff')
        $targetDir = Split-Path -Parent $target
        if ($targetDir) { [void][System.IO.Directory]::CreateDirectory($targetDir) }
        [System.IO.File]::WriteAllText($target, $body, [System.Text.UTF8Encoding]::new($false))
        $diffPaths[$path] = ($target -replace '\\','/')
    }
}

# Batch existence check — locale-independent, one call regardless of file count.
# Paths missing from the head ref are absent from ls-tree output.
$existingFiles = [System.Collections.Generic.HashSet[string]]::new()
$lsArgs = @('ls-tree','--name-only',$headRef,'--') + $files
$lsRes = Invoke-Git -ArgumentList $lsArgs
if ($lsRes.ok) {
    foreach ($p in ($lsRes.stdout -split "`n")) {
        $trimmed = $p.Trim()
        if ($trimmed) { [void]$existingFiles.Add($trimmed) }
    }
}

# Same existence check against baseRef — only needed when the caller asked for
# base dumps. Files newly added by the PR will be absent here; that's expected.
$existingBaseFiles = [System.Collections.Generic.HashSet[string]]::new()
$wantBaseDump = $wantBase -and $writeDir
if ($wantBaseDump) {
    $lsBaseArgs = @('ls-tree','--name-only',$baseRef,'--') + $files
    $lsBaseRes = Invoke-Git -ArgumentList $lsBaseArgs
    if ($lsBaseRes.ok) {
        foreach ($p in ($lsBaseRes.stdout -split "`n")) {
            $trimmed = $p.Trim()
            if ($trimmed) { [void]$existingBaseFiles.Add($trimmed) }
        }
    }
}

$root = $PSScriptRoot

# Fan-out per-file content reads (git show doesn't accept multi-path). Files
# flagged absent by ls-tree skip `git show` entirely.
$contentResults = @{}
# Run the `git show` fan-out when the caller wants inline content OR wants
# bodies dumped to disk (head via writeDir, base via include.base+writeDir)
# OR wants a style scan (which needs the raw body to inspect).
if ($wantContent -or $writeDir -or $wantStyleScan) {
    $fileResults = $files | ForEach-Object -ThrottleLimit 8 -Parallel {
        . "$using:root/_shared.ps1"
        $path = $_
        $hr = $using:headRef
        $br = $using:baseRef
        $maxBytes = $using:maxContentBytes
        $existing = $using:existingFiles
        $existingBase = $using:existingBaseFiles
        $wdir = $using:writeDir
        $emitInline = $using:wantContent
        $dumpBase = $using:wantBaseDump
        $doStyleScan = $using:wantStyleScan
        $cPath = $null
        $bPath = $null
        $content = $null
        $truncated = $false
        $rawBytes = 0
        $rawLines = 0
        $styleFindings = @()
        $headErr = $null
        $headExists = $existing.Contains($path)
        if ($headExists) {
            $r = Invoke-Git @('show', "${hr}:${path}")
            if (-not $r.ok) {
                # File existed in the tree but show failed (binary? unreadable?).
                $headErr = $r.error
            } else {
                $raw = $r.stdout
                $rawBytes = [System.Text.Encoding]::UTF8.GetByteCount($raw)
                $rawLines = if ($raw.Length -eq 0) { 0 } else { ($raw -split "`n").Length }
                if ($emitInline) {
                    $content = $raw
                    if ($maxBytes -gt 0 -and $rawBytes -gt $maxBytes) {
                        $content = Get-Utf8SafeTruncate -Text $raw -MaxBytes $maxBytes
                        $truncated = $true
                    }
                }
                if ($wdir) {
                    # Preserve source directory structure under writeDir so paths echo
                    # the original repo layout — the caller can Read/Grep the full body
                    # without the truncation / JSON-embed friction of the inline field.
                    $target = Join-Path $wdir $path
                    $targetDir = Split-Path -Parent $target
                    if ($targetDir) { [void][System.IO.Directory]::CreateDirectory($targetDir) }
                    [System.IO.File]::WriteAllText($target, $raw, [System.Text.UTF8Encoding]::new($false))
                    $cPath = ($target -replace '\\','/')
                }
                if ($doStyleScan) {
                    $styleFindings = @(Find-StyleIssues -Body $raw)
                }
            }
        }
        if ($dumpBase -and $existingBase.Contains($path)) {
            # Same tree layout, rooted at <writeDir>/_base/… so head and base
            # dumps can coexist and be diffed directly with a local tool.
            $rb = Invoke-Git @('show', "${br}:${path}")
            if ($rb.ok) {
                $baseDirRoot = Join-Path $wdir '_base'
                $baseTarget = Join-Path $baseDirRoot $path
                $baseTargetDir = Split-Path -Parent $baseTarget
                if ($baseTargetDir) { [void][System.IO.Directory]::CreateDirectory($baseTargetDir) }
                [System.IO.File]::WriteAllText($baseTarget, $rb.stdout, [System.Text.UTF8Encoding]::new($false))
                $bPath = ($baseTarget -replace '\\','/')
            }
        }
        $entry = [pscustomobject]@{
            path = $path
            exists = $headExists
            content = $content
            contentTruncated = $truncated
            contentPath = $cPath
            basePath = $bPath
            rawBytes = $rawBytes
            rawLines = $rawLines
            styleFindings = $styleFindings
        }
        if ($headErr) { $entry | Add-Member -NotePropertyName error -NotePropertyValue $headErr }
        return $entry
    }
    foreach ($r in $fileResults) { $contentResults[$r.path] = $r }
} else {
    foreach ($f in $files) {
        $contentResults[$f] = [pscustomobject]@{
            path = $f
            exists = $existingFiles.Contains($f)
            content = $null
            contentTruncated = $false
            contentPath = $null
            basePath = $null
            styleFindings = @()
        }
    }
}

$out = foreach ($path in $files) {
    $c = $contentResults[$path]
    if (-not $c) { $c = [pscustomobject]@{ path = $path; exists = $true; content = $null; contentTruncated = $false; contentPath = $null; basePath = $null; styleFindings = @() } }
    $content = if ($wantContent) { $c.content } else { $null }
    $contentTruncated = if ($wantContent) { [bool]$c.contentTruncated } else { $false }
    # Prefer raw-body counts carried back from the parallel block (populated
    # whenever content was fetched, even when inline emission was suppressed).
    # Fall back to the inline string only when neither a raw count nor a path
    # dump was produced.
    $contentBytes = if ($null -ne $c.rawBytes) { [int]$c.rawBytes }
                    elseif ($content -is [string]) { [System.Text.Encoding]::UTF8.GetByteCount($content) }
                    else { 0 }
    $lineCount = if ($null -ne $c.rawLines) { [int]$c.rawLines }
                 elseif ($content -is [string]) {
                     if ($content.Length -eq 0) { 0 } else { ($content -split "`n").Length }
                 } else { 0 }

    $entry = [ordered]@{
        path = $path
        exists = $c.exists
        content = $content
        contentTruncated = $contentTruncated
        contentBytes = $contentBytes
        contentPath = $c.contentPath
        basePath = $c.basePath
        lineCount = $lineCount
    }
    if ($wantDiff) {
        $entry.diff = if ($diffBodies.ContainsKey($path)) { $diffBodies[$path] } else { '' }
    }
    if ($wantDiffPath) {
        $entry.diffPath = if ($diffPaths.ContainsKey($path)) { $diffPaths[$path] } else { $null }
    }
    if ($wantHunks) {
        # PS unwraps empty arrays in if/else expression context, so assign the
        # default first and overwrite on the non-empty branch.
        $hunksVal = @()
        if ($hunksByFile.ContainsKey($path)) { $hunksVal = @($hunksByFile[$path]) }
        $entry.hunks = $hunksVal
    }
    if ($wantStyleScan) {
        $sfVal = @()
        if ($c.styleFindings) { $sfVal = @($c.styleFindings) }
        $entry.styleFindings = $sfVal
    }
    if ($c.error) { $entry.error = $c.error }
    [pscustomobject]$entry
}

Write-JsonOutput ([pscustomobject]@{
    headRef = $headRef
    baseRef = $baseRef
    files = @($out)
})
