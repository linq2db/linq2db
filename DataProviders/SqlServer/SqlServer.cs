using System;
using System.Collections.Specialized;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Data;

	public class SqlServer : IDataProviderFactory
	{
		static readonly SqlServerDataProvider _sqlServerDataProvider2005 = new SqlServerDataProvider(
			ProviderName.SqlServer2005, SqlServerVersion.v2005, new SqlServer2005MappingSchema());
		static readonly SqlServerDataProvider _sqlServerDataProvider2008 = new SqlServerDataProvider(
			ProviderName.SqlServer2008, SqlServerVersion.v2008, new SqlServer2008MappingSchema());
		static readonly SqlServerDataProvider _sqlServerDataProvider2012 = new SqlServerDataProvider(
			ProviderName.SqlServer2012, SqlServerVersion.v2012, new SqlServer2012MappingSchema());

		static SqlServer()
		{
			DataConnection.AddDataProvider(ProviderName.SqlServer, _sqlServerDataProvider2008);
			DataConnection.AddDataProvider(_sqlServerDataProvider2012);
			DataConnection.AddDataProvider(_sqlServerDataProvider2008);
			DataConnection.AddDataProvider(_sqlServerDataProvider2005);
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
				case SqlServerVersion.v2012 : return _sqlServerDataProvider2012;
			}

			return _sqlServerDataProvider2008;
		}

		public static void AddUdtType(Type type, string udtName)
		{
			_sqlServerDataProvider2005.AddUdtType(type, udtName);
			_sqlServerDataProvider2008.AddUdtType(type, udtName);
			_sqlServerDataProvider2012.AddUdtType(type, udtName);
		}

		public static void AddUdtType<T>(string udtName, T nullValue, DataType dataType = DataType.Undefined)
		{
			_sqlServerDataProvider2005.AddUdtType(udtName, nullValue, dataType);
			_sqlServerDataProvider2008.AddUdtType(udtName, nullValue, dataType);
			_sqlServerDataProvider2012.AddUdtType(udtName, nullValue, dataType);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, connectionString);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, connectionString);
			}

			return new DataConnection(_sqlServerDataProvider2008, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, connection);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, connection);
			}

			return new DataConnection(_sqlServerDataProvider2008, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, transaction);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, transaction);
			}

			return new DataConnection(_sqlServerDataProvider2008, transaction);
		}

		#endregion
	}
}
