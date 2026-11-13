<#
verify-browser-wasm-asset.ps1 - assert the packages a browser app can load carry a Mono-compatible assembly.

net11.0 assemblies are built with runtime-async, which Mono, the default browser-wasm runtime, cannot run. Each
package below therefore carries a second build under runtimes/browser-wasm/lib/<tfm>/, produced by the
AddBrowserWasmAssetToPackage target in Directory.Build.targets. Nothing else notices if that build stops being
packed or starts being compiled with runtime-async: every server-side test passes either way.

Checks, for each package in $Packages and each net11.0-or-newer lib/<tfm>/<id>.dll:
  1. runtimes/browser-wasm/lib/<tfm>/<id>.dll exists.
  2. It has no method with MethodImplAttributes.Async.
  3. lib/<tfm>/<id>.dll does have such methods, so the check above can tell the two builds apart.

Usage:

  pwsh -NoProfile -File Build/Azure/scripts/verify-browser-wasm-asset.ps1 -PackagesDir <dir>

  -PackagesDir   directory to scan recursively for *.nupkg (required)
  -NoAzdoLogs    suppress the Azure DevOps `##vso[task.logissue]` lines, which are on by default. A switch rather
                 than a [bool], because `pwsh -File` passes every argument as a string.

Exit codes:
  0  every check passed
  1  a check failed
  2  invalid args, or a listed package is absent from the drop
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $PackagesDir,
    [switch] $NoAzdoLogs
)

$ErrorActionPreference = 'Stop'

$Packages = @('linq2db', 'linq2db.Remote.HttpClient.Client', 'linq2db.Remote.SignalR.Client', 'linq2db.Remote.Grpc')

$MethodImplAsync = 0x2000

function Write-Failure([string] $Message) {
    if (-not $NoAzdoLogs) { Write-Host "##vso[task.logissue type=error]$Message" }
    Write-Host "FAIL: $Message"
}

function Get-AsyncMethodCount([System.IO.Compression.ZipArchiveEntry] $Entry) {
    $buffer = [System.IO.MemoryStream]::new()

    $source = $Entry.Open()

    try {
        $source.CopyTo($buffer)
    }
    finally {
        $source.Dispose()
    }

    $buffer.Position = 0

    $reader = [System.Reflection.PortableExecutable.PEReader]::new($buffer)

    try {
        $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
        $count    = 0

        foreach ($handle in $metadata.MethodDefinitions) {
            if ([int]$metadata.GetMethodDefinition($handle).ImplAttributes -band $MethodImplAsync) {
                $count++
            }
        }

        return $count
    }
    finally {
        $reader.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $PackagesDir)) {
    Write-Host "PackagesDir '$PackagesDir' does not exist"
    exit 2
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$nupkgs = @(Get-ChildItem -LiteralPath $PackagesDir -Recurse -Filter *.nupkg)
$failed = $false

foreach ($id in $Packages) {
    $nupkg = $nupkgs | Where-Object { $_.Name -match ('^' + [regex]::Escape($id) + '\.\d') } | Select-Object -First 1

    if (-not $nupkg) {
        Write-Host "Package '$id' not found under '$PackagesDir'"
        exit 2
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($nupkg.FullName)

    try {
        $libs = @($zip.Entries | Where-Object { $_.FullName -match ('^lib/net(\d+)\.0/' + [regex]::Escape($id) + '\.dll$') -and [int]$Matches[1] -ge 11 })

        if ($libs.Count -eq 0) {
            Write-Failure "$id has no net11.0-or-newer lib asset"
            $failed = $true
        }

        foreach ($lib in $libs) {
            $tfm      = $lib.FullName.Split('/')[1]
            $assetPath = "runtimes/browser-wasm/lib/$tfm/$id.dll"
            $asset    = $zip.GetEntry($assetPath)

            if (-not $asset) {
                Write-Failure "$id has no $assetPath"
                $failed = $true
                continue
            }

            $assetAsync = Get-AsyncMethodCount $asset
            $libAsync   = Get-AsyncMethodCount $lib

            if ($assetAsync -ne 0) {
                Write-Failure "$id $assetPath has $assetAsync runtime-async methods, expected 0"
                $failed = $true
            }

            if ($libAsync -eq 0) {
                Write-Failure "$id $($lib.FullName) has no runtime-async methods, so the build is not what this check assumes"
                $failed = $true
            }

            Write-Host "$id ${tfm}: lib $libAsync runtime-async methods, browser-wasm $assetAsync"
        }
    }
    finally {
        $zip.Dispose()
    }
}

if ($failed) {
    exit 1
}

exit 0
