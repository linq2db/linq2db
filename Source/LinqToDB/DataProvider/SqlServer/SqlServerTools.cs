﻿using System;
using System.Data.Common;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;

using LinqToDB.Common.Internal;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.SqlServer
{
	[PublicAPI]
	public static partial class SqlServerTools
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
			sb.Append('[');

			if (identifier.Contains("]"))
				sb.Append(identifier.Replace("]", "]]"));
			else
				sb.Append(identifier);

			sb.Append(']');

			return sb;
		}

		#endregion

		#region Public Members

		public static IDataProvider GetDataProvider(
			SqlServerVersion  version          = SqlServerVersion.AutoDetect,
			SqlServerProvider provider         = SqlServerProvider.AutoDetect,
			string?           connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version);
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
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString: connectionString), provider, version), connectionString);
		}

		public static DataConnection CreateDataConnection(
			DbConnection      connection,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbConnection: connection), provider, version), connection);
		}

		public static DataConnection CreateDataConnection(
			DbTransaction     transaction,
			SqlServerVersion  version  = SqlServerVersion.AutoDetect,
			SqlServerProvider provider = SqlServerProvider.AutoDetect)
		{
			return new DataConnection(ProviderDetector.GetDataProvider(new ConnectionOptions(DbTransaction: transaction), provider, version), transaction);
		}

		#endregion
	}
}
