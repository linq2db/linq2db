using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class IdentifierBuilderTests : TestBase
	{
		private static readonly object?[][] _testCases = new object?[][]
		{
			// null
			new object?[]{ null, null, true },
			new object?[]{ null, true, false },
			new object?[]{ null, false, false },
			new object?[]{ true, null, false },
			new object?[]{ false, null, false },

			// bool
			new object?[]{ true, true, true },
			new object?[]{ false, false, true },
			new object?[]{ false, true, false },
			new object?[]{ true, false, false },

			// string
			new object?[]{ "one", "one", true },
			new object?[]{ "one", "two", false },

			// int
			new object?[]{ 1, 1, true },
			new object?[]{ -1, 2, false },

			// delegate
			new object?[]{ (Delegate)Method1, (Delegate)Method1, true },
			new object?[]{ (Delegate)Method1, (Delegate)Method2, false },
			// !!! false as C# compiler generates two methods currently. could change in future
			new object?[]{ () => Method1(), () => Method1(), false },
			new object?[]{ () => Method1(), () => Method2(), false },

			// collections
			new object?[]{ new object[] { true, 1, "3" }, new object[] { true, 1, "3" }, true },
			new object?[]{ new object[] { true, 1, "3" }, new object[] { true, 1, false }, false },

			// Type
			new object?[]{ typeof(string), typeof(string), true },
			new object?[]{ typeof(string), typeof(object), false },

			// Expression
			new object?[]{ Expression.Constant(1), Expression.Constant(1), true },
			new object?[]{ Expression.Constant(1), Expression.Constant(2), false },
		};

		private static void Method1()
		{
		}

		private static void Method2()
		{
		}

		[TestCaseSource(nameof(_testCases))]
		public void TestEquality(object? value1, object? value2, bool equals)
		{
			using var ib1 = new IdentifierBuilder();
			using var ib2 = new IdentifierBuilder();

			ib1.Add(value1);
			ib2.Add(value2);

			Assert.That(ib1.CreateID() == ib2.CreateID(), Is.EqualTo(equals));
		}
	}
}
