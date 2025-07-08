using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class JoinToLimitedTests : TestBase
	{
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void LeftJoinToTop([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).DefaultIfEmpty().Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).DefaultIfEmpty().Take(1)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void LeftJoinToTopWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).OrderByDescending(x => x.ChildID).DefaultIfEmpty().Take(1)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).OrderByDescending(x => x.ChildID).DefaultIfEmpty().Take(1)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		// Sybase escalates TOP 1 closure in subquery to all query
		[Test]
		public void LeftJoinLimited([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		// Sybase escalates TOP 1 closure in subquery to all query
		[Test]
		public void LeftJoinLimitedWhere([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.OrderByDescending(x => x.ChildID).Take(1).Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.OrderByDescending(x => x.ChildID).Take(1).Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void LeftJoinLimited2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1).DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void LeftJoinToDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty().Distinct()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty().Distinct()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void LeftJoinToDistinctWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Distinct()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Distinct()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void LeftJoinDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void LeftJoinDistinctWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Distinct().Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Distinct().Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void LeftJoinDistinct2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct().DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct().DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void InnerJoinToTop([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void InnerJoinToTopWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void InnerJoinLimited([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void InnerJoinLimitedWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Take(1).Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Take(1).Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, ProviderName.Firebird25, TestProvName.AllMySql57, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[Test]
		public void InnerJoinLimited2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.OrderByDescending(x => x.ChildID).Take(1)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void InnerJoinToDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void InnerJoinToDistinctWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).Distinct()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).Distinct()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void InnerJoinDistinct([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp =
					from o in Parent
					join c in Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				var act =
					from o in db.Parent
					join c in db.Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void InnerJoinDistinctWhere([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp =
					from o in Parent
					from c in Child.Distinct().Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				var act =
					from o in db.Parent
					from c in db.Child.Distinct().Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test]
		public void InnerJoinDistinct2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Distinct()
					select new { o, c };

				AreEqual(exp, act);
			}
		}
	}
}
