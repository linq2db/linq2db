using System;

namespace LinqToDB
{
	using Interceptors;
	using Interceptors.Internal;

	public partial class DataContext :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IUnwrapDataObjectInterceptor>,
		IInterceptable<IEntityBindingInterceptor>,
		IInterceptable<IQueryExpressionInterceptor>,
		IInterceptable<IExceptionInterceptor>
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

		AggregatedUnwrapDataObjectInterceptor? _unwrapDataObjectInterceptor;
		IUnwrapDataObjectInterceptor? IInterceptable<IUnwrapDataObjectInterceptor>.Interceptor
		{
			get => _unwrapDataObjectInterceptor;
			set => _unwrapDataObjectInterceptor = (AggregatedUnwrapDataObjectInterceptor?)value;
		}

		AggregatedEntityBindingInterceptor? _entityBindingInterceptor;
		IEntityBindingInterceptor? IInterceptable<IEntityBindingInterceptor>.Interceptor
		{
			get => _entityBindingInterceptor;
			set => _entityBindingInterceptor = (AggregatedEntityBindingInterceptor?)value;
		}

		AggregatedQueryExpressionInterceptor? _queryExpressionInterceptor;
		IQueryExpressionInterceptor? IInterceptable<IQueryExpressionInterceptor>.Interceptor
		{
			get => _queryExpressionInterceptor;
			set => _queryExpressionInterceptor = (AggregatedQueryExpressionInterceptor?)value;
		}

		AggregatedExceptionInterceptor? _exceptionInterceptor;
		IExceptionInterceptor? IInterceptable<IExceptionInterceptor>.Interceptor
		{
			get => _exceptionInterceptor;
			set => _exceptionInterceptor = (AggregatedExceptionInterceptor?)value;
		}

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			AddInterceptor(interceptor, true);
		}

		void AddInterceptor(IInterceptor interceptor, bool addToOptions)
		{
			if (addToOptions)
				Options = Options.UseInterceptor(interceptor);

			switch (interceptor)
			{
				case ICommandInterceptor          cm: Add(ref _commandInterceptor,          cm); break;
				case IConnectionInterceptor       cn: Add(ref _connectionInterceptor,       cn); break;
				case IDataContextInterceptor      dc: Add(ref _dataContextInterceptor,      dc); break;
				case IEntityServiceInterceptor    es: Add(ref _entityServiceInterceptor,    es); break;
				case IUnwrapDataObjectInterceptor wr: Add(ref _unwrapDataObjectInterceptor, wr); break;
				case IEntityBindingInterceptor    ex: Add(ref _entityBindingInterceptor,    ex); break;
				case IQueryExpressionInterceptor  ex: Add(ref _queryExpressionInterceptor,  ex); break;
				case IExceptionInterceptor        ex: Add(ref _exceptionInterceptor,        ex); break;
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
				}
				else
				{
					if (aggregator == null)
					{
						aggregator = new();
						_dataConnection?.AddInterceptor(aggregator);
					}

					aggregator.Interceptors.Add(intercept);
				}
			}
		}

		public void RemoveInterceptor(IInterceptor interceptor)
		{
			Options = Options.RemoveInterceptor(interceptor);

			this.RemoveInterceptorImpl(interceptor);
		}
	}
}
