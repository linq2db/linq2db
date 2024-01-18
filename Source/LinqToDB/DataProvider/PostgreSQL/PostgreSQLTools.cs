using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Data;

	[PublicAPI]
	public static partial class PostgreSQLTools
	{
		internal static PostgreSQLProviderDetector ProviderDetector = new();

		public static bool AutoDetectProvider
		{
			get => ProviderDetector.AutoDetectProvider;
			set => ProviderDetector.AutoDetectProvider = value;
		}

		/// <summary>
		/// Enables normalization of <see cref="DateTime"/> and <see cref="DateTimeOffset"/> data, passed to query
		/// as parameter or passed to <see cref="DataConnectionExtensions.BulkCopy{T}(ITable{T}, IEnumerable{T})"/> APIs,
		/// to comform with Npgsql 6 requerements:
		/// <list type="bullet">
		/// <item>convert <see cref="DateTimeOffset"/> value to UTC value with zero <see cref="DateTimeOffset.Offset"/></item>
		/// <item>Use <see cref="DateTimeKind.Utc"/> for <see cref="DateTime"/> timestamptz values</item>
		/// <item>Use <see cref="DateTimeKind.Unspecified"/> for <see cref="DateTime"/> timestamp values with <see cref="DateTimeKind.Utc"/> kind</item>
		/// </list>
		/// Default value: <c>true</c>.
		/// </summary>
		[Obsolete("Use PostgreSQLOptions.Default.NormalizeTimestampData instead.")]
		public static bool NormalizeTimestampData
		{
			get => PostgreSQLOptions.Default.NormalizeTimestampData;
			set => PostgreSQLOptions.Default = new PostgreSQLOptions(PostgreSQLOptions.Default) { NormalizeTimestampData = value };
		}

		public static IDataProvider GetDataProvider(PostgreSQLVersion version = PostgreSQLVersion.AutoDetect, string? connectionString = null)
		{
			return ProviderDetector.GetDataProvider(new ConnectionOptions(ConnectionString : connectionString), default, version);
		}

		public static void ResolvePostgreSQL(string path)
		{
			_ = new AssemblyResolver(path, NpgsqlProviderAdapter.AssemblyName);
		}

		public static void ResolvePostgreSQL(Assembly assembly)
		{
			_ = new AssemblyResolver(assembly, assembly.FullName!);
		}

		#region CreateDataConnection

		public static DataConnection CreateDataConnection(string connectionString, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version, connectionString: connectionString), connectionString);
		}

		public static DataConnection CreateDataConnection(DbConnection connection, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version), connection);
		}

		public static DataConnection CreateDataConnection(DbTransaction transaction, PostgreSQLVersion version = PostgreSQLVersion.AutoDetect)
		{
			return new DataConnection(GetDataProvider(version), transaction);
		}

		#endregion

		#region BulkCopy

		[Obsolete("Use PostgreSQLOptions.Default.BulkCopyType instead.")]
		public static BulkCopyType DefaultBulkCopyType
		{
			get => PostgreSQLOptions.Default.BulkCopyType;
			set => PostgreSQLOptions.Default = new PostgreSQLOptions(PostgreSQLOptions.Default) { BulkCopyType = value };
		}

		#endregion
	}
}
