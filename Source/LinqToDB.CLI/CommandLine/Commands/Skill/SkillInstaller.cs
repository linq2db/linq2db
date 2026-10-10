using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Plans and applies copies of skills into a skill directory, replacing only files this tool installed and
	/// that were not changed since.
	/// </summary>
	internal static class SkillInstaller
	{
		/// <exception cref="InvalidOperationException">A path involved goes through a symbolic link or junction.</exception>
		public static SkillInstallPlan Analyze(SkillBundle bundle, string root, string directory)
		{
			SkillPath.EnsureNoLinks(root, directory);

			var comparer     = SkillPath.Comparer;
			var manifestPath = Path.Combine(directory, SkillManifest.FileName);

			SkillPath.EnsureNoLinks(root, manifestPath);

			var manifest = SkillManifest.TryRead(manifestPath);

			if (!Directory.Exists(directory) || (manifest == null && !Directory.EnumerateFileSystemEntries(directory).Any()))
			{
				var missing = new SkillInstallPlan { Bundle = bundle, Root = root, Directory = directory, State = SkillInstallState.Missing, Reason = "not installed", WriteManifest = true };

				missing.Writes.AddRange(bundle.Files);
				missing.Owned.UnionWith(bundle.Hashes.Keys);

				return missing;
			}

			if (manifest == null)
			{
				var unmanaged = new SkillInstallPlan
				{
					Bundle        = bundle,
					Root          = root,
					Directory     = directory,
					State         = SkillInstallState.Unmanaged,
					Reason        = $"directory exists but was not installed by this tool ('{SkillManifest.FileName}' is missing, unreadable or invalid)",
					WriteManifest = true,
				};

				unmanaged.Writes.AddRange(bundle.Files);
				unmanaged.Owned.UnionWith(bundle.Hashes.Keys);
				unmanaged.Conflicts.Add($"{directory}: {unmanaged.Reason}");

				return unmanaged;
			}

			var manifestFiles = new Dictionary<string, string>(manifest.Files, comparer);
			var bundleHashes  = new Dictionary<string, string>(bundle.Hashes, comparer);
			var edited        = new HashSet<string>(comparer);
			var missingFiles  = new HashSet<string>(comparer);

			foreach (var (path, hash) in manifest.Files)
			{
				var full = GetPath(directory, path);

				SkillPath.EnsureNoLinks(root, full);

				if (!File.Exists(full))
					missingFiles.Add(path);
				else if (!string.Equals(SkillHash.ComputeFile(full), hash, StringComparison.Ordinal))
					edited.Add(path);
			}

			var staleReasons = new List<string>();

			if (!string.Equals(manifest.Version, bundle.Version, StringComparison.OrdinalIgnoreCase))
				staleReasons.Add($"installed version {manifest.Version}, current {bundle.Version}");

			if (missingFiles.Count > 0)
				staleReasons.Add($"{missingFiles.Count} installed file(s) missing");

			var conflicts = new List<string>();
			var writes    = new List<SkillFile>();
			var deletes   = new List<string>();

			foreach (var file in bundle.Files)
			{
				var full   = GetPath(directory, file.Path);
				var hash   = bundle.Hashes[file.Path];

				SkillPath.EnsureNoLinks(root, full);

				var exists = File.Exists(full);

				if (manifestFiles.TryGetValue(file.Path, out var installedHash))
				{
					if (edited.Contains(file.Path))
					{
						// an edit that already equals the current content is not in anybody's way
						if (!string.Equals(SkillHash.ComputeFile(full), hash, StringComparison.Ordinal))
						{
							conflicts.Add($"{full}: edited locally");
							writes.Add(file);
						}
					}
					else if (!exists || !string.Equals(installedHash, hash, StringComparison.Ordinal))
					{
						writes.Add(file);
					}
				}
				else if (exists)
				{
					// not installed by this tool: leave alone when identical, and do not claim it in the manifest
					if (!string.Equals(SkillHash.ComputeFile(full), hash, StringComparison.Ordinal))
					{
						conflicts.Add($"{full}: exists but was not installed by this tool");
						writes.Add(file);
					}
				}
				else
				{
					writes.Add(file);
				}
			}

			foreach (var path in manifest.Files.Keys.Where(p => !bundleHashes.ContainsKey(p)))
			{
				if (edited.Contains(path))
					conflicts.Add($"{GetPath(directory, path)}: edited locally, and no longer part of the skill");

				if (!missingFiles.Contains(path))
					deletes.Add(path);
			}

			var isUpToDate = edited.Count == 0 && staleReasons.Count == 0 && writes.Count == 0 && deletes.Count == 0
				&& string.Equals(manifest.Source, bundle.IsEmbedded ? "embedded" : "package", StringComparison.Ordinal);

			SkillInstallState state;
			string            reason;

			if (edited.Count > 0)
			{
				state  = SkillInstallState.Modified;
				reason = $"{edited.Count} installed file(s) edited locally";
			}
			else if (isUpToDate)
			{
				state  = SkillInstallState.UpToDate;
				reason = string.Empty;
			}
			else
			{
				state  = SkillInstallState.Stale;
				reason = staleReasons.Count > 0 ? string.Join(", ", staleReasons) : "content differs from the current skill";
			}

			var plan = new SkillInstallPlan { Bundle = bundle, Root = root, Directory = directory, State = state, Reason = reason };

			plan.Writes   .AddRange(writes);
			plan.Deletes  .AddRange(deletes);
			plan.Conflicts.AddRange(conflicts);
			plan.WriteManifest = state != SkillInstallState.UpToDate;

			// the manifest lists what an earlier run installed and what this run writes, never a file that merely happens to match
			plan.Owned.UnionWith(writes.Select(static f => f.Path));
			plan.Owned.UnionWith(bundle.Hashes.Keys.Where(manifestFiles.ContainsKey));

			return plan;
		}

		/// <exception cref="InvalidOperationException">A path involved goes through a symbolic link or junction.</exception>
		public static void Apply(SkillInstallPlan plan)
		{
			// nothing is changed before every path has been checked again
			SkillPath.EnsureNoLinks(plan.Root, plan.Directory);

			foreach (var path in plan.Deletes.Concat(plan.Writes.Select(static f => f.Path)))
				SkillPath.EnsureNoLinks(plan.Root, GetPath(plan.Directory, path));

			SkillPath.EnsureNoLinks(plan.Root, Path.Combine(plan.Directory, SkillManifest.FileName));

			Directory.CreateDirectory(plan.Directory);

			// deletions come first, so a file that is deleted and written under a differently cased name ends up written
			foreach (var path in plan.Deletes)
			{
				var full = GetPath(plan.Directory, path);

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

			foreach (var file in plan.Writes)
			{
				var full = GetPath(plan.Directory, file.Path);

				Directory.CreateDirectory(Path.GetDirectoryName(full)!);
				File.WriteAllBytes(full, file.Content);
			}

			if (plan.WriteManifest)
				File.WriteAllText(Path.Combine(plan.Directory, SkillManifest.FileName), SkillManifest.Create(plan.Bundle, plan.Owned).Serialize());
		}

		private static string GetPath(string directory, string relativePath)
		{
			return Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
