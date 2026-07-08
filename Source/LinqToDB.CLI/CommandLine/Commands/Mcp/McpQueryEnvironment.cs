using System;
using System.IO;

using LinqToDB.CommandLine;

namespace LinqToDB.CommandLine.Commands.Mcp
{
	/// <summary>
	/// Query resolver environment for MCP tool calls.
	/// </summary>
	sealed class McpQueryEnvironment(TextWriter error) : ICliEnvironment
	{
		readonly ICliEnvironment _inner = SystemCliEnvironment.Instance;

		public TextWriter Out   => TextWriter.Null;
		public TextWriter Error { get; } = error;

		public int BufferWidth => _inner.BufferWidth;

		public bool FileExists(string path)
		{
			return _inner.FileExists(path);
		}

		public string ReadAllText(string path)
		{
			return _inner.ReadAllText(path);
		}

		public void WriteAllText(string path, string contents)
		{
			_inner.WriteAllText(path, contents);
		}

		public TextWriter CreateTextWriter(string path)
		{
			return _inner.CreateTextWriter(path);
		}

		public string? GetEnvironmentVariable(string name)
		{
			return _inner.GetEnvironmentVariable(name);
		}
	}
}
