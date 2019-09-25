using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TableExpressionTests : TestBase
	{
		[Table]
		class TestClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }


			[Association(QueryExpressionMethod = nameof(OthersImpl))]
			public List<TestClass> Others { get; }

			[Association(QueryExpressionMethod = nameof(Others2Impl))]
			public List<TestClass> Others2 { get; }

			static Expression<Func<TestClass, IDataContext, IQueryable<TestClass>>> OthersImpl()
			{
				return (t, dc) => dc.GetTable<TestClass>().TableName("TestClassChild").TableGroup("A").Where(z => z.Id == t.Id).Take(10);
			}

			static Expression<Func<TestClass, IDataContext, IQueryable<TestClass>>> Others2Impl()
			{
				return (t, dc) => ChildTable(dc).Where(o2 => o2.Id == t.Id).Take(10);
			}

			private static ITable<TestClass> ChildTable(IDataContext dc)
			{
				return dc.GetTable<TestClass>().TableName("TestClassChild2").TableGroup("A").TableGroup("A2");
			}

		}

		[Test]
		public void TestGroups([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from c1 in db.GetTable<TestClass>().TableGroup("G1")
					from c2 in db.GetTable<TestClass>().TableGroup("G2").LeftJoin(c2 => c2.Id != c1.Id)
					from c3 in db.GetTable<TestClass>().LeftJoin(c3 => c3.Id != c1.Id)
					select new { c1, c2, c3 };

				var query1 = query.ApplyWith("NOLOCK", "G1");
				var str1  = query1.ToString();
				Console.WriteLine(str1);
				Assert.That(str1, Does.Contain("[c1] WITH (NOLOCK)").And.Not.Contain("[c2] WITH (NOLOCK)").And.Not.Contain("[c3] WITH (NOLOCK)"));

				var query2 = query.ApplyWithExcept("NOLOCK", "G1");
				var str2   = query2.ToString();
				Console.WriteLine(str2);
				Assert.That(str2, Does.Contain("[c2] WITH (NOLOCK)").And.Contain("[c3] WITH (NOLOCK)").And.Not.Contain("[c1] WITH (NOLOCK)"));

				var query3 = query.ApplyWithExcept("NOLOCK", "G1,G2");
				var str3   = query3.ToString();
				Console.WriteLine(str3);
				Assert.That(str3, Does.Contain("[c3] WITH (NOLOCK)").And.Not.Contain("[c1] WITH (NOLOCK)").And.Not.Contain("[c2] WITH (NOLOCK)"));
			}
		}

		[Test]
		public void TestQueryableAssociation([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from c1 in db.GetTable<TestClass>().TableGroup("G1")
					from a1 in c1.Others 
					from a2 in c1.Others2 
					select new
					{
						c1,
						a1,
						a2
					};

				var query1 = query.ApplyWith("NOLOCK", "A");
				var str1  = query1.ToString();
				Console.WriteLine(str1);

				Assert.That(str1, Does.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Not.Contain("[TestClass] [c1] WITH (NOLOCK)"));

				var query2 = query.ApplyWithExcept("NOLOCK", "A");
				var str2   = query2.ToString();
				Console.WriteLine(str2);

				Assert.That(str2, Does.Contain("[TestClass] [c1] WITH (NOLOCK)").And.Not.Contain("[TestClassChild] [z] WITH (NOLOCK)"));

				var query3 = query.ApplyWithExcept("NOLOCK", "G1");
				var str3   = query3.ToString();
				Console.WriteLine(str3);

				Assert.That(str3, Does.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Not.Contain("[TestClass] [c1] WITH (NOLOCK)"));

				var query4 = query.ApplyWith("NOLOCK");
				var str4  = query4.ToString();
				Console.WriteLine(str4);

				Assert.That(str4, Does.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Contain("[TestClass] [c1] WITH (NOLOCK)"));

				var query5 = query;
				var str5  = query5.ToString();
				Console.WriteLine(str5);

				Assert.That(str5, Does.Not.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Not.Contain("[TestClass] [c1] WITH (NOLOCK)"));

				var query6 = query.ApplyWithExcept("NOLOCK", "A2,G1");
				var str6   = query6.ToString();
				Console.WriteLine(str6);

				Assert.That(str6, Does.Not.Contain("[TestClassChild2] [o2] WITH (NOLOCK)").And.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Not.Contain("[TestClass] [c1] WITH (NOLOCK)"));

				var cteQuery = query.AsCte();
				var query7 = (from t2 in db.GetTable<TestClass>().TableGroup("G2")
					from cte in cteQuery 
					select new { t2, cte }).ApplyWith("NOLOCK");
				var str7   = query7.ToString();
				Console.WriteLine(str7);

				Assert.That(str7, Does.Contain("[TestClass] [t2] WITH (NOLOCK)").And.Not.Contain("[TestClassChild] [z] WITH (NOLOCK)").And.Not.Contain("[TestClassChild2] [o2] WITH (NOLOCK)"));

			}
		}

	}
}
