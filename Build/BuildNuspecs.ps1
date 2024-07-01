Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$buildPath,
	[Parameter(Mandatory=$true)][string]$version,
	[Parameter(Mandatory=$false)][string]$branch
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($version) {
	if (-not $branch) {
		$branch = (git rev-parse --abbrev-ref HEAD)
	}

	dotnet tool install -g dotnet-script
	dotnet script ..\NuGet\BuildNuspecs.csx /path:$path /buildPath:$buildPath /version:$version /branch:$branch
}
