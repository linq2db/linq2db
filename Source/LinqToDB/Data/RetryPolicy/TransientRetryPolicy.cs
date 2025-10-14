// IsTransient property added in .NET 5
// Some providers implement it for other runtimes (e.g. MySqlConnector), but we don't handle them here (could be added on request)
#if ADO_IS_TRANSIENT
using System;
using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Data.RetryPolicy;

namespace LinqToDB.Data.RetryPolicy
{
	/// <summary>
	/// Retry policy handles exceptions with <c>DbException.IsTransient == true</c> (requires provider support and .NET 6 or greater).
	/// </summary>
	public sealed class TransientRetryPolicy : RetryPolicyBase
	{
		/// <summary>
		/// Creates a new instance of <see cref="TransientRetryPolicy" />.
		/// </summary>
		public TransientRetryPolicy()
			: this(Common.Configuration.RetryPolicy.DefaultMaxRetryCount)
		{ }

		/// <summary>
		/// Creates a new instance of <see cref="TransientRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public TransientRetryPolicy(int maxRetryCount)
			: this(
				maxRetryCount,
				Common.Configuration.RetryPolicy.DefaultMaxDelay,
				Common.Configuration.RetryPolicy.DefaultRandomFactor,
				Common.Configuration.RetryPolicy.DefaultExponentialBase,
				Common.Configuration.RetryPolicy.DefaultCoefficient
			)
		{ }

		/// <summary>
		/// Creates a new instance of <see cref="TransientRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount">The maximum number of retry attempts.</param>
		/// <param name="maxRetryDelay">The maximum delay in milliseconds between retries.</param>
		/// <param name="randomFactor">The maximum random factor. </param>
		/// <param name="exponentialBase">The base for the exponential function used to compute the delay between retries. </param>
		/// <param name="coefficient">The coefficient for the exponential function used to compute the delay between retries. </param>
		public TransientRetryPolicy(
			int               maxRetryCount,
			TimeSpan          maxRetryDelay,
			double            randomFactor,
			double            exponentialBase,
			TimeSpan          coefficient)
			: base(maxRetryCount, maxRetryDelay, randomFactor, exponentialBase, coefficient)
		{
		}

		protected override bool ShouldRetryOn(Exception exception) => DbExceptionTransientExceptionDetector.ShouldRetryOn(exception);
	}
}
#endif
