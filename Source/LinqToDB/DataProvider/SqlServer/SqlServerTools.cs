using System.Data.Common;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.SqlServer;

namespace LinqToDB.DataProvider.SqlServer
{
	[PublicAPI]
	public static class SqlServerTools
	{
		#region Init

		internal static SqlServerProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static string QuoteIdentifier(string identifier)
		{
			using var sb = Pools.StringBuilder.Allocate();
			return QuoteIdentifier(sb.Value, identifier).ToString();
		}

		internal static StringBuilder QuoteIdentifier(StringBuilder sb, string identifier)
		{
			return sb
				.Append('[')
				.Append(identifier.Replace("]", "]]"))
				.Append(']');
		}

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion  version          = SqlServerVersion.AutoDetect,
			SqlServerProvider provider         = SqlServerProvider.AutoDetect,
			string?           connectionString = null,
			DbConnection?     connection       = null,
			DbTransaction?    transaction      = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString, DbConnection: connection, DbTransaction: transaction), provider, version);
		}

		/// <summary>
		/// Tries to load and register spatial types using provided path to types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(string path)
		{
			SqlServerProviderDetector.ResolveSqlTypes(path);
		}

		/// <summary>
		/// Registers spatial types assembly (Microsoft.SqlServer.Types).
		/// Also check https://linq2db.github.io/articles/FAQ.html#how-can-i-use-sql-server-spatial-types
		/// for additional required configuration steps.
		/// </summary>
		public static void ResolveSqlTypes(Assembly assembly)
		{
			SqlServerProviderDetector.ResolveSqlTypes(assembly);
		}

		#endregion

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(
			string            connectionString,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnectionString(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString));
		}

		public static DataConnection CreateDataConnection(
			DbConnection      connection,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection));
		}

		public static DataConnection CreateDataConnection(
			DbTransaction     transaction,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			return new DataConnection(new DataOptions()
				.UseTransaction(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction));
		}

		#endregion
	}
}
