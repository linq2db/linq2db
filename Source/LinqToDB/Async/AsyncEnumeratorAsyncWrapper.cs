using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if !NATIVE_ASYNC
using TASK  = System.Threading.Tasks.Task;
using TASKB = System.Threading.Tasks.Task<bool>;
#else
using TASK  = System.Threading.Tasks.ValueTask;
using TASKB = System.Threading.Tasks.ValueTask<bool>;
#endif

namespace LinqToDB.Async
{
	internal sealed class AsyncEnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
	{
		private IAsyncEnumerator<T>? _enumerator;
		private readonly Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> _init;
		private IAsyncDisposable? _disposable;

		public AsyncEnumeratorAsyncWrapper(Func<Task<Tuple<IAsyncEnumerator<T>, IAsyncDisposable?>>> init)
		{
			_init = init;
		}

		T IAsyncEnumerator<T>.Current => _enumerator!.Current;

		async TASK IAsyncDisposable.DisposeAsync()
		{
			await _enumerator!.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			if (_disposable != null)
				await _disposable.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		async TASKB IAsyncEnumerator<T>.MoveNextAsync()
		{
			if (_enumerator == null)
			{
				var tuple   = await _init().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				_enumerator = tuple.Item1;
				_disposable = tuple.Item2;
			}

			return await _enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
