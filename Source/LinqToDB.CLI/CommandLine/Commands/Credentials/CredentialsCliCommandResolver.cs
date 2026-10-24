using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Splits a configured credentials CLI command line, resolves its program to an absolute file and decides how to start
	/// it. Pure: the file system and environment are passed in, so Windows rules are testable on every OS.
	/// </summary>
	internal static class CredentialsCliCommandResolver
	{
		/// <summary>
		/// How to start a credentials CLI: the program and the raw argument string handed to the process.
		/// </summary>
		internal sealed record Launch(string FileName, string Arguments);

		/// <summary>Extensions a Windows program may have; PATHEXT is filtered to these.</summary>
		static readonly string[] _windowsExtensions = [".com", ".exe", ".bat", ".cmd"];

		const string DefaultPathExt = ".COM;.EXE;.BAT;.CMD";

		/// <summary>
		/// Splits a command line into the program and the rest. The program is the first word: up to the first space or tab,
		/// or, when the line starts with <c>"</c>, up to the next <c>"</c>. The rest, without the separating whitespace, is
		/// the argument string; it is never parsed here (a quote in it is the program's business).
		/// </summary>
		public static bool TryParseCommandLine(string commandLine, out string program, out string arguments, out string? error)
		{
			program   = string.Empty;
			arguments = string.Empty;

			if (string.IsNullOrWhiteSpace(commandLine))
			{
				error = "The credentials CLI command line is empty: it names the program and its arguments.";
				return false;
			}

			if (commandLine.Any(static c => char.IsControl(c) && c != '\t'))
			{
				error = "The credentials CLI command line must not contain line breaks or other control characters.";
				return false;
			}

			var line = commandLine.TrimStart(' ', '\t');
			int end;

			if (line[0] == '"')
			{
				end = line.IndexOf('"', 1);

				if (end < 0)
				{
					var position = (commandLine.Length - line.Length + 1).ToString(CultureInfo.InvariantCulture);
					error = $"The credentials CLI command line '{commandLine}' has an unterminated quote at position {position}: the program is quoted up to the next '\"'.";
					return false;
				}

				program = line.Substring(1, end - 1);
				end++;

				if (end < line.Length && line[end] != ' ' && line[end] != '\t')
				{
					error = $"The credentials CLI command line '{commandLine}' must have a space or tab after the quoted program.";
					return false;
				}
			}
			else
			{
				end     = line.IndexOfAny([' ', '\t']);
				end     = end < 0 ? line.Length : end;
				program = line.Substring(0, end);
			}

			if (string.IsNullOrWhiteSpace(program))
			{
				error = $"The credentials CLI command line '{commandLine}' does not name a program.";
				return false;
			}

			arguments = line.Substring(end).Trim(' ', '\t');
			error     = null;
			return true;
		}

		public static bool TryResolve(
			string             program,
			string?            baseDirectory,
			bool               isWindows,
			string?            pathVariable,
			string?            pathExtVariable,
			Func<string, bool> fileExists,
			out string         resolvedPath,
			out string?        error)
		{
			resolvedPath = string.Empty;

			if (string.IsNullOrWhiteSpace(program) || program.Any(char.IsControl))
			{
				error = "The credentials CLI program must be a non-empty path or file name without control characters.";
				return false;
			}

			var extensions = isWindows ? GetWindowsExtensions(pathExtVariable) : [];

			if (HasDirectory(program, isWindows))
			{
				var path = program;

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
					return CheckExtension(program, resolvedPath, isWindows, out error);

				error = NotFoundMessage(program, $"looked for '{path}'");
				return false;
			}

			foreach (var directory in (pathVariable ?? string.Empty).Split(isWindows ? ';' : ':'))
			{
				var entry = directory.Trim();

				if (isWindows && entry.Length > 1 && entry[0] == '"' && entry[^1] == '"')
					entry = entry.Substring(1, entry.Length - 2);

				// Empty and relative PATH entries name the current directory; a program is never looked up there.
				if (entry.Length == 0 || !IsRooted(entry, isWindows))
					continue;

				var candidate = isWindows
					? entry.TrimEnd('\\', '/') + "\\" + program
					: entry.TrimEnd('/')       + "/"  + program;

				if (TryFindFile(candidate, isWindows, extensions, fileExists, out resolvedPath))
					return CheckExtension(program, resolvedPath, isWindows, out error);
			}

			error = isWindows && HasUnsupportedWindowsExtension(program)
				? UnsupportedWindowsExtensionMessage(program)
				: NotFoundMessage(program, "looked up on PATH; the current directory is never searched");
			return false;
		}

		static string NotFoundMessage(string program, string detail)
		{
			// Nothing in the value is expanded: a '~', '$HOME' or '%VAR%' reaches the file system as written.
			var note = program.IndexOfAny(['~', '$', '%']) >= 0
				? " '~', '$HOME' and '%VAR%' are not expanded in the credentials CLI command line; write the full path."
				: string.Empty;

			return $"Credentials CLI program '{program}' was not found ({detail}).{note}";
		}

		/// <summary>
		/// Returns how to start the program for a verb. The argument string is passed unchanged and the verb is appended as
		/// a separate final argument; no shell is involved. On Unix .NET splits the string into arguments by the Windows
		/// (MSVCRT) rules: whitespace separates, <c>"..."</c> groups, a backslash escapes a quote. Windows <c>.cmd</c>/<c>.bat</c>
		/// files run as <c>cmd.exe /d /v:off /s /c ""script" arguments verb"</c>: with <c>/s</c> cmd.exe removes only the outer
		/// quotes, so a script path with spaces and parentheses (<c>C:\Program Files (x86)\...</c>) stays intact, and
		/// <c>/d</c> skips AutoRun commands that would write to standard output. Everything else is started directly.
		/// </summary>
		public static Launch GetLaunch(string resolvedPath, string arguments, string verb, bool isWindows, string? systemDirectory)
		{
			var tail = arguments.Length == 0 ? verb : arguments + " " + verb;

			if (isWindows)
			{
				var extension = GetExtension(resolvedPath);

				if (string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase))
				{
					var cmd = (systemDirectory ?? @"C:\Windows\System32").TrimEnd('\\') + @"\cmd.exe";
					return new Launch(cmd, $"/d /v:off /s /c \"\"{resolvedPath}\" {tail}\"");
				}
			}

			return new Launch(resolvedPath, tail);
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

		static bool CheckExtension(string program, string resolvedPath, bool isWindows, out string? error)
		{
			if (isWindows && !_windowsExtensions.Contains(GetExtension(resolvedPath) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
			{
				error = UnsupportedWindowsExtensionMessage(resolvedPath);
				return false;
			}

			error = null;
			return true;
		}

		static bool HasUnsupportedWindowsExtension(string program)
		{
			var extension = GetExtension(program);
			return extension != null && !_windowsExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// The message for a program Windows cannot start itself, with the interpreter command line that runs it.
		/// </summary>
		internal static string UnsupportedWindowsExtensionMessage(string program)
		{
			return $"Credentials CLI program '{program}' is not a valid application on Windows: a program must be an .exe, .com, .cmd or .bat file. {GetInterpreterHint(program)}";
		}

		/// <summary>The command line that starts a script through its interpreter, by the script's extension.</summary>
		internal static string GetInterpreterHint(string program)
		{
			var extension = GetExtension(program)?.ToLowerInvariant();
			var quoted    = $"\"{program}\"";

			var (what, commandLine) = extension switch
			{
				".ps1" => ("a PowerShell script through PowerShell", $"pwsh -NoProfile -File {quoted}"),
				".py"  => ("a Python script through Python",         $"python {quoted}"),
				".cs"  => ("a C# file-based app through dotnet",     $"dotnet run --file {quoted} --"),
				_      => (null, null),
			};

			if (commandLine == null)
				return "Run a script through its interpreter, for example \"pwsh -NoProfile -File <file>\" or \"python <file>\".";

			var json = commandLine.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
			return $"Run {what}: \"credentialsCli\": \"{json}\".";
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

		internal static string? GetExtension(string path)
		{
			var nameStart = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\')) + 1;
			var dot       = path.LastIndexOf('.');

			return dot > nameStart && dot < path.Length - 1 ? path.Substring(dot) : null;
		}

		static bool HasDirectory(string program, bool isWindows)
		{
			return program.Contains('/', StringComparison.Ordinal) || (isWindows && (program.Contains('\\', StringComparison.Ordinal) || program.Contains(':', StringComparison.Ordinal)));
		}

		internal static bool IsRooted(string path, bool isWindows)
		{
			if (!isWindows)
				return path.StartsWith('/');

			return path.StartsWith(@"\\", StringComparison.Ordinal)
				|| (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'));
		}
	}
}
