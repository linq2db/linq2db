// BASEDON: https://github.com/aspnet/EntityFramework/blob/rel/2.0.0-preview1/src/EFCore.SqlServer/SqlServerRetryingExecutionStrategy.cs

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SqlServer
{
	public class SqlServerRetryPolicy : RetryPolicyBase
	{
		private readonly ICollection<int> _additionalErrorNumbers;

		/// <summary>
		///   Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <remarks>
		///     The default retry limit is 5, which means that the total amount of time spent before failing is 26 seconds plus the random factor.
		/// </remarks>
		public SqlServerRetryPolicy() : this(DefaultMaxRetryCount)
		{}

		/// <summary>
		///     Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		public SqlServerRetryPolicy(int maxRetryCount) : this(maxRetryCount, DefaultMaxDelay, null)
		{}

		/// <summary>
		///     Creates a new instance of <see cref="SqlServerRetryPolicy" />.
		/// </summary>
		/// <param name="maxRetryCount"> The maximum number of retry attempts. </param>
		/// <param name="maxRetryDelay"> The maximum delay in milliseconds between retries. </param>
		/// <param name="errorNumbersToAdd"> Additional SQL error numbers that should be considered transient. </param>
		public SqlServerRetryPolicy(
			int maxRetryCount,
			TimeSpan maxRetryDelay,
			[CanBeNull] ICollection<int> errorNumbersToAdd)
			: base(maxRetryCount, maxRetryDelay)
		{
			_additionalErrorNumbers = errorNumbersToAdd;
		}

		protected override bool ShouldRetryOn(Exception exception)
		{
			if (_additionalErrorNumbers != null)
			{
				var sqlException = exception as SqlException;
				if (sqlException != null)
					foreach (SqlError err in sqlException.Errors)
						if (_additionalErrorNumbers.Contains(err.Number))
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

		private static bool IsMemoryOptimizedError(Exception exception)
		{
			var sqlException = exception as SqlException;
			if (sqlException != null)
				foreach (SqlError err in sqlException.Errors)
					switch (err.Number)
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