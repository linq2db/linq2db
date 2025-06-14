using System;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;

using NUnit.Framework;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class IdentifierBuilderTests : TestBase
	{
		private static readonly object?[][] _testCases = new object?[][]
		{
			// null
			[null,  null,  true],
			[null,  true,  false],
			[null,  false, false],
			[true,  null,  false],
			[false, null,  false],

			// bool
			[true,  true,  true],
			[false, false, true],
			[false, true,  false],
			[true,  false, false],

			// string
			["one", "one", true],
			["one", "two", false],

			// int
			[ 1, 1, true],
			[-1, 2, false],

			// delegate
			[(Delegate)Method1, (Delegate)Method1, true],
			[(Delegate)Method1, (Delegate)Method2, false],

			// !!! false as C# compiler generates two methods currently. could change in future
			[() => Method1(), () => Method1(), false],
			[() => Method1(), () => Method2(), false],

			// collections
			[new object[] { true, 1, "3" }, new object[] { true, 1, "3"   }, true],
			[new object[] { true, 1, "3" }, new object[] { true, 1, false }, false],
			[new object[] { true, 1, "2" }, new object[] { true, 1, false }, false],
			[new object[] { true, 1, "2" }, new object[] { true, 1, 2 },     false],

			// Type
			[typeof(string), typeof(string), true],
			[typeof(string), typeof(object), false],

			// Expression
			[Expression.Constant(1), Expression.Constant(1), true],
			[Expression.Constant(1), Expression.Constant(2), false],
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
