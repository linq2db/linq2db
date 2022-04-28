using System;

namespace LinqToDB
{
	using Interceptors;

	public partial class DataContext :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IUnwrapDataObjectInterceptor>
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

		IUnwrapDataObjectInterceptor? IDataContext.UnwrapDataObjectInterceptor => _unwrapDataObjectInterceptor;

		AggregatedUnwrapDataObjectInterceptor? _unwrapDataObjectInterceptor;
		IUnwrapDataObjectInterceptor? IInterceptable<IUnwrapDataObjectInterceptor>.Interceptor
		{
			get => _unwrapDataObjectInterceptor;
			set => _unwrapDataObjectInterceptor = (AggregatedUnwrapDataObjectInterceptor?)value;
		}

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			switch (interceptor)
			{
				case ICommandInterceptor          cm : Add(ref _commandInterceptor,          cm); break;
				case IConnectionInterceptor       cn : Add(ref _connectionInterceptor,       cn); break;
				case IDataContextInterceptor      dc : Add(ref _dataContextInterceptor,      dc); break;
				case IEntityServiceInterceptor    es : Add(ref _entityServiceInterceptor,    es); break;
				case IUnwrapDataObjectInterceptor wr : Add(ref _unwrapDataObjectInterceptor, wr); break;
			}

			void Add<TA,TI>(ref TA? aggregator, TI intercept)
				where TI : IInterceptor
				where TA : AggregatedInterceptor<TI>, new()
			{
				if (intercept is AggregatedInterceptor<TI> ai)
				{
					if (aggregator != null)
						// this actually shouldn't be possible
						throw new InvalidOperationException($"{nameof(AggregatedInterceptor<TI>)}<{nameof(TI)}> already exists");

					aggregator = new();
					aggregator.Interceptors.AddRange(ai.Interceptors);

					_prebuiltOptionsExtension = _prebuiltOptionsExtension.WithInterceptor(aggregator);
					_prebuiltOptions          = _prebuiltOptions.WithExtension(_prebuiltOptionsExtension);
				}
				else
				{
					if (aggregator == null)
					{
						aggregator = new();
						_dataConnection?.AddInterceptor(aggregator);

						_prebuiltOptionsExtension = _prebuiltOptionsExtension.WithInterceptor(aggregator);
						_prebuiltOptions          = _prebuiltOptions.WithExtension(_prebuiltOptionsExtension);
					}

					aggregator.Interceptors.Add(intercept);
				}
			}
		}
	}
}
