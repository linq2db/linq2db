using System;

using NUnit.Framework;

namespace Tests.Utils
{
	public static class NUnitAssert
	{
		public static void AreEqual(object expected, object actual)
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

		public static void IsNull(object anObject)
		{
			Assert.IsNull(anObject);
		}

		public static void IsNotNull(object anObject)
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
	}
}
