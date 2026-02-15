using System;
using LinqToDB.Data.RetryPolicy;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// YDB-specific retry policy implementation.
	/// Implements official YDB retry strategies such as Full Jitter and Equal Jitter,
	/// with different backoff timings depending on status codes.
	/// Based on the retry logic from the YDB SDK (YdbRetryPolicy + YdbRetryPolicyConfig).
	/// </summary>
	public sealed class YdbRetryPolicy : RetryPolicyBase
	{
		// Default configuration matching YDB SDK:
		// MaxAttempts=10; FastBase=5ms; SlowBase=50ms;
		// FastCap=500ms; SlowCap=5000ms; EnableRetryIdempotence=false
		private readonly int  _maxAttempts;
		private readonly int  _fastBackoffBaseMs;
		private readonly int  _slowBackoffBaseMs;
		private readonly int  _fastCeiling;
		private readonly int  _slowCeiling;
		private readonly int  _fastCapBackoffMs;
		private readonly int  _slowCapBackoffMs;
		private readonly bool _enableRetryIdempotence;

		/// <summary>
		/// Default constructor — fully matches the default settings from YDB SDK.
		/// </summary>
		public YdbRetryPolicy()
			: this(
				maxAttempts: 10,
				fastBackoffBaseMs: 5,
				slowBackoffBaseMs: 50,
				fastCapBackoffMs: 500,
				slowCapBackoffMs: 5000,
				enableRetryIdempotence: false)
		{ }

		/// <summary>
		/// Extended constructor (can be used together with RetryPolicyOptions if desired).
		/// Base class parameters are not critical since we completely override the delay calculation logic.
		/// </summary>
		public YdbRetryPolicy(
			int maxAttempts,
			int fastBackoffBaseMs,
			int slowBackoffBaseMs,
			int fastCapBackoffMs,
			int slowCapBackoffMs,
			bool enableRetryIdempotence)
			: base(
				maxRetryCount: Math.Max(0, maxAttempts - 1), // prevent base mechanics from interfering
				maxRetryDelay: TimeSpan.FromMilliseconds(Math.Max(fastCapBackoffMs, slowCapBackoffMs)),
				randomFactor: 1.1,
				exponentialBase: 2.0,
				coefficient: TimeSpan.FromMilliseconds(fastBackoffBaseMs))
		{
			_maxAttempts = maxAttempts;
			_fastBackoffBaseMs = fastBackoffBaseMs;
			_slowBackoffBaseMs = slowBackoffBaseMs;
			_fastCapBackoffMs = fastCapBackoffMs;
			_slowCapBackoffMs = slowCapBackoffMs;
			_fastCeiling = (int)Math.Ceiling(Math.Log(fastCapBackoffMs + 1, 2));
			_slowCeiling = (int)Math.Ceiling(Math.Log(slowCapBackoffMs + 1, 2));
			_enableRetryIdempotence = enableRetryIdempotence;
		}

		/// <summary>
		/// Determines if the given exception is retryable according to YDB rules.
		/// </summary>
		protected override bool ShouldRetryOn(Exception exception)
			=> YdbTransientExceptionDetector.ShouldRetryOn(exception, _enableRetryIdempotence);

		/// <summary>
		/// Calculates the next retry delay based on the last exception and retry attempt count.
		/// </summary>
		protected override TimeSpan? GetNextDelay(Exception lastException)
		{
			// If it's not a YDB-specific exception — fallback to the base exponential retry logic
			if (!YdbTransientExceptionDetector.TryGetYdbException(lastException, out var ydbEx))
				return base.GetNextDelay(lastException);

			var attempt = ExceptionsEncountered.Count - 1;

			// Lifetime of retry strategy: stop retrying after reaching the maximum number of attempts
			if (attempt >= _maxAttempts - 1)
				return null;

			// Respect the IsTransient flag if idempotence is disabled
			_ = YdbTransientExceptionDetector.TryGetCodeAndTransient(ydbEx, out var code, out var isTransient);
			if (!_enableRetryIdempotence && !isTransient)
				return null;

			// Mapping of status codes to jitter type — same as in the YDB SDK:
			//  - BadSession/SessionBusy         -> 0ms
			//  - Aborted/Undetermined           -> FullJitter (fast)
			//  - Unavailable/ClientTransport*   -> EqualJitter (fast)
			//  - Overloaded/ClientTransportRes* -> EqualJitter (slow)
			return code switch
			{
				"BadSession" or "SessionBusy"
					=> TimeSpan.Zero,

				"Aborted" or "Undetermined"
					=> FullJitter(_fastBackoffBaseMs, _fastCapBackoffMs, _fastCeiling, attempt),

				"Unavailable" or "ClientTransportUnknown" or "ClientTransportUnavailable"
					=> EqualJitter(_fastBackoffBaseMs, _fastCapBackoffMs, _fastCeiling, attempt),

				"Overloaded" or "ClientTransportResourceExhausted"
					=> EqualJitter(_slowBackoffBaseMs, _slowCapBackoffMs, _slowCeiling, attempt),

				_ => null,
			};
		}

		// ===== Algorithms based on the official YDB SDK =====

		/// <summary>
		/// Full Jitter backoff calculation — completely random delay in the range [0..maxBackoff].
		/// </summary>
		private TimeSpan FullJitter(int backoffBaseMs, int capMs, int ceiling, int attempt)
		{
			var maxMs = CalculateBackoff(backoffBaseMs, capMs, ceiling, attempt);
			// Random.Next(max) — [0..max-1], in SDK they add +1 to avoid a strictly zero delay
			return TimeSpan.FromMilliseconds(Random.Next(maxMs + 1));
		}

		/// <summary>
		/// Equal Jitter backoff calculation — base delay + random jitter in [0..halfBackoff].
		/// </summary>
		private TimeSpan EqualJitter(int backoffBaseMs, int capMs, int ceiling, int attempt)
		{
			var calc = CalculateBackoff(backoffBaseMs, capMs, ceiling, attempt);
			var half = calc / 2;
			// SDK: temp + calculatedBackoff % 2 + random.Next(temp + 1)
			return TimeSpan.FromMilliseconds(half + calc % 2 + Random.Next(half + 1));
		}

		/// <summary>
		/// Exponential backoff calculation with upper cap.
		/// </summary>
		private static int CalculateBackoff(int backoffBaseMs, int capMs, int ceiling, int attempt)
			=> Math.Min(backoffBaseMs * (1 << Math.Min(ceiling, attempt)), capMs);
	}
}
