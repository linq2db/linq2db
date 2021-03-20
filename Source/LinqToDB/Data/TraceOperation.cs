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
		/// Synchronous <see cref="DbCommand.ExecuteNonQuery"/> operation.
		/// </summary>
		ExecuteNonQuery,

		/// <summary>
		/// Asynchronous <see cref="DbCommand.ExecuteNonQueryAsync(System.Threading.CancellationToken)"/> operation.
		/// </summary>
		ExecuteNonQueryAsync,

		/// <summary>
		/// Synchronous <see cref="DbCommand.ExecuteReader(System.Data.CommandBehavior)"/> operation.
		/// </summary>
		ExecuteReader,

		/// <summary>
		/// Asynchronous <see cref="DbCommand.ExecuteReaderAsync(System.Data.CommandBehavior, System.Threading.CancellationToken)"/> operation.
		/// </summary>
		ExecuteReaderAsync,

		/// <summary>
		/// Synchronous <see cref="DbCommand.ExecuteScalar"/> operation.
		/// </summary>
		ExecuteScalar,

		/// <summary>
		/// Asynchronous <see cref="DbCommand.ExecuteScalarAsync(System.Threading.CancellationToken)"/> operation.
		/// </summary>
		ExecuteScalarAsync,

		/// <summary>
		/// Synchronous <see cref="DataConnectionExtensions.BulkCopy{T}(ITable{T}, System.Collections.Generic.IEnumerable{T})"/> operation.
		/// </summary>
		BulkCopy,

		/// <summary>
		/// Asynchronous <see cref="DataConnectionExtensions.BulkCopyAsync{T}(DataConnection, int, System.Collections.Generic.IEnumerable{T}, System.Threading.CancellationToken)"/> operation.
		/// </summary>
		BulkCopyAsync,

		/// <summary>
		/// Synchronous <see cref="DbConnection.Open"/> operation.
		/// </summary>
		Open,

		/// <summary>
		/// Asynchronous <see cref="DbConnection.OpenAsync(System.Threading.CancellationToken)"/> operation.
		/// </summary>
		OpenAsync,
	}
}
