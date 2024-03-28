Param(
	[Parameter(Mandatory=$true)][string]$path,
	[Parameter(Mandatory=$true)][string]$buildPath,
	[Parameter(Mandatory=$true)][string]$version,
	[Parameter(Mandatory=$false)][string]$branch
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest


function Set-File {
	param (
		[Parameter(Mandatory=$true)][string]$src,
		[Parameter(Mandatory=$true)][string]$target
	)

	$xml.package.files.AppendChild($xml.CreateSignificantWhitespace("`n`t`t"))
	$child      = $xml.CreateElement('file', $nsUri)
	$attr       = $xml.CreateAttribute('src')
	$attr.Value = $src
	$child.Attributes.Append($attr)
	$attr       = $xml.CreateAttribute('target')
	$attr.Value = $target
	$child.Attributes.Append($attr)
	$xml.package.files.AppendChild($child)
}

function Set-Metadata {
	param (
		[Parameter(Mandatory=$true)][string]$name,
		[Parameter(Mandatory=$true)][string]$value
	)

	$xml.package.metadata.AppendChild($xml.CreateSignificantWhitespace("`n`t`t"))
	$child           = $xml.CreateElement($name, $nsUri)
	$child.InnerText = $value
	$xml.package.metadata.AppendChild($child)
}

if (Test-Path $buildPath) {
	Remove-Item $buildPath -Recurse
}

New-Item -Path $buildPath -ItemType Directory

if ($version) {

	$nsUri          = 'http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'
	$authors        = 'Igor Tkachev, Ilya Chudin, Svyatoslav Danyliv, Dmitry Lukashenko'
	$description    = ' is a data access technology that provides a run-time infrastructure for managing relational data as objects. Install this package only if you want to use database model scaffolding using T4 templates (requires Visual Studio or Rider), otherwise you should use linq2db package.'
	$ns             = @{ns=$nsUri}
	$dotlessVersion = $version -replace '\.',''
	$commit         = (git rev-parse HEAD)
	if (-not $branch) {
		$branch = (git rev-parse --abbrev-ref HEAD)
	}

	Get-ChildItem $path | ForEach {
		$xmlPath = Resolve-Path $_.FullName

		$isT4 = Select-String -Path $xmlPath -Pattern "content\LinqToDB.Templates" -SimpleMatch -Quiet

		$xml = [xml]::new()
		$xml.PreserveWhitespace = $true
		$xml.Load("$xmlPath")

		Select-Xml -Xml $xml -XPath '//ns:metadata/ns:version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.InnerText = $version }

		Select-Xml -Xml $xml -XPath '//ns:dependency[@id="linq2db.t4models"]/@version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.Value = $version }

		Select-Xml -Xml $xml -XPath '//ns:dependency[@id="linq2db"]/@version' -Namespace $ns |
		Select -expand node |
		ForEach { $_.Value = $version }

		Set-Metadata -name 'version' -value $version

		$descNodes = Select-Xml -Xml $xml -XPath '//ns:metadata/ns:description' -Namespace $ns
		if ($descNodes -eq $null) {
			Set-Metadata -name 'description' -value ($xml.package.metadata.title + $description)
		}

		Set-Metadata -name 'releaseNotes'             -value ('https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-' + $dotlessVersion)
		Set-Metadata -name 'copyright'                -value ('Copyright © 2024 ' + $authors)
		Set-Metadata -name 'authors'                  -value $authors
		Set-Metadata -name 'owners'                   -value $authors
		Set-Metadata -name 'projectUrl'               -value 'http://linq2db.com'
		Set-Metadata -name 'icon'                     -value 'images\icon.png'
		Set-Metadata -name 'requireLicenseAcceptance' -value 'false'

		$xml.package.metadata.AppendChild($xml.CreateSignificantWhitespace("`n`t`t"))
		$child = $xml.CreateElement('license', $nsUri)
		$attr = $xml.CreateAttribute('type')
		$attr.Value = 'file'
		$child.Attributes.Append($attr)
		$child.InnerText = 'MIT-LICENSE.txt'
		$xml.package.metadata.AppendChild($child)

		$xml.package.metadata.AppendChild($xml.CreateSignificantWhitespace("`n`t`t"))
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

		$xml.package.files.AppendChild($xml.CreateSignificantWhitespace("`n`t`t"))
		$child = $xml.CreateElement('file', $nsUri)
		$attr = $xml.CreateAttribute('src')
		$attr.Value = '..\MIT-LICENSE.txt'
		$child.Attributes.Append($attr)
		$xml.package.files.AppendChild($child)

		if ($isT4 -eq $true) {
			Set-File -src '..\NuGet\readme.T4.txt' -target 'readme.txt'
			Set-File -src '..\NuGet\README.T4.md'  -target 'README.md'
		}

		Set-File -src '..\NuGet\icon.png' -target 'images\icon.png'

		$xml.package.metadata.AppendChild($xml.CreateSignificantWhitespace("`n`t"))
		$xml.package.files.AppendChild($xml.CreateSignificantWhitespace("`n`t"))

		Write-Host "Patched $xmlPath"
		$xml.Save($buildPath + '\' + [System.IO.Path]::GetFileName($xmlPath))
	}
}
