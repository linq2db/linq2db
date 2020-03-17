using System;
using System.Collections.Generic;

using JetBrains.Annotations;

using Npgsql;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;

	using Data.RetryPolicy;

	public class PostgreSQLRetryPolicy : RetryPolicyBase
	{
		private readonly ICollection<int> _additionalErrorNumbers;

		/// <summary>
		///   Creates a new instance of <see cref="PostgreSQLRetryPolicy" />.
		/// </summary>
		/// <remarks>
		///     The default retry limit is 5, which means that the total amount of time spent before failing is 26 seconds plus the random factor.
		/// </remarks>
		public PostgreSQLRetryPolicy()
			: this(Configuration.RetryPolicy.DefaultMaxRetryCount)
		{
		}

		/// <summary>
		///     Creates a new instance of <see cref="PostgreSQLRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public PostgreSQLRetryPolicy(int maxRetryCount)
			: this(maxRetryCount, Configuration.RetryPolicy.DefaultMaxDelay)
		{
		}

		/// <summary>
		///     Creates a new instance of <see cref="PostgreSQLRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount">The maximum number of retry attempts.</param>
		/// <param name="maxRetryDelay">The maximum delay in milliseconds between retries.</param>
		/// <param name="errorNumbersToAdd">Additional SQL error numbers that should be considered transient.</param>
		public PostgreSQLRetryPolicy(
			int maxRetryCount,
			TimeSpan maxRetryDelay,
			[CanBeNull] ICollection<int> errorNumbersToAdd = null) : base(maxRetryCount, maxRetryDelay) =>
			_additionalErrorNumbers = errorNumbersToAdd;

		protected override bool ShouldRetryOn(Exception exception)
		{
			if (_additionalErrorNumbers != null)
			{
				if (exception is PostgresException postgresException)
				{
					if (_additionalErrorNumbers.Contains(postgresException.ErrorCode))
						return true;
				}
			}

			if (exception is NpgsqlException sqlException)
			{
				return sqlException.IsTransient;
			}

			return exception is TimeoutException;
		}
	}
}
