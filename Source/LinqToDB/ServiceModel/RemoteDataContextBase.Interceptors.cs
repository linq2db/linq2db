#if NETFRAMEWORK
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	using System;
	using LinqToDB.Interceptors;

	public abstract partial class RemoteDataContextBase
	{
		// remote context interceptors support is quite limited and supports only IDataContextInterceptor
		// interceptors, but not other interceptors, including AggregatedInterceptor<T>
		private AggregatedInterceptor<IDataContextInterceptor>? _contextInterceptors;

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			if (interceptor is AggregatedInterceptor<IDataContextInterceptor> aggregateContextInterceptor)
			{
				if (_contextInterceptors != null)
					throw new InvalidOperationException($"{nameof(AggregatedInterceptor<IDataContextInterceptor>)}<{nameof(IDataContextInterceptor)}> already exists");
				else
					_contextInterceptors = aggregateContextInterceptor;
			}

			if (interceptor is IDataContextInterceptor contextInterceptor)
				(_contextInterceptors ??= new AggregatedInterceptor<IDataContextInterceptor>()).Add(contextInterceptor);
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_contextInterceptors == null)
				yield break;

			var type = typeof(TInterceptor);

			if (type == typeof(IDataContextInterceptor) || type == typeof(IInterceptor))
			{
				foreach (var interceptor in _contextInterceptors.GetInterceptors())
					yield return (TInterceptor)interceptor;
			}

			yield break;
		}
	}
}
#endif
