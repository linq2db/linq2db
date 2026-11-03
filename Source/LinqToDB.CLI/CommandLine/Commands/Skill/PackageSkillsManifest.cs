using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Read-only view of the record that the dotnet-package-skills tool keeps in a skill directory
	/// (<c>.dotnet-package-skills.json</c>): which skill folders it installed, from which package version.
	/// </summary>
	/// <remarks>
	/// This tool never writes, locks or deletes that file; it only learns from it which folders it must leave alone.
	/// The file has a format <c>version</c> and a <c>packages</c> map keyed by lowercase package id; each package has a
	/// <c>versions</c> map keyed by package version, and each version lists the <c>skills</c> folder names it owns.
	/// </remarks>
	internal sealed class PackageSkillsManifest
	{
		public const string FileName = ".dotnet-package-skills.json";

		/// <summary>The only format version of the file that is understood.</summary>
		private const int SupportedFormatVersion = 1;

		private readonly Dictionary<string, string> _owners = new(StringComparer.OrdinalIgnoreCase);

		private PackageSkillsManifest(bool exists, bool isReadable)
		{
			Exists     = exists;
			IsReadable = isReadable;
		}

		/// <summary>The file is present in the directory.</summary>
		public bool Exists { get; }

		/// <summary>
		/// The file was parsed and has a format version this tool understands. When it exists but is not readable,
		/// which folders the other tool owns is unknown.
		/// </summary>
		public bool IsReadable { get; }

		/// <summary>Directory without the file.</summary>
		public static PackageSkillsManifest None { get; } = new(false, true);

		/// <summary>
		/// Version of the package that owns the skill folder, or <see langword="null"/> when no package does.
		/// Names are compared without regard to case.
		/// </summary>
		public string? GetOwnerVersion(string skillName)
		{
			return _owners.TryGetValue(skillName, out var version) ? version : null;
		}

		/// <summary>
		/// Reads the file of a skill directory leniently: a missing file gives <see cref="None"/>; an unparsable file or
		/// one of an unsupported format version gives a manifest with <see cref="IsReadable"/> unset. Only the properties
		/// this tool needs are checked.
		/// </summary>
		public static PackageSkillsManifest TryRead(string directory)
		{
			var path = Path.Combine(directory, FileName);

			try
			{
				if (!File.Exists(path))
					return None;

				using var document = JsonDocument.Parse(File.ReadAllText(path));

				var root = document.RootElement;

				if (root.ValueKind != JsonValueKind.Object
					|| !TryGetProperty(root, "version", out var format)
					|| format.ValueKind != JsonValueKind.Number
					|| !format.TryGetInt32(out var number)
					|| number is < 1 or > SupportedFormatVersion
					|| !TryGetProperty(root, "packages", out var packages)
					|| packages.ValueKind != JsonValueKind.Object)
				{
					return new(true, false);
				}

				var manifest = new PackageSkillsManifest(true, true);

				foreach (var package in packages.EnumerateObject())
				{
					if (package.Value.ValueKind != JsonValueKind.Object
						|| !TryGetProperty(package.Value, "versions", out var versions)
						|| versions.ValueKind != JsonValueKind.Object)
					{
						return new(true, false);
					}

					foreach (var version in versions.EnumerateObject())
					{
						if (version.Value.ValueKind != JsonValueKind.Object
							|| !TryGetProperty(version.Value, "skills", out var skills)
							|| skills.ValueKind != JsonValueKind.Array)
						{
							return new(true, false);
						}

						foreach (var skill in skills.EnumerateArray())
						{
							if (skill.ValueKind == JsonValueKind.String && skill.GetString() is { Length: > 0 } name)
								manifest._owners[name] = version.Name;
						}
					}
				}

				return manifest;
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				return new(true, false);
			}
		}

		private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
		{
			foreach (var property in element.EnumerateObject())
			{
				if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					value = property.Value;
					return true;
				}
			}

			value = default;
			return false;
		}
	}
}
