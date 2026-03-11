$src = "../.build/package/release"

$nuget = dir $src linq2db.cli.*.nupkg
if (-not $nuget)
{
	Write-Host -Object 'Cannot find linq2db.cli nuget at $src, run UpdateBaselines.cmd to build nugets'
	return -1
}

if (-not ($nuget -match "linq2db\.cli\.(.*)\.nupkg"))
{
	Write-Host -Object 'Cannot extract nuget version from $nuget'
	return -1
}

$version = $matches[1]

dotnet tool uninstall linq2db.cli -g
dotnet tool install -g --no-cache --source $src --version $version linq2db.cli

