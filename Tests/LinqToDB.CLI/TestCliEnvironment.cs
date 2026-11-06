using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Credentials;

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
		public Queue<string> Secrets { get; } = new();
		public Queue<string> InputLines { get; } = new();

		public Exception? WriteAllTextException { get; init; }

		public TextWriter Out   => _output;
		public TextWriter Error => _error;

		public int BufferWidth => 120;

		public TestCliEnvironment()
		{
			// The OS-default local store needs a resolvable credentials directory; with the default (fake) local store
			// nothing is created there.
			EnvironmentVariables[CredentialsDirectory.Variable] = Path.Combine(Path.GetTempPath(), "linq2db-cli-tests-unused-credentials");
		}

		/// <summary>Windows Credential Manager (and, unless <see cref="UseRealLocalStore"/> is set, the local store).</summary>
		public ICredentialStore CredentialStore { get; } = new TestCredentialStore();
		public Dictionary<string, (string User, string Password)> Credentials => ((TestCredentialStore)CredentialStore).Credentials;
		public HashSet<string> UnreadableCredentialTargets => ((TestCredentialStore)CredentialStore).UnreadableTargets;

		/// <summary>Use the real local store in the credentials directory instead of the in-memory store.</summary>
		public bool UseRealLocalStore { get; set; }

		/// <summary>Extra environment for credentials CLI processes (store file, fake tools on PATH).</summary>
		public Dictionary<string, string?> CredentialsCliEnvironment { get; } = new(StringComparer.Ordinal);
		/// <summary>Whether credentials CLI runs are interactive (LINQ2DB_CREDENTIAL_INTERACTIVE=1).</summary>
		public bool InteractiveCredentialsCli { get; set; }

		/// <summary>Fails <see cref="MoveFile"/> for a destination: returns the exception to throw, or <see langword="null"/>.</summary>
		public Func<string, Exception?>? MoveFileFault { get; set; }

		public ICredentialStore CreateLocalCredentialStore(string directory)
		{
			return UseRealLocalStore
				? new LocalCredentialStore(directory, EnvironmentVariables.GetValueOrDefault("USERPROFILE"))
				: CredentialStore;
		}

		public ICredentialStore CreateCredentialsCliStore(CredentialsCliSettings settings)
		{
			// The example credentials CLI runs through "dotnet run": allow for a slow machine.
			return new CredentialsCliStore(new CredentialsCliProcessRunner(settings, InteractiveCredentialsCli, CredentialsCliEnvironment)
			{
				Timeout            = TimeSpan.FromSeconds(120),
				InteractiveTimeout = TimeSpan.FromSeconds(120),
			});
		}

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
			if (MoveFileFault?.Invoke(destinationPath) is { } fault)
				throw fault;

			// Files written by the command itself (generated scripts) live on the real file system.
			if (!Files.ContainsKey(sourcePath) && File.Exists(sourcePath))
			{
				File.Move(sourcePath, destinationPath, overwrite);
				return;
			}

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

		public bool TryReadSecret(string prompt, out string? secret, out string? error)
		{
			_error.Write(prompt);

			if (Secrets.TryDequeue(out secret))
			{
				error = null;
				return true;
			}

			error = "Interactive secret input is not available.";
			return false;
		}

		public string? ReadLine()
		{
			return InputLines.TryDequeue(out var line) ? line : null;
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
