using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public partial class ParameterTests : TestBase
	{
		[Test]
		public void InlineParameter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.InlineParameters = true;

				var id = 1;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id);
				id++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}

		[Test]
		public void TestQueryCacheWithNullParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				int? id = null;
				Assert.AreEqual(0, db.Person.Where(_ => _.ID == id).Count());

				id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());
			}
		}

		[Test]
		public void TestOptimizingParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id || _.ID <= id || _.ID == id).Count());
			}
		}

		[Test]
		public void CharAsSqlParameter1(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "0 \x0 ' 0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter2(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0 \x0 ' \x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter3(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				ProviderName.SQLiteMS,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter4([DataSources] string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x1-\x2-\x3";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter5(
			[DataSources(
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = '\x0';
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		class AllTypes
		{
			public decimal DecimalDataType;
			public byte[]  BinaryDataType;
			public byte[]  VarBinaryDataType;
			[Column(DataType = DataType.VarChar)]
			public string  VarcharDataType;
		}

		// Excluded providers inline such parameter
		[Test]
		public void ExposeSqlDecimalParameter([DataSources(false, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = 123.456m;
				var sql = db.GetTable<AllTypes>().Where(t => t.DecimalDataType == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(6,3)"));
			}
		}

		// DB2: see DB2SqlOptimizer.SetQueryParameter - binary parameters inlined for DB2
		[Test]
		public void ExposeSqlBinaryParameter([DataSources(false, ProviderName.DB2)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = new byte[] { 0, 1, 2 };
				var sql = db.GetTable<AllTypes>().Where(t => t.BinaryDataType == p).ToString();

				Console.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("Blob").Or.Contains("(8000)"));
			}
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var dt = DateTime.Now;

				if (context.Contains("Informix"))
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

				var _ = db.Types.Where(t => t.DateTimeValue == Sql.ToSql(dt)).ToList();
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				int id1 = 1, id2 = 10000;

				var parent1 = db.Parent.OrderBy(p => p.ParentID).FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2);
				id1++;
				var parent2 = db.Parent.OrderBy(p => p.ParentID).FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2);

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}


		static class AdditionalSql
		{
			[Sql.Expression("(({2} * ({1} - {0}) / {2}) * {0})", ServerSideOnly = true)]
			public static int Operation(int item1, int item2, int item3)
			{
				return (item3 * (item2 - item1) / item3) * item1;
			}

		}

		[Test]
		public void TestPositionedParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var x3  = 3;
				var y10 = 10;
				var z2  = 2;

				var query = from child in db.Child
					select new
					{
						Value1 = Sql.AsSql(AdditionalSql.Operation(child.ChildID,
							AdditionalSql.Operation(z2, y10, AdditionalSql.Operation(z2, y10, x3)),
							AdditionalSql.Operation(z2, y10, x3)))
					};

				var expected = from child in Child
					select new
					{
						Value1 = AdditionalSql.Operation(child.ChildID,
							AdditionalSql.Operation(z2, y10, AdditionalSql.Operation(z2, y10, x3)),
							AdditionalSql.Operation(z2, y10, x3))
					};

				AreEqual(expected, query);
			}
		}

		[Test]
		public void TestQueryableCall([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildren(db).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered(db, c => c.ChildID != 5).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParametersWorkaround([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered(db, ChildFilter).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		// sequence evaluation fails in GetChildrenFiltered2
		[ActiveIssue("Unable to cast object of type 'System.Linq.Expressions.FieldExpression' to type 'System.Linq.Expressions.LambdaExpression'.")]
		[Test]
		public void TestQueryableCallWithParametersWorkaround2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered2(db, ChildFilter).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallMustFail([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// we use external parameter p in GetChildrenFiltered parameter expression
				// Sequence 'GetChildrenFiltered(value(Tests.Linq.ParameterTests+<>c__DisplayClass18_0).db, c => (c.ChildID != p.ParentID))' cannot be converted to SQL.
				Assert.Throws<LinqException>(()
					=> db.Parent.Where(p => GetChildrenFiltered(db, c => c.ChildID != p.ParentID).Select(c => c.ParentID).Contains(p.ParentID)).ToList());
			}
		}

		private static bool ChildFilter(Model.Child c) => c.ChildID != 5;

		private static IQueryable<Model.Child> GetChildren(Model.ITestDataContext db)
		{
			return db.Child;
		}

		private static IQueryable<Model.Child> GetChildrenFiltered(Model.ITestDataContext db, Func<Model.Child, bool> filter)
		{
			// looks strange, but it's just to make testcase work
			var list = db.Child.Where(filter).Select(r => r.ChildID).ToList();
			return db.Child.Where(c => list.Contains(c.ChildID));
		}

		private static IQueryable<Model.Child> GetChildrenFiltered2(Model.ITestDataContext db, Func<Model.Child, bool> filter)
		{
			var list = db.Child.ToList();
			return db.Child.Where(c => list.Where(filter).Select(r => r.ChildID).Contains(c.ChildID));
		}
	}
}
