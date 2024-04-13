using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Security;

namespace LinqToDB.DataProvider.Access
{
	using Data;

	/// <summary>
	/// Contains Access provider management tools.
	/// </summary>
	public static partial class AccessTools
	{
		internal static AccessProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		/// <summary>
		/// Returns instance of Access database provider.
		/// </summary>
		public static IDataProvider GetDataProvider(AccessProvider provider = AccessProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(connectionString), provider, default);
		}

		/// <summary>
		/// Returns instance of Access database provider.
		/// </summary>
		/// <returns><see cref="AccessOleDbDataProvider"/> or <see cref="AccessODBCDataProvider"/> instance.</returns>
		[Obsolete($"Use overload with {nameof(AccessProvider)} parameter")]
		public static IDataProvider GetDataProvider(string? providerName = null, string? connectionString = null)
		{
			return providerName switch
			{
				ProviderName.AccessOdbc => GetDataProvider(AccessProvider.ODBC, connectionString),
				_                       => GetDataProvider(AccessProvider.AutoDetect, connectionString),
			};
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(GetDataProvider(provider), transaction);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided Access connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		[Obsolete($"Use overload with {nameof(AccessProvider)} parameter")]
		public static DataConnection CreateDataConnection(string connectionString, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName, connectionString), connectionString);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		[Obsolete($"Use overload with {nameof(AccessProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbConnection connection, string? providerName = null)
		{
			return new DataConnection(GetDataProvider(providerName), connection);
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="providerName">Provider name.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		[Obsolete($"Use overload with {nameof(AccessProvider)} parameter")]
		public static DataConnection CreateDataConnection(DbTransaction transaction, string? providerName = null)
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

			if (!databaseName.ToLowerInvariant().EndsWith(".mdb"))
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
		[Obsolete("Use AccessOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => AccessOptions.Default.BulkCopyType;
			set => AccessOptions.Default = AccessOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
