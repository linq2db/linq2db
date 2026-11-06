using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Removes the files this tool installed into a skill directory and nothing else.
	/// </summary>
	internal static class SkillUninstaller
	{
		/// <exception cref="InvalidOperationException">A path involved goes through a symbolic link or junction.</exception>
		public static SkillUninstallPlan Analyze(string name, string root, string directory)
		{
			SkillPath.EnsureNoLinks(root, directory);

			if (!Directory.Exists(directory))
				return new SkillUninstallPlan { Name = name, Directory = directory, State = SkillUninstallState.NotInstalled };

			var destination = Path.GetDirectoryName(directory)!;

			SkillPath.EnsureNoLinks(root, Path.Combine(destination, PackageSkillsManifest.FileName));

			var otherTool = PackageSkillsManifest.TryRead(destination);
			var owner     = otherTool.GetOwner(name);

			if (owner != null)
			{
				var who = owner.IsLinq2db ? $"linq2db {owner.Version}" : $"package {owner}";

				return new SkillUninstallPlan { Name = name, Directory = directory, State = SkillUninstallState.ManagedExternally, Reason = $"managed by dotnet-package-skills ({who}); left unchanged" };
			}

			if (otherTool.Exists && !otherTool.IsReadable)
			{
				return new SkillUninstallPlan
				{
					Name      = name,
					Directory = directory,
					State     = SkillUninstallState.OwnershipUnknown,
					Reason    = $"{PackageSkillsManifest.FileName} could not be read, so it is unknown whether dotnet-package-skills owns this directory; left unchanged",
				};
			}

			var markerPath = Path.Combine(directory, SkillManifest.FileName);

			SkillPath.EnsureNoLinks(root, markerPath);

			var manifest = SkillManifest.TryRead(markerPath);

			if (manifest == null)
			{
				return new SkillUninstallPlan
				{
					Name      = name,
					Directory = directory,
					State     = SkillUninstallState.NotInstalledByTool,
					Reason    = $"not installed by this tool ('{SkillManifest.FileName}' is missing, unreadable or invalid); left unchanged",
				};
			}

			var plan = new SkillUninstallPlan { Name = name, Directory = directory, State = SkillUninstallState.Installed, Manifest = manifest };

			foreach (var (path, hash) in manifest.Files)
			{
				var full = GetPath(directory, path);

				SkillPath.EnsureNoLinks(root, full);

				if (!File.Exists(full))
					continue;

				if (string.Equals(SkillHash.ComputeFile(full), hash, StringComparison.Ordinal))
					plan.Unchanged.Add(path);
				else
					plan.Changed.Add(path);
			}

			var recorded = new HashSet<string>(manifest.Files.Keys, SkillPath.Comparer);

			foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).OrderBy(static f => f, StringComparer.Ordinal))
			{
				var relative = Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/');

				if (string.Equals(relative, SkillManifest.FileName, StringComparison.Ordinal) || recorded.Contains(relative))
					continue;

				SkillPath.EnsureNoLinks(root, file);
				plan.Added.Add(relative);
			}

			return plan;
		}

		/// <summary>
		/// Deletes the unchanged recorded files (and the changed ones with <paramref name="force"/>), then the marker and the
		/// directory when nothing else is left. Returns how many files were left in place.
		/// </summary>
		/// <exception cref="InvalidOperationException">A path involved goes through a symbolic link or junction.</exception>
		public static int Apply(SkillUninstallPlan plan, string root, bool force)
		{
			var manifest = plan.Manifest!;
			var removed  = plan.Unchanged.Concat(force ? plan.Changed : []).ToList();
			var kept     = new List<string>(force ? [] : plan.Changed);

			// nothing is changed before every path has been checked again
			SkillPath.EnsureNoLinks(root, plan.Directory);

			foreach (var path in removed)
				SkillPath.EnsureNoLinks(root, GetPath(plan.Directory, path));

			SkillPath.EnsureNoLinks(root, Path.Combine(plan.Directory, SkillManifest.FileName));

			foreach (var path in removed)
			{
				var full = GetPath(plan.Directory, path);

				// the content is checked again right before the delete: a file edited since the analysis is kept
				if (!force && File.Exists(full) && !string.Equals(SkillHash.ComputeFile(full), manifest.Files[path], StringComparison.Ordinal))
				{
					kept.Add(path);
					continue;
				}

				if (File.Exists(full))
					File.Delete(full);

				// drop directories left empty, but never the skill directory itself
				var parent = Path.GetDirectoryName(full);

				while (parent != null && !string.Equals(parent, plan.Directory, StringComparison.Ordinal)
					&& Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
				{
					Directory.Delete(parent);
					parent = Path.GetDirectoryName(parent);
				}
			}

			var markerPath = Path.Combine(plan.Directory, SkillManifest.FileName);

			if (kept.Count == 0)
			{
				File.Delete(markerPath);
			}
			else
			{
				// the marker keeps listing what is still there, so a later install or uninstall treats the edits as before
				var remaining = new SkillManifest
				{
					Skill   = manifest.Skill,
					Version = manifest.Version,
					Source  = manifest.Source,
					Files   = manifest.Files.Where(p => kept.Contains(p.Key, SkillPath.Comparer)).ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal),
				};

				File.WriteAllText(markerPath, remaining.Serialize());
			}

			if (!Directory.EnumerateFileSystemEntries(plan.Directory).Any())
				Directory.Delete(plan.Directory);

			return kept.Count + plan.Added.Count;
		}

		private static string GetPath(string directory, string relativePath)
		{
			return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
