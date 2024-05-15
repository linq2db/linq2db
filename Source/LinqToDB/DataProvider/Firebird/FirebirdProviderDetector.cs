using System;
using System.Data.Common;

namespace LinqToDB.DataProvider.Firebird
{
	using Configuration;
	using Data;

	sealed class FirebirdProviderDetector : ProviderDetectorBase<FirebirdProviderDetector.Provider, FirebirdVersion>
	{
		internal enum Provider {}

		public FirebirdProviderDetector() : base(FirebirdVersion.AutoDetect, FirebirdVersion.v25)
		{
		}

		static readonly Lazy<IDataProvider> _firebirdDataProvider25 = CreateDataProvider<FirebirdDataProvider25>();
		static readonly Lazy<IDataProvider> _firebirdDataProvider3  = CreateDataProvider<FirebirdDataProvider3 >();
		static readonly Lazy<IDataProvider> _firebirdDataProvider4  = CreateDataProvider<FirebirdDataProvider4 >();
		static readonly Lazy<IDataProvider> _firebirdDataProvider5  = CreateDataProvider<FirebirdDataProvider5 >();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			switch (options.ProviderName)
			{
				case ProviderName.Firebird25 : return _firebirdDataProvider25.Value;
				case ProviderName.Firebird3  : return _firebirdDataProvider3.Value;
				case ProviderName.Firebird4  : return _firebirdDataProvider4.Value;
				case ProviderName.Firebird5  : return _firebirdDataProvider5.Value;
				case ""                      :
				case null                    :
					if (options.ConfigurationString?.Contains(ProviderName.Firebird) == true)
						goto case FirebirdProviderAdapter.ClientNamespace;
					break;
				case FirebirdProviderAdapter.ClientNamespace :
				case var providerName when providerName.Contains(ProviderName.Firebird) || providerName.Contains(FirebirdProviderAdapter.AssemblyName):
					if (options.ConfigurationString != null)
					{
						if (options.ConfigurationString.Contains("2.5") || options.ConfigurationString.Contains("25"))
							return _firebirdDataProvider25.Value;

						if (options.ConfigurationString.Contains("5"))
							return _firebirdDataProvider5.Value;

						if (options.ConfigurationString.Contains("4"))
							return _firebirdDataProvider4.Value;

						if (options.ConfigurationString.Contains("3"))
							return _firebirdDataProvider3.Value;
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
							return _firebirdDataProvider25.Value;
						}
					}

					return GetDataProvider(options, default, DefaultVersion);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, Provider provider, FirebirdVersion version)
		{
			return version switch
			{
				FirebirdVersion.AutoDetect => GetDataProvider(options, default, DetectServerVersion(options, default) ?? DefaultVersion),
				FirebirdVersion.v25        => _firebirdDataProvider25.Value,
				FirebirdVersion.v3         => _firebirdDataProvider3 .Value,
				FirebirdVersion.v4         => _firebirdDataProvider4 .Value,
				FirebirdVersion.v5         => _firebirdDataProvider5 .Value,
				_                          => _firebirdDataProvider25.Value,
			};
		}

		public override FirebirdVersion? DetectServerVersion(DbConnection connection)
		{
			using var cmd = connection.CreateCommand();

			// note: query requires FB 2.1+, for older versions user should specify 2.5 provider explicitly
			cmd.CommandText = "SELECT rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database";
			var versionString = cmd.ExecuteScalar() as string;

			if (versionString == null || !Version.TryParse(versionString, out var version))
				return null;

			if (version.Major < 3)
				return FirebirdVersion.v25;

			if (version.Major < 4)
				return FirebirdVersion.v3;

			if (version.Major < 5)
				return FirebirdVersion.v4;

			return FirebirdVersion.v5;
		}

		protected override DbConnection CreateConnection(Provider provider, string connectionString)
		{
			return FirebirdProviderAdapter.Instance.CreateConnection(connectionString);
		}
	}
}
