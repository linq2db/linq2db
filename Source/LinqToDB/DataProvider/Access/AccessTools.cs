using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.Access
{
	using System.IO;
	using System.Runtime.InteropServices;
	using Data;
	using LinqToDB.Configuration;

	/// <summary>
	/// Contains Access provider management tools.
	/// </summary>
	public static class AccessTools
	{
		private static readonly Lazy<IDataProvider> _accessDataProvider = new Lazy<IDataProvider>(() =>
		{
			var provider = new AccessDataProvider();

			DataConnection.AddDataProvider(provider);

			return provider;
		}, true);

		internal static IDataProvider? ProviderDetector(IConnectionStringSettings css, string connectionString)
		{
			if (css.ProviderName == ProviderName.Access
				|| css.Name.Contains("Access")
				|| connectionString.Contains("Microsoft.ACE.OLEDB")
				|| connectionString.Contains("Microsoft.Jet.OLEDB"))
			{
				return _accessDataProvider.Value;
			}

			return null;
		}

		/// <summary>
		/// Returns default instance of Access database provider.
		/// </summary>
		/// <returns><see cref="AccessDataProvider"/> instance.</returns>
		public static IDataProvider GetDataProvider()
		{
			return _accessDataProvider.Value;
		}

		#region CreateDataConnection
		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided Access connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(_accessDataProvider.Value, connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbConnection connection)
		{
			return new DataConnection(_accessDataProvider.Value, connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(IDbTransaction transaction)
		{
			return new DataConnection(_accessDataProvider.Value, transaction);
		}
		#endregion

		#region Database management
		// ADOX.Catalog
		[ComImport, Guid("00000602-0000-0010-8000-00AA006D2EA4")]
		class CatalogClass
		{
		}

		/// <summary>
		/// Creates new Access database file. Requires Jet OLE DB provider and ADOX.
		/// </summary>
		/// <param name="databaseName">Name of database to create.</param>
		/// <param name="deleteIfExists">If <c>true</c>, existing database will be removed before create.</param>
		public static void CreateDatabase(string databaseName, bool deleteIfExists = false)
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

			var connectionString = string.Format(
				@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Locale Identifier=1033;Jet OLEDB:Engine Type=5",
				databaseName);

			DataTools.CreateFileDatabase(
				databaseName, deleteIfExists, ".mdb",
				dbName =>
				{
					dynamic catalog = new CatalogClass();

					var conn = catalog.Create(connectionString);

					if (conn != null)
						conn.Close();
				});
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

		// TODO: V3 - maybe we should remove bulk copy methods from tools?
		// If user has DataConnection - he can call BulkCopy directly and Tools methods only provide some
		// defaults for parameters
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
