#!/usr/bin/env pwsh
<#
wiki-commit.ps1 - commit edited page(s) to the linq2db wiki clone via git plumbing.

Why this isn't a plain `git add` + `git commit`
-----------------------------------------------
NOTE (2026-07-31): the colon-named page this script was written for has been
renamed (`linq2db.wiki` f338e68), the clone is a normal full checkout again, and a
plain `git add` + `git commit` now works. This script is no longer mandatory - it
is kept because a GitHub wiki page title may still contain a colon, which would
reintroduce the problem, and because it stays the safest way to script a page
edit: it always bases the commit on `origin/master` and aborts if the diff touches
anything but the named pages. See `.agents/docs/windows-dev-gotchas.md` ->
*Cloning the `linq2db.wiki` repo*.

The original problem: the repo contained a page whose filename held a colon
(`[Internal]-Azure-Pipelines:-Open-Tasks.md`), which NTFS cannot materialise. The
clone was therefore a sparse checkout, and every page outside the sparse set read
as **deleted** in the working tree. A normal `git commit` staged all of those
deletions alongside your edit - even after a fully successful sparse checkout.

So the commit is built from the **index and object store only**, never from a
working-tree scan:

    read-tree origin/master     index := remote tree (working tree untouched)
    hash-object -w <page>       edited content -> blob
    update-index --cacheinfo    point the index entry at that blob
    write-tree                  index -> tree
    commit-tree -p origin/master  tree -> commit
    update-ref refs/heads/master  move the branch

`core.protectNTFS=false` is required on the tree-touching steps so git will
handle the colon-named entry it can never write to disk.

This script replaces hand-running that six-call sequence and substituting each
printed SHA into the next call - see `.agents/docs/windows-dev-gotchas.md` ->
*Cloning the `linq2db.wiki` repo fails on a colon-named page*.

The commit is always based on `origin/master`, never the local branch: the clone
is long-lived and routinely behind, and a page added upstream after the last
fetch does not exist in the local `master` commit at all.

Usage:

  pwsh -NoProfile -File .agents/scripts/wiki-commit.ps1 -Page L2DB1001.md -MessageFile .build/.agents/wiki-msg.txt

  -Page         page filename(s) relative to the wiki root, repeatable
                (`-Page A.md -Page B.md`). Must already be edited on disk.
  -MessageFile  path to the commit message (multi-line; UTF-8)
  -WikiDir      wiki clone path (default ../linq2db.wiki)
  -DryRun       build the tree and report what would be committed, but don't
                move refs/heads/master

Always fetches `origin` first, then verifies the resulting commit's diff against
`origin/master` touches **only** the named pages - a non-empty extra path (most
likely the colon page showing as deleted) aborts before any ref moves.

Exit codes:
  0  committed (or, with -DryRun, tree built and verified)
  1  hard failure (bad args, page missing, git error, unexpected paths in diff)
#>

param(
    [Parameter(Mandatory = $true)]
    [string[]] $Page,
    [Parameter(Mandatory = $true)]
    [string]   $MessageFile,
    [string]   $WikiDir = '../linq2db.wiki',
    [switch]   $DryRun
)

$global:ScriptBaseName = 'wiki-commit'
. "$PSScriptRoot/_shared.ps1"

# --- Validate inputs -------------------------------------------------------
if (-not (Test-Path -LiteralPath $WikiDir)) {
    Exit-WithError "wiki clone not found: $WikiDir" -NextAction "clone it per windows-dev-gotchas.md (git clone --no-checkout https://github.com/linq2db/linq2db.wiki.git $WikiDir)"
}
if (-not (Test-Path -LiteralPath $MessageFile)) {
    Exit-WithError "message file not found: $MessageFile" -NextAction "write the commit message to $MessageFile (under .build/.agents/) before re-invoking"
}

$pages = @($Page | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim().Replace('\', '/') })
if ($pages.Count -eq 0) { Exit-WithError '-Page requires at least one page name' }

foreach ($p in $pages) {
    if ($p -match '^(/|[A-Za-z]:)' -or $p.Contains('..')) {
        Exit-WithError "-Page must be a path relative to the wiki root, without '..': $p"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $WikiDir $p))) {
        Exit-WithError "page not materialised in the working tree: $p" -NextAction "materialise it from the remote ref first: git -C $WikiDir checkout origin/master -- $p"
    }
}

# `git -C <dir>` for every call keeps the caller's cwd irrelevant.
function Wiki {
    param([string[]]$GitArgs, [switch]$ProtectNtfsOff)
    $argv = @('-C', $WikiDir)
    if ($ProtectNtfsOff) { $argv += @('-c', 'core.protectNTFS=false') }
    $argv += $GitArgs
    $r = Invoke-Git -ArgumentList $argv
    # $r.error already folds stderr+stdout for a non-zero exit, so don't append stderr again.
    if (-not $r.ok) { Exit-WithError "git $($GitArgs -join ' ') failed: $($r.error)" }
    return $r.stdout.Trim()
}

# --- Refresh origin so we base on the true upstream tip --------------------
[void](Wiki @('fetch', 'origin'))
$baseSha = Wiki @('rev-parse', 'origin/master')

# --- Build the commit from the index + object store -------------------------
# read-tree replaces the index wholesale with the remote tree, so the working
# tree's phantom deletions never reach the commit.
[void](Wiki -ProtectNtfsOff -GitArgs @('read-tree', 'origin/master'))

foreach ($p in $pages) {
    $blob = Wiki @('hash-object', '-w', $p)
    if ($blob -notmatch '^[0-9a-f]{40,64}$') { Exit-WithError "unexpected hash-object output for ${p}: $blob" }
    [void](Wiki @('update-index', '--cacheinfo', "100644,$blob,$p"))
}

$tree = Wiki -ProtectNtfsOff -GitArgs @('write-tree')
if ($tree -notmatch '^[0-9a-f]{40,64}$') { Exit-WithError "unexpected write-tree output: $tree" }

# An unchanged tree means the edits were already committed (or never made) -
# committing would produce an empty commit.
if ($tree -eq (Wiki -ProtectNtfsOff -GitArgs @('rev-parse', 'origin/master^{tree}'))) {
    Exit-WithError 'tree is identical to origin/master - nothing to commit' -NextAction 'edit the page(s) on disk first, or drop the commit if it already landed upstream'
}

$commit = Wiki @('commit-tree', $tree, '-p', $baseSha, '-F', (Resolve-Path -LiteralPath $MessageFile).Path)
if ($commit -notmatch '^[0-9a-f]{40,64}$') { Exit-WithError "unexpected commit-tree output: $commit" }

# --- Verify the commit touches only the named pages ------------------------
# This is the load-bearing check: the usual failure is the colon-named page
# appearing as a deletion, which must never reach the wiki.
$nameStatus = Wiki -ProtectNtfsOff -GitArgs @('diff', '--name-only', $baseSha, $commit)
$touched    = @($nameStatus -split "`n" | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim() })
$unexpected = @($touched | Where-Object { $pages -notcontains $_ })

if ($unexpected.Count -gt 0) {
    Exit-WithError "commit would touch unexpected paths: $($unexpected -join ', ')" -NextAction 'do not push; re-check the sparse-checkout state and re-run (the colon-named page showing as deleted is the usual cause)'
}

# --- Move the branch -------------------------------------------------------
$applied = $false
if (-not $DryRun) {
    [void](Wiki @('update-ref', 'refs/heads/master', $commit))
    $applied = $true
}

Write-JsonOutput ([pscustomobject]@{
    wikiDir  = $WikiDir
    base     = $baseSha
    tree     = $tree
    commit   = $commit
    pages    = $pages
    touched  = $touched
    applied  = $applied
    dryRun   = [bool]$DryRun
    nextStep = if ($applied) { "git -C $WikiDir push origin master" } else { 'dry run - re-invoke without -DryRun to move refs/heads/master' }
})
exit 0
