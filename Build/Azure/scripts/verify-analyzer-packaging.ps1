<#
verify-analyzer-packaging.ps1 — assert the bundled Roslyn analyzers are actually in the linq2db nupkg.

The user-facing analyzer + code-fix assemblies are packed into the linq2db package by the
`_BundleAnalyzersIntoPackage` target in Source/LinqToDB/LinqToDB.csproj. That target hooks
`_GetPackageFiles` (an SDK-private target) and resolves each assembly through the `GetTargetPath`
target, which computes a path without asserting the file exists. So every failure mode there is
silent: pack succeeds, the package ships without analyzers, and Tests/Tests.Analyzers stays green
because it references the analyzer projects directly rather than the produced package.

This is the gate for that: fail the build before publish when the expected analyzer assemblies are
missing from the expected Roslyn-version-specific path.

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/verify-analyzer-packaging.ps1 -PackagesDir <dir>

  -PackagesDir         directory to scan recursively for the linq2db package (required)
  -PackageId           package id to inspect (default linq2db)
  -AnalyzerPath        expected in-package folder (default analyzers/dotnet/roslyn4.8/cs); must match
                       the PackagePath in _BundleAnalyzersIntoPackage
  -ExpectedAssemblies  assembly file names expected under -AnalyzerPath (default: the analyzer and
                       code-fix assemblies)
  -NoAzdoLogs          suppress the Azure DevOps `##vso[task.logissue]` lines, which are on by default
                       (the script's primary caller is the AzDO publish pipeline). A switch rather than a
                       [bool] because `pwsh -File` passes every argument as a string, and a [bool]
                       parameter rejects strings outright.

Exit codes:
  0  every expected assembly present at the expected path
  1  one or more expected assemblies missing — release-blocking; build should fail
  2  invalid args / package not found
#>

param(
    [Parameter(Mandatory = $true)]
    [string]   $PackagesDir,
    [string]   $PackageId          = 'linq2db',
    [string]   $AnalyzerPath       = 'analyzers/dotnet/roslyn4.8/cs',
    [string[]] $ExpectedAssemblies = @('LinqToDB.Analyzers.dll', 'LinqToDB.Analyzers.CodeFixes.dll'),
    [switch]   $NoAzdoLogs
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PackagesDir)) {
    [Console]::Error.WriteLine("PackagesDir not found: $PackagesDir")
    exit 2
}

# Match <PackageId>.<version>.nupkg only — the version segment starts with a digit, which keeps
# sibling packages (linq2db.Tools.*, linq2db.EntityFrameworkCore.*, …) out of the match.
$pattern = '^' + [regex]::Escape($PackageId) + '\.\d.*\.nupkg$'
$pkgs    = @(Get-ChildItem -Path $PackagesDir -Recurse -Filter '*.nupkg' -File | Where-Object { $_.Name -match $pattern })

if ($pkgs.Count -eq 0) {
    [Console]::Error.WriteLine("No $PackageId package found under: $PackagesDir")
    exit 2
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$missing = @()

foreach ($pkg in $pkgs) {
    Write-Output ("Inspecting {0}" -f $pkg.Name)

    $zip = [System.IO.Compression.ZipFile]::OpenRead($pkg.FullName)
    try {
        # Zip entry names always use forward slashes, so the comparison needs no normalisation.
        $entries = $zip.Entries | ForEach-Object { $_.FullName }
    }
    finally {
        $zip.Dispose()
    }

    foreach ($assembly in $ExpectedAssemblies) {
        $expected = "$AnalyzerPath/$assembly"
        if ($entries -contains $expected) {
            Write-Output ("  [OK]    $expected")
        }
        else {
            Write-Output ("  [FAIL]  $expected — not in the package")
            $missing += [pscustomobject]@{ package = $pkg.Name; path = $pkg.FullName; entry = $expected }
        }
    }
}

if ($missing.Count -gt 0) {
    Write-Output ""
    foreach ($m in $missing) {
        $msg = "{0} is missing '{1}'. The _BundleAnalyzersIntoPackage target in Source/LinqToDB/LinqToDB.csproj no longer contributes that assembly; consumers would get the package with no analyzers." -f $m.package, $m.entry
        Write-Output ("  [FAIL]  $msg")
        if (-not $NoAzdoLogs) {
            Write-Output ("##vso[task.logissue type=error;sourcepath={0}]{1}" -f $m.path, $msg)
        }
    }
    exit 1
}

Write-Output ""
Write-Output ("All {0} expected analyzer assembly(ies) present under {1}." -f $ExpectedAssemblies.Count, $AnalyzerPath)
exit 0
