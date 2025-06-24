using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLProviderDetector : ProviderDetectorBase<PostgreSQLProviderDetector.Provider,PostgreSQLVersion>
	{
		internal enum Provider {}

		public PostgreSQLProviderDetector() : base(PostgreSQLVersion.AutoDetect, PostgreSQLVersion.v92)
		{
		}

		static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = CreateDataProvider<PostgreSQLDataProvider92>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = CreateDataProvider<PostgreSQLDataProvider93>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = CreateDataProvider<PostgreSQLDataProvider95>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider15 = CreateDataProvider<PostgreSQLDataProvider15>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			switch (options.ProviderName)
			{
				case ProviderName.PostgreSQL92 : return _postgreSQLDataProvider92.Value;
				case ProviderName.PostgreSQL93 : return _postgreSQLDataProvider93.Value;
				case ProviderName.PostgreSQL95 : return _postgreSQLDataProvider95.Value;
				case ProviderName.PostgreSQL15 : return _postgreSQLDataProvider15.Value;
				case ""                        :
				case null                      :
					if (options.ConfigurationString == "PostgreSQL")
						goto case NpgsqlProviderAdapter.ClientNamespace;
					break;
				case NpgsqlProviderAdapter.ClientNamespace :
				case var providerName when providerName.Contains("PostgreSQL") || providerName.Contains(NpgsqlProviderAdapter.AssemblyName):
					if (options.ConfigurationString != null)
					{
						if (options.ConfigurationString.Contains("15") || options.ConfigurationString.Contains("16"))
							return _postgreSQLDataProvider15.Value;

						if (options.ConfigurationString.Contains("92") || options.ConfigurationString.Contains("9.2"))
							return _postgreSQLDataProvider92.Value;

						if (options.ConfigurationString.Contains("93") || options.ConfigurationString.Contains("9.3") ||
							options.ConfigurationString.Contains("94") || options.ConfigurationString.Contains("9.4"))
							return _postgreSQLDataProvider93.Value;

						if (options.ConfigurationString.Contains("95") || options.ConfigurationString.Contains("9.5") ||
							options.ConfigurationString.Contains("96") || options.ConfigurationString.Contains("9.6") ||
							options.ConfigurationString.Contains("10") ||
							options.ConfigurationString.Contains("11") ||
							options.ConfigurationString.Contains("12") ||
							options.ConfigurationString.Contains("13") ||
							options.ConfigurationString.Contains("14"))
							return _postgreSQLDataProvider95.Value;
					}

					if (AutoDetectProvider)
					{
						try
						{
							var dv = DetectServerVersion(options, default);

							return dv != null ? GetDataProvider(options, default, dv.Value) : null;
						}
						catch
						{
							return _postgreSQLDataProvider92.Value;
						}
					}

					return GetDataProvider(options, default, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, Provider provider, PostgreSQLVersion version)
		{
			return version switch
			{
				PostgreSQLVersion.AutoDetect => GetDataProvider(options, default, DetectServerVersion(options, default) ?? DefaultVersion),
				PostgreSQLVersion.v15        => _postgreSQLDataProvider15.Value,
				PostgreSQLVersion.v95        => _postgreSQLDataProvider95.Value,
				PostgreSQLVersion.v93        => _postgreSQLDataProvider93.Value,
				_                            => _postgreSQLDataProvider92.Value,
			};
		}

		public override PostgreSQLVersion? DetectServerVersion(DbConnection connection)
		{
			var postgreSqlVersion = NpgsqlProviderAdapter.GetInstance().ConnectionWrapper(connection).PostgreSqlVersion;

			if (postgreSqlVersion.Major >= 15)
				return PostgreSQLVersion.v15;

			if (postgreSqlVersion.Major > 9 || postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 4)
				return PostgreSQLVersion.v95;

			if (postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 2)
				return PostgreSQLVersion.v93;

			return DefaultVersion;
		}

		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return NpgsqlProviderAdapter.GetInstance().CreateConnection(connectionString);
		}
	}
}
