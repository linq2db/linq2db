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
			throw new NotSupportedException("MCP query execution does not support file output.");
		}

		public TextWriter CreateTextWriter(string path)
		{
			throw new NotSupportedException("MCP query execution does not support file output.");
		}

		public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
		{
			throw new NotSupportedException("MCP query execution does not support file output.");
		}

		public void DeleteFile(string path)
		{
			throw new NotSupportedException("MCP query execution does not support file output.");
		}

		public void CreateDirectory(string path)
		{
			_inner.CreateDirectory(path);
		}

		public string? GetEnvironmentVariable(string name)
		{
			return _inner.GetEnvironmentVariable(name);
		}
	}
}
