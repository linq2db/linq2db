using System;
using System.Collections.Generic;
using System.Text;
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
			_includeSchemas = configuration.GetValue(SettingsKeys.INCLUDE_SCHEMAS, false);
			_schemas = new HashSet<string>(configuration.GetValue(SettingsKeys.SCHEMAS, Array.Empty<string>()));
		}

		private readonly string _providerName;
		private readonly string _connectionString;
		private readonly HashSet<string> _schemas;
		private readonly bool _includeSchemas;

		string IModelSettings.Provider => _providerName;

		string IModelSettings.ConnectionString => _connectionString;

		bool IModelSettings.IncludeSchemas => _includeSchemas;

		ISet<string> IModelSettings.Schemas => _schemas;
	}
}
