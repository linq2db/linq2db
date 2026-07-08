using System;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.DataProvider.Firebird;

namespace LinqToDB.DataProvider.Firebird
{
	/// <summary>
	/// Firebird ADO.NET provider registration and <see cref="DataConnection"/> factory helpers.
	/// </summary>
	[PublicAPI]
	public static class FirebirdTools
	{
		internal static FirebirdProviderDetector ProviderDetector = new();

		/// <summary>
		/// Gets or sets whether the Firebird dialect is detected automatically by querying the server engine
		/// version when it is not specified explicitly.
		/// </summary>
		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		/// <summary>
		/// Returns the Firebird <see cref="IDataProvider"/> for the requested (or auto-detected) dialect version.
		/// </summary>
		/// <param name="version">Firebird dialect version.</param>
		/// <param name="connectionString">Optional connection string, used for version auto-detection.</param>
		/// <param name="connection">Optional connection, used for version auto-detection.</param>
		/// <param name="transaction">Optional transaction, used for version auto-detection.</param>
		/// <returns>Firebird data provider instance.</returns>
		public static IDataProvider GetDataProvider(FirebirdVersion version = FirebirdVersion.AutoDetect, string? connectionString = null, DbConnection? connection = null, DbTransaction? transaction = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString, DbConnection: connection, DbTransaction: transaction), default, version);
		}

		/// <summary>
		/// Registers an assembly resolver that loads the Firebird ADO.NET client assembly
		/// (<c>FirebirdSql.Data.FirebirdClient</c>) from the specified path.
		/// </summary>
		/// <param name="path">Path to the folder or file containing the Firebird client assembly.</param>
		public static void ResolveFirebird(string path)
		{
			ArgumentNullException.ThrowIfNull(path);
			_ = new AssemblyResolver(path, FirebirdProviderAdapter.AssemblyName);
		}

		/// <summary>
		/// Registers the specified Firebird ADO.NET client assembly (<c>FirebirdSql.Data.FirebirdClient</c>) for resolution.
		/// </summary>
		/// <param name="assembly">The Firebird client assembly.</param>
		public static void ResolveFirebird(Assembly assembly)
		{
			ArgumentNullException.ThrowIfNull(assembly);
			_ = new AssemblyResolver(assembly, FirebirdProviderAdapter.AssemblyName);
		}

		#region CreateDataConnection

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided Firebird connection string.
		/// </summary>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="version">Firebird dialect version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), default, version), connectionString));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided connection object.
		/// </summary>
		/// <param name="connection">Connection instance.</param>
		/// <param name="version">Firebird dialect version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbConnection connection, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), default, version), connection));
		}

		/// <summary>
		/// Creates <see cref="DataConnection"/> object using provided transaction object.
		/// </summary>
		/// <param name="transaction">Transaction instance.</param>
		/// <param name="version">Firebird dialect version.</param>
		/// <returns><see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbTransaction transaction, FirebirdVersion version = FirebirdVersion.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), default, version), transaction));
		}

		#endregion

		/// <summary>
		/// Clears every Firebird connection pool process-wide (all connection strings).
		/// See <see cref="ClearPool(DbConnection)"/> / <see cref="ClearPool(string)"/> for a scoped alternative
		/// that leaves other databases' pools untouched.
		/// </summary>
		public static void ClearAllPools() => FirebirdProviderAdapter.Instance.ClearAllPools();

		/// <summary>
		/// Clears the connection pool associated with the given connection's connection string.
		/// Unlike <see cref="ClearAllPools"/> (which evicts every Firebird pool process-wide), this only
		/// affects the pool for <paramref name="connection"/>'s connection string, leaving connections for
		/// other databases/servers untouched.
		/// </summary>
		/// <param name="connection">Connection whose connection-string pool should be cleared.</param>
		public static void ClearPool(DbConnection connection) => FirebirdProviderAdapter.Instance.ClearPool(connection);

		/// <summary>
		/// Clears the connection pool for the given connection string. Same scoping as
		/// <see cref="ClearPool(DbConnection)"/>, but keyed by the connection string directly (no live
		/// connection object required).
		/// </summary>
		/// <param name="connectionString">Connection string whose pool should be cleared.</param>
		public static void ClearPool(string connectionString) => FirebirdProviderAdapter.Instance.ClearPoolByConnectionString(connectionString);
	}
}
