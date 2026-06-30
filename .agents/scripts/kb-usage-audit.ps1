<#
kb-usage-audit.ps1 — Knowledge-base *consultation* audit.

Parses Claude Code session transcripts and reports how often the knowledge
base under `.agents/knowledge-base/` is actually consulted during work, as
distinct from being built/refreshed. Authoritative + retroactive: it reads the
real tool-call records, so it is immune to the injected CLAUDE.md / skill-list
prose that mentions "knowledge-base" / "/kb-ask" in every session.

Transcripts live OUTSIDE the repo, under the user's home:
  $HOME/.claude/projects/<encoded-cwd>/<session-uuid>.jsonl
  $HOME/.claude/projects/<encoded-cwd>/<session-uuid>/subagents/agent-*.jsonl
Each top-level *.jsonl is one session; subagent activity is attributed to the
parent session (the folder named after the parent uuid).

Classification per session:
  maintenance — ran /kb-build|/kb-refresh|/kb-status|kb-coverage*|kb-fetch*|
                kb-state|kb-audit-citations, or spawned a build indexer agent
                (kb-architect|kb-historian|kb-github-curator|kb-issue-detector).
  consulted   — invoked /kb-ask|/kb-issues, spawned kb-research, ran kb-search,
                OR (in a NON-maintenance session) read/searched a KB file.
  none        — neither.

Usage:
  pwsh -NoProfile -File .agents/scripts/kb-usage-audit.ps1
  pwsh -NoProfile -File .agents/scripts/kb-usage-audit.ps1 -Json
  pwsh -NoProfile -File .agents/scripts/kb-usage-audit.ps1 -Since 2026-04-01
  pwsh -NoProfile -File .agents/scripts/kb-usage-audit.ps1 -TranscriptDir <path>

Default output is a human-readable text report. -Json emits a single JSON
object (consumed by the /kb-status skill). Nothing is written to disk.
#>

[CmdletBinding()]
param(
    [string]$TranscriptDir,
    [string]$Since,
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

# --- Resolve transcript directory -------------------------------------------
if (-not $TranscriptDir) {
    # Claude Code stores each project's transcripts under ~/.claude/projects/<slug>,
    # where <slug> is the project's working directory with ':' '\' '/' '.' each
    # replaced by '-'. Derive it from the current location so this isn't pinned to
    # one machine's clone path; pass -TranscriptDir to override.
    $home1 = [Environment]::GetFolderPath('UserProfile')
    $slug  = (Get-Location).Path -replace '[:\\/.]', '-'
    $TranscriptDir = Join-Path $home1 ".claude/projects/$slug"
}
if (-not (Test-Path -LiteralPath $TranscriptDir)) {
    [Console]::Error.WriteLine("kb-usage-audit: transcript dir not found: $TranscriptDir")
    exit 1
}

$sinceDt = $null
if ($Since) {
    try { $sinceDt = [datetime]::Parse($Since).ToUniversalTime() }
    catch { [Console]::Error.WriteLine("kb-usage-audit: bad -Since '$Since'"); exit 1 }
}

# --- Classification token sets ----------------------------------------------
$maintSkills = @('kb-build','kb-refresh','kb-status')
$maintAgents = @('kb-architect','kb-historian','kb-github-curator','kb-issue-detector')

function Get-Area {
    param([string]$path)
    if (-not $path) { return $null }
    $p = $path -replace '\\','/'
    if ($p -match 'knowledge-base/(areas/[^/"'']+)') { return $Matches[1] }
    if ($p -match 'knowledge-base/(architecture|conventions|history|detected-issues|github)') { return 'knowledge-base/' + $Matches[1] }
    if ($p -match 'knowledge-base/(glossary|README)') { return 'knowledge-base/' + $Matches[1] }
    return 'knowledge-base/(other)'
}

function Test-IsKbPath {
    param([string]$path)
    if (-not $path) { return $false }
    $p = $path -replace '\\','/'
    if ($p -notmatch 'knowledge-base') { return $false }
    if ($p -match 'knowledge-base/state') { return $false }  # build state, not consultation
    return $true
}

# --- Precompiled extraction regexes -----------------------------------------
# We scan the raw transcript text rather than ConvertFrom-Json each line: at
# hundreds of MB across all sessions + subagents, full JSON parsing took many
# minutes. These patterns key off the literal tool_use block shape, so injected
# CLAUDE.md / skill-list prose (which lives in user/system records that contain
# no `"type":"tool_use"`) is never matched.
$ro = [System.Text.RegularExpressions.RegexOptions]::Compiled
$reTs        = [regex]::new('"timestamp":"([^"]+)"', $ro)
$reSkill     = [regex]::new('"name":"Skill","input":\{"skill":"(kb-[A-Za-z]+)"', $ro)
$reSubagent  = [regex]::new('"subagent_type":"(kb-[A-Za-z-]+)"', $ro)
$reReadKb    = [regex]::new('"name":"Read","input":\{"file_path":"([^"]*knowledge-base[^"]*)"', $ro)
$rePathKb    = [regex]::new('"(?:path|pattern)":"([^"]*knowledge-base[^"]*)"', $ro)   # Grep/Glob over KB
$reCmd       = [regex]::new('"name":"(?:Bash|PowerShell)"[^}]*?"command":"((?:[^"\\]|\\.)*)"', $ro)

# --- Per-session accumulator -------------------------------------------------
# sessionId -> ordered hashtable of signals
$sessions = @{}

function Get-Session {
    param([string]$id)
    if (-not $sessions.ContainsKey($id)) {
        $sessions[$id] = [pscustomobject]@{
            id           = $id
            isMaintenance = $false
            hasAsk       = $false   # kb-ask / kb-issues skill
            hasResearch  = $false   # kb-research subagent
            hasSearch    = $false   # kb-search.ps1
            hasRead      = $false   # KB file read/grep/glob
            areas        = @{}      # area -> count (KB reads)
            firstConsult = $null    # earliest consult timestamp (datetime)
            lastConsult  = $null
        }
    }
    return $sessions[$id]
}

function Add-Area {
    param($sess, [string]$area)
    if (-not $area) { return }
    if ($sess.areas.ContainsKey($area)) { $sess.areas[$area]++ } else { $sess.areas[$area] = 1 }
}

function Stamp-Consult {
    param($sess, $ts)
    if (-not $ts) { return }
    if ($null -eq $sess.firstConsult -or $ts -lt $sess.firstConsult) { $sess.firstConsult = $ts }
    if ($null -eq $sess.lastConsult  -or $ts -gt $sess.lastConsult)  { $sess.lastConsult  = $ts }
}

# --- Enumerate sessions ------------------------------------------------------
$topFiles = @(Get-ChildItem -LiteralPath $TranscriptDir -Filter '*.jsonl' -File)
if ($sinceDt) {
    $topFiles = @($topFiles | Where-Object { $_.LastWriteTimeUtc -ge $sinceDt })
}
$totalSessions = $topFiles.Count

foreach ($top in $topFiles) {
    $sid = [System.IO.Path]::GetFileNameWithoutExtension($top.Name)
    [void](Get-Session $sid)   # ensure session exists even with no tool calls

    # The session's transcript = the top-level file + any subagent transcripts.
    $files = New-Object System.Collections.Generic.List[string]
    $files.Add($top.FullName)
    $subDir = Join-Path $TranscriptDir (Join-Path $sid 'subagents')
    if (Test-Path -LiteralPath $subDir) {
        foreach ($f in Get-ChildItem -LiteralPath $subDir -Filter '*.jsonl' -File) { $files.Add($f.FullName) }
    }

    $sess = Get-Session $sid
    foreach ($file in $files) {
        foreach ($line in [System.IO.File]::ReadLines($file)) {
            # Fast gates: only assistant tool-call lines that mention the KB.
            if ($line.IndexOf('"type":"tool_use"', [StringComparison]::Ordinal) -lt 0) { continue }
            $hasKbWord = $line.IndexOf('knowledge-base', [StringComparison]::Ordinal) -ge 0
            $hasKbDash = $line.IndexOf('kb-', [StringComparison]::Ordinal) -ge 0
            if (-not ($hasKbWord -or $hasKbDash)) { continue }

            $ts = $null
            $mTs = $reTs.Match($line)
            if ($mTs.Success) { try { $ts = ([datetime]$mTs.Groups[1].Value).ToUniversalTime() } catch { $ts = $null } }

            if ($hasKbDash) {
                foreach ($m in $reSkill.Matches($line)) {
                    $sk = $m.Groups[1].Value
                    if ($maintSkills -contains $sk -or $sk -like 'kb-coverage*' -or $sk -like 'kb-fetch*') { $sess.isMaintenance = $true }
                    elseif ($sk -eq 'kb-ask' -or $sk -eq 'kb-issues') { $sess.hasAsk = $true; Stamp-Consult $sess $ts }
                }
                foreach ($m in $reSubagent.Matches($line)) {
                    $st = $m.Groups[1].Value
                    if ($maintAgents -contains $st) { $sess.isMaintenance = $true }
                    elseif ($st -eq 'kb-research') { $sess.hasResearch = $true; Stamp-Consult $sess $ts }
                }
                foreach ($m in $reCmd.Matches($line)) {
                    $cmd = $m.Groups[1].Value
                    if ($cmd -match 'kb-(fetch|coverage|state|audit-citations)') { $sess.isMaintenance = $true }
                    elseif ($cmd -match 'kb-search') { $sess.hasSearch = $true; Stamp-Consult $sess $ts }
                }
            }

            if ($hasKbWord) {
                foreach ($m in $reReadKb.Matches($line)) {
                    $fp = $m.Groups[1].Value
                    if (Test-IsKbPath $fp) { $sess.hasRead = $true; Add-Area $sess (Get-Area $fp); Stamp-Consult $sess $ts }
                }
                foreach ($m in $rePathKb.Matches($line)) {
                    $pp = $m.Groups[1].Value
                    if (Test-IsKbPath $pp) { $sess.hasRead = $true; Add-Area $sess (Get-Area $pp); Stamp-Consult $sess $ts }
                }
            }
        }
    }
}

# --- Derive per-session verdict ---------------------------------------------
$consultList = New-Object System.Collections.Generic.List[object]
$maintCount  = 0
$mechSkill = 0; $mechResearch = 0; $mechSearch = 0; $mechRead = 0
$areaTotals = @{}
$monthTotals = @{}   # yyyy-MM -> consulting-session count

foreach ($sess in $sessions.Values) {
    if ($sess.isMaintenance) { $maintCount++ }

    # Consultation: unambiguous signals always count; bare KB reads count only
    # when the session is not a build/refresh session (else they're build reads).
    $consulted = $sess.hasAsk -or $sess.hasResearch -or $sess.hasSearch -or ((-not $sess.isMaintenance) -and $sess.hasRead)
    if (-not $consulted) { continue }

    $consultList.Add($sess)
    if ($sess.hasAsk)      { $mechSkill++ }
    if ($sess.hasResearch) { $mechResearch++ }
    if ($sess.hasSearch)   { $mechSearch++ }
    if ((-not $sess.isMaintenance) -and $sess.hasRead) { $mechRead++ }

    if (-not $sess.isMaintenance) {
        foreach ($k in $sess.areas.Keys) {
            if ($areaTotals.ContainsKey($k)) { $areaTotals[$k] += $sess.areas[$k] } else { $areaTotals[$k] = $sess.areas[$k] }
        }
    }
    if ($sess.firstConsult) {
        $mk = $sess.firstConsult.ToString('yyyy-MM')
        if ($monthTotals.ContainsKey($mk)) { $monthTotals[$mk]++ } else { $monthTotals[$mk] = 1 }
    }
}

$consultCount  = $consultList.Count
$nonMaint      = $totalSessions - $maintCount
$pctAll        = if ($totalSessions -gt 0) { [math]::Round(100.0 * $consultCount / $totalSessions, 1) } else { 0 }
$pctNonMaint   = if ($nonMaint -gt 0)      { [math]::Round(100.0 * $consultCount / $nonMaint, 1) }      else { 0 }

$lastConsultDt = $null
foreach ($s in $consultList) { if ($s.lastConsult -and ($null -eq $lastConsultDt -or $s.lastConsult -gt $lastConsultDt)) { $lastConsultDt = $s.lastConsult } }

$topAreas = @($areaTotals.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 12)
$months   = @($monthTotals.GetEnumerator() | Sort-Object Name)

# --- Emit --------------------------------------------------------------------
if ($Json) {
    $obj = [ordered]@{
        transcriptDir   = $TranscriptDir
        since           = if ($sinceDt) { $sinceDt.ToString('yyyy-MM-ddTHH:mm:ssZ') } else { $null }
        totalSessions   = $totalSessions
        maintenance     = $maintCount
        nonMaintenance  = $nonMaint
        consulted       = $consultCount
        consultedPctOfAll          = $pctAll
        consultedPctOfNonMaint     = $pctNonMaint
        mechanism       = [ordered]@{ askSkill = $mechSkill; research = $mechResearch; search = $mechSearch; directRead = $mechRead }
        lastConsult     = if ($lastConsultDt) { $lastConsultDt.ToString('yyyy-MM-ddTHH:mm:ssZ') } else { $null }
        topAreas        = @($topAreas | ForEach-Object { [ordered]@{ area = $_.Key; reads = $_.Value } })
        byMonth         = @($months   | ForEach-Object { [ordered]@{ month = $_.Name; sessions = $_.Value } })
        consultingSessions = @($consultList | Sort-Object { $_.lastConsult } -Descending | ForEach-Object {
            [ordered]@{
                id          = $_.id
                lastConsult = if ($_.lastConsult) { $_.lastConsult.ToString('yyyy-MM-ddTHH:mm:ssZ') } else { $null }
                askSkill    = $_.hasAsk
                research    = $_.hasResearch
                search      = $_.hasSearch
                directRead  = $_.hasRead
                maintenance = $_.isMaintenance
            }
        })
    }
    $obj | ConvertTo-Json -Depth 6
    exit 0
}

# Text report
$nl = [Environment]::NewLine
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('# KB usage audit')
[void]$sb.AppendLine('')
[void]$sb.AppendLine("Transcript dir: $TranscriptDir")
if ($sinceDt) { [void]$sb.AppendLine("Since: " + $sinceDt.ToString('yyyy-MM-dd')) }
[void]$sb.AppendLine('')
[void]$sb.AppendLine("Total sessions          : $totalSessions")
[void]$sb.AppendLine("Maintenance sessions    : $maintCount  (kb-build / kb-refresh / indexers)")
[void]$sb.AppendLine("Non-maintenance sessions: $nonMaint")
[void]$sb.AppendLine("Consulted the KB        : $consultCount   ($pctNonMaint% of non-maintenance, $pctAll% of all)")
$lc = if ($lastConsultDt) { $lastConsultDt.ToString('yyyy-MM-dd') } else { '(never)' }
[void]$sb.AppendLine("Last consultation       : $lc")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Mechanism (consulting sessions)')
[void]$sb.AppendLine("  /kb-ask | /kb-issues : $mechSkill")
[void]$sb.AppendLine("  kb-research subagent : $mechResearch")
[void]$sb.AppendLine("  kb-search.ps1        : $mechSearch")
[void]$sb.AppendLine("  direct KB file read  : $mechRead")
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Top consulted areas (KB reads, non-maintenance)')
if ($topAreas.Count -eq 0) { [void]$sb.AppendLine('  (none)') }
else { foreach ($a in $topAreas) { [void]$sb.AppendLine(("  {0,-40} {1}" -f $a.Key, $a.Value)) } }
[void]$sb.AppendLine('')
[void]$sb.AppendLine('## Consulting sessions by month')
if ($months.Count -eq 0) { [void]$sb.AppendLine('  (none)') }
else { foreach ($m in $months) { [void]$sb.AppendLine(("  {0}  {1}" -f $m.Name, $m.Value)) } }
Write-Output $sb.ToString()
