using System;
using System.IO;

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

		/// <summary>Checks whether a file exists.</summary>
		bool FileExists(string path);
		/// <summary>Reads all text from a file.</summary>
		string ReadAllText(string path);
		/// <summary>Writes all text to a file.</summary>
		void WriteAllText(string path, string contents);
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
	}
}
