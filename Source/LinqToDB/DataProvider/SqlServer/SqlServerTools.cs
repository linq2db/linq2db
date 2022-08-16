using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Configuration;
	using Data;

	public static partial class SqlServerTools
	{
		#region Init

		public static SqlServerProvider Provider = SqlServerProvider.MicrosoftDataSqlClient;
		private static readonly ConcurrentQueue<SqlServerDataProvider> _providers = new();

		// System.Data
		// and/or
		// System.Data.SqlClient
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2005sdc = CreateDataProvider<SqlServerDataProvider2005SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2008sdc = CreateDataProvider<SqlServerDataProvider2008SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2012sdc = CreateDataProvider<SqlServerDataProvider2012SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2014sdc = CreateDataProvider<SqlServerDataProvider2014SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2016sdc = CreateDataProvider<SqlServerDataProvider2016SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2017sdc = CreateDataProvider<SqlServerDataProvider2017SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2019sdc = CreateDataProvider<SqlServerDataProvider2019SystemDataSqlClient>();
		// Microsoft.Data.SqlClient
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2005mdc = CreateDataProvider<SqlServerDataProvider2005MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2008mdc = CreateDataProvider<SqlServerDataProvider2008MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2012mdc = CreateDataProvider<SqlServerDataProvider2012MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2014mdc = CreateDataProvider<SqlServerDataProvider2014MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2016mdc = CreateDataProvider<SqlServerDataProvider2016MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2017mdc = CreateDataProvider<SqlServerDataProvider2017MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2019mdc = CreateDataProvider<SqlServerDataProvider2019MicrosoftDataSqlClient>();

		static Lazy<IDataProvider> CreateDataProvider<T>()
			where T : SqlServerDataProvider, new()
		{
			return new(() =>
			{
				var provider = new T();

				if (Provider == provider.Provider)
					DataConnection.AddDataProvider(provider);

				_providers.Enqueue(provider);

				return provider;
			}, true);
		}

		public static bool AutoDetectProvider { get; set; } = true;

		public static string QuoteIdentifier(string identifier)
		{
			return QuoteIdentifier(new StringBuilder(), identifier).ToString();
		}

		internal static StringBuilder QuoteIdentifier(StringBuilder sb, string identifier)
		{
			sb.Append('[');

			if (identifier.Contains("]"))
				sb.Append(identifier.Replace("]", "]]"));
			else
				sb.Append(identifier);

			sb.Append(']');

			return sb;
		}

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			var provider = Provider;

			if (css.ProviderName      == SqlServerProviderAdapter.MicrosoftClientNamespace)
				provider = SqlServerProvider.MicrosoftDataSqlClient;
			else if (css.ProviderName == SqlServerProviderAdapter.SystemClientNamespace)
				provider = SqlServerProvider.SystemDataSqlClient;

			switch (css.ProviderName)
			{
				case ""                      :
				case null                    :
					if (css.Name == "SqlServer")
						goto case ProviderName.SqlServer;
					break;
					// SqlClient use dot prefix, as SqlClient itself used by some other providers
				case var providerName when providerName.Contains("SqlServer") || providerName.Contains(".SqlClient"):
				case ProviderName.SqlServer:
					if (css.Name.Contains("2005") || css.ProviderName?.Contains("2005") == true) return GetDataProvider(SqlServerVersion.v2005, provider);
					if (css.Name.Contains("2008") || css.ProviderName?.Contains("2008") == true) return GetDataProvider(SqlServerVersion.v2008, provider);
					if (css.Name.Contains("2012") || css.ProviderName?.Contains("2012") == true) return GetDataProvider(SqlServerVersion.v2012, provider);
					if (css.Name.Contains("2014") || css.ProviderName?.Contains("2014") == true) return GetDataProvider(SqlServerVersion.v2014, provider);
					if (css.Name.Contains("2016") || css.ProviderName?.Contains("2016") == true) return GetDataProvider(SqlServerVersion.v2016, provider);
					if (css.Name.Contains("2017") || css.ProviderName?.Contains("2017") == true) return GetDataProvider(SqlServerVersion.v2017, provider);
					if (css.Name.Contains("2019") || css.ProviderName?.Contains("2019") == true) return GetDataProvider(SqlServerVersion.v2019, provider);

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							using (var conn = SqlServerProviderAdapter.GetInstance(provider).CreateConnection(cs))
							{
								conn.Open();

								if (int.TryParse(conn.ServerVersion.Split('.')[0], out var version))
								{
									if (version <= 8)
										// sql server <= 2000
										return null;

									using (var cmd = conn.CreateCommand())
									{
										cmd.CommandText = "SELECT compatibility_level FROM sys.databases WHERE name = db_name()";
										var level = Converter.ChangeTypeTo<int>(cmd.ExecuteScalar());

										if (level >= 150)
											return GetDataProvider(SqlServerVersion.v2019, provider);
										if (level >= 140)
											return GetDataProvider(SqlServerVersion.v2017, provider);
										if (level >= 130)
											return GetDataProvider(SqlServerVersion.v2016, provider);
										if (level >= 120)
											return GetDataProvider(SqlServerVersion.v2014, provider);
										if (level >= 110)
											return GetDataProvider(SqlServerVersion.v2012, provider);
										if (level >= 100)
											return GetDataProvider(SqlServerVersion.v2008, provider);
										if (level >= 90)
											return GetDataProvider(SqlServerVersion.v2005, provider);
										if (level < 90)
											// sql server <= 2000
											return null;

										switch (version)
										{
											// versions below 9 handled above already
											case  9 : return GetDataProvider(SqlServerVersion.v2005, provider);
											case 10 : return GetDataProvider(SqlServerVersion.v2008, provider);
											case 11 : return GetDataProvider(SqlServerVersion.v2012, provider);
											case 12 : return GetDataProvider(SqlServerVersion.v2014, provider);
											case 13 : return GetDataProvider(SqlServerVersion.v2016, provider);
											case 14 : return GetDataProvider(SqlServerVersion.v2017, provider);
											//case 15 : // v2019 : no own dialect yet
											default : return GetDataProvider(SqlServerVersion.v2019, provider);
										}
									}
								}
							}
						}
						catch
						{
						}
					}

					return GetDataProvider(provider: provider);
			}

			return null;
		}

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion version   = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return (provider, version) switch
			{
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2005) => _sqlServerDataProvider2005sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2012) => _sqlServerDataProvider2012sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2014) => _sqlServerDataProvider2014sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2016) => _sqlServerDataProvider2016sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2017) => _sqlServerDataProvider2017sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2019) => _sqlServerDataProvider2019sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    _                     ) => _sqlServerDataProvider2008sdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2005) => _sqlServerDataProvider2005mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2012) => _sqlServerDataProvider2012mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2014) => _sqlServerDataProvider2014mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2016) => _sqlServerDataProvider2016mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2017) => _sqlServerDataProvider2017mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2019) => _sqlServerDataProvider2019mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, _                     ) => _sqlServerDataProvider2008mdc.Value,
				_ => _sqlServerDataProvider2008sdc.Value,
			};
		}

		/// <summary>
		/// Tries to load and register spatial types using provided path to types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(string path)
		{
			if (path == null) ThrowHelper.ThrowArgumentNullException(nameof(path));

			new AssemblyResolver(path, SqlServerTypes.AssemblyName);

			if (SqlServerTypes.UpdateTypes())
				foreach (var provider in _providers)
					SqlServerTypes.Configure(provider);
		}

		/// <summary>
		/// Registers spatial types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(Assembly assembly)
		{
			if (SqlServerTypes.UpdateTypes(assembly))
				foreach (var provider in _providers)
					SqlServerTypes.Configure(provider);
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
			DbConnection      connection,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction     transaction,
			SqlServerVersion  version  = SqlServerVersion.v2008,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		#endregion

		[Obsolete("Use 'QueryHint(Hints.Option.Recompile)' instead.")]
		public static class Sql
		{
			public const string OptionRecompile = "OPTION(RECOMPILE)";
		}
	}
}
