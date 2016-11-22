using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB.Configuration;

namespace Tests
{
    public class TxtSettings : ILinqToDBSettings
	{
		class DataProviderSettings : IDataProviderSettings
		{
			public string                  TypeName   { get; set; }
			public string                  Name       { get; set; }
			public bool                    Default    { get; set; }
			public IEnumerable<NamedValue> Attributes { get { return new NamedValue[0]; } }

		}

		class ConnectionStringSettings : IConnectionStringSettings
		{
			public string ConnectionString { get; set; }
			public string Name             { get; set; }
			public string ProviderName     { get; set; }
			public bool   IsGlobal         { get; set; }
		}

		public IEnumerable<IDataProviderSettings>     DataProviders        { get { return _dataProviders; } }
		public string                                 DefaultConfiguration { get; set; }
		public string                                 DefaultDataProvider  { get; set; }
		public IEnumerable<IConnectionStringSettings> ConnectionStrings    { get { return _strings; } }


		private static TxtSettings _instance                         = new TxtSettings();
		private        List<DataProviderSettings>     _dataProviders = new List<DataProviderSettings>();
		private        List<ConnectionStringSettings> _strings       = new List<ConnectionStringSettings>();

		private TxtSettings()
		{

		}

		public static TxtSettings Instance { get { return _instance; } }

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
