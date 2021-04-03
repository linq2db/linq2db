namespace LinqToDB.Data
{
	using System;
	using LinqToDB.Interceptors;

	public partial class DataConnection
	{
		private AggregatedInterceptor<ICommandInterceptor>? _commandInterceptors;

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
		}

		internal void RemoveInterceptor(IInterceptor interceptor)
		{
			if (_commandInterceptors != null && interceptor is ICommandInterceptor commandInterceptor)
				_commandInterceptors.Remove(commandInterceptor);
		}
	}
}
