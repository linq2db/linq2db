$src = '../.build/package/release'

# Match the pointer package only (linq2db.cli.<version>.nupkg, with a leading
# digit on the version segment). Excludes per-RID sub-packages introduced in
# 6.3.0 (linq2db.cli.win-x64.<version>.nupkg, linq2db.cli.linux-arm64.*.nupkg,
# etc.) — those would otherwise multi-match the glob and the regex would
# either pick a RID-prefixed name as version or leave $matches unset.
$nuget = dir $src linq2db.cli.[0-9]*.nupkg
if (-not $nuget)
{
	Write-Host -Object "Cannot find linq2db.cli pointer nuget at $src, run UpdateBaselines.cmd to build nugets"
	return -1
}

if (-not ($nuget -match '^linq2db\.cli\.(\d.*)\.nupkg$'))
{
	Write-Host -Object "Cannot extract nuget version from $nuget"
	return -1
}

$version = $matches[1]

dotnet tool uninstall linq2db.cli -g
dotnet tool install -g --no-cache --source $src --version $version linq2db.cli

