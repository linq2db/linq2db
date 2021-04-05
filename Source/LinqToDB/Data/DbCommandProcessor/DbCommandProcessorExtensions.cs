using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Data.DbCommandProcessor
{
	/// <summary>
	/// Extension point adds possibility to change implementation of DbCommand methods ExecuteScalar, ExecuteNonQuery, ExecureReader and their Async equivalents.
	/// One of possible use cases: put commands into queue and initiate them in special separate thread, to overcome lock contention in TimerQueue.Timer
	/// </summary>
	[PublicAPI]
	public static class DbCommandProcessorExtensions
	{
		/// <summary>
		/// Single instance. Change of it is not thread safe.
		/// </summary>
		public static IDbCommandProcessor? Instance { get; set; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static object? ExecuteScalarExt(this DbCommand cmd) =>
			Instance == null ? cmd.ExecuteScalar() : Instance.ExecuteScalar(cmd);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<object?> ExecuteScalarExtAsync(this DbCommand cmd, CancellationToken token) =>
			Instance == null ? cmd.ExecuteScalarAsync(token) : Instance.ExecuteScalarAsync(cmd, token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ExecuteNonQueryExt(this DbCommand cmd) =>
			Instance == null ? cmd.ExecuteNonQuery() : Instance.ExecuteNonQuery(cmd);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<int> ExecuteNonQueryExtAsync(this DbCommand cmd, CancellationToken token) =>
			Instance == null ? cmd.ExecuteNonQueryAsync(token) : Instance.ExecuteNonQueryAsync(cmd, token);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static DbDataReader ExecuteReaderExt(this DbCommand cmd, CommandBehavior commandBehavior) =>
			Instance == null ? cmd.ExecuteReader(commandBehavior) : Instance.ExecuteReader(cmd, commandBehavior);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Task<DbDataReader> ExecuteReaderExtAsync(this DbCommand cmd, CommandBehavior commandBehavior, CancellationToken token) =>
			Instance == null ? cmd.ExecuteReaderAsync(commandBehavior, token) : Instance.ExecuteReaderAsync(cmd, commandBehavior, token);
	}
}
