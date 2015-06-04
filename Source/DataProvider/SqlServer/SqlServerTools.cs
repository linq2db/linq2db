using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;

	public static class SqlServerTools
	{
		#region Init

		static readonly SqlServerDataProvider _sqlServerDataProvider2000 = new SqlServerDataProvider(ProviderName.SqlServer2000, SqlServerVersion.v2000);
		static readonly SqlServerDataProvider _sqlServerDataProvider2005 = new SqlServerDataProvider(ProviderName.SqlServer2005, SqlServerVersion.v2005);
		static readonly SqlServerDataProvider _sqlServerDataProvider2008 = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008);
		static readonly SqlServerDataProvider _sqlServerDataProvider2012 = new SqlServerDataProvider(ProviderName.SqlServer2012, SqlServerVersion.v2012);

		public static bool AutoDetectProvider { get; set; }

		static SqlServerTools()
		{
			AutoDetectProvider = true;

			DataConnection.AddDataProvider(ProviderName.SqlServer, _sqlServerDataProvider2008);
			DataConnection.AddDataProvider(_sqlServerDataProvider2012);
			DataConnection.AddDataProvider(_sqlServerDataProvider2008);
			DataConnection.AddDataProvider(_sqlServerDataProvider2005);
			DataConnection.AddDataProvider(_sqlServerDataProvider2000);

			DataConnection.AddProviderDetector(ProviderDetector);
		}

		static IDataProvider ProviderDetector(ConnectionStringSettings css)
		{
			if (DataConnection.IsMachineConfig(css))
				return null;

			switch (css.ProviderName)
			{
				case ""                      :
				case null                    :

					if (css.Name == "SqlServer")
						goto case "SqlServer";
					break;

				case "SqlServer"             :
				case "System.Data.SqlClient" :

					if (css.Name.Contains("2000")) return _sqlServerDataProvider2000;
					if (css.Name.Contains("2005")) return _sqlServerDataProvider2005;
					if (css.Name.Contains("2008")) return _sqlServerDataProvider2008;
					if (css.Name.Contains("2012")) return _sqlServerDataProvider2012;
					if (css.Name.Contains("2014")) return _sqlServerDataProvider2012;

					if (AutoDetectProvider)
					{
						try
						{
							var connectionString = css.ConnectionString;

							using (var conn = new SqlConnection(connectionString))
							{
								conn.Open();

								switch (conn.ServerVersion.Split('.')[0])
								{
									case  "8" : return _sqlServerDataProvider2000;
									case  "9" :	return _sqlServerDataProvider2005;
									case "10" :	return _sqlServerDataProvider2008;
									case "11" : return _sqlServerDataProvider2012;
									case "12" : return _sqlServerDataProvider2012;
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

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2000 : return _sqlServerDataProvider2000;
				case SqlServerVersion.v2005 : return _sqlServerDataProvider2005;
				case SqlServerVersion.v2012 : return _sqlServerDataProvider2012;
			}

			return _sqlServerDataProvider2008;
		}

		public static void AddUdtType(Type type, string udtName)
		{
			_sqlServerDataProvider2000.AddUdtType(type, udtName);
			_sqlServerDataProvider2005.AddUdtType(type, udtName);
			_sqlServerDataProvider2008.AddUdtType(type, udtName);
			_sqlServerDataProvider2012.AddUdtType(type, udtName);
		}

		public static void AddUdtType<T>(string udtName, T nullValue, DataType dataType = DataType.Undefined)
		{
			_sqlServerDataProvider2000.AddUdtType(udtName, nullValue, dataType);
			_sqlServerDataProvider2005.AddUdtType(udtName, nullValue, dataType);
			_sqlServerDataProvider2008.AddUdtType(udtName, nullValue, dataType);
			_sqlServerDataProvider2012.AddUdtType(udtName, nullValue, dataType);
		}

		public static void ResolveSqlTypes([NotNull] string path)
		{
			if (path == null) throw new ArgumentNullException("path");
			new AssemblyResolver(path, "Microsoft.SqlServer.Types");
		}

		public static void ResolveSqlTypes([NotNull] Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");
			new AssemblyResolver(assembly, "Microsoft.SqlServer.Types");
		}

		#endregion

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2000 : return new DataConnection(_sqlServerDataProvider2000, connectionString);
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, connectionString);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, connectionString);
			}

			return new DataConnection(_sqlServerDataProvider2008, connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2000 : return new DataConnection(_sqlServerDataProvider2000, connection);
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, connection);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, connection);
			}

			return new DataConnection(_sqlServerDataProvider2008, connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, SqlServerVersion version = SqlServerVersion.v2008)
		{
			switch (version)
			{
				case SqlServerVersion.v2000 : return new DataConnection(_sqlServerDataProvider2000, transaction);
				case SqlServerVersion.v2005 : return new DataConnection(_sqlServerDataProvider2005, transaction);
				case SqlServerVersion.v2012 : return new DataConnection(_sqlServerDataProvider2012, transaction);
			}

			return new DataConnection(_sqlServerDataProvider2008, transaction);
		}

		#endregion

		#region BulkCopy

		private static BulkCopyType _defaultBulkCopyType = BulkCopyType.ProviderSpecific;
		public  static BulkCopyType  DefaultBulkCopyType
		{
			get { return _defaultBulkCopyType;  }
			set { _defaultBulkCopyType = value; }
		}

//		public static int MultipleRowsCopy<T>(DataConnection dataConnection, IEnumerable<T> source, int maxBatchSize = 1000)
//		{
//			return dataConnection.BulkCopy(
//				new BulkCopyOptions
//				{
//					BulkCopyType = BulkCopyType.MultipleRows,
//					MaxBatchSize = maxBatchSize,
//				}, source);
//		}

		public static BulkCopyRowsCopied ProviderSpecificBulkCopy<T>(
			DataConnection             dataConnection,
			IEnumerable<T>             source,
			int?                       maxBatchSize       = null,
			int?                       bulkCopyTimeout    = null,
			bool                       keepIdentity       = false,
			bool                       checkConstraints   = false,
			int                        notifyAfter        = 0,
			Action<BulkCopyRowsCopied> rowsCopiedCallback = null)
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

		#region Extensions

		public static void SetIdentityInsert<T>(this DataConnection dataConnection, ITable<T> table, bool isOn)
		{
			dataConnection.Execute("SET IDENTITY_INSERT ");
		}

		#endregion

		public static class Sql
		{
			public const string OptionRecompile = "OPTION(RECOMPILE)";
		}
	}
}
