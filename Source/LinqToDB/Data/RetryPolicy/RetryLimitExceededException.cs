using System;
using System.Runtime.Serialization;

namespace LinqToDB.Data.RetryPolicy
{
#if !NET8_0_OR_GREATER
	[Serializable]
#endif
	public class RetryLimitExceededException : LinqToDBException
	{
		const string RetryLimitExceededMessage = "Retry limit exceeded";

		public RetryLimitExceededException() : base(RetryLimitExceededMessage)
		{ }

		public RetryLimitExceededException(Exception innerException) : base(RetryLimitExceededMessage, innerException)
		{ }
#if !NET8_0_OR_GREATER
		protected RetryLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
#endif
	}
}
