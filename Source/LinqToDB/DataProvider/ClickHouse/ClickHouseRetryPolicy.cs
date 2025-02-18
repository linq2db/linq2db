using System;
using System.Collections.Generic;

using LinqToDB.Data.RetryPolicy;

namespace LinqToDB.DataProvider.ClickHouse
{
	/// <summary>
	/// Retry policy handles only following exceptions:
	/// <list type="bullet">
	/// <item>Octonica client ClickHouseException with codes ClickHouseErrorCodes.InvalidConnectionState, ClickHouseErrorCodes.ConnectionClosed, ClickHouseErrorCodes.NetworkError</item>
	/// <item>MySqlConnector <c>MySqlException.IsTransient == true</c> (requires .NET 6+ and MySqlConnector 1.3.0 or greater)</item>
	/// </list>
	/// </summary>
	public class ClickHouseRetryPolicy : RetryPolicyBase
	{
		readonly ICollection<int>? _additionalErrorNumbers;

		/// <summary>
		/// Creates a new instance of <see cref="ClickHouseRetryPolicy" />.
		/// </summary>
		public ClickHouseRetryPolicy()
			: this(Common.Configuration.RetryPolicy.DefaultMaxRetryCount)
		{ }

		/// <summary>
		/// Creates a new instance of <see cref="ClickHouseRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public ClickHouseRetryPolicy(int maxRetryCount)
			: this(
				maxRetryCount,
				Common.Configuration.RetryPolicy.DefaultMaxDelay,
				Common.Configuration.RetryPolicy.DefaultRandomFactor,
				Common.Configuration.RetryPolicy.DefaultExponentialBase,
				Common.Configuration.RetryPolicy.DefaultCoefficient,
				null
			)
		{ }

		/// <summary>
		///     Creates a new instance of <see cref="ClickHouseRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount">The maximum number of retry attempts.</param>
		/// <param name="maxRetryDelay">The maximum delay in milliseconds between retries.</param>
		/// <param name="randomFactor">The maximum random factor. </param>
		/// <param name="exponentialBase">The base for the exponential function used to compute the delay between retries. </param>
		/// <param name="coefficient">The coefficient for the exponential function used to compute the delay between retries. </param>
		/// <param name="errorNumbersToAdd">Additional SQL error numbers that should be considered transient.</param>
		public ClickHouseRetryPolicy(
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
				if (ClickHouseTransientExceptionDetector.IsHandled(exception, out var errorNumbers))
					foreach (var errNumber in errorNumbers)
						if (_additionalErrorNumbers.Contains(errNumber))
							return true;
			}

			return
#if NET6_0_OR_GREATER
				DbExceptionTransientExceptionDetector.ShouldRetryOn(exception) ||
#endif
				ClickHouseTransientExceptionDetector.ShouldRetryOn(exception);
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
			if (ClickHouseTransientExceptionDetector.IsHandled(exception, out var errorNumbers))
				foreach (var errNumber in errorNumbers)
					switch (errNumber)
					{
						case 3:
							return true;
					}

			return false;
		}
	}
}
