<#
verify-nuget-sizes.ps1 — assert .nupkg sizes stay within nuget.org limits.

Surfaced after the 6.3.0 release-publish job hit HTTP 413 on
`linq2db.cli.6.3.0.nupkg` (~416 MB) at nuget.org's 250 MB upload
limit, after every other package in the publish run had already been
pushed. The publish step had no pre-flight size check; pack succeeded,
then push failed atomically too late to reroute.

Hard limit: 200 MB (project-internal ceiling — well under nuget.org's
250 MB HTTP-413 cliff, leaving 50 MB of headroom for native-asset
growth without re-tripping the publish gate).
Warn limit:  180 MB (early signal — at this size a single package is
close enough to the ceiling that the next dependency / native-asset
bump could push it over; investigate before it does).

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/verify-nuget-sizes.ps1 -PackagesDir <dir>

  -PackagesDir   directory to scan recursively for *.nupkg (required)
  -WarnMB        warn-threshold in MB (default 180)
  -FailMB        fail-threshold in MB (default 200)
  -AzdoLogs      emit Azure DevOps `##vso[task.logissue]` lines for warnings
                 and errors. Default true (the script's primary caller is
                 the AzDO publish pipeline). Disable for local invocation.

Exit codes:
  0  no packages over WarnMB and FailMB — clean
  0  some packages over WarnMB but none over FailMB — warnings only
  1  any package over FailMB — release-blocking; build should fail
  2  invalid args / no nupkgs found
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDir,
    [int]    $WarnMB   = 180,
    [int]    $FailMB   = 200,
    [bool]   $AzdoLogs = $true
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PackagesDir)) {
    [Console]::Error.WriteLine("PackagesDir not found: $PackagesDir")
    exit 2
}

$pkgs = Get-ChildItem -Path $PackagesDir -Recurse -Filter '*.nupkg' -File
if (-not $pkgs -or $pkgs.Count -eq 0) {
    [Console]::Error.WriteLine("No .nupkg files found under: $PackagesDir")
    exit 2
}

$warnBytes = [long]$WarnMB * 1MB
$failBytes = [long]$FailMB * 1MB

$warned  = @()
$failed  = @()
$ok      = @()

foreach ($p in $pkgs) {
    $sizeMB = [math]::Round($p.Length / 1MB, 1)
    $row = [pscustomobject]@{ name = $p.Name; sizeMB = $sizeMB; path = $p.FullName }
    if ($p.Length -ge $failBytes)    { $failed += $row }
    elseif ($p.Length -ge $warnBytes) { $warned += $row }
    else                              { $ok    += $row }
}

Write-Output ("Scanned {0} nupkg(s). OK: {1}  Warned (>={2}MB): {3}  Failed (>={4}MB): {5}" -f $pkgs.Count, $ok.Count, $WarnMB, $warned.Count, $FailMB, $failed.Count)
Write-Output ""

$failed | Sort-Object sizeMB -Descending | ForEach-Object {
    $msg = "{0}: {1} MB — exceeds {2} MB project ceiling (well under nuget.org's 250 MB hard cap, but past the size we ship); audit native-asset / dependency growth" -f $_.name, $_.sizeMB, $FailMB
    Write-Output ("  [FAIL]  $msg")
    if ($AzdoLogs) {
        Write-Output ("##vso[task.logissue type=error;sourcepath={0}]{1}" -f $_.path, $msg)
    }
}

$warned | Sort-Object sizeMB -Descending | ForEach-Object {
    $msg = "{0}: {1} MB — exceeds {2} MB sanity threshold; verify the size growth is intentional before it hits the {3} MB ceiling" -f $_.name, $_.sizeMB, $WarnMB, $FailMB
    Write-Output ("  [WARN]  $msg")
    if ($AzdoLogs) {
        Write-Output ("##vso[task.logissue type=warning;sourcepath={0}]{1}" -f $_.path, $msg)
    }
}

if ($ok.Count -gt 0) {
    Write-Output ""
    Write-Output "OK packages (under $WarnMB MB):"
    $ok | Sort-Object sizeMB -Descending | ForEach-Object {
        Write-Output ("  {0,8:N1} MB  {1}" -f $_.sizeMB, $_.name)
    }
}

if ($failed.Count -gt 0) { exit 1 }
exit 0
