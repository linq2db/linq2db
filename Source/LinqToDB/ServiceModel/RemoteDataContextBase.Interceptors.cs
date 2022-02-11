#if NETFRAMEWORK
using System;

namespace LinqToDB.ServiceModel
{
	using Interceptors;

	public abstract partial class RemoteDataContextBase :
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>
	{
		// remote context interceptors support is quite limited and supports only IDataContextInterceptor
		// interceptors, but not other interceptors, including AggregatedInterceptor<T>
		IDataContextInterceptor? _dataContextInterceptor;
		IDataContextInterceptor? IInterceptable<IDataContextInterceptor>.Interceptor
		{
			get => _dataContextInterceptor;
			set => _dataContextInterceptor= value;
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
			this.AddInterceptorImpl(interceptor);
		}
	}
}

#endif
