using System;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Configuration;
	using Data;

	sealed class PostgreSQLProviderDetector : ProviderDetectorBase<PostgreSQLProviderDetector.Provider,PostgreSQLVersion,NpgsqlProviderAdapter.NpgsqlConnection>
	{
		internal enum Provider {}

		public PostgreSQLProviderDetector() : base(PostgreSQLVersion.AutoDetect, PostgreSQLVersion.v92)
		{
		}

		static readonly Lazy<IDataProvider> _postgreSQLDataProvider92 = DataConnection.CreateDataProvider<PostgreSQLDataProvider92>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider93 = DataConnection.CreateDataProvider<PostgreSQLDataProvider93>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider95 = DataConnection.CreateDataProvider<PostgreSQLDataProvider95>();
		static readonly Lazy<IDataProvider> _postgreSQLDataProvider15 = DataConnection.CreateDataProvider<PostgreSQLDataProvider15>();

		public override IDataProvider? DetectProvider(IConnectionStringSettings css, string connectionString)
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
						goto case NpgsqlProviderAdapter.ClientNamespace;
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
							var dv = DetectServerVersion(default, cs);

							return dv != null ? GetDataProvider(default, dv.Value, connectionString) : null;
						}
						catch
						{
							return _postgreSQLDataProvider92.Value;
						}
					}

					return GetDataProvider(default, DefaultVersion, connectionString);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(Provider provider, PostgreSQLVersion version, string? connectionString)
		{
			return version switch
			{
				PostgreSQLVersion.AutoDetect => AutoDetectProvider(),
				PostgreSQLVersion.v15        => _postgreSQLDataProvider15.Value,
				PostgreSQLVersion.v95        => _postgreSQLDataProvider95.Value,
				PostgreSQLVersion.v93        => _postgreSQLDataProvider93.Value,
				_                            => _postgreSQLDataProvider92.Value,
			};

			IDataProvider AutoDetectProvider()
			{
				if (connectionString == null)
					throw new InvalidOperationException("Connection string is not provided.");

				return GetDataProvider(default, DetectServerVersion(default, connectionString) ?? DefaultVersion, null);
			}
		}

		public override PostgreSQLVersion? DetectServerVersion(NpgsqlProviderAdapter.NpgsqlConnection connection)
		{
			var postgreSqlVersion = connection.PostgreSqlVersion;

			if (postgreSqlVersion.Major >= 15)
				return PostgreSQLVersion.v15;

			if (postgreSqlVersion.Major > 9 || postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 4)
				return PostgreSQLVersion.v95;

			if (postgreSqlVersion.Major == 9 && postgreSqlVersion.Minor > 2)
				return PostgreSQLVersion.v93;

			return DefaultVersion;
		}

		protected override NpgsqlProviderAdapter.NpgsqlConnection CreateConnection(Provider provider, string connectionString)
		{
			return NpgsqlProviderAdapter.GetInstance().CreateConnection(connectionString);
		}
	}
}
