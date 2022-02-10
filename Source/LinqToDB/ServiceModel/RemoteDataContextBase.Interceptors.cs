#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;

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

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_dataContextInterceptor == null && _entityServiceInterceptor == null)
				return Enumerable.Empty<TInterceptor>();

			switch (typeof(TInterceptor))
			{
				case IDataContextInterceptor   : return (IEnumerable<TInterceptor>)((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors();
				case IEntityServiceInterceptor : return (IEnumerable<TInterceptor>)((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors();
			}

			return
				((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors().Cast<TInterceptor>().Union(
				((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors().Cast<TInterceptor>());
		}
	}
}

#endif
