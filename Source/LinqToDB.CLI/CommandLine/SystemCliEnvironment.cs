using System;
using System.IO;
using System.Text;

using LinqToDB.CommandLine.Commands.Credentials;

namespace LinqToDB.CommandLine
{
	internal sealed class SystemCliEnvironment : ICliEnvironment
	{
		public static ICliEnvironment Instance { get; } = new SystemCliEnvironment();

		private SystemCliEnvironment()
		{
		}

		public TextWriter Out   => Console.Out;
		public TextWriter Error => Console.Error;

		public int BufferWidth
		{
			get
			{
				try
				{
					return Console.BufferWidth;
				}
				catch
				{
					return 80;
				}
			}
		}

		public bool FileExists(string path)
		{
			return File.Exists(path);
		}

		public string ReadAllText(string path)
		{
			return File.ReadAllText(path);
		}

		public void WriteAllText(string path, string contents)
		{
			File.WriteAllText(path, contents);
		}

		public void SetOwnerOnlyFilePermissions(string path)
		{
			if (!OperatingSystem.IsWindows())
				File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
		}

		public TextWriter CreateTextWriter(string path)
		{
			return File.CreateText(path);
		}

		public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
		{
			File.Move(sourcePath, destinationPath, overwrite);
		}

		public void DeleteFile(string path)
		{
			File.Delete(path);
		}

		public void CreateDirectory(string path)
		{
			Directory.CreateDirectory(path);
		}

		public string? GetEnvironmentVariable(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}

		public bool TryReadSecret(string prompt, out string? secret, out string? error)
		{
			secret = null;

			if (Console.IsInputRedirected)
			{
				error = "Interactive secret input requires a console.";
				return false;
			}

			Error.Write(prompt);

			var value = new StringBuilder();

			while (true)
			{
				var key = Console.ReadKey(true);

				if (key.Key == ConsoleKey.Enter)
				{
					Error.WriteLine();
					secret = value.ToString();
					error  = null;
					return true;
				}

				if (key.Key == ConsoleKey.Backspace)
				{
					if (value.Length > 0)
						value.Length--;

					continue;
				}

				if (!char.IsControl(key.KeyChar))
					value.Append(key.KeyChar);
			}
		}

		public ICredentialStore CredentialStore { get; } = WindowsCredentialStore.Instance;

		public string? ReadLine()
		{
			return Console.ReadLine();
		}
	}
}
