using System;
using System.IO;
using System.Runtime.Versioning;
using System.Text;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Starter credential helper scripts written by <c>credentials helper init</c>. They are POSIX sh scripts over a
	/// platform secret store CLI, contain no secrets, and are the user's to review and edit.
	/// </summary>
	internal static class CredentialHelperTemplates
	{
		public static string? Get(string backend)
		{
			var resourceName = backend switch
			{
				"secret-tool" => "LinqToDB.CLI.CredentialHelpers.secret-tool.sh",
				"pass"        => "LinqToDB.CLI.CredentialHelpers.pass.sh",
				_             => null,
			};

			if (resourceName == null)
				return null;

			using var stream = typeof(CredentialHelperTemplates).Assembly.GetManifestResourceStream(resourceName)
				?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
			using var reader = new StreamReader(stream, Encoding.UTF8);

			// sh needs LF line endings whatever the checkout of the template used.
			return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
		}

		public static string GetBackendCommand(string backend)
		{
			return string.Equals(backend, "pass", StringComparison.Ordinal) ? "pass" : "secret-tool";
		}

		public static string GetBackendPackage(string backend)
		{
			return string.Equals(backend, "pass", StringComparison.Ordinal)
				? "package 'pass', plus an initialized password store: pass init <gpg-id>"
				: "package 'libsecret-tools' on Debian/Ubuntu, 'libsecret' on Fedora/Arch";
		}

		/// <summary>
		/// Writes the script owner-only (0700) in an owner-only directory when the directory is created. The file is
		/// created with its final mode and then renamed into place, so it is never readable or writable by others.
		/// </summary>
		[UnsupportedOSPlatform("windows")]
		public static bool TryWrite(string path, string contents, bool force, out string? error)
		{
			try
			{
				if (Directory.Exists(path))
				{
					error = $"Cannot write credential helper script '{path}': it is a directory.";
					return false;
				}

				if (File.Exists(path) && !force)
				{
					error = $"Credential helper script '{path}' already exists. Use '--force' to replace it.";
					return false;
				}

				var directory = Path.GetDirectoryName(path)!;

				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

				var temporaryFile = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

				try
				{
					using (var stream = new FileStream(temporaryFile, new FileStreamOptions
					{
						Mode           = FileMode.CreateNew,
						Access         = FileAccess.Write,
						Share          = FileShare.None,
						UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
					}))
					{
						var bytes = new UTF8Encoding(false).GetBytes(contents);
						stream.Write(bytes, 0, bytes.Length);
					}

					File.Move(temporaryFile, path, overwrite: true);
				}
				finally
				{
					if (File.Exists(temporaryFile))
						File.Delete(temporaryFile);
				}

				error = null;
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot write credential helper script '{path}': {ex.Message}";
				return false;
			}
		}
	}
}
