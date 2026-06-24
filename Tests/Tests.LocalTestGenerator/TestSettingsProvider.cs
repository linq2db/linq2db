using Tests.Tools;

namespace Tests.LocalTestGenerator
{
	internal sealed class TestSettingsProvider
	{
		private readonly string  _dataProvidersJson;
		private readonly string? _userDataProvidersJson;

		public TestSettingsProvider(TestConfigurationFiles files)
		{
			Files = files;

			_dataProvidersJson     = File.ReadAllText(files.DataProvidersJsonFile);
			_userDataProvidersJson = files.UserDataProvidersJsonFile == null ? null : File.ReadAllText(files.UserDataProvidersJsonFile);
		}

		public TestConfigurationFiles Files { get; }

		public TestSettings GetSettings(string configurationName)
		{
			var settings = SettingsReader.Deserialize(configurationName, _dataProvidersJson, _userDataProvidersJson);

			settings.Connections ??= new();

			return settings;
		}
	}
}
