using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Data
{
	using Common;
	using Interceptors;

	public partial class DataConnection :
		IInterceptable<ICommandInterceptor>,
		IInterceptable<IConnectionInterceptor>,
		IInterceptable<IDataContextInterceptor>,
		IInterceptable<IEntityServiceInterceptor>
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

		/// <inheritdoc cref="IDataContext.AddInterceptor(IInterceptor)"/>
		public void AddInterceptor(IInterceptor interceptor)
		{
			InterceptorExtensions.AddInterceptor(this, interceptor);
		}

		internal void RemoveInterceptor(IInterceptor interceptor)
		{
			((IInterceptable<ICommandInterceptor>)      this).RemoveInterceptor(interceptor);
			((IInterceptable<IConnectionInterceptor>)   this).RemoveInterceptor(interceptor);
			((IInterceptable<IDataContextInterceptor>)  this).RemoveInterceptor(interceptor);
			((IInterceptable<IEntityServiceInterceptor>)this).RemoveInterceptor(interceptor);
		}

		IEnumerable<TInterceptor> IDataContext.GetInterceptors<TInterceptor>()
		{
			if (_commandInterceptor == null && _connectionInterceptor == null && _dataContextInterceptor == null && _entityServiceInterceptor == null)
				return Array<TInterceptor>.Empty;

			switch (typeof(TInterceptor))
			{
				case ICommandInterceptor       : return (IEnumerable<TInterceptor>)((IInterceptable<ICommandInterceptor>)      this).GetInterceptors();
				case IConnectionInterceptor    : return (IEnumerable<TInterceptor>)((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors();
				case IDataContextInterceptor   : return (IEnumerable<TInterceptor>)((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors();
				case IEntityServiceInterceptor : return (IEnumerable<TInterceptor>)((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors();
			}

			return
				((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors().Cast<TInterceptor>(). Union(
				((IInterceptable<IConnectionInterceptor>)   this).GetInterceptors().Cast<TInterceptor>()).Union(
				((IInterceptable<IDataContextInterceptor>)  this).GetInterceptors().Cast<TInterceptor>()).Union(
				((IInterceptable<IEntityServiceInterceptor>)this).GetInterceptors().Cast<TInterceptor>());
		}

		void IInterceptable.InterceptorAdded(IInterceptor interceptor)
		{
		}
	}
}
