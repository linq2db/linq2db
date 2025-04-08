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
		public static IDataProvider GetDataProvider(AccessVersion version = AccessVersion.AutoDetect, AccessProvider provider = AccessProvider.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version);
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
		/// <param name="deleteIfExists">If <c>true</c>, existing database will be removed before create.</param>
		/// <param name="version">Access engine to use to create database. Default value: <see cref="AccessVersion.Ace"/>.</param>
		public static void CreateDatabase(string databaseName, bool deleteIfExists = false, AccessVersion version = AccessVersion.Ace)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			databaseName = databaseName.Trim();

			var defaultExtension = version == AccessVersion.Ace ? ".accdb" : ".mdb";

			// add extension if not specified
			if (!databaseName.ToLowerInvariant().EndsWith(".mdb") && !databaseName.ToLowerInvariant().EndsWith(defaultExtension))
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
		/// <param name="deleteIfExists">If <c>true</c>, existing database will be removed before create.</param>
		/// <param name="provider">Name of OleDb provider to use to create database. Default value: "Microsoft.Jet.OLEDB.4.0".</param>
		/// <remarks>
		/// Provider value examples: Microsoft.Jet.OLEDB.4.0 (for JET database), Microsoft.ACE.OLEDB.12.0, Microsoft.ACE.OLEDB.15.0 (for ACE database).
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with 'AccessVersion version' argument. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
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
		public static void DropDatabase(string databaseName, string? extension = null)
		{
			if (databaseName == null) throw new ArgumentNullException(nameof(databaseName));

			DataTools.DropFileDatabase(databaseName, extension ?? ".mdb");
		}

		#endregion
	}
}
