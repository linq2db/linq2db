<#
publish-azure-artifacts.ps1 — push .nupkg files to the Azure Artifacts feed, tolerating the feed's
storage-quota rejection.

The feed has a fixed storage allowance. Once it is full, Azure Artifacts answers a push with
HTTP 402 (Payment Required — "Artifact cannot be uploaded because max quantity has been exceeded
or the payment instrument is invalid"). That is a feed capacity condition, not a defect in the
commit being built, but the NuGetCommand@2 task it replaces treats any non-zero push exit as a
build failure — so a full feed turned every master build red (e.g. build 22947 on af5eda3d8).

The CI feed is a convenience mirror, not a release channel (releases go to nuget.org via a separate
step), so an over-quota feed must not fail the build. Every *other* push failure still does — a
blanket `continueOnError: true` on the task would have hidden genuine publish breakage too, which
is the reason this is a script rather than one line of YAML.

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/publish-azure-artifacts.ps1 -PackagesDir <dir> -Source <url>

  -PackagesDir   directory to scan recursively for *.nupkg (required)
  -Source        NuGet v3 index URL of the target feed (required)
  -NoAzdoLogs    suppress the Azure DevOps `##vso[...]` lines, which are on by default. Use it for
                 local invocation. A switch rather than a [bool], because `pwsh -File` passes every
                 argument as a string and a [bool] parameter rejects strings outright.

Exit codes:
  0  every package pushed, or the only failures were the feed being over quota (HTTP 402)
  1  any package failed to push for any other reason
  2  invalid args / no nupkgs found
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDir,
    [Parameter(Mandatory = $true)]
    [string] $Source,
    [switch] $NoAzdoLogs
)

$ErrorActionPreference = 'Continue'

if (-not (Test-Path $PackagesDir)) {
    [Console]::Error.WriteLine("PackagesDir not found: $PackagesDir")
    exit 2
}

$pkgs = Get-ChildItem -Path $PackagesDir -Recurse -Filter '*.nupkg' -File
if (-not $pkgs -or $pkgs.Count -eq 0) {
    [Console]::Error.WriteLine("No .nupkg files found under: $PackagesDir")
    exit 2
}

$pushed      = @()
$overQuota   = @()
$failed      = @()

foreach ($p in $pkgs) {
    Write-Output "Pushing $($p.Name)"

    # 2>&1 so the 402 body, which NuGet writes to stderr, is matchable below.
    $output   = & dotnet nuget push $p.FullName --source $Source --api-key AzureArtifacts --skip-duplicate 2>&1
    $exitCode = $LASTEXITCODE
    $text     = ($output | Out-String)

    Write-Output $text

    if ($exitCode -eq 0) {
        $pushed += $p.Name
        continue
    }

    # Match the phrase and the status code independently, so a reword of either still lands. The
    # numeric form is anchored to a status-code context on purpose: a bare 402 also occurs inside
    # the DevOps activity GUID the same message carries, and matching that would silently swallow
    # an unrelated push failure.
    if ($text -match 'Payment Required' -or $text -match 'status code[^\r\n]{0,60}\b402\b') {
        $overQuota += $p.Name
    }
    else {
        $failed += $p.Name
    }
}

Write-Output ""
Write-Output ("Scanned {0} nupkg(s). Pushed: {1}  Over quota: {2}  Failed: {3}" -f $pkgs.Count, $pushed.Count, $overQuota.Count, $failed.Count)

foreach ($name in $failed) {
    $msg = "$name failed to publish to the Azure Artifacts feed"
    Write-Output "  [FAIL]  $msg"
    if (-not $NoAzdoLogs) {
        Write-Output "##vso[task.logissue type=error]$msg"
    }
}

if ($failed.Count -gt 0) { exit 1 }

if ($overQuota.Count -gt 0) {
    $msg = "Azure Artifacts feed is over its storage quota (HTTP 402); {0} package(s) were not published: {1}. Release publishing to nuget.org is unaffected. See https://aka.ms/artbilling" -f $overQuota.Count, ($overQuota -join ', ')
    Write-Output "  [WARN]  $msg"
    if (-not $NoAzdoLogs) {
        Write-Output "##vso[task.logissue type=warning]$msg"
        Write-Output "##vso[task.complete result=SucceededWithIssues;]$msg"
    }
}

exit 0
