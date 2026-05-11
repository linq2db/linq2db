<#
Fetch the failed-test list (with first error per failure) from an Azure DevOps
build on the linq2db project. Used when reviewing PR CI failures: get from
"build N failed" to "exact failing test names + per-provider error messages"
without doing the timeline -> log-id -> Invoke-WebRequest -> Grep dance by hand.

The Azure DevOps `dev.azure.com/linq2db` build API is publicly readable
(no auth needed). The script hits:
  1. /timeline                            -> list failed Task records
  2. /logs/<id> for each failing test job -> raw log
  3. parse log for `Failed <name>... Error Message:` blocks

Invoke directly via the PowerShell tool (preferred), NOT wrapped in Bash:

    .claude\scripts\azp-build-failures.ps1 -BuildId 20250
    .claude\scripts\azp-build-failures.ps1 -BuildId 20250 -WriteDir .build/.claude/azp-20250

Output: single JSON document on stdout with per-task failures and on-disk
log paths. Logs are persisted under -WriteDir for follow-up Read / Grep.
#>

param(
    [Parameter(Mandatory)][int]$BuildId,
    [string]$WriteDir,
    [string]$Org = 'linq2db',
    [string]$Project = '0dcc414b-ea54-451e-a54f-d63f05367c4b',
    [int]$MaxFailuresPerTask = 20
)

$global:ScriptBaseName = 'azp-build-failures'
. "$PSScriptRoot/_shared.ps1"

if (-not $WriteDir) {
    $WriteDir = ".build/.claude/azp-$BuildId"
}
New-Item -ItemType Directory -Force -Path $WriteDir | Out-Null

$baseUrl = "https://dev.azure.com/$Org/$Project/_apis/build/builds/$BuildId"

# 1. Timeline -> failed Task records that look like test jobs.
try {
    $timeline = Invoke-RestMethod -Uri "$baseUrl/timeline?api-version=7.0"
}
catch {
    Exit-WithError "failed to fetch timeline for build ${BuildId}: $_"
}

$failedTasks = @($timeline.records |
    Where-Object { $_.type -eq 'Task' -and $_.result -eq 'failed' -and $_.name -like 'Tests *' -and $_.log })

if ($failedTasks.Count -eq 0) {
    @{
        buildId         = $BuildId
        logsDir         = $WriteDir
        failedTaskCount = 0
        tasks           = @()
    } | ConvertTo-Json -Depth 6 -Compress:$false
    exit 0
}

# 2. Download each failing task's raw log to disk + parse for failures.
function ConvertTo-Slug {
    param([string]$Name)
    $s = $Name.ToLowerInvariant()
    $s = $s -replace '[^a-z0-9]+', '-'
    return $s.Trim('-')
}

$tasks = foreach ($t in $failedTasks) {
    $logUrl  = $t.log.url
    $slug    = ConvertTo-Slug -Name $t.name
    $logPath = Join-Path $WriteDir "$slug.log"

    try {
        Invoke-WebRequest -Uri $logUrl -OutFile $logPath -UseBasicParsing | Out-Null
    }
    catch {
        [Console]::Error.WriteLine("azp-build-failures: log fetch failed for '$($t.name)': $_")
        [pscustomobject]@{
            name     = $t.name
            logUrl   = $logUrl
            logPath  = $null
            failures = @()
            error    = "$_"
        }
        continue
    }

    # Parse `Failed <TestName>(...) [Nms]` + next `Error Message:` line(s).
    $lines = Get-Content -LiteralPath $logPath
    $failures = @()
    for ($i = 0; $i -lt $lines.Count -and $failures.Count -lt $MaxFailuresPerTask; $i++) {
        $line = $lines[$i]
        $m = [regex]::Match($line, '^\s*\S+\s+Failed\s+(?<test>\S.*?)\s+\[\d+(\.\d+)?\s*m?s\]\s*$')
        if (-not $m.Success) { continue }

        $testName = $m.Groups['test'].Value.Trim()
        $errMessage = $null

        # Scan up to next ~5 lines for "Error Message:" header, then take the
        # next non-empty line as the headline error.
        for ($j = $i + 1; $j -lt [Math]::Min($i + 6, $lines.Count); $j++) {
            if ($lines[$j] -match 'Error Message:\s*$') {
                for ($k = $j + 1; $k -lt [Math]::Min($j + 3, $lines.Count); $k++) {
                    $candidate = $lines[$k] -replace '^\s*\S+Z\s+', ''
                    $candidate = $candidate.Trim()
                    if ($candidate) { $errMessage = $candidate; break }
                }
                break
            }
        }

        $failures += [pscustomobject]@{
            test         = $testName
            errorMessage = $errMessage
        }
    }

    [pscustomobject]@{
        name     = $t.name
        logUrl   = $logUrl
        logPath  = $logPath
        failures = $failures
    }
}

# 3. Emit one JSON result.
$result = [ordered]@{
    buildId         = $BuildId
    logsDir         = $WriteDir
    failedTaskCount = $failedTasks.Count
    tasks           = @($tasks)
}

$result | ConvertTo-Json -Depth 6
