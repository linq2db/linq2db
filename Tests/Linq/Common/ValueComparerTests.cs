using FluentAssertions;

using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ValueComparerTests
	{
		sealed class TestObject
		{
			public int Id { get; set; }
		}

		interface ITestObject
		{
			public int Id { get; set; }
		}

		[Test]
		public void TestStringNullComparison()
		{
			var comparer = ValueComparer.GetDefaultValueComparer<string>(true);

			string? str1 = null;
			string? str2 = null;

			comparer.GetHashCode(str1).Should().Be(0);
			comparer.Equals(str1, str2).Should().BeTrue();
		}

		[Test]
		public void TestObjectNullComparison()
		{
			var comparer = ValueComparer.GetDefaultValueComparer<TestObject>(true);

			TestObject? obj1 = null;
			TestObject? obj2 = null;

			comparer.GetHashCode(obj1).Should().Be(0);
			comparer.Equals(obj1, obj2).Should().BeTrue();
		}

		[Test]
		public void TestInterfaceNullComparison()
		{
			var comparer = ValueComparer.GetDefaultValueComparer<TestObject>(true);

			ITestObject? obj1 = null;
			ITestObject? obj2 = null;

			comparer.GetHashCode(obj1).Should().Be(0);
			comparer.Equals(obj1, obj2).Should().BeTrue();
		}

		[Test]
		public void TestNullableNullComparison()
		{
			var comparer = ValueComparer.GetDefaultValueComparer<int?>(true);

			int? obj1 = null;
			int? obj2 = null;

			comparer.GetHashCode(obj1).Should().Be(0);
			comparer.Equals(obj1, obj2).Should().BeTrue();
		}

	}
}
