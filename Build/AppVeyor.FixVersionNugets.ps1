Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$nugetVersion
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($nugetVersion) {

	$nsUri = 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'
	$authors = 'Igor Tkachev, Ilya Chudin, Svyatoslav Danyliv, Dmitry Lukashenko'
	$ns = @{ns=$nsUri}
	$dotlessVersion = $nugetVersion -replace '\.',''

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

		$child = $xml.CreateElement('releaseNotes', $nsUri)
		$child.InnerText = 'https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-' + $dotlessVersion
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('copyright', $nsUri)
		$child.InnerText = 'Copyright (c) 2018 ' + $authors
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('authors', $nsUri)
		$child.InnerText = $authors
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('owners', $nsUri)
		$child.InnerText = $authors
		$xml.package.metadata.AppendChild($child)

		Write-Host "Patched $xmlPath"
		$xml.Save($xmlPath)
	}
}
