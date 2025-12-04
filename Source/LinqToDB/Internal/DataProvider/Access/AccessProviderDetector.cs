using System;
using System.Data.Common;
using System.IO;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessProviderDetector : ProviderDetectorBase<AccessProvider, AccessVersion>
	{
		public AccessProviderDetector() : base(AccessVersion.AutoDetect, AccessVersion.Jet)
		{
		}

		static readonly Lazy<IDataProvider> _accessJetOleDbDataProvider = CreateDataProvider<AccessJetOleDbDataProvider>();
		static readonly Lazy<IDataProvider> _accessJetODBCDataProvider  = CreateDataProvider<AccessJetODBCDataProvider>();
		static readonly Lazy<IDataProvider> _accessAceOleDbDataProvider = CreateDataProvider<AccessAceOleDbDataProvider>();
		static readonly Lazy<IDataProvider> _accessAceODBCDataProvider  = CreateDataProvider<AccessAceODBCDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ConnectionString?.Contains("Microsoft.ACE.OLEDB") == true)
				return _accessAceOleDbDataProvider.Value;

			if (options.ConnectionString?.Contains("Microsoft.Jet.OLEDB") == true)
				return _accessJetOleDbDataProvider.Value;

			if (options.ConnectionString?.Contains("(*.mdb, *.accdb)") == true)
				return _accessAceODBCDataProvider.Value;

			if (options.ConnectionString?.Contains("(*.mdb)") == true)
				return _accessJetODBCDataProvider.Value;

			switch (options.ProviderName)
			{
				case ProviderName.AccessJetOdbc: return _accessJetODBCDataProvider.Value;
				case ProviderName.AccessAceOdbc: return _accessAceODBCDataProvider.Value;
				case ProviderName.AccessJetOleDb: return _accessJetOleDbDataProvider.Value;
				case ProviderName.AccessAceOleDb: return _accessAceOleDbDataProvider.Value;
			}

			var provider = AccessProvider.AutoDetect;
			if (options.ProviderName == ProviderName.AccessOdbc)
			{
				provider = AccessProvider.ODBC;
			}
			else if (options.ProviderName != ProviderName.Access && options.ConfigurationString?.Contains("DataAccess") == true)
			{
				// when provider name is not set to known access provider name, we must check that we don't have detection
				// conflict with providers that use very "unique" Access substring like Oracle*DataAccess providers
				return null;
			}

			if (options.ConfigurationString?.Contains("Access") == true)
			{
				if (provider == AccessProvider.AutoDetect)
				{
					if (options.ConfigurationString.Contains("Access.Odbc"))
						provider = AccessProvider.ODBC;
					else if (options.ConfigurationString.Contains("Access.OleDb"))
						provider = AccessProvider.OleDb;
				}

				var version = AccessVersion.AutoDetect;

				if (options.ConfigurationString.Contains("Jet"))
					version = AccessVersion.Jet;
				else if (options.ConfigurationString.Contains("Ace"))
					version = AccessVersion.Ace;

				if (version == AccessVersion.AutoDetect && AutoDetectProvider)
					version = DetectServerVersion(options, provider) ?? DefaultVersion;

				return GetDataProvider(options, provider, version == AccessVersion.AutoDetect ? DefaultVersion : version);
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, AccessProvider provider, AccessVersion version)
		{
			if (provider == AccessProvider.AutoDetect)
			{
				var providerImpl = DetectProvider(options);
				if (providerImpl != null)
					return providerImpl;

				provider = DetectProvider();
			}

			return (provider, version) switch
			{
				(_, AccessVersion.AutoDetect)             => GetDataProvider(options, provider, DetectServerVersion(options, provider) ?? DefaultVersion),
				(AccessProvider.ODBC, AccessVersion.Jet)  => _accessJetODBCDataProvider.Value,
				(AccessProvider.ODBC, AccessVersion.Ace)  => _accessAceODBCDataProvider.Value,
				(AccessProvider.OleDb, AccessVersion.Jet) => _accessJetOleDbDataProvider.Value,
				(AccessProvider.OleDb, AccessVersion.Ace) => _accessAceOleDbDataProvider.Value,
				_                                         => _accessJetOleDbDataProvider.Value,
			};
		}

		public static AccessProvider DetectProvider()
		{
			var fileName = typeof(AccessProviderDetector).Assembly.GetFileName();
			var dirName  = Path.GetDirectoryName(fileName);

			return File.Exists(Path.Combine(dirName ?? ".", OdbcProviderAdapter.AssemblyName + ".dll"))
				? AccessProvider.ODBC
				: AccessProvider.OleDb;
		}

		public override AccessVersion? DetectServerVersion(DbConnection connection, DbTransaction? transaction)
		{
			// we don't know connection type, so we probe both
			try
			{
				var provider = OleDbProviderAdapter.GetInstance().ConnectionWrapper(connection).Provider;

				//JET: Provider=Microsoft.Jet.OLEDB.4.0
				//ACE: Provider=Microsoft.ACE.OLEDB.12.0 (or e.g. 15, etc)
				return provider.StartsWith("Microsoft.Jet.OLEDB.", StringComparison.Ordinal)
					? AccessVersion.Jet
					: AccessVersion.Ace;
			}
			catch
			{
			}

			try
			{
				var driver = OdbcProviderAdapter.GetInstance().ConnectionWrapper(connection).Driver;

				// ACE: Driver=ACEODBC.DLL
				// JET: Driver=odbcjt32.dll
				return driver.Equals("odbcjt32.dll", StringComparison.OrdinalIgnoreCase)
					? AccessVersion.Jet
					: AccessVersion.Ace;
			}
			catch
			{
			}

			return null;
		}

		protected override DbConnection CreateConnection(AccessProvider provider, string connectionString)
		{
			var adapter = provider == AccessProvider.ODBC
				? (IDynamicProviderAdapter)OdbcProviderAdapter.GetInstance()
				: OleDbProviderAdapter.GetInstance();
			return adapter.CreateConnection(connectionString);
		}
	}
}
