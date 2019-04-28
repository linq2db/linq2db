using JetBrains.Annotations;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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
		public static IDbCommandProcessor Instance { get; set; } = new DbCommandDefaultProcessor();

		public static object ExecuteScalarExt(this IDbCommand cmd) =>
			Instance.ExecuteScalar((DbCommand)cmd);

		public static Task<object> ExecuteScalarExtAsync(this DbCommand cmd, CancellationToken ct) =>
			Instance.ExecuteScalarAsync(cmd, ct);

		public static int ExecuteNonQueryExt(this IDbCommand cmd) =>
			Instance.ExecuteNonQuery((DbCommand)cmd);

		public static Task<int> ExecuteNonQueryExtAsync(this DbCommand cmd, CancellationToken ct) =>
			Instance.ExecuteNonQueryAsync(cmd, ct);

		public static DbDataReader ExecuteReaderExt(this IDbCommand cmd, CommandBehavior commandBehavior) =>
			Instance.ExecuteReader((DbCommand)cmd, commandBehavior);

		public static Task<DbDataReader> ExecuteReaderExtAsync(this DbCommand cmd, CommandBehavior commandBehavior, CancellationToken ct) =>
			Instance.ExecuteReaderAsync(cmd, commandBehavior, ct);
	}
}
