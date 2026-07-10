<#
Download a named build step's raw log from an Azure DevOps build on the linq2db
project, to disk, for follow-up Read / Grep. Complements azp-build-failures.ps1:
that script targets *failed* test / build steps; this one fetches ANY step's log
by name — including a *succeeded* diagnostic step whose value is its stdout (e.g.
a CI probe that prints a summary block), which the failures script never surfaces.

Saves doing the timeline -> log-id -> download -> Grep dance by hand (which also
drags in a `curl … | python` compound + bare `python -c`, both prompt-prone).

The `dev.azure.com/linq2db` build API is publicly readable (no auth). The script:
  1. /timeline                 -> Task records whose name contains -StepName
  2. /logs/<id> for each match -> raw log, saved under -WriteDir

A step's log only exists once the step has started; a pending / queued step is
reported (with its state) but yields no logPath — the script does not fail on it.

Invoke directly via the PowerShell tool (preferred), NOT wrapped in Bash:

    .agents\scripts\azp-step-log.ps1 -BuildId 22116 -StepName 'RAM disk'
    .agents\scripts\azp-step-log.ps1 -BuildId 22116 -StepName 'RAM disk' -WriteDir .build/.agents/azp-22116

Output: single JSON document on stdout with the matched steps' name / state /
result and on-disk log paths. Grep the persisted log for the content you need.
#>

param(
    [Parameter(Mandatory)][int]$BuildId,
    [Parameter(Mandatory)][string]$StepName,
    [string]$WriteDir,
    [string]$Org = 'linq2db',
    [string]$Project = '0dcc414b-ea54-451e-a54f-d63f05367c4b'
)

$global:ScriptBaseName = 'azp-step-log'
. "$PSScriptRoot/_shared.ps1"

if (-not $WriteDir) {
    $WriteDir = ".build/.agents/azp-$BuildId"
}
New-Item -ItemType Directory -Force -Path $WriteDir | Out-Null

$baseUrl = "https://dev.azure.com/$Org/$Project/_apis/build/builds/$BuildId"

function ConvertTo-Slug {
    param([string]$Name)
    $s = $Name.ToLowerInvariant()
    $s = $s -replace '[^a-z0-9]+', '-'
    return $s.Trim('-')
}

try {
    $timeline = Invoke-RestMethod -Uri "$baseUrl/timeline?api-version=7.1"
}
catch {
    Exit-WithError "failed to fetch timeline for build ${BuildId}: $_"
}

$needle = $StepName.ToLowerInvariant()
$matched = @($timeline.records |
    Where-Object { $_.type -eq 'Task' -and $_.name -and $_.name.ToLowerInvariant().Contains($needle) })

if ($matched.Count -eq 0) {
    Exit-WithError "no timeline step matches '$StepName' in build ${BuildId} (step may not exist, or the build hasn't reached it)"
}

$withLog = @($matched | Where-Object { $_.log -and $_.log.url })
if ($withLog.Count -eq 0) {
    # Step(s) matched but no log yet (pending / not started). Report state, don't fail.
    Write-JsonOutput ([pscustomobject]@{
        buildId  = $BuildId
        stepName = $StepName
        logsDir  = $WriteDir
        steps    = @($matched | ForEach-Object { [pscustomobject]@{ name = $_.name; state = $_.state; result = $_.result; logPath = $null } })
        note     = 'matched step(s) have no log yet (pending / not started)'
    })
    exit 0
}

$results = @()
foreach ($rec in $withLog) {
    $slug    = ConvertTo-Slug -Name $rec.name
    $logPath = Join-Path $WriteDir "$slug.log"
    try {
        Invoke-WebRequest -Uri $rec.log.url -OutFile $logPath -UseBasicParsing | Out-Null
    }
    catch {
        Exit-WithError "log fetch failed for step '$($rec.name)': $_"
    }
    $results += [pscustomobject]@{
        name    = $rec.name
        state   = $rec.state
        result  = $rec.result
        logPath = $logPath
    }
}

Write-JsonOutput ([pscustomobject]@{
    buildId  = $BuildId
    stepName = $StepName
    logsDir  = $WriteDir
    steps    = $results
})
