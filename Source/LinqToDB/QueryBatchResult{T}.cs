using System;

using LinqToDB.Internal.Common;

namespace LinqToDB
{
	/// <summary>
	/// The result of one read added to a <see cref="QueryBatchBuilder"/> by
	/// <see cref="QueryBatchBuilder.AddRequest{T}(System.Linq.Expressions.Expression{Func{T}})"/>.
	/// </summary>
	/// <typeparam name="T">Type of the result.</typeparam>
	public sealed class QueryBatchResult<T>
	{
		T    _value = default!;
		bool _hasValue;

		internal QueryBatchResult()
		{
		}

		/// <summary>
		/// The value the read returned.
		/// </summary>
		/// <remarks>
		/// Read it after the batch is executed, never inside a part of the same batch: the batch is not executed yet while
		/// its parts are read, so such a part fails the batch. A setter of the same batch can read it: every result is set
		/// before the first setter runs.
		/// </remarks>
		/// <exception cref="InvalidOperationException">The batch has not been executed.</exception>
		public T Value => _hasValue ? _value : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_NotExecuted);

		internal void SetValue(object? value)
		{
			_value    = (T)value!;
			_hasValue = true;
		}
	}
}
