using System;
using System.IO;

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

		public TextWriter CreateTextWriter(string path)
		{
			return File.CreateText(path);
		}

		public string? GetEnvironmentVariable(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}
	}
}
