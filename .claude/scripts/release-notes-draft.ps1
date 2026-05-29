#!/usr/bin/env pwsh
<#
release-notes-draft.ps1 — deterministic GitHub/wiki plumbing for the
release-notes automation (`/release-notes` skill).

This script does NOT compose prose. The agent writes the user-facing change
text (per the provider-claim-verification discipline) and hands it to this
script as manifest fields; the script wraps it in the comment scaffolding,
posts/updates the PR draft comment idempotently, harvests drafts at release
time, and regenerates the wiki release-notes section.

Actions
-------

  find        Locate the draft comment on a PR and parse its state.
              In:  -Pr <n> [-Repo <owner/repo>]
              Out: { ok, present, commentId?, url?, state: { lastSha, omit,
                     includeBrief, markerVersion } }

  upsert      Create or update (PATCH) the draft comment.
              In:  -ManifestFile { pr, lastSha, omit, includeBrief,
                                   fullText, briefText, [repo] }
              Out: { ok, commentId, url, created|patched, verify: { ok,
                     sentLength, storedLength } }

  harvest     Collect every milestone PR's draft (flags + full + brief text).
              In:  -Milestone <ver> [-PlanFile <path>] [-Repo] [-Version]
              Out (stdout): counts + harvestFile path
              File: .build/.claude/release-<ver>-notes-harvest.json
                    { items[]: { pr, hasDraft, omit, includeBrief, lastSha,
                                 fullText, briefText, linkedIssues[] } }

  apply-wiki  Regenerate the version's release-notes section in the local wiki
              clone (idempotent — full-section rebuild) and emit a git diff.
              Does NOT commit or push (the skill confirms the diff, then the
              user-gated git commit + push runs separately).
              In:  -ManifestFile { version, wikiClone, [page], prBullets[],
                                   [deepDives[]] }
                     prBullets[]: { pr, [issue], component, changeType, text,
                                    [url] }
                     deepDives[]: { pr, heading, body }
              Out: { ok, page, sectionAction, diffPath, bulletCount,
                     deepDiveCount }

  sweep-plan  Find milestone PRs missing a draft comment and/or missing from
              the wiki notes (orphan sweep — catches user-merged PRs).
              In:  -Milestone <ver> [-PlanFile] [-WikiClone] [-Repo] [-Version]
              Out (stdout): counts + sweepFile path
              File: .build/.claude/release-<ver>-notes-sweep.json
                    { missingDraft[]: { pr, title, url },
                      missingWiki[]:  { pr, title, url, hasDraft, omit } }

Conventions: `.claude/docs/script-authoring.md`.
Marker scheme mirrors `release-state.ps1` (start/end + machine block).
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('find','upsert','harvest','apply-wiki','sweep-plan')]
    [string]$Action,

    [int]$Pr,
    [string]$Milestone,
    [string]$Repo = 'linq2db/linq2db',
    [string]$Version,
    [string]$ManifestFile,
    [string]$PlanFile,
    [string]$WikiClone,
    [switch]$NoSync
)

$global:ScriptBaseName = 'release-notes-draft'
. "$PSScriptRoot/_shared.ps1"

if (-not $Version) { $Version = $Milestone }

# -- constants ---------------------------------------------------------------

$DraftMarkerPrefix = '<!-- release-notes:draft:'      # version-agnostic find anchor
$DraftStart   = '<!-- release-notes:draft:v1:start -->'
$DraftEnd     = '<!-- release-notes:draft:v1:end -->'
$FullStart    = '<!-- rn:full:start -->'
$FullEnd      = '<!-- rn:full:end -->'
$BriefStart   = '<!-- rn:brief:start -->'
$BriefEnd     = '<!-- rn:brief:end -->'
$StateStart   = '<!-- release-notes:state'
$OmitLabel    = '**Omit from release notes**'
$BriefLabel   = '**Include in the GitHub release highlights**'
$MarkerVersion = 1

# -- paths -------------------------------------------------------------------

function Get-WorkDir {
    $dir = Join-Path (Get-Location) '.build/.claude'
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    return $dir
}

# -- comment body parse/build ------------------------------------------------

function Get-BetweenMarkers {
    param([string]$Body, [string]$Open, [string]$Close)
    if (-not $Body) { return $null }
    $a = $Body.IndexOf($Open, [System.StringComparison]::Ordinal)
    if ($a -lt 0) { return $null }
    $a += $Open.Length
    $b = $Body.IndexOf($Close, $a, [System.StringComparison]::Ordinal)
    if ($b -lt 0) { return $null }
    return $Body.Substring($a, $b - $a).Trim("`r","`n")
}

# Parse the draft comment body into a state object. Rendered checkboxes are
# authoritative for the flags (the maintainer may have toggled them in the UI);
# the embedded JSON is authoritative for lastSha.
function Read-DraftState {
    param([string]$Body)
    $state = [ordered]@{ lastSha = $null; omit = $false; includeBrief = $false; markerVersion = $MarkerVersion }
    if (-not $Body) { return $state }

    # checkbox flags
    $omitRx  = [regex]::Escape($OmitLabel)
    $briefRx = [regex]::Escape($BriefLabel)
    if ($Body -match "(?m)^\s*-\s*\[(?<m>[ xX])\]\s*$omitRx")  { $state.omit         = ($Matches['m'] -match '[xX]') }
    if ($Body -match "(?m)^\s*-\s*\[(?<m>[ xX])\]\s*$briefRx") { $state.includeBrief = ($Matches['m'] -match '[xX]') }

    # machine block (JSON inside the HTML comment); lastSha + markerVersion
    $stateBlock = Get-BetweenMarkers -Body $Body -Open $StateStart -Close '-->'
    if ($stateBlock) {
        try {
            $j = $stateBlock | ConvertFrom-Json -Depth 20
            if ($j.lastSha) { $state.lastSha = [string]$j.lastSha }
            if ($j.markerVersion) { $state.markerVersion = [int]$j.markerVersion }
        } catch { }
    }
    return $state
}

function Build-CommentBody {
    param(
        [int]$PrNumber, [string]$LastSha, [bool]$Omit, [bool]$IncludeBrief,
        [string]$FullText, [string]$BriefText
    )
    $omitMark  = if ($Omit)         { 'x' } else { ' ' }
    $briefMark = if ($IncludeBrief) { 'x' } else { ' ' }
    $shortSha  = if ($LastSha -and $LastSha.Length -ge 7) { $LastSha.Substring(0,7) } else { [string]$LastSha }
    $full  = if ($FullText)  { $FullText.Trim("`r","`n") }  else { '_(none — nothing user-facing in this PR)_' }
    $brief = if ($BriefText) { $BriefText.Trim("`r","`n") } else { '_(none)_' }
    $stateJson = ([pscustomobject]@{
        lastSha       = $LastSha
        omit          = $Omit
        includeBrief  = $IncludeBrief
        markerVersion = $MarkerVersion
    } | ConvertTo-Json -Compress)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add($DraftStart)
    [void]$lines.Add('## 📝 Release-notes draft')
    [void]$lines.Add('')
    [void]$lines.Add('<sub>🤖 Auto-generated user-facing summary for this PR. Toggle the boxes to control how it ships; the text is regenerated when new commits land (the maintainer confirms every change).</sub>')
    [void]$lines.Add('')
    [void]$lines.Add("- [$omitMark] $OmitLabel (exclude from both the wiki notes and the GitHub release highlights)")
    [void]$lines.Add("- [$briefMark] $BriefLabel (the brief release-page notes)")
    [void]$lines.Add('')
    [void]$lines.Add('### Full release notes (wiki)')
    [void]$lines.Add($FullStart)
    [void]$lines.Add($full)
    [void]$lines.Add($FullEnd)
    [void]$lines.Add('')
    [void]$lines.Add('### GitHub release highlight (brief)')
    [void]$lines.Add($BriefStart)
    [void]$lines.Add($brief)
    [void]$lines.Add($BriefEnd)
    [void]$lines.Add('')
    [void]$lines.Add('---')
    [void]$lines.Add("<sub>Generated from commit ``$shortSha``.</sub>")
    [void]$lines.Add("$StateStart")
    [void]$lines.Add($stateJson)
    [void]$lines.Add('-->')
    [void]$lines.Add($DraftEnd)
    return ($lines -join "`n")
}

# -- comment fetch/write -----------------------------------------------------

function Find-DraftComment {
    param([int]$PrNumber, [string]$RepoFull)
    $r = Invoke-GhJson -ArgumentList @('api', "repos/$RepoFull/issues/$PrNumber/comments", '--paginate')
    if (-not $r.ok) { Exit-WithError "comment list for PR #$PrNumber failed: $($r.error)" }
    foreach ($c in @($r.data)) {
        if ($c.body -and $c.body.Contains($DraftMarkerPrefix)) {
            return [pscustomobject]@{ id = [long]$c.id; url = [string]$c.html_url; body = [string]$c.body }
        }
    }
    return $null
}

# POST a new issue comment; returns { id, url }.
function Add-IssueComment {
    param([int]$PrNumber, [string]$RepoFull, [string]$Body)
    $workDir = Get-WorkDir
    $payloadPath = Join-Path $workDir "release-notes-draft-post-$PrNumber.json"
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($payloadPath, ([pscustomobject]@{ body = $Body } | ConvertTo-Json -Depth 100 -Compress), $utf8)
    $r = Invoke-GhJson -ArgumentList @('api', '--method', 'POST', "repos/$RepoFull/issues/$PrNumber/comments", '--input', $payloadPath)
    if (-not $r.ok) { Exit-WithError "POST comment on PR #$PrNumber failed: $($r.error)" }
    return [pscustomobject]@{ id = [long]$r.data.id; url = [string]$r.data.html_url }
}

# PATCH an existing comment + byte-verify (mirrors edit-gh-comment.ps1).
function Set-IssueComment {
    param([long]$CommentId, [string]$RepoFull, [string]$Body)
    $workDir = Get-WorkDir
    $payloadPath = Join-Path $workDir "release-notes-draft-patch-$CommentId.json"
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($payloadPath, ([pscustomobject]@{ body = $Body } | ConvertTo-Json -Depth 100 -Compress), $utf8)
    $endpoint = "repos/$RepoFull/issues/comments/$CommentId"
    $r = Invoke-GhJson -ArgumentList @('api', '--method', 'PATCH', $endpoint, '--input', $payloadPath)
    if (-not $r.ok) { Exit-WithError "PATCH comment $CommentId failed: $($r.error)" }
    $url = [string]$r.data.html_url
    # verify
    $g = Invoke-Gh -ArgumentList @('api', $endpoint, '--jq', '.body')
    $verifyOk = $false; $storedLen = -1
    $sentLen = [System.Text.Encoding]::UTF8.GetByteCount($Body)
    if ($g.ok) {
        $stored = $g.stdout
        if ($stored.EndsWith("`n")) { $stored = $stored.Substring(0, $stored.Length - 1) }
        $stored = $stored.Replace("`r`n", "`n")
        $storedLen = [System.Text.Encoding]::UTF8.GetByteCount($stored)
        $verifyOk = ($stored -ceq $Body)
    }
    return [pscustomobject]@{ url = $url; verifyOk = $verifyOk; sentLength = $sentLen; storedLength = $storedLen }
}

# -- merged-PR listing (for harvest / sweep) ---------------------------------

function Get-MergedMilestonePRs {
    param([string]$RepoFull, [string]$Title)
    $r = Invoke-GhJson -ArgumentList @(
        'pr','list','--repo',$RepoFull,
        '--state','closed',
        '--search', "is:pr is:merged milestone:`"$Title`"",
        '--limit','500',
        '--json','number,title,body,url,closingIssuesReferences'
    )
    if (-not $r.ok) { Exit-WithError "merged PR list failed: $($r.error)" }
    return @($r.data)
}

# Resolve the PR list for harvest/sweep: from a plan file (audit output) if
# given, else by listing merged milestone PRs.
function Resolve-MilestonePRs {
    param([string]$RepoFull, [string]$Title, [string]$Plan)
    if ($Plan) {
        if (-not (Test-Path -LiteralPath $Plan)) { Exit-WithError "plan file not found: $Plan" }
        $p = (Get-Content -Raw -LiteralPath $Plan) | ConvertFrom-Json -Depth 100
        $byPr = @{}
        foreach ($it in @($p.items)) {
            if (-not $it.prNumber) { continue }
            $n = [int]$it.prNumber
            if (-not $byPr.ContainsKey($n)) {
                $byPr[$n] = [pscustomobject]@{ number = $n; title = $it.title; url = $it.url; linkedIssues = @() }
            }
            if ($it.issueNumber) { $byPr[$n].linkedIssues = @(@($byPr[$n].linkedIssues) + [int]$it.issueNumber | Sort-Object -Unique) }
        }
        return @($byPr.Values)
    }
    $prs = Get-MergedMilestonePRs -RepoFull $RepoFull -Title $Title
    $out = @()
    foreach ($pr in $prs) {
        $out += [pscustomobject]@{
            number       = [int]$pr.number
            title        = [string]$pr.title
            url          = [string]$pr.url
            linkedIssues = @(Get-PrLinkedIssues -Pr $pr)
        }
    }
    return $out
}

# -- wiki section regeneration -----------------------------------------------

$ComponentOrderHead = @('LinqToDB')        # always first
$ComponentOrderTail = @('LinqToDB CLI','linq2db.cli')  # always last
$ChangeTypeOrder = @('Breaking','Added','Improved','Fixed','Changed','Removed','Other')

function Get-ComponentRank {
    param([string]$Name)
    for ($i = 0; $i -lt $ComponentOrderHead.Count; $i++) { if ($Name -ieq $ComponentOrderHead[$i]) { return $i } }
    for ($i = 0; $i -lt $ComponentOrderTail.Count; $i++) { if ($Name -ieq $ComponentOrderTail[$i]) { return 9000 + $i } }
    return 1000  # middle: sorted alphabetically among themselves
}

function Get-ChangeTypeRank {
    param([string]$Name)
    for ($i = 0; $i -lt $ChangeTypeOrder.Count; $i++) { if ($Name -ieq $ChangeTypeOrder[$i]) { return $i } }
    return $ChangeTypeOrder.Count
}

# Build the markdown for a version section from prBullets + deepDives.
function Build-VersionSection {
    param([string]$Ver, $PrBullets, $DeepDives)
    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add("### Release $Ver")
    [void]$lines.Add('')

    # group bullets by component, then change type
    $byComp = @{}
    foreach ($b in @($PrBullets)) {
        $comp = if ($b.component) { [string]$b.component } else { 'LinqToDB' }
        $ct   = if ($b.changeType) { [string]$b.changeType } else { 'Other' }
        if (-not $byComp.ContainsKey($comp)) { $byComp[$comp] = @{} }
        if (-not $byComp[$comp].ContainsKey($ct)) { $byComp[$comp][$ct] = @() }
        $byComp[$comp][$ct] += $b
    }

    $comps = @($byComp.Keys) | Sort-Object @{Expression={Get-ComponentRank $_}}, @{Expression={$_}}
    foreach ($comp in $comps) {
        [void]$lines.Add("#### $comp")
        [void]$lines.Add('')
        $cts = @($byComp[$comp].Keys) | Sort-Object @{Expression={Get-ChangeTypeRank $_}}, @{Expression={$_}}
        foreach ($ct in $cts) {
            # change-type as h5 so it renders visibly smaller than the h4 component header
            $label = if ($ct -ieq 'Breaking') { '##### ⚠ Breaking changes' } else { "##### $ct" }
            [void]$lines.Add($label)
            [void]$lines.Add('')
            $bullets = @($byComp[$comp][$ct]) | Sort-Object @{Expression={[int]$_.pr}}
            foreach ($b in $bullets) {
                $ref = if ($b.url) { "[#$($b.pr)]($($b.url))" } else { "#$($b.pr)" }
                $txt = ([string]$b.text).Trim()
                [void]$lines.Add("- $txt ($ref)")
            }
            [void]$lines.Add('')
        }
    }

    foreach ($d in (@($DeepDives) | Sort-Object @{Expression={[int]$_.pr}})) {
        [void]$lines.Add("<!-- rn:deepdive:#$($d.pr) -->")
        # deep-dive spotlight at h4 (component-level peer), above the h5 change-type buckets
        [void]$lines.Add("#### $($d.heading)")
        [void]$lines.Add('')
        [void]$lines.Add((([string]$d.body).Trim("`r","`n")))
        [void]$lines.Add('')
    }
    return (($lines -join "`n").TrimEnd() + "`n")
}

# Splice the regenerated section into the page text, replacing an existing
# `### Release <ver>` block or inserting a new one at the top of the release
# list. Also maintains the TOC bullet. Returns @{ text; action }.
function Splice-VersionSection {
    param([string]$PageText, [string]$Ver, [string]$Section)
    $nl = "`n"
    $text = $PageText.Replace("`r`n", "`n")
    $verRx = [regex]::Escape($Ver)
    $anchor = '#release-' + ($Ver -replace '\.', '')

    # locate existing section: `### Release <ver>` up to next release heading or EOF
    $headRx = "(?m)^###?\s+Release\s+$verRx\b.*$"
    $m = [regex]::Match($text, $headRx)
    $action = ''
    if ($m.Success) {
        $start = $m.Index
        # next release heading after this one
        $nextRx = '(?m)^###?\s+Release\s+\S'
        $after = [regex]::Match($text.Substring($m.Index + $m.Length), $nextRx)
        if ($after.Success) {
            $end = $m.Index + $m.Length + $after.Index
        } else {
            $end = $text.Length
        }
        $newText = $text.Substring(0, $start) + $Section.TrimEnd() + $nl + $nl + $text.Substring($end).TrimStart("`n")
        $text = $newText
        $action = 'replaced'
    } else {
        # insert before the first existing `### Release ` section, or after `***`
        $firstRel = [regex]::Match($text, '(?m)^###?\s+Release\s+\S')
        $insertAt = if ($firstRel.Success) { $firstRel.Index } else { $text.Length }
        $block = $Section.TrimEnd() + $nl + $nl
        $text = $text.Substring(0, $insertAt) + $block + $text.Substring($insertAt)
        $action = 'inserted'
    }

    # TOC bullet maintenance: add `- [Release <ver>](#release-xyz)` as the first
    # TOC entry if not already present.
    if ($text -notmatch [regex]::Escape("($anchor)")) {
        $tocM = [regex]::Match($text, '(?m)^- \[Release ')
        if ($tocM.Success) {
            $tocLine = "- [Release $Ver]($anchor)" + $nl
            $text = $text.Substring(0, $tocM.Index) + $tocLine + $text.Substring($tocM.Index)
        }
    }
    return [pscustomobject]@{ text = $text; action = $action }
}

# -- actions -----------------------------------------------------------------

function Do-Find {
    if (-not $Pr) { Exit-WithError '-Pr required for find' }
    $c = Find-DraftComment -PrNumber $Pr -RepoFull $Repo
    if (-not $c) {
        Write-JsonOutput ([ordered]@{ ok = $true; action = 'find'; pr = $Pr; present = $false; state = $null })
        return
    }
    $state = Read-DraftState -Body $c.body
    Write-JsonOutput ([ordered]@{
        ok = $true; action = 'find'; pr = $Pr; present = $true
        commentId = $c.id; url = $c.url; state = $state
    })
}

function Do-Upsert {
    $m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
    if (-not (Test-IsInteger $m.pr) -or [long]$m.pr -le 0) { Exit-WithError 'manifest.pr (positive int) required' }
    $prNum = [int]$m.pr
    $repoFull = if ($m.repo) { [string]$m.repo } else { $Repo }
    $lastSha = [string]$m.lastSha
    $omit = [bool]$m.omit
    $includeBrief = [bool]$m.includeBrief
    $fullText  = [string]$m.fullText
    $briefText = [string]$m.briefText

    $body = Build-CommentBody -PrNumber $prNum -LastSha $lastSha -Omit $omit -IncludeBrief $includeBrief -FullText $fullText -BriefText $briefText

    $existing = Find-DraftComment -PrNumber $prNum -RepoFull $repoFull
    if ($existing) {
        $res = Set-IssueComment -CommentId $existing.id -RepoFull $repoFull -Body $body
        $exit = if ($res.verifyOk) { 0 } else { 2 }
        Write-JsonOutput ([ordered]@{
            ok = $res.verifyOk; action = 'upsert'; pr = $prNum; commentId = $existing.id
            url = $res.url; patched = $true; created = $false
            verify = [ordered]@{ ok = $res.verifyOk; sentLength = $res.sentLength; storedLength = $res.storedLength }
        })
        exit $exit
    } else {
        $posted = Add-IssueComment -PrNumber $prNum -RepoFull $repoFull -Body $body
        Write-JsonOutput ([ordered]@{
            ok = $true; action = 'upsert'; pr = $prNum; commentId = $posted.id
            url = $posted.url; patched = $false; created = $true
            verify = [ordered]@{ ok = $true; sentLength = [System.Text.Encoding]::UTF8.GetByteCount($body); storedLength = $null }
        })
    }
}

function Do-Harvest {
    if (-not $Milestone) { Exit-WithError '-Milestone required for harvest' }
    $prs = Resolve-MilestonePRs -RepoFull $Repo -Title $Milestone -Plan $PlanFile
    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($pr in $prs) {
        $c = Find-DraftComment -PrNumber ([int]$pr.number) -RepoFull $Repo
        if (-not $c) {
            $items.Add([ordered]@{
                pr = [int]$pr.number; title = $pr.title; hasDraft = $false
                omit = $false; includeBrief = $false; lastSha = $null
                fullText = $null; briefText = $null; linkedIssues = @($pr.linkedIssues)
            }) | Out-Null
            continue
        }
        $state = Read-DraftState -Body $c.body
        $items.Add([ordered]@{
            pr = [int]$pr.number; title = $pr.title; hasDraft = $true
            omit = $state.omit; includeBrief = $state.includeBrief; lastSha = $state.lastSha
            fullText  = (Get-BetweenMarkers -Body $c.body -Open $FullStart  -Close $FullEnd)
            briefText = (Get-BetweenMarkers -Body $c.body -Open $BriefStart -Close $BriefEnd)
            linkedIssues = @($pr.linkedIssues)
        }) | Out-Null
    }
    $harvestPath = Join-Path (Get-WorkDir) "release-$Version-notes-harvest.json"
    $payload = [ordered]@{ ok = $true; action = 'harvest'; milestone = $Milestone; harvestFile = $harvestPath; items = $items.ToArray() }
    [System.IO.File]::WriteAllText($harvestPath, ($payload | ConvertTo-Json -Depth 100), [System.Text.UTF8Encoding]::new($false))
    $briefCount = @($items | Where-Object { $_.includeBrief -and -not $_.omit }).Count
    Write-JsonOutput ([ordered]@{
        ok = $true; action = 'harvest'; milestone = $Milestone; harvestFile = $harvestPath
        counts = [ordered]@{ total = $items.Count; withDraft = @($items | Where-Object { $_.hasDraft }).Count; brief = $briefCount; omitted = @($items | Where-Object { $_.omit }).Count }
    })
}

function Do-ApplyWiki {
    $m = Read-ManifestFromFileOrStdin -ManifestFile $ManifestFile
    $ver = if ($m.version) { [string]$m.version } else { $Version }
    if (-not $ver) { Exit-WithError 'manifest.version (or -Version) required' }
    $clone = if ($m.wikiClone) { [string]$m.wikiClone } elseif ($WikiClone) { $WikiClone } else { Exit-WithError 'manifest.wikiClone (or -WikiClone) required' }
    if (-not (Test-Path -LiteralPath $clone)) {
        Exit-WithError "wiki clone not found at '$clone' — clone it once (on Windows use the no-checkout + sparse-checkout recipe in .claude/docs/release/external-repos.md; a plain 'git clone' fails on the colon-named wiki page)"
    }
    $page = if ($m.page) { [string]$m.page } else { 'Releases-and-Roadmap.md' }
    $pagePath = Join-Path $clone $page

    # sync: require clean tree, then ff-only pull (never reset — don't clobber)
    if (-not $NoSync) {
        $st = Invoke-Git -ArgumentList @('status','--porcelain') -WorkingDirectory $clone
        if (-not $st.ok) { Exit-WithError "git status in wiki clone failed: $($st.error)" }
        if ($st.stdout.Trim()) { Exit-WithError "wiki clone has uncommitted changes — commit/stash/discard them before apply-wiki, or pass -NoSync" }
        $fetch = Invoke-Git -ArgumentList @('fetch','origin') -WorkingDirectory $clone
        if (-not $fetch.ok) { Exit-WithError "git fetch in wiki clone failed: $($fetch.error)" }
        $pull = Invoke-Git -ArgumentList @('pull','--ff-only') -WorkingDirectory $clone
        if (-not $pull.ok) { Exit-WithError "git pull --ff-only in wiki clone failed (diverged?): $($pull.error)" }
    }

    if (-not (Test-Path -LiteralPath $pagePath)) { Exit-WithError "wiki page not found: $pagePath" }
    $pageText = [System.IO.File]::ReadAllText($pagePath, [System.Text.UTF8Encoding]::new($false))

    $section = Build-VersionSection -Ver $ver -PrBullets $m.prBullets -DeepDives $m.deepDives
    $spliced = Splice-VersionSection -PageText $pageText -Ver $ver -Section $section
    [System.IO.File]::WriteAllText($pagePath, $spliced.text, [System.Text.UTF8Encoding]::new($false))

    $diff = Invoke-Git -ArgumentList @('diff','--', $page) -WorkingDirectory $clone
    $diffPath = Join-Path (Get-WorkDir) "release-$ver-wiki.diff"
    [System.IO.File]::WriteAllText($diffPath, [string]$diff.stdout, [System.Text.UTF8Encoding]::new($false))

    Write-JsonOutput ([ordered]@{
        ok = $true; action = 'apply-wiki'; page = $page; sectionAction = $spliced.action
        diffPath = $diffPath; bulletCount = @($m.prBullets).Count; deepDiveCount = @($m.deepDives).Count
        clone = $clone
    })
}

function Do-SweepPlan {
    if (-not $Milestone) { Exit-WithError '-Milestone required for sweep-plan' }
    $prs = Resolve-MilestonePRs -RepoFull $Repo -Title $Milestone -Plan $PlanFile

    # wiki text: local clone page if provided, else published raw URL
    $wikiText = ''
    if ($WikiClone -and (Test-Path -LiteralPath (Join-Path $WikiClone 'Releases-and-Roadmap.md'))) {
        $wikiText = [System.IO.File]::ReadAllText((Join-Path $WikiClone 'Releases-and-Roadmap.md'), [System.Text.UTF8Encoding]::new($false))
    } else {
        $url = "https://raw.githubusercontent.com/wiki/$Repo/Releases-and-Roadmap.md"
        try { $wikiText = (Invoke-WebRequest -Uri $url -TimeoutSec 30 -UseBasicParsing).Content } catch { $wikiText = '' }
    }

    $missingDraft = New-Object 'System.Collections.Generic.List[object]'
    $missingWiki  = New-Object 'System.Collections.Generic.List[object]'
    foreach ($pr in $prs) {
        $prNum = [int]$pr.number
        $c = Find-DraftComment -PrNumber $prNum -RepoFull $Repo
        $hasDraft = [bool]$c
        $omit = $false
        if ($c) { $omit = (Read-DraftState -Body $c.body).omit }
        if (-not $hasDraft) {
            $missingDraft.Add([ordered]@{ pr = $prNum; title = $pr.title; url = $pr.url }) | Out-Null
        }
        # wiki application: covered if the PR# or any linked issue# is mentioned
        $covered = (Test-MentionsRef -Text $wikiText -N $prNum)
        if (-not $covered) {
            foreach ($iss in @($pr.linkedIssues)) {
                if (Test-MentionsRef -Text $wikiText -N ([int]$iss)) { $covered = $true; break }
            }
        }
        if (-not $covered -and -not $omit) {
            $missingWiki.Add([ordered]@{ pr = $prNum; title = $pr.title; url = $pr.url; hasDraft = $hasDraft; omit = $omit }) | Out-Null
        }
    }

    $sweepPath = Join-Path (Get-WorkDir) "release-$Version-notes-sweep.json"
    $payload = [ordered]@{
        ok = $true; action = 'sweep-plan'; milestone = $Milestone; sweepFile = $sweepPath
        missingDraft = $missingDraft.ToArray(); missingWiki = $missingWiki.ToArray()
    }
    [System.IO.File]::WriteAllText($sweepPath, ($payload | ConvertTo-Json -Depth 100), [System.Text.UTF8Encoding]::new($false))
    Write-JsonOutput ([ordered]@{
        ok = $true; action = 'sweep-plan'; milestone = $Milestone; sweepFile = $sweepPath
        counts = [ordered]@{ total = @($prs).Count; missingDraft = $missingDraft.Count; missingWiki = $missingWiki.Count }
    })
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'find'       { Do-Find }
    'upsert'     { Do-Upsert }
    'harvest'    { Do-Harvest }
    'apply-wiki' { Do-ApplyWiki }
    'sweep-plan' { Do-SweepPlan }
    default      { Exit-WithError "unknown action: $Action" }
}
