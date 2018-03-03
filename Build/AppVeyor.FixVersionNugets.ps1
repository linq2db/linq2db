Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$nugetVersion
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($nugetVersion) {

	$ns = @{ns='http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'}

	Get-ChildItem $path | ForEach {
		$xmlPath = Resolve-Path $_.FullName

		$xml = [xml] (Get-Content "$xmlPath")
		$xml.PreserveWhitespace = $true

		Select-Xml -Xml $xml -XPath '//ns:metadata/ns:version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.InnerText = $nugetVersion }

		Select-Xml -Xml $xml -XPath '//ns:dependency[@id="linq2db.t4models"]/@version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.Value = $nugetVersion }

		Select-Xml -Xml $xml -XPath '//ns:dependency[@id="linq2db"]/@version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.Value = $nugetVersion }

		Write-Host "Patched $xmlPath"
		$xml.Save($xmlPath)
	}
}
