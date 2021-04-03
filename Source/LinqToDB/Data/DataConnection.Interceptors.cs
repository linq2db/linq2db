namespace LinqToDB.Data
{
	using System;
	using LinqToDB.Interceptors;

	public partial class DataConnection
	{
		private AggregatedInterceptor<ICommandInterceptor>?    _commandInterceptors;
		private AggregatedInterceptor<IConnectionInterceptor>? _connectionInterceptors;

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
		}

		internal void RemoveInterceptor(IInterceptor interceptor)
		{
			if (_commandInterceptors != null && interceptor is ICommandInterceptor commandInterceptor)
				_commandInterceptors.Remove(commandInterceptor);

			if (_connectionInterceptors != null && interceptor is IConnectionInterceptor connectionInterceptor)
				_connectionInterceptors.Remove(connectionInterceptor);
		}
	}
}
