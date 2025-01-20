using System.IO;

namespace Tests
{
	public static class TestExternals
	{
		public static string? LogFilePath;
		public static bool    IsParallelRun;
		public static int     RunID;
		public static string? Configuration;

		static StreamWriter? _logWriter;

		public static void Log(string text)
		{
			if (LogFilePath != null)
			{
				_logWriter ??= File.CreateText(LogFilePath);

				_logWriter.WriteLine(text);
				_logWriter.Flush();
			}
		}
	}
}
