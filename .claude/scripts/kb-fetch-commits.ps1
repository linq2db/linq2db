#!/usr/bin/env pwsh
<#
KB commit fetcher — extracts structured commit history from linq2db's git
repo, with cursor support.

One permission rule:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-fetch-commits.ps1 *)

Input (stdin, JSON):
  {
    "since":  "<commit-sha>",   // exclusive lower bound; if null, fetch from
                                // the very first commit
    "until":  "HEAD",           // optional, default HEAD
    "year":   2024,             // optional — restrict to a single year
    "limit":  5000,             // optional cap; default 50000 (effectively unbounded)
    "includeBody": true         // optional, default true — include commit body
  }

Output (stdout, JSON):
  {
    "ok": true,
    "head_sha":  "<HEAD sha>",
    "base_sha":  "<since sha or null>",
    "commits": [
      {
        "sha": "abc1234",
        "author":     "Author Name",
        "author_email":"a@b.c",
        "date":       "2024-03-15T10:11:12+00:00",
        "subject":    "Fix Oracle identity mapping",
        "body":       "...full body...",
        "files":      ["path/a.cs", "path/b.cs"],
        "files_changed": 2,
        "insertions": 12,
        "deletions":  3,
        "year":       2024
      },
      ...
    ],
    "fetched": 412,
    "first_year": 2014,
    "last_year":  2026
  }
#>

$global:ScriptBaseName = 'kb-fetch-commits'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson
$since = if ($m.since) { [string]$m.since } else { $null }
$until = if ($m.until) { [string]$m.until } else { 'HEAD' }
$year  = if ((Test-IsInteger $m.year) -and [long]$m.year -gt 0) { [int]$m.year } else { 0 }
$limit = if ((Test-IsInteger $m.limit) -and [long]$m.limit -gt 0) { [int]$m.limit } else { 50000 }
$includeBody = ($null -eq $m.includeBody) -or ([bool]$m.includeBody)

# Resolve HEAD sha
$rev = Invoke-Git -ArgumentList @('rev-parse', $until)
if (-not $rev.ok) { Exit-WithError "git rev-parse $until failed: $($rev.error)" }
$headSha = $rev.stdout.Trim()

$range = if ($since) { "$since..$until" } else { $until }

# Use a uniquely-shaped delimiter so we can parse multi-line bodies cleanly.
$marker = '<<<KBCOMMIT|>>>'
$fieldSep = '|kbcf|'
# Format: marker sha|author|email|iso-date|subject|body
# %x00 won't survive line splits cleanly — use sentinel strings instead.
$fmt = "$marker%H${fieldSep}%an${fieldSep}%ae${fieldSep}%aI${fieldSep}%s${fieldSep}%B"

$args = @('log', "--pretty=format:$fmt", '--name-only', '--numstat')
if ($year -gt 0) {
    $args += @("--since=$year-01-01", "--until=$($year + 1)-01-01")
}
$args += @("-n", "$limit", $range)

$r = Invoke-Git -ArgumentList $args
if (-not $r.ok) { Exit-WithError "git log failed: $($r.error)" }

# `--name-only --numstat` interleaves: numstat lines first (per file), then
# blank, then name-only lines. Actually git's behavior with both flags is:
# numstat block followed by names block, separated by a blank line — but
# realistically, numstat alone gives us files + ins/del. Drop --name-only
# to keep the parse simpler.
$args2 = @('log', "--pretty=format:$fmt", '--numstat')
if ($year -gt 0) { $args2 += @("--since=$year-01-01", "--until=$($year + 1)-01-01") }
$args2 += @("-n", "$limit", $range)
$r2 = Invoke-Git -ArgumentList $args2
if (-not $r2.ok) { Exit-WithError "git log (numstat) failed: $($r2.error)" }

$out = $r2.stdout
$commits = @()
$entries = $out -split [regex]::Escape($marker)
foreach ($entry in $entries) {
    if (-not $entry -or -not $entry.Trim()) { continue }
    # Split header and numstat tail. The header is one logical line up to the
    # last $fieldSep block, but %B contains arbitrary newlines. Split on
    # $fieldSep first.
    $parts = $entry -split [regex]::Escape($fieldSep), 6
    if ($parts.Count -lt 6) { continue }
    $sha = $parts[0]
    $author = $parts[1]
    $email = $parts[2]
    $date = $parts[3]
    $subject = $parts[4]
    # parts[5] is body + numstat lines + trailing blank; numstat lines look like
    #   <ins>\t<del>\t<path>
    # We split on lines: body ends at the first numstat-shaped line.
    $rest = $parts[5] -split "`n"
    $bodyLines = @()
    $statLines = @()
    $inStat = $false
    foreach ($ln in $rest) {
        if (-not $inStat -and $ln -match '^\s*\d+\s+\d+\s+\S' ) {
            $inStat = $true
        } elseif (-not $inStat -and $ln -match '^\s*-\s+-\s+\S') {
            # binary file line
            $inStat = $true
        }
        if ($inStat) {
            if ($ln.Trim()) { $statLines += $ln }
        } else {
            $bodyLines += $ln
        }
    }
    $body = ($bodyLines -join "`n").TrimEnd()
    $files = @()
    $ins = 0; $del = 0
    foreach ($sl in $statLines) {
        $cols = $sl -split "`t"
        if ($cols.Count -ge 3) {
            $iv = if ($cols[0] -eq '-') { 0 } else { [int]$cols[0] }
            $dv = if ($cols[1] -eq '-') { 0 } else { [int]$cols[1] }
            $ins += $iv
            $del += $dv
            $files += $cols[2]
        }
    }
    $yearOf = 0
    if ($date -match '^(\d{4})') { $yearOf = [int]$Matches[1] }
    $entryObj = [pscustomobject]@{
        sha           = $sha
        author        = $author
        author_email  = $email
        date          = $date
        subject       = $subject
        body          = if ($includeBody) { $body } else { '' }
        files         = $files
        files_changed = $files.Count
        insertions    = $ins
        deletions     = $del
        year          = $yearOf
    }
    $commits += $entryObj
}

# Year span
$firstYear = $null; $lastYear = $null
if ($commits.Count -gt 0) {
    $years = @($commits | Where-Object { $_.year -gt 0 } | ForEach-Object { $_.year })
    if ($years.Count -gt 0) {
        $firstYear = ($years | Measure-Object -Minimum).Minimum
        $lastYear  = ($years | Measure-Object -Maximum).Maximum
    }
}

$result = [pscustomobject]@{
    ok         = $true
    head_sha   = $headSha
    base_sha   = $since
    commits    = $commits
    fetched    = $commits.Count
    first_year = $firstYear
    last_year  = $lastYear
}
Write-JsonOutput -InputObject $result
