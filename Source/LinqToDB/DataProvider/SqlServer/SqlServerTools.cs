#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Configuration;
	using Data;

	public static class SqlServerTools
	{
		#region Init

		public static SqlServerProvider Provider = SqlServerProvider.SystemDataSqlClient;

		private static readonly Func<string, string> _quoteIdentifier;

		// System.Data
		// and/or
		// System.Data.SqlClient
		static readonly SqlServerDataProvider _sqlServerDataProvider2000sdc = new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000, SqlServerProvider.SystemDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2005sdc = new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.SystemDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2008sdc = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2012sdc = new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2017sdc = new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.SystemDataSqlClient);

		// Microsoft.Data.SqlClient
		static readonly SqlServerDataProvider _sqlServerDataProvider2000mdc = new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000, SqlServerProvider.MicrosoftDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2005mdc = new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2008mdc = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.MicrosoftDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2012mdc = new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
		static readonly SqlServerDataProvider _sqlServerDataProvider2017mdc = new SqlServerDataProvider(ProviderName.SqlServer2017, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient);

		public static bool AutoDetectProvider { get; set; }

		static SqlServerTools()
		{
			AutoDetectProvider = true;

			switch (Provider)
			{
				case SqlServerProvider.SystemDataSqlClient:
					DataConnection.AddDataProvider(ProviderName.SqlServer, _sqlServerDataProvider2008sdc);
					DataConnection.AddDataProvider(ProviderName.SqlServer2014, _sqlServerDataProvider2012sdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2017sdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2012sdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2008sdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2005sdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2000sdc);
					break;
				case SqlServerProvider.MicrosoftDataSqlClient:
					DataConnection.AddDataProvider(ProviderName.SqlServer, _sqlServerDataProvider2008mdc);
					DataConnection.AddDataProvider(ProviderName.SqlServer2014, _sqlServerDataProvider2012mdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2017mdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2012mdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2008mdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2005mdc);
					DataConnection.AddDataProvider(_sqlServerDataProvider2000mdc);
					break;
			}

			DataConnection.AddProviderDetector(ProviderDetector);

			try
			{
				_quoteIdentifier = TryToUseCommandBuilder();
			}
			catch
			{
			}

			if (_quoteIdentifier == null)
				_quoteIdentifier = identifier => '[' + identifier.Replace("]", "]]") + ']';

		}

		// also check https://github.com/linq2db/linq2db/issues/1487
		private static Func<string, string> TryToUseCommandBuilder()
		{
#if NET45 || NET46
			return new System.Data.SqlClient.SqlCommandBuilder().QuoteIdentifier;
#else
			var type = Type.GetType("System.Data.SqlClient.SqlCommandBuilder, System.Data.SqlClient", false);
			type = type ?? Type.GetType("System.Data.SqlClient.SqlCommandBuilder, Microsoft.Data.SqlClient", false);

			if (type != null)
			{
				var mi = type.GetMethod("QuoteIdentifier", BindingFlags.Public | BindingFlags.Instance);
				if (mi != null)
					return (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), Activator.CreateInstance(type), mi);
			}

			return null;
#endif
		}

		internal static string QuoteIdentifier(string identifier)
		{
			return _quoteIdentifier(identifier);
		}

		static IDataProvider ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			var provider = Provider;

			if (css.ProviderName == "Microsoft.Data.SqlClient")
			{
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			}
			else if (css.ProviderName == "System.Data.SqlClient")
			{
				// not sure about it, netfx applications will start failing if they were using sql client from System.Data
				// with this provider name
				provider = SqlServerProvider.SystemDataSqlClient;
			}

			switch (css.ProviderName)
			{
				case ""                      :
				case null                    :

					if (css.Name == "SqlServer")
						goto case "SqlServer";
					break;

				case "SqlServer2000"            :
				case "SqlServer.2000"           : return GetDataProvider(SqlServerVersion.v2000, provider);
				case "SqlServer2005"            :
				case "SqlServer.2005"           : return GetDataProvider(SqlServerVersion.v2005, provider);
				case "SqlServer2008"            :
				case "SqlServer.2008"           : return GetDataProvider(SqlServerVersion.v2008, provider);
				case "SqlServer2012"            :
				case "SqlServer.2012"           :
				case "SqlServer2014"            :
				case "SqlServer.2014"           :
				case "SqlServer2016"            :
				case "SqlServer.2016"           : return GetDataProvider(SqlServerVersion.v2012, provider);
				case "SqlServer2017"            :
				case "SqlServer.2017"           :
				case "SqlServer2019"            :
				case "SqlServer.2019"           : return GetDataProvider(SqlServerVersion.v2017, provider);

				case "SqlServer"                :
				case "System.Data.SqlClient"    :
				case "Microsoft.Data.SqlClient" :

					if (css.Name.Contains("2000"))	return GetDataProvider(SqlServerVersion.v2000, provider);
					if (css.Name.Contains("2005"))	return GetDataProvider(SqlServerVersion.v2005, provider);
					if (css.Name.Contains("2008"))	return GetDataProvider(SqlServerVersion.v2008, provider);
					if (css.Name.Contains("2012") || css.Name.Contains("2014") || css.Name.Contains("2016"))
													return GetDataProvider(SqlServerVersion.v2012, provider);
					if (css.Name.Contains("2017") || css.Name.Contains("2019"))
													return GetDataProvider(SqlServerVersion.v2017, provider);

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = CreateConnection(provider, cs))
							{
								conn.Open();
								dynamic dconn = conn;

								if (int.TryParse(((string)dconn.ServerVersion).Split('.')[0], out var version))
								{
									if (version <= 8)
										return GetDataProvider(SqlServerVersion.v2000, provider);

									using (var cmd = conn.CreateCommand())
									{
										cmd.CommandText = "SELECT compatibility_level FROM sys.databases WHERE name = db_name()";
										var level = Converter.ChangeTypeTo<int>(cmd.ExecuteScalar());

										if (level >= 140)
											return GetDataProvider(SqlServerVersion.v2017, provider);
										if (level >= 110)
											return GetDataProvider(SqlServerVersion.v2012, provider);
										if (level >= 100)
											return GetDataProvider(SqlServerVersion.v2008, provider);
										if (level >= 90)
											return GetDataProvider(SqlServerVersion.v2005, provider);
										if (level >= 80)
											return GetDataProvider(SqlServerVersion.v2000, provider);

										switch (version)
										{
											case  8 : return GetDataProvider(SqlServerVersion.v2000, provider);
											case  9 : return GetDataProvider(SqlServerVersion.v2005, provider);
											case 10 : return GetDataProvider(SqlServerVersion.v2008, provider);
											case 11 :
											case 12 : return GetDataProvider(SqlServerVersion.v2012, provider);
											case 14 : return GetDataProvider(SqlServerVersion.v2017, provider);
											default :
												if (version > 14)
													return GetDataProvider(SqlServerVersion.v2017, provider);
												return GetDataProvider(SqlServerVersion.v2008, provider);
										}
									}
								}
							}
						}
						catch
						{
						}
					}

					break;
			}

			return null;
		}

		private static IDbConnection CreateConnection(SqlServerProvider provider, string connectionString)
		{
			Type type;
			switch (provider)
			{
				case SqlServerProvider.SystemDataSqlClient:
#if NET45 || NET46
					type = typeof(System.Data.SqlClient.SqlConnection);
#else
					type = Type.GetType("System.Data.SqlClient.SqlConnection, System.Data.SqlClient");
#endif
					break;
				case SqlServerProvider.MicrosoftDataSqlClient:
					type = Type.GetType("Microsoft.Data.SqlClient.SqlConnection, Microsoft.Data.SqlClient");
					break;
				default:
					throw new InvalidOperationException();
			}

			return (IDbConnection)Activator.CreateInstance(type, connectionString);
		}

#endregion

#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion version   = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			switch (provider)
			{
				case SqlServerProvider.SystemDataSqlClient:
					switch (version)
					{
						case SqlServerVersion.v2000: return _sqlServerDataProvider2000sdc;
						case SqlServerVersion.v2005: return _sqlServerDataProvider2005sdc;
						case SqlServerVersion.v2012: return _sqlServerDataProvider2012sdc;
						case SqlServerVersion.v2017: return _sqlServerDataProvider2017sdc;
						default: return _sqlServerDataProvider2008sdc;
					}
				case SqlServerProvider.MicrosoftDataSqlClient:
					switch (version)
					{
						case SqlServerVersion.v2000: return _sqlServerDataProvider2000mdc;
						case SqlServerVersion.v2005: return _sqlServerDataProvider2005mdc;
						case SqlServerVersion.v2012: return _sqlServerDataProvider2012mdc;
						case SqlServerVersion.v2017: return _sqlServerDataProvider2017mdc;
						default: return _sqlServerDataProvider2008mdc;
					}
				default: return _sqlServerDataProvider2008sdc;
			}
		}

		private static IEnumerable<SqlServerDataProvider> Providers
		{
			get
			{
				yield return _sqlServerDataProvider2000sdc;
				yield return _sqlServerDataProvider2005sdc;
				yield return _sqlServerDataProvider2008sdc;
				yield return _sqlServerDataProvider2012sdc;
				yield return _sqlServerDataProvider2017sdc;

				yield return _sqlServerDataProvider2000mdc;
				yield return _sqlServerDataProvider2005mdc;
				yield return _sqlServerDataProvider2008mdc;
				yield return _sqlServerDataProvider2012mdc;
				yield return _sqlServerDataProvider2017mdc;
			}
		}

		public static void AddUdtType(Type type, string udtName)
		{
			foreach (var provider in Providers)
				provider.AddUdtType(type, udtName);
		}

		public static void AddUdtType<T>(string udtName, T nullValue, DataType dataType = DataType.Undefined)
		{
			foreach (var provider in Providers)
				provider.AddUdtType(udtName, nullValue, dataType);
		}

		/// <summary>
		/// Loads and registers spatial types assembly (Microsoft.SqlServer.Types) using provided path.
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, "Microsoft.SqlServer.Types");
		}

		/// <summary>
		/// Registers spatial types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes([NotNull] Assembly assembly)
		{
			var types = assembly.GetTypes();

			SqlHierarchyIdType = types.First(t => t.Name == "SqlHierarchyId");
			SqlGeographyType   = types.First(t => t.Name == "SqlGeography");
			SqlGeometryType    = types.First(t => t.Name == "SqlGeometry");
		}

		internal static Type SqlHierarchyIdType;
		internal static Type SqlGeographyType;
		internal static Type SqlGeometryType;

		public static void SetSqlTypes(Type sqlHierarchyIdType, Type sqlGeographyType, Type sqlGeometryType)
		{
			SqlHierarchyIdType = sqlHierarchyIdType;
			SqlGeographyType   = sqlGeographyType;
			SqlGeometryType    = sqlGeometryType;
		}

#endregion

#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string            connectionString,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), connectionString);
		}

		public static DataConnection CreateDataConnection(
			IDbConnection     connection,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			IDbTransaction    transaction,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

#endregion

#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int?                       maxBatchSize       = null,
			int?                       bulkCopyTimeout    = null,
			bool                       keepIdentity       = false,
			bool                       checkConstraints   = false,
			int                        notifyAfter        = 0,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.ProviderSpecific,
					MaxBatchSize       = maxBatchSize,
					BulkCopyTimeout    = bulkCopyTimeout,
					KeepIdentity       = keepIdentity,
					CheckConstraints   = checkConstraints,
					NotifyAfter        = notifyAfter,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

#endregion

		public static class Sql
		{
			public const string OptionRecompile = "OPTION(RECOMPILE)";
		}

		public static Func<IDataReader,int,decimal> DataReaderGetMoney   = (dr, i) => dr.GetDecimal(i);
		public static Func<IDataReader,int,decimal> DataReaderGetDecimal = (dr, i) => dr.GetDecimal(i);
	}
}
