using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// The credentials directory: one directory for the local store's files and the generated <c>keyring</c>/<c>gpg</c>
	/// scripts. Resolution is pure (the environment is passed in), so the rules of both operating systems are testable
	/// everywhere; the checks touch the real file system.
	/// </summary>
	internal static class CredentialsDirectory
	{
		/// <summary>Overrides the directory; meant for tests and containers.</summary>
		public const string Variable = "LINQ2DB_CREDENTIALS_DIR";

		public const UnixFileMode DirectoryMode  = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
		public const UnixFileMode FileMode       = UnixFileMode.UserRead | UnixFileMode.UserWrite;
		public const UnixFileMode ScriptMode     = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
		public const UnixFileMode OthersWrite    = UnixFileMode.GroupWrite | UnixFileMode.OtherWrite;
		public const UnixFileMode OthersAnything = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

		/// <summary>
		/// Unix: <c>$LINQ2DB_CREDENTIALS_DIR</c>, else <c>$XDG_CONFIG_HOME/linq2db</c>, else <c>$HOME/.config/linq2db</c>.
		/// Windows: <c>%LINQ2DB_CREDENTIALS_DIR%</c>, else <c>%LOCALAPPDATA%\linq2db</c>. A relative value is ignored.
		/// </summary>
		public static bool TryResolve(bool isWindows, Func<string, string?> getVariable, out string directory, out string? error)
		{
			directory = string.Empty;

			var explicitDirectory = getVariable(Variable);

			if (IsAbsolute(explicitDirectory, isWindows))
			{
				directory = TrimEnd(explicitDirectory!, isWindows);
				error     = null;
				return true;
			}

			if (isWindows)
			{
				var localAppData = getVariable("LOCALAPPDATA");

				if (IsAbsolute(localAppData, isWindows))
				{
					directory = TrimEnd(localAppData!, isWindows) + @"\linq2db";
					error     = null;
					return true;
				}
			}
			else
			{
				var configHome = getVariable("XDG_CONFIG_HOME");

				if (IsAbsolute(configHome, isWindows))
				{
					directory = TrimEnd(configHome!, isWindows) + "/linq2db";
					error     = null;
					return true;
				}

				var home = getVariable("HOME");

				if (IsAbsolute(home, isWindows))
				{
					directory = TrimEnd(home!, isWindows) + "/.config/linq2db";
					error     = null;
					return true;
				}
			}

			error = isWindows
				? $"Cannot determine the credentials directory: LOCALAPPDATA is not set to an absolute path. Set {Variable} to an absolute path inside your profile."
				: $"Cannot determine the credentials directory: neither XDG_CONFIG_HOME nor HOME is set to an absolute path. Set {Variable} to an absolute path.";
			return false;
		}

		/// <summary>
		/// The Windows rule for the local store: the directory must be inside the user profile. A folder elsewhere may give
		/// other users write access, who could then delete the data or put back an older copy. On Windows both paths are
		/// first resolved by the file system (8.3 short names, junctions and symbolic links), so a link inside the profile
		/// that points elsewhere does not count as inside.
		/// </summary>
		public static bool IsInsideProfile(string directory, string? userProfile)
		{
			if (string.IsNullOrEmpty(userProfile))
				return false;

			if (OperatingSystem.IsWindows())
			{
				directory   = WindowsPaths.GetFinalPath(directory);
				userProfile = WindowsPaths.GetFinalPath(userProfile);
			}

			var profile = Normalize(userProfile);
			var path    = Normalize(directory);

			return path.StartsWith(profile + "\\", StringComparison.OrdinalIgnoreCase);

			static string Normalize(string value)
			{
				return value.Replace('/', '\\').TrimEnd('\\');
			}
		}

		/// <summary>The message for a Windows directory outside the user profile.</summary>
		public static string OutsideProfileMessage(string directory)
		{
			return $"The local store refuses the credentials directory '{directory}': it is outside your user profile, where other users may be able to replace its files. Set {Variable} inside your profile.";
		}

		/// <summary>
		/// Creates the directory owner-only (Unix 0700) when it is missing, then checks it (<see cref="CheckUnix"/>).
		/// </summary>
		public static bool TryEnsure(string directory, out string? error)
		{
			try
			{
				if (OperatingSystem.IsWindows())
				{
					Directory.CreateDirectory(directory);
					error = null;
					return true;
				}

				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory, DirectoryMode);

				return CheckUnix(directory, out error);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot create the credentials directory '{directory}': {ex.Message}{AccessHint(ex)}";
				return false;
			}
		}

		/// <summary>
		/// Refuses an existing directory that is a symbolic link or that group or other users can write to, and a directory
		/// whose real path has an ancestor that every user can write to without the sticky bit: they could replace the key,
		/// the data or a generated script, or rename the whole directory and put their own in its place. Ownership is not
		/// checked.
		/// </summary>
		[UnsupportedOSPlatform("windows")]
		public static bool CheckUnix(string directory, out string? error)
		{
			try
			{
				var info = new DirectoryInfo(directory);

				if (info.LinkTarget != null)
				{
					error = $"The credentials directory '{directory}' is a symbolic link; it must be a directory of its own.";
					return false;
				}

				if ((info.UnixFileMode & OthersWrite) != 0)
				{
					error = $"The credentials directory '{directory}' is writable by other users, who could replace its files. Run: chmod 700 '{directory}'";
					return false;
				}

				// Ancestors of the real path: a link such as ~/work -> /srv/shared/work puts the directory under /srv/shared.
				// Group write is accepted there: with private user groups (umask 002) ~/.config is often 0775.
				for (var ancestor = new DirectoryInfo(GetRealPath(directory)).Parent; ancestor != null; ancestor = ancestor.Parent)
				{
					var mode = ancestor.UnixFileMode;

					if (mode.HasFlag(UnixFileMode.OtherWrite) && !mode.HasFlag(UnixFileMode.StickyBit))
					{
						error = $"The credentials directory '{directory}' is inside '{ancestor.FullName}', which every user can write to without the sticky bit: another user could replace the credentials directory. Move it (set {Variable}) or run: chmod o-w '{ancestor.FullName}'";
						return false;
					}
				}

				error = null;
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot check the credentials directory '{directory}': {ex.Message}{AccessHint(ex)}";
				return false;
			}
		}

		/// <summary>
		/// Checks the directory of a script written to a path the user chose (<c>credentials cli init --output</c>): another
		/// user who can replace the script receives the passwords it is given. Refuses a directory whose real path, or an
		/// ancestor of it, every user can write to without the sticky bit, as for the credentials directory's ancestors.
		/// A group-writable script directory is the user's choice (<c>~/bin</c> is often <c>0775</c> with umask <c>002</c>),
		/// so it only gives a <paramref name="warning"/>. Directories that do not exist yet are skipped: they are created
		/// owner-only.
		/// </summary>
		[UnsupportedOSPlatform("windows")]
		public static bool CheckScriptDirectory(string script, out string? warning, out string? error)
		{
			warning = null;

			try
			{
				var nearest = true;

				for (var directory = new DirectoryInfo(GetRealPath(Path.GetDirectoryName(script)!)); directory != null; directory = directory.Parent)
				{
					if (!directory.Exists)
						continue;

					var mode = directory.UnixFileMode;

					if (mode.HasFlag(UnixFileMode.OtherWrite) && !mode.HasFlag(UnixFileMode.StickyBit))
					{
						error = $"Cannot write '{script}': it would be inside '{directory.FullName}', which every user can write to without the sticky bit; another user could replace the script and receive the passwords given to it. Choose another --output or run: chmod o-w '{directory.FullName}'";
						return false;
					}

					// Group write on an ancestor is accepted, as for the credentials directory.
					if (nearest && mode.HasFlag(UnixFileMode.GroupWrite) && !mode.HasFlag(UnixFileMode.StickyBit))
						warning = $"Warning: '{directory.FullName}' is writable by its group; a member of the group could replace '{script}' and receive the passwords given to it.";

					nearest = false;
				}

				error = null;
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot check the directory of '{script}': {ex.Message}{AccessHint(ex)}";
				return false;
			}
		}

		/// <summary>
		/// Resolves symbolic links in every component of an absolute Unix path (like realpath; components that do not exist
		/// are kept as written). Gives up after 40 links, as the kernel does.
		/// </summary>
		internal static string GetRealPath(string path)
		{
			var current = Path.GetFullPath(path);

			for (var links = 0; links < 40; links++)
			{
				var parts    = current.Split('/', StringSplitOptions.RemoveEmptyEntries);
				var resolved = "/";
				string? next = null;

				for (var i = 0; i < parts.Length; i++)
				{
					var candidate = Path.Combine(resolved, parts[i]);
					var target    = new FileInfo(candidate).LinkTarget;

					if (target != null)
					{
						var linked = Path.IsPathRooted(target) ? target : Path.Combine(resolved, target);
						next = Path.GetFullPath(Path.Combine([linked, .. parts.Skip(i + 1)]));
						break;
					}

					resolved = candidate;
				}

				if (next == null)
					return resolved;

				current = next;
			}

			return current;
		}

		/// <summary>The hint added to an access-denied error: the usual cause is a file created with sudo.</summary>
		public static string AccessHint(Exception exception)
		{
			return exception is UnauthorizedAccessException && !OperatingSystem.IsWindows()
				? " (created by another user, for example with sudo?)"
				: string.Empty;
		}

		/// <summary>Whether the directory is inside a git working tree (a <c>.git</c> entry in it or an ancestor).</summary>
		public static bool IsInsideGitWorkTree(string directory)
		{
			try
			{
				for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
				{
					var git = Path.Combine(current.FullName, ".git");

					if (Directory.Exists(git) || File.Exists(git))
						return true;
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
			{
			}

			return false;
		}

		static bool IsAbsolute(string? path, bool isWindows)
		{
			return !string.IsNullOrEmpty(path) && CredentialsCliCommandResolver.IsRooted(path, isWindows);
		}

		static string TrimEnd(string path, bool isWindows)
		{
			var trimmed = isWindows ? path.TrimEnd('\\', '/') : path.TrimEnd('/');
			return trimmed.Length == 0 || (isWindows && trimmed.EndsWith(':')) ? path : trimmed;
		}
	}
}
