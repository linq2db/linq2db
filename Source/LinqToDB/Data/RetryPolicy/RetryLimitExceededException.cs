using System;
using System.Runtime.Serialization;

namespace LinqToDB.Data.RetryPolicy
{
	[Serializable]
	public class RetryLimitExceededException : LinqToDBException
	{
		const string RetryLimitExceededMessage = "Retry limit exceeded";

		public RetryLimitExceededException() : base(RetryLimitExceededMessage)
		{}

		public RetryLimitExceededException(Exception innerException) : base(RetryLimitExceededMessage, innerException)
		{}
	}
}
