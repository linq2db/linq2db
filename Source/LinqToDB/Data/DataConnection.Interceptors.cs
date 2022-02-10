using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Data
{
	using Common;
	using Interceptors;

	public partial class DataConnection :
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IDataContextInterceptor>
	{
		AggregatedInterceptor<ICommandInterceptor>?       _commandInterceptors;
		AggregatedInterceptor<IConnectionInterceptor>?    _connectionInterceptors;

		IDataContextInterceptor? _dataContextInterceptor;
		IDataContextInterceptor? IInterceptable<IDataContextInterceptor>.Interceptor
		{
			get => _dataContextInterceptor;
			set => _dataContextInterceptor = value;
		}

		IEntityServiceInterceptor? _entityServiceInterceptor;
		IEntityServiceInterceptor? IInterceptable<IEntityServiceInterceptor>.Interceptor
		{
			get => _entityServiceInterceptor;
			set => _entityServiceInterceptor = value;
		}

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			Add(ref _commandInterceptors);
			Add(ref _connectionInterceptors);
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

		internal void RemoveInterceptor(IInterceptor interceptor)
		{
			if (_commandInterceptors != null && interceptor is ICommandInterceptor commandInterceptor)
				_commandInterceptors.Remove(commandInterceptor);

			if (_connectionInterceptors != null && interceptor is IConnectionInterceptor connectionInterceptor)
				_connectionInterceptors.Remove(connectionInterceptor);

			((IInterceptable<IDataContextInterceptor>)  this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityServiceInterceptor>)this).RemoveInterceptor(interceptor);
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptors == null && _connectionInterceptors == null && _dataContextInterceptor == null && _entityServiceInterceptor == null)
				return Array<TInterceptor>.Empty;

			switch (typeof(TInterceptor))
			{
				case ICommandInterceptor:
					if (_commandInterceptors != null)
						return (IEnumerable<TInterceptor>)_commandInterceptors.GetInterceptors();
					break;
				case IConnectionInterceptor:
					if (_connectionInterceptors != null)
						return (IEnumerable<TInterceptor>)_connectionInterceptors.GetInterceptors();
					break;
				case IDataContextInterceptor   : return (IEnumerable<TInterceptor>)((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors();
				case IEntityServiceInterceptor : return (IEnumerable<TInterceptor>)((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors();
			}

			IEnumerable<TInterceptor> result = Array<TInterceptor>.Empty;

			if (_commandInterceptors != null)
				result = result.Concat(_commandInterceptors.GetInterceptors().Cast<TInterceptor>());

			if (_connectionInterceptors != null)
				result = result.Concat(_connectionInterceptors.GetInterceptors().Cast<TInterceptor>());

			return result
				.Union(((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors().Cast<TInterceptor>())
				.Union(((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors().Cast<TInterceptor>());
		}

		void IInterceptable.InterceptorAdded(IInterceptor interceptor)
		{
		}
	}
}
