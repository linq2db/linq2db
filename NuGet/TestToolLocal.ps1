$src = '../.build/package/release'

# Match the pointer package only (linq2db.cli.<version>.nupkg, with a leading
# digit on the version segment). Excludes per-RID sub-packages introduced in
# 6.3.0 (linq2db.cli.win-x64.<version>.nupkg, linq2db.cli.linux-arm64.*.nupkg,
# etc.) — those would otherwise multi-match the glob and the regex would
# either pick a RID-prefixed name as version or leave $matches unset.
# Get-ChildItem's -Filter is a FileSystem-provider pattern that supports only
# * and ?, so the version-segment narrowing has to happen in the Where-Object
# regex (FileInfo.Name -match '^linq2db\.cli\.\d.*\.nupkg$') rather than the
# -Filter argument. If the directory has more than one pointer (multiple
# package versions), pick the most recently written one.
$nuget = Get-ChildItem -Path $src -Filter 'linq2db.cli.*.nupkg' |
	Where-Object { $_.Name -match '^linq2db\.cli\.\d.*\.nupkg$' } |
	Sort-Object LastWriteTime -Descending |
	Select-Object -First 1
if (-not $nuget)
{
	Write-Host -Object "Cannot find linq2db.cli pointer nuget at $src, run UpdateBaselines.cmd to build nugets"
	return -1
}

if (-not ($nuget.Name -match '^linq2db\.cli\.(\d.*)\.nupkg$'))
{
	Write-Host -Object "Cannot extract nuget version from $($nuget.Name)"
	return -1
}

$version = $matches[1]

dotnet tool uninstall linq2db.cli -g
dotnet tool install -g --no-cache --source $src --version $version linq2db.cli

