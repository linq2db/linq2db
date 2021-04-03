using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
		public TResult Apply<TArg, TResult>(Func<TInterceptor, TArg, TResult, TResult> apply, TArg arg1, TResult arg2)
		{
			_enumerating = true;
			try
			{
				foreach (var interceptor in _interceptors)
					arg2 = apply(interceptor, arg1, arg2);

				return arg2;
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
