using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB
{
	using Common;
	using Interceptors;

	public partial class DataContext :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>
	{
		AggregatedCommandInterceptor? _commandInterceptor;
		ICommandInterceptor? IInterceptable<ICommandInterceptor>.Interceptor
		{
			get => _commandInterceptor;
			set => _commandInterceptor = (AggregatedCommandInterceptor?)value;
		}

		AggregatedConnectionInterceptor? _connectionInterceptor;
		IConnectionInterceptor? IInterceptable<IConnectionInterceptor>.Interceptor
		{
			get => _connectionInterceptor;
			set => _connectionInterceptor = (AggregatedConnectionInterceptor?)value;
		}

		AggregatedDataContextInterceptor? _dataContextInterceptor;
		IDataContextInterceptor? IInterceptable<IDataContextInterceptor>.Interceptor
		{
			get => _dataContextInterceptor;
			set => _dataContextInterceptor = (AggregatedDataContextInterceptor?)value;
		}

		AggregatedEntityServiceInterceptor? _entityServiceInterceptor;
		IEntityServiceInterceptor? IInterceptable<IEntityServiceInterceptor>.Interceptor
		{
			get => _entityServiceInterceptor;
			set => _entityServiceInterceptor = (AggregatedEntityServiceInterceptor?)value;
		}

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			switch (interceptor)
			{
				case ICommandInterceptor       cm : Add(ref _commandInterceptor,       cm); break;
				case IConnectionInterceptor    cn : Add(ref _connectionInterceptor,    cn); break;
				case IDataContextInterceptor   dc : Add(ref _dataContextInterceptor,   dc); break;
				case IEntityServiceInterceptor es : Add(ref _entityServiceInterceptor, es); break;
			}

			void Add<TA,TI>(ref TA? aggregator, TI interceptor)
				where TI : IInterceptor
				where TA : AggregatedInterceptor<TI>, new()
			{
				if (interceptor is AggregatedInterceptor<TI> ai)
				{
					if (aggregator != null)
						// this actually shouldn't be possible
						throw new InvalidOperationException($"{nameof(AggregatedInterceptor<TI>)}<{nameof(TI)}> already exists");

					aggregator = new();
					aggregator.Interceptors.AddRange(ai.Interceptors);

					_optionsBuilder.WithInterceptor(aggregator);
					_prebuiltOptions = _optionsBuilder.Build();
				}
				else
				{
					if (aggregator == null)
					{
						aggregator = new();
						_dataConnection?.AddInterceptor(aggregator);

						_optionsBuilder.WithInterceptor(aggregator);
						_prebuiltOptions = _optionsBuilder.Build();
					}

					aggregator.Interceptors.Add(interceptor);
				}
			}
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptor == null && _connectionInterceptor == null && _dataContextInterceptor == null && _entityServiceInterceptor == null)
				return Array<TInterceptor>.Empty;

			switch (typeof(TInterceptor))
			{
				case ICommandInterceptor:       return (IEnumerable<TInterceptor>)((IInterceptable<ICommandInterceptor>)      this).GetInterceptors();
				case IConnectionInterceptor:    return (IEnumerable<TInterceptor>)((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors();
				case IDataContextInterceptor:   return (IEnumerable<TInterceptor>)((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors();
				case IEntityServiceInterceptor: return (IEnumerable<TInterceptor>)((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors();
			}

			return
				((IInterceptable<ICommandInterceptor>)      this).GetInterceptors().Cast<TInterceptor>().Union(
				((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors().Cast<TInterceptor>()).Union(
				((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors().Cast<TInterceptor>()).Union(
				((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors().Cast<TInterceptor>());
		}
	}
}
