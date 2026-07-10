<#
analyzer-profile-report.ps1 — parses a
`dotnet build -v:detailed -p:ReportAnalyzer=true` log produced by
`analyzer-profile-build.ps1` and reports the slowest Roslyn analyzers.

Scans the MSBuild log for `Total analyzer execution time` blocks,
attributes each block to the owning project, and produces three rankings:

  1. Top N slowest (analyzer x project) pairs - where one analyzer is
     particularly painful in one project.
  2. Top N busiest analyzers - sum of time across every project that ran them,
     so analyzers that are uniformly expensive rise to the top.
  3. Top N projects by total analyzer time - which projects dominate the build
     cost overall.

The log MUST be produced with detailed verbosity (`-v:detailed`). Normal
verbosity filters /reportanalyzer output (MessageImportance.Low) out of
the MSBuild log entirely and the report won't appear.

Project attribution strategy: for each report, scan preceding lines (no bound)
and accept the first match among, in priority order:
  1. `N:M>CoreCompile:`                                   (short target header)
  2. `N:M>Target "CoreCompile" in file ... from project "X.csproj"`  (long header)
  3. `/out:...\X.dll`                                     (csc command line)
  4. `Compilation request X`                              (build server line)
The unbounded scan is necessary because projects emit thousands of
diagnostic-info lines between the target header and the report.

Default mode: pretty tables on stdout for human consumption.
`-AsJson` mode: single JSON object to stdout (per script-authoring conventions).

Conventions: `.agents/docs/script-authoring.md`.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$LogPath,
    [int]    $Top = 10,
    [switch] $AsJson
)

$global:ScriptBaseName = 'analyzer-profile-report'
. (Join-Path $PSScriptRoot '_shared.ps1')

if (-not (Test-Path -LiteralPath $LogPath)) { Exit-WithError "Log not found: $LogPath" }

$lines = [System.IO.File]::ReadAllLines($LogPath)

# -- regexes -----------------------------------------------------------------

$reIsBuilding   = [regex]'is building "([^"]+\.csproj)" \((\d+:\d+)\)'
$reProjectStart = [regex]'Project "([^"]+\.csproj)" \((\d+:\d+)\)'
$reCoreShort    = [regex]'^\s*(\d+:\d+)>CoreCompile:'
$reCoreLong     = [regex]'^\s*(\d+:\d+)>Target "CoreCompile" in file ".*?" from project "([^"]+\.csproj)"'
$reRow          = [regex]'^\s+(<?\d+\.\d+|<0\.\d+)\s+(<?\d+)\s+(\S.*)$'
$reCompReq      = [regex]'Compilation request (\S+) \('
$reOutDll       = [regex]'/out:(?:[^ ]*\\)?([A-Za-z0-9_.]+)\.dll\b'
$reTotal        = [regex]'Total analyzer execution time: ([\d.]+) seconds'

# -- pass 1: PID -> project name --------------------------------------------

$projects = @{}
foreach ($line in $lines) {
    $m = $reIsBuilding.Match($line)
    if ($m.Success) {
        $leaf = [IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)
        $projects[$m.Groups[2].Value] = $leaf
        continue
    }
    $m = $reProjectStart.Match($line)
    if ($m.Success -and -not $projects.ContainsKey($m.Groups[2].Value)) {
        $leaf = [IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)
        $projects[$m.Groups[2].Value] = $leaf
    }
}

# -- attribution helper ------------------------------------------------------

function Resolve-Project([int]$fromIndex) {
    for ($j = $fromIndex; $j -ge 0; $j--) {
        $line = $lines[$j]
        $m = $reCoreShort.Match($line)
        if ($m.Success) {
            $id = $m.Groups[1].Value
            if ($projects.ContainsKey($id)) { return $projects[$id] }
        }
        $m = $reCoreLong.Match($line)
        if ($m.Success) { return [IO.Path]::GetFileNameWithoutExtension($m.Groups[2].Value) }
        $m = $reOutDll.Match($line)
        if ($m.Success) { return $m.Groups[1].Value }
        $m = $reCompReq.Match($line)
        if ($m.Success) { return $m.Groups[1].Value }
    }
    return 'unknown'
}

# -- pass 2: collect reports -------------------------------------------------

$pairs         = [System.Collections.Generic.List[psobject]]::new()
$projectTotals = [System.Collections.Generic.List[psobject]]::new()
$analyzerSums  = @{}
$analyzerHits  = @{}

$i = 0
while ($i -lt $lines.Length) {
    if ($lines[$i].Contains('Total analyzer execution time')) {
        $mt = $reTotal.Match($lines[$i])
        $totalS = if ($mt.Success) { [double]$mt.Groups[1].Value } else { 0.0 }
        $projectName = Resolve-Project $i
        $projectTotals.Add([pscustomobject]@{ Time = $totalS; Project = $projectName })

        # skip to first data row (after the "Time (s)" header line)
        $j = $i + 1
        while ($j -lt $lines.Length -and -not $lines[$j].Contains('Time (s)')) { $j++ }
        $j++

        $blank = 0
        while ($j -lt $lines.Length) {
            $row = $lines[$j].TrimEnd()
            if ($row.Trim() -eq '') {
                $blank++
                if ($blank -ge 3) { break }
                $j++; continue
            }
            if ($row -match '^\s*\d+:\d+>' -or $row -match 'Build (succeeded|FAILED)') { break }
            $mm = $reRow.Match($row)
            if (-not $mm.Success) { break }
            $blank = 0
            $timeStr = $mm.Groups[1].Value
            $name    = $mm.Groups[3].Value.Trim()
            $tVal = if ($timeStr.StartsWith('<')) { 0.0005 } else { [double]$timeStr }

            # Skip the assembly-level aggregate row (e.g. "Microsoft.CodeAnalysis.CSharp.CodeStyle, Version=...")
            if ($name -like '*, Version=*') { $j++; continue }

            $pairs.Add([pscustomobject]@{ Time = $tVal; Project = $projectName; Analyzer = $name })
            if (-not $analyzerSums.ContainsKey($name)) {
                $analyzerSums[$name] = 0.0
                $analyzerHits[$name] = 0
            }
            $analyzerSums[$name] += $tVal
            $analyzerHits[$name] += 1
            $j++
        }
        $i = $j
    } else {
        $i++
    }
}

# -- formatting helper -------------------------------------------------------

function Split-Analyzer([string]$n) {
    $idx = $n.IndexOf('(')
    if ($idx -ge 0) {
        $head = $n.Substring(0, $idx).Trim()
        $rules = $n.Substring($idx)
    } else {
        $head = $n.Trim()
        $rules = ''
    }
    $parts = $head -split '\.'
    [pscustomobject]@{ Short = $parts[-1]; Rules = $rules }
}

# -- rankings ----------------------------------------------------------------

$slowestPairs = $pairs |
    Sort-Object Time -Descending |
    Select-Object -First $Top |
    ForEach-Object -Begin { $rank = 0 } -Process {
        $rank++
        $sa = Split-Analyzer $_.Analyzer
        [pscustomobject]@{
            Rank     = $rank
            Time_s   = [Math]::Round($_.Time, 3)
            Project  = $_.Project
            Analyzer = $sa.Short
            Rules    = $sa.Rules
        }
    }

$busiest = $analyzerSums.Keys |
    Sort-Object { - $analyzerSums[$_] } |
    Select-Object -First $Top |
    ForEach-Object -Begin { $rank = 0 } -Process {
        $rank++
        $name  = $_
        $hits  = $analyzerHits[$name]
        $total = $analyzerSums[$name]
        $sa    = Split-Analyzer $name
        [pscustomobject]@{
            Rank     = $rank
            Total_s  = [Math]::Round($total, 3)
            Projects = $hits
            Avg_s    = [Math]::Round($total / [Math]::Max(1, $hits), 3)
            Analyzer = $sa.Short
            Rules    = $sa.Rules
        }
    }

$projectRanking = $projectTotals |
    Sort-Object Time -Descending |
    Select-Object -First $Top |
    ForEach-Object -Begin { $rank = 0 } -Process {
        $rank++
        [pscustomobject]@{
            Rank    = $rank
            Time_s  = [Math]::Round($_.Time, 3)
            Project = $_.Project
        }
    }

# -- output ------------------------------------------------------------------

if ($AsJson) {
    Write-JsonOutput @{
        ok               = $true
        action           = 'report'
        slowestPairs     = @($slowestPairs)
        busiestAnalyzers = @($busiest)
        projectTotals    = @($projectRanking)
        diagnostics      = @{
            pairRows       = $pairs.Count
            projectReports = $projectTotals.Count
            analyzers      = $analyzerSums.Count
        }
    }
    return
}

# Pretty mode: tables to stdout for human reading.
Write-Host ("Per-analyzer rows: {0}   Project reports: {1}   Distinct analyzers: {2}" -f `
    $pairs.Count, $projectTotals.Count, $analyzerSums.Count)

Write-Host ''
Write-Host ('=' * 120)
Write-Host ("TOP {0} SLOWEST  (analyzer x project)  by per-analyzer time" -f $Top)
Write-Host ('=' * 120)
$slowestPairs | Format-Table Rank, @{Name='Time(s)'; Expression='Time_s'; Alignment='right'}, Project, Analyzer, Rules -AutoSize | Out-Host

Write-Host ('=' * 120)
Write-Host ("TOP {0} BUSIEST analyzers (sum across measured projects)" -f $Top)
Write-Host ('=' * 120)
$busiest | Format-Table Rank, @{Name='Total(s)'; Expression='Total_s'; Alignment='right'}, Projects, @{Name='Avg(s)'; Expression='Avg_s'; Alignment='right'}, Analyzer, Rules -AutoSize | Out-Host

Write-Host ('=' * 120)
Write-Host ("TOP {0} projects by total analyzer time" -f $Top)
Write-Host ('=' * 120)
$projectRanking | Format-Table Rank, @{Name='Time(s)'; Expression='Time_s'; Alignment='right'}, Project -AutoSize | Out-Host
