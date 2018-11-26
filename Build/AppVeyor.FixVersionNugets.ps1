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
	$commit = (git rev-parse HEAD)
	$branch = (git rev-parse --abbrev-ref HEAD)

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

		$child = $xml.CreateElement('licenseUrl', $nsUri)
		$child.InnerText = 'https://github.com/linq2db/linq2db/blob/master/MIT-LICENSE.txt'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('projectUrl', $nsUri)
		$child.InnerText = 'https://github.com/linq2db/linq2db'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('iconUrl', $nsUri)
		$child.InnerText = 'http://www.gravatar.com/avatar/fc2e509b6ed116b9aa29a7988fdb8990?s=320'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('requireLicenseAcceptance', $nsUri)
		$child.InnerText = 'false'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('repository', $nsUri)
		$attr = $xml.CreateAttribute('type')
		$attr.Value = 'git'
		$child.Attributes.Append($attr)
		$attr = $xml.CreateAttribute('url')
		$attr.Value = 'https://github.com/linq2db/linq2db.git'
		$child.Attributes.Append($attr)
		$attr = $xml.CreateAttribute('branch')
		$attr.Value = $branch
		$child.Attributes.Append($attr)
		$attr = $xml.CreateAttribute('commit')
		$attr.Value = $commit
		$child.Attributes.Append($attr)
		$xml.package.metadata.AppendChild($child)

		Write-Host "Patched $xmlPath"
		$xml.Save($xmlPath)
	}
}
