using System;

using LinqToDB.Data.RetryPolicy;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Retry policy for YDB.
	/// Focuses on isTransient/IsTransientWhenIdempotent from the YDB SDK, plus
	/// basic gRPC codes and timeouts. On .NET 5+, it additionally uses DbException.IsTransient.
	/// </summary>
	public sealed class YdbRetryPolicy : RetryPolicyBase
	{
		readonly bool _treatAsIdempotent;

		public YdbRetryPolicy(
			int      maxRetryCount,
			TimeSpan maxRetryDelay,
			double   randomFactor,
			double   exponentialBase,
			TimeSpan coefficient,
			bool     treatAsIdempotent = true)
			: base(maxRetryCount, maxRetryDelay, randomFactor, exponentialBase, coefficient)
		{
			_treatAsIdempotent = treatAsIdempotent;
		}

		protected override bool ShouldRetryOn(Exception exception)
		{
			if (exception is OperationCanceledException)
				return false;

#if ADO_IS_TRANSIENT
			if (DbExceptionTransientExceptionDetector.ShouldRetryOn(exception))
				return true;
#endif
			if (YdbTransientExceptionDetector.ShouldRetryOn(exception, _treatAsIdempotent))
				return true;

			if (exception is TimeoutException)
				return true;

			return false;
		}
	}
}
