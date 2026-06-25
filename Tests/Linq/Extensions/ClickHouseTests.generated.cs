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
	public partial class ClickHouseTests
	{
		[Test]
		public void LeftJoinOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
		public void LeftJoinGlobalAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
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
#pragma warning disable CS0618 // Type or member is obsolete
		[Test]
		public void LeftJoinAllOuterHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllOuterHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT OUTER JOIN"));
		}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
		[Test]
		public void LeftJoinAllSemiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllSemiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT SEMI JOIN"));
		}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
		[Test]
		public void LeftJoinAllAntiHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllAntiHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ANTI JOIN"));
		}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
		[Test]
		public void LeftJoinAllAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllAnyHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ANY JOIN"));
		}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
		[Test]
		public void LeftJoinAllAsOfHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllAsOfHint() on c.ParentID equals p.ParentID into g
				from p in g.DefaultIfEmpty()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"LEFT ASOF JOIN"));
		}
#pragma warning restore CS0618 // Type or member is obsolete
		[Test]
		public void InnerJoinAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAnyHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER ANY JOIN"));
		}

		[Test]
		public void InnerJoinAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinAllHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"INNER ALL JOIN"));
		}

		[Test]
		public void InnerJoinGlobalHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL INNER JOIN"));
		}

		[Test]
		public void InnerJoinGlobalAnyHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalAnyHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL INNER ANY JOIN"));
		}

		[Test]
		public void InnerJoinGlobalAllHintTest([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from c in db.Child
				join p in db.Parent.AsClickHouse().JoinGlobalAllHint() on c.ParentID equals p.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"GLOBAL INNER ALL JOIN"));
		}

	}
}
