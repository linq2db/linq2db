using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Record of what the installer wrote into a skill directory. It is what lets a later run tell the files it
	/// installed (and may replace) from files that belong to the user.
	/// </summary>
	internal sealed class SkillManifest
	{
		public const string FileName = ".linq2db-skill.json";

		private static readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

		public required string                     Skill   { get; init; }
		public required string                     Version { get; init; }
		public required string                     Source  { get; init; }
		public required Dictionary<string, string> Files   { get; init; }

		public static SkillManifest Create(SkillBundle bundle, IEnumerable<string> owned)
		{
			return new SkillManifest
			{
				Skill   = bundle.Name,
				Version = bundle.Version,
				Source  = bundle.IsEmbedded ? "embedded" : "package",
				Files   = owned.OrderBy(static p => p, StringComparer.Ordinal).ToDictionary(static p => p, p => bundle.Hashes[p], StringComparer.Ordinal),
			};
		}

		public string Serialize()
		{
			return JsonSerializer.Serialize(new ManifestDto(Skill, Version, Source, Files), _options) + "\n";
		}

		/// <summary>
		/// Reads a manifest. Returns <see langword="null"/> for a missing, unreadable or malformed file, or one that
		/// lists paths leaving the skill directory; such a directory is treated as not installed by this tool.
		/// </summary>
		public static SkillManifest? TryRead(string path)
		{
			try
			{
				if (!File.Exists(path))
					return null;

				var dto = JsonSerializer.Deserialize<ManifestDto>(File.ReadAllText(path), _options);

				if (dto?.Skill == null || dto.Version == null || dto.Source == null || dto.Files == null)
					return null;

				if (dto.Files.Keys.Any(static p => !SkillPath.IsSafeRelative(p)))
					return null;

				return new SkillManifest
				{
					Skill   = dto.Skill,
					Version = dto.Version,
					Source  = dto.Source,
					Files   = new Dictionary<string, string>(dto.Files, StringComparer.Ordinal),
				};
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}

		private sealed record ManifestDto(string? Skill, string? Version, string? Source, Dictionary<string, string>? Files);
	}
}
