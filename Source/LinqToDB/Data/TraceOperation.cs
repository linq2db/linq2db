using System.Data.Common;

namespace LinqToDB.Data
{
	/// <summary>
	/// Type of operation associated with specific trace event.
	/// </summary>
	/// <seealso cref="TraceInfo"/>
	public enum TraceOperation
	{
		/// <summary>
		/// <see cref="DbCommand.ExecuteNonQuery"/> or <see cref="DbCommand.ExecuteNonQueryAsync(System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		ExecuteNonQuery,

		/// <summary>
		/// <see cref="DbCommand.ExecuteReader(System.Data.CommandBehavior)"/> or <see cref="DbCommand.ExecuteReaderAsync(System.Data.CommandBehavior, System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		ExecuteReader,

		/// <summary>
		/// <see cref="DbCommand.ExecuteScalar"/> or <see cref="DbCommand.ExecuteScalarAsync(System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		ExecuteScalar,

		/// <summary>
		/// <see cref="DataContextExtensions.BulkCopy{T}(ITable{T}, System.Collections.Generic.IEnumerable{T})"/> or <see cref="DataContextExtensions.BulkCopyAsync{T}(IDataContext, int, System.Collections.Generic.IEnumerable{T}, System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		BulkCopy,

		/// <summary>
		/// <see cref="DbConnection.Open"/> or <see cref="DbConnection.OpenAsync(System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		Open,

		/// <summary>
		/// Mapper build operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		BuildMapping,

		/// <summary>
		/// Query runner disposal operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		DisposeQuery,

		/// <summary>
		/// <see cref="DataConnection.BeginTransaction()"/> or <see cref="DataConnection.BeginTransaction(System.Data.IsolationLevel)"/> or
		/// <see cref="DataConnection.BeginTransactionAsync(System.Threading.CancellationToken)"/> or <see cref="DataConnection.BeginTransactionAsync(System.Data.IsolationLevel, System.Threading.CancellationToken)"/>operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		BeginTransaction,

		/// <summary>
		/// <see cref="DataConnection.CommitTransaction"/> or <see cref="DataConnection.CommitTransactionAsync(System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		CommitTransaction,

		/// <summary>
		/// <see cref="DataConnection.RollbackTransaction"/> or <see cref="DataConnection.RollbackTransactionAsync(System.Threading.CancellationToken)"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		RollbackTransaction,

		/// <summary>
		/// <see cref="DataConnection.DisposeTransaction"/> or <see cref="DataConnection.DisposeTransactionAsync"/> operation.
		/// See also <seealso cref="TraceInfo.IsAsync"/>.
		/// </summary>
		DisposeTransaction
	}
}
