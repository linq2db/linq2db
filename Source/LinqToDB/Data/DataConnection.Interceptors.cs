using System;

namespace LinqToDB.Data
{
	using Interceptors;
	using Interceptors.Internal;

	public partial class DataConnection :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IUnwrapDataObjectInterceptor>,
		IInterceptable<IEntityBindingInterceptor>,
		IInterceptable<IQueryExpressionInterceptor>
	{
		ICommandInterceptor?          IInterceptable<ICommandInterceptor>.         Interceptor { get; set; }
		IConnectionInterceptor?       IInterceptable<IConnectionInterceptor>.      Interceptor { get; set; }
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

		public Action<IInterceptor>? OnRemoveInterceptor { get; set; }

		public void RemoveInterceptor(IInterceptor interceptor)
		{
			((IInterceptable<ICommandInterceptor>)         this).RemoveInterceptor(interceptor);
			((IInterceptable<IConnectionInterceptor>)      this).RemoveInterceptor(interceptor);
			((IInterceptable<IDataContextInterceptor>)     this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityServiceInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IUnwrapDataObjectInterceptor>)this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityBindingInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IQueryExpressionInterceptor>) this).RemoveInterceptor(interceptor);

			OnRemoveInterceptor?.Invoke(interceptor);
		}
	}
}
