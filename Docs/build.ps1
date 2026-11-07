<#
.SYNOPSIS
	Builds the linq2db documentation site into .build/docs/_site.

.DESCRIPTION
	Uses the custom docfx build pinned in Docs/docfx-version.json (see Docs/README.md): downloads it on
	first use, verifies its SHA-256 and caches it under .build/docfx/<sha256>/.

.PARAMETER WarningsAsErrors
	Fail the build on any docfx warning (CI mode).
#>
param(
	[switch] $WarningsAsErrors
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot

# docfx spawns `dotnet restore` in the current directory, so global.json must resolve from the repo root
Push-Location $root
try
{
	$pin      = Get-Content (Join-Path $PSScriptRoot 'docfx-version.json') -Raw | ConvertFrom-Json
	$sha      = $pin.sha256.ToLowerInvariant()
	$cache    = Join-Path $root '.build/docfx'
	$docfx    = Join-Path $cache $sha
	$docfxDll = Join-Path $docfx 'docfx.dll'

	Write-Host 'Cleanup previous build artifacts...'
	foreach ($dir in @('.build/docs/_site', 'Docs/source/api'))
	{
		if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
	}

	if (-not (Test-Path $docfxDll))
	{
		Write-Host "Downloading docfx $($pin.tag) from $($pin.url)..."
		New-Item -ItemType Directory -Force $cache | Out-Null
		$zip = Join-Path $cache "$sha.zip"
		Invoke-WebRequest -Uri $pin.url -OutFile $zip

		$actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
		if ($actual -ne $sha)
		{
			Remove-Item $zip -Force
			throw "docfx.zip SHA-256 mismatch: expected $sha, got $actual ($($pin.url))"
		}
		Write-Host "docfx.zip SHA-256 verified: $actual"

		$staging = "$docfx.tmp"
		if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
		Expand-Archive $zip -DestinationPath $staging
		Move-Item $staging $docfx
		Remove-Item $zip -Force
	}

	Write-Host 'Restore...'
	# workaround for https://github.com/dotnet/docfx/pull/8375
	# also works as workaround for https://github.com/dotnet/docfx/issues/9775
	# -p: flags suppress IDE0390/IDE0391 enforcement in Release config. Newer Roslyn (SDK 10.0.x+
	# on the CI windows-2025 image) enforces these as errors via TreatWarningsAsErrors=true; the
	# F# pre-build builds the consumed LinqToDB.csproj transitively across all 5 TFMs and trips
	# on a few `Method can be made synchronous` sites. Docs only needs API surface, not pristine
	# style output. Older SDKs (some local dev boxes) don't enforce — the CI hit is the canonical
	# trigger.
	dotnet build -c Release -p:RunAnalyzersDuringBuild=false -p:EnforceCodeStyleInBuild=false 'Source/LinqToDB.FSharp/LinqToDB.FSharp.fsproj'
	if ($LASTEXITCODE -ne 0)
	{
		throw 'F# pre-build failed'
	}

	Write-Host 'Build DocFX documentation...'
	$docfxArgs = @($docfxDll, 'Docs/source/docfx.json')
	if ($WarningsAsErrors) { $docfxArgs += '--warningsAsErrors' }
	dotnet @docfxArgs
	if ($LASTEXITCODE -ne 0)
	{
		throw 'DocFx build failed'
	}

	Write-Host "Done: $(Join-Path $root '.build/docs/_site')"
}
finally
{
	Pop-Location
}
