using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Size budget and shape of the <c>linq2db</c> skill in <c>Source/Skills/linq2db</c>, the folder the package and the tool
	/// ship. An agent loads the description always, <c>SKILL.md</c> when the skill is activated and a reference when it opens
	/// one, so the budgets bound what the skill takes from the agent's context. <c>Source/Skills/README.md</c> explains them;
	/// a budget is raised in the change that needs it, with the reason.
	/// </summary>
	[TestFixture]
	public sealed class SkillBudgetTests
	{
		// Token estimate: UTF-8 bytes with LF line endings / 3.5, rounded up. It is an estimate, not the count of any particular
		// tokenizer; CRLF is counted as LF so that a Windows checkout measures the same as a Linux one.
		private const double BytesPerToken = 3.5;

		private const int DescriptionMaxCharacters = 500;
		private const int SkillMaxTokens           = 2_000;
		private const int SkillMaxLines            = 500;
		private const int ReferenceMaxTokens       = 1_500;
		private const int TotalMaxTokens           = 6_000;

		private const string SkillName       = "linq2db";
		private const string ReferenceFolder = "docs";

		private static readonly Regex _linkRegex = new(@"\]\((?<target>[^)\s#]+)(#[^)\s]*)?\)", RegexOptions.Compiled);

		// Wording that makes agents open every file for every task.
		private static readonly string[] _loadEverythingPhrases = ["must read", "required reading", "read this first", "always read"];

		[Test]
		public void SkillMdFitsTheBudget()
		{
			var file   = Path.Combine(GetSkillDirectory(), "SKILL.md");
			var length = GetLength(file);
			var lines  = File.ReadAllLines(file);

			using (Assert.EnterMultipleScope())
			{
				EstimateTokens(length).ShouldBeLessThanOrEqualTo(SkillMaxTokens, $"SKILL.md: {length} bytes");
				lines.Length.ShouldBeLessThanOrEqualTo(SkillMaxLines, "SKILL.md lines");
			}
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

			failures.ShouldBeEmpty();
		}

		[Test]
		public void WholeSkillFitsTheBudget()
		{
			var length = Directory.GetFiles(GetSkillDirectory(), "*", SearchOption.AllDirectories).Sum(GetLength);

			EstimateTokens(length).ShouldBeLessThanOrEqualTo(TotalMaxTokens, $"whole skill: {length} bytes");
		}

		[Test]
		public void FrontmatterNameMatchesTheFolderAndDescriptionFitsTheBudget()
		{
			var lines = File.ReadAllLines(Path.Combine(GetSkillDirectory(), "SKILL.md"));

			lines[0].ShouldBe("---", "SKILL.md must start with YAML frontmatter");

			var end    = Array.IndexOf(lines, "---", 1);
			var fields = lines
				.Skip(1)
				.Take(end - 1)
				.Select(static l => l.Split(':', 2))
				.ToDictionary(static p => p[0], static p => p.Length > 1 ? p[1].Trim() : string.Empty, StringComparer.Ordinal);

			using (Assert.EnterMultipleScope())
			{
				end.ShouldBeGreaterThan(1, "SKILL.md frontmatter is not closed with '---'");
				fields["name"].ShouldBe(SkillName);
				fields["description"].ShouldNotBeNullOrWhiteSpace();
				fields["description"].Length.ShouldBeLessThanOrEqualTo(DescriptionMaxCharacters);
			}
		}

		[Test]
		public void ReferencesAreOneLevelDeep()
		{
			var directory = GetSkillDirectory();

			using (Assert.EnterMultipleScope())
			{
				Directory.GetDirectories(directory).Select(static d => Path.GetFileName(d)).ShouldBe([ReferenceFolder]);
				Directory.GetDirectories(Path.Combine(directory, ReferenceFolder)).ShouldBeEmpty();
				Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Where(static f => !f.EndsWith(".md", StringComparison.Ordinal)).ShouldBeEmpty();
			}
		}

		[Test]
		public void SkillMdLinksEveryReference()
		{
			var text   = File.ReadAllText(Path.Combine(GetSkillDirectory(), "SKILL.md"));
			var linked = _linkRegex.Matches(text).Select(static m => m.Groups["target"].Value).ToHashSet(StringComparer.Ordinal);

			GetReferences()
				.Select(GetRelativePath)
				.Where(r => !linked.Contains(r))
				.ShouldBeEmpty("references that SKILL.md does not link");
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

			failures.ShouldBeEmpty();
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

			failures.ShouldBeEmpty();
		}

		private static int EstimateTokens(long bytes)
		{
			return (int)Math.Ceiling(bytes / BytesPerToken);
		}

		/// <summary>UTF-8 length of the file with CRLF counted as LF.</summary>
		private static long GetLength(string file)
		{
			return Encoding.UTF8.GetByteCount(File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal));
		}

		private static List<string> GetReferences()
		{
			var files = Directory.GetFiles(Path.Combine(GetSkillDirectory(), ReferenceFolder), "*.md").OrderBy(static f => f, StringComparer.Ordinal).ToList();

			files.ShouldNotBeEmpty();

			return files;
		}

		private static string GetRelativePath(string file)
		{
			return Path.GetRelativePath(GetSkillDirectory(), file).Replace('\\', '/');
		}

		// located from this source file, so the build output may be anywhere
		private static string GetSkillDirectory([System.Runtime.CompilerServices.CallerFilePath] string? thisFile = null)
		{
			var directory = new DirectoryInfo(Path.GetDirectoryName(thisFile)!);

			while (directory != null && !File.Exists(Path.Combine(directory.FullName, "linq2db.slnx")))
				directory = directory.Parent;

			directory.ShouldNotBeNull("repository root (linq2db.slnx) above " + thisFile);

			return Path.Combine(directory.FullName, "Source", "Skills", SkillName);
		}
	}
}
