Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$buildPath,
	[Parameter(Mandatory=$true)][string]$version,
	[Parameter(Mandatory=$false)][string]$linq2DbVersion,
	[Parameter(Mandatory=$false)][string]$branch,
	[Parameter(Mandatory=$false)][string]$clean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($version) {
	if (-not $branch) {
		$branch = (git rev-parse --abbrev-ref HEAD)
	}

	cd $PSScriptRoot

	dotnet tool install -g dotnet-script
	dotnet script ..\NuGet\BuildNuspecs.csx $path $buildPath $version $linq2DbVersion $branch $clean
}
