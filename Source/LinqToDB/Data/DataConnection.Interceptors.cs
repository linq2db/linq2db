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
		IInterceptable<IEntityBindingInterceptor>
	{
		ICommandInterceptor? _commandInterceptor;
		ICommandInterceptor? IInterceptable<ICommandInterceptor>.Interceptor
		{
			get => _commandInterceptor;
			set => _commandInterceptor = value;
		}

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

		IUnwrapDataObjectInterceptor? IDataContext.UnwrapDataObjectInterceptor => _unwrapDataObjectInterceptor;

		IUnwrapDataObjectInterceptor? _unwrapDataObjectInterceptor;
		IUnwrapDataObjectInterceptor? IInterceptable<IUnwrapDataObjectInterceptor>.Interceptor
		{
			get => _unwrapDataObjectInterceptor;
			set => _unwrapDataObjectInterceptor = value;
		}

		IEntityBindingInterceptor? _entityBindingInterceptor;
		IEntityBindingInterceptor? IInterceptable<IEntityBindingInterceptor>.Interceptor
		{
			get => _entityBindingInterceptor;
			set => _entityBindingInterceptor = value;
		}

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

			OnRemoveInterceptor?.Invoke(interceptor);
		}
	}
}
