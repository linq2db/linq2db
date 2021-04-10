using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	internal sealed class AggregatedInterceptor<TInterceptor>: IInterceptor where TInterceptor : IInterceptor
	{
		private readonly List<TInterceptor> _interceptors = new ();

		// as we support interceptor removal we should delay removal when interceptors collection enumerated to
		// avoid errors
		private bool _enumerating;
		private readonly IList<TInterceptor> _removeList = new List<TInterceptor>();

		public void Add(TInterceptor interceptor)
		{
			_interceptors.Add(interceptor);
		}

		public void Remove(TInterceptor interceptor)
		{
			if (!_enumerating)
				_interceptors.Remove(interceptor);
			else
				_removeList.Add(interceptor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RemoveDelayed()
		{
			foreach (var interceptor in _removeList)
				_interceptors.Remove(interceptor);
		}

		// add overloads for other signatures when we have them

		// result = event(arg, result)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TResult Apply<TArg, TResult>(Func<TInterceptor, TArg, TResult, TResult> apply, TArg arg, TResult result)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					result = apply(interceptor, arg, result);

				return result;
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// void event(arg1, arg2)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Apply<TArg1, TArg2>(Action<TInterceptor, TArg1, TArg2> apply, TArg1 arg1, TArg2 arg2)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					apply(interceptor, arg1, arg2);
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// Task event(arg1, arg2)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task Apply<TArg1, TArg2>(Func<TInterceptor, TArg1, TArg2, CancellationToken, Task> apply, TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					await apply(interceptor, arg1, arg2, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// result = event(arg1, arg2, result)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TResult Apply<TArg1, TArg2, TResult>(Func<TInterceptor, TArg1, TArg2, TResult, TResult> apply, TArg1 arg1, TArg2 arg2, TResult result)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					result = apply(interceptor, arg1, arg2, result);

				return result;
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// result = await event(arg1, arg2, result, token)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<TResult> Apply<TArg1, TArg2, TResult>(Func<TInterceptor, TArg1, TArg2, TResult, CancellationToken, Task<TResult>> apply, TArg1 arg1, TArg2 arg2, TResult result, CancellationToken cancellationToken)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					result = await apply(interceptor, arg1, arg2, result, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				return result;
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// result = event(arg1, arg2, arg3, result)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TResult Apply<TArg1, TArg2, TArg3, TResult>(Func<TInterceptor, TArg1, TArg2, TArg3, TResult, TResult> apply, TArg1 arg1, TArg2 arg2, TArg3 arg3, TResult result)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					result = apply(interceptor, arg1, arg2, arg3, result);

				return result;
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		// result = await event(arg1, arg2, arg3, result, token)
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<TResult> Apply<TArg1, TArg2, TArg3, TResult>(Func<TInterceptor, TArg1, TArg2, TArg3, TResult, CancellationToken, Task<TResult>> apply, TArg1 arg1, TArg2 arg2, TArg3 arg3, TResult result, CancellationToken cancellationToken)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					result = await apply(interceptor, arg1, arg2, arg3, result, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext); ;

				return result;
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		public AggregatedInterceptor<TInterceptor> Clone()
		{
			var clone = new AggregatedInterceptor<TInterceptor>();
			clone._interceptors.AddRange(_interceptors);
			return clone;
		}
	}
}
