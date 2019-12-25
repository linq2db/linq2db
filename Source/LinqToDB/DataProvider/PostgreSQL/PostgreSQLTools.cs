using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static class PostgreSQLTools
	{
		private static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = new Lazy<IDataProvider>(() =>
		{
			var provider = new PostgreSQLDataProvider(ProviderName.PostgreSQL92, PostgreSQLVersion.v92);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = new Lazy<IDataProvider>(() =>
		{
			var provider = new PostgreSQLDataProvider(ProviderName.PostgreSQL93, PostgreSQLVersion.v93);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = new Lazy<IDataProvider>(() =>
		{
			var provider = new PostgreSQLDataProvider(ProviderName.PostgreSQL95, PostgreSQLVersion.v95);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case ""               :
				case null             :

					if (css.Name == "PostgreSQL")
						goto case "PostgreSQL";
					break;

				case "PostgreSQL92"   :
				case "PostgreSQL.92"  :
				case "PostgreSQL.9.2" :
					return _postgreSQLDataProvider92.Value;

				case "PostgreSQL93"   : case "PostgreSQL.93"  : case "PostgreSQL.9.3" :
				case "PostgreSQL94"   : case "PostgreSQL.94"  : case "PostgreSQL.9.4" :
					return _postgreSQLDataProvider93.Value;

				case "PostgreSQL95"   : case "PostgreSQL.95"  : case "PostgreSQL.9.5" :
				case "PostgreSQL96"   : case "PostgreSQL.96"  : case "PostgreSQL.9.6" :
					return _postgreSQLDataProvider95.Value;

				case "PostgreSQL"     :
				case "Npgsql"         :

					if (css.Name.Contains("92") || css.Name.Contains("9.2"))
						return _postgreSQLDataProvider92.Value;

					if (css.Name.Contains("93") || css.Name.Contains("9.3") ||
						css.Name.Contains("94") || css.Name.Contains("9.4"))
						return _postgreSQLDataProvider93.Value;

					if (css.Name.Contains("95") || css.Name.Contains("9.5") ||
						css.Name.Contains("96") || css.Name.Contains("9.6"))
						return _postgreSQLDataProvider95.Value;

					if (AutoDetectProvider)
					{
						try
						{
							// TODO: use provider wrapper
							var cs                = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							var wrapper = PostgreSQLWrappers.Initialize();
							using (var conn = wrapper.CreateNpgsqlConnection(cs))
							{
								conn.Open();

								var postgreSqlVersion = conn.PostgreSqlVersion;

								if (postgreSqlVersion.Major > 9 || postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 4)
									return _postgreSQLDataProvider95.Value;

								if (postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 2)
									return _postgreSQLDataProvider93.Value;

								return _postgreSQLDataProvider92.Value;
							}
						}
						catch
						{
							return _postgreSQLDataProvider92.Value;
						}
					}

					break;
			}

			return null;
		}

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			switch (version)
			{
				case PostgreSQLVersion.v95:
					return _postgreSQLDataProvider95.Value;
				case PostgreSQLVersion.v93:
					return _postgreSQLDataProvider93.Value;
				default:
				case PostgreSQLVersion.v92:
					return _postgreSQLDataProvider92.Value;
			}
		}

		public static void ResolvePostgreSQL(string path)
		{
			new AssemblyResolver(path, PostgreSQLWrappers.AssemblyName);
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int                         maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion
	}
}
