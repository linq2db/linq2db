using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// Checks the agent skill shipped in the <c>linq2db</c> package (<c>Source/Skills/linq2db</c>, packed as
	/// <c>skills/linq2db/**</c>). Skill loaders reject a skill whose frontmatter is invalid, and the skill is read
	/// both from the restored package and from a copy in the consumer's repository, so every link has to stay
	/// inside the skill folder.
	/// </summary>
	[TestFixture]
	public class SkillPackageTests : TestBase
	{
		const string SkillName = "linq2db";

		static readonly Regex _linkRegex    = new(@"\[[^\]]*\]\((?<target>[^)\s]+)\)", RegexOptions.Compiled);
		static readonly Regex _headingRegex = new(@"^#{1,6}\s+(?<text>.+?)\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
		static readonly Regex _fenceRegex   = new(@"^```.*?^```", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);
		static readonly Regex _nameRegex    = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

		[Test]
		public void FrontmatterIsValid()
		{
			var skillFile = Path.Combine(GetSkillRoot(), "SKILL.md");
			var lines     = File.ReadAllLines(skillFile);

			lines.Length.ShouldBeGreaterThan(2);
			lines[0].ShouldBe("---", "SKILL.md must start with YAML frontmatter");

			var end = Array.IndexOf(lines, "---", 1);
			end.ShouldBeGreaterThan(1, "SKILL.md frontmatter is not closed with '---'");

			var fields = new Dictionary<string, string>(StringComparer.Ordinal);

			for (var i = 1; i < end; i++)
			{
				var separator = lines[i].IndexOf(':');
				separator.ShouldBeGreaterThan(0, $"Frontmatter line {i + 1} is not a 'key: value' pair: {lines[i]}");

				fields.Add(lines[i].Substring(0, separator), lines[i].Substring(separator + 1).Trim());
			}

			fields.ShouldContainKey("name");
			fields.ShouldContainKey("description");

			var name        = fields["name"];
			var description = fields["description"];

			name.ShouldBe(SkillName, "Frontmatter 'name' must equal the skill folder name");
			name.Length.ShouldBeLessThanOrEqualTo(64);
			_nameRegex.IsMatch(name).ShouldBeTrue("Frontmatter 'name' must be lower-case words joined by hyphens");

			description.ShouldNotBeNullOrWhiteSpace();
			description.Length.ShouldBeLessThanOrEqualTo(1024, "Frontmatter 'description' exceeds 1024 characters");

			// The value is a plain YAML scalar: these sequences would end or reinterpret it.
			description.ShouldNotContain(": ");
			description.ShouldNotContain(" #");
			description.ShouldNotStartWith("\"");
			description.ShouldNotStartWith("'");
		}

		[Test]
		public void RelativeLinksResolveInsideSkill()
		{
			var root     = GetSkillRoot();
			var failures = new List<string>();

			foreach (var file in GetSkillFiles(root))
			{
				var text = StripCodeFences(File.ReadAllText(file));

				foreach (Match match in _linkRegex.Matches(text))
				{
					var target = match.Groups["target"].Value;

					if (target.StartsWith("http://", StringComparison.Ordinal)
						|| target.StartsWith("https://", StringComparison.Ordinal)
						|| target.StartsWith("mailto:", StringComparison.Ordinal))
					{
						continue;
					}

					var hash     = target.IndexOf('#');
					var pathPart = hash < 0 ? target : target.Substring(0, hash);
					var anchor   = hash < 0 ? null   : target.Substring(hash + 1);
					var source   = Relative(root, file);

					var targetFile = pathPart.Length == 0
						? file
						: Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, pathPart.Replace('/', Path.DirectorySeparatorChar)));

					if (!IsUnder(root, targetFile))
					{
						failures.Add($"{source}: link '{target}' leaves the skill folder");
						continue;
					}

					if (!File.Exists(targetFile))
					{
						failures.Add($"{source}: link '{target}' points to a missing file");
						continue;
					}

					if (anchor != null && targetFile.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
						&& !GetAnchors(targetFile).Contains(anchor))
					{
						failures.Add($"{source}: link '{target}' points to a missing heading");
					}
				}
			}

			failures.ShouldBeEmpty();
		}

		[Test]
		public void GuidesUseCopySafePaths()
		{
			var root     = GetSkillRoot();
			var failures = new List<string>();

			foreach (var file in GetSkillFiles(root))
			{
				// SKILL.md explains where the package's linq2db.xml lies; guides name the file only, because a
				// package-root path such as lib/<tfm>/... does not exist next to a copy of the skill.
				if (string.Equals(Path.GetFileName(file), "SKILL.md", StringComparison.Ordinal))
					continue;

				var text = File.ReadAllText(file);

				if (text.Contains("lib/<"))
					failures.Add($"{Relative(root, file)}: package-root path 'lib/<...>'");

				if (text.Contains("Source/LinqToDB"))
					failures.Add($"{Relative(root, file)}: repository source path 'Source/LinqToDB'");
			}

			failures.ShouldBeEmpty();
		}

		[Test]
		public void SkillFilesHaveNoBom()
		{
			var root = GetSkillRoot();

			foreach (var file in GetSkillFiles(root))
			{
				var bytes = File.ReadAllBytes(file);
				var bom   = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

				bom.ShouldBeFalse($"{Relative(root, file)} starts with a UTF-8 BOM");
			}
		}

		[Test]
		public void ProjectPacksSkill()
		{
			var project = Path.Combine(GetRepositoryRoot(), "Source", "LinqToDB", "LinqToDB.csproj");
			var items   = XDocument.Load(project).Descendants("None")
				.Where(e => string.Equals((string?)e.Attribute("Include"), @"..\Skills\linq2db\**\*.md", StringComparison.Ordinal))
				.ToList();

			items.Count.ShouldBe(1, "LinqToDB.csproj must pack ..\\Skills\\linq2db\\**\\*.md once");
			((string?)items[0].Attribute("Pack")).ShouldBe("true");
			((string?)items[0].Attribute("PackagePath")).ShouldBe("skills/linq2db/%(RecursiveDir)%(Filename)%(Extension)");
		}

#if !NETFRAMEWORK
		/// <summary>
		/// Compares the newest packed <c>linq2db.*.nupkg</c> under <c>.build/package</c> (where <c>dotnet pack</c>
		/// writes) with the skill sources. Ignored when nothing has been packed.
		/// </summary>
		[Test]
		public void PackedPackageContainsSkill()
		{
			var repository  = GetRepositoryRoot();
			var packageRoot = Path.Combine(repository, ".build", "package");
			var package     = Directory.Exists(packageRoot)
				? new DirectoryInfo(packageRoot)
					.EnumerateFiles("linq2db.*.nupkg", SearchOption.AllDirectories)
					.Where(f => Regex.IsMatch(f.Name, @"^linq2db\.\d+\.\d+\.\d+.*\.nupkg$") && !f.Name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(f => f.LastWriteTimeUtc)
					.FirstOrDefault()
				: null;

			if (package == null)
				Assert.Ignore($"No packed linq2db package under {packageRoot}; run 'dotnet pack Source/LinqToDB/LinqToDB.csproj' to check its content.");

			var root     = GetSkillRoot();
			var expected = GetSkillFiles(root)
				.Select(f => "skills/linq2db/" + Relative(root, f))
				.OrderBy(f => f, StringComparer.Ordinal)
				.ToList();

			List<string> actual;

			using (var zip = System.IO.Compression.ZipFile.OpenRead(package!.FullName))
			{
				actual = zip.Entries
					.Select(e => Uri.UnescapeDataString(e.FullName.Replace('\\', '/')))
					.Where(e => e.StartsWith("skills/", StringComparison.Ordinal))
					.OrderBy(e => e, StringComparer.Ordinal)
					.ToList();
			}

			actual.ShouldBe(expected, $"{package.Name} does not carry the current skill files; re-pack if it is stale");
		}
#endif

		static string GetRepositoryRoot([CallerFilePath] string sourceFile = "")
		{
			foreach (var start in new[] { Path.GetDirectoryName(sourceFile), AppContext.BaseDirectory })
			{
				for (var dir = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start); dir != null; dir = dir.Parent)
				{
					if (File.Exists(Path.Combine(dir.FullName, "linq2db.slnx")))
						return dir.FullName;
				}
			}

			throw new InvalidOperationException("Cannot find the repository root (linq2db.slnx).");
		}

		static string GetSkillRoot()
		{
			var root = Path.Combine(GetRepositoryRoot(), "Source", "Skills", SkillName);

			Directory.Exists(root).ShouldBeTrue($"Skill folder not found: {root}");

			return root;
		}

		static List<string> GetSkillFiles(string root)
		{
			var files = Directory.GetFiles(root, "*.md", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToList();

			files.ShouldNotBeEmpty();

			return files;
		}

		static string Relative(string root, string file)
		{
			return file.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
		}

		static bool IsUnder(string root, string path)
		{
			var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

			return path.StartsWith(prefix, StringComparison.Ordinal);
		}

		static string StripCodeFences(string text)
		{
			return _fenceRegex.Replace(text, string.Empty);
		}

		/// <summary>
		/// GitHub-style heading anchors: lower case, HTML comments and punctuation dropped, spaces turned into hyphens,
		/// repeated headings numbered.
		/// </summary>
		static HashSet<string> GetAnchors(string file)
		{
			var anchors = new HashSet<string>(StringComparer.Ordinal);
			var text    = StripCodeFences(File.ReadAllText(file));

			foreach (Match match in _headingRegex.Matches(text))
			{
				var heading = Regex.Replace(match.Groups["text"].Value, "<!--.*?-->", string.Empty).Trim();
				var slug    = new StringBuilder();

				foreach (var c in heading.ToLowerInvariant())
				{
					if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
						slug.Append(c);
					else if (c == ' ')
						slug.Append('-');
				}

				var anchor = slug.ToString();
				var unique = anchor;

				for (var i = 1; !anchors.Add(unique); i++)
					unique = anchor + "-" + i;
			}

			return anchors;
		}
	}
}
