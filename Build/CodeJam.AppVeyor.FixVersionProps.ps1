Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$buildVersion,
	[Parameter(Mandatory=$true)][string]$nugetVersion
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($buildVersion -or $nugetVersion) {

	$xmlPath = Resolve-Path "$path"

	$xml = [XML](Get-Content "$xmlPath")
	$xml.PreserveWhitespace = $true
	$save = $false

	if ($buildVersion) {
		$xPath = "//PropertyGroup/Version"
		$nodes = $xml.SelectNodes($xPath)
		foreach($node in $nodes) {
			$node.InnerXml = $buildVersion
			$save = $true
		}
	}

	if ($nugetVersion) {
		$xPath = "//PropertyGroup/PackageVersion"
		$nodes = $xml.SelectNodes($xPath)
		foreach($node in $nodes) {
			$node.InnerXml = $nugetVersion
			$save = $true
		}
	}

	if ($save) {
		Write-Host "Patched $xmlPath"
		$xml.Save($xmlPath)
	}
}
