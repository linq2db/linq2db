using System;
using System.Data.Common;
using System.IO;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Data;

	sealed class AccessProviderDetector : ProviderDetectorBase<AccessProvider, AccessProviderDetector.Dialect>
	{
		internal enum Dialect { }

		public AccessProviderDetector() : base()
		{
		}

		static readonly Lazy<IDataProvider> _accessOleDbDataProvider = CreateDataProvider<AccessOleDbDataProvider>();
		static readonly Lazy<IDataProvider> _accessODBCDataProvider  = CreateDataProvider<AccessODBCDataProvider>();

		public override IDataProvider? DetectProvider(ConnectionOptions options)
		{
			if (options.ConnectionString?.Contains("Microsoft.ACE.OLEDB") == true
			    || options.ConnectionString?.Contains("Microsoft.Jet.OLEDB") == true)
			{
				return _accessOleDbDataProvider.Value;
			}

			if (options.ConnectionString?.Contains("(*.mdb)") == true
				|| options.ConnectionString?.Contains("(*.mdb, *.accdb)") == true)
			{
				return _accessODBCDataProvider.Value;
			}

			if (options.ProviderName == ProviderName.AccessOdbc
				|| options.ConfigurationString?.Contains("Access.Odbc") == true)
			{
				return _accessODBCDataProvider.Value;
			}

			if (options.ProviderName == ProviderName.Access
			    || (options.ConfigurationString?.Contains("Access") == true
			        && !options.ConfigurationString.Contains("DataAccess")))
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
			{
				var providerImpl = DetectProvider(options);
				if (providerImpl != null)
					return providerImpl;

				provider = DetectProvider();
			}

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
			var adapter = provider == AccessProvider.ODBC
				? (IDynamicProviderAdapter)OdbcProviderAdapter.GetInstance()
				: OleDbProviderAdapter.GetInstance();
			return adapter.CreateConnection(connectionString);
		}
	}
}
