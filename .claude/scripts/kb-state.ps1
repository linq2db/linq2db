#!/usr/bin/env pwsh
<#
KB state manager — single entry point for reading/writing
.claude/knowledge-base/state/* and applying agent fenced output.

One permission rule covers every operation:
    Bash(pwsh -NoProfile -File .claude/scripts/kb-state.ps1:*)

Input (stdin, JSON):
  {
    "op": "<operation>",
    ...op-specific fields...
  }

Output (stdout, JSON): operation-specific result.

Operations
----------
  get-progress
    -> { schema, started_at, current_step, steps[] }

  set-step
    fields: { step: <id-or-name>, status: pending|in-progress|partial|done,
              gate_failures?: [string], started_at?: ISO, finished_at?: ISO }
    -> { ok: true, step: {<merged step entry>} }

  get-cursor
    fields: { source: code|commits|issues|prs|discussions|wiki }
    -> { source, ...cursor fields... }

  set-cursor
    fields: { source, value: { ... } }
    -> { ok: true, source, value }

  init
    Initializes state files (build-progress.json, cursors.json, audit-log.md)
    if they don't exist. Idempotent.
    -> { ok: true, created: [paths] }

  apply-fences
    fields: { agentOutput: "<full text containing fences>",
              currentSha: "<git rev-parse HEAD>" }
    Parses fenced output per .claude/agents/_shared/kb-protocol.md, validates,
    writes artifacts, applies index patches, returns gate result.
    -> { ok, artifacts: [{path, ok, errors[]}],
         indexPatches: [{path, ok, errors[]}],
         coverageSummary: { tier_1, tier_2, tier_3 } | null,
         unclassified: [{path, reason}],
         auditNotes: [string],
         deferred:        [{area, added, cleared}],
         gateFailures: [string] }

  get-deferred
    fields: { area?: <code> }
    -> { schema, areas: { <code>: { deferred_at, deferred_at_sha, files[] } } }
       (filtered to one area when `area` is given)

  set-deferred-area
    fields: { area, deferred_at?, deferred_at_sha?, files: [{path, reason}] }
    Replaces the area's entry. Used by the backfill script.
    -> { ok: true, area, count }

  summary
    fields: { }
    -> { progress, cursors, audit_tail }   for /kb-status

  append-audit
    fields: { event: "<title>", lines?: [string] }
    -> { ok: true }
#>

$global:ScriptBaseName = 'kb-state'
. "$PSScriptRoot/_shared.ps1"

$RepoRoot     = Resolve-Path "$PSScriptRoot/../.." | Select-Object -ExpandProperty Path
$KbRoot       = Join-Path $RepoRoot '.claude/knowledge-base'
$StateDir     = Join-Path $KbRoot 'state'
$ProgressFile = Join-Path $StateDir 'build-progress.json'
$CursorsFile  = Join-Path $StateDir 'cursors.json'
$DeferredFile = Join-Path $StateDir 'deferred-coverage.json'
$AuditLog     = Join-Path $StateDir 'audit-log.md'

function Read-JsonFile {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    $raw = [System.IO.File]::ReadAllText($Path, [System.Text.UTF8Encoding]::new($false))
    if (-not $raw -or -not $raw.Trim()) { return $null }
    return $raw | ConvertFrom-Json -Depth 100
}

function Write-JsonFile {
    param([string]$Path, $Data)
    $dir = Split-Path -Parent $Path
    [void](New-Item -ItemType Directory -Force -Path $dir)
    $json = $Data | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($Path, $json + "`n", [System.Text.UTF8Encoding]::new($false))
}

function Append-AuditEntry {
    param([string]$Event, [string[]]$Lines = @())
    $ts = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("## $ts — $Event")
    foreach ($l in $Lines) {
        if ($l) { [void]$sb.AppendLine("- $l") }
    }
    [void]$sb.AppendLine('')
    [void](New-Item -ItemType Directory -Force -Path (Split-Path -Parent $AuditLog))
    [System.IO.File]::AppendAllText($AuditLog, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
}

function Get-StepDefaults {
    @(
        @{ id = 0;  name = 'bootstrap'              }
        @{ id = 1;  name = 'area-registry'          }
        @{ id = 2;  name = 'architecture-overview'  }
        @{ id = 3;  name = 'architecture-per-area'  }
        @{ id = 4;  name = 'conventions'            }
        @{ id = 5;  name = 'history-by-year'        }
        @{ id = 6;  name = 'history-decisions'      }
        @{ id = 7;  name = 'github-indexes'         }
        @{ id = 8;  name = 'github-themes'          }
        @{ id = 9;  name = 'wiki-mirror'            }
        @{ id = 10; name = 'detected-issues'        }
        @{ id = 11; name = 'area-rollup'            }
        @{ id = 12; name = 'glossary'               }
        @{ id = 13; name = 'validation'             }
    )
}

function Get-DefaultCursors {
    [pscustomobject]@{
        schema      = 1
        code        = [pscustomobject]@{ sha = $null; verified_at = $null }
        commits     = [pscustomobject]@{ sha = $null; year_done_through = $null }
        issues      = [pscustomobject]@{ updated_at = '1970-01-01T00:00:00Z' }
        prs         = [pscustomobject]@{ updated_at = '1970-01-01T00:00:00Z' }
        discussions = [pscustomobject]@{ updated_at = '1970-01-01T00:00:00Z' }
        wiki        = [pscustomobject]@{ sha = $null }
    }
}

function Get-DefaultProgress {
    $steps = @()
    foreach ($s in Get-StepDefaults) {
        $steps += [pscustomobject]@{
            id           = $s.id
            name         = $s.name
            status       = 'pending'
            started_at   = $null
            finished_at  = $null
            gate_failures = @()
        }
    }
    [pscustomobject]@{
        schema       = 1
        started_at   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        current_step = 0
        steps        = $steps
    }
}

function Get-DefaultDeferred {
    [pscustomobject]@{
        schema = 1
        areas  = [pscustomobject]@{}
    }
}

function Op-Init {
    $created = @()
    if (-not (Test-Path $ProgressFile)) {
        Write-JsonFile -Path $ProgressFile -Data (Get-DefaultProgress)
        $created += $ProgressFile
    }
    if (-not (Test-Path $CursorsFile)) {
        Write-JsonFile -Path $CursorsFile -Data (Get-DefaultCursors)
        $created += $CursorsFile
    }
    if (-not (Test-Path $DeferredFile)) {
        Write-JsonFile -Path $DeferredFile -Data (Get-DefaultDeferred)
        $created += $DeferredFile
    }
    if (-not (Test-Path $AuditLog)) {
        Append-AuditEntry -Event 'kb-build started' -Lines @('state initialized')
        $created += $AuditLog
    }
    return [pscustomobject]@{ ok = $true; created = $created }
}

function Read-Deferred {
    $d = Read-JsonFile $DeferredFile
    if (-not $d) { return Get-DefaultDeferred }
    if (-not $d.areas) { $d | Add-Member -NotePropertyName areas -NotePropertyValue ([pscustomobject]@{}) -Force }
    return $d
}

function Save-Deferred {
    param($D)
    Write-JsonFile -Path $DeferredFile -Data $D
}

function Get-AreaEntry {
    param($Deferred, [string]$Area)
    if (-not $Deferred.areas.PSObject.Properties[$Area]) { return $null }
    return $Deferred.areas.$Area
}

function Set-AreaEntry {
    param($Deferred, [string]$Area, $Entry)
    if ($Deferred.areas.PSObject.Properties[$Area]) {
        $Deferred.areas.$Area = $Entry
    } else {
        $Deferred.areas | Add-Member -NotePropertyName $Area -NotePropertyValue $Entry -Force
    }
}

function Remove-AreaEntry {
    param($Deferred, [string]$Area)
    if ($Deferred.areas.PSObject.Properties[$Area]) {
        $Deferred.areas.PSObject.Properties.Remove($Area)
    }
}

function Op-GetDeferred {
    param($M)
    $d = Read-Deferred
    if ($M.area) {
        $entry = Get-AreaEntry -Deferred $d -Area ([string]$M.area)
        $areas = [pscustomobject]@{}
        if ($entry) {
            $areas | Add-Member -NotePropertyName ([string]$M.area) -NotePropertyValue $entry -Force
        }
        return [pscustomobject]@{ schema = $d.schema; areas = $areas }
    }
    return $d
}

function Op-SetDeferredArea {
    param($M)
    if (-not $M.area) { Exit-WithError 'area required' }
    if ($null -eq $M.files) { Exit-WithError 'files required (use [] to clear)' }
    $d = Read-Deferred
    $files = @()
    foreach ($f in $M.files) {
        if (-not $f.path) { continue }
        $files += [pscustomobject]@{ path = [string]$f.path; reason = if ($f.reason) { [string]$f.reason } else { 'budget' } }
    }
    if ($files.Count -eq 0) {
        Remove-AreaEntry -Deferred $d -Area ([string]$M.area)
        Save-Deferred -D $d
        return [pscustomobject]@{ ok = $true; area = $M.area; count = 0 }
    }
    $today = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
    $entry = [pscustomobject]@{
        deferred_at      = if ($M.deferred_at) { [string]$M.deferred_at } else { $today }
        deferred_at_sha  = if ($M.deferred_at_sha) { [string]$M.deferred_at_sha } else { '' }
        files            = $files
    }
    Set-AreaEntry -Deferred $d -Area ([string]$M.area) -Entry $entry
    Save-Deferred -D $d
    return [pscustomobject]@{ ok = $true; area = [string]$M.area; count = $files.Count }
}

function Merge-DeferredFiles {
    param($Deferred, [string]$Area, $NewFiles, [string]$DeferredAt, [string]$DeferredAtSha)
    $entry = Get-AreaEntry -Deferred $Deferred -Area $Area
    $byPath = @{}
    if ($entry -and $entry.files) {
        foreach ($f in $entry.files) {
            if ($f.path) { $byPath[[string]$f.path] = $f }
        }
    }
    $added = 0
    foreach ($nf in $NewFiles) {
        if (-not $nf.path) { continue }
        $p = [string]$nf.path
        $reason = if ($nf.reason) { [string]$nf.reason } else { 'budget' }
        if (-not $byPath.ContainsKey($p)) { $added++ }
        $byPath[$p] = [pscustomobject]@{ path = $p; reason = $reason }
    }
    $files = @()
    foreach ($p in ($byPath.Keys | Sort-Object)) { $files += $byPath[$p] }
    $newEntry = [pscustomobject]@{
        deferred_at     = $DeferredAt
        deferred_at_sha = $DeferredAtSha
        files           = $files
    }
    Set-AreaEntry -Deferred $Deferred -Area $Area -Entry $newEntry
    return $added
}

function Clear-DeferredFiles {
    param($Deferred, [string]$Area, [string[]]$Paths)
    $entry = Get-AreaEntry -Deferred $Deferred -Area $Area
    if (-not $entry -or -not $entry.files) { return 0 }
    $set = @{}
    foreach ($p in $Paths) { $set[[string]$p] = $true }
    $kept = @()
    $cleared = 0
    foreach ($f in $entry.files) {
        if ($f.path -and $set.ContainsKey([string]$f.path)) { $cleared++ } else { $kept += $f }
    }
    if ($kept.Count -eq 0) {
        Remove-AreaEntry -Deferred $Deferred -Area $Area
    } else {
        $entry.files = $kept
    }
    return $cleared
}

function Op-GetProgress {
    $p = Read-JsonFile $ProgressFile
    if (-not $p) { Exit-WithError "build-progress.json missing — run init first" }
    return $p
}

function Find-Step {
    param($Progress, $Selector)
    foreach ($s in $Progress.steps) {
        if (Test-IsInteger $Selector) {
            if ([long]$s.id -eq [long]$Selector) { return $s }
        } else {
            if ($s.name -eq [string]$Selector) { return $s }
        }
    }
    return $null
}

function Op-SetStep {
    param($M)
    $progress = Op-GetProgress
    $step = Find-Step -Progress $progress -Selector $M.step
    if (-not $step) { Exit-WithError "step not found: $($M.step)" }
    if ($M.status) { $step.status = [string]$M.status }
    if ($M.started_at)  { $step.started_at  = [string]$M.started_at }
    if ($M.finished_at) { $step.finished_at = [string]$M.finished_at }
    if ($M.gate_failures) { $step.gate_failures = @($M.gate_failures) }
    if ($step.status -eq 'in-progress') {
        if (-not $step.started_at) { $step.started_at = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ') }
        $progress.current_step = $step.id
    }
    if ($step.status -eq 'done' -and -not $step.finished_at) {
        $step.finished_at = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    Write-JsonFile -Path $ProgressFile -Data $progress
    return [pscustomobject]@{ ok = $true; step = $step }
}

function Op-GetCursor {
    param($M)
    if (-not $M.source) { Exit-WithError 'source required' }
    $cursors = Read-JsonFile $CursorsFile
    if (-not $cursors) { Exit-WithError 'cursors.json missing — run init first' }
    $val = $cursors.($M.source)
    if (-not $val) { Exit-WithError "unknown source: $($M.source)" }
    return [pscustomobject]@{ source = $M.source; value = $val }
}

function Op-SetCursor {
    param($M)
    if (-not $M.source) { Exit-WithError 'source required' }
    if (-not $M.value)  { Exit-WithError 'value required' }
    $cursors = Read-JsonFile $CursorsFile
    if (-not $cursors) { $cursors = Get-DefaultCursors }
    $cursors.($M.source) = $M.value
    Write-JsonFile -Path $CursorsFile -Data $cursors
    return [pscustomobject]@{ ok = $true; source = $M.source; value = $M.value }
}

function Op-Summary {
    $progress = Read-JsonFile $ProgressFile
    $cursors  = Read-JsonFile $CursorsFile
    $audit_tail = @()
    if (Test-Path $AuditLog) {
        $lines = [System.IO.File]::ReadAllLines($AuditLog, [System.Text.UTF8Encoding]::new($false))
        $tail = if ($lines.Count -gt 40) { $lines[($lines.Count - 40)..($lines.Count - 1)] } else { $lines }
        $audit_tail = @($tail)
    }
    return [pscustomobject]@{
        progress   = $progress
        cursors    = $cursors
        audit_tail = $audit_tail
    }
}

function Op-AppendAudit {
    param($M)
    if (-not $M.event) { Exit-WithError 'event required' }
    $lines = @()
    if ($M.lines) { foreach ($l in $M.lines) { $lines += [string]$l } }
    Append-AuditEntry -Event ([string]$M.event) -Lines $lines
    return [pscustomobject]@{ ok = $true }
}

# ---------- apply-fences ----------

$KnownArtifactRoots = @(
    'architecture/', 'conventions/', 'history/by-year/', 'history/decisions/',
    'github/wiki/', 'detected-issues/items/', 'areas/', 'glossary.md', 'README.md'
)

$KnownIndexFiles = @(
    'github/issues-index.json', 'github/prs-index.json',
    'github/discussions-index.json', 'detected-issues/index.json'
)

$KnownIndexWriteFiles = @('github/milestones.json')

function Test-ArtifactPath {
    param([string]$Path)
    if ($Path -match '\.\.|^/|\\') { return $false }
    foreach ($root in $KnownArtifactRoots) {
        if ($root.EndsWith('/')) {
            if ($Path.StartsWith($root)) { return $true }
        } else {
            if ($Path -eq $root) { return $true }
        }
    }
    return $false
}

function Parse-Frontmatter {
    param([string]$Body)
    $lines = $Body -split "`n"
    if ($lines.Count -lt 3) { return $null }
    if ($lines[0].Trim() -ne '---') { return $null }
    $end = -1
    for ($i = 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '---') { $end = $i; break }
    }
    if ($end -lt 0) { return $null }
    $fm = @{}
    for ($i = 1; $i -lt $end; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*([A-Za-z0-9_]+)\s*:\s*(.*)$') {
            $key = $Matches[1]
            $val = $Matches[2].Trim()
            if ($val.StartsWith('[') -and $val.EndsWith(']')) {
                $inner = $val.Substring(1, $val.Length - 2).Trim()
                if (-not $inner) {
                    $fm[$key] = @()
                } else {
                    $fm[$key] = @($inner -split ',\s*' | ForEach-Object { $_.Trim() })
                }
            } else {
                $fm[$key] = $val
            }
        }
    }
    return [pscustomobject]@{ data = $fm; bodyStart = $end + 1; bodyEnd = $lines.Count - 1; raw = $Body }
}

function Validate-Artifact {
    param([string]$Path, [string]$Body, [string]$CurrentSha)
    $errors = @()
    if (-not (Test-ArtifactPath $Path)) {
        $errors += "path '$Path' is not a known KB artifact root"
        return $errors
    }
    # README.md, glossary.md, github/wiki/* don't require KB frontmatter (wiki is mirrored verbatim)
    $exemptFromFrontmatter = ($Path -eq 'README.md') -or ($Path.StartsWith('github/wiki/'))
    if ($exemptFromFrontmatter) { return $errors }

    $fm = Parse-Frontmatter $Body
    if (-not $fm) {
        $errors += "frontmatter missing or unterminated"
        return $errors
    }
    foreach ($req in @('area', 'kind', 'sources', 'confidence', 'last_verified', 'last_verified_sha')) {
        if (-not $fm.data.ContainsKey($req)) { $errors += "frontmatter missing field: $req" }
    }
    if ($fm.data.ContainsKey('confidence')) {
        if ($fm.data['confidence'] -notin @('high', 'medium', 'low')) {
            $errors += "confidence must be high|medium|low (got: $($fm.data['confidence']))"
        }
    }
    if ($fm.data.ContainsKey('last_verified_sha') -and $CurrentSha -and $fm.data['last_verified_sha'] -ne $CurrentSha) {
        $errors += "last_verified_sha '$($fm.data['last_verified_sha'])' does not match current HEAD '$CurrentSha'"
    }
    if ($fm.data.ContainsKey('coverage_tier_1') -or $fm.data.ContainsKey('coverage_tier_2')) {
        if ($Body -notmatch '<details><summary>Coverage</summary>') {
            $errors += 'coverage frontmatter declared but body has no Coverage block'
        }
    }
    return $errors
}

function Parse-Envelope {
    param([string]$Text)
    $artifacts = @()
    $indexPatches = @()
    $indexWrites = @()
    $coverageSummary = $null
    $unclassified = @()
    $auditNotes = @()
    $deferredAdd = @()
    $deferredClear = @()

    $start = $Text.IndexOf('=== KB-INDEXER OUTPUT v1 ===')
    $end   = $Text.IndexOf('=== END KB-INDEXER OUTPUT ===')
    if ($start -lt 0 -or $end -lt 0 -or $end -le $start) {
        return [pscustomobject]@{
            ok = $false; error = 'envelope not found'
            artifacts = @(); indexPatches = @(); indexWrites = @()
            coverageSummary = $null; unclassified = @(); auditNotes = @()
            deferredAdd = @(); deferredClear = @()
        }
    }
    $body = $Text.Substring($start, $end - $start)

    $rxArtifact   = '(?ms)^=== ARTIFACT: (?<path>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END ARTIFACT ==='
    $rxIdxPatch   = '(?ms)^=== INDEX-PATCH: (?<path>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END INDEX-PATCH ==='
    $rxIdxWrite   = '(?ms)^=== INDEX-WRITE: (?<path>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END INDEX-WRITE ==='
    $rxCoverage   = '(?ms)^=== COVERAGE-SUMMARY ===\r?\n(?<body>.*?)\r?\n=== END COVERAGE-SUMMARY ==='
    $rxUnclass    = '(?ms)^=== UNCLASSIFIED-FILE: (?<path>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END UNCLASSIFIED-FILE ==='
    $rxAudit      = '(?ms)^=== AUDIT-NOTE ===\r?\n(?<body>.*?)\r?\n=== END AUDIT-NOTE ==='
    $rxDefAdd     = '(?ms)^=== DEFERRED-COVERAGE: (?<area>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END DEFERRED-COVERAGE ==='
    $rxDefClear   = '(?ms)^=== DEFERRED-COVERAGE-CLEAR: (?<area>[^\r\n=]+?) ===\r?\n(?<body>.*?)\r?\n=== END DEFERRED-COVERAGE-CLEAR ==='

    foreach ($m in [regex]::Matches($body, $rxArtifact)) {
        $artifacts += [pscustomobject]@{ path = $m.Groups['path'].Value.Trim(); body = $m.Groups['body'].Value }
    }
    foreach ($m in [regex]::Matches($body, $rxIdxPatch)) {
        $indexPatches += [pscustomobject]@{ path = $m.Groups['path'].Value.Trim(); body = $m.Groups['body'].Value }
    }
    foreach ($m in [regex]::Matches($body, $rxIdxWrite)) {
        $indexWrites += [pscustomobject]@{ path = $m.Groups['path'].Value.Trim(); body = $m.Groups['body'].Value }
    }
    $covMatch = [regex]::Match($body, $rxCoverage)
    if ($covMatch.Success) {
        try { $coverageSummary = $covMatch.Groups['body'].Value | ConvertFrom-Json -Depth 10 } catch { }
    }
    foreach ($m in [regex]::Matches($body, $rxUnclass)) {
        $unclassified += [pscustomobject]@{ path = $m.Groups['path'].Value.Trim(); reason = $m.Groups['body'].Value.Trim() }
    }
    foreach ($m in [regex]::Matches($body, $rxAudit)) {
        $auditNotes += $m.Groups['body'].Value.Trim()
    }
    foreach ($m in [regex]::Matches($body, $rxDefAdd)) {
        $deferredAdd += [pscustomobject]@{ area = $m.Groups['area'].Value.Trim(); body = $m.Groups['body'].Value }
    }
    foreach ($m in [regex]::Matches($body, $rxDefClear)) {
        $deferredClear += [pscustomobject]@{ area = $m.Groups['area'].Value.Trim(); body = $m.Groups['body'].Value }
    }

    return [pscustomobject]@{
        ok = $true
        artifacts = $artifacts
        indexPatches = $indexPatches
        indexWrites = $indexWrites
        coverageSummary = $coverageSummary
        unclassified = $unclassified
        auditNotes = $auditNotes
        deferredAdd = $deferredAdd
        deferredClear = $deferredClear
    }
}

function Write-Artifact {
    param([string]$RelPath, [string]$Body)
    $full = Join-Path $KbRoot $RelPath
    $dir = Split-Path -Parent $full
    [void](New-Item -ItemType Directory -Force -Path $dir)
    $normalized = $Body -replace "`r`n", "`n"
    if (-not $normalized.EndsWith("`n")) { $normalized += "`n" }
    [System.IO.File]::WriteAllText($full, $normalized, [System.Text.UTF8Encoding]::new($false))
}

function Apply-IndexPatch {
    param([string]$RelPath, [string]$Body)
    if ($RelPath -notin $KnownIndexFiles) {
        return @("unknown index file: $RelPath")
    }
    $full = Join-Path $KbRoot $RelPath
    $existing = @()
    if (Test-Path $full) {
        $loaded = Read-JsonFile $full
        if ($loaded -is [System.Collections.IEnumerable] -and $loaded -isnot [string]) {
            $existing = @($loaded)
        } else {
            $existing = @()
        }
    }
    $patch = $null
    try { $patch = $Body | ConvertFrom-Json -Depth 100 } catch { return @("patch JSON parse failed: $($_.Exception.Message)") }
    if (-not $patch.op) { return @('patch missing op') }
    $errors = @()
    switch ($patch.op) {
        'upsert' {
            if (-not $patch.entry) { return @('upsert missing entry') }
            if (-not $patch.entry.id) { return @('upsert entry missing id') }
            $id = [string]$patch.entry.id
            $existing = @($existing | Where-Object { [string]$_.id -ne $id })
            $existing += $patch.entry
        }
        'delete' {
            if (-not $patch.id) { return @('delete missing id') }
            $existing = @($existing | Where-Object { [string]$_.id -ne [string]$patch.id })
        }
        'update' {
            if (-not $patch.id) { return @('update missing id') }
            $found = $false
            $newList = @()
            foreach ($e in $existing) {
                if ([string]$e.id -eq [string]$patch.id) {
                    $found = $true
                    $merged = $e.PSObject.Copy()
                    if ($patch.patch) {
                        foreach ($prop in $patch.patch.PSObject.Properties) {
                            $merged | Add-Member -NotePropertyName $prop.Name -NotePropertyValue $prop.Value -Force
                        }
                    }
                    $newList += $merged
                } else { $newList += $e }
            }
            if (-not $found) { $errors += "update target id '$($patch.id)' not in index" }
            $existing = $newList
        }
        default { $errors += "unknown op: $($patch.op)" }
    }
    if ($errors.Count -eq 0) {
        Write-JsonFile -Path $full -Data $existing
    }
    return $errors
}

function Apply-IndexWrite {
    param([string]$RelPath, [string]$Body)
    if ($RelPath -notin $KnownIndexWriteFiles) {
        return @("unknown index-write file: $RelPath")
    }
    try { $obj = $Body | ConvertFrom-Json -Depth 100 } catch { return @("JSON parse failed: $($_.Exception.Message)") }
    $full = Join-Path $KbRoot $RelPath
    Write-JsonFile -Path $full -Data $obj
    return @()
}

function Op-ApplyFences {
    param($M)
    $text = if ($M.agentOutput) { [string]$M.agentOutput }
            elseif ($M.agentOutputFile -and (Test-Path $M.agentOutputFile)) {
                [System.IO.File]::ReadAllText($M.agentOutputFile, [System.Text.UTF8Encoding]::new($false))
            } else { Exit-WithError 'agentOutput or agentOutputFile required' }
    $currentSha = if ($M.currentSha) { [string]$M.currentSha } else { '' }

    $env = Parse-Envelope -Text $text
    if (-not $env.ok) { Exit-WithError 'envelope not found in agent output' }

    $artifactResults = @()
    $gateFailures = @()
    foreach ($a in $env.artifacts) {
        $errors = Validate-Artifact -Path $a.path -Body $a.body -CurrentSha $currentSha
        if ($errors.Count -eq 0) {
            try { Write-Artifact -RelPath $a.path -Body $a.body } catch { $errors += "write failed: $($_.Exception.Message)" }
        }
        $artifactResults += [pscustomobject]@{ path = $a.path; ok = ($errors.Count -eq 0); errors = $errors }
        if ($errors.Count -gt 0) { $gateFailures += "artifact $($a.path): $($errors -join '; ')" }
    }

    $patchResults = @()
    foreach ($p in $env.indexPatches) {
        $errors = Apply-IndexPatch -RelPath $p.path -Body $p.body
        $patchResults += [pscustomobject]@{ path = $p.path; ok = ($errors.Count -eq 0); errors = $errors }
        if ($errors.Count -gt 0) { $gateFailures += "patch $($p.path): $($errors -join '; ')" }
    }

    $writeResults = @()
    foreach ($w in $env.indexWrites) {
        $errors = Apply-IndexWrite -RelPath $w.path -Body $w.body
        $writeResults += [pscustomobject]@{ path = $w.path; ok = ($errors.Count -eq 0); errors = $errors }
        if ($errors.Count -gt 0) { $gateFailures += "index-write $($w.path): $($errors -join '; ')" }
    }

    if ($env.unclassified.Count -gt 0) {
        $lines = @($env.unclassified | ForEach-Object { "$($_.path) — $($_.reason)" })
        Append-AuditEntry -Event 'unclassified files' -Lines $lines
    }
    if ($env.auditNotes.Count -gt 0) {
        Append-AuditEntry -Event 'agent audit notes' -Lines $env.auditNotes
    }

    $deferredResults = @()
    if ($env.deferredAdd.Count -gt 0 -or $env.deferredClear.Count -gt 0) {
        $today = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')
        $d = Read-Deferred
        $perArea = @{}
        foreach ($a in $env.deferredAdd) {
            $files = @()
            try {
                $obj = $a.body | ConvertFrom-Json -Depth 10
                if ($obj.files) { $files = @($obj.files) }
            } catch {
                $gateFailures += "deferred-coverage $($a.area): JSON parse failed: $($_.Exception.Message)"
                continue
            }
            $sha = if ($currentSha) { $currentSha } else { '' }
            $added = Merge-DeferredFiles -Deferred $d -Area $a.area -NewFiles $files -DeferredAt $today -DeferredAtSha $sha
            if (-not $perArea.ContainsKey($a.area)) { $perArea[$a.area] = [pscustomobject]@{ area = $a.area; added = 0; cleared = 0 } }
            $perArea[$a.area].added += $added
        }
        foreach ($c in $env.deferredClear) {
            $paths = @()
            try {
                $obj = $c.body | ConvertFrom-Json -Depth 10
                if ($obj.paths) { $paths = @($obj.paths | ForEach-Object { [string]$_ }) }
            } catch {
                $gateFailures += "deferred-coverage-clear $($c.area): JSON parse failed: $($_.Exception.Message)"
                continue
            }
            $cleared = Clear-DeferredFiles -Deferred $d -Area $c.area -Paths $paths
            if (-not $perArea.ContainsKey($c.area)) { $perArea[$c.area] = [pscustomobject]@{ area = $c.area; added = 0; cleared = 0 } }
            $perArea[$c.area].cleared += $cleared
        }
        Save-Deferred -D $d
        foreach ($k in $perArea.Keys) { $deferredResults += $perArea[$k] }
        $logLines = @()
        foreach ($r in $deferredResults) {
            if ($r.added -gt 0)   { $logLines += "$($r.area): +$($r.added) deferred" }
            if ($r.cleared -gt 0) { $logLines += "$($r.area): -$($r.cleared) cleared" }
        }
        if ($logLines.Count -gt 0) {
            Append-AuditEntry -Event 'deferred-coverage queue updated' -Lines $logLines
        }
    }

    return [pscustomobject]@{
        ok = ($gateFailures.Count -eq 0)
        artifacts = $artifactResults
        indexPatches = $patchResults
        indexWrites = $writeResults
        coverageSummary = $env.coverageSummary
        unclassified = $env.unclassified
        auditNotes = $env.auditNotes
        deferred = $deferredResults
        gateFailures = $gateFailures
    }
}

# ---------- dispatch ----------

$m = Read-StdinJson
if (-not $m.op) { Exit-WithError 'op required' }

$result = switch ([string]$m.op) {
    'init'                { Op-Init }
    'get-progress'        { Op-GetProgress }
    'set-step'            { Op-SetStep   -M $m }
    'get-cursor'          { Op-GetCursor -M $m }
    'set-cursor'          { Op-SetCursor -M $m }
    'apply-fences'        { Op-ApplyFences -M $m }
    'summary'             { Op-Summary }
    'append-audit'        { Op-AppendAudit -M $m }
    'get-deferred'        { Op-GetDeferred -M $m }
    'set-deferred-area'   { Op-SetDeferredArea -M $m }
    default               { Exit-WithError "unknown op: $($m.op)" }
}

Write-JsonOutput -InputObject $result
