using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests.Tools
{
	public static class NUnitAssert
	{
		public static void AreEqual(object? expected, object? actual)
		{
			Assert.AreEqual(expected, actual);
		}

		public static void IsTrue(bool condition)
		{
			Assert.IsTrue(condition);
		}

		public static void IsTrue(bool? condition)
		{
			Assert.IsTrue(condition);
		}

		public static void IsNull(object? anObject)
		{
			Assert.IsNull(anObject);
		}

		public static void IsNotNull(object? anObject)
		{
			Assert.IsNotNull(anObject);
		}

		public static void ThatIsLessThan<T>(T actual, object expected)
		{
			Assert.That(actual, Is.LessThan(expected));
		}

		public static void ThatIsGreaterThan<T>(T actual, object expected)
		{
			Assert.That(actual, Is.GreaterThan(expected));
		}

		/// <summary>
		/// Verifies that a delegate throws any exception when called. The returned exception may be <see
		/// langword="null"/> when inside a multiple assert block.
		/// </summary>
		/// <param name="code">A TestDelegate</param>
		public static Exception? ThrowsAny(TestDelegate code, params Type?[]? exceptions)
		{
			try
			{
				code();

				Assert.Fail("Expected code to throw an exception, but it ran without exceptions.");

				// above does not return, but we need to satisfy the flow-analyzer
				return default;
			}
			catch (Exception ex)
			{
				if (exceptions != null)
					Assert.That(ex.GetType(), Is.AnyOf(exceptions));

				return ex;
			}
		}

		/// <summary>
		/// Verifies that a delegate throws any exception when called. The returned exception may be <see
		/// langword="null"/> when inside a multiple assert block.
		/// </summary>
		/// <param name="code">A TestDelegate</param>
		/// <param name="message">The message that will be displayed on failure</param>
		/// <param name="args">Arguments to be used in formatting the message</param>
		public static async Task<Exception?> ThrowsAnyAsync(AsyncTestDelegate code, params Type?[]? exceptions)
		{
			try
			{
				await code();

				Assert.Fail(message ?? "Expected code to throw an exception, but it ran without exceptions.", args);

				// above does not return, but we need to satisfy the flow-analyzer
				return default;
			}
			catch (Exception ex)
			{
				if (exceptions != null)
					Assert.That(ex.GetType(), Is.AnyOf(exceptions));

				return ex;
			}
		}
	}
}
