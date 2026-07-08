using System;
using System.IO;

using LinqToDB.CommandLine.Options;
using LinqToDB.CommandLine.Commands.Skill;
using LinqToDB.CommandLine.Commands.Query;
using LinqToDB.CommandLine.Commands;

namespace LinqToDB.CommandLine
{
	internal interface ICliEnvironment
	{
		TextWriter Out   { get; }
		TextWriter Error { get; }

		int BufferWidth { get; }

		bool FileExists(string path);
		string ReadAllText(string path);
		void WriteAllText(string path, string contents);
		TextWriter CreateTextWriter(string path);
		string? GetEnvironmentVariable(string name);
	}
}
