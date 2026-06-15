// The YdbException test double (same full name as the real internal type) is declared in
// YdbTransientExceptionDetectorTests.cs; referencing it triggers the same intentional CS0436.
#pragma warning disable CS0436

using System;

using LinqToDB.Internal.DataProvider.Ydb;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbRetryPolicyTests : TestBase
	{
		const int MaxAttempts = 5;

		// All backoff bases/caps are zeroed so retried operations don't actually sleep — the tests
		// assert retry *behaviour* (how many times the operation runs), driven through the public
		// Execute API, not the internal delay values.
		static YdbRetryPolicy Policy(bool enableRetryIdempotence)
			=> new(
				maxAttempts: MaxAttempts,
				fastBackoffBaseMs: 0,
				slowBackoffBaseMs: 0,
				fastCapBackoffMs: 0,
				slowCapBackoffMs: 0,
				enableRetryIdempotence: enableRetryIdempotence);

		static Exception YdbEx(string code, bool isTransient = false)
			=> new global::Ydb.Sdk.Ado.YdbException(code, isTransient, $"YDB error: {code}");

		// Runs an always-failing operation through the policy and returns how many times it ran.
		// A retryable failure runs MaxAttempts times; a non-retryable one runs exactly once.
		static int RunCount(YdbRetryPolicy policy, Exception ex)
		{
			var count = 0;

			Assert.Throws(Is.SameAs(ex), () => policy.Execute(() => { count++; throw ex; }));

			return count;
		}

		// Immediate-retry bucket — SessionExpired must retry (parity with SDK 0.33.0).
		[Test]
		public void Retries_ImmediateCodes([Values("BadSession", "SessionBusy", "SessionExpired")] string code)
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: true), YdbEx(code)), Is.EqualTo(MaxAttempts));
		}

		[Test]
		public void Retries_JitterCodes([Values(
			"Aborted", "Undetermined",
			"Unavailable", "ClientTransportUnknown", "ClientTransportUnavailable",
			"Overloaded", "ClientTransportResourceExhausted")] string code)
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: true), YdbEx(code)), Is.EqualTo(MaxAttempts));
		}

		[Test]
		public void DoesNotRetry_NonRetryableCode()
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: true), YdbEx("BadRequest")), Is.EqualTo(1));
		}

		// Idempotence disabled + non-transient ⇒ not retried, even for an otherwise-retryable code.
		[Test]
		public void IdempotenceDisabled_NonTransient_NotRetried()
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: false), YdbEx("Aborted", isTransient: false)), Is.EqualTo(1));
		}

		// Idempotence disabled + transient flag ⇒ retried.
		[Test]
		public void IdempotenceDisabled_Transient_Retried()
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: false), YdbEx("SessionBusy", isTransient: true)), Is.EqualTo(MaxAttempts));
		}

		// Non-YDB transient (TimeoutException) is retried via the base strategy; a plain exception is not.
		[Test]
		public void NonYdbException_Timeout_Retried()
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: true), new TimeoutException()), Is.EqualTo(MaxAttempts));
		}

		[Test]
		public void NonYdbException_Plain_NotRetried()
		{
			Assert.That(RunCount(Policy(enableRetryIdempotence: true), new InvalidOperationException()), Is.EqualTo(1));
		}
	}
}
