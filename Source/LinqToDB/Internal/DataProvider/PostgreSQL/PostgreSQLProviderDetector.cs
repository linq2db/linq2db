using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	public class PostgreSQLProviderDetector() : ProviderDetectorBase<PostgreSQLProviderDetector.Provider,PostgreSQLVersion>(PostgreSQLVersion.AutoDetect, PostgreSQLVersion.v92)
	{
		public enum Provider {}

		static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = CreateDataProvider<PostgreSQLDataProvider92>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = CreateDataProvider<PostgreSQLDataProvider93>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = CreateDataProvider<PostgreSQLDataProvider95>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider13 = CreateDataProvider<PostgreSQLDataProvider13>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider15 = CreateDataProvider<PostgreSQLDataProvider15>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider18 = CreateDataProvider<PostgreSQLDataProvider18>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			switch (options.ProviderName)
			{
				case ProviderName.PostgreSQL92 : return _postgreSQLDataProvider92.Value;
				case ProviderName.PostgreSQL93 : return _postgreSQLDataProvider93.Value;
				case ProviderName.PostgreSQL95 : return _postgreSQLDataProvider95.Value;
				case ProviderName.PostgreSQL13 : return _postgreSQLDataProvider13.Value;
				case ProviderName.PostgreSQL15 : return _postgreSQLDataProvider15.Value;
				case ProviderName.PostgreSQL18 : return _postgreSQLDataProvider18.Value;
				case ""                        :
				case null                      :
					if (string.Equals(options.ConfigurationString, "PostgreSQL", StringComparison.Ordinal))
						goto case NpgsqlProviderAdapter.ClientNamespace;
					break;
				case NpgsqlProviderAdapter.ClientNamespace :
				case var providerName when providerName.Contains("PostgreSQL", StringComparison.Ordinal) || providerName.Contains(NpgsqlProviderAdapter.AssemblyName, StringComparison.Ordinal):
					if (options.ConfigurationString != null)
					{
						if (options.ConfigurationString.Contains("18", StringComparison.Ordinal))
							return _postgreSQLDataProvider18.Value;

						if (options.ConfigurationString.Contains("15", StringComparison.Ordinal)
							|| options.ConfigurationString.Contains("16", StringComparison.Ordinal)
							|| options.ConfigurationString.Contains("17", StringComparison.Ordinal))
						{
							return _postgreSQLDataProvider15.Value;
						}

						if (options.ConfigurationString.Contains("14", StringComparison.Ordinal)
							|| options.ConfigurationString.Contains("13", StringComparison.Ordinal))
						{
							return _postgreSQLDataProvider13.Value;
						}

						if (options.ConfigurationString.Contains("92", StringComparison.Ordinal) || options.ConfigurationString.Contains("9.2", StringComparison.Ordinal))
							return _postgreSQLDataProvider92.Value;

						if (options.ConfigurationString.Contains("93", StringComparison.Ordinal) || options.ConfigurationString.Contains("9.3", StringComparison.Ordinal) ||
							options.ConfigurationString.Contains("94", StringComparison.Ordinal) || options.ConfigurationString.Contains("9.4", StringComparison.Ordinal))
							return _postgreSQLDataProvider93.Value;

						if (options.ConfigurationString.Contains("95", StringComparison.Ordinal) || options.ConfigurationString.Contains("9.5", StringComparison.Ordinal) ||
							options.ConfigurationString.Contains("96", StringComparison.Ordinal) || options.ConfigurationString.Contains("9.6", StringComparison.Ordinal) ||
							options.ConfigurationString.Contains("10", StringComparison.Ordinal) ||
							options.ConfigurationString.Contains("11", StringComparison.Ordinal) ||
							options.ConfigurationString.Contains("12", StringComparison.Ordinal))
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
				PostgreSQLVersion.v18        => _postgreSQLDataProvider18.Value,
				PostgreSQLVersion.v15        => _postgreSQLDataProvider15.Value,
				PostgreSQLVersion.v13        => _postgreSQLDataProvider13.Value,
				PostgreSQLVersion.v95        => _postgreSQLDataProvider95.Value,
				PostgreSQLVersion.v93        => _postgreSQLDataProvider93.Value,
				_                            => _postgreSQLDataProvider92.Value,
			};
		}

		protected override PostgreSQLVersion? DetectServerVersion(DbConnection connection, DbTransaction? transaction)
		{
			return NpgsqlProviderAdapter.GetInstance().ConnectionWrapper(connection).PostgreSqlVersion switch
			{
				{ Major: >= 18 } => PostgreSQLVersion.v18,
				{ Major: >= 15 } => PostgreSQLVersion.v15,
				{ Major: >= 13 } => PostgreSQLVersion.v13,
				{ Major: > 9 } or { Major: 9, Minor: > 4 } => PostgreSQLVersion.v95,
				{ Major: 9, Minor: > 2 } => PostgreSQLVersion.v93,
				_ => DefaultVersion,
			};
		}

		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return NpgsqlProviderAdapter.GetInstance().CreateConnection(connectionString);
		}
	}
}
