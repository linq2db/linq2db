using System;
using System.IO;

using LinqToDB.CommandLine.Commands.Credentials;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Provides runtime services used by CLI commands.
	/// </summary>
	public interface ICliEnvironment
	{
		/// <summary>Standard output writer.</summary>
		TextWriter Out   { get; }
		/// <summary>Diagnostic output writer.</summary>
		TextWriter Error { get; }

		/// <summary>Available console buffer width.</summary>
		int BufferWidth { get; }
		/// <summary>
		/// Windows Credential Manager: the default credential store on Windows, and <c>@credential-manager</c>.
		/// </summary>
		ICredentialStore CredentialStore { get; }
		/// <summary>
		/// Creates the built-in local store in a credentials directory: the default credential store on Linux and macOS,
		/// and <c>@local</c>.
		/// </summary>
		ICredentialStore CreateLocalCredentialStore(string directory);
		/// <summary>Creates a credential store backed by a credentials CLI.</summary>
		ICredentialStore CreateCredentialsCliStore(CredentialsCliSettings settings);

		/// <summary>Checks whether a file exists.</summary>
		bool FileExists(string path);
		/// <summary>Reads all text from a file.</summary>
		string ReadAllText(string path);
		/// <summary>Writes all text to a file.</summary>
		void WriteAllText(string path, string contents);
		/// <summary>Restricts file access to the current user on platforms that support Unix file modes.</summary>
		void SetOwnerOnlyFilePermissions(string path);
		/// <summary>Creates a text writer for a file.</summary>
		TextWriter CreateTextWriter(string path);
		/// <summary>Moves a file to a destination path.</summary>
		void MoveFile(string sourcePath, string destinationPath, bool overwrite);
		/// <summary>Deletes a file.</summary>
		void DeleteFile(string path);
		/// <summary>Creates a directory.</summary>
		void CreateDirectory(string path);
		/// <summary>Reads an environment variable.</summary>
		string? GetEnvironmentVariable(string name);
		/// <summary>Reads a secret from the interactive console, echoing a mask character per typed character.</summary>
		bool TryReadSecret(string prompt, out string? secret, out string? error);
		/// <summary>Reads one line from standard input.</summary>
		string? ReadLine();
	}
}
