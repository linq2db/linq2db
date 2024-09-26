#!/usr/bin/env dotnet-script
#r "nuget:System.Xml.ReaderWriter, 4.3.1"
#r "nuget:LibGit2Sharp, 0.30.0"

using System.Xml;
using LibGit2Sharp;

#nullable enable

WriteLine(Environment.CommandLine);

var args           = GetArgs();
var path           = GetArg(args, "path",           @"*.nuspec")!;
var buildPath      = GetArg(args, "buildPath",      @"..\.build\nuspecs")!;
var version        = GetArg(args, "version",        mandatory : true)!;
var linq2DbVersion = GetArg(args, "linq2DbVersion", "version")!;
var branch         = GetArg(args, "branch");
var clean          = GetArg(args, "clean")
var authors        = "Igor Tkachev, Ilya Chudin, Svyatoslav Danyliv, Dmitry Lukashenko";
var description    = " is a data access technology that provides a run-time infrastructure for managing relational data as objects. Install this package if you want to use database model scaffolding using T4 templates (requires Visual Studio or Rider), otherwise you should use linq2db package.";

string commit;

using (var repo = new Repository(".."))
{
	commit = repo.Head.Commits.First().Id.Sha;

	if (branch is null)
		branch  = ((SymbolicReference)repo.Refs.Head).Target.CanonicalName.Split('/')[^1];

	WriteLine($"branch  : {branch}");
	WriteLine($"commit  : {commit}");
}


if (clean.ToLower() is "1" or "true" && Directory.Exists(buildPath))
	Directory.Delete(buildPath, true);

if (!Directory.Exists(buildPath))
	Directory.CreateDirectory(buildPath);

var releasePath = File.Exists(Path.Combine(buildPath, "..", "bin", "NuGet", "Release", "linq2db.dll"))
	? "Release"
	: File.Exists(Path.Combine(buildPath, "..", "bin", "NuGet", "Debug", "linq2db.dll"))
		? "Debug"
		: "Release";
var binPath     = @"..\bin";
var t4binPath   = @"..\bin\NuGet\" + releasePath;

IEnumerable<string> GetFiles(string path)
{
	var dir = Path.GetDirectoryName(path);

	if (dir is [_, ..] d)
	{
		if (dir.EndsWith("**"))
		{
			foreach (var subDir in Directory.GetDirectories(d = (dir[..^2] is [_, ..] sd ? sd : ".")))
			foreach (var file in Directory.GetFiles(subDir, Path.GetFileName(path)))
				yield return file;
		}
	}
	else
	{
		d = ".";
	}

	foreach (var file in Directory.GetFiles(".", Path.GetFileName(path)))
		yield return file;
}

foreach (var xmlPath in GetFiles(path))
{
	WriteLine($"Processing '{xmlPath}'...");

	var isT4 = File.ReadAllText(xmlPath).IndexOf("content\\LinqToDB.Templates") > 0;
	var xml  = new XmlDocument();

	xml.PreserveWhitespace = true;
	xml.Load(xmlPath);

	var nsUri = xml.DocumentElement!.NamespaceURI;
	var ns    = new XmlNamespaceManager(xml.NameTable);

	ns.AddNamespace("ns", nsUri);

	var metadata = xml.SelectSingleNode("//ns:package/ns:metadata", ns)!;
	var files    = xml.SelectSingleNode("//ns:package/ns:files",    ns)!;

	SetMetadata  ("version",                  version);
	SetDependency("linq2db",                  linq2DbVersion);
	SetDependency("linq2db.t4models",         linq2DbVersion);
	SetMetadata  ("description",              metadata.SelectSingleNode("//ns:title", ns)!.InnerText + description, false);
	SetMetadata  ("releaseNotes",             "https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-" + version.Replace(".", ""));
	SetMetadata  ("copyright",                "Copyright © 2024 " + authors);
	SetMetadata  ("authors",                  authors);
	SetMetadata  ("owners",                   authors);
	SetMetadata  ("readme",                   "README.md");
	SetMetadata  ("projectUrl",               "http://linq2db.com");
	SetMetadata  ("icon",                     "images\\icon.png");
	SetMetadata  ("requireLicenseAcceptance", "false");
	SetMetadataA ("license",                  "MIT-LICENSE.txt", true,
		SetAttribute("type",   "file"));
	SetMetadataA ("repository",               null,              true,
		SetAttribute("type",   "git"),
		SetAttribute("url",    "https://github.com/linq2db/linq2db.git"),
		SetAttribute("branch", branch),
		SetAttribute("commit", commit));
	SetFile      (
		SetAttribute("src",    @"root\MIT-LICENSE.txt"));
	SetFile      (
		SetAttribute("src",    @"NuGet\icon.png"),
		SetAttribute("target", @"images\icon.png"));

	if (isT4)
	{
		SetFile(SetAttribute("src", @"NuGet\readme.T4.txt"),                            SetAttribute("target", "readme.txt"));
		SetFile(SetAttribute("src", @"NuGet\README.T4.md"),                             SetAttribute("target", "README.md"));
		SetFile(SetAttribute("src", @"..\..\Source\LinqToDB.Templates\*.*"),            SetAttribute("target", @"contentFiles\any\any\LinqToDB.Templates"));
		SetFile(SetAttribute("src", @"..\..\Source\LinqToDB.Templates\*.*"),            SetAttribute("target", @"content\LinqToDB.Templates"));
		SetFile(SetAttribute("src", t4binPath + @"\linq2db.dll"),                       SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", t4binPath + @"\linq2db.Tools.dll"),                 SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", t4binPath + @"\Humanizer.dll"),                     SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", t4binPath + @"\Microsoft.Bcl.AsyncInterfaces.dll"), SetAttribute("target", "tools"));

		{
			var contentFilesNode = metadata.SelectSingleNode($"//ns:contentFiles", ns);
			var filesNode = xml.CreateElement("files", nsUri);

			if (contentFilesNode == null)
			{
				metadata.AppendChild(xml.CreateSignificantWhitespace("\t"));
				metadata.AppendChild(contentFilesNode = xml.CreateElement("contentFiles", nsUri));
				metadata.AppendChild(xml.CreateSignificantWhitespace("\n\t"));
				contentFilesNode.AppendChild(xml.CreateSignificantWhitespace("\n\t\t\t"));
			}
			else
			{
				contentFilesNode.AppendChild(xml.CreateSignificantWhitespace("\t"));
			}

			contentFilesNode.AppendChild(filesNode);
			contentFilesNode.AppendChild(xml.CreateSignificantWhitespace("\n\t\t"));

			filesNode.Attributes.Append(SetAttribute("include",     "**\\*"));
			filesNode.Attributes.Append(SetAttribute("buildAction", "None"));
		}
	}

	foreach (XmlAttribute attr in xml.SelectNodes("//ns:file/@src", ns)!)
	{
		switch (attr.Value.ToLower())
		{
			case var s when s.StartsWith("t4bin\\") :
				attr.Value = t4binPath + attr.Value[5..];
				break;
			case var s when s.StartsWith("bin\\") :
				attr.Value = string.Format(binPath + attr.Value[3..].Replace("\\Release\\", "\\{0}\\"), releasePath);
				break;
			case var s when s.StartsWith("nuget\\") :
				attr.Value = @"..\..\" + attr.Value;
				break;
			case var s when s.StartsWith("root\\") :
				attr.Value = @"..\..\" + attr.Value[5..];
				break;
		}
	}

	xml.Save(Path.Combine(buildPath, Path.GetFileName(xmlPath)));

	XmlNode SetFile(params XmlAttribute[] attrs)
	{
		var node = xml.CreateElement("file", nsUri);

		files.AppendChild(xml.CreateSignificantWhitespace("\t"));
		files.AppendChild(node);
		files.AppendChild(xml.CreateSignificantWhitespace("\n\t"));

		foreach (var attr in attrs)
			node.Attributes!.Append(attr);

		return node;
	}

	XmlNode SetMetadata(string name, string? value, bool update = true)
	{
		var node = metadata.SelectSingleNode($"//ns:{name}", ns);

		if (node == null)
		{
			metadata.AppendChild(xml.CreateSignificantWhitespace("\t"));
			metadata.AppendChild(node = xml.CreateElement(name, nsUri));
			metadata.AppendChild(xml.CreateSignificantWhitespace("\n\t"));
		}
		else if (!update)
		{
			return node;
		}

		if (value != null)
			node.InnerText = value.Length > 20 ? $"\n\t\t\t{value}\n\t\t" : value;

		return node;
	}

	XmlNode SetMetadataA(string name, string? value, bool update, params XmlAttribute[] attrs)
	{
		var node = SetMetadata(name, value, update);

		foreach (var attr in attrs)
			node.Attributes!.Append(attr);

		return node;
	}

	void SetDependency(string name, string value)
	{
		foreach (XmlAttribute attr in xml.SelectNodes($"//ns:dependency[@id=\"{name}\"]/@version", ns)!)
			attr.Value = value;
	}

	XmlAttribute SetAttribute(string name, string value)
	{
		var attr = xml.CreateAttribute(name);
		attr.Value = value;
		return attr;
	}
}

string? GetArg(Dictionary<string,string?> args, string key, string? defaultValue = null, bool mandatory = false) =>
	args.TryGetValue(key, out var value) ? value : !mandatory ? defaultValue : throw new ArgumentException($"Argument '{key}' is required.");

Dictionary<string,string?> GetArgs() =>
	(
		from a in  Environment.CommandLine.Split(' ')
		where a is ['/' or '-', ..]
		let aa = a[1..].Split(':')
		select
		(
			Key   : aa[0],
			Value : aa is [_, var v] ? v : null
		)
	)
	.Select(a => { WriteLine($"{a.Key} : {a.Value}"); return a; })
	.ToDictionary(a => a.Key, a => (string?)a.Value);


/*

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
		$attr.Value = '..\..\MIT-LICENSE.txt'
		$child.Attributes.Append($attr)
		$xml.package.files.AppendChild($child)

		if ($isT4 -eq $true) {
			Set-File -src '..\..\NuGet\readme.T4.txt' -target 'readme.txt'
			Set-File -src '..\..\NuGet\README.T4.md'  -target 'README.md'
		}

		Set-File -src '..\..\NuGet\icon.png' -target 'images\icon.png'

		$xml.package.metadata.AppendChild($xml.CreateSignificantWhitespace("`n`t"))
		$xml.package.files.AppendChild($xml.CreateSignificantWhitespace("`n`t"))

		Write-Host "Patched $xmlPath"
		$xml.Save($buildPath + '\' + [System.IO.Path]::GetFileName($xmlPath))
	}
}
*/
