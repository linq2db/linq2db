﻿using System.Collections.Generic;

using LinqToDB.Configuration;

namespace Tests
{
	public class TxtSettings : ILinqToDBSettings
	{
		sealed class DataProviderSettings : IDataProviderSettings
		{
			public string                  TypeName   { get; set; } = null!;
			public string?                 Name       { get; set; }
			public bool                    Default    { get; set; }
			public IEnumerable<NamedValue> Attributes => [];
		}

		sealed class ConnectionStringSettings : IConnectionStringSettings
		{
			public string  ConnectionString { get; set; } = null!;
			public string  Name             { get; set; } = null!;
			public string? ProviderName     { get; set; }
			public bool    IsGlobal         { get; set; }
		}

		public IEnumerable<IDataProviderSettings>     DataProviders => _dataProviders;
		public string?                                DefaultConfiguration { get; set; }
		public string?                                DefaultDataProvider  { get; set; }
		public IEnumerable<IConnectionStringSettings> ConnectionStrings => _strings;

		readonly List<DataProviderSettings>     _dataProviders = new List<DataProviderSettings>();
		readonly List<ConnectionStringSettings> _strings       = new List<ConnectionStringSettings>();

		TxtSettings()
		{
		}

		public static TxtSettings Instance { get; } = new TxtSettings();

		public void AddConnectionString(string name, string providerName, string connectionString)
		{
			var s = new ConnectionStringSettings
			{
				ConnectionString = connectionString,
				Name             = name,
				ProviderName     = providerName,
				IsGlobal         = false
			};

			_strings.Add(s);
		}
	}
}
