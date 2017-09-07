using System;
using System.Runtime.Serialization;

namespace LinqToDB.Data.RetryPolicy
{
	public class RetryLimitExceededException : LinqToDBException
	{
		const string RetryLimitExceededMessage = "Retry limit exceeded";

		public RetryLimitExceededException() : base(RetryLimitExceededMessage)
		{}

		public RetryLimitExceededException(Exception innerException) : base(RetryLimitExceededMessage, innerException)
		{}

#if !NETSTANDARD1_6
		protected RetryLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
#endif
	}
}
