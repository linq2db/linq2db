using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;
	using LinqToDB.Configuration;

	[PublicAPI]
	public static class FirebirdTools
	{
		private static readonly Lazy<IDataProvider> _firebird25DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v2_5, FirebirdDialect.Dialect3);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _firebird25D1DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v2_5, FirebirdDialect.Dialect1);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _firebird3DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v3, FirebirdDialect.Dialect3);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _firebird3D1DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v3, FirebirdDialect.Dialect1);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _firebird4DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v4, FirebirdDialect.Dialect3);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _firebird4D1DataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new FirebirdDataProvider(FirebirdVersion.v4, FirebirdDialect.Dialect1);

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		public static bool AutoDetectProvider { get; set; } = true;

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			switch (css.ProviderName)
			{
				case ProviderName.Firebird25                : return _firebird25DataProvider.Value;
				case ProviderName.Firebird25Dialect1        : return _firebird25D1DataProvider.Value;
				case ProviderName.Firebird3                 : return _firebird3DataProvider.Value;
				case ProviderName.Firebird3Dialect1         : return _firebird3D1DataProvider.Value;
				case ProviderName.Firebird4                 : return _firebird4DataProvider.Value;
				case ProviderName.Firebird4Dialect1         : return _firebird4D1DataProvider.Value;
				case ""                                     :
				case null                                   :
					if (css.Name.Contains("Firebird"))
						goto case FirebirdProviderAdapter.ClientNamespace;
					break;
				case FirebirdProviderAdapter.ClientNamespace:
				case var providerName when providerName.Contains("Firebird") || providerName.Contains(FirebirdProviderAdapter.ClientNamespace):

					switch (css.Name)
					{
						case ProviderName.Firebird25        : return _firebird25DataProvider.Value;
						case ProviderName.Firebird25Dialect1: return _firebird25D1DataProvider.Value;
						case ProviderName.Firebird3         : return _firebird3DataProvider.Value;
						case ProviderName.Firebird3Dialect1 : return _firebird3D1DataProvider.Value;
						case ProviderName.Firebird4         : return _firebird4DataProvider.Value;
						case ProviderName.Firebird4Dialect1 : return _firebird4D1DataProvider.Value;
					}

					if (css.Name.Contains("25") || css.Name.Contains("2.5"))
						if (css.Name.Contains("Dialect1") || css.Name.Contains("Dialect.1"))
							return _firebird25D1DataProvider.Value;
						else
							return _firebird25DataProvider.Value;

					if (css.Name.Contains("3"))
						if (css.Name.Contains("Dialect1") || css.Name.Contains("Dialect.1"))
							return _firebird3D1DataProvider.Value;
						else
							return _firebird3DataProvider.Value;

					if (css.Name.Contains("4"))
						if (css.Name.Contains("Dialect1") || css.Name.Contains("Dialect.1"))
							return _firebird4D1DataProvider.Value;
						else
							return _firebird4DataProvider.Value;

					if (AutoDetectProvider)
					{
						try
						{
							var cs = string.IsNullOrWhiteSpace(connectionString) ? css.ConnectionString : connectionString;

							var csBuilder   = FirebirdProviderAdapter.GetInstance().CreateConnectionStringBuilder(cs);
							using (var conn = FirebirdProviderAdapter.GetInstance().CreateConnection(cs))
							{
								conn.Open();

								using (var cmd = conn.CreateCommand())
								{
									// conn.ServerVersion returns unusable string like this:
									// WI-T4.0.0.1963 Firebird 4.0 Beta 2/tcp (XXX)/P13:C
									cmd.CommandText = "SELECT rdb$get_context('SYSTEM', 'ENGINE_VERSION') from rdb$database; ";

									var version = new Version((string)cmd.ExecuteScalar());

									if (version.Major <= 3)
										return csBuilder.Dialect == 1 ? _firebird25D1DataProvider.Value : _firebird25DataProvider.Value;
									if (version.Major == 3)
										return csBuilder.Dialect == 1 ? _firebird3D1DataProvider.Value : _firebird3DataProvider.Value;
									if (version.Major > 3)
										return csBuilder.Dialect == 1 ? _firebird4D1DataProvider.Value : _firebird4DataProvider.Value;
								}
							}
						}
						catch
						{
							return _firebird25DataProvider.Value;
						}
					}

					return GetDataProvider();
			}

			return null;
		}

		public static IDataProvider GetDataProvider(FirebirdVersion version = FirebirdVersion.v2_5, FirebirdDialect dialect = FirebirdDialect.Dialect3)
		{
			return version switch
			{
				FirebirdVersion.v3 => dialect == FirebirdDialect.Dialect1 ? _firebird3D1DataProvider.Value : _firebird3DataProvider.Value,
				FirebirdVersion.v4 => dialect == FirebirdDialect.Dialect1 ? _firebird4D1DataProvider.Value : _firebird4DataProvider.Value,
				_ => dialect == FirebirdDialect.Dialect1 ? _firebird25D1DataProvider.Value : _firebird25DataProvider.Value,
			};
		}

		public static void ResolveFirebird(string path)
		{
			if (path == null) throw new ArgumentNullException(nameof(path));
			new AssemblyResolver(path, FirebirdProviderAdapter.AssemblyName);
		}

		public static void ResolveFirebird(Assembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException(nameof(assembly));
			new AssemblyResolver(assembly, FirebirdProviderAdapter.AssemblyName);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, FirebirdVersion version = FirebirdVersion.v2_5, FirebirdDialect dialect = FirebirdDialect.Dialect3)
		{
			return new DataConnection(GetDataProvider(version, dialect), connectionString);
		}

		public static DataConnection CreateDataConnection(IDbConnection connection, FirebirdVersion version = FirebirdVersion.v2_5, FirebirdDialect dialect = FirebirdDialect.Dialect3)
		{
			return new DataConnection(GetDataProvider(version, dialect), connection);
		}

		public static DataConnection CreateDataConnection(IDbTransaction transaction, FirebirdVersion version = FirebirdVersion.v2_5, FirebirdDialect dialect = FirebirdDialect.Dialect3)
		{
			return new DataConnection(GetDataProvider(version, dialect), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		[Obsolete("Please use the BulkCopy extension methods within DataConnectionExtensions")]
		public static BulkCopyRowsCopied MultipleRowsCopy<T>(
			DataConnection              dataConnection,
			IEnumerable<T>              source,
			int                         maxBatchSize       = 1000,
			Action<BulkCopyRowsCopied>? rowsCopiedCallback = null)
			where T : class
		{
			return dataConnection.BulkCopy(
				new BulkCopyOptions
				{
					BulkCopyType       = BulkCopyType.MultipleRows,
					MaxBatchSize       = maxBatchSize,
					RowsCopiedCallback = rowsCopiedCallback,
				}, source);
		}

		#endregion

		#region ClearAllPools

		public static void ClearAllPools() => FirebirdProviderAdapter.GetInstance().ClearAllPools();

		#endregion
	}
}
