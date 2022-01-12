// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.SqlCe;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class SqlCeTests
	{
		[Test]
		public void WithHoldLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithHoldLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (HoldLock)"));
		}

		[Test]
		public void WithHoldLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithHoldLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (HoldLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (HoldLock)"));
		}

		[Test]
		public void WithNoLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithNoLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (NoLock)"));
		}

		[Test]
		public void WithNoLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithNoLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (NoLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (NoLock)"));
		}

		[Test]
		public void WithPagLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithPagLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (PagLock)"));
		}

		[Test]
		public void WithPagLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithPagLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (PagLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (PagLock)"));
		}

		[Test]
		public void WithRowLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithRowLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (RowLock)"));
		}

		[Test]
		public void WithRowLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithRowLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (RowLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (RowLock)"));
		}

		[Test]
		public void WithTabLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithTabLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (TabLock)"));
		}

		[Test]
		public void WithTabLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithTabLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (TabLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (TabLock)"));
		}

		[Test]
		public void WithUpdLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithUpdLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (UpdLock)"));
		}

		[Test]
		public void WithUpdLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithUpdLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (UpdLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (UpdLock)"));
		}

		[Test]
		public void WithXLockTableTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent
					.AsSqlCe()
					.WithXLock()
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH (XLock)"));
		}

		[Test]
		public void WithXLockInScopeTest([IncludeDataSources(true, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsSqlCe()
			.WithXLockInScope();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("[p] WITH (XLock)"));
			Assert.That(LastQuery, Contains.Substring("[c_1] WITH (XLock)"));
		}

	}
}
