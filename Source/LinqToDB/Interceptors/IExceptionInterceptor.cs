using System;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Intercepts exceptions thrown during database command or query execution.
	/// </summary>
	/// <remarks>
	/// Use for exception translation, policy enforcement, or diagnostic enrichment.
	/// Called after a command/query exception is thrown and before LinqToDB rethrows it.
	/// If an implementation throws a new exception from this interceptor, that exception replaces the original one.
	/// </remarks>
	public interface IExceptionInterceptor : IInterceptor
	{
		/// <summary>
		/// Called when an exception is thrown while executing a database command/query.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
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
