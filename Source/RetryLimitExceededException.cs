using System;
using System.Runtime.Serialization;

namespace LinqToDB
{
	public class RetryLimitExceededException : LinqToDBException
	{
		private const string _retryLimitExceededMessage = "Retry limit exceeded";

		public RetryLimitExceededException() : base(_retryLimitExceededMessage)
		{}

		public RetryLimitExceededException(Exception innerException) : base(_retryLimitExceededMessage, innerException)
		{}

#if !SILVERLIGHT && !NETFX_CORE && !NETSTANDARD
		protected RetryLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
#endif
	}
}