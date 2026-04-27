using System;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with a no-op implementation for <see cref="IExceptionInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when translating, enriching, or observing command/query exceptions.
	/// For callback timing and exception replacement behavior, see <see cref="IExceptionInterceptor"/>.
	/// </remarks>
	public class ExceptionInterceptor : IExceptionInterceptor
	{
		/// <inheritdoc />
		public virtual void ProcessException(ExceptionEventData eventData, Exception exception) { }
	}
}
