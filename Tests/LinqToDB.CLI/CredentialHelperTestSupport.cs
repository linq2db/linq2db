using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Shared helpers for credential helper tests: private temporary directories, per-OS helper scripts, and the reference
	/// helper (<c>secrethelper.cs</c>) built once per test run.
	/// </summary>
	internal static class CredentialHelperTestSupport
	{
		static readonly Lock _buildLock = new();
		static string? _secretHelperPath;

		/// <summary>Root for every directory these tests create, private to the current user.</summary>
		public static string Root
		{
			get
			{
				var root = Path.Combine(Path.GetTempPath(), $"linq2db-cli-tests-{Environment.UserName}");
				CreatePrivateDirectory(root);
				return root;
			}
		}

		public static string CreateTempDirectory()
		{
			var directory = Path.Combine(Root, Guid.NewGuid().ToString("N"));
			CreatePrivateDirectory(directory);
			return directory;
		}

		public static void DeleteDirectory(string directory)
		{
			try
			{
				if (Directory.Exists(directory))
					Directory.Delete(directory, true);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				TestContext.Out.WriteLine($"Cannot delete '{directory}': {ex.Message}");
			}
		}

		static void CreatePrivateDirectory(string directory)
		{
			if (OperatingSystem.IsWindows())
				Directory.CreateDirectory(directory);
			else
				Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
		}

		/// <summary>
		/// Writes a helper script: <c>&lt;name&gt;.sh</c> with <paramref name="sh"/> on POSIX, <c>&lt;name&gt;.cmd</c> with
		/// <paramref name="cmd"/> on Windows. Returns its path.
		/// </summary>
		public static string WriteScript(string directory, string name, string sh, string cmd)
		{
			if (OperatingSystem.IsWindows())
			{
				var path = Path.Combine(directory, name + ".cmd");
				File.WriteAllText(path, cmd.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal));
				return path;
			}
			else
			{
				var path = Path.Combine(directory, name + ".sh");
				File.WriteAllText(path, sh.Replace("\r\n", "\n", StringComparison.Ordinal));
				File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
				return path;
			}
		}

		public static byte[] Request(params string[] lines)
		{
			return Encoding.UTF8.GetBytes(string.Concat(lines.Select(static line => line + "\n")));
		}

		public static CredentialHelperProcessRunner CreateRunner(string command, bool interactive = false, IReadOnlyDictionary<string, string?>? environment = null)
		{
			return new CredentialHelperProcessRunner(new CredentialHelperSettings(command, CredentialHelperProtocol.Linq2Db, null), interactive, environment);
		}

		/// <summary>The command that runs the reference helper: <c>dotnet</c> on PATH.</summary>
		public const string DotnetCommand = "dotnet";

		/// <summary>Timeout for runs of the reference helper: its first <c>dotnet run</c> builds it.</summary>
		public const int SecretHelperTimeoutSeconds = 300;

		/// <summary>
		/// Returns the path of a private copy of the reference helper <c>secrethelper.cs</c>, run as <c>dotnet run
		/// &lt;file&gt; -- &lt;verb&gt;</c> exactly as its documentation shows. The copy's directory is keyed by the source
		/// content, so the SDK's build cache for it is reused by later tests and runs; the first call warms that cache.
		/// </summary>
		public static string GetSecretHelperSource()
		{
			lock (_buildLock)
			{
				if (_secretHelperPath != null)
					return _secretHelperPath;

				var content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "CredentialHelpers", "secrethelper.cs"));
				var hash    = Convert.ToHexString(SHA256.HashData(content)).Substring(0, 16).ToLowerInvariant();
				var dir     = Path.Combine(Root, $"secrethelper-{hash}");
				var source  = Path.Combine(dir, "secrethelper.cs");

				CreatePrivateDirectory(dir);

				using (AcquireFileLock(Path.Combine(dir, "build.lock")))
				{
					if (!File.Exists(source))
						File.WriteAllBytes(source, content);

					// Warm-up run: builds the file-based app on first use, so the tests themselves measure warm runs.
					var store     = Path.Combine(dir, "warmup.json");
					var startInfo = new ProcessStartInfo(DotnetCommand)
					{
						UseShellExecute        = false,
						RedirectStandardInput  = true,
						RedirectStandardOutput = true,
						RedirectStandardError  = true,
						WorkingDirectory       = dir,
					};

					foreach (var argument in new[] { "run", source, "--", "list" })
						startInfo.ArgumentList.Add(argument);

					startInfo.Environment["SECRETHELPER_FILE"] = store;

					using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start dotnet run.");

					process.StandardInput.Write("protocol=1\n");
					process.StandardInput.Close();

					var stdout = process.StandardOutput.ReadToEndAsync();
					var stderr = process.StandardError.ReadToEndAsync();

					if (!process.WaitForExit(TimeSpan.FromSeconds(SecretHelperTimeoutSeconds)))
					{
						process.Kill(entireProcessTree: true);
						throw new InvalidOperationException("The warm-up run of secrethelper.cs timed out.");
					}

					if (process.ExitCode != 0)
						throw new InvalidOperationException($"The warm-up run of secrethelper.cs failed with exit code {process.ExitCode}:\n{stdout.Result}\n{stderr.Result}");
				}

				return _secretHelperPath = source;
			}
		}

		/// <summary>
		/// The <c>credentialHelper</c> configuration value that runs the reference helper with <c>dotnet run</c>.
		/// </summary>
		public static string SecretHelperConfigJson()
		{
			var source = GetSecretHelperSource().Replace("\\", "\\\\", StringComparison.Ordinal);
			return $$"""{ "command": "{{DotnetCommand}}", "args": ["run", "{{source}}", "--"], "timeout": {{SecretHelperTimeoutSeconds}} }""";
		}

		/// <summary>
		/// Writes a wrapper script that points the reference helper at <paramref name="storeFile"/> and runs it with
		/// <c>dotnet run</c> (for <c>--credential-helper</c>, which takes a command without arguments). Returns its path.
		/// </summary>
		public static string WriteSecretHelperWrapper(string directory, string storeFile)
		{
			var source = GetSecretHelperSource();

			return WriteScript(
				directory,
				"secrethelper-wrapper",
				$"SECRETHELPER_FILE='{storeFile}'\nexport SECRETHELPER_FILE\nexec {DotnetCommand} run '{source}' -- \"$1\"\n",
				$"@echo off\r\nset \"SECRETHELPER_FILE={storeFile}\"\r\n{DotnetCommand} run \"{source}\" -- %1\r\nexit /b %ERRORLEVEL%\r\n");
		}

		static FileStream AcquireFileLock(string path)
		{
			for (var attempt = 0; ; attempt++)
			{
				try
				{
					return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				}
				catch (IOException) when (attempt < 600)
				{
					Thread.Sleep(500);
				}
			}
		}
	}
}
