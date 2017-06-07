using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue358Tests : TestBase
	{
#if !MONO
		enum TestIssue358Enum
		{
			Value1,
			Value2
		}

		class TestIssue358Class
		{
			public TestIssue358Enum? MyEnum;
			public TestIssue358Enum  MyEnum2;
		}

		[Test]
		public void HasIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum != TestIssue358Enum.Value1
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.GreaterThan(0), sql);
			} 
		}

		[Test]
		public void ContainsHasIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var filter = new[] {TestIssue358Enum.Value2};

				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !!filter.Contains(p.MyEnum.Value)
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.GreaterThan(0), sql);
			} 
		}

		[Test]
		public void NoIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where p.MyEnum2 != TestIssue358Enum.Value1
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.LessThan(0), sql);
			} 
		}

		[Test]
		public void ContainsNoIsNull()
		{
			using (var db = new TestDataConnection())
			{
				var filter = new[] {TestIssue358Enum.Value2};

				var qry =
					from p in db.GetTable<TestIssue358Class>()
					where !filter.Contains(p.MyEnum2)
					select p;

				var sql = qry.ToString();

				Assert.That(sql.IndexOf("NULL"), Is.LessThan(0), sql);
			} 
		}

		static LinqDataTypes2 FixData(LinqDataTypes2 data)
		{
			data.StringValue = null;
			return data;
		}

		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => _.BigIntValue != 2),
					db.Types2.Where(_ => _.BigIntValue != 2));
			}
		}

		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => !_.BoolValue.Value),
					db.Types2.Where(_ => !_.BoolValue.Value));
			}
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(FixData,
					   Types2.Where(_ => (_.BoolValue ?? false) != true),
					db.Types2.Where(_ => _.BoolValue != true));
			}
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new Int64?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue)),
					db.Types2.Where(_ => !bigintFilter.Contains(_.BigIntValue)));
			}
		}

		[Test, DataContextSource]
		public void Test5(string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => !boolFilter.  Contains(_.BoolValue)),
					db.Types2.Where(_ => !boolFilter.  Contains(_.BoolValue)));
			}
		}

		[Test, DataContextSource]
		public void Test6(string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new Int64?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == false),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == false));
			}
		}

		[Test, DataContextSource]
		public void Test7(string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == false),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == false));
			}
		}

		[Test, DataContextSource]
		public void Test8(string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new Int64?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) != true),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) != true));
			}
		}

		[Test, DataContextSource]
		public void Test9(string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) != true),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) != true));
			}
		}

		[Test, DataContextSource]
		public void Test81(string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new Int64?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue)),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue)));
			}
		}

		[Test, DataContextSource]
		public void Test91(string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue)),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue)));
			}
		}

		[Test, DataContextSource]
		public void Test82(string context)
		{
			using (var db = GetDataContext(context))
			{
				var bigintFilter = new Int64?[] {2};

				AreEqual(FixData,
					   Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == true),
					db.Types2.Where(_ => bigintFilter.Contains(_.BigIntValue) == true));
			}
		}

		[Test, DataContextSource]
		public void Test92(string context)
		{
			using (var db = GetDataContext(context))
			{
				var boolFilter   = new bool? [] {true};

				AreEqual(FixData,
					   Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == true),
					db.Types2.Where(_ => boolFilter.  Contains(_.BoolValue) == true));
			}
		}
#endif
	}
}
