namespace LinqToDB.Data
{
	using System;
	using System.Collections.Generic;
	using LinqToDB.Interceptors;

	public partial class DataConnection
	{
		private AggregatedInterceptor<ICommandInterceptor>?     _commandInterceptors;
		private AggregatedInterceptor<IConnectionInterceptor>?  _connectionInterceptors;
		private AggregatedInterceptor<IDataContextInterceptor>? _contextInterceptors;

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			// command interceptors
			if (interceptor is ICommandInterceptor commandInterceptor)
				(_commandInterceptors ??= new AggregatedInterceptor<ICommandInterceptor>()).Add(commandInterceptor);

			if (interceptor is AggregatedInterceptor<ICommandInterceptor> aggregateCommandInterceptor)
			{
				if (_commandInterceptors != null)
					// this actually shouldn't be possible
					throw new InvalidOperationException($"{nameof(AggregatedInterceptor<ICommandInterceptor>)}<{nameof(ICommandInterceptor)}> already exists");
				else
					_commandInterceptors = aggregateCommandInterceptor;
			}

			// connection interceptors
			if (interceptor is IConnectionInterceptor connectionInterceptor)
				(_connectionInterceptors ??= new AggregatedInterceptor<IConnectionInterceptor>()).Add(connectionInterceptor);

			if (interceptor is AggregatedInterceptor<IConnectionInterceptor> aggregateConnectionInterceptor)
			{
				if (_connectionInterceptors != null)
					throw new InvalidOperationException($"{nameof(AggregatedInterceptor<IConnectionInterceptor>)}<{nameof(IConnectionInterceptor)}> already exists");
				else
					_connectionInterceptors = aggregateConnectionInterceptor;
			}

			// context interceptors
			if (interceptor is IDataContextInterceptor contextInterceptor)
				(_contextInterceptors ??= new AggregatedInterceptor<IDataContextInterceptor>()).Add(contextInterceptor);

			if (interceptor is AggregatedInterceptor<IDataContextInterceptor> aggregateContextInterceptor)
			{
				if (_contextInterceptors != null)
					throw new InvalidOperationException($"{nameof(AggregatedInterceptor<IDataContextInterceptor>)}<{nameof(IDataContextInterceptor)}> already exists");
				else
					_contextInterceptors = aggregateContextInterceptor;
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
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptors == null && _connectionInterceptors == null && _contextInterceptors == null)
				yield break;

			var type = typeof(TInterceptor);

			if (type == typeof(ICommandInterceptor))
			{
				if (_commandInterceptors != null)
				{
					foreach (var interceptor in _commandInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				}

				yield break;
			}

			if (type == typeof(IConnectionInterceptor))
			{
				if (_connectionInterceptors != null)
				{
					foreach (var interceptor in _connectionInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				}

				yield break;
			}

			if (type == typeof(IDataContextInterceptor))
			{
				if (_contextInterceptors != null)
				{
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				}

				yield break;
			}

			if (type == typeof(IInterceptor))
			{
				if (_commandInterceptors != null)
				{
					foreach (var interceptor in _commandInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;
				}

				if (_connectionInterceptors != null)
				{
					foreach (var interceptor in _connectionInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				}

				if (_contextInterceptors != null)
				{
					foreach (var interceptor in _contextInterceptors.GetInterceptors())
						yield return (TInterceptor)interceptor;

				}

				yield break;
			}
		}
		
	}
}
