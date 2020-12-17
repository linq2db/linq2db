using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using System.IO;
	using System.Security;
	using Data;
	using LinqToDB.Configuration;

	/// <summary>
	/// Contains Access provider management tools.
	/// </summary>
	public static class AccessTools
	{
		private static readonly Lazy<IDataProvider> _accessOleDbDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new AccessOleDbDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		private static readonly Lazy<IDataProvider> _accessODBCDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new AccessODBCDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (connectionString.Contains("Microsoft.ACE.OLEDB")
				|| connectionString.Contains("Microsoft.Jet.OLEDB"))
			{
				return _accessOleDbDataProvider.Value;
			}

			if (css.ProviderName == ProviderName.AccessOdbc
				|| css.Name.Contains("Access.Odbc"))
			{
				return _accessODBCDataProvider.Value;
			}

			if (css.ProviderName == ProviderName.Access || css.Name.Contains("Access"))
			{
				if (connectionString.Contains("*.mdb")
					|| connectionString.Contains("*.accdb"))
					return _accessODBCDataProvider.Value;

				return _accessOleDbDataProvider.Value;
			}

			return null;
		}

		/// <summary>
		/// Returns instance of Access database provider.
		/// </summary>
		/// <returns><see cref="AccessOleDbDataProvider"/> or <see cref="AccessODBCDataProvider"/> instance.</returns>
		public static IDataProvider GetDataProvider(string? providerName = null)
		{
			if (providerName == ProviderName.AccessOdbc)
				return _accessODBCDataProvider.Value;

			return _accessOleDbDataProvider.Value;
		}

		#region CreateDataConnection
		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided Access connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbTransaction transaction, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), transaction);
		}
		#endregion

		#region Database management
		/// <summary>
		/// Creates new Access database file. Requires Access OLE DB provider (JET or ACE) and ADOX.
		/// </summary>
		/// <param name="databaseName">Name of database to create.</param>
		/// <param name="deleteIfExists">If <c>true</c>, existing database will be removed before create.</param>
		/// <param name="provider">Name of OleDb provider to use to create database. Default value: "Microsoft.Jet.OLEDB.4.0".</param>
		/// <remarks>
		/// Provider value examples: Microsoft.Jet.OLEDB.4.0 (for JET database), Microsoft.ACE.OLEDB.12.0, Microsoft.ACE.OLEDB.15.0 (for ACE database).
		/// </remarks>
		public static void CreateDatabase(string databaseName, bool deleteIfExists = false, string provider = "Microsoft.Jet.OLEDB.4.0")
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			databaseName = databaseName.Trim();

			if (!databaseName.ToLower().EndsWith(".mdb"))
				databaseName += ".mdb";

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			var connectionString = $"Provider={provider};Data Source={databaseName};Locale Identifier=1033";

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".mdb",
				_ => CreateAccessDB(connectionString));
		}

		[SecuritySafeCritical]
		private static void CreateAccessDB(string connectionString)
		{
			using (var catalog = ComWrapper.Create("ADOX.Catalog"))
				using (var conn = ComWrapper.Wrap(catalog.Create(connectionString)))
					conn.Close();
		}

		/// <summary>
		/// Removes database file by database name.
		/// </summary>
		/// <param name="databaseName">Name of database to remove.</param>
		public static void DropDatabase(string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.DropFileDatabase(databaseName, ".mdb");
		}
		#endregion

		#region BulkCopy
		/// <summary>
		/// Default bulk copy mode, used for Access by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
		/// methods, if mode is not specified explicitly.
		/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
		/// </summary>
		public static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.MultipleRows;

		// If user has DataConnection - he can call BulkCopy directly and Tools methods only provide some
		// defaults for parameters
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
	}
}
