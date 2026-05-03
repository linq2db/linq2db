using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5398Tests : TestBase
	{
		public enum TestEnum
		{
			Val1 = 1,
			Val2 = 2,
			Val3 = 3,
		}

		[Table]
		public class TestEnumTable
		{
			[Column, PrimaryKey] public int  Id    { get; set; }
			[Column]             public int? Value { get; set; }
		}

		[Test]
		public void CastNullableIntToEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = 3 },
				new TestEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((TestEnum)i.Value!).In(TestEnum.Val1))
				.Select(i => i.Id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((TestEnum)i.Value!).In(TestEnum.Val1))
				.Select(i => i.Id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test]
		public void CastNullableIntToEnumWithInMultipleValues([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = 3 },
				new TestEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((TestEnum)i.Value!).In(TestEnum.Val1, TestEnum.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((TestEnum)i.Value!).In(TestEnum.Val1, TestEnum.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test]
		public void CastIntToEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = 3 },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var query = table
				.Where(i => ((TestEnum)i.Value!).In(TestEnum.Val1, TestEnum.Val3))
				.Select(i => i.Id)
				.OrderBy(id => id);

			AssertQuery(query);
		}

		[Test]
		public void CastNullableIntToNullableEnumWithIn([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = 3 },
				new TestEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((TestEnum?)i.Value).In(TestEnum.Val1))
				.Select(i => i.Id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((TestEnum?)i.Value).In(TestEnum.Val1))
				.Select(i => i.Id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test]
		public void CastNullableIntToNullableEnumWithInMultipleValues([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = 3 },
				new TestEnumTable { Id = 4, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((TestEnum?)i.Value).In(TestEnum.Val1, TestEnum.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value != null && ((TestEnum?)i.Value).In(TestEnum.Val1, TestEnum.Val2))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}

		[Test]
		public void CastNullableIntToNullableEnumWithInNull([DataSources] string context)
		{
			var testData = new[]
			{
				new TestEnumTable { Id = 1, Value = 1 },
				new TestEnumTable { Id = 2, Value = 2 },
				new TestEnumTable { Id = 3, Value = null },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var actual = table
				.Where(i => ((TestEnum?)i.Value).In(TestEnum.Val1, null))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			var expected = testData
				.Where(i => i.Value == null || ((TestEnum?)i.Value).In(TestEnum.Val1, null))
				.Select(i => i.Id)
				.OrderBy(id => id)
				.ToList();

			AreEqual(actual, expected);
		}
	}
}
