using System;
using System.Data.Common;

using LinqToDB.Data;
using LinqToDB.Internal.Interceptors;

namespace LinqToDB.Interceptors
{
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
	}
}
