using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerProviderDetector : ProviderDetectorBase<SqlServerProvider,SqlServerVersion>
	{
		public SqlServerProviderDetector() : base(SqlServerVersion.AutoDetect, SqlServerVersion.v2012)
		{
		}

		// System.Data
		// and/or
		// System.Data.SqlClient
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2005Sdc = CreateDataProvider<SqlServerDataProvider2005SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2008Sdc = CreateDataProvider<SqlServerDataProvider2008SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2012Sdc = CreateDataProvider<SqlServerDataProvider2012SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2014Sdc = CreateDataProvider<SqlServerDataProvider2014SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2016Sdc = CreateDataProvider<SqlServerDataProvider2016SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2017Sdc = CreateDataProvider<SqlServerDataProvider2017SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2019Sdc = CreateDataProvider<SqlServerDataProvider2019SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2022Sdc = CreateDataProvider<SqlServerDataProvider2022SystemDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2025Sdc = CreateDataProvider<SqlServerDataProvider2025SystemDataSqlClient>();
		// Microsoft.Data.SqlClient
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2005Mdc = CreateDataProvider<SqlServerDataProvider2005MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2008Mdc = CreateDataProvider<SqlServerDataProvider2008MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2012Mdc = CreateDataProvider<SqlServerDataProvider2012MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2014Mdc = CreateDataProvider<SqlServerDataProvider2014MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2016Mdc = CreateDataProvider<SqlServerDataProvider2016MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2017Mdc = CreateDataProvider<SqlServerDataProvider2017MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2019Mdc = CreateDataProvider<SqlServerDataProvider2019MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2022Mdc = CreateDataProvider<SqlServerDataProvider2022MicrosoftDataSqlClient>();
		static readonly Lazy<IDataProvider> _sqlServerDataProvider2025Mdc = CreateDataProvider<SqlServerDataProvider2025MicrosoftDataSqlClient>();

		static IEnumerable<SqlServerDataProvider> GetInstantiatedProviders()
		{
			return DataConnection.GetRegisteredProviders().Values.OfType<SqlServerDataProvider>();
		}

		/// <summary>
		/// Tries to load and register spatial types using provided path to types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));

			_ = new AssemblyResolver(path, SqlServerTypes.AssemblyName);

			if (SqlServerTypes.UpdateTypes())
				foreach (var provider in GetInstantiatedProviders())
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
				foreach (var provider in GetInstantiatedProviders())
					SqlServerTypes.Configure(provider);
		}

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				SqlServerProviderAdapter.MicrosoftClientNamespace => SqlServerProvider.MicrosoftDataSqlClient,
				SqlServerProviderAdapter.SystemClientNamespace    => SqlServerProvider.SystemDataSqlClient,
				_                                                 => DetectProvider()
			};

			switch (options.ProviderName)
			{
				case ""                      :
				case null                    :
					if (options.ConfigurationString == "SqlServer")
						goto case ProviderName.SqlServer;
					break;
					// SqlClient use dot prefix, as SqlClient itself used by some other providers
				case var providerName when providerName.Contains("SqlServer") || providerName.Contains(".SqlClient"):
				case ProviderName.SqlServer:
					if (options.ConfigurationString?.Contains("2005") == true || options.ProviderName?.Contains("2005") == true) return GetDataProvider(options, provider, SqlServerVersion.v2005);
					if (options.ConfigurationString?.Contains("2008") == true || options.ProviderName?.Contains("2008") == true) return GetDataProvider(options, provider, SqlServerVersion.v2008);
					if (options.ConfigurationString?.Contains("2012") == true || options.ProviderName?.Contains("2012") == true) return GetDataProvider(options, provider, SqlServerVersion.v2012);
					if (options.ConfigurationString?.Contains("2014") == true || options.ProviderName?.Contains("2014") == true) return GetDataProvider(options, provider, SqlServerVersion.v2014);
					if (options.ConfigurationString?.Contains("2016") == true || options.ProviderName?.Contains("2016") == true) return GetDataProvider(options, provider, SqlServerVersion.v2016);
					if (options.ConfigurationString?.Contains("2017") == true || options.ProviderName?.Contains("2017") == true) return GetDataProvider(options, provider, SqlServerVersion.v2017);
					if (options.ConfigurationString?.Contains("2019") == true || options.ProviderName?.Contains("2019") == true) return GetDataProvider(options, provider, SqlServerVersion.v2019);
					if (options.ConfigurationString?.Contains("2022") == true || options.ProviderName?.Contains("2022") == true) return GetDataProvider(options, provider, SqlServerVersion.v2022);
					if (options.ConfigurationString?.Contains("2025") == true || options.ProviderName?.Contains("2025") == true) return GetDataProvider(options, provider, SqlServerVersion.v2025);

					if (AutoDetectProvider)
					{
						try
						{
							var dv = DetectServerVersion(options, provider);

							return dv != null ? GetDataProvider(options, provider, dv.Value) : null;
						}
						catch
						{
							// ignored
						}
					}

					return GetDataProvider(options, provider, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, SqlServerProvider provider, SqlServerVersion version)
		{
			if (provider == SqlServerProvider.AutoDetect)
				provider = DetectProvider();

			return (provider, version) switch
			{
				(_,                                        SqlServerVersion.AutoDetect) => GetDataProvider(options, provider, DetectServerVersion(options, provider) ?? DefaultVersion),
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2005)      => _sqlServerDataProvider2005Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2008)      => _sqlServerDataProvider2008Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2012)      => _sqlServerDataProvider2012Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2014)      => _sqlServerDataProvider2014Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2016)      => _sqlServerDataProvider2016Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2017)      => _sqlServerDataProvider2017Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2019)      => _sqlServerDataProvider2019Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2022)      => _sqlServerDataProvider2022Sdc.Value,
				(SqlServerProvider.SystemDataSqlClient,    SqlServerVersion.v2025)      => _sqlServerDataProvider2025Sdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2005)      => _sqlServerDataProvider2005Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2008)      => _sqlServerDataProvider2008Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2012)      => _sqlServerDataProvider2012Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2014)      => _sqlServerDataProvider2014Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2016)      => _sqlServerDataProvider2016Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2017)      => _sqlServerDataProvider2017Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2019)      => _sqlServerDataProvider2019Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2022)      => _sqlServerDataProvider2022Mdc.Value,
				(SqlServerProvider.MicrosoftDataSqlClient, SqlServerVersion.v2025)      => _sqlServerDataProvider2025Mdc.Value,
				_                                                                       => _sqlServerDataProvider2008Sdc.Value,
			};
		}

		public static SqlServerProvider DetectProvider()
		{
			var fileName = typeof(SqlServerProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", SqlServerProviderAdapter.MicrosoftAssemblyName + ".dll"))
				? SqlServerProvider.MicrosoftDataSqlClient
				: SqlServerProvider.SystemDataSqlClient;
		}

		public override SqlServerVersion? DetectServerVersion(DbConnection connection)
		{
			if (!int.TryParse(connection.ServerVersion.Split('.')[0], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var version))
				return null;

			if (version <= 8)
				// sql server <= 2000
				return null;

			using var cmd = connection.CreateCommand();

			cmd.CommandText = "SELECT compatibility_level FROM sys.databases WHERE name = db_name()";

			var level = Converter.ChangeTypeTo<int>(cmd.ExecuteScalar());

			return level switch
			{
				>= 170 => SqlServerVersion.v2025,
				>= 160 => SqlServerVersion.v2022,
				>= 150 => SqlServerVersion.v2019,
				>= 140 => SqlServerVersion.v2017,
				>= 130 => SqlServerVersion.v2016,
				>= 120 => SqlServerVersion.v2014,
				>= 110 => SqlServerVersion.v2012,
				>= 100 => SqlServerVersion.v2008,
				>=  90 => SqlServerVersion.v2005,
				_      => version switch
				{
					// versions below 9 handled above already
					 9 => SqlServerVersion.v2005,
					10 => SqlServerVersion.v2008,
					11 => SqlServerVersion.v2012,
					12 => SqlServerVersion.v2014,
					13 => SqlServerVersion.v2016,
					14 => SqlServerVersion.v2017,
					15 => SqlServerVersion.v2019,
					16 => SqlServerVersion.v2022,
					_  => SqlServerVersion.v2025
				}
			};
		}

		protected override DbConnection CreateConnection(SqlServerProvider provider, string connectionString)
		{
			return SqlServerProviderAdapter.GetInstance(provider).CreateConnection(connectionString);
		}
	}
}
