#!/usr/bin/env pwsh
<#
KB wiki fetcher — clone or update the linq2db.wiki repo and report changed
articles since a cursor SHA.

One permission rule:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-fetch-wiki.ps1:*)

Input (stdin, JSON):
  {
    "since":      "<wiki commit sha>",  // null on first run -> full mirror
    "owner":      "linq2db",
    "repo":       "linq2db",
    "wikiClone":  ".build/.claude/kb-wiki",  // optional, default
    "list":       false                       // optional; if true, return file listing only
  }

Output (stdout, JSON):
  {
    "ok": true,
    "head_sha": "<wiki HEAD sha>",
    "base_sha": "<since-sha or null>",
    "changed_files": ["Home.md", "Provider-Oracle.md", ...],   // relative paths inside wiki repo
    "added_files":   [...],
    "deleted_files": [...],
    "all_files":     [...]    // present on full mirror or when list=true
  }

  Errors:
    { "ok": false, "status": "unreachable" | "error", "error": "..." }
#>

$global:ScriptBaseName = 'kb-fetch-wiki'
. "$PSScriptRoot/_shared.ps1"

$m = Read-StdinJson
$since = if ($m.since) { [string]$m.since } else { $null }
$owner = if ($m.owner) { [string]$m.owner } else { 'linq2db' }
$repo  = if ($m.repo)  { [string]$m.repo }  else { 'linq2db' }
$cloneRel = if ($m.wikiClone) { [string]$m.wikiClone } else { '.build/.claude/kb-wiki' }
$listOnly = [bool]$m.list

$repoRoot = Resolve-Path "$PSScriptRoot/../.." | Select-Object -ExpandProperty Path
$cloneAbs = Join-Path $repoRoot $cloneRel
$wikiUrl = "https://github.com/$owner/$repo.wiki.git"

if (-not (Test-Path $cloneAbs)) {
    [void](New-Item -ItemType Directory -Force -Path (Split-Path -Parent $cloneAbs))
    $r = Invoke-Git -ArgumentList @('clone', '--depth', '1', $wikiUrl, $cloneAbs)
    if (-not $r.ok) {
        $err = $r.error
        if ($err -match '(?i)not found|404|repository.*does not exist') {
            Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $false; status = 'unreachable'; error = $err })
            exit 0
        }
        Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $false; status = 'error'; error = $err })
        exit 0
    }
    # Unshallow so since-diff works on subsequent runs.
    [void](Invoke-Git -ArgumentList @('fetch', '--unshallow') -WorkingDirectory $cloneAbs)
} else {
    $r = Invoke-Git -ArgumentList @('fetch', 'origin') -WorkingDirectory $cloneAbs
    if (-not $r.ok) {
        Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $false; status = 'error'; error = $r.error })
        exit 0
    }
    [void](Invoke-Git -ArgumentList @('reset', '--hard', 'origin/HEAD') -WorkingDirectory $cloneAbs)
}

$rev = Invoke-Git -ArgumentList @('rev-parse', 'HEAD') -WorkingDirectory $cloneAbs
if (-not $rev.ok) {
    Write-JsonOutput -InputObject ([pscustomobject]@{ ok = $false; status = 'error'; error = "rev-parse: $($rev.error)" })
    exit 0
}
$headSha = $rev.stdout.Trim()

$changed = @(); $added = @(); $deleted = @()
$allFiles = @()

if ($listOnly -or -not $since) {
    # Full file listing
    $r = Invoke-Git -ArgumentList @('ls-files') -WorkingDirectory $cloneAbs
    if ($r.ok) {
        $allFiles = @(($r.stdout -split "`n") | Where-Object { $_.Trim() -and $_.EndsWith('.md') })
        $changed = $allFiles
        $added = $allFiles
    }
} else {
    $r = Invoke-Git -ArgumentList @('diff', '--name-status', "$since..$headSha") -WorkingDirectory $cloneAbs
    if (-not $r.ok) {
        # Cursor sha unreachable in shallow clone history — fall back to full re-mirror.
        $rls = Invoke-Git -ArgumentList @('ls-files') -WorkingDirectory $cloneAbs
        if ($rls.ok) {
            $allFiles = @(($rls.stdout -split "`n") | Where-Object { $_.Trim() -and $_.EndsWith('.md') })
            $changed = $allFiles
            $added = $allFiles
        }
    } else {
        foreach ($ln in ($r.stdout -split "`n")) {
            if (-not $ln.Trim()) { continue }
            $parts = $ln -split "`t", 2
            if ($parts.Count -lt 2) { continue }
            $status = $parts[0]
            $path = $parts[1]
            if (-not $path.EndsWith('.md')) { continue }
            switch -Regex ($status) {
                '^A'  { $added += $path; $changed += $path }
                '^M'  { $changed += $path }
                '^R'  { $changed += $path }
                '^D'  { $deleted += $path }
            }
        }
    }
}

$result = [pscustomobject]@{
    ok            = $true
    head_sha      = $headSha
    base_sha      = $since
    clone_path    = $cloneAbs
    changed_files = @($changed | Sort-Object -Unique)
    added_files   = @($added   | Sort-Object -Unique)
    deleted_files = @($deleted | Sort-Object -Unique)
    all_files     = @($allFiles | Sort-Object -Unique)
}
Write-JsonOutput -InputObject $result
