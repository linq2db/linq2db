using System;
using System.Data.Common;
using LinqToDB.Data;
using LinqToDB.Interceptors;

namespace LinqToDB
{
	/// <summary>
	/// Contains extensions that add one-time interceptors to connection.
	/// </summary>
	public static class InterceptorExtensions
	{
		#region ICommandInterceptor
		// CommandInitialized

		/// <summary>
		/// Adds <see cref="ICommandInterceptor.CommandInitialized(CommandInitializedEventData, DbCommand)"/> interceptor, fired on next command only.
		/// </summary>
		/// <param name="dataConnection">Data connection to apply interceptor to.</param>
		/// <param name="onCommandInitialized">Interceptor delegate.</param>
		public static void OnNextCommandInitialized(this DataConnection dataConnection, Func<CommandInitializedEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			dataConnection.AddInterceptor(new OneTimeCommandInterceptor(onCommandInitialized));
		}

		/// <summary>
		/// Adds <see cref="ICommandInterceptor.CommandInitialized(CommandInitializedEventData, DbCommand)"/> interceptor, fired on next command only.
		/// </summary>
		/// <param name="dataContext">Data context to apply interceptor to.</param>
		/// <param name="onCommandInitialized">Interceptor delegate.</param>
		public static void OnNextCommandInitialized(this DataContext dataContext, Func<CommandInitializedEventData, DbCommand, DbCommand> onCommandInitialized)
		{
			dataContext.AddInterceptor(new OneTimeCommandInterceptor(onCommandInitialized));
		}
		#endregion
	}
}
