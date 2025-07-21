using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue358Tests : TestBase
	{
		enum TestIssue358Enum
		{
			Value1,
			Value2
		}

		sealed class TestIssue358Class
		{
			public TestIssue358Enum? MyEnum;
			public TestIssue358Enum  MyEnum2;
		}

		[Test]
		public void HasIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestIssue358Class>();

			var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum != TestIssue358Enum.Value1
					select p;

			Assert.That(qry.ToSqlQuery().Sql, Does.Contain("NULL"));

			qry.ToArray();
		}

		[Test]
		public void ContainsDoesNotHaveIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestIssue358Class>();

			var filter = new[] {TestIssue358Enum.Value2};

			var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !!filter.Contains(p.MyEnum!.Value)
					select p;

			Assert.That(qry.ToSqlQuery().Sql, Does.Not.Contain("NULL"));

			qry.ToArray();
		}

		[Test]
		public void NoIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestIssue358Class>();

			var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum2 != TestIssue358Enum.Value1
					select p;

			Assert.That(qry.ToSqlQuery().Sql, Does.Not.Contain("NULL"));

			qry.ToArray();
		}

		[Test]
		public void ContainsNoIsNull([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestIssue358Class>();

			var filter = new[] {TestIssue358Enum.Value2};

			var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !filter.Contains(p.MyEnum2)
					select p;

			Assert.That(qry.ToSqlQuery().Sql, Does.Not.Contain("NULL"));

			qry.ToArray();
		}

		static LinqDataTypes2 FixData(LinqDataTypes2 data)
		{
			data = data.Clone();
			data.StringValue = null;
			return data;
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => _.BigIntValue != 2),
					db.Types2.Where(_ => _.BigIntValue != 2));
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => !_.BoolValue!.Value),
					db.Types2.Where(_ => !_.BoolValue!.Value));
			}
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => (_.BoolValue ?? false) != true),
					db.Types2.Where(_ => _.BoolValue != true));
			}
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new long?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue)),
					db.Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue)));
			}
		}

		[Test]
		public void Test4WithoutComparasionNullCheck([DataSources] string context)
		{
			using var db = GetDataContext(context, o => o.UseCompareNulls(CompareNulls.LikeSql));
			var bigintFilter = new long?[] {2};

			AreEqual(FixData,
				   Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue) && _.BigIntValue != null),
				db.Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue)));
		}

		[Test]
		public void Test5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter = new bool?[] {true};

				AreEqual(FixData,
					   Types2.Where(_ => !boolFilter.  Contains(_.BoolValue)),
					db.Types2.Where(_ => !boolFilter.  Contains(_.BoolValue)));
			}
		}

		[Test]
		public void Test6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new long?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == false),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == false));
			}
		}

		[Test]
		public void Test7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter = new bool?[] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == false),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == false));
			}
		}

		[Test]
		public void Test8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new long?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) != true),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) != true));
			}
		}

		[Test]
		public void Test9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) != true),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) != true));
			}
		}

		[Test]
		public void Test81([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new long?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue)),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue)));
			}
		}

		[Test]
		public void Test91([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue)),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue)));
			}
		}

		[Test]
		public void Test82([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new long?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == true),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == true));
			}
		}

		[Test]
		public void Test92([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == true),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == true));
			}
		}
	}
}
