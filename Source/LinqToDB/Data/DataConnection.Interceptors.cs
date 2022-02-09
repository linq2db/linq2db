using System;
using System.Collections.Generic;

namespace LinqToDB.Data
{
	using Interceptors;

	public partial class DataConnection : IEntityServiceInterceptable
	{
		AggregatedInterceptor<ICommandInterceptor>?       _commandInterceptors;
		AggregatedInterceptor<IConnectionInterceptor>?    _connectionInterceptors;
		AggregatedInterceptor<IDataContextInterceptor>?   _contextInterceptors;
		AggregatedInterceptor<IEntityServiceInterceptor>? _entityServiceInterceptors;

		AggregatedInterceptor<IEntityServiceInterceptor>? IEntityServiceInterceptable.Interceptors => _entityServiceInterceptors;

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			Add(ref _commandInterceptors);
			Add(ref _connectionInterceptors);
			Add(ref _contextInterceptors);
			Add(ref _entityServiceInterceptors);

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

		internal void RemoveInterceptor(IInterceptor interceptor)
		{
			if (_commandInterceptors != null && interceptor is ICommandInterceptor commandInterceptor)
				_commandInterceptors.Remove(commandInterceptor);

			if (_connectionInterceptors != null && interceptor is IConnectionInterceptor connectionInterceptor)
				_connectionInterceptors.Remove(connectionInterceptor);

			if (_contextInterceptors != null && interceptor is IDataContextInterceptor contextInterceptor)
				_contextInterceptors.Remove(contextInterceptor);

			if (_entityServiceInterceptors != null && interceptor is IEntityServiceInterceptor entityServiceInterceptor)
				_entityServiceInterceptors.Remove(entityServiceInterceptor);
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptors == null && _connectionInterceptors == null && _contextInterceptors == null && _entityServiceInterceptors == null)
				yield break;

			var type = typeof(TInterceptor);

			if (type == typeof(ICommandInterceptor))
			{
				if (_commandInterceptors != null)
					foreach (var interceptor in _commandInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				yield break;
			}

			if (type == typeof(IConnectionInterceptor))
			{
				if (_connectionInterceptors != null)
					foreach (var interceptor in _connectionInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				yield break;
			}

			if (type == typeof(IDataContextInterceptor))
			{
				if (_contextInterceptors != null)
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				yield break;
			}

			if (type == typeof(IEntityServiceInterceptor))
			{
				if (_entityServiceInterceptors != null)
					foreach (var interceptor in _entityServiceInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				yield break;
			}

			if (type == typeof(IInterceptor))
			{
				if (_commandInterceptors != null)
					foreach (var interceptor in _commandInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				if (_connectionInterceptors != null)
					foreach (var interceptor in _connectionInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				if (_contextInterceptors != null)
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				if (_entityServiceInterceptors != null)
					foreach (var interceptor in _entityServiceInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				yield break;
			}
		}
	}
}
