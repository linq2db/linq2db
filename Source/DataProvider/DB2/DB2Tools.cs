using System;
using System.Data;
using System.Reflection;

namespace LinqToDB.DataProvider.DB2
{
	using System.Configuration;
	using System.Linq;
	using System.Linq.Expressions;

	using Data;

	public static class DB2Tools
	{
		static readonly DB2DataProvider _db2DataProviderzOS = new DB2DataProvider(ProviderName.DB2zOS, DB2Version.zOS);
		static readonly DB2DataProvider _db2DataProviderLUW = new DB2DataProvider(ProviderName.DB2LUW, DB2Version.LUW);

		public static bool AutoDetectProvider { get; set; }

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.MultipleRows;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

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
				case ""             :
				case null           :

					if (css.Name == "DB2")
						goto case "DB2";
					break;

				case "DB2"          :
				case "IBM.Data.DB2" :

					if (css.Name.Contains("LUW") || css.Name.Contains("z/OS") || css.Name.Contains("zOS"))
						break;

					if (AutoDetectProvider)
					{
						try
						{
							var connectionType = Type.GetType("IBM.Data.DB2.DB2Connection, IBM.Data.DB2", true);
							var serverTypeProp = connectionType
								.GetProperties (BindingFlags.NonPublic | BindingFlags.Instance)
								.FirstOrDefault(p => p.Name == "eServerType");

							if (serverTypeProp != null)
							{
								var connectionCreator = DynamicDataProviderBase.CreateConnectionExpression(connectionType).Compile();

								using (var conn = connectionCreator(css.ConnectionString))
								{
									conn.Open();

									var serverType = Expression.Lambda<Func<object>>(
										Expression.Convert(
											Expression.MakeMemberAccess(Expression.Constant(conn), serverTypeProp),
											typeof(object)))
										.Compile()();

									var iszOS = serverType.ToString() == "DB2_390";

									return iszOS ? _db2DataProviderzOS : _db2DataProviderLUW;
								}
							}
						}
						catch (Exception)
						{
						}
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return _db2DataProviderzOS;
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

		public static DataConnection CreateDataConnection(string connectionString, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, connectionString);
			}

			return new DataConnection(_db2DataProviderLUW, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, connection);
			}

			return new DataConnection(_db2DataProviderLUW, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, DB2Version version = DB2Version.LUW)
		{
			switch (version)
			{
				case DB2Version.zOS : return new DataConnection(_db2DataProviderzOS, transaction);
			}

			return new DataConnection(_db2DataProviderLUW, transaction);
		}

		#endregion
	}
}
