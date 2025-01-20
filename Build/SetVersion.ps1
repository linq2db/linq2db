Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$version,
	[Parameter(Mandatory=$true)][string]$prop
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$xmlPath = Resolve-Path "$path"

$xml = New-Object System.Xml.XmlDocument
$xml.PreserveWhitespace = $true
$xml.Load("$xmlPath")

$nsm = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$nsm.AddNamespace("ns", 'http://schemas.microsoft.com/developer/msbuild/2003')

$xPath = "/ns:Project/ns:PropertyGroup/ns:$prop"
$node = $xml.SelectSingleNode($xPath, $nsm)

if($node) {
	$node.InnerXml = $version
	Write-Host "Patched $xmlPath"
	$xml.Save($xmlPath)
} else {
	Write-Host "Patching failed. Node $xPath not found"
	exit -1
}

