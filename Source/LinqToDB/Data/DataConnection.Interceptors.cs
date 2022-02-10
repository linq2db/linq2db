using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Data
{
	using Common;
	using Interceptors;

	public partial class DataConnection :
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>
	{
		AggregatedInterceptor<ICommandInterceptor>?       _commandInterceptors;

		IConnectionInterceptor? _connectionInterceptor;
		IConnectionInterceptor? IInterceptable<IConnectionInterceptor>.Interceptor
		{
			get => _connectionInterceptor;
			set => _connectionInterceptor = value;
		}

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

			((IInterceptable<IConnectionInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IDataContextInterceptor>)  this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityServiceInterceptor>)this).RemoveInterceptor(interceptor);
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptors == null && _connectionInterceptor == null && _dataContextInterceptor == null && _entityServiceInterceptor == null)
				return Array<TInterceptor>.Empty;

			switch (typeof(TInterceptor))
			{
				case ICommandInterceptor:
					if (_commandInterceptors != null)
						return (IEnumerable<TInterceptor>)_commandInterceptors.GetInterceptors();
					break;
				case IConnectionInterceptor    : return (IEnumerable<TInterceptor>)((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors();
				case IDataContextInterceptor   : return (IEnumerable<TInterceptor>)((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors();
				case IEntityServiceInterceptor : return (IEnumerable<TInterceptor>)((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors();
			}

			IEnumerable<TInterceptor> result = Array<TInterceptor>.Empty;

			if (_commandInterceptors != null)
				result = result.Concat(_commandInterceptors.GetInterceptors().Cast<TInterceptor>());

			return result
				.Union(((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors().Cast<TInterceptor>())
				.Union(((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors().Cast<TInterceptor>())
				.Union(((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors().Cast<TInterceptor>());
		}

		void IInterceptable.InterceptorAdded(IInterceptor interceptor)
		{
		}
	}
}
