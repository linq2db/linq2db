// The test double below is declared with the same full name as the real
// Ydb.Sdk.Ado.YdbException so YdbTransientExceptionDetector's name-based reflection
// binds to it; the real type's constructors are internal. CS0436 ("type conflicts
// with the imported type — using the source type") is exactly that, by design.
#pragma warning disable CS0436

using System;

using LinqToDB.Internal.DataProvider.Ydb;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class YdbTransientExceptionDetectorTests : TestBase
	{
		static Exception MakeYdbException(string code, bool isTransient)
			=> new global::Ydb.Sdk.Ado.YdbException(code, isTransient, $"YDB error: {code}");

		[Test]
		public void TryGetYdbException_TopLevel()
		{
			var ex = MakeYdbException("Aborted", isTransient: true);

			Assert.That(YdbTransientExceptionDetector.TryGetYdbException(ex, out var found), Is.True);
			Assert.That(found, Is.SameAs(ex));
		}

		[Test]
		public void TryGetYdbException_Nested()
		{
			var inner = MakeYdbException("Aborted", isTransient: true);
			var outer = new InvalidOperationException("wrap", inner);

			Assert.That(YdbTransientExceptionDetector.TryGetYdbException(outer, out var found), Is.True);
			Assert.That(found, Is.SameAs(inner));
		}

		[Test]
		public void TryGetYdbException_NonYdb()
		{
			Assert.That(YdbTransientExceptionDetector.TryGetYdbException(new InvalidOperationException(), out var found), Is.False);
			Assert.That(found, Is.Null);
		}

		[Test]
		public void TryGetCodeAndTransient_ReadsProperties()
		{
			var ex = MakeYdbException("BadSession", isTransient: true);

			Assert.That(YdbTransientExceptionDetector.TryGetCodeAndTransient(ex, out var code, out var isTransient), Is.True);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(code,        Is.EqualTo("BadSession"));
				Assert.That(isTransient, Is.True);
			}
		}

		// Idempotence disabled: only the IsTransient flag is honoured, the code list is ignored.
		[Test]
		public void ShouldRetryOn_TransientFlag_WhenIdempotenceDisabled([Values] bool isTransient)
		{
			var ex = MakeYdbException("GenericError", isTransient);

			Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(ex, enableRetryIdempotence: false), Is.EqualTo(isTransient));
		}

		// Idempotence enabled: these codes are retried even with IsTransient == false;
		// idempotence disabled: the same codes are not retried (flag-only).
		[Test]
		public void ShouldRetryOn_IdempotentCodes([Values(
			"BadSession", "SessionBusy", "SessionExpired", "Aborted", "Undetermined",
			"Unavailable", "ClientTransportUnknown", "ClientTransportUnavailable",
			"Overloaded", "ClientTransportResourceExhausted")] string code)
		{
			var ex = MakeYdbException(code, isTransient: false);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(ex, enableRetryIdempotence: true),  Is.True);
				Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(ex, enableRetryIdempotence: false), Is.False);
			}
		}

		[Test]
		public void ShouldRetryOn_NonIdempotentCode_NotRetried()
		{
			var ex = MakeYdbException("BadRequest", isTransient: false);

			Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(ex, enableRetryIdempotence: true), Is.False);
		}

		[Test]
		public void ShouldRetryOn_TimeoutException_Retried()
		{
			Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(new TimeoutException(), enableRetryIdempotence: false), Is.True);
		}

		[Test]
		public void ShouldRetryOn_PlainException_NotRetried()
		{
			Assert.That(YdbTransientExceptionDetector.ShouldRetryOn(new InvalidOperationException(), enableRetryIdempotence: true), Is.False);
		}
	}
}

namespace Ydb.Sdk.Ado
{
	/// <summary>
	/// Test double mimicking <c>Ydb.Sdk.Ado.YdbException</c>'s shape — the real type's
	/// constructors are <c>internal</c>, so it can't be instantiated from tests.
	/// <see cref="LinqToDB.Internal.DataProvider.Ydb.YdbTransientExceptionDetector"/> binds by
	/// full type name plus the <c>IsTransient</c>/<c>Code</c> members via reflection, so this
	/// stand-in (same full name) drives it without a live connection.
	/// </summary>
	internal sealed class YdbException : Exception
	{
		public YdbException(string code, bool isTransient, string message) : base(message)
		{
			Code        = code;
			IsTransient = isTransient;
		}

		public bool   IsTransient { get; }
		public string Code        { get; }
	}
}
