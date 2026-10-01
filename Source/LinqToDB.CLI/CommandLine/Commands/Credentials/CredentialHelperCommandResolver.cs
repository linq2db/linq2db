using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Resolves a configured credential helper command to an absolute file and decides how to start it.
	/// Pure: the file system and environment are passed in, so Windows rules are testable on every OS.
	/// </summary>
	internal static class CredentialHelperCommandResolver
	{
		/// <summary>
		/// How to start a credential helper: the program, and either its argument list or a raw command line.
		/// </summary>
		internal sealed record Launch(string FileName, IReadOnlyList<string> Arguments, string? RawArguments);

		/// <summary>Extensions a Windows helper may have; PATHEXT is filtered to these.</summary>
		static readonly string[] _windowsExtensions = [".com", ".exe", ".bat", ".cmd"];

		const string DefaultPathExt = ".COM;.EXE;.BAT;.CMD";

		public static bool TryResolve(
			string             command,
			string?            baseDirectory,
			bool               isWindows,
			string?            pathVariable,
			string?            pathExtVariable,
			Func<string, bool> fileExists,
			out string         resolvedPath,
			out string?        error)
		{
			resolvedPath = string.Empty;

			if (string.IsNullOrWhiteSpace(command) || command.Any(char.IsControl))
			{
				error = "Credential helper command must be a non-empty path or file name without control characters.";
				return false;
			}

			var extensions = isWindows ? GetWindowsExtensions(pathExtVariable) : [];

			if (HasDirectory(command, isWindows))
			{
				var path = command;

				if (!IsRooted(path, isWindows))
				{
					var directory = baseDirectory ?? Directory.GetCurrentDirectory();
					path = isWindows
						? directory.TrimEnd('\\', '/') + "\\" + path
						: directory.TrimEnd('/')       + "/"  + path;
				}

				if (!isWindows || OperatingSystem.IsWindows())
					path = Path.GetFullPath(path);

				if (TryFindFile(path, isWindows, extensions, fileExists, out resolvedPath))
					return CheckExtension(command, resolvedPath, isWindows, out error);

				error = $"Credential helper '{command}' was not found.";
				return false;
			}

			foreach (var directory in (pathVariable ?? string.Empty).Split(isWindows ? ';' : ':'))
			{
				var entry = directory.Trim();

				if (isWindows && entry.Length > 1 && entry[0] == '"' && entry[^1] == '"')
					entry = entry.Substring(1, entry.Length - 2);

				// Empty and relative PATH entries name the current directory; a helper is never looked up there.
				if (entry.Length == 0 || !IsRooted(entry, isWindows))
					continue;

				var candidate = isWindows
					? entry.TrimEnd('\\', '/') + "\\" + command
					: entry.TrimEnd('/')       + "/"  + command;

				if (TryFindFile(candidate, isWindows, extensions, fileExists, out resolvedPath))
					return CheckExtension(command, resolvedPath, isWindows, out error);
			}

			error = isWindows && HasUnsupportedWindowsExtension(command)
				? UnsupportedWindowsExtensionMessage(command)
				: $"Credential helper '{command}' was not found on PATH.";
			return false;
		}

		/// <summary>
		/// Returns how to start the helper for a verb: the program and either an argument list or, for Windows batch files,
		/// a raw command line. The configured arguments come first and the verb last; each is one argument, never parsed by
		/// a shell. POSIX shell scripts (<c>*.sh</c>) run as <c>/bin/sh script arguments verb</c>, so they need neither an exec
		/// bit nor a shebang. Windows <c>.cmd</c>/<c>.bat</c> files run as <c>cmd.exe /d /v:off /s /c ""script" verb"</c>:
		/// with <c>/s</c> cmd.exe removes only the outer quotes, so a script path with spaces and parentheses
		/// (<c>C:\Program Files (x86)\...</c>) stays intact, which is not the case when CreateProcess starts a batch file
		/// itself. The verb is a fixed lower-case word and the path is quoted, but cmd.exe still expands <c>%NAME%</c> inside
		/// quotes, so a batch path containing <c>%</c> is refused; configured arguments are refused for batch files because
		/// cmd.exe would parse them. Everything else is started directly.
		/// </summary>
		public static bool TryGetLaunch(string resolvedPath, IReadOnlyList<string> arguments, string verb, bool isWindows, string? systemDirectory, out Launch launch, out string? error)
		{
			if (arguments.Any(static argument => argument.Any(char.IsControl)))
			{
				launch = null!;
				error  = $"Credential helper '{resolvedPath}' arguments must not contain control characters.";
				return false;
			}

			error = null;

			if (!isWindows)
			{
				launch = resolvedPath.EndsWith(".sh", StringComparison.Ordinal)
					? new Launch("/bin/sh", [resolvedPath, .. arguments, verb], null)
					: new Launch(resolvedPath, [.. arguments, verb], null);
				return true;
			}

			var extension = GetExtension(resolvedPath);

			if (string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase))
			{
				if (resolvedPath.Contains('%', StringComparison.Ordinal))
				{
					launch = null!;
					error  = $"Credential helper '{resolvedPath}' is a batch file whose path contains '%', which cmd.exe would expand. Move it to a path without '%'.";
					return false;
				}

				if (arguments.Count > 0)
				{
					launch = null!;
					error  = $"Credential helper '{resolvedPath}' is a batch file; configured arguments are not supported for batch files because cmd.exe would interpret them. Put them in the batch file, or configure the program it runs as the command.";
					return false;
				}

				var cmd = (systemDirectory ?? @"C:\Windows\System32").TrimEnd('\\') + @"\cmd.exe";
				launch = new Launch(cmd, [], $"/d /v:off /s /c \"\"{resolvedPath}\" {verb}\"");
				return true;
			}

			launch = new Launch(resolvedPath, [.. arguments, verb], null);
			return true;
		}

		static bool TryFindFile(string path, bool isWindows, IReadOnlyList<string> extensions, Func<string, bool> fileExists, out string resolvedPath)
		{
			if (!isWindows)
			{
				resolvedPath = path;
				return fileExists(path);
			}

			var extension = GetExtension(path);

			if (extension != null && _windowsExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
			{
				resolvedPath = path;
				return fileExists(path);
			}

			// Like cmd.exe: a name without a runnable extension is tried with each PATHEXT extension in order.
			foreach (var candidateExtension in extensions)
			{
				var candidate = path + candidateExtension;

				if (fileExists(candidate))
				{
					resolvedPath = candidate;
					return true;
				}
			}

			if (extension != null && fileExists(path))
			{
				// Found as written, but with an extension Windows cannot start (reported by CheckExtension).
				resolvedPath = path;
				return true;
			}

			resolvedPath = string.Empty;
			return false;
		}

		static bool CheckExtension(string command, string resolvedPath, bool isWindows, out string? error)
		{
			if (isWindows && !_windowsExtensions.Contains(GetExtension(resolvedPath) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
			{
				error = UnsupportedWindowsExtensionMessage(command);
				return false;
			}

			error = null;
			return true;
		}

		static bool HasUnsupportedWindowsExtension(string command)
		{
			var extension = GetExtension(command);
			return extension != null && !_windowsExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
		}

		static string UnsupportedWindowsExtensionMessage(string command)
		{
			return $"Credential helper '{command}' cannot be started on Windows: a helper must be an .exe, .com, .cmd or .bat file. Wrap a PowerShell or other script in a .cmd file, for example: @pwsh -NoProfile -File \"%~dp0helper.ps1\" %1";
		}

		static IReadOnlyList<string> GetWindowsExtensions(string? pathExtVariable)
		{
			var result = new List<string>();

			foreach (var item in (string.IsNullOrWhiteSpace(pathExtVariable) ? DefaultPathExt : pathExtVariable!).Split(';'))
			{
				var extension = item.Trim();

				if (_windowsExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) && !result.Contains(extension, StringComparer.OrdinalIgnoreCase))
					result.Add(extension.ToLowerInvariant());
			}

			return result;
		}

		static string? GetExtension(string path)
		{
			var nameStart = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\')) + 1;
			var dot       = path.LastIndexOf('.');

			return dot > nameStart && dot < path.Length - 1 ? path.Substring(dot) : null;
		}

		static bool HasDirectory(string command, bool isWindows)
		{
			return command.Contains('/', StringComparison.Ordinal) || (isWindows && (command.Contains('\\', StringComparison.Ordinal) || command.Contains(':', StringComparison.Ordinal)));
		}

		static bool IsRooted(string path, bool isWindows)
		{
			if (!isWindows)
				return path.StartsWith('/');

			return path.StartsWith(@"\\", StringComparison.Ordinal)
				|| (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'));
		}
	}
}
