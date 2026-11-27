using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	/// <summary>
	/// The rows of a stream part added to a <see cref="QueryBatchBuilder"/> by
	/// <see cref="QueryBatchBuilder.AddStreamRequest{T}(System.Linq.Expressions.Expression{Func{System.Linq.IQueryable{T}}})"/>,
	/// read once after the batch is executed.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Read it the way the batch ran: with <see langword="foreach"/> after <see cref="QueryBatchBuilder.Execute"/>, with
	/// <c>await foreach</c> after <see cref="QueryBatchBuilder.ExecuteAsync"/>. <see cref="AsEnumerable"/> and
	/// <see cref="AsAsyncEnumerable"/> give the same rows to LINQ, with the same rule, checked when they are enumerated.
	/// </para>
	/// <para>
	/// The streams of a batch are read forward, in the order they were added: reading a stream skips what is left of the
	/// streams added before it, which can then no longer be read, and an enumeration of one of them still open fails at its
	/// next row. Each stream is read once, once the execution has returned: a stream callback or a setter of the same batch
	/// cannot read it.
	/// </para>
	/// <para>
	/// Cancelling the token of an asynchronous read ends the batch as a failed read does: the read throws, and the streams
	/// not read yet are closed.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">Type of a row.</typeparam>
	public sealed class QueryBatchStream<T> : IQueryBatchStream
	{
		enum State
		{
			NotReady,
			Delivering,
			Ready,
			Reading,
			Finished,
			Closed,
		}

		readonly QueryBatchBuilder _batch;

		State                      _state;
		bool                       _readAsync;
		QueryBatchStreamSource<T>? _source;
		List<T>?                   _buffer;

		internal QueryBatchStream(QueryBatchBuilder batch)
		{
			_batch = batch;
		}

		/// <summary>
		/// Returns an enumerator over the rows, after <see cref="QueryBatchBuilder.Execute"/>.
		/// </summary>
		/// <returns>An enumerator over the rows of the stream.</returns>
		/// <exception cref="InvalidOperationException">The batch has not been executed or is being executed; failed after its
		/// results were set, including a read of another of its streams that failed or was cancelled; was disposed while the
		/// stream read from its reader; ran with <see cref="QueryBatchBuilder.ExecuteAsync"/>; or the stream was already read or
		/// skipped.</exception>
		public IEnumerator<T> GetEnumerator()
		{
			CheckStart(isAsync: false);

			return Enumerate();
		}

		/// <summary>
		/// Returns an asynchronous enumerator over the rows, after <see cref="QueryBatchBuilder.ExecuteAsync"/>.
		/// </summary>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token. Cancelling it ends the read and
		/// closes the streams of the batch not read yet.</param>
		/// <returns>An asynchronous enumerator over the rows of the stream.</returns>
		/// <exception cref="InvalidOperationException">The batch has not been executed or is being executed; failed after its
		/// results were set, including a read of another of its streams that failed or was cancelled; was disposed while the
		/// stream read from its reader; ran with <see cref="QueryBatchBuilder.Execute"/>; or the stream was already read or
		/// skipped.</exception>
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			CheckStart(isAsync: true);

			return EnumerateAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
		}

		/// <summary>
		/// The rows as an <see cref="IEnumerable{T}"/>, read after <see cref="QueryBatchBuilder.Execute"/>.
		/// </summary>
		/// <returns>The rows of the stream; the stream's rules are checked when they are enumerated.</returns>
		public IEnumerable<T> AsEnumerable()
		{
			using var enumerator = GetEnumerator();

			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
		}

		/// <summary>
		/// The rows as an <see cref="IAsyncEnumerable{T}"/>, read after <see cref="QueryBatchBuilder.ExecuteAsync"/>.
		/// </summary>
		/// <param name="cancellationToken">Optional asynchronous operation cancellation token. Cancelling it ends the read and
		/// closes the streams of the batch not read yet.</param>
		/// <returns>The rows of the stream; the stream's rules are checked when they are enumerated.</returns>
		public async IAsyncEnumerable<T> AsAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var enumerator = GetAsyncEnumerator(cancellationToken);

			await using (enumerator.ConfigureAwait(false))
			{
				while (await enumerator.MoveNextAsync().ConfigureAwait(false))
				{
					yield return enumerator.Current;
				}
			}
		}

		// The refusals in precedence order: not ready, being executed, closed, the other mode, already read. Checked when the
		// enumerator is taken, and again by Start at its first move.
		void CheckStart(bool isAsync)
		{
			switch (_state)
			{
				case State.NotReady   : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamNotReady);
				case State.Delivering : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_Running);
				case State.Closed     : throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamClosed);
			}

			if (isAsync != _readAsync)
				throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamWrongMode);

			if (_state != State.Ready)
				throw new InvalidOperationException(ErrorHelper.Error_QueryBatch_StreamAlreadyRead);
		}

		// The stream starts at the enumerator's first move, inside the iterator: one disposed before it moves has taken
		// nothing and skipped nothing, and the handle stays readable.
		void Start(bool isAsync)
		{
			CheckStart(isAsync);

			_batch.StartStream(this);

			_state = State.Reading;
		}

		// Read to its end or left before it, the handle is done; the batch releases the reader after the last one. The state
		// is checked before every move, the last one too, so a handle skipped or closed meanwhile stops on either route.
		IEnumerator<T> Enumerate()
		{
			var started = false;

			try
			{
				Start(isAsync: false);
				started = true;

				if (_buffer != null)
				{
					var buffer = _buffer;
					var index  = 0;

					while (true)
					{
						ThrowIfLeft();

						if (index == buffer.Count)
							break;

						yield return buffer[index++];
					}
				}
				else
				{
					using var enumerator = _source!.Read().GetEnumerator();

					while (true)
					{
						ThrowIfLeft();

						T item;

						try
						{
							if (!enumerator.MoveNext())
								break;

							item = enumerator.Current;
						}
						catch
						{
							_batch.FailStreams();
							throw;
						}

						yield return item;
					}
				}
			}
			finally
			{
				if (started && _state == State.Reading)
				{
					_state = State.Finished;
					_batch.StreamFinished();
				}
			}
		}

		async IAsyncEnumerable<T> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var started = false;

			try
			{
				Start(isAsync: true);
				started = true;

				if (_buffer != null)
				{
					var buffer = _buffer;
					var index  = 0;

					while (true)
					{
						ThrowIfLeft();

						// The rows are in memory, yet a cancelled read ends the batch as one over its reader does.
						try
						{
							cancellationToken.ThrowIfCancellationRequested();
						}
						catch
						{
							await _batch.FailStreamsAsync().ConfigureAwait(false);
							throw;
						}

						if (index == buffer.Count)
							break;

						yield return buffer[index++];
					}
				}
				else
				{
					var enumerator = _source!.ReadAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);

					await using (enumerator.ConfigureAwait(false))
					{
						while (true)
						{
							ThrowIfLeft();

							T item;

							try
							{
								if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
									break;

								item = enumerator.Current;
							}
							catch
							{
								await _batch.FailStreamsAsync().ConfigureAwait(false);
								throw;
							}

							yield return item;
						}
					}
				}
			}
			finally
			{
				if (started && _state == State.Reading)
				{
					_state = State.Finished;
					await _batch.StreamFinishedAsync().ConfigureAwait(false);
				}
			}
		}

		// Between two rows a later handle may have been read, which moved the reader past this one, or the batch may have
		// been disposed.
		void ThrowIfLeft()
		{
			if (_state != State.Reading)
				throw new InvalidOperationException(_state == State.Closed ? ErrorHelper.Error_QueryBatch_StreamClosed : ErrorHelper.Error_QueryBatch_StreamAlreadyRead);
		}

		bool IQueryBatchStream.IsCallback => false;

		bool IQueryBatchStream.IsCombined => _source?.IsCombined == true;

		// Bound when the execution starts delivering, readable only once it has returned: a callback or a setter that reads
		// it would move the batch's reader under a stream still being read.
		void IQueryBatchStream.Bind(object? source, bool readAsync)
		{
			_source    = (QueryBatchStreamSource<T>)source!;
			_readAsync = readAsync;
			_state     = State.Delivering;
		}

		void IQueryBatchStream.Ready()
		{
			if (_state == State.Delivering)
				_state = State.Ready;
		}

		void IQueryBatchStream.Run()
		{
			var buffer = new List<T>();

			foreach (var item in _source!.Read())
			{
				buffer.Add(item);
			}

			_buffer = buffer;
		}

		async Task IQueryBatchStream.RunAsync(CancellationToken cancellationToken)
		{
			var buffer = new List<T>();

			await foreach (var item in _source!.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				buffer.Add(item);
			}

			_buffer = buffer;
		}

		// A handle being read is skipped too: its enumerator stops at its next row.
		void IQueryBatchStream.Skip()
		{
			if (_state is State.Ready or State.Reading)
				_state = State.Finished;
		}

		bool IQueryBatchStream.IsOpen => _state is State.Ready or State.Reading;

		void IQueryBatchStream.Close()
		{
			if (_state is State.Delivering or State.Ready or State.Reading)
				_state = State.Closed;
		}

		void IQueryBatchStream.Release()
		{
			_source?.Close();
		}

		ValueTask IQueryBatchStream.ReleaseAsync()
		{
			return _source?.CloseAsync() ?? default;
		}
	}
}
