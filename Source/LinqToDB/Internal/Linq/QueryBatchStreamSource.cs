using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;

#pragma warning disable MA0048 // the stream's marker, sources and cursor live together

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Which stream of an explicit query batch an eager load reads: its index among the batch's streams and whether a
	/// callback (read inside <c>Execute</c>) or a handle (read after it) consumes it.
	/// </summary>
	sealed record QueryBatchStreamInfo(int Index, bool IsCallback);

	/// <summary>
	/// The call a stream part of an explicit query batch is wrapped in. The builder turns it into a streamed eager load; its
	/// body runs only when the part is evaluated on the client, which a stream part must never be.
	/// </summary>
	static class QueryBatchStreamMarker
	{
		internal static readonly MethodInfo PartMethodInfo = typeof(QueryBatchStreamMarker).GetMethod(nameof(Part), BindingFlags.Static | BindingFlags.NonPublic)!;

		internal static QueryBatchStreamSource<T> Part<T>(IQueryable<T> source, int index, bool isCallback)
		{
			throw new LinqToDBException(ErrorHelper.Error_QueryBatch_StreamClientPart);
		}

		internal static bool IsMarker(MethodCallExpression call)
			=> call.Method.IsGenericMethod && call.Method.GetGenericMethodDefinition() == PartMethodInfo;

		internal static Expression MakePart(Type elementType, Expression source, int index, bool isCallback)
		{
			return Expression.Call(
				PartMethodInfo.MakeGenericMethod(elementType),
				source,
				Expression.Constant(index),
				Expression.Constant(isCallback));
		}
	}

	/// <summary>
	/// The rows of one stream part, handed by the engine to the batch through the stream's result slot.
	/// </summary>
	abstract class QueryBatchStreamSource<T>
	{
		/// <summary>
		/// Whether the rows come from the batch's own reader, held past <c>Execute</c>; otherwise the part runs as a query of
		/// its own.
		/// </summary>
		public abstract bool IsCombined { get; }

		public abstract IEnumerable<T>      Read();
		public abstract IAsyncEnumerable<T> ReadAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Releases what the source holds: the batch's reader for a combined source.
		/// </summary>
		public abstract void      Close();
		public abstract ValueTask CloseAsync();
	}

	// A part read from the batch's combined reader, after the main row.
	sealed class CombinedQueryBatchStreamSource<T>(
		QueryBatchStreamCursor cursor,
		int                    stepIndex,
		Query<T>               query,
		IDataContext           dataContext,
		IQueryExpressions      expressions,
		SqlCommandExecutionContext context)
		: QueryBatchStreamSource<T>
	{
		public override bool IsCombined => true;

		public override IEnumerable<T> Read()
		{
			var reader = cursor.MoveTo(stepIndex);

			foreach (var item in query.GetResultFromReader!(dataContext, expressions, context, reader))
			{
				yield return item;
			}

			cursor.Finished(stepIndex);
		}

		public override async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var reader = await cursor.MoveToAsync(stepIndex, cancellationToken).ConfigureAwait(false);

			await foreach (var item in query.GetResultFromReader!(dataContext, expressions, context, reader).WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return item;
			}

			await cursor.FinishedAsync(stepIndex).ConfigureAwait(false);
		}

		public override void      Close()      => cursor.Release();
		public override ValueTask CloseAsync() => cursor.ReleaseAsync();
	}

	// A part that runs as a query of its own: where the batch is not combined, or on a remote context.
	sealed class SequentialQueryBatchStreamSource<T>(
		Query<T>                    query,
		IDataContext                dataContext,
		IQueryExpressions           expressions,
		SqlCommandExecutionContext? context)
		: QueryBatchStreamSource<T>
	{
		public override bool IsCombined => false;

		public override IEnumerable<T> Read() => query.GetResultEnumerable(dataContext, expressions, context);

		public override async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			await foreach (var item in query.GetResultEnumerable(dataContext, expressions, context).WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return item;
			}
		}

		public override void      Close()      { }
		public override ValueTask CloseAsync() => default;
	}

	/// <summary>
	/// Owns the batch's combined reader once the main row is read: the reader, the connection lock and the deferred
	/// <c>CloseAfterUse</c> restore. Moves forward through the stream result sets, running any later command the statement
	/// limit split off, and releases everything after the last stream or on <see cref="Release"/>.
	/// </summary>
	sealed class QueryBatchStreamCursor
	{
		// The stream steps in command order.
		readonly int[] _order;

		DataConnection?                   _dataConnection;
		DataContext?                      _dataContext;
		object?                           _lock;
		bool                              _closeAfterUse;
		DataReaderWrapper?                _reader;
		IReadOnlyList<SqlCommandGroup>?   _groups;
		Func<SqlCommandGroup, int, CombinedCommand>? _getCommand;

		int  _groupIndex;
		int  _positionInGroup;
		int  _current = -1;      // the index in _order of the stream the reader is at; -1 while it is at the main row
		bool _released;

		public QueryBatchStreamCursor(int[] order)
		{
			_order = order;
		}

		// Takes over the reader positioned at the main row's result set, in the group that carries it.
		public void Attach(
			DataConnection                             dataConnection,
			IDataContext                               dataContext,
			bool                                       closeAfterUse,
			DataReaderWrapper                          reader,
			IReadOnlyList<SqlCommandGroup>             groups,
			Func<SqlCommandGroup, int, CombinedCommand> getCommand,
			int                                        groupIndex,
			int                                        mainStepIndex)
		{
			// The cursor's own lock is taken before the enumerator releases its one, so the count never reaches zero between.
			if (dataContext is DataContext context)
			{
				_dataContext = context;
				_lock        = context.AcquireLock();
			}

			_dataConnection  = dataConnection;
			_closeAfterUse   = closeAfterUse;
			_reader          = reader;
			_groups          = groups;
			_getCommand      = getCommand;
			_groupIndex      = groupIndex;
			_positionInGroup = IndexOf(groups[groupIndex].StepIndexes, mainStepIndex);
		}

		static int IndexOf(IReadOnlyList<int> list, int value)
		{
			for (var i = 0; i < list.Count; i++)
			{
				if (list[i] == value)
					return i;
			}

			return -1;
		}

		int OrderOf(int stepIndex)
		{
			for (var i = 0; i < _order.Length; i++)
			{
				if (_order[i] == stepIndex)
					return i;
			}

			throw new InvalidOperationException("The step is not a stream of this batch.");
		}

		public DbDataReader MoveTo(int stepIndex)
		{
			var target = PrepareMove(stepIndex);

			while (_current < target)
			{
				Advance();
			}

			return _reader!.DataReader!;
		}

		public async ValueTask<DbDataReader> MoveToAsync(int stepIndex, CancellationToken cancellationToken)
		{
			var target = PrepareMove(stepIndex);

			while (_current < target)
			{
				await AdvanceAsync(cancellationToken).ConfigureAwait(false);
			}

			return _reader!.DataReader!;
		}

		int PrepareMove(int stepIndex)
		{
			if (_released || _reader == null)
				throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamClosed);

			var target = OrderOf(stepIndex);

			if (target <= _current)
				throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamAlreadyRead);

			return target;
		}

		// To the next stream's result set: the next one of this command, or the first of the next command.
		void Advance()
		{
			var group = _groups![_groupIndex];

			if (_positionInGroup + 1 < group.StepIndexes.Count)
			{
				if (!_reader!.DataReader!.NextResult())
					throw new InvalidOperationException(ErrorHelper.Error_CombinedCommand_FewerResultSets);

				_positionInGroup++;
			}
			else
			{
				_reader!.Dispose();
				_reader = null;

				_groupIndex++;
				_positionInGroup = 0;
				_reader          = DataConnection.QueryRunner.ExecuteCombined(_dataConnection!, _getCommand!(_groups[_groupIndex], _groupIndex), CommandBehavior.Default);
			}

			_current++;
		}

		async ValueTask AdvanceAsync(CancellationToken cancellationToken)
		{
			var group = _groups![_groupIndex];

			if (_positionInGroup + 1 < group.StepIndexes.Count)
			{
				if (!await _reader!.DataReader!.NextResultAsync(cancellationToken).ConfigureAwait(false))
					throw new InvalidOperationException(ErrorHelper.Error_CombinedCommand_FewerResultSets);

				_positionInGroup++;
			}
			else
			{
				await _reader!.DisposeAsync().ConfigureAwait(false);
				_reader = null;

				_groupIndex++;
				_positionInGroup = 0;
				_reader          = await DataConnection.QueryRunner.ExecuteCombinedAsync(_dataConnection!, _getCommand!(_groups[_groupIndex], _groupIndex), CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
			}

			_current++;
		}

		// The last stream read to its end releases the reader.
		public void Finished(int stepIndex)
		{
			if (OrderOf(stepIndex) == _order.Length - 1)
				Release();
		}

		public ValueTask FinishedAsync(int stepIndex)
		{
			return OrderOf(stepIndex) == _order.Length - 1 ? ReleaseAsync() : default;
		}

		// Restore CloseAfterUse first, so disposing the reader closes the connection as asked; the lock last. A disposed
		// DataContext has closed its connection already, and releasing its lock would throw.
		public void Release()
		{
			if (_released)
				return;

			_released = true;

			// The lock is released even when disposing the reader throws, as the enumerator's own using does.
			try
			{
				if (_dataConnection != null)
					((IDataContext)_dataConnection).CloseAfterUse = _closeAfterUse;

				var reader = _reader;

				_reader = null;
				reader?.Dispose();
			}
			finally
			{
				if (_lock is IDisposable disposable && _dataContext is { IsDisposed: false })
					disposable.Dispose();
			}
		}

		public async ValueTask ReleaseAsync()
		{
			if (_released)
				return;

			_released = true;

			try
			{
				if (_dataConnection != null)
					((IDataContext)_dataConnection).CloseAfterUse = _closeAfterUse;

				var reader = _reader;

				_reader = null;

				if (reader != null)
					await reader.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if (_lock is IAsyncDisposable disposable && _dataContext is { IsDisposed: false })
					await disposable.DisposeAsync().ConfigureAwait(false);
			}
		}

		// A run that failed before the hand-off: nothing was taken over, so the sources only report the stream closed.
		public void Fail()
		{
			_released = true;
		}
	}
}
