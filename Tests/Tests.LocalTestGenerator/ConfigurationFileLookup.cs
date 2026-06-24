using System;

namespace Tests.LocalTestGenerator
{
	public sealed record TestConfigurationFiles(
		string  DataProvidersJsonFile,
		string? UserDataProvidersJsonFile);

	internal static class ConfigurationFileLookup
	{
		public static TestConfigurationFiles GetConfigurationFiles(string startDirectory)
		{
			var dataProvidersJsonFile = GetFilePath(startDirectory, "DataProviders.json")
			                            ?? throw new InvalidOperationException("DataProviders.json not found.");

			var userDataProvidersJsonFile = GetFilePath(startDirectory, "UserDataProviders.json");

			return new TestConfigurationFiles(
				dataProvidersJsonFile,
				userDataProvidersJsonFile);
		}

		private static string? GetFilePath(string basePath, string findFileName)
		{
			var path = Path.GetFullPath(basePath);

			while (path != null)
			{
				var fileName = Path.GetFullPath(Path.Combine(path, findFileName));

				if (File.Exists(fileName))
					return fileName;

				path = Path.GetDirectoryName(path);
			}

			return null;
		}
	}
}
