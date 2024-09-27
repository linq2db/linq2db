using System;

namespace LinqToDB.Interceptors
{
	public interface IExceptionInterceptor : IInterceptor
	{
		/// <summary>
		///	Event, triggered when an exception is thrown while executing a database command/query.
		/// </summary>
		/// <param name="exception">
		/// The thrown <see cref="Exception" />.
		/// </param>
		/// <remarks>
		/// Implementors should throw their own exceptions directly in this method. If this method returns normally,
		/// then the existing exception will be thrown.
		/// </remarks>
		void ProcessException(ExceptionEventData eventData, Exception exception);
	}
}
