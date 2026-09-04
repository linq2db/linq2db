<#
dispatch-github-tests.ps1 - trigger the GitHub Actions `tests` workflow from Azure Pipelines.

Verifies the run appeared rather than trusting the POST: the dispatch endpoint answers 204 with no
body, so a wrong ref, a revoked token or a workflow rename all look identical to success from the
caller's side. Exits non-zero when no run shows up, so the Azure job fails loudly instead of leaving
the impression that GitHub is testing something.

`ref` must be a branch or tag in the target repo. A fork PR has only refs/pull/<n>/head, which the
endpoint rejects with 422 "No ref found" - callers must skip fork PRs rather than pass that.

Auth: $Env:GITHUB_TOKEN, a PAT with Actions: Read and write (fine-grained) on the target repo.

Exit codes: 0 = run created, 1 = dispatch rejected or no run appeared, 2 = bad arguments.
#>

[CmdletBinding()]
param(
    # AllowEmptyString, or parameter binding rejects an empty $(dispatch_ref) with "Cannot bind
    # argument to parameter 'Ref'" before the validation below can say what is actually wrong.
    [Parameter(Mandatory)][AllowEmptyString()][string] $Ref,
    [Parameter(Mandatory)][string] $Surface,
    [string] $BaselinesBranch = '',
    [string] $PrId            = '',
    [bool]   $FullRun         = $false,
    [string] $Repo            = 'linq2db/linq2db',
    [string] $Workflow        = 'tests.yml',
    [int]    $VerifyTimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'

if (-not $Env:GITHUB_TOKEN) {
    Write-Host "##vso[task.logissue type=error]GITHUB_TOKEN is not set - it carries the dispatch PAT"
    exit 2
}

# Validated here rather than left to the API, which answers every bad ref with the same 422 and no
# indication of which of these it was.
if ([string]::IsNullOrWhiteSpace($Ref)) {
    Write-Host "##vso[task.logissue type=error]-Ref is empty - on a non-PR run it should fall back to Build.SourceBranch"
    exit 2
}
if ($Ref -like 'refs/pull/*') {
    Write-Host "##vso[task.logissue type=error]-Ref is '$Ref'. workflow_dispatch takes a branch or tag; a pull ref is rejected. Fork PRs must be skipped by the caller, and a PR build must pass System.PullRequest.SourceBranch, not Build.SourceBranch (which is refs/pull/<n>/merge)."
    exit 2
}

# Azure's SourceBranch carries the refs/ prefix; the API wants the short name.
$branch = $Ref -replace '^refs/(heads|tags)/', ''

$headers = @{
    Authorization          = "Bearer $Env:GITHUB_TOKEN"
    Accept                 = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
}
$api = "https://api.github.com/repos/$Repo/actions/workflows/$Workflow"

# Every input is a string on the wire, including the boolean.
$inputs = @{ surface = $Surface; full_run = $FullRun.ToString().ToLowerInvariant() }
if ($BaselinesBranch) { $inputs.baselines_branch = $BaselinesBranch }
if ($PrId)            { $inputs.pr               = $PrId }

$since = (Get-Date).ToUniversalTime().AddSeconds(-30)
Write-Host "Dispatching $Workflow on ${branch}: surface=$Surface full_run=$FullRun baselines_branch='$BaselinesBranch' pr='$PrId'"

try {
    Invoke-RestMethod -Method Post -Uri "$api/dispatches" -Headers $headers `
        -ContentType 'application/json' `
        -Body (@{ ref = $branch; inputs = $inputs } | ConvertTo-Json -Compress) | Out-Null
}
catch {
    Write-Host "##vso[task.logissue type=error]dispatch rejected: $($_.Exception.Message)"
    if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message }
    exit 1
}

# The run takes a moment to register, and the endpoint gives us no id to look it up by.
$deadline = (Get-Date).AddSeconds($VerifyTimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    try {
        $runs = Invoke-RestMethod -Uri "$api/runs?event=workflow_dispatch&branch=$branch&per_page=10" -Headers $headers
    } catch { continue }

    $run = $runs.workflow_runs | Where-Object { [datetime]::Parse($_.created_at).ToUniversalTime() -ge $since } |
        Sort-Object { [datetime]::Parse($_.created_at) } -Descending | Select-Object -First 1
    if ($run) {
        Write-Host "GitHub run: $($run.html_url)"
        Write-Host "##vso[task.setvariable variable=github_run_id;isOutput=true]$($run.id)"
        exit 0
    }
}

Write-Host "##vso[task.logissue type=error]dispatch accepted but no run appeared within ${VerifyTimeoutSeconds}s - check the workflow's triggers and the token's Actions permission"
exit 1
