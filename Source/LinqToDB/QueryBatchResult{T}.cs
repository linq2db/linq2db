using System;

using LinqToDB.Internal.Common;

namespace LinqToDB
{
	/// <summary>
	/// The result of one read added to a <see cref="QueryBatchBuilder"/>.
	/// </summary>
	/// <typeparam name="T">Type of the result.</typeparam>
	public sealed class QueryBatchResult<T>
	{
		readonly QueryBatchBuilder _batch;

		T _value = default!;

		internal QueryBatchResult(QueryBatchBuilder batch)
		{
			_batch = batch;
		}

		/// <summary>
		/// The value the read returned.
		/// </summary>
		/// <remarks>
		/// Read it after the batch is executed, never inside a part of the same batch: the batch is not executed yet while
		/// its parts are read, so such a part fails the batch.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The batch has not been executed.</exception>
		public T Value => _batch.IsExecuted ? _value : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_NotExecuted);

		internal void SetValue(object? value)
		{
			_value = (T)value!;
		}
	}
}
