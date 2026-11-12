using System;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;

namespace LinqToDB.Data
{
	public partial class DataConnection :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>,
		IInterceptable<IUnwrapDataObjectInterceptor>,
		IInterceptable<IEntityBindingInterceptor>,
		IInterceptable<IQueryExpressionInterceptor>,
		IInterceptable<IExceptionInterceptor>,
		IInterceptorsGeneration
	{
		ICommandInterceptor?          IInterceptable<ICommandInterceptor>.         Interceptor { get; set; }
		IConnectionInterceptor?       IInterceptable<IConnectionInterceptor>.      Interceptor { get; set; }
		IDataContextInterceptor?      IInterceptable<IDataContextInterceptor>.     Interceptor { get; set; }
		IEntityServiceInterceptor?    IInterceptable<IEntityServiceInterceptor>.   Interceptor { get; set; }
		IUnwrapDataObjectInterceptor? IInterceptable<IUnwrapDataObjectInterceptor>.Interceptor { get; set; }
		IEntityBindingInterceptor?    IInterceptable<IEntityBindingInterceptor>.   Interceptor { get; set; }
		IQueryExpressionInterceptor?  IInterceptable<IQueryExpressionInterceptor>. Interceptor { get; set; }
		IExceptionInterceptor?        IInterceptable<IExceptionInterceptor>.       Interceptor { get; set; }

		// Changes whenever an interceptor is added or removed. A scope (UseOptions, UseMappingSchema) restores the
		// configuration id it saved only if no interceptor changed meanwhile, and an IQueryable drops the query it kept.
		int _interceptorsVersion;

		int IInterceptorsGeneration.InterceptorsGeneration => _interceptorsVersion;

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			CheckAndThrowOnDisposed();

			this.AddInterceptorImpl(interceptor);

			// interceptors that take part in query translation are part of the configuration id
			_configurationID = null;
			_interceptorsVersion++;
		}

		public Action<IInterceptor>? OnRemoveInterceptor { get; set; }

		/// <inheritdoc cref="IDataContext.RemoveInterceptor(IInterceptor)"/>
		public void RemoveInterceptor(IInterceptor interceptor)
		{
			CheckAndThrowOnDisposed();

			this.RemoveInterceptorImpl(interceptor);

			_configurationID = null;
			_interceptorsVersion++;

			OnRemoveInterceptor?.Invoke(interceptor);
		}
	}
}
