using System;

namespace LinqToDB.Remote
{
	using Interceptors;

	using Interceptors.Internal;

	public abstract partial class RemoteDataContextBase :
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IUnwrapDataObjectInterceptor>,
		IInterceptable<IEntityBindingInterceptor>,
		IInterceptable<IQueryExpressionInterceptor>
	{
		// remote context interceptors support is quite limited and supports only IDataContextInterceptor
		// interceptors, but not other interceptors, including AggregatedInterceptor<T>
		IDataContextInterceptor?      IInterceptable<IDataContextInterceptor>.     Interceptor { get; set; }
		IEntityServiceInterceptor?    IInterceptable<IEntityServiceInterceptor>.   Interceptor { get; set; }
		IUnwrapDataObjectInterceptor? IInterceptable<IUnwrapDataObjectInterceptor>.Interceptor { get; set; }
		IEntityBindingInterceptor?    IInterceptable<IEntityBindingInterceptor>.   Interceptor { get; set; }
		IQueryExpressionInterceptor?  IInterceptable<IQueryExpressionInterceptor>. Interceptor { get; set; }

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			this.AddInterceptorImpl(interceptor);
		}

		/// <inheritdoc cref="IDataContext.RemoveInterceptor(IInterceptor)"/>
		public void RemoveInterceptor(IInterceptor interceptor)
		{
			((IInterceptable<IDataContextInterceptor>)     this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityServiceInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IUnwrapDataObjectInterceptor>)this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityBindingInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IQueryExpressionInterceptor>) this).RemoveInterceptor(interceptor);
		}
	}
}
