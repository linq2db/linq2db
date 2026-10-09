using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// The stream a <see cref="LinqToDBHub"/> method returns: runs one remote operation and hands its result out as
	/// stream items.
	/// <list type="bullet">
	/// <item>The operation starts on the first read, which Signal/R issues after the hub method returns.</item>
	/// <item>Every cancellation token a read receives cancels the operation. The .NET 8+ server passes the stream's
	/// token to <see cref="WaitToReadAsync"/> (after a first <see cref="TryRead"/> without one); the legacy server
	/// passes it to <see cref="ReadAsync"/>. Both tokens fire on the client's cancellation and on disconnect.</item>
	/// <item>A read finishes only after the operation has finished, cancelled or not, so the operation never
	/// outlives the hub's service scope, which Signal/R disposes when the stream ends.</item>
	/// </list>
	/// Signal/R reads a stream from one consumer at a time, so the reads need no synchronization between
	/// themselves; only the registrations race with the operation's completion.
	/// </summary>
	sealed class OperationChannelReader<T> : ChannelReader<T>
	{
		readonly Func<CancellationToken,Task<IReadOnlyList<T>>> _operation;
		readonly Lock                                            _sync       = new();
		readonly TaskCompletionSource<bool>                      _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

		// Not linked to the incoming tokens with CreateLinkedTokenSource: registrations are disposed when the
		// operation finishes, so a long-lived connection token does not collect one per call, and the source itself
		// is never disposed, so a late cancellation can never hit a disposed source. Without timers or a wait
		// handle it holds no unmanaged resources.
		readonly CancellationTokenSource _cts = new();

		List<CancellationTokenRegistration>? _registrations = new();
		Task?                                _task;
		volatile IReadOnlyList<T>?           _items;
		int                                  _index;

		public OperationChannelReader(Func<CancellationToken,Task<IReadOnlyList<T>>> operation, CancellationToken connectionAborted)
		{
			_operation = operation;

			Register(connectionAborted);
		}

		public override Task Completion => _completion.Task;

		public override bool TryRead(out T item)
		{
			EnsureStarted();

			if (_items is { } items)
			{
				if (_index < items.Count)
				{
					item = items[_index++];

					if (_index == items.Count)
						_completion.TrySetResult(true);

					return true;
				}

				_completion.TrySetResult(true);
			}

			item = default!;
			return false;
		}

		public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
		{
			Register(cancellationToken);

			EnsureStarted();

			if (_items is { } items)
				return new ValueTask<bool>(_index < items.Count);

			return WaitToReadSlowAsync();
		}

		async ValueTask<bool> WaitToReadSlowAsync()
		{
			// Rethrows the operation's exception, cancellation included, once the operation has finished.
			await _task!.ConfigureAwait(false);

			return _index < _items!.Count;
		}

		public override ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
		{
			Register(cancellationToken);

			if (TryRead(out var item))
				return new ValueTask<T>(item);

			return ReadSlowAsync();
		}

		async ValueTask<T> ReadSlowAsync()
		{
			if (await WaitToReadAsync(default).ConfigureAwait(false) && TryRead(out var item))
				return item;

			// The legacy server ends a stream on a ChannelClosedException without an inner exception.
			throw new ChannelClosedException();
		}

		void EnsureStarted()
		{
			_task ??= RunAsync();
		}

		async Task RunAsync()
		{
			try
			{
				// Leave the reader's call first: the operation's synchronous part must not run inside TryRead,
				// which Signal/R may call from the connection's message loop.
				await Task.Yield();

				_items = await _operation(_cts.Token).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (ex is OperationCanceledException)
					_completion.TrySetCanceled();
				else
					_completion.TrySetException(ex);

				throw;
			}
			finally
			{
				ReleaseRegistrations();
			}
		}

		void Register(CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return;

			// Registered outside the lock: an already cancelled token runs the callback right here, and the
			// operation may then complete and release the registrations on this thread.
			var registration = token.Register(static state => ((OperationChannelReader<T>)state!).Cancel(), this);

			lock (_sync)
			{
				if (_registrations != null)
				{
					_registrations.Add(registration);
					return;
				}
			}

			// The operation has already finished.
			registration.Dispose();
		}

		void Cancel()
		{
			_cts.Cancel();
		}

		void ReleaseRegistrations()
		{
			List<CancellationTokenRegistration>? registrations;

			lock (_sync)
			{
				registrations  = _registrations;
				_registrations = null;
			}

			if (registrations != null)
				foreach (var registration in registrations)
					registration.Dispose();
		}
	}
}
