using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using NUnit.Framework;

namespace Tests.Analyzers
{
	/// <summary>
	/// The <c>linq2db</c> Agent Skill in <c>Source/Skills/linq2db</c>, packed into the linq2db package as
	/// <c>skills/linq2db/</c>: its format (agentskills.io), its size budget and its packaging. An agent loads the
	/// description always, <c>SKILL.md</c> when the skill is activated and a reference when it opens one, so the budgets
	/// bound what the skill takes from the agent's context. <c>Source/Skills/README.md</c> explains them; a budget is
	/// raised in the change that needs it, with the reason.
	/// </summary>
	[TestFixture]
	public sealed class SkillContentTests
	{
		// Token estimate: UTF-8 bytes with LF line endings / 3.5, rounded up. It is an estimate, not the count of any particular
		// tokenizer; CRLF is counted as LF so that a Windows checkout measures the same as a Linux one.
		const double BytesPerToken = 3.5;

		const int DescriptionMaxCharacters = 500;
		const int SkillMaxTokens           = 2_000;
		const int SkillMaxLines            = 500;
		const int ReferenceMaxTokens       = 1_500;
		const int TotalMaxTokens           = 6_000;

		const string SkillName       = "linq2db";
		const string ReferenceFolder = "references";

		// Frontmatter fields defined by the Agent Skills specification.
		static readonly string[] _specFields = ["name", "description", "license", "compatibility", "metadata", "allowed-tools"];

		static readonly Regex _nameRegex  = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
		static readonly Regex _linkRegex  = new(@"\]\((?<target>[^)\s#]+)(#[^)\s]*)?\)", RegexOptions.Compiled);
		static readonly Regex _fenceRegex = new(@"^```.*?^```", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);

		// Wording that makes agents open every file for every task.
		static readonly string[] _loadEverythingPhrases = ["must read", "required reading", "read this first", "always read"];

		[Test]
		public void SkillMdFitsTheBudget()
		{
			var file   = Path.Combine(GetSkillDirectory(), "SKILL.md");
			var length = GetLength(file);

			Assert.Multiple(() =>
			{
				Assert.That(EstimateTokens(length), Is.LessThanOrEqualTo(SkillMaxTokens), $"SKILL.md: {length} bytes");
				Assert.That(File.ReadAllLines(file), Has.Length.LessThanOrEqualTo(SkillMaxLines), "SKILL.md lines");
			});
		}

		[Test]
		public void EachReferenceFitsTheBudget()
		{
			var failures = new List<string>();

			foreach (var file in GetReferences())
			{
				var length = GetLength(file);
				var tokens = EstimateTokens(length);

				if (tokens > ReferenceMaxTokens)
					failures.Add($"{GetRelativePath(file)}: about {tokens} tokens ({length} bytes), budget {ReferenceMaxTokens}");
			}

			Assert.That(failures, Is.Empty);
		}

		[Test]
		public void WholeSkillFitsTheBudget()
		{
			var length = Directory.GetFiles(GetSkillDirectory(), "*", SearchOption.AllDirectories).Sum(GetLength);

			Assert.That(EstimateTokens(length), Is.LessThanOrEqualTo(TotalMaxTokens), $"whole skill: {length} bytes");
		}

		[Test]
		public void FrontmatterFollowsTheSpecification()
		{
			var lines = File.ReadAllLines(Path.Combine(GetSkillDirectory(), "SKILL.md"));

			Assert.That(lines[0], Is.EqualTo("---"), "SKILL.md must start with YAML frontmatter");

			var end = Array.IndexOf(lines, "---", 1);

			Assert.That(end, Is.GreaterThan(1), "SKILL.md frontmatter is not closed with '---'");

			var fields = new Dictionary<string, string>(StringComparer.Ordinal);

			for (var i = 1; i < end; i++)
			{
				var separator = lines[i].IndexOf(':', StringComparison.Ordinal);

				Assert.That(separator, Is.GreaterThan(0), $"frontmatter line {i + 1} is not a plain 'key: value' pair: {lines[i]}");

				fields.Add(lines[i].Substring(0, separator), lines[i].Substring(separator + 1).Trim());
			}

			Assert.Multiple(() =>
			{
				Assert.That(fields.Keys, Is.SubsetOf(_specFields), "frontmatter fields outside the Agent Skills specification");
				Assert.That(fields["name"], Is.EqualTo(SkillName), "'name' must equal the skill folder name");
				Assert.That(_nameRegex.IsMatch(fields["name"]), Is.True, "'name': lower-case words joined by single hyphens");
				Assert.That(fields["description"], Is.Not.Empty);
				Assert.That(fields["description"], Has.Length.LessThanOrEqualTo(DescriptionMaxCharacters));

				// a plain YAML scalar: these sequences would end or reinterpret it
				Assert.That(fields["description"], Does.Not.Contain(": ").And.Not.Contain(" #"));
				Assert.That(fields["description"], Does.Not.StartWith("\"").And.Not.StartWith("'").And.Not.StartWith(">").And.Not.StartWith("|"));
			});
		}

		[Test]
		public void ReferencesAreOneLevelDeepAndLinkedFromSkillMd()
		{
			var directory = GetSkillDirectory();
			var text      = File.ReadAllText(Path.Combine(directory, "SKILL.md"));
			var linked    = _linkRegex.Matches(text).Select(static m => m.Groups["target"].Value).ToHashSet(StringComparer.Ordinal);

			Assert.Multiple(() =>
			{
				Assert.That(Directory.GetDirectories(directory).Select(Path.GetFileName), Is.EqualTo(new[] { ReferenceFolder }));
				Assert.That(Directory.GetDirectories(Path.Combine(directory, ReferenceFolder)), Is.Empty);
				Assert.That(Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Where(static f => !f.EndsWith(".md", StringComparison.Ordinal)), Is.Empty);
				Assert.That(GetReferences().Select(GetRelativePath).Where(r => !linked.Contains(r)), Is.Empty, "references that SKILL.md does not link");
			});
		}

		// The skill is read from the package and from copies in a repository (.agents/skills/linq2db), so links stay inside
		// the skill folder and paths outside it are not written as links.
		[Test]
		public void RelativeLinksResolveInsideTheSkill()
		{
			var root     = GetSkillDirectory();
			var failures = new List<string>();

			foreach (var file in Directory.GetFiles(root, "*.md", SearchOption.AllDirectories))
			{
				foreach (Match match in _linkRegex.Matches(_fenceRegex.Replace(File.ReadAllText(file), string.Empty)))
				{
					var target = match.Groups["target"].Value;

					if (target.StartsWith("https://", StringComparison.Ordinal) || target.StartsWith("http://", StringComparison.Ordinal))
						continue;

					var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, target.Replace('/', Path.DirectorySeparatorChar)));

					if (!resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
						failures.Add($"{GetRelativePath(file)}: '{target}' leaves the skill folder");
					else if (!File.Exists(resolved))
						failures.Add($"{GetRelativePath(file)}: '{target}' does not exist");
				}
			}

			Assert.That(failures, Is.Empty);
		}

		[Test]
		public void SkillDoesNotAskToReadEverything()
		{
			var failures = new List<string>();

			foreach (var file in Directory.GetFiles(GetSkillDirectory(), "*.md", SearchOption.AllDirectories))
			{
				var text = File.ReadAllText(file);

				foreach (var phrase in _loadEverythingPhrases)
				{
					if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
						failures.Add($"{GetRelativePath(file)}: '{phrase}'");
				}
			}

			Assert.That(failures, Is.Empty);
		}

		[Test]
		public void FilesAreUtf8WithoutBom()
		{
			var failures = new List<string>();
			var utf8     = new UTF8Encoding(false, true);

			foreach (var file in Directory.GetFiles(GetSkillDirectory(), "*", SearchOption.AllDirectories))
			{
				var bytes = File.ReadAllBytes(file);

				if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
					failures.Add($"{GetRelativePath(file)}: UTF-8 BOM");

				try
				{
					utf8.GetString(bytes);
				}
				catch (DecoderFallbackException)
				{
					failures.Add($"{GetRelativePath(file)}: not UTF-8");
				}
			}

			Assert.That(failures, Is.Empty);
		}

		// Package layout read by NuGet's dotnet-package-skills tool: skills/<skill name>/SKILL.md, one folder per skill.
		// Only the linq2db package carries the skill: provider and integration packages depend on it, and the same skill
		// name from two packages is a collision in that tool. Every item that can place a file in a package is read -
		// any item type with a PackagePath, in projects, .props and .targets files under Source/ and NuGet/ and at the
		// repository root, and <file target> in a .nuspec.
		[Test]
		public void OnlyTheLinq2dbPackagePacksTheSkill()
		{
			var root     = GetRepositoryRoot();
			var packed   = new Dictionary<string, string>(StringComparer.Ordinal);
			var carriers = new List<string>();

			foreach (var file in GetPackagingFiles(root))
			{
				foreach (var (packagePath, include) in GetPackagePaths(file))
				{
					if (!packagePath.StartsWith("skills/", StringComparison.OrdinalIgnoreCase))
						continue;

					var carrier = Path
						.GetRelativePath(root, file)
						.Replace('\\', '/');

					carriers.Add(carrier);
					packed[packagePath] = include.Replace('\\', '/');
				}
			}

			Assert.Multiple(() =>
			{
				Assert.That(carriers.Distinct(), Is.EqualTo(new[] { "Source/LinqToDB/LinqToDB.csproj" }));
				Assert.That(packed, Is.EqualTo(new Dictionary<string, string> { ["skills/linq2db/%(RecursiveDir)%(Filename)%(Extension)"] = "../Skills/linq2db/**/*.md" }));
			});
		}

		static IEnumerable<string> GetPackagingFiles(string root)
		{
			string[] patterns = ["*.csproj", "*.props", "*.targets", "*.nuspec"];

			foreach (var pattern in patterns)
			{
				foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.TopDirectoryOnly))
					yield return file;

				foreach (var folder in new[] { "Source", "NuGet" })
					foreach (var file in Directory.EnumerateFiles(Path.Combine(root, folder), pattern, SearchOption.AllDirectories))
						yield return file;
			}
		}

		// (package path, source) pairs; element and attribute names are matched without the MSBuild / nuspec namespace.
		static IEnumerable<(string PackagePath, string Include)> GetPackagePaths(string file)
		{
			foreach (var element in XDocument.Load(file).Descendants())
			{
				var packagePath = element.Attribute("PackagePath")?.Value;
				var include     = element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value;

				if (packagePath is null && string.Equals(element.Name.LocalName, "file", StringComparison.Ordinal))
				{
					packagePath = element.Attribute("target")?.Value;
					include     = element.Attribute("src")?.Value;
				}

				if (packagePath is null)
					continue;

				foreach (var path in packagePath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					var normalized = path
						.Replace('\\', '/')
						.TrimStart('/');

					yield return (normalized, include ?? string.Empty);
				}
			}
		}

		static int EstimateTokens(long bytes)
		{
			return (int)Math.Ceiling(bytes / BytesPerToken);
		}

		/// <summary>UTF-8 length of the file with CRLF counted as LF.</summary>
		static long GetLength(string file)
		{
			return Encoding.UTF8.GetByteCount(File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal));
		}

		static List<string> GetReferences()
		{
			var files = Directory.GetFiles(Path.Combine(GetSkillDirectory(), ReferenceFolder), "*.md").OrderBy(static f => f, StringComparer.Ordinal).ToList();

			Assert.That(files, Is.Not.Empty);

			return files;
		}

		static string GetRelativePath(string file)
		{
			return Path.GetRelativePath(GetSkillDirectory(), file).Replace('\\', '/');
		}

		static string GetSkillDirectory()
		{
			return Path.Combine(GetRepositoryRoot(), "Source", "Skills", SkillName);
		}

		static string GetRepositoryRoot()
		{
			var directory = new DirectoryInfo(AppContext.BaseDirectory);

			while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "linq2db.slnx")))
				directory = directory.Parent;

			Assert.That(directory, Is.Not.Null, "repository root (linq2db.slnx) above " + AppContext.BaseDirectory);

			return directory!.FullName;
		}
	}
}
