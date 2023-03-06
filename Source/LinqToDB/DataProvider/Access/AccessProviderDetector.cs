using System;
using System.IO;

namespace LinqToDB.DataProvider.Access
{
	using System.Data.Common;
	using Common;
	using Data;

	sealed class AccessProviderDetector : ProviderDetectorBase<AccessProvider, AccessProviderDetector.Dialect, DbConnection>
	{
		internal enum Dialect { }

		public AccessProviderDetector() : base(default, default)
		{
		}

		static readonly Lazy<IDataProvider> _accessOleDbDataProvider = DataConnection.CreateDataProvider<AccessOleDbDataProvider>();
		static readonly Lazy<IDataProvider> _accessODBCDataProvider  = DataConnection.CreateDataProvider<AccessODBCDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			var provider = options.ProviderName switch
			{
				OdbcProviderAdapter.ClientNamespace  => AccessProvider.ODBC,
				OleDbProviderAdapter.ClientNamespace => AccessProvider.OleDb,
				_                                    => DetectProvider()
			};

			if (options.ConnectionString?.Contains("Microsoft.ACE.OLEDB") == true || options.ConnectionString?.Contains("Microsoft.Jet.OLEDB") == true)
			{
				return _accessOleDbDataProvider.Value;
			}

			if (options.ProviderName == ProviderName.AccessOdbc
				|| options.ConfigurationString?.Contains("Access.Odbc") == true)
			{
				return _accessODBCDataProvider.Value;
			}

			if (options.ProviderName == ProviderName.Access || (options.ConfigurationString?.Contains("Access") == true && !options.ConfigurationString.Contains("DataAccess")))
			{
				if (options.ConnectionString?.Contains("*.mdb") == true
					|| options.ConnectionString?.Contains("*.accdb") == true)
					return _accessODBCDataProvider.Value;

				return _accessOleDbDataProvider.Value;
			}

			return null;
		}

		public override IDataProvider GetDataProvider(ConnectionOptions options, AccessProvider provider, Dialect version)
		{
			if (provider == AccessProvider.AutoDetect)
				provider = DetectProvider();

			return provider switch
			{
				AccessProvider.ODBC => _accessODBCDataProvider.Value,
				_                   => _accessOleDbDataProvider.Value,
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

		public override Dialect? DetectServerVersion(DbConnection connection)
		{
			return default(Dialect);
		}

		protected override DbConnection CreateConnection(AccessProvider provider, string connectionString)
		{
			return (provider == AccessProvider.ODBC
					? (IConnectionWrapper)OdbcProviderAdapter.GetInstance().CreateConnection(connectionString)
					: OleDbProviderAdapter.GetInstance().CreateConnection(connectionString))
				.Connection;
		}
	}
}
