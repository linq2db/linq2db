using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// A complete skill (all of its files) ready to be installed, together with its provenance.
	/// </summary>
	internal sealed class SkillBundle
	{
		private Dictionary<string, string>? _hashes;

		public SkillBundle(string name, string version, bool isEmbedded, string origin, IReadOnlyList<SkillFile> files)
		{
			Name       = name;
			Version    = version;
			IsEmbedded = isEmbedded;
			Origin     = origin;
			Files      = files;

			foreach (var file in files)
			{
				if (!SkillPath.IsSafeRelative(file.Path))
					throw new InvalidDataException($"The {name} skill ({origin}) contains an unsafe file path '{file.Path}'.");
			}
		}

		/// <summary>Skill name, which is also its directory name.</summary>
		public string                  Name       { get; }
		/// <summary>Version of the library the skill describes.</summary>
		public string                  Version    { get; }
		/// <summary>Whether the files come from the copy embedded in the tool rather than from a NuGet package.</summary>
		public bool                    IsEmbedded { get; }
		/// <summary>Human-readable source description.</summary>
		public string                  Origin     { get; }
		public IReadOnlyList<SkillFile> Files     { get; }

		/// <summary>Line-ending-insensitive hash of each file, by relative path.</summary>
		public IReadOnlyDictionary<string, string> Hashes
		{
			get
			{
				return _hashes ??= Files.ToDictionary(static f => f.Path, static f => SkillHash.Compute(f.Content), StringComparer.Ordinal);
			}
		}
	}
}
