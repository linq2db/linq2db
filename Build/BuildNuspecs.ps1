Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$buildPath,
	[Parameter(Mandatory=$true)][string]$version,
	[Parameter(Mandatory=$false)][string]$linq2DbVersion,
	[Parameter(Mandatory=$false)][string]$branch,
	[Parameter(Mandatory=$false)][string]$clean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($clean -and (Test-Path $buildPath)) {
	Remove-Item $buildPath -Recurse
}

if (!(Test-Path $buildPath)) {
	New-Item -Path $buildPath -ItemType Directory
}

if (!$linq2DbVersion){
	$linq2DbVersion = $version
}

if ($version) {

	$nsUri = 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'
	$authors = 'Igor Tkachev, Ilya Chudin, Svyatoslav Danyliv, Dmitry Lukashenko'
	$ns = @{ns=$nsUri}
	$dotlessVersion = $version -replace '\.',''
	$commit = (git rev-parse HEAD)
	if (-not $branch) {
		$branch = (git rev-parse --abbrev-ref HEAD)
	}

	Get-ChildItem $path | ForEach {
		$xmlPath = Resolve-Path $_.FullName

		$xml = [xml] (Get-Content "$xmlPath")
		$xml.PreserveWhitespace = $true

		Select-Xml -Xml $xml -XPath '//ns:metadata/ns:version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.InnerText = $version }

		Select-Xml -Xml $xml -XPath '//ns:dependency[@id="linq2db"]/@version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.Value = $linq2DbVersion }

		$child = $xml.CreateElement('version', $nsUri)
		$child.InnerText = $version
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('releaseNotes', $nsUri)
		$child.InnerText = 'https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-' + $dotlessVersion
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('copyright', $nsUri)
		$child.InnerText = 'Copyright © 2024 ' + $authors
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('authors', $nsUri)
		$child.InnerText = $authors
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('owners', $nsUri)
		$child.InnerText = $authors
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('license', $nsUri)
		$attr = $xml.CreateAttribute('type')
		$attr.Value = 'file'
		$child.Attributes.Append($attr)
		$child.InnerText = 'MIT-LICENSE.txt'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('file', $nsUri)
		$attr = $xml.CreateAttribute('src')
		$attr.Value = '..\..\MIT-LICENSE.txt'
		$child.Attributes.Append($attr)
		$xml.package.files.AppendChild($child)

		$child = $xml.CreateElement('projectUrl', $nsUri)
		$child.InnerText = 'http://linq2db.com'
		$xml.package.metadata.AppendChild($child)

		# add icon + icon file
		$child = $xml.CreateElement('icon', $nsUri)
		$child.InnerText = 'images\icon.png'
		$xml.package.metadata.AppendChild($child)

		$child = $xml.CreateElement('file', $nsUri)
		$attr = $xml.CreateAttribute('src')
		$attr.Value = '..\..\NuGet\icon.png'
		$child.Attributes.Append($attr)
		$attr = $xml.CreateAttribute('target')
		$attr.Value = 'images\icon.png'
		$child.Attributes.Append($attr)
		$xml.package.files.AppendChild($child)


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
		$xml.Save($buildPath + '\' + [System.IO.Path]::GetFileName($xmlPath))
	}
}
