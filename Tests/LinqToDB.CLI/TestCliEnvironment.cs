using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

namespace Tests.LinqToDB.CLI
{
	internal sealed class TestCliEnvironment : ICliEnvironment
	{
		private readonly StringWriter _output = new();
		private readonly StringWriter _error  = new();

		public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
		public HashSet<string> Directories { get; } = new(StringComparer.Ordinal);
		public HashSet<string> OwnerOnlyFiles { get; } = new(StringComparer.Ordinal);
		public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.Ordinal);
		public Dictionary<string, (string User, string Password)> WindowsCredentials { get; } = new(StringComparer.Ordinal);

		public Exception? WriteAllTextException { get; init; }

		public TextWriter Out   => _output;
		public TextWriter Error => _error;

		public int BufferWidth => 120;

		public string Output      => _output.ToString();
		public string ErrorOutput => _error .ToString();

		public bool FileExists(string path)
		{
			return Files.ContainsKey(path) || File.Exists(path);
		}

		public string ReadAllText(string path)
		{
			return Files[path];
		}

		public void WriteAllText(string path, string contents)
		{
			Files[path] = contents;

			if (WriteAllTextException != null)
				throw WriteAllTextException;
		}

		public void SetOwnerOnlyFilePermissions(string path)
		{
			OwnerOnlyFiles.Add(path);
		}

		public TextWriter CreateTextWriter(string path)
		{
			return new TestFileWriter(contents => Files[path] = contents);
		}

		public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
		{
			if (!Files.TryGetValue(sourcePath, out var contents))
				throw new FileNotFoundException("Source file not found.", sourcePath);

			if (!overwrite && Files.ContainsKey(destinationPath))
				throw new IOException($"File '{destinationPath}' already exists.");

			Files[destinationPath] = contents;
			Files.Remove(sourcePath);

			if (OwnerOnlyFiles.Remove(sourcePath))
				OwnerOnlyFiles.Add(destinationPath);
		}

		public void DeleteFile(string path)
		{
			Files.Remove(path);
		}

		public void CreateDirectory(string path)
		{
			Directories.Add(path);
		}

		public string? GetEnvironmentVariable(string name)
		{
			return EnvironmentVariables.GetValueOrDefault(name);
		}

		public bool TryGetWindowsCredentials(string target, out string? user, out string? password, out string? error)
		{
			if (WindowsCredentials.TryGetValue(target, out var credentials))
			{
				user     = credentials.User;
				password = credentials.Password;
				error    = null;
				return true;
			}

			user     = null;
			password = null;
			error    = $"Windows Credential Manager target '{target}' was not found for the current Windows account.";
			return false;
		}

		private sealed class TestFileWriter(Action<string> save) : StringWriter
		{
			private bool _saved;

			public override ValueTask DisposeAsync()
			{
				Save();
				return base.DisposeAsync();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					Save();

				base.Dispose(disposing);
			}

			private void Save()
			{
				if (_saved)
					return;

				save(ToString());
				_saved = true;
			}
		}
	}
}
