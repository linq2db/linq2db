﻿using System;
using System.Data.Common;

namespace LinqToDB
{
	using Data;
	using Interceptors;

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
				case ICommandInterceptor          cm : AddInterceptorImpl(interceptable, cm); break;
				case IConnectionInterceptor       cn : AddInterceptorImpl(interceptable, cn); break;
				case IDataContextInterceptor      dc : AddInterceptorImpl(interceptable, dc); break;
				case IEntityServiceInterceptor    es : AddInterceptorImpl(interceptable, es); break;
				case IUnwrapDataObjectInterceptor wr : AddInterceptorImpl(interceptable, wr); break;
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
					cmi.Interceptor = new AggregatedCommandInterceptor          { Interceptors = { cmi.Interceptor!, cm } };
					break;
				case IInterceptable<IConnectionInterceptor> ci when interceptor is IConnectionInterceptor c:
					ci.Interceptor = new AggregatedConnectionInterceptor        { Interceptors = { ci. Interceptor!, c  } };
					break;
				case IInterceptable<IDataContextInterceptor> dci when interceptor is IDataContextInterceptor dc:
					dci.Interceptor = new AggregatedDataContextInterceptor      { Interceptors = { dci.Interceptor!, dc } };
					break;
				case IInterceptable<IEntityServiceInterceptor> esi when interceptor is IEntityServiceInterceptor es:
					esi.Interceptor = new AggregatedEntityServiceInterceptor    { Interceptors = { esi.Interceptor!, es } };
					break;
				case IInterceptable<IUnwrapDataObjectInterceptor> wri when interceptor is IUnwrapDataObjectInterceptor wr:
					wri.Interceptor = new AggregatedUnwrapDataObjectInterceptor { Interceptors = { wri.Interceptor!, wr } };
					break;
				default:
					throw new NotImplementedException($"AddInterceptor for '{typeof(T).Name}' is not implemented.");
			}
		}

		internal static void RemoveInterceptor<T>(this IInterceptable<T> interceptable, IInterceptor interceptor)
			where T : IInterceptor
		{
			if (interceptor is T i)
			{
				switch (interceptable.Interceptor)
				{
					case null :
						break;
					case AggregatedInterceptor<T> ae :
						ae.Remove(i);
						break;
					case IInterceptor e when e == interceptor :
						interceptable.Interceptor = default;
						break;
				}
			}
		}

		internal static T? CloneAggregated<T>(this T? interceptor)
			where T : IInterceptor
		{
			return interceptor is AggregatedInterceptor<T> ai ? (T?)(object?)ai.Clone() : interceptor;
		}
	}
}
