using System;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Path rules shared by everything that turns a name from a package, an embedded resource or a manifest into a file path.
	/// </summary>
	internal static class SkillPath
	{
		/// <summary>
		/// Comparison that matches how the platform's file system treats names.
		/// </summary>
		public static StringComparer Comparer { get; } = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal;

		/// <summary>
		/// A '/'-separated path that stays inside the directory it is relative to: not rooted, no drive or stream
		/// separator, no backslash, no empty, '.' or '..' segments.
		/// </summary>
		public static bool IsSafeRelative(string path)
		{
			if (path.Length == 0 || path.Contains('\\', StringComparison.Ordinal) || path.Contains(':', StringComparison.Ordinal) || Path.IsPathRooted(path))
				return false;

			return path.Split('/').All(static s => s.Length != 0 && !string.Equals(s, ".", StringComparison.Ordinal) && !string.Equals(s, "..", StringComparison.Ordinal));
		}

		/// <summary>
		/// Finds the first symbolic link or junction on the way from <paramref name="root"/> to <paramref name="path"/> and
		/// returns its path, or <see langword="null"/> when the path has no link. A link that cannot be inspected counts as a link.
		/// </summary>
		public static string? FindLink(string root, string path)
		{
			var current = root;

			foreach (var segment in Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				current = Path.Combine(current, segment);

				try
				{
					FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);

					if (info.LinkTarget != null || (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint)))
						return current;

					if (!info.Exists)
						return null;
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					return current;
				}
			}

			return null;
		}

		/// <summary>
		/// The path with every link on the way resolved, ancestors included (a symbolic link above the repository, a
		/// <c>/tmp</c> that is a link), so that two spellings of one directory compare equal. A link that cannot
		/// be resolved is left as it is.
		/// </summary>
		public static string GetRealPath(string path)
		{
			return GetRealPath(path, 0);
		}

		private static string GetRealPath(string path, int depth)
		{
			var full    = Path.GetFullPath(path);
			var current = Path.GetPathRoot(full)!;

			foreach (var segment in full.Substring(current.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				if (segment.Length == 0)
					continue;

				current = Path.Combine(current, segment);

				try
				{
					FileSystemInfo info   = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
					var            target = depth < 32 ? info.ResolveLinkTarget(true) : null;

					if (target != null)
						current = GetRealPath(target.FullName, depth + 1);
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					// not resolvable: keep the name as written
				}
			}

			return current;
		}

		/// <summary>
		/// Throws when any existing part of <paramref name="path"/> below <paramref name="root"/> is a symbolic link or
		/// a junction. Writing or deleting through one could leave the repository.
		/// </summary>
		public static void EnsureNoLinks(string root, string path)
		{
			var current = root;

			foreach (var segment in Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{
				current = Path.Combine(current, segment);

				FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);

				if (info.LinkTarget != null || (info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint)))
					throw new InvalidOperationException($"'{current}' is a symbolic link or junction; refusing to write through it.");

				if (!info.Exists)
					return;
			}
		}
	}
}
