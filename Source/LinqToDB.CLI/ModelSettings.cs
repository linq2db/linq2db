using Microsoft.Extensions.Configuration;

namespace LinqToDB.CodeGen.Configuration
{
	public class ModelSettings : IModelSettings
	{
		private readonly IConfiguration _configuration;

		public ModelSettings(IConfiguration configuration)
		{
			_configuration = configuration;

			_providerName = configuration.GetValue(SettingsKeys.PROVIDER_NAME, string.Empty);
			_connectionString = configuration.GetValue(SettingsKeys.CONNECTION_STRING, string.Empty);
		}

		private readonly string _providerName;
		private readonly string _connectionString;

		string IModelSettings.Provider => _providerName;
		string IModelSettings.ConnectionString => _connectionString;
	}
}
