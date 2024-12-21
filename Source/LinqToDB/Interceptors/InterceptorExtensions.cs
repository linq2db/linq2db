using System;
using System.Data.Common;

namespace LinqToDB.Interceptors
{
	using Data;
	using Interceptors.Internal;

	/// <summary>
	/// Contains extensions that add one-time interceptors to connection.
	/// </summary>
	public static class InterceptorExtensions
	{
		#region ICommandInterceptor

		// CommandInitialized

		/// <summary>
		/// Adds <see cref="ICommandInterceptor.CommandInitialized(CommandEventData, DbCommand)"/> interceptor, fired on next command only.
		/// </summary>
		/// <param name="dataConnection">Data connection to apply interceptor to.</param>
		/// <param name="onCommandInitialized">Interceptor delegate.</param>
		public static void OnNextCommandInitialized(this DataConnection dataConnection, Func<CommandEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			dataConnection.AddInterceptor(new OneTimeCommandInterceptor(onCommandInitialized));
		}

		/// <summary>
		/// Adds <see cref="ICommandInterceptor.CommandInitialized(CommandEventData, DbCommand)"/> interceptor, fired on next command only.
		/// </summary>
		/// <param name="dataContext">Data context to apply interceptor to.</param>
		/// <param name="onCommandInitialized">Interceptor delegate.</param>
		public static void OnNextCommandInitialized(this DataContext dataContext, Func<CommandEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			dataContext.AddInterceptor(new OneTimeCommandInterceptor(onCommandInitialized));
		}

		#endregion

		internal static void AddInterceptorImpl(this IInterceptable interceptable, IInterceptor interceptor)
		{
			switch (interceptor)
			{
				case ICommandInterceptor          cm: AddInterceptorImpl(interceptable, cm); break;
				case IConnectionInterceptor       cn: AddInterceptorImpl(interceptable, cn); break;
				case IDataContextInterceptor      dc: AddInterceptorImpl(interceptable, dc); break;
				case IEntityServiceInterceptor    es: AddInterceptorImpl(interceptable, es); break;
				case IUnwrapDataObjectInterceptor wr: AddInterceptorImpl(interceptable, wr); break;
				case IEntityBindingInterceptor    ex: AddInterceptorImpl(interceptable, ex); break;
				case IQueryExpressionInterceptor  ep: AddInterceptorImpl(interceptable, ep); break;
				case IExceptionInterceptor        ex: AddInterceptorImpl(interceptable, ex); break;
			}
		}

		internal static void AddInterceptorImpl<T>(this IInterceptable interceptable, T interceptor)
			where T : IInterceptor
		{
			if (interceptable is not IInterceptable<T> typedInterceptable)
				throw new ArgumentException($"Context of type {interceptable.GetType()} doesn't support {typeof(T)} interceptor");

			if (typedInterceptable.Interceptor == null)
				typedInterceptable.Interceptor = interceptor;
			else if (typedInterceptable.Interceptor is AggregatedInterceptor<T> aggregated)
				aggregated.Interceptors.Add(interceptor);
			else switch (typedInterceptable)
			{
				case IInterceptable<ICommandInterceptor> cmi when interceptor is ICommandInterceptor cm:
					cmi.Interceptor = new AggregatedCommandInterceptor          { Interceptors = { cmi. Interceptor!, cm } };
					break;
				case IInterceptable<IConnectionInterceptor> ci when interceptor is IConnectionInterceptor c:
					ci.Interceptor = new AggregatedConnectionInterceptor        { Interceptors = { ci.  Interceptor!, c  } };
					break;
				case IInterceptable<IDataContextInterceptor> dci when interceptor is IDataContextInterceptor dc:
					dci.Interceptor = new AggregatedDataContextInterceptor      { Interceptors = { dci. Interceptor!, dc } };
					break;
				case IInterceptable<IEntityServiceInterceptor> esi when interceptor is IEntityServiceInterceptor es:
					esi.Interceptor = new AggregatedEntityServiceInterceptor    { Interceptors = { esi. Interceptor!, es } };
					break;
				case IInterceptable<IUnwrapDataObjectInterceptor> wri when interceptor is IUnwrapDataObjectInterceptor wr:
					wri.Interceptor = new AggregatedUnwrapDataObjectInterceptor { Interceptors = { wri. Interceptor!, wr } };
					break;
				case IInterceptable<IEntityBindingInterceptor> exi when interceptor is IEntityBindingInterceptor ex:
					exi.Interceptor = new AggregatedEntityBindingInterceptor    { Interceptors = { exi. Interceptor!, ex } };
					break;
				case IInterceptable<IQueryExpressionInterceptor> qexi when interceptor is IQueryExpressionInterceptor ex:
					qexi.Interceptor = new AggregatedQueryExpressionInterceptor { Interceptors = { qexi.Interceptor!, ex } };
					break;
				case IInterceptable<IExceptionInterceptor> exi when interceptor is IExceptionInterceptor ex:
					exi.Interceptor = new AggregatedExceptionInterceptor        { Interceptors = { exi. Interceptor!, ex } };
					break;
				default:
					throw new NotImplementedException($"AddInterceptor for '{typeof(T).Name}' is not implemented.");
			}
		}

		internal static void RemoveInterceptorImpl(this IInterceptable interceptable, IInterceptor interceptor)
		{
			switch (interceptor)
			{
				case ICommandInterceptor          cm: RemoveInterceptorImpl(interceptable, cm); break;
				case IConnectionInterceptor       cn: RemoveInterceptorImpl(interceptable, cn); break;
				case IDataContextInterceptor      dc: RemoveInterceptorImpl(interceptable, dc); break;
				case IEntityServiceInterceptor    es: RemoveInterceptorImpl(interceptable, es); break;
				case IUnwrapDataObjectInterceptor wr: RemoveInterceptorImpl(interceptable, wr); break;
				case IEntityBindingInterceptor    ex: RemoveInterceptorImpl(interceptable, ex); break;
				case IQueryExpressionInterceptor  ep: RemoveInterceptorImpl(interceptable, ep); break;
				case IExceptionInterceptor        ex: RemoveInterceptorImpl(interceptable, ex); break;
			}
		}

		internal static void RemoveInterceptorImpl<T>(this IInterceptable interceptable, T interceptor)
			where T : IInterceptor
		{
			if (interceptable is not IInterceptable<T> typedInterceptable)
				throw new ArgumentException($"Context of type {interceptable.GetType()} doesn't support {typeof(T)} interceptor");

			switch (typedInterceptable.Interceptor)
			{
				case AggregatedInterceptor<T> ae:
					ae.Remove(interceptor);
					break;
				case T e when ReferenceEquals(e, interceptor):
					typedInterceptable.Interceptor = default;
					break;
				default:
					break;
			}
		}
	}
}
