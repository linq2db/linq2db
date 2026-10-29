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
