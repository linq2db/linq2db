using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Data;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Utility methods for working with Linq To DB and YDB,
	/// similar to <c>PostgreSQLTools</c> for PostgreSQL.
	/// </summary>
	[PublicAPI]
	public static partial class YdbTools
	{
		//--------------------------------------------------------------------
		//  Provider detector
		//--------------------------------------------------------------------

		private static YdbProviderDetector? _providerDetector;

		internal static YdbProviderDetector ProviderDetector
		{
			get => _providerDetector ??= new();   // Lazy initialization
			set => _providerDetector = value;
		}

		/// <summary>
		/// Enables or disables automatic provider detection.
		/// Default is <c>true</c>. Typically there is no reason to disable this,
		/// but the option is provided for consistency with other providers.
		/// </summary>
		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		//--------------------------------------------------------------------
		//  Data provider
		//--------------------------------------------------------------------

		/// <summary>
		/// Returns an instance of <see cref="YdbDataProvider"/>.
		/// The <paramref name="connectionString"/> is optional —
		/// it's not required by the provider itself, but included for API compatibility
		/// with other Linq To DB helper methods.
		/// </summary>
		/// <param name="connectionString">Optional connection string.</param>
		/// <returns>YDB data provider instance.</returns>
		public static IDataProvider GetDataProvider(string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(
				new ConnectionOptions(ConnectionString: connectionString),
				default,                                 // Provider (not used)
				YdbProviderDetector.Version.AutoDetect); // Version (only one currently exists)
		}

		//--------------------------------------------------------------------
		//  Assembly resolver
		//--------------------------------------------------------------------

		/// <summary>
		/// Registers YDB assembly loading from the specified directory.
		/// Required only if <c>Ydb.Sdk.dll</c> is placed next to the executable
		/// and not added via NuGet.
		/// </summary>
		/// <param name="path">Path to the directory containing the YDB assemblies.</param>
		public static void ResolveYdb(string path)
		{
			_ = new AssemblyResolver(path, YdbProviderAdapter.AssemblyName);
		}

		/// <summary>
		/// Registers a container assembly that already includes all
		/// the provider dependencies. Rarely needed, but available for completeness.
		/// </summary>
		/// <param name="assembly">Container assembly.</param>
		public static void ResolveYdb(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		//--------------------------------------------------------------------
		//  Quick DataConnection creation
		//--------------------------------------------------------------------

		/// <summary>
		/// Creates a <see cref="DataConnection"/> using a connection string.
		/// </summary>
		/// <param name="connectionString">YDB connection string.</param>
		/// <returns>A configured <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(string connectionString)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(GetDataProvider(connectionString), connectionString));
		}

		/// <summary>
		/// Creates a <see cref="DataConnection"/> from an existing <see cref="DbConnection"/>.
		/// </summary>
		/// <param name="connection">An open <see cref="DbConnection"/> to YDB.</param>
		/// <returns>A configured <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbConnection connection)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(GetDataProvider(connection.ConnectionString), connection));
		}

		/// <summary>
		/// Creates a <see cref="DataConnection"/> from an existing <see cref="DbTransaction"/>.
		/// </summary>
		/// <param name="transaction">An active <see cref="DbTransaction"/> for YDB.</param>
		/// <returns>A configured <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateDataConnection(DbTransaction transaction)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(GetDataProvider(transaction.Connection!.ConnectionString), transaction));
		}
	}
}
