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
		/// <see cref="DataConnectionExtensions.BulkCopy{T}(ITable{T}, System.Collections.Generic.IEnumerable{T})"/> or <see cref="DataConnectionExtensions.BulkCopyAsync{T}(DataConnection, int, System.Collections.Generic.IEnumerable{T}, System.Threading.CancellationToken)"/> operation.
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
	}
}
