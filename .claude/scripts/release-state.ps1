<#
release-state.ps1 — state-machine helper for the `/release` orchestrator.

Owns the release-state JSON at `.build/.claude/release-<version>.json` and
keeps the prep-PR body checklist in sync with it. PR body wins on conflict
(it's the canonical store; the state file is a session-resume cache — see
`.claude/docs/release/overview.md` → State).

Actions:

  init          Create default state for a version with an optional ad-hoc
                task list. Writes the state file. Emits the new state.
                Inputs:  -Version <ver>  [-AdHoc '<label1>;<label2>;...']
                         [-PrepBranch <name>]  [-PrepPR <n>]  [-Force]
                Output:  { ok, statePath, state, action: 'init' }

  load          Load state file if present; else synthesize from the PR body
                (if -PrepPR given and the PR has a marker-delimited checklist);
                else fall back to default (requires -Version).
                Inputs:  -Version <ver>  [-PrepPR <n>]
                Output:  { ok, statePath, state, source: 'file'|'pr'|'default' }

  render        Render the orchestrator's status table from a state file.
                Inputs:  -Version <ver>
                Output:  { ok, statePath, render }  (markdown text)

  update        Set the status / annotation of a top-level task or sub-task.
                Recomputes the parent's rolled-up status if a sub-task changes.
                Inputs:  -Version <ver> -TaskId <id> -Status <status>
                         [-Annotation <text>]
                Output:  { ok, statePath, state, action: 'update', taskId, status }

  sync-to-pr    Rewrite the PR body's marker-delimited checklist region from
                state. Preserves the rest of the body verbatim.
                Inputs:  -Version <ver> -PrepPR <n>  [-DryRun]
                Output:  { ok, prepPR, dryRun, before, after }
                         (before/after are only the marker region content)

  sync-from-pr  Read the PR body's marker-delimited checklist, update each
                task's status from the parsed checkboxes (annotations on each
                line are preserved into state). Use on session resume when
                the user may have hand-edited the PR.
                Inputs:  -Version <ver> -PrepPR <n>
                Output:  { ok, statePath, state, changedTasks[] }

Conventions follow `.claude/docs/agent-rules.md` →
*PowerShell Core scripts for complex operations* and
`.claude/docs/script-authoring.md`.

State file schema is documented inline below (BuildDefaultState).
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('init','load','render','update','sync-to-pr','sync-from-pr','ci-probe','ci-ack')]
    [string]$Action,

    [string]$Version,
    [string]$PrepBranch,
    [int]$PrepPR,
    [string]$AdHoc,           # ';'-separated free-form labels
    [string]$TaskId,
    [ValidateSet('open','done','partial','in-progress','skipped')]
    [string]$Status,
    [string]$Annotation,
    [string]$RunIds,          # ci-ack: ','- or ';'-separated build IDs to record as reported
    [switch]$Force,
    [switch]$DryRun,

    [string]$StatePath        # optional override; default = .build/.claude/release-<ver>.json
)

$global:ScriptBaseName = 'release-state'
. (Join-Path $PSScriptRoot '_shared.ps1')

# Capture script-level bound parameters before any function call. Inside a
# function `$PSBoundParameters` refers to the *function's* bound params, not
# the script's — checking it there returns false even for parameters the user
# passed on the command line. Stash the script-scope dictionary here so action
# functions can ask "did the user pass -Annotation?".
$script:BoundParams = $PSBoundParameters

# -- helpers -----------------------------------------------------------------

function Resolve-StatePath {
    param([string]$Ver, [string]$Override)
    if ($Override) { return $Override }
    if (-not $Ver) { Exit-WithError "Version required when -StatePath is not given" }
    $dir = Join-Path (Get-Location) '.build\.claude'
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    return (Join-Path $dir "release-$Ver.json")
}

# Default sub-task template for task 4 (Test matrix). Kept here so the
# checklist structure stays in one place — adding/removing a sub-track is a
# one-line edit. The orchestrator's table and the PR body checklist both
# derive from this list.
function Get-DefaultTestMatrixSubtasks {
    return @(
        @{ id = '4.0'; label = 'T4 binary prerequisite' }
        @{ id = '4.1'; label = 'Provider container + DB init' }
        @{ id = '4.2'; label = 'DB2 iSeries decision' }
        @{ id = '4.3'; label = 'LINQPad 5 (.lpx)' }
        @{ id = '4.4'; label = 'LINQPad 7+ (nugets)' }
        @{ id = '4.5'; label = 'NuGet T4 scaffold' }
        @{ id = '4.6'; label = 'T4 templates (Tests.T4)' }
        @{ id = '4.7'; label = 'CLI scaffold' }
        @{ id = '4.8'; label = 'Targeted-change retest' }
    )
}

function New-Task {
    param([string]$Id, [string]$Label, [bool]$AdHocFlag = $false, [array]$Subtasks = @())
    return [ordered]@{
        id         = $Id
        label      = $Label
        status     = 'open'
        annotation = ''
        adHoc      = $AdHocFlag
        subtasks   = @($Subtasks | ForEach-Object {
            [ordered]@{
                id         = $_.id
                label      = $_.label
                status     = 'open'
                annotation = ''
            }
        })
    }
}

function Build-DefaultState {
    param([string]$Ver, [string]$Branch, [int]$Pr, [string[]]$AdHocLabels)

    if (-not $Branch) { $Branch = "release/prepare-$Ver" }

    $tasks = @(
        (New-Task '0' 'Branch + version bump')
        (New-Task '1' 'Dependencies update')
        (New-Task '2' 'PublicAPI reconciliation')
        (New-Task '3' 'Milestone check')
        (New-Task '4' 'Test matrix' $false (Get-DefaultTestMatrixSubtasks))
        (New-Task '5' 'Release-notes validation')
    )

    if ($AdHocLabels) {
        $i = 1
        foreach ($lbl in $AdHocLabels) {
            $lbl = $lbl.Trim()
            if ($lbl) {
                $tasks += (New-Task "6.$i" "[ad-hoc] $lbl" $true)
                $i++
            }
        }
    }

    return [ordered]@{
        schemaVersion = 1
        version       = $Ver
        prepBranch    = $Branch
        prepPR        = if ($Pr) { $Pr } else { $null }
        releasePR     = $null
        currentPhase  = 'prep'
        tasks         = $tasks
        publish       = [ordered]@{}
        postpublish   = [ordered]@{}
        ci            = [ordered]@{ lastReportedRunIds = @() }
        deps          = [ordered]@{ applied = @(); skipped = @(); ruleAdditions = @(); lastCiTrigger = $null }
        notes         = [ordered]@{ intentionalOmissions = @() }
    }
}

function Read-StateFile {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $raw = Get-Content -Raw -LiteralPath $Path
    if (-not $raw.Trim()) { return $null }
    try { return $raw | ConvertFrom-Json -Depth 100 -AsHashtable }
    catch { Exit-WithError "invalid state JSON at ${Path}: $($_.Exception.Message)" }
}

function Write-StateFile {
    param([string]$Path, $State)
    $json = $State | ConvertTo-Json -Depth 100
    # Always UTF-8 without BOM (matches the rest of `.build/.claude/` and avoids
    # the BOM-required-for-.cs convention that doesn't apply here).
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

# Status tokens used in the PR body checklist. The canonical mapping lives in
# `.claude/docs/release/branch-and-pr.md` → Status tokens.
$script:StatusToken = @{
    'open'        = ' '
    'in-progress' = '~'
    'partial'     = '~'
    'done'        = 'x'
    'skipped'     = '-'
}
$script:TokenStatus = @{
    ' ' = 'open'
    '~' = 'in-progress'
    'x' = 'done'   # uppercase 'X' from manual edits is normalised by ToLowerInvariant below
    '-' = 'skipped'
}

function Get-StatusToken {
    param([string]$Status)
    if ($script:StatusToken.ContainsKey($Status)) { return $script:StatusToken[$Status] }
    return ' '
}

# Roll up a parent's status from its sub-tasks (only when the parent has any).
#   - all sub-tasks done|skipped → done
#   - any sub-task done|skipped + any open → in-progress
#   - all sub-tasks open → open
function Update-ParentRollup {
    param($Task)
    if (-not $Task.subtasks -or @($Task.subtasks).Count -eq 0) { return }
    $subs = @($Task.subtasks)
    $closedCount = ($subs | Where-Object { $_.status -in 'done','skipped' }).Count
    $openCount   = ($subs | Where-Object { $_.status -eq 'open' }).Count
    if ($closedCount -eq $subs.Count) {
        $Task.status = 'done'
    } elseif ($closedCount -gt 0 -and $openCount -gt 0) {
        $Task.status = 'in-progress'
    } else {
        $Task.status = 'open'
    }
}

# -- checklist rendering -----------------------------------------------------

function Format-ChecklistMarkdown {
    param($State)
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($t in @($State.tasks)) {
        $tok = Get-StatusToken $t.status
        $line = "- [$tok] $($t.id). $($t.label)"
        if ($t.annotation) { $line += "  _$($t.annotation)_" }
        [void]$lines.Add($line)
        foreach ($s in @($t.subtasks)) {
            $stok = Get-StatusToken $s.status
            $sline = "  - [$stok] $($s.id) $($s.label)"
            if ($s.annotation) { $sline += "  _$($s.annotation)_" }
            [void]$lines.Add($sline)
        }
    }
    return ($lines -join "`n")
}

# Plain-text status table used by /release step 3.
function Format-StatusTable {
    param($State)
    $sb = [System.Text.StringBuilder]::new()
    $head = "Release $($State.version) — branch $($State.prepBranch)"
    if ($State.prepPR) { $head += " — PR #$($State.prepPR)" }
    $head += " — phase: $($State.currentPhase)"
    [void]$sb.AppendLine($head)
    [void]$sb.AppendLine('')
    foreach ($t in @($State.tasks)) {
        $tok = Get-StatusToken $t.status
        $line = "  [$tok] $($t.id). $($t.label)"
        if ($t.annotation) { $line += "  ($($t.annotation))" }
        [void]$sb.AppendLine($line)
        foreach ($s in @($t.subtasks)) {
            $stok = Get-StatusToken $s.status
            $sline = "        [$stok] $($s.id) $($s.label)"
            if ($s.annotation) { $sline += "  ($($s.annotation))" }
            [void]$sb.AppendLine($sline)
        }
    }
    # Suggest next task: lowest-id open or in-progress that isn't a parent
    # carrying sub-tasks (parents resolve via their children).
    $next = $null
    foreach ($t in @($State.tasks)) {
        if ($t.status -in 'open','in-progress','partial') {
            if (@($t.subtasks).Count -gt 0) {
                $openSub = @($t.subtasks) | Where-Object { $_.status -eq 'open' } | Select-Object -First 1
                if ($openSub) { $next = "$($t.id) → $($openSub.id) $($openSub.label) (in $($t.label))"; break }
            } else {
                $next = "$($t.id). $($t.label)"; break
            }
        }
    }
    if ($next) {
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("Next: $next")
    } else {
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine("Next: all standard tasks done — see /release step 5 (prep-merge gate)")
    }
    return $sb.ToString().TrimEnd()
}

# -- PR body sync ------------------------------------------------------------

$script:StartMarker = '<!-- release-state:checklist:start -->'
$script:EndMarker   = '<!-- release-state:checklist:end -->'

function Get-PrBody {
    param([int]$PrNumber)
    $r = Invoke-Gh -ArgumentList @('pr','view',$PrNumber,'--repo','linq2db/linq2db','--json','body','--jq','.body')
    if (-not $r.ok) { Exit-WithError "gh pr view #$PrNumber failed: $($r.error)" }
    return $r.stdout.TrimEnd("`n")
}

function Set-PrBody {
    param([int]$PrNumber, [string]$Body)
    $tmp = Join-Path (Get-Location) ".build\.claude\release-state-prbody-$PrNumber.md"
    [System.IO.File]::WriteAllText($tmp, $Body, [System.Text.UTF8Encoding]::new($false))
    $r = Invoke-Gh -ArgumentList @('pr','edit',$PrNumber,'--repo','linq2db/linq2db','--body-file',$tmp)
    if (-not $r.ok) { Exit-WithError "gh pr edit #$PrNumber failed: $($r.error)" }
}

function Split-BodyByMarkers {
    param([string]$Body)
    $startIdx = $Body.IndexOf($script:StartMarker)
    $endIdx   = $Body.IndexOf($script:EndMarker)
    if ($startIdx -lt 0 -or $endIdx -lt 0 -or $endIdx -le $startIdx) {
        return @{ pre = $Body; checklist = ''; post = ''; hasMarkers = $false }
    }
    $preEnd = $startIdx + $script:StartMarker.Length
    $pre = $Body.Substring(0, $preEnd)
    $checklist = $Body.Substring($preEnd, $endIdx - $preEnd).Trim("`r","`n")
    $post = $Body.Substring($endIdx)
    return @{ pre = $pre; checklist = $checklist; post = $post; hasMarkers = $true }
}

function Build-PrBodyWithChecklist {
    param([string]$Body, [string]$NewChecklist)
    $split = Split-BodyByMarkers $Body
    $checklistBlock = "`n" + $NewChecklist + "`n"
    if ($split.hasMarkers) {
        return $split.pre + $checklistBlock + $split.post
    }
    # No markers: append a default body wrapper at the end.
    $appendix = "`n`n" + $script:StartMarker + $checklistBlock + $script:EndMarker + "`n"
    return ($Body.TrimEnd("`r","`n") + $appendix)
}

# Parse "  - [x] 4.1 Provider container + DB init  _annotation_" into a
# checklist row. Returns $null if the line doesn't match.
function ConvertFrom-ChecklistLine {
    param([string]$Line)
    if ($Line -notmatch '^\s*-\s*\[([ xX~\-])\]\s+([0-9]+(?:\.[0-9]+)?)\s*\.?\s+(.+?)(?:\s+_(.+)_)?\s*$') {
        return $null
    }
    $token = $Matches[1].ToLowerInvariant()
    $id    = $Matches[2]
    $label = $Matches[3].Trim().TrimEnd('.')
    $annot = $Matches[4]
    if (-not $annot) { $annot = '' }
    $status = if ($script:TokenStatus.ContainsKey($token)) { $script:TokenStatus[$token] } else { 'open' }
    return @{ id = $id; label = $label; status = $status; annotation = $annot }
}

# Update state from a parsed-from-PR checklist. Only status + annotation are
# pulled — labels and task structure stay in the state file (PR-body label
# edits are not authoritative because they'd let typos diverge state).
function Sync-StateFromChecklist {
    param($State, [string]$ChecklistText)
    $byId = @{}
    foreach ($t in @($State.tasks)) {
        $byId[$t.id] = $t
        foreach ($s in @($t.subtasks)) { $byId[$s.id] = $s }
    }
    $changed = @()
    foreach ($line in ($ChecklistText -split "`n")) {
        $row = ConvertFrom-ChecklistLine $line
        if (-not $row) { continue }
        $target = $byId[$row.id]
        if (-not $target) { continue }   # unknown id (ad-hoc not yet in state) — skip
        if ($target.status -ne $row.status -or $target.annotation -ne $row.annotation) {
            $changed += @{ id = $row.id; oldStatus = $target.status; newStatus = $row.status }
            $target.status = $row.status
            $target.annotation = $row.annotation
        }
    }
    # Re-roll-up parents in case a sub-task moved.
    foreach ($t in @($State.tasks)) { Update-ParentRollup $t }
    return $changed
}

# -- actions -----------------------------------------------------------------

function Do-Init {
    if (-not $Version) { Exit-WithError "init: -Version required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    if ((Test-Path -LiteralPath $path) -and -not $Force) {
        Exit-WithError "init: state file already exists at $path (use -Force to overwrite)"
    }
    $adHocList = @()
    if ($AdHoc) {
        $adHocList = $AdHoc -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    }
    $state = Build-DefaultState -Ver $Version -Branch $PrepBranch -Pr $PrepPR -AdHocLabels $adHocList
    Write-StateFile -Path $path -State $state
    Write-JsonOutput @{ ok = $true; action = 'init'; statePath = $path; state = $state }
}

function Do-Load {
    if (-not $Version) { Exit-WithError "load: -Version required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    $source = 'file'
    if (-not $state) {
        if ($PrepPR) {
            # Try to synthesize from PR body
            $body = Get-PrBody -PrNumber $PrepPR
            $split = Split-BodyByMarkers $body
            if ($split.hasMarkers) {
                $state = Build-DefaultState -Ver $Version -Branch $PrepBranch -Pr $PrepPR -AdHocLabels @()
                Sync-StateFromChecklist -State $state -ChecklistText $split.checklist | Out-Null
                Write-StateFile -Path $path -State $state
                $source = 'pr'
            }
        }
    }
    if (-not $state) {
        $state = Build-DefaultState -Ver $Version -Branch $PrepBranch -Pr $PrepPR -AdHocLabels @()
        Write-StateFile -Path $path -State $state
        $source = 'default'
    }
    # PR-body wins on conflict when we did read from file but also have a PR
    # number — quick re-sync brings file in line with any manual checkbox flips.
    if ($source -eq 'file' -and $PrepPR) {
        $body = Get-PrBody -PrNumber $PrepPR
        $split = Split-BodyByMarkers $body
        if ($split.hasMarkers) {
            $changed = Sync-StateFromChecklist -State $state -ChecklistText $split.checklist
            if (@($changed).Count -gt 0) { Write-StateFile -Path $path -State $state }
        }
    }
    Write-JsonOutput @{ ok = $true; action = 'load'; statePath = $path; state = $state; source = $source }
}

function Do-Render {
    if (-not $Version) { Exit-WithError "render: -Version required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) { Exit-WithError "render: no state at $path — run -Action init or -Action load first" }
    $render = Format-StatusTable $state
    Write-JsonOutput @{ ok = $true; action = 'render'; statePath = $path; render = $render }
}

function Do-Update {
    if (-not $Version) { Exit-WithError "update: -Version required" }
    if (-not $TaskId)  { Exit-WithError "update: -TaskId required" }
    if (-not $Status)  { Exit-WithError "update: -Status required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) { Exit-WithError "update: no state at $path" }

    $found = $null
    $parent = $null
    foreach ($t in @($state.tasks)) {
        if ($t.id -eq $TaskId) { $found = $t; break }
        foreach ($s in @($t.subtasks)) {
            if ($s.id -eq $TaskId) { $found = $s; $parent = $t; break }
        }
        if ($found) { break }
    }
    if (-not $found) { Exit-WithError "update: task id '$TaskId' not found in state" }

    $found.status = $Status
    if ($script:BoundParams.ContainsKey('Annotation')) { $found.annotation = $Annotation }
    if ($parent) { Update-ParentRollup $parent }

    Write-StateFile -Path $path -State $state
    Write-JsonOutput @{ ok = $true; action = 'update'; statePath = $path; taskId = $TaskId; status = $Status; state = $state }
}

function Do-SyncToPr {
    if (-not $Version) { Exit-WithError "sync-to-pr: -Version required" }
    if (-not $PrepPR)  { Exit-WithError "sync-to-pr: -PrepPR required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) { Exit-WithError "sync-to-pr: no state at $path" }

    $body = Get-PrBody -PrNumber $PrepPR
    $split = Split-BodyByMarkers $body
    $before = $split.checklist
    $newChecklist = Format-ChecklistMarkdown $state
    $newBody = Build-PrBodyWithChecklist -Body $body -NewChecklist $newChecklist

    if (-not $DryRun) {
        if ($newBody -ne $body) {
            Set-PrBody -PrNumber $PrepPR -Body $newBody
        }
    }

    Write-JsonOutput @{
        ok      = $true
        action  = 'sync-to-pr'
        prepPR  = $PrepPR
        dryRun  = [bool]$DryRun
        before  = $before
        after   = $newChecklist
        changed = ($newBody -ne $body)
    }
}

function Do-SyncFromPr {
    if (-not $Version) { Exit-WithError "sync-from-pr: -Version required" }
    if (-not $PrepPR)  { Exit-WithError "sync-from-pr: -PrepPR required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) {
        $state = Build-DefaultState -Ver $Version -Branch $PrepBranch -Pr $PrepPR -AdHocLabels @()
    }
    $body = Get-PrBody -PrNumber $PrepPR
    $split = Split-BodyByMarkers $body
    $changed = @()
    if ($split.hasMarkers) {
        $changed = Sync-StateFromChecklist -State $state -ChecklistText $split.checklist
        if (@($changed).Count -gt 0) { Write-StateFile -Path $path -State $state }
    }
    Write-JsonOutput @{ ok = $true; action = 'sync-from-pr'; statePath = $path; state = $state; changedTasks = $changed }
}

# -- ci-probe / ci-ack -------------------------------------------------------

# Parse an Azure DevOps build URL into a build id. Examples:
#   https://dev.azure.com/.../_build/results?buildId=12345&view=...   -> 12345
#   https://dev.azure.com/.../_build/results/12345                     -> 12345
# Returns $null if no buildId can be extracted.
function Get-AzpBuildIdFromUrl {
    param([string]$Url)
    if (-not $Url) { return $null }
    if ($Url -match '[?&]buildId=([0-9]+)') { return [int]$Matches[1] }
    if ($Url -match '/_build/results/([0-9]+)\b') { return [int]$Matches[1] }
    return $null
}

# Classify a `gh pr checks` row as a failure-needing-alert. `bucket` from gh's
# new API is the cleanest signal (fail / cancel / pending / pass / skipping).
# Older gh versions don't expose `bucket` and we fall back to `state` /
# `conclusion`.
function Test-IsFailureCheck {
    param($Check)
    $b = ($Check.bucket | ForEach-Object { $_ }) -as [string]
    if ($b) { return ($b -in 'fail','cancel') }
    $s = (($Check.state -as [string]) + ' ' + ($Check.conclusion -as [string])).ToLowerInvariant()
    return ($s -match '\b(failure|failed|cancelled|canceled|timed_out|action_required)\b')
}

function Do-CiProbe {
    if (-not $Version) { Exit-WithError "ci-probe: -Version required" }
    if (-not $PrepPR)  { Exit-WithError "ci-probe: -PrepPR required" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) { Exit-WithError "ci-probe: no state at $path" }

    # gh pr checks doesn't accept --json directly the way pr view does — we use
    # `--json` with a curated field list via the newer gh release; older
    # versions silently drop unknown fields, leaving us with what they do
    # know. `bucket` is the post-2024 normalized failure classification.
    $r = Invoke-Gh -ArgumentList @(
        'pr','checks',[string]$PrepPR,'--repo','linq2db/linq2db',
        '--json','name,state,bucket,conclusion,link,startedAt,completedAt,workflow'
    )
    if (-not $r.ok) { Exit-WithError "gh pr checks failed: $($r.error)" }
    $checks = @()
    if ($r.stdout.Trim()) {
        try { $checks = $r.stdout | ConvertFrom-Json -Depth 50 -AsHashtable }
        catch { Exit-WithError "gh pr checks output is not JSON: $($_.Exception.Message)" }
    }

    $reported = @($state.ci.lastReportedRunIds)
    $newFailures = @()
    foreach ($c in @($checks)) {
        if (-not (Test-IsFailureCheck $c)) { continue }
        $buildId = Get-AzpBuildIdFromUrl ($c.link -as [string])
        # Use buildId when known, else a name+startedAt synthetic key.
        $key = if ($buildId) { "azp:$buildId" } else { "key:$($c.name)@$($c.startedAt)" }
        if ($reported -contains $key) { continue }
        $newFailures += [ordered]@{
            key         = $key
            buildId     = $buildId
            name        = $c.name
            state       = $c.state
            bucket      = $c.bucket
            conclusion  = $c.conclusion
            link        = $c.link
            startedAt   = $c.startedAt
            completedAt = $c.completedAt
            workflow    = $c.workflow
        }
    }

    Write-JsonOutput @{
        ok          = $true
        action      = 'ci-probe'
        statePath   = $path
        prepPR      = $PrepPR
        newFailures = $newFailures
        totalChecks = @($checks).Count
        reportedKeys = $reported
    }
}

function Do-CiAck {
    if (-not $Version) { Exit-WithError "ci-ack: -Version required" }
    if (-not $RunIds)  { Exit-WithError "ci-ack: -RunIds required (',' or ';' separated keys from ci-probe.newFailures[].key)" }
    $path = Resolve-StatePath -Ver $Version -Override $StatePath
    $state = Read-StateFile -Path $path
    if (-not $state) { Exit-WithError "ci-ack: no state at $path" }

    $incoming = ($RunIds -split '[,;]') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    $reported = @($state.ci.lastReportedRunIds)
    $added = @()
    foreach ($k in $incoming) {
        if ($reported -notcontains $k) {
            $reported += $k
            $added += $k
        }
    }
    $state.ci.lastReportedRunIds = $reported
    Write-StateFile -Path $path -State $state
    Write-JsonOutput @{
        ok        = $true
        action    = 'ci-ack'
        statePath = $path
        added     = $added
        total     = @($reported).Count
    }
}

# -- dispatch ----------------------------------------------------------------

switch ($Action) {
    'init'         { Do-Init }
    'load'         { Do-Load }
    'render'       { Do-Render }
    'update'       { Do-Update }
    'sync-to-pr'   { Do-SyncToPr }
    'sync-from-pr' { Do-SyncFromPr }
    'ci-probe'     { Do-CiProbe }
    'ci-ack'       { Do-CiAck }
    default        { Exit-WithError "unknown action: $Action" }
}
