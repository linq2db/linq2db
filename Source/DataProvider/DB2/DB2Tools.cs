using System;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.DB2
{
	using System.Configuration;

	using Data;

	public static class DB2Tools
	{
		static readonly DB2DataProvider _db2DataProviderzOS = new DB2DataProvider(ProviderName.DB2zOS, DB2ServerVersion.zOS);
		static readonly DB2DataProvider _db2DataProviderLUW = new DB2DataProvider(ProviderName.DB2LUW, DB2ServerVersion.LUW);

		public static bool AutoDetectProvider { get; set; }

		static DB2Tools()
		{
			AutoDetectProvider = true;

			DataConnection.AddDataProvider(ProviderName.DB2, _db2DataProviderLUW);
			DataConnection.AddDataProvider(_db2DataProviderLUW);
			DataConnection.AddDataProvider(_db2DataProviderzOS);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(ConnectionStringSettings css)
		{
			if (css.ElementInformation.Source == null ||
			    css.ElementInformation.Source.EndsWith("machine.config", StringComparison.OrdinalIgnoreCase))
				return null;

			switch (css.ProviderName)
			{
				case ""                      :
				case null                    :

					if (css.Name == "DB2")
						goto case "DB2";
					break;

				case "DB2"             :
				case "IBM.Data.DB2" :

					if (css.Name.Contains("LUW") || css.Name.Contains("z/OS") || css.Name.Contains("zOS"))
						break;

					if (AutoDetectProvider)
					{
						/*
						try
						{
							using (var conn = new SqlConnection(css.ConnectionString))
							{
								conn.Open();

								switch (conn.ServerVersion.Split('.')[0])
								{
									case  "8" : return _sqlServerDataProvider2000;
									case  "9" :	return _sqlServerDataProvider2005;
									case "10" :	return _sqlServerDataProvider2008;
									case "11" : return _sqlServerDataProvider2012;
								}
							}
						}
						catch (Exception)
						{
						}
						*/
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(DB2ServerVersion version = DB2ServerVersion.LUW)
		{
			switch (version)
			{
				case DB2ServerVersion.zOS : return _db2DataProviderzOS;
			}

			return _db2DataProviderLUW;
		}

		public static void ResolveDB2(string path)
		{
			new AssemblyResolver(path, "IBM.Data.DB2");
		}

		public static void ResolveDB2(Assembly assembly)
		{
			new AssemblyResolver(assembly, "IBM.Data.DB2");
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, DB2ServerVersion version = DB2ServerVersion.LUW)
		{
			switch (version)
			{
				case DB2ServerVersion.zOS : return new DataConnection(_db2DataProviderzOS, connectionString);
			}

			return new DataConnection(_db2DataProviderLUW, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, DB2ServerVersion version = DB2ServerVersion.LUW)
		{
			switch (version)
			{
				case DB2ServerVersion.zOS : return new DataConnection(_db2DataProviderzOS, connection);
			}

			return new DataConnection(_db2DataProviderLUW, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, DB2ServerVersion version = DB2ServerVersion.LUW)
		{
			switch (version)
			{
				case DB2ServerVersion.zOS : return new DataConnection(_db2DataProviderzOS, transaction);
			}

			return new DataConnection(_db2DataProviderLUW, transaction);
		}

		#endregion
	}
}
