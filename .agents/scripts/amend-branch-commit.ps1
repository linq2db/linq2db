<#
amend-branch-commit.ps1 — rewrite the message of the tip commit on a
NON-checked-out branch without touching the working tree.

Codifies .agents/docs/pr-and-push.md -> "Amending a commit on a non-checked-out
branch with a dirty current tree". Reuses the branch tip's tree (%T), so this is
a message/metadata amend only — content is unchanged. Avoids the
stash/switch/--amend/pop dance (whose pop can conflict on overlapping files) by
building a replacement commit object and atomically retargeting the ref with the
old-SHA safety check, all while staying on the current branch. The env-var author
plumbing the Bash no-chaining rule forbids (`VAR=… git commit-tree`) lives here.

GLUE ONLY — the message text, branch identity, and the decision that this
(non-stash) path is right stay with the agent/skill.

  pwsh -NoProfile -File .agents/scripts/amend-branch-commit.ps1 -Branch issue/1234-fix -Message "fix: corrected subject"
  pwsh -NoProfile -File .agents/scripts/amend-branch-commit.ps1 -Branch issue/1234-fix -MessageFile .build/.agents/msg.txt -Sign
#>
param(
    [Parameter(Mandatory)][string]$Branch,
    [string]$Message,
    [string]$MessageFile,
    [switch]$Sign
)

. "$PSScriptRoot/_shared.ps1"
$global:ScriptBaseName = 'amend-branch-commit'

if (-not $Message -and -not $MessageFile) { Exit-WithError 'provide -Message or -MessageFile' }
if ($MessageFile) {
    if (-not (Test-Path -LiteralPath $MessageFile)) { Exit-WithError "message file not found: $MessageFile" }
    $Message = Get-Content -Raw -LiteralPath $MessageFile
}
if (-not $Message -or -not $Message.Trim()) { Exit-WithError 'commit message is empty' }

$ref = "refs/heads/$Branch"

$old = Invoke-Git @('rev-parse', '--verify', $ref)
if (-not $old.ok) { Exit-WithError "cannot resolve ${ref}: $($old.error)" }
$oldSha = $old.stdout.Trim()

$meta = Invoke-Git @('show', '-s', '--format=%T%n%P%n%an%n%ae%n%aI', $ref)
if (-not $meta.ok) { Exit-WithError "cannot read commit metadata for ${ref}: $($meta.error)" }
$lines = $meta.stdout.Trim() -split "`n"
if ($lines.Count -lt 5) { Exit-WithError "unexpected metadata for ${ref}: $($meta.stdout)" }
$tree        = $lines[0].Trim()
$parentField = $lines[1].Trim()
$authorName  = $lines[2]
$authorEmail = $lines[3]
$authorDate  = $lines[4].Trim()
$parents = @()
if ($parentField) { $parents = @($parentField -split '\s+' | Where-Object { $_ }) }

$ctArgs = @('commit-tree', $tree)
foreach ($p in $parents) { $ctArgs += @('-p', $p) }
if ($Sign) { $ctArgs += '-S' }
$ctArgs += @('-m', $Message)

# Preserve the original author identity; committer falls to the current
# identity/time (standard amend semantics). Restore env afterward.
$prevName  = $env:GIT_AUTHOR_NAME
$prevEmail = $env:GIT_AUTHOR_EMAIL
$prevDate  = $env:GIT_AUTHOR_DATE
$env:GIT_AUTHOR_NAME  = $authorName
$env:GIT_AUTHOR_EMAIL = $authorEmail
$env:GIT_AUTHOR_DATE  = $authorDate
try {
    $ct = Invoke-Git $ctArgs
} finally {
    $env:GIT_AUTHOR_NAME  = $prevName
    $env:GIT_AUTHOR_EMAIL = $prevEmail
    $env:GIT_AUTHOR_DATE  = $prevDate
}
if (-not $ct.ok) { Exit-WithError "git commit-tree failed: $($ct.error)" }
$newSha = $ct.stdout.Trim()

$upd = Invoke-Git @('update-ref', $ref, $newSha, $oldSha)
if (-not $upd.ok) { Exit-WithError "git update-ref failed (did $Branch move?): $($upd.error)" }

Write-JsonOutput ([pscustomobject]@{
    ok      = $true
    branch  = $Branch
    ref     = $ref
    oldSha  = $oldSha
    newSha  = $newSha
    tree    = $tree
    parents = $parents
    signed  = [bool]$Sign
})
