using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;

	[PublicAPI]
	public static partial class SqlServerTools
	{
		#region Init

		internal static SqlServerProviderDetector ProviderDetector = new();

		public static SqlServerProvider DefaultProvider
		{
			get => SqlServerProviderDetector.DefaultProvider;
			set => SqlServerProviderDetector.DefaultProvider = value;
		}

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		public static string QuoteIdentifier(string identifier)
		{
			return QuoteIdentifier(new StringBuilder(), identifier).ToString();
		}

		internal static StringBuilder QuoteIdentifier(StringBuilder sb, string identifier)
		{
			sb.Append('[');

			if (identifier.Contains("]"))
				sb.Append(identifier.Replace("]", "]]"));
			else
				sb.Append(identifier);

			sb.Append(']');

			return sb;
		}

		/// <summary>
		/// Connects to SQL Server Database and parses version information.
		/// </summary>
		/// <param name="provider"></param>
		/// <param name="connectionString"></param>
		/// <returns>Detected SQL Server version.</returns>
		public static SqlServerVersion? DetectServerVersion(SqlServerProvider provider, string connectionString)
		{
			return ProviderDetector.DetectServerVersion(provider, connectionString);
		}

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion  version          = SqlServerVersion.v2008,
			SqlServerProvider provider         = SqlServerProvider.SystemDataSqlClient,
			string?           connectionString = null)
		{
			return ProviderDetector.GetDataProvider(provider, version, connectionString);
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
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			return new DataConnection(GetDataProvider(version, provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection      connection,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			if (version is SqlServerVersion.AutoDetect)
				version = ProviderDetector.DetectServerVersion((SqlServerProviderAdapter.SqlConnection)(IDbConnection)connection) ?? SqlServerVersion.v2008;

			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction     transaction,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.SystemDataSqlClient)
		{
			if (version is SqlServerVersion.AutoDetect)
			{
				version = ProviderDetector.DetectServerVersion((SqlServerProviderAdapter.SqlConnection)(IDbConnection)transaction.Connection!) ?? SqlServerVersion.v2008;
			}

			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

		#endregion

		#region BulkCopy

		public  static BulkCopyType  DefaultBulkCopyType { get; set; } = BulkCopyType.ProviderSpecific;

		#endregion

		[Obsolete("Use 'QueryHint(Hints.Option.Recompile)' instead.")]
		public static class Sql
		{
			public const string OptionRecompile = "OPTION(RECOMPILE)";
		}
	}
}
