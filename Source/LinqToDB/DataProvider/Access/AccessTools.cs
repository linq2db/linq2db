﻿using System;
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
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, default), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, default), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, AccessProvider provider = AccessProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, default), transaction);
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
	}
}
