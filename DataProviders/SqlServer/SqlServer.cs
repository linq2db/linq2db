using System;
using System.Collections.Specialized;

namespace LinqToDB.DataProvider
{
	using Data;

	public class SqlServer : IDataProviderFactory
	{
		static readonly SqlServerDataProvider _sqlServerDataProvider2005 = new SqlServerDataProvider2005();
		static readonly SqlServerDataProvider _sqlServerDataProvider2008 = new SqlServerDataProvider2008();
		static readonly SqlServerDataProvider _sqlServerDataProvider2012 = new SqlServerDataProvider2012();

		static SqlServer()
		{
			DataConnection.AddDataProvider(     _sqlServerDataProvider2008);
			//DataConnection.AddDataProvider(ProviderName.SqlServer,     _sqlServerDataProvider2008);
			DataConnection.AddDataProvider(ProviderName.SqlServer2012, _sqlServerDataProvider2012);
			DataConnection.AddDataProvider(ProviderName.SqlServer2008, _sqlServerDataProvider2008);
			DataConnection.AddDataProvider(ProviderName.SqlServer2005, _sqlServerDataProvider2005);
		}

		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
			{
				if (attributes.GetKey(i) == "version")
				{
					switch (attributes.Get(i))
					{
						case "2005" : return _sqlServerDataProvider2005;
						case "2008" : return _sqlServerDataProvider2008;
						case "2012" : return _sqlServerDataProvider2012;
					}
				}
			}

			return _sqlServerDataProvider2008;
		}

		public static IDataProvider GetDataProvider(SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2005 : return _sqlServerDataProvider2005;
				case SqlServerVersion.v2008 : return _sqlServerDataProvider2008;
				case SqlServerVersion.v2012 : return _sqlServerDataProvider2012;
			}

			throw new ArgumentException("Invalid argument 'version'.", "version");
		}
	}
}
