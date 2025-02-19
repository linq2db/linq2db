// BASEDON: https://github.com/aspnet/EntityFramework/blob/rel/2.0.0-preview1/src/EFCore.SqlServer/SqlServerRetryingExecutionStrategy.cs

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

using LinqToDB.Data.RetryPolicy;
using LinqToDB.Internal.DataProvider.SqlServer;

namespace LinqToDB.DataProvider.SqlServer
{
	public class SqlServerRetryPolicy : RetryPolicyBase
	{
		readonly ICollection<int>? _additionalErrorNumbers;

		/// <summary>
		///   Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <remarks>
		///     The default retry limit is 5, which means that the total amount of time spent before failing is 26 seconds plus the random factor.
		/// </remarks>
		public SqlServerRetryPolicy()
			: this(Common.Configuration.RetryPolicy.DefaultMaxRetryCount)
		{}

		/// <summary>
		///     Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public SqlServerRetryPolicy(int maxRetryCount)
			: this(
				maxRetryCount,
				Common.Configuration.RetryPolicy.DefaultMaxDelay,
				Common.Configuration.RetryPolicy.DefaultRandomFactor,
				Common.Configuration.RetryPolicy.DefaultExponentialBase,
				Common.Configuration.RetryPolicy.DefaultCoefficient,
				null
			)
		{}

		/// <summary>
		///     Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount">The maximum number of retry attempts.</param>
		/// <param name="maxRetryDelay">The maximum delay in milliseconds between retries.</param>
		/// <param name="randomFactor">The maximum random factor. </param>
		/// <param name="exponentialBase">The base for the exponential function used to compute the delay between retries. </param>
		/// <param name="coefficient">The coefficient for the exponential function used to compute the delay between retries. </param>
		/// <param name="errorNumbersToAdd">Additional SQL error numbers that should be considered transient.</param>
		public SqlServerRetryPolicy(
			int               maxRetryCount,
			TimeSpan          maxRetryDelay,
			double            randomFactor,
			double            exponentialBase,
			TimeSpan          coefficient,
			ICollection<int>? errorNumbersToAdd)
			: base(maxRetryCount, maxRetryDelay, randomFactor, exponentialBase, coefficient)
		{
			_additionalErrorNumbers = errorNumbersToAdd;
		}

		protected override bool ShouldRetryOn(Exception exception)
		{
			if (_additionalErrorNumbers != null)
			{
				if (SqlServerTransientExceptionDetector.IsHandled(exception, out var errorNumbers))
					foreach (var errNumber in errorNumbers)
						if (_additionalErrorNumbers.Contains(errNumber))
							return true;
			}

			return SqlServerTransientExceptionDetector.ShouldRetryOn(exception);
		}

		protected override TimeSpan? GetNextDelay(Exception lastException)
		{
			var baseDelay = base.GetNextDelay(lastException);

			if (baseDelay == null)
				return null;

			if (IsMemoryOptimizedError(lastException))
				return TimeSpan.FromMilliseconds(baseDelay.Value.TotalSeconds);

			return baseDelay;
		}

		static bool IsMemoryOptimizedError(Exception exception)
		{
			if (SqlServerTransientExceptionDetector.IsHandled(exception, out var errorNumbers))
				foreach (var errNumber in errorNumbers)
					switch (errNumber)
					{
						case 41301:
						case 41302:
						case 41305:
						case 41325:
						case 41839:
							return true;
					}

			return false;
		}
	}
}
