using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Reflection;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2478Tests : TestBase
	{

		public T[] AssertQuery<T>(IQueryable<T> query)
		{
			var expr = query.Expression;

			var newExpr = expr.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;
					if (mc.IsSameGenericMethod(Methods.LinqToDB.GetTable))
					{
						var newCall = TypeHelper.MakeMethodCall(Methods.Queryable.ToArray, mc);
						newCall = TypeHelper.MakeMethodCall(Methods.Enumerable.AsQueryable, newCall);
						return newCall;
					}
				}

				return e;
			})!;


			var actual = query.ToArray();

			var empty = LinqToDB.Common.Tools.CreateEmptyQuery<T>();
			T[]? expected;
			using (new DisableLogging())
			{
				expected = empty.Provider.CreateQuery<T>(newExpr).ToArray();
			}

			AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<T>());

			return actual;
		}


		[Test]
		public void CrossApplyTest([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };


				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void CrossApplyTestExt([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						select new { Count = Sql.Ext.Count().ToValue(), Sum = Sql.Ext.Sum(t.ChildID).ToValue() })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void CrossApplyTest2([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					// where p.ParentID <= 2
					from c in (from t in db.GetTable<Child>()
						where t.ParentID != p.ParentID && p.ParentID <= 2
						group t by 1
						into g
						select new { Count = g.Count(), Sum = g.Sum(_ => _.ChildID) })
					select new { p.ParentID, Count = c == null ? 0 : c.Count, Sum = c == null ? 0 : c.Sum };


				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}

		[Test]
		public void OuterApplyTest([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from p in db.GetTable<Parent>()
					from c in (from t in db.GetTable<Child>()
						where t.ParentID == p.ParentID
						group t by 1
						into g
						select new { Count = g.Count() }).DefaultIfEmpty()
					select new { p.ParentID, Count = c == null ? 0 : c.Count };

				var result = query.ToArray();
				var cnt    = query.Count();
				
				Assert.That(cnt, Is.EqualTo(result.Length));
			}
		}
	}
}
