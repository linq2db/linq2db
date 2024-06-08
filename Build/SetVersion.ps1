Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$version
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($version) {

	$xmlPath = Resolve-Path "$path"

	$xml = [XML](Get-Content "$xmlPath")
	$xml.PreserveWhitespace = $true
	$save = $false

	$xPath = "//PropertyGroup/Version"
	$nodes = $xml.SelectNodes($xPath)
	foreach($node in $nodes) {
		$node.InnerXml = $version
		$save = $true
	}

	if ($save) {
		Write-Host "Patched $xmlPath"
		$xml.Save($xmlPath)
	}
}
