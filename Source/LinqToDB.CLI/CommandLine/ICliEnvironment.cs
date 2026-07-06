using System.IO;

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
