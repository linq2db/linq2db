using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	abstract class AggregatedInterceptor<TInterceptor> : IInterceptor
		where TInterceptor : IInterceptor
	{
		public List<TInterceptor> Interceptors { get; } = new ();

		// as we support interceptor removal we should delay removal when interceptors collection enumerated to
		// avoid errors
		bool _enumerating;
		readonly List<TInterceptor> _removeList = new ();

		public void Add(TInterceptor interceptor)
		{
			Interceptors.Add(interceptor);
		}

		public void Remove(TInterceptor interceptor)
		{
			if (!_enumerating)
				Interceptors.Remove(interceptor);
			else
				_removeList.Add(interceptor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void RemoveDelayed()
		{
			foreach (var interceptor in _removeList)
				Interceptors.Remove(interceptor);
			_removeList.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void Apply(Action func)
		{
			_enumerating = true;

			try
			{
				func();
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Apply<T>(Func<T> func)
		{
			_enumerating = true;

			try
			{
				return func();
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task Apply(Func<Task> func)
		{
			_enumerating = true;

			try
			{
				await func().ConfigureAwait(false);
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected async Task<T> Apply<T>(Func<Task<T>> func)
		{
			_enumerating = true;

			try
			{
				return await func().ConfigureAwait(false);
			}
			finally
			{
				_enumerating = false;
				RemoveDelayed();
			}
		}
	}
}
