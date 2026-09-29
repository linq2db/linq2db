<#
run-provider-tests.ps1 - run one TFM's test suites for one Windows provider leg, then free the
binaries:
  config -> local setup -> pre-test -> main suite (optional) -> EF.Core suite -> post-test -> remove

Windows counterpart of run-provider-tests.sh, and the same reason for existing: it is the body of
test-workflow-windows.yml's per-TFM loop, which GitHub cannot express, because a matrix leg's steps
are fixed at parse time and artifact downloads must be actions. The workflow downloads per TFM and
calls this per downloaded TFM. Exiting non-zero reproduces Azure's succeeded() guards, skipping the
EF.Core suite and later TFMs while leaving the .trx already written.

Run from the leg root - the directory holding scripts/, configs/ and the downloaded <tfm>/.

Usage:
    ./scripts/run-provider-tests.ps1 -Tfm net10.0 -Flag net100 -Arch x64 -Config sqlce `
                                     -Main true -Retry false
#>

[CmdletBinding()]
param(
    # Directory the binaries were extracted into, and the TFM's moniker: net462, net8.0, net9.0, net10.0.
    [Parameter(Mandatory)][string] $Tfm,
    # Matrix flag for the same TFM - netfx, net80, net90, net100. Names the config subdirectory.
    [Parameter(Mandatory)][string] $Flag,
    # Config file name without extension, from the entry's config_win.
    [Parameter(Mandatory)][string] $Config,
    [ValidateSet('x64', 'x86')][string] $Arch = 'x64',
    # test-matrix.yml's script_win_local / psscript_win_local, run from the main test app's directory.
    [string] $SetupCmd = '',
    [string] $SetupPs1 = '',
    # pre_test_win_local / post_test_win_local, run from scripts/ as Azure does.
    [string] $PreTest  = '',
    [string] $PostTest = '',
    # Strings rather than switches, so a matrix value forwards straight through and a typo is a hard
    # failure here rather than a silently skipped main suite - the same quiet no-op an empty test
    # matrix produces.
    [ValidateSet('true', 'false')][string] $Main  = 'false',
    [ValidateSet('true', 'false')][string] $Retry = 'false'
)

# Continue, like push-baselines.ps1 and like the .sh which runs without `set -e`: every exit code below
# is inspected explicitly. Stop would also route a native command's non-zero exit through the error
# stream on PowerShell 7.4+ ($PSNativeCommandUseErrorActionPreference defaults on), turning a failing
# test suite into a terminating error that skips the retry loop and loses the suite's own exit code.
$ErrorActionPreference = 'Continue'

$runMain  = $Main  -eq 'true'
$doRetry  = $Retry -eq 'true'

$root    = (Get-Location).Path
$tfmDir  = Join-Path $root $Tfm
$results = Join-Path $root 'TestResults'

if (-not (Test-Path -LiteralPath $tfmDir)) {
    Write-Host "::error::run-provider-tests: '$Tfm' does not exist - the binaries artifact was not downloaded"
    exit 2
}

New-Item -ItemType Directory -Force -Path $results | Out-Null

# Every native call below pipes to Out-Host, and that is load-bearing rather than cosmetic: a native
# command's stdout joins its enclosing function's OUTPUT stream, so `$rc = Invoke-Suite ...` would be
# an array of log lines with the exit code as its last element. Every comparison against it is then
# nonsense - `$rc -ne 0` filters the array and is truthy no matter what the suite did. Measured on
# GitHub run 34291985676: after a PASSING main suite the script exited without running EF.Core at all,
# and the leg reported green. Out-Host writes straight to the host, so the log is unchanged and the
# function returns only the int.
#
# Runs a script from scripts/ and returns its exit code. cmd files go through cmd.exe: PowerShell
# will not surface a .cmd's exit code in $LASTEXITCODE reliably when invoked any other way.
function Invoke-LegScript([string] $name, [string] $workingDirectory) {
    $path = Join-Path (Join-Path $root 'scripts') $name
    # Checked separately from the run so an absent script reads as a staging bug rather than a
    # provider one - the two get debugged in different places.
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Host "::error::run-provider-tests: scripts/$name is not in the test-scripts artifact"
        return 2
    }

    Push-Location $workingDirectory
    try {
        # Reset first: a .ps1 that ends in `return` rather than `exit` - which both of the current
        # global setup scripts do on their success path - leaves $LASTEXITCODE holding whatever the
        # previous native command set, so an unreset read invents a failure.
        $global:LASTEXITCODE = 0
        if ([System.IO.Path]::GetExtension($name) -eq '.ps1') { & $path | Out-Host }
        else { & cmd.exe /c $path | Out-Host }
        return $LASTEXITCODE
    }
    finally { Pop-Location }
}

# Only the main suite retries as a whole, matching retryCountOnTaskFailure: 2 on the Azure step -
# GitHub Actions has no step-level retry, so the loop lives here. It covers the crash case that MTP's
# in-process retry cannot: a host that dies takes its results with it.
function Invoke-Suite([string] $kind, [string] $exeName) {
    $appDir = Join-Path (Join-Path $tfmDir $kind) $Arch
    $exe    = Join-Path $appDir $exeName

    if (-not (Test-Path -LiteralPath $exe)) {
        Write-Host "::error::run-provider-tests: $exe is missing - the $Arch binaries artifact is not what this leg needs"
        return 2
    }

    $testArgs = @(
        '--filter', 'TestCategory != SkipCI'
        '--settings', (Join-Path $appDir '.runsettings')
        '--report-trx', '--report-trx-filename', "$Tfm-$kind-$Arch.trx"
        '--results-directory', $results
        '--hangdump', '--hangdump-timeout', '5m'
    )
    # MTP's own failed-test retry, applied to the flaky legs (Access) only.
    if ($doRetry) { $testArgs += @('--retry-failed-tests', '2', '--retry-failed-tests-max-tests', '5') }

    $attempts = if ($doRetry -and $kind -eq 'main') { 3 } else { 1 }

    for ($i = 1; ; $i++) {
        Write-Host "::group::$kind suite, $Tfm ($Arch, attempt $i/$attempts)"
        & $exe @testArgs | Out-Host
        $rc = $LASTEXITCODE
        Write-Host '::endgroup::'
        if ($rc -eq 0 -or $i -ge $attempts) { return $rc }
        Write-Host "::warning::$kind suite failed for $Tfm (attempt $i/$attempts), retrying"
    }
}

try {
    # The config lands in the TFM root rather than beside the test app: TestConfiguration walks up from
    # the assembly location to find UserDataProviders.json.
    #
    # Checked, because a silent failure here leaves the suite to fall back to DataProviders.json
    # defaults and run a different provider set to a green finish.
    $source = Join-Path (Join-Path (Join-Path $root 'configs') $Flag) "$Config.json"
    try {
        Copy-Item -LiteralPath $source -Destination (Join-Path $tfmDir 'UserDataProviders.json') -Force -ErrorAction Stop
    }
    catch {
        Write-Host "::error::run-provider-tests: could not stage configs/$Flag/$Config.json - the leg would test the wrong providers"
        exit 2
    }
    Write-Host ">>> config: configs/$Flag/$Config.json -> $Tfm/UserDataProviders.json"

    $appDir = Join-Path (Join-Path $tfmDir 'main') $Arch

    foreach ($setup in @(@{ n = $SetupCmd; d = $appDir }, @{ n = $SetupPs1; d = $appDir },
                         @{ n = $PreTest;  d = (Join-Path $root 'scripts') })) {
        if (-not $setup.n) { continue }
        Write-Host "::group::Setup $Tfm ($($setup.n))"
        $rc = Invoke-LegScript $setup.n $setup.d
        Write-Host '::endgroup::'
        if ($rc -ne 0) {
            Write-Host "::error::run-provider-tests: setup script '$($setup.n)' failed for $Tfm"
            exit $rc
        }
    }

    # The main suite is skipped on non-release runs for every TFM but netfx and the newest, and for a
    # win_efcore_only entry it is skipped outright - both decided by the caller. The EF.Core suite
    # always runs, on every enabled TFM.
    if ($runMain) {
        $rc = Invoke-Suite 'main' 'linq2db.Tests.exe'
        if ($rc -ne 0) { exit $rc }
    }

    $status = Invoke-Suite 'efcore' 'linq2db.EntityFrameworkCore.Tests.exe'
    if ($status -ne 0) { exit $status }

    # Azure's post-test step is gated on succeeded(), so a failed suite skips it - hence the exit
    # above rather than running it on the way out.
    if ($PostTest) {
        $status = Invoke-LegScript $PostTest (Join-Path $root 'scripts')
    }

    exit $status
}
finally {
    # Azure removes the TFM directory whether or not the suites passed, so the next TFM's download has
    # the disk. Errors ignored, matching its `rmdir /S /Q`.
    Remove-Item -LiteralPath $tfmDir -Recurse -Force -ErrorAction SilentlyContinue
}
