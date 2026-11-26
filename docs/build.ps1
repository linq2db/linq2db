<#
.SYNOPSIS
	Builds the linq2db documentation site into .build/docs/_site.

.DESCRIPTION
	Uses the custom docfx build pinned in docs/docfx-version.json (see docs/README.md): downloads it on
	first use, verifies its SHA-256 and caches it under .build/docfx/<sha256>/.

.PARAMETER WarningsAsErrors
	Fail the build on any docfx warning (CI mode).

.PARAMETER ArticlesOnly
	Rebuild only the articles into .build/docs/_preview, in seconds: skips the API metadata and the API pages,
	and resolves links to the API through the xref map of the last full build, which has to exist.
#>
param(
	[switch] $WarningsAsErrors,
	[switch] $ArticlesOnly
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

	$output  = if ($ArticlesOnly) { '.build/docs/_preview' } else { '.build/docs/_site' }
	$xrefMap = '.build/docs/_site/xrefmap.yml'

	if ($ArticlesOnly -and -not ((Test-Path $xrefMap) -and (Test-Path 'docs/source/api')))
	{
		throw '-ArticlesOnly reuses the API of the last full build: run ./docs/build.ps1 without it first'
	}

	Write-Host 'Cleanup previous build artifacts...'
	foreach ($dir in $(if ($ArticlesOnly) { @($output) } else { @($output, 'docs/source/api') }))
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
		if (-not (Test-Path (Join-Path $staging 'docfx.dll')))
		{
			throw "docfx.zip from $($pin.url) has no docfx.dll at its root"
		}
		Move-Item $staging $docfx
		Remove-Item $zip -Force
	}

	if ($ArticlesOnly)
	{
		# The build section of docfx.json, minus the API pages, which take nearly all of a build: their
		# tables of contents stay, because the site navigation links to them. Written next to the output, so
		# every path into docs/source gets a src.
		$build  = (Get-Content 'docs/source/docfx.json' -Raw | ConvertFrom-Json).build
		$source = '../../docs/source'

		foreach ($entry in @($build.content) + @($build.resource))
		{
			$src = if ($entry.PSObject.Properties['src']) { "$source/$($entry.src)" } else { $source }
			$entry | Add-Member -NotePropertyName src -NotePropertyValue $src -Force
		}

		foreach ($entry in $build.content)
		{
			$entry.files = @($entry.files | ForEach-Object { if ($_ -like 'api/*') { 'api/**/toc.yml' } else { $_ } } | Select-Object -Unique)
		}

		$build.template = @($build.template | ForEach-Object { if (Test-Path (Join-Path 'docs/source' $_)) { "$source/$_" } else { $_ } })
		$build.dest     = '_preview'
		$build | Add-Member -NotePropertyName xref -NotePropertyValue @('_site/xrefmap.yml') -Force

		$config = '.build/docs/docfx.articles.json'
		@{ build = $build } | ConvertTo-Json -Depth 10 | Set-Content $config -Encoding utf8

		Write-Host 'Build DocFX articles...'
		# the API tables of contents link pages this build leaves out, which docfx reports at info level
		$docfxArgs = @($docfxDll, 'build', $config, '--logLevel', 'warning')
	}
	else
	{
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
		$docfxArgs = @($docfxDll, 'docs/source/docfx.json')
	}

	if ($WarningsAsErrors) { $docfxArgs += '--warningsAsErrors' }
	dotnet @docfxArgs
	if ($LASTEXITCODE -ne 0)
	{
		throw 'DocFx build failed'
	}

	Write-Host "Done: $(Join-Path $root $output)"
}
finally
{
	Pop-Location
}
