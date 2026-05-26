// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.ClickHouse;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class ClickHouseTests
	{
		[Test]
		public void JoinOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinOuterHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT OUTER JOIN"));
		}

		[Test]
		public void JoinSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT SEMI JOIN"));
		}

		[Test]
		public void JoinAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAntiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ANTI JOIN"));
		}

		[Test]
		public void JoinAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAnyHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ANY JOIN"));
		}

		[Test]
		public void JoinAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ALL JOIN"));
		}

		[Test]
		public void JoinGlobalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT JOIN"));
		}

		[Test]
		public void JoinGlobalOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalOuterHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT OUTER JOIN"));
		}

		[Test]
		public void JoinGlobalSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT SEMI JOIN"));
		}

		[Test]
		public void JoinGlobalAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalAntiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT ANTI JOIN"));
		}

		[Test]
		public void JoinGlobalAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalAnyHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT ANY JOIN"));
		}

		[Test]
		public void JoinGlobalAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalAllHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL LEFT ALL JOIN"));
		}

	}
}
