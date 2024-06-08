﻿// IsTransient property added in .NET 5
// Some providers implement it for other runtimes (e.g. MySqlConnector), but we don't handle them here (could be added on request)
#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace LinqToDB.Data.RetryPolicy
{
	using Common;
	using Data.RetryPolicy;

	/// <summary>
	/// Retry policy handles exceptions with <c>DbException.IsTransient == true</c> (requires provider support and .NET 6 or greater).
	/// </summary>
	public sealed class TransientRetryPolicy : RetryPolicyBase
	{
		/// <summary>
		/// Creates a new instance of <see cref="TransientRetryPolicy" />.
		/// </summary>
		public TransientRetryPolicy()
			: this(Configuration.RetryPolicy.DefaultMaxRetryCount)
		{ }

		/// <summary>
		/// Creates a new instance of <see cref="TransientRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public TransientRetryPolicy(int maxRetryCount)
			: this(maxRetryCount, Configuration.RetryPolicy.DefaultMaxDelay, Configuration.RetryPolicy.DefaultRandomFactor, Configuration.RetryPolicy.DefaultExponentialBase, Configuration.RetryPolicy.DefaultCoefficient)
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
