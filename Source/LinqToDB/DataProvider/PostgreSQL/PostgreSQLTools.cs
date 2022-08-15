using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Configuration;
	using Data;

	[PublicAPI]
	public static partial class PostgreSQLTools
	{
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = DataConnection.CreateDataProvider<PostgreSQLDataProvider92>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = DataConnection.CreateDataProvider<PostgreSQLDataProvider93>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = DataConnection.CreateDataProvider<PostgreSQLDataProvider95>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider15 = DataConnection.CreateDataProvider<PostgreSQLDataProvider15>();

		public static bool AutoDetectProvider     { get; set; } = true;

		/// <summary>
		/// Enables normalization of <see cref="DateTime"/> and <see cref="DateTimeOffset"/> data, passed to query
		/// as parameter or passed to <see cref="DataConnectionExtensions.BulkCopy{T}(ITable{T}, IEnumerable{T})"/> APIs,
		/// to comform with Npgsql 6 requerements:
		/// <list type="bullet">
		/// <item>convert <see cref="DateTimeOffset"/> value to UTC value with zero <see cref="DateTimeOffset.Offset"/></item>
		/// <item>Use <see cref="DateTimeKind.Utc"/> for <see cref="DateTime"/> timestamptz values</item>
		/// <item>Use <see cref="DateTimeKind.Unspecified"/> for <see cref="DateTime"/> timestamp values with <see cref="DateTimeKind.Utc"/> kind</item>
		/// </list>
		/// Default value: <c>true</c>.
		/// </summary>
		public static bool NormalizeTimestampData { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case ProviderName.PostgreSQL92 : return _postgreSQLDataProvider92.Value;
				case ProviderName.PostgreSQL93 : return _postgreSQLDataProvider93.Value;
				case ProviderName.PostgreSQL95 : return _postgreSQLDataProvider95.Value;
				case ProviderName.PostgreSQL15 : return _postgreSQLDataProvider15.Value;
				case ""                        :
				case null                      :
					if (css.Name == "PostgreSQL")
						goto case "Npgsql";
					break;
				case NpgsqlProviderAdapter.ClientNamespace :
				case var providerName when providerName.Contains("PostgreSQL") || providerName.Contains(NpgsqlProviderAdapter.AssemblyName):
					if (css.Name.Contains("15"))
						return _postgreSQLDataProvider15.Value;

					if (css.Name.Contains("92") || css.Name.Contains("9.2"))
						return _postgreSQLDataProvider92.Value;

					if (css.Name.Contains("93") || css.Name.Contains("9.3") ||
						css.Name.Contains("94") || css.Name.Contains("9.4"))
						return _postgreSQLDataProvider93.Value;

					if (css.Name.Contains("95") || css.Name.Contains("9.5") ||
						css.Name.Contains("96") || css.Name.Contains("9.6") ||
						css.Name.Contains("10") ||
						css.Name.Contains("11") ||
						css.Name.Contains("12") ||
						css.Name.Contains("13") ||
						css.Name.Contains("14"))
						return _postgreSQLDataProvider95.Value;

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = NpgsqlProviderAdapter.GetInstance().CreateConnection(cs))
							{
								conn.Open();

								var postgreSqlVersion = conn.PostgreSqlVersion;

								if (postgreSqlVersion.Major >= 15)
									return _postgreSQLDataProvider15.Value;

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

					return GetDataProvider();
			}

			return null;
		}

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return version switch
			{
				PostgreSQLVersion.v15 => _postgreSQLDataProvider15.Value,
				PostgreSQLVersion.v95 => _postgreSQLDataProvider95.Value,
				PostgreSQLVersion.v93 => _postgreSQLDataProvider93.Value,
				_                     => _postgreSQLDataProvider92.Value,
			};
		}

		public static void ResolvePostgreSQL(string path)
		{
			new AssemblyResolver(path, NpgsqlProviderAdapter.AssemblyName);
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, PostgreSQLVersion version = PostgreSQLVersion.v92)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		#endregion
	}
}
