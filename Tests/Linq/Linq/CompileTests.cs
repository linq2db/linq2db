using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class CompileTests : TestBase
	{
		[Test]
		public void CompiledTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, string n1, int n2) =>
				n1 + n2);

			using (var db = GetDataContext(context))
			{
				Assert.AreEqual("11", query(db, "1", 1));
				Assert.AreEqual("22", query(db, "2", 2));
			}
		}

		[Test]
		public void CompiledTest2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).Take(n));

			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(1, query(db, 1).ToList().Count());
				Assert.AreEqual(2, query(db, 2).ToList().Count());
			}
		}

		[Test]
		public void CompiledTest3([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.GetTable<Child>().Where(c => c.ParentID == n).Take(n));

			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(1, query(db, 1).ToList().Count());
				Assert.AreEqual(2, query(db, 2).ToList().Count());
			}
		}

		[Test]
		public void CompiledTest4([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int[] n) =>
				db.GetTable<Child>().Where(c => n.Contains(c.ParentID)));

			using (var db = GetDataContext(context))
				Assert.AreEqual(3, query(db, new[] { 1, 2 }).ToList().Count());
		}

		[Test]
		public void CompiledTest5([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, object[] ps) =>
				db.Parent.Where(p => p.ParentID == (int)ps[0] && p.Value1 == (int?)ps[1]));

			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(1, query(db, new object[] { 1, 1    }).ToList().Count());
				Assert.AreEqual(1, query(db, new object[] { 2, null }).ToList().Count());
			}
		}

		[Test]
		public void CompiledTable1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db) =>
				db.Child);

			using (var db = GetDataContext(context))
			{
				var _ = query(db).ToList().Count();
			}
		}

		[Test]
		public void CompiledTable2([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db) =>
				db.GetTable<Child>());

			using (var db = GetDataContext(context))
				query(db).ToList().Count();
		}

		[Test]
		public void ConcurrentTest1([IncludeDataSources(
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
			string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.GetTable<Parent>().Where(p => p.ParentID == n).First().ParentID);

			const int count = 100;

			var threads = new Thread[count];
			var results = new int   [count, 2];

			for (var i = 0; i < count; i++)
			{
				var n = i;

				threads[i] = new Thread(() =>
				{
					using (var db = GetDataContext(context))
					{
						var id = (n % 6) + 1;
						results[n,0] = id;
						results[n,1] = query(db, id);
					}
				});
			}

			for (var i = 0; i < count; i++)
				threads[i].Start();

			for (var i = 0; i < count; i++)
				threads[i].Join();

			for (var i = 0; i < count; i++)
				Assert.AreEqual(results[i,0], results[i,1]);
		}

		[Test]
		public void ConcurrentTest2([IncludeDataSources(
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
			string context)
		{
			var threads = new Thread[100];
			var results = new int   [100,2];

			for (var i = 0; i < 100; i++)
			{
				var n = i;

				threads[i] = new Thread(() =>
				{
					using (var db = GetDataContext(context))
					{
						var id = (n % 6) + 1;
						results[n,0] = id;
						results[n,1] = db.Parent.Where(p => p.ParentID == id).First().ParentID;
					}
				});
			}

			for (var i = 0; i < 100; i++)
				threads[i].Start();

			for (var i = 0; i < 100; i++)
				threads[i].Join();

			for (var i = 0; i < 100; i++)
				Assert.AreEqual(results[i,0], results[i,1]);
		}

		[Test]
		public void ParamTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile<ITestDataContext,int,IEnumerable<Child>>((db, id) =>
				from c in db.Child
				where c.ParentID == id
				select new Child
				{
					ParentID = id,
					ChildID  = c.ChildID
				});

			using (var db = GetDataContext(context))
				Assert.AreEqual(2, query(db, 2).ToList().Count());
		}

		[Test]
		public void ElementTest1([DataSources] string context)
		{
			var query = CompiledQuery.Compile((ITestDataContext db, int n) =>
				db.Child.Where(c => c.ParentID == n).First());

			using (var db = GetDataContext(context))
			{
				Assert.AreEqual(1, query(db, 1).ParentID);
				Assert.AreEqual(2, query(db, 2).ParentID);
			}
		}

		[Test]
		public void CompiledQueryWithExpressionMethodTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = CompiledQuery.Compile((ITestDataContext xdb, int id) => Filter(xdb, id).FirstOrDefault());

				query(db, 1);
			}
		}

		[ExpressionMethod(nameof(FilterExpression))]
		public static IQueryable<Parent> Filter(ITestDataContext db, int date)
		{
			throw new NotImplementedException();
		}

		static Expression<Func<ITestDataContext, int, IQueryable<Parent>>> FilterExpression()
		{
			return (db, id) =>
				from x in db.GetTable<Parent>()
				where x.ParentID == id
				orderby x.ParentID descending
				select x;
		}
	}
}
