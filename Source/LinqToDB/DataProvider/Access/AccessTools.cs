using System;
using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Security;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Access;

namespace LinqToDB.DataProvider.Access
{
	/// <summary>
	/// Contains Access provider management tools.
	/// </summary>
	public static class AccessTools
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
		public static IDataProvider GetDataProvider(AccessVersion version = AccessVersion.AutoDetect, AccessProvider provider = AccessProvider.AutoDetect, string? connectionString = null, DbConnection? connection = null, DbTransaction? transaction = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString, DbConnection: connection, DbTransaction: transaction), provider, version);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, AccessVersion version = AccessVersion.AutoDetect, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString));
		}

		public static DataConnection CreateDataConnection(DbConnection connection, AccessVersion version = AccessVersion.AutoDetect, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection));
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, AccessVersion version = AccessVersion.AutoDetect, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction));
		}

		#endregion

		#region Database management

		/// <summary>
		/// Creates new Access database file. Requires Access OLE DB provider (JET or ACE) and ADOX.
		/// </summary>
		/// <param name="databaseName">Name of database to create.</param>
		/// <param name="deleteIfExists">If <see langword="true"/>, existing database will be removed before create.</param>
		/// <param name="version">Access engine to use to create database. Default value: <see cref="AccessVersion.Ace"/>.</param>
		public static void CreateDatabase(string databaseName, bool deleteIfExists = false, AccessVersion version = AccessVersion.Ace)
		{
			ArgumentNullException.ThrowIfNull(databaseName);

			databaseName = databaseName.Trim();

			var defaultExtension = version == AccessVersion.Ace ? ".accdb" : ".mdb";

			// add extension if not specified
			if (!databaseName.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase) && !databaseName.EndsWith(defaultExtension, StringComparison.OrdinalIgnoreCase))
				databaseName += defaultExtension;

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			// note that it is fine to use Microsoft.ACE.OLEDB.12.0 even if newer version installed,
			// as newer versions register itself with older version number too for backward compatibility with such
			// situations when provider name is hardcoded
			var connectionString = $"Provider={(version == AccessVersion.Jet ? "Microsoft.Jet.OLEDB.4.0" : "Microsoft.ACE.OLEDB.12.0")};Data Source={databaseName};Locale Identifier=1033";

			CreateAccessDB(connectionString);
		}

		/// <summary>
		/// Creates new Access database file. Requires Access OLE DB provider (JET or ACE) and ADOX.
		/// </summary>
		/// <param name="databaseName">Name of database to create.</param>
		/// <param name="deleteIfExists">If <see langword="true"/>, existing database will be removed before create.</param>
		/// <param name="provider">Name of OleDb provider to use to create database. Default value: "Microsoft.Jet.OLEDB.4.0".</param>
		/// <remarks>
		/// Provider value examples: Microsoft.Jet.OLEDB.4.0 (for JET database), Microsoft.ACE.OLEDB.12.0, Microsoft.ACE.OLEDB.15.0 (for ACE database).
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with 'AccessVersion version' argument. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static void CreateDatabase(string databaseName, bool deleteIfExists = false, string provider = "Microsoft.Jet.OLEDB.4.0")
		{
			ArgumentNullException.ThrowIfNull(databaseName);

			databaseName = databaseName.Trim();

			if (!databaseName.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase))
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

		/// <summary>
		/// Creates new Access database file using the specified provider.
		/// </summary>
		/// <param name="databaseName">Name of database to create.</param>
		/// <param name="provider">Provider to create the file with. <see cref="AccessProvider.LibRed"/> uses the managed
		/// LibRed engine and works on any OS; any other value uses ADOX with the Access OLE DB provider, as
		/// <see cref="CreateDatabase(string, bool, AccessVersion)"/> does.</param>
		/// <param name="deleteIfExists">If <see langword="true"/>, existing database will be removed before create.</param>
		/// <param name="version">Database format when <paramref name="databaseName"/> has no extension:
		/// <see cref="AccessVersion.Ace"/> creates an <c>.accdb</c> file, any other value an <c>.mdb</c> one. With any provider
		/// other than <see cref="AccessProvider.LibRed"/> it also selects the OLE DB engine (ACE or Jet), as
		/// <see cref="CreateDatabase(string, bool, AccessVersion)"/> does. Default value: <see cref="AccessVersion.Ace"/>.</param>
		public static void CreateDatabase(string databaseName, AccessProvider provider, bool deleteIfExists = false, AccessVersion version = AccessVersion.Ace)
		{
			if (provider != AccessProvider.LibRed)
			{
				CreateDatabase(databaseName, deleteIfExists, version);
				return;
			}

			ArgumentNullException.ThrowIfNull(databaseName);

			databaseName = databaseName.Trim();

			var defaultExtension = version == AccessVersion.Ace ? ".accdb" : ".mdb";

			// add extension if not specified
			if (!databaseName.EndsWith(".mdb", StringComparison.OrdinalIgnoreCase) && !databaseName.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase))
				databaseName += defaultExtension;

			var adapter          = LibRedProviderAdapter.GetInstance();
			var connectionString = $"Data Source={databaseName}";

			if (adapter.DatabaseExists(connectionString))
			{
				if (!deleteIfExists)
					return;

				adapter.DropDatabase(connectionString);
			}

			adapter.CreateDatabase(connectionString, databaseName.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase));
		}

		[SecuritySafeCritical]
		private static void CreateAccessDB(string connectionString)
		{
			using var catalog = ComWrapper.Create("ADOX.Catalog");
			using var conn = ComWrapper.Wrap(catalog.Create(connectionString));
			conn.Close();
		}

		/// <summary>
		/// Removes database file by database name.
		/// </summary>
		/// <param name="databaseName">Name of database to remove.</param>
		public static void DropDatabase(string databaseName, string? extension = null)
		{
			ArgumentNullException.ThrowIfNull(databaseName);

			DataTools.DropFileDatabase(databaseName, extension ?? ".mdb");
		}

		/// <summary>
		/// Removes database file by database name using the specified provider.
		/// </summary>
		/// <param name="databaseName">Name of database to remove.</param>
		/// <param name="provider">Provider the file was used with. <see cref="AccessProvider.LibRed"/> drops it through the
		/// LibRed engine; any other value deletes the file, as <see cref="DropDatabase(string, string?)"/> does.</param>
		/// <param name="extension">File extension to try when no file named <paramref name="databaseName"/> exists.
		/// Default value: <c>.mdb</c>.</param>
		public static void DropDatabase(string databaseName, AccessProvider provider, string? extension = null)
		{
			if (provider != AccessProvider.LibRed)
			{
				DropDatabase(databaseName, extension);
				return;
			}

			ArgumentNullException.ThrowIfNull(databaseName);

			databaseName = databaseName.Trim();
			extension  ??= ".mdb";

			if (!File.Exists(databaseName) && !databaseName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
				databaseName += extension;

			var adapter          = LibRedProviderAdapter.GetInstance();
			var connectionString = $"Data Source={databaseName}";

			if (adapter.DatabaseExists(connectionString))
				adapter.DropDatabase(connectionString);
		}

		#endregion
	}
}
