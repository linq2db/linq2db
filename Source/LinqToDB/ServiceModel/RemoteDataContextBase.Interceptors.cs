#if NETFRAMEWORK
using System.Collections.Generic;

namespace LinqToDB.ServiceModel
{
	using System;
	using Interceptors;

	public abstract partial class RemoteDataContextBase : IInterceptable<IEntityServiceInterceptor>
	{
		// remote context interceptors support is quite limited and supports only IDataContextInterceptor
		// interceptors, but not other interceptors, including AggregatedInterceptor<T>
		AggregatedInterceptor<IDataContextInterceptor>? _contextInterceptors;

		IEntityServiceInterceptor? _entityServiceInterceptor;
		IEntityServiceInterceptor? IInterceptable<IEntityServiceInterceptor>.Interceptor
		{
			get => _entityServiceInterceptor;
			set => _entityServiceInterceptor = value;
		}

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			Add(ref _contextInterceptors);
			InterceptorExtensions.AddInterceptor(this, interceptor);

			void Add<T>(ref AggregatedInterceptor<T>? aggregator)
				where T : IInterceptor
			{
				if (interceptor is T i)
					(aggregator ??= new ()).Add(i);

				if (interceptor is AggregatedInterceptor<T> ai)
				{
					if (aggregator != null)
						throw new InvalidOperationException($"{nameof(AggregatedInterceptor<T>)}<{nameof(T)}> already exists");
					aggregator = ai;
				}
			}
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_contextInterceptors == null && _entityServiceInterceptor == null)
				yield break;

			var type = typeof(TInterceptor);

			if (type == typeof(IDataContextInterceptor))
			{
				if (_contextInterceptors != null)
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				yield break;
			}

			if (type == typeof(IEntityServiceInterceptor))
			{
				if (_entityServiceInterceptor != null)
				{
					if (_entityServiceInterceptor is AggregatedEntityServiceInterceptor entityServiceInterceptor)
						foreach (var interceptor in entityServiceInterceptor.GetInterceptors())
							yield return (TInterceptor)interceptor;
					else
						yield return (TInterceptor)_entityServiceInterceptor;
				}

				yield break;
			}

			if (type == typeof(IInterceptor))
			{
				if (_contextInterceptors != null)
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				if (_entityServiceInterceptor != null)
				{
					if (_entityServiceInterceptor is AggregatedEntityServiceInterceptor entityServiceInterceptor)
						foreach (var interceptor in entityServiceInterceptor.GetInterceptors())
							yield return (TInterceptor)interceptor;
					else
						yield return (TInterceptor)_entityServiceInterceptor;
				}

				yield break;
			}
		}

		void IInterceptable.InterceptorAdded(IInterceptor interceptor)
		{
		}
	}
}

#endif
