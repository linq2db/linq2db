using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Produces the skills to install: the library skill from the linq2db NuGet package or from the copy embedded
	/// in the tool, and the tool's own skill.
	/// </summary>
	internal static class SkillSource
	{
		public const string LibrarySkillName = "linq2db";
		public const string CliSkillName     = "linq2db-cli";

		private const string EmbeddedLibraryPrefix = "LinqToDB.CLI.skill-linq2db/";

		/// <summary>
		/// Version of the tool, which equals the version of the linq2db library it was built with.
		/// </summary>
		public static string ToolVersion
		{
			get
			{
				var assembly = typeof(LinqToDB.Data.DataConnection).Assembly;
				var version  = FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion
					?? assembly.GetName().Version?.ToString(3)
					?? "unknown";
				var plus     = version.IndexOf('+', StringComparison.Ordinal);

				return plus >= 0 ? version.Substring(0, plus) : version;
			}
		}

		public static SkillBundle GetCliSkill()
		{
			return new SkillBundle(
				CliSkillName,
				ToolVersion,
				true,
				"embedded in the tool",
				[new SkillFile("SKILL.md", SkillResource.ReadRawBytes())]);
		}

		public static SkillBundle GetEmbeddedLibrarySkill()
		{
			var assembly = typeof(SkillSource).Assembly;
			var files    = new List<SkillFile>();

			foreach (var name in assembly.GetManifestResourceNames().OrderBy(static n => n, StringComparer.Ordinal))
			{
				if (!name.StartsWith(EmbeddedLibraryPrefix, StringComparison.Ordinal))
					continue;

				using var stream = assembly.GetManifestResourceStream(name)!;
				using var memory = new MemoryStream();

				stream.CopyTo(memory);
				files.Add(new SkillFile(name.Substring(EmbeddedLibraryPrefix.Length).Replace('\\', '/'), memory.ToArray()));
			}

			if (files.Count == 0)
				throw new InvalidOperationException("The tool does not contain an embedded linq2db skill.");

			return new SkillBundle(LibrarySkillName, ToolVersion, true, "embedded in the tool", files);
		}

		/// <summary>
		/// Loads the skill shipped inside an unpacked linq2db package, or returns <see langword="null"/> when the
		/// package does not carry one (not restored, or a version that predates the skill).
		/// </summary>
		public static SkillBundle? TryGetPackageLibrarySkill(ProjectLinq2db linq2db)
		{
			foreach (var packageDirectory in linq2db.PackageDirectories)
			{
				var skillDirectory = Path.Combine(packageDirectory, "skills", LibrarySkillName);

				if (!File.Exists(Path.Combine(skillDirectory, "SKILL.md")))
					continue;

				var files = Directory.EnumerateFiles(skillDirectory, "*", SearchOption.AllDirectories)
					.OrderBy(static f => f, StringComparer.Ordinal)
					.Select(f => new SkillFile(Path.GetRelativePath(skillDirectory, f).Replace('\\', '/'), File.ReadAllBytes(f)))
					.ToList();

				return new SkillBundle(LibrarySkillName, linq2db.Version, false, $"NuGet package linq2db {linq2db.Version}", files);
			}

			return null;
		}
	}
}
