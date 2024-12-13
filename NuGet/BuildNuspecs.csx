#!/usr/bin/env dotnet-script
#r "nuget:System.Xml.ReaderWriter, 4.3.1"
#r "nuget:LibGit2Sharp, 0.30.0"

#nullable enable

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

using LibGit2Sharp;

WriteLine(Environment.CommandLine);

var args           = GetArgs();
var path           = GetArg(args, "path",           @"*.nuspec")!;
var buildPath      = GetArg(args, "buildPath",      @"..\.build\nuspecs")!;
var version        = GetArg(args, "version",        mandatory : true)!;
var linq2DbVersion = GetArg(args, "linq2DbVersion", version)!;
var clean          = GetArg(args, "clean",          "0");
var branch         = GetArg(args, "branch");
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

string? IfExists(string config) => File.Exists(Path.Combine(buildPath, "..", "bin", "NuGet", config, "linq2db.dll")) ? config : null;

var releasePath = IfExists("Azure") ?? IfExists("Release") ?? IfExists("Debug") ?? "Azure";
var binPath     = @"..\bin";
var t4binPath   = @"..\bin\NuGet\" + releasePath;

Console.WriteLine($"releasePath: {releasePath}");

IEnumerable<string> GetFiles(string path)
{
	var dir = Path.GetDirectoryName(path);

	if (dir is [_, ..] d)
	{
		if (dir.EndsWith("**"))
		{
			d = dir[..^2] is [_, ..] sd ? sd : ".";

			foreach (var subDir in Directory.GetDirectories(d))
			foreach (var file in Directory.GetFiles(subDir, Path.GetFileName(path)))
				yield return file;
		}
	}
	else
	{
		d = ".";
	}

	foreach (var file in Directory.GetFiles(d.TrimEnd('*'), Path.GetFileName(path)))
		yield return file;
}

//System.Diagnostics.Debugger.Launch();

foreach (var xmlPath in GetFiles(path))
{
	WriteLine($"Processing '{xmlPath}'...");

	var isT4 = File.ReadAllText(xmlPath).IndexOf("%T4%") > 0;
	var xml  = new XmlDocument();

	xml.PreserveWhitespace = true;
	xml.Load(xmlPath);

	var nsUri = xml.DocumentElement!.NamespaceURI;
	var ns    = new XmlNamespaceManager(xml.NameTable);

	ns.AddNamespace("ns", nsUri);

	var metadata = xml.SelectSingleNode("//ns:package/ns:metadata", ns)!;
	var files    = xml.SelectSingleNode("//ns:package/ns:files",    ns);

	if (files == null)
	{
		xml.DocumentElement.AppendChild(xml.CreateSignificantWhitespace("\t"));
		xml.DocumentElement.AppendChild(files = xml.CreateElement("files", nsUri));
		xml.DocumentElement.AppendChild(xml.CreateSignificantWhitespace("\n"));
		files.AppendChild(xml.CreateSignificantWhitespace("\n\t"));
	}

	SetMetadata  ("version",                  version,        true);
	SetDependency("linq2db",                  linq2DbVersion);
	SetDependency("linq2db.t4models",         linq2DbVersion);
	SetMetadata  ("description",              metadata.SelectSingleNode("//ns:title", ns)!.InnerText + description,                               false);
	SetMetadata  ("releaseNotes",             "https://github.com/linq2db/linq2db/wiki/releases-and-roadmap#release-" + version.Replace(".", ""), true);
	SetMetadata  ("copyright",                "Copyright © 2025 " + authors, true);
	SetMetadata  ("authors",                  authors,                       true);
	SetMetadata  ("owners",                   authors,                       true);
	SetMetadata  ("readme",                   "README.md",                   true);
	SetMetadata  ("projectUrl",               "http://linq2db.com",          true);
	SetMetadata  ("icon",                     "images\\icon.png",            true);
	SetMetadata  ("requireLicenseAcceptance", "false",                       true);
	SetMetadata  ("license",                  "MIT-LICENSE.txt",             true,
		SetAttribute("type",   "file"));
	SetMetadata  ("repository",               null,                          true,
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
		var name = Path.GetFileName(xmlPath).Split('.')[1];

		SetMetadata("summary", $"This package includes a T4 template to generate data models for {name} database.", false);
		SetMetadata("tags",    $"{name} linq linq2db LinqToDB ORM database DB SQL",                                 false);

		SetFile(SetAttribute("src", @"NuGet\readme.T4.txt"),                     SetAttribute("target", "readme.txt"));
		SetFile(SetAttribute("src", @"NuGet\README.T4.md"),                      SetAttribute("target", "README.md"));
		SetFile(SetAttribute("src", @"..\..\Source\LinqToDB.Templates\*.*"),     SetAttribute("target", @"contentFiles\any\any\LinqToDB.Templates"));
		SetFile(SetAttribute("src", @"..\..\Source\LinqToDB.Templates\*.*"),     SetAttribute("target", @"content\LinqToDB.Templates"));
		SetFile(SetAttribute("src", @"t4bin\linq2db.dll"),                       SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", @"t4bin\linq2db.Tools.dll"),                 SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", @"t4bin\linq2db.Tools.Internal.dll"),        SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", @"t4bin\Humanizer.dll"),                     SetAttribute("target", "tools"));
		SetFile(SetAttribute("src", @"t4bin\Microsoft.Bcl.AsyncInterfaces.dll"), SetAttribute("target", "tools"));

		if (name is not "t4models")
		{
			SetFile(
				SetAttribute("src",     @$"NuGet\{name}\linq2db.{name}.props"),
				SetAttribute("target",  @$"build"));
			SetFile(
				SetAttribute("src",     @$"NuGet\{name}\*.*"),
				SetAttribute("exclude", @"**\*.props"),
				SetAttribute("target",  @"contentFiles\any\any\LinqToDB.Templates"));
			SetFile(
				SetAttribute("src",     @$"NuGet\{name}\*.*"),
				SetAttribute("exclude", @"**\*.props"),
				SetAttribute("target",  @"content\LinqToDB.Templates"));
		}

		{
			var contentFilesNode = metadata.SelectSingleNode($"//ns:contentFiles", ns);
			var filesNode        = xml.CreateElement("files", nsUri);

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

	XmlNode SetMetadata(string name, string? value, bool update = true, params XmlAttribute[] attrs)
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
			node.InnerText = value.Length > 100 ? $"\n\t\t\t{value}\n\t\t" : value;

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

[return: NotNullIfNotNull(nameof(defaultValue))]
string? GetArg(Dictionary<string,string?> args, string key, string? defaultValue = null, bool mandatory = false) =>
	args.TryGetValue(key, out var value) ? value : !mandatory ? defaultValue : throw new ArgumentException($"Argument '{key}' is required.");

Dictionary<string,string?> GetArgs() =>
	(
		from a in  Environment.CommandLine.Split(' ')
		where a is ['/' or '-', ..]
		let aa = a[1..].Split([':'], 2)
		select
		(
			Key   : aa[0],
			Value : aa is [_, var v] ? v : null
		)
	)
	.Select(a => { WriteLine($"{a.Key} : {a.Value}"); return a; })
	.ToDictionary(a => a.Key, a => (string?)a.Value);

