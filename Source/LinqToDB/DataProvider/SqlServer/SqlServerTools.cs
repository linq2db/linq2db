using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;
	using Common.Internal;
	using Extensions;

	[PublicAPI]
	public static partial class SqlServerTools
	{
		#region Init

		internal static SqlServerProviderDetector ProviderDetector = new();

		public static string QuoteIdentifier(string identifier)
		{
			using var sb = Pools.StringBuilder.Allocate();
			return QuoteIdentifier(sb.Value, identifier).ToString();
		}

		internal static StringBuilder QuoteIdentifier(StringBuilder sb, string identifier)
		{
			sb.Append('[');

			if (identifier.ContainsEx("]"))
				sb.Append(identifier.ReplaceEx("]", "]]"));
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
			return ProviderDetector.DetectServerVersion(new ConnectionOptions(ConnectionString : connectionString), provider);
		}

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion  version          = SqlServerVersion.AutoDetect,
			SqlServerProvider provider         = SqlServerProvider.AutoDetect,
			string?           connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString : connectionString), provider, version);
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
			return new DataConnection(GetDataProvider(version, provider, connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection      connection,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			if (version is SqlServerVersion.AutoDetect)
				version = ProviderDetector.DetectServerVersion((SqlServerProviderAdapter.SqlConnection)(IDbConnection)connection) ?? ProviderDetector.DefaultVersion;

			return new DataConnection(GetDataProvider(version, provider), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction     transaction,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			if (version is SqlServerVersion.AutoDetect)
			{
				version = ProviderDetector.DetectServerVersion((SqlServerProviderAdapter.SqlConnection)(IDbConnection)transaction.Connection!) ?? ProviderDetector.DefaultVersion;
			}

			return new DataConnection(GetDataProvider(version, provider), transaction);
		}

		#endregion

		#region BulkCopy

		[Obsolete("Use SqlServerOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => SqlServerOptions.Default.BulkCopyType;
			set => SqlServerOptions.Default = SqlServerOptions.Default with { BulkCopyType = value };
		}

		#endregion
	}
}
