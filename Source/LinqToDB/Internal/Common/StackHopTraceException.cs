using System;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Represents an exception that captures a stack trace at a stack-hop boundary for diagnostic purposes.
	/// </summary>
	/// <remarks>This exception is typically used to preserve the call stack at a specific point in asynchronous or
	/// multi-threaded code, where the logical flow of execution may cross thread or context boundaries. The captured stack
	/// trace can be accessed via the <see cref="CapturedStackTrace"/> property to aid in debugging complex call
	/// flows.</remarks>
	public sealed class StackHopTraceException : Exception
	{
		readonly System.Diagnostics.StackTrace _capturedTrace;

		public StackHopTraceException(System.Diagnostics.StackTrace capturedTrace)
			: base("Captured caller stack at stack-hop boundary")
		{
			_capturedTrace = capturedTrace ?? new System.Diagnostics.StackTrace();
		}

		public System.Diagnostics.StackTrace CapturedStackTrace => _capturedTrace;

		public override string ToString()
		{
			return base.ToString() + Environment.NewLine + _capturedTrace;
		}
	}
}
