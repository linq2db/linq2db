using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class JoinToLimitedTests : TestBase
	{
		[Test, DataContextSource]
		public void LeftJoinToTop(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty().Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty().Take(1)
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void LeftJoinToTopWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Take(1)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Take(1)
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		// Sybase escalates TOP 1 closure in subquery to all query
		[Test, DataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void LeftJoinLimited(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child.Take(1) on o.ParentID equals c.ParentID into cg
					from c in cg.DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		// Sybase escalates TOP 1 closure in subquery to all query
		[Test, DataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void LeftJoinLimitedWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Take(1).Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Take(1).Where(x => x.ParentID == o.ParentID).DefaultIfEmpty()
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void LeftJoinLimited2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1).DefaultIfEmpty()
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1).DefaultIfEmpty()
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void LeftJoinToDistinct(string context)
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

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void LeftJoinToDistinctWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Distinct()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).DefaultIfEmpty().Distinct()
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void LeftJoinDistinct(string context)
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

		[Test, DataContextSource]
		public void LeftJoinDistinctWhere(string context)
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

		[Test, DataContextSource]
		public void LeftJoinDistinct2(string context)
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

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinToTop(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1)
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinToTopWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).Take(1)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).Take(1)
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinLimited(string context)
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

		[Test, DataContextSource]
		public void InnerJoinLimitedWhere(string context)
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

		[Test, DataContextSource]
		public void InnerJoinLimited2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1)
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child on o.ParentID equals c.ParentID into cg
					from c in cg.Take(1)
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinToDistinct(string context)
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

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinToDistinctWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Where(x => x.ParentID == o.ParentID).Distinct()
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Where(x => x.ParentID == o.ParentID).Distinct()
					select new { o, c };

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinDistinct(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					join c in Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				var act = from o in db.Parent
					join c in db.Child.Distinct() on o.ParentID equals c.ParentID into cg
					from c in cg
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinDistinctWhere(string context)
		{
			using (var db = GetDataContext(context))
			{
				var exp = from o in Parent
					from c in Child.Distinct().Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				var act = from o in db.Parent
					from c in db.Child.Distinct().Where(x => x.ParentID == o.ParentID)
					select new { o, c };

				AreEqual(exp, act);
			}
		}

		[Test, DataContextSource]
		public void InnerJoinDistinct2(string context)
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

				if (!db.SqlProviderFlags.IsApplyJoinSupported)
					Assert.Throws<LinqToDBException>(() => AreEqual(exp, act));
				else
					AreEqual(exp, act);
			}
		}
	}
}
