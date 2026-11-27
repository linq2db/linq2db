using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Linq.Builder;

#pragma warning disable MA0048 // the batch's stream contract lives with the batch

namespace LinqToDB
{
	/// <summary>
	/// Collects reads from any number of call sites, runs them as one command, as one
	/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/> call, and hands each value to its result or
	/// its setter. Created by <see cref="LinqExtensions.CreateQueryBatch(IDataContext)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every part is read as a part of that call, so its rules apply: where the context and its provider can combine
	/// statements the batch is sent as one command, otherwise its statements run one after another;
	/// <c>First()</c> and <c>Single()</c> inside a part throw when they find no row; see
	/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.
	/// </para>
	/// <para>
	/// Variables a part captures are read when the batch is executed, not when the part is added. A part of type
	/// <see cref="IQueryable{T}"/> is read inside the batch and returned as rows in memory.
	/// </para>
	/// <para>
	/// The parts — their number, order and types — are part of the query the batch builds and caches: a batch whose parts follow
	/// the data builds a query for each distinct set of parts.
	/// </para>
	/// <para>
	/// Every part is read before any setter runs, so a part that reads what a setter assigns sees the value it had before the
	/// batch was executed, and a part that reads a <see cref="QueryBatchResult{T}.Value"/> of the same batch fails the batch.
	/// Then every result is set, the stream callbacks run, and the setters run in the order their parts were added,
	/// synchronously, where the execution completes; an asynchronous lambda passed as a setter or a callback is not awaited.
	/// </para>
	/// <para>
	/// A stream part (<see cref="AddStreamRequest{T}(Expression{Func{IQueryable{T}}})"/>) hands its rows over one at a time
	/// instead of reading them into a list. Where the batch is sent as one command, a stream follows the batch row in it: its
	/// callback receives the rows during the execution, and its <see cref="QueryBatchStream{T}"/> reads them after it, from
	/// the batch's open reader. Otherwise each stream runs as a query of its own during the execution, and a
	/// <see cref="QueryBatchStream{T}"/> is read from memory. Either way a <see cref="QueryBatchStream{T}"/> is read once the
	/// execution has returned; a stream callback or a setter of the same batch cannot read it. A batch whose streams are not
	/// read yet holds its connection until the enumeration of the last one ends, read to its end or not, or until the batch is
	/// disposed. A batch with a stream part opens no transaction of its own.
	/// </para>
	/// <para>
	/// A batch is executed once. The parts are read as one result, so a part that fails fails the whole batch; a batch that
	/// failed before its results were set has set no result and assigned nothing, and can be executed again, in the
	/// transaction of its context as that transaction stands. Once the results are set the batch is executed: a stream
	/// callback, a stream's query or a setter that throws ends the run, the setters not yet run do not run, and the streams not
	/// read yet are closed. A read of a <see cref="QueryBatchStream{T}"/> that fails, or whose token is cancelled, closes the
	/// streams not read yet too. Executing an empty batch sends nothing.
	/// </para>
	/// <para>
	/// A batch is not thread-safe.
	/// </para>
	/// </remarks>
	public sealed class QueryBatchBuilder : IDisposable, IAsyncDisposable
	{
		enum State
		{
			Pending,
			Running,
			Streaming,
			Executed,
		}

		enum SinkKind
		{
			Result,
			Setter,
			Stream,
		}

		readonly IDataContext                                                             _dataContext;
		readonly List<LambdaExpression>                                                   _parts   = new();
		readonly List<(Action<object?>? Deliver, SinkKind Kind, IQueryBatchStream? Stream)> _sinks   = new();
		readonly List<IQueryBatchStream>                                                  _streams = new();

		State                _state;
		IQueryBatchStream[]? _handles;

		internal QueryBatchBuilder(IDataContext dataContext)
		{
			_dataContext = dataContext;
		}

		/// <summary>
		/// Adds a read to the batch and returns the result that receives its value.
		/// </summary>
		/// <typeparam name="T">Type of the value.</typeparam>
		/// <param name="query">The read, written as a selector of
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</param>
		/// <returns>The result, readable after the batch is executed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">An expression in <paramref name="query"/> may create a task, which could run a query
		/// outside the batch. Reading a task held by a captured variable is allowed.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open.</exception>
		public QueryBatchResult<T> AddRequest<T>(Expression<Func<T>> query)
		{
			ArgumentNullException.ThrowIfNull(query);

			var result = new QueryBatchResult<T>();

			AddPart(query, result.SetValue, SinkKind.Result);

			return result;
		}

		/// <summary>
		/// Adds a read to the batch and the setter that receives its value.
		/// </summary>
		/// <typeparam name="T">Type of the value.</typeparam>
		/// <param name="query">The read, written as a selector of
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</param>
		/// <param name="setter">Receives the value once the batch has read every part.</param>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> or <paramref name="setter"/> is
		/// <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">An expression in <paramref name="query"/> may create a task, which could run a query
		/// outside the batch. Reading a task held by a captured variable is allowed.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open.</exception>
		public void AddRequest<T>(Expression<Func<T>> query, Action<T> setter)
		{
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(setter);

			AddPart(query, value => setter((T)value!), SinkKind.Setter);
		}

		/// <summary>
		/// Adds a stream part to the batch and returns the stream its rows are read from after the batch is executed.
		/// </summary>
		/// <typeparam name="T">Type of a row.</typeparam>
		/// <param name="query">The rows, written as a query of the database.</param>
		/// <returns>The stream, read once after the batch is executed, in the order the streams were added.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">An expression in <paramref name="query"/> may create a task, which could run a query
		/// outside the batch.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open.</exception>
		public QueryBatchStream<T> AddStreamRequest<T>(Expression<Func<IQueryable<T>>> query)
		{
			ArgumentNullException.ThrowIfNull(query);

			var stream = new QueryBatchStream<T>(this);

			AddStream(query, stream, typeof(T), isCallback: false);

			return stream;
		}

		/// <summary>
		/// Adds a stream part to the batch and the callback that receives its rows, one at a time, while the batch is executed.
		/// </summary>
		/// <typeparam name="T">Type of a row.</typeparam>
		/// <param name="query">The rows, written as a query of the database.</param>
		/// <param name="onRow">Receives each row, after every result of the batch is set and before the setters run.</param>
		/// <exception cref="ArgumentNullException"><paramref name="query"/> or <paramref name="onRow"/> is
		/// <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">An expression in <paramref name="query"/> may create a task, which could run a query
		/// outside the batch.</exception>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open.</exception>
		public void AddStreamRequest<T>(Expression<Func<IQueryable<T>>> query, Action<T> onRow)
		{
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(onRow);

			AddStream(query, new CallbackStream<T>(onRow), typeof(T), isCallback: true);
		}

		void AddPart(LambdaExpression query, Action<object?> deliver, SinkKind kind)
		{
			ThrowIfNotPending();

			// The rule the QueryBatch node enforces when the batch is built; checked here so the part is never kept.
			if (ScalarSelectBuilder.CreatesTask(query.Body))
				throw new ArgumentException(ErrorHelper.Error_QueryBatch_TaskPart, nameof(query));

			_parts.Add(query);
			_sinks.Add((deliver, kind, null));
		}

		// A stream part enters the batch wrapped in the marker the builder turns into a streamed load; its index and kind are
		// constants of the tree, so part of the cached query.
		void AddStream(LambdaExpression query, IQueryBatchStream stream, Type elementType, bool isCallback)
		{
			ThrowIfNotPending();

			if (ScalarSelectBuilder.CreatesTask(query.Body))
				throw new ArgumentException(ErrorHelper.Error_QueryBatch_TaskPart, nameof(query));

			_parts.Add(Expression.Lambda(QueryBatchStreamMarker.MakePart(elementType, query.Body, _streams.Count, isCallback)));
			_sinks.Add((null, SinkKind.Stream, stream));
			_streams.Add(stream);
		}

		/// <summary>
		/// Executes the batch, sets every result, hands the rows of every stream part with a callback to it and every other
		/// value to its setter.
		/// </summary>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, the provider cannot translate the parts as one statement, an expression
		/// in a part may create a task, which could run a query outside the batch, or a stream part loads a collection or is not
		/// a query of the database.</exception>
		public void Execute()
		{
			ThrowIfNotPending();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			object?[] values;

			try
			{
				var (expression, query) = BuildQuery();

				values = ReadValues(((IQueryProvider)query).Execute<object>(expression));
			}
			catch
			{
				_state = State.Pending;
				throw;
			}

			var (sinks, streams) = BeginDelivery(values, readAsync: false);

			try
			{
				DeliverValues(sinks, values, SinkKind.Result);

				foreach (var stream in streams)
				{
					if (stream.IsCallback || !stream.IsCombined)
						stream.Run();
				}

				DeliverValues(sinks, values, SinkKind.Setter);
			}
			catch
			{
				CloseStreams(streams);
				throw;
			}

			EndDelivery(streams);
		}

		/// <summary>
		/// Executes the batch asynchronously, sets every result, hands the rows of every stream part with a callback to it and
		/// every other value to its setter.
		/// </summary>
		/// <param name="token">Optional asynchronous operation cancellation token. A token cancelled before the call sends
		/// nothing.</param>
		/// <returns>A task that completes when every result is set, every stream callback has received its rows and every
		/// setter has run.</returns>
		/// <exception cref="InvalidOperationException">The batch has been executed, is being executed or has streams open, or a part's
		/// <c>First()</c> or <c>Single()</c> failed as described for
		/// <see cref="LinqExtensions.QueryBatch{T}(IDataContext, Expression{Func{T}})"/>.</exception>
		/// <exception cref="LinqToDBException">The context has pending <see cref="IDataContext.QueryHints"/> or
		/// <see cref="IDataContext.NextQueryHints"/>, the provider cannot translate the parts as one statement, an expression
		/// in a part may create a task, which could run a query outside the batch, or a stream part loads a collection or is not
		/// a query of the database.</exception>
		/// <exception cref="OperationCanceledException"><paramref name="token"/> was cancelled and the batch has parts.</exception>
		public async Task ExecuteAsync(CancellationToken token = default)
		{
			ThrowIfNotPending();

			if (_parts.Count == 0)
			{
				_state = State.Executed;
				return;
			}

			_state = State.Running;

			object?[] values;

			try
			{
				var (expression, query) = BuildQuery();

				values = ReadValues(await ((IQueryProviderAsync)query).ExecuteAsync<object>(expression, token).ConfigureAwait(false));
			}
			catch
			{
				_state = State.Pending;
				throw;
			}

			var (sinks, streams) = BeginDelivery(values, readAsync: true);

			try
			{
				DeliverValues(sinks, values, SinkKind.Result);

				foreach (var stream in streams)
				{
					if (stream.IsCallback || !stream.IsCombined)
						await stream.RunAsync(token).ConfigureAwait(false);
				}

				DeliverValues(sinks, values, SinkKind.Setter);
			}
			catch
			{
				await CloseStreamsAsync(streams).ConfigureAwait(false);
				throw;
			}

			EndDelivery(streams);
		}

		// The batch is executed before the first value is delivered, so a callback or a setter that throws ends a run that is
		// not repeated. The streams take their sources first: a stream is bound, and so closed by a later failure, from here;
		// a handle becomes readable only once the delivery has ended (EndDelivery).
		//
		// The order after it: every result, then the stream callbacks and, where a stream runs as a query of its own, the
		// handles' reads, then the setters — so every stream reads the values of the parts from before any setter, as a combined
		// batch, which binds every statement before the batch row, does.
		((Action<object?>? Deliver, SinkKind Kind, IQueryBatchStream? Stream)[] Sinks, IQueryBatchStream[] Streams) BeginDelivery(object?[] values, bool readAsync)
		{
			var sinks   = _sinks.ToArray();
			var streams = _streams.ToArray();

			Complete();

			for (var i = 0; i < sinks.Length; i++)
			{
				if (sinks[i].Kind == SinkKind.Stream)
					sinks[i].Stream!.Bind(values[i], readAsync);
			}

			return (sinks, streams);
		}

		static void DeliverValues((Action<object?>? Deliver, SinkKind Kind, IQueryBatchStream? Stream)[] sinks, object?[] values, SinkKind kind)
		{
			for (var i = 0; i < sinks.Length; i++)
			{
				if (sinks[i].Kind == kind)
					sinks[i].Deliver!(values[i]);
			}
		}

		// The handles become readable; those still to read from the batch's reader keep the batch Streaming until they are
		// read or it is disposed.
		void EndDelivery(IQueryBatchStream[] streams)
		{
			_handles = streams.Where(static s => !s.IsCallback).ToArray();

			foreach (var handle in _handles)
			{
				handle.Ready();
			}

			foreach (var handle in _handles)
			{
				if (handle.IsCombined && handle.IsOpen)
				{
					_state = State.Streaming;
					return;
				}
			}
		}

		// Single use: once executed, the parts and the sinks are never read again, so a batch kept after the run pins neither the
		// parts' captured closures nor the results and the setters' targets.
		void Complete()
		{
			_state = State.Executed;

			_parts.Clear();
			_sinks.Clear();
			_streams.Clear();
		}

		void ThrowIfNotPending()
		{
			switch (_state)
			{
				case State.Running   : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_Running);
				case State.Streaming : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamsOpen);
				case State.Executed  : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_AlreadyExecuted);
			}
		}

		// Reading a handle skips what is left of the handles added before it.
		internal void StartStream(IQueryBatchStream stream)
		{
			if (_handles == null)
				return;

			foreach (var handle in _handles)
			{
				if (ReferenceEquals(handle, stream))
					break;

				handle.Skip();
			}
		}

		// A handle read to its end or left before it. Once the last one is done no handle reads the reader any more, so it is
		// released: reading the last handle skipped every one before it.
		internal void StreamFinished()
		{
			if (_state == State.Streaming && !_handles![_handles.Length - 1].IsOpen)
				FailStreams();
		}

		internal ValueTask StreamFinishedAsync()
		{
			return _state == State.Streaming && !_handles![_handles.Length - 1].IsOpen ? FailStreamsAsync() : default;
		}

		// Executed first, so a release that throws still leaves the batch ended.
		internal void FailStreams()
		{
			_state = State.Executed;

			if (_handles != null)
				CloseStreams(_handles);
		}

		internal async ValueTask FailStreamsAsync()
		{
			_state = State.Executed;

			if (_handles != null)
				await CloseStreamsAsync(_handles).ConfigureAwait(false);
		}

		static void CloseStreams(IQueryBatchStream[] streams)
		{
			foreach (var stream in streams)
			{
				stream.Close();
			}

			foreach (var stream in streams)
			{
				stream.Release();
			}
		}

		static async ValueTask CloseStreamsAsync(IQueryBatchStream[] streams)
		{
			foreach (var stream in streams)
			{
				stream.Close();
			}

			foreach (var stream in streams)
			{
				await stream.ReleaseAsync().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Closes the streams that are not read yet from the batch's reader and releases the connection they hold. A batch
		/// without open streams has nothing to release; streams read into memory during the execution stay readable.
		/// </summary>
		public void Dispose()
		{
			if (_state == State.Streaming)
				FailStreams();
		}

		/// <summary>
		/// Closes the streams that are not read yet from the batch's reader and releases the connection they hold,
		/// asynchronously. A batch without open streams has nothing to release; streams read into memory during the execution
		/// stay readable.
		/// </summary>
		/// <returns>A task that completes when the connection is released.</returns>
		public ValueTask DisposeAsync()
		{
			return _state == State.Streaming ? FailStreamsAsync() : default;
		}

		// The parts become one QueryBatch call whose selector builds a tuple of them, nested through Rest beyond seven; the
		// tuple type, which keys the query cache, is known only now.
		(Expression expression, IQueryable query) BuildQuery()
		{
			var bodies     = _parts.Select(static p => p.Body).ToArray();
			var tupleType  = ExpressionBuilder.BuildValueTupleType(bodies.Select(static b => b.Type).ToArray());
			var body       = ExpressionBuilder.BuildValueTupleNew(tupleType, bodies);
			var expression = LinqExtensions.CreateQueryBatchExpression(_dataContext, Expression.Lambda(body));

			return (expression, ExpressionQueryImpl.CreateQuery(tupleType, _dataContext, expression));
		}

		// Seven values per tuple, the rest in the nested tuple of its Rest field.
		object?[] ReadValues(object? tuple)
		{
			var values = new object?[_parts.Count];
			var holder = tuple!;

			for (var i = 0; i < values.Length; i++)
			{
				var index = i % 7;

				if (i > 0 && index == 0)
					holder = holder.GetType().GetField("Rest")!.GetValue(holder)!;

				values[i] = holder.GetType().GetField("Item" + (index + 1).ToString(CultureInfo.InvariantCulture))!.GetValue(holder);
			}

			return values;
		}
	}

	/// <summary>
	/// A stream part of a <see cref="QueryBatchBuilder"/>, as the batch drives it: a handle read after the run, or a callback
	/// run during it.
	/// </summary>
	interface IQueryBatchStream
	{
		bool IsCallback { get; }

		// Reads from the batch's reader; otherwise the stream runs as a query of its own.
		bool IsCombined { get; }

		// Not yet read or skipped, and not closed.
		bool IsOpen { get; }

		void Bind(object? source, bool readAsync);
		void Ready();

		// A callback's rows to the callback; a handle that runs as its own query, its rows into memory.
		void Run();
		Task RunAsync(CancellationToken cancellationToken);

		void Skip();
		void Close();

		void      Release();
		ValueTask ReleaseAsync();
	}

	sealed class CallbackStream<T>(Action<T> onRow) : IQueryBatchStream
	{
		QueryBatchStreamSource<T>? _source;

		public bool IsCallback => true;
		public bool IsCombined => _source?.IsCombined == true;
		public bool IsOpen     => false;

		public void Bind(object? source, bool readAsync)
		{
			_source = (QueryBatchStreamSource<T>)source!;
		}

		public void Ready()
		{
		}

		public void Run()
		{
			foreach (var row in _source!.Read())
			{
				onRow(row);
			}
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			await foreach (var row in _source!.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				onRow(row);
			}
		}

		public void Skip()
		{
		}

		public void Close()
		{
		}

		public void      Release()      => _source?.Close();
		public ValueTask ReleaseAsync() => _source?.CloseAsync() ?? default;
	}
}
