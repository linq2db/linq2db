using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common.Internal.Cache;
	using Configuration;
	using Data;

	[PublicAPI]
	public static partial class PostgreSQLTools
	{
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = DataConnection.CreateDataProvider<PostgreSQLDataProvider92>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = DataConnection.CreateDataProvider<PostgreSQLDataProvider93>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = DataConnection.CreateDataProvider<PostgreSQLDataProvider95>();

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
				case ""                        :
				case null                      :
					if (css.Name == "PostgreSQL")
						goto case "Npgsql";
					break;
				case NpgsqlProviderAdapter.ClientNamespace :
				case var providerName when providerName.Contains("PostgreSQL") || providerName.Contains(NpgsqlProviderAdapter.AssemblyName):
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
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;
							var dv = DetectServerVersionCached(cs);

							return dv != null ? GetDataProvider(dv.Value, connectionString) : null;
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

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.v92, string? connectionString = null)
		{
			return version switch
			{
				PostgreSQLVersion.AutoDetect => DetectProvider(),
				PostgreSQLVersion.v95        => _postgreSQLDataProvider95.Value,
				PostgreSQLVersion.v93        => _postgreSQLDataProvider93.Value,
				_                            => _postgreSQLDataProvider92.Value,
			};

			IDataProvider DetectProvider()
			{
				if (connectionString == null)
					throw new InvalidOperationException("Connection string is not provided.");

				return GetDataProvider(DetectServerVersionCached(connectionString) ?? PostgreSQLVersion.v92);
			}
		}

		static readonly MemoryCache<string,PostgreSQLVersion?> _providerCache = new(new());

		/// <summary>
		/// Clears provider version cache.
		/// </summary>
		public static void ClearCache()
		{
			_providerCache.Clear();
		}

		/// <summary>
		/// Connects to PostgreSQL and parses version information.
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns>Detected PostgreSQL version.</returns>
		/// <remarks>Uses cache to avoid unwanted connections to Database.</remarks>
		public static PostgreSQLVersion? DetectServerVersionCached(string connectionString)
		{
			var version = _providerCache.GetOrCreate(connectionString, entry =>
			{
				entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
				return DetectServerVersion(entry.Key);
			});

			return version;
		}

		/// <summary>
		/// Connects to SQL Server Database and parses version information.
		/// </summary>
		/// <param name="connectionString"></param>
		/// <returns>Detected SQL Server version.</returns>
		public static PostgreSQLVersion? DetectServerVersion(string connectionString)
		{
			using var conn = NpgsqlProviderAdapter.GetInstance().CreateConnection(connectionString);

			conn.Open();

			return DetectServerVersion(conn);
		}

		internal static bool TryGetCachedServerVersion(string connectionString, out PostgreSQLVersion? version)
		{
			return _providerCache.TryGetValue(connectionString, out version);
		}

		static PostgreSQLVersion? DetectServerVersion(NpgsqlProviderAdapter.NpgsqlConnection connection)
		{
			var postgreSqlVersion = connection.PostgreSqlVersion;

			if (postgreSqlVersion.Major > 9 || postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 4)
				return PostgreSQLVersion.v95;

			if (postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 2)
				return PostgreSQLVersion.v93;

			return PostgreSQLVersion.v92;
		}

		public static void ResolvePostgreSQL(string path)
		{
			_ = new AssemblyResolver(path, NpgsqlProviderAdapter.AssemblyName);
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
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
