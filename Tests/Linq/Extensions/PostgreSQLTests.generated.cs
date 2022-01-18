// Generated.
//
using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;

using NUnit.Framework;

namespace Tests.Extensions
{
	partial class PostgreSQLTests
	{
		[Test]
		public void QueryHintForUpdateTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate}"));
		}

		[Test]
		public void QueryHintForUpdateTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate} OF p, c_1"));
		}

		[Test]
		public void QueryHintForUpdateNoWaitTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateNoWaitHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate} {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForUpdateNoWaitTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate} OF p, c_1 {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForUpdateSkipLockedTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateSkipLockedHint();

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate}{skipLocked}"));
		}

		[Test]
		public void QueryHintForUpdateSkipLockedTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate} OF p, c_1{skipLocked}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate} OF p, c_1"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateNoWaitTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateNoWaitHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate} {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateNoWaitTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate} OF p, c_1 {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateSkipLockedTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateSkipLockedHint();

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate}{skipLocked}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateSkipLockedTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate} OF p, c_1{skipLocked}"));
		}

		[Test]
		public void QueryHintForShareTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare}"));
		}

		[Test]
		public void QueryHintForShareTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare} OF p, c_1"));
		}

		[Test]
		public void QueryHintForShareNoWaitTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareNoWaitHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare} {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForShareNoWaitTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare} OF p, c_1 {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForShareSkipLockedTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareSkipLockedHint();

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare}{skipLocked}"));
		}

		[Test]
		public void QueryHintForShareSkipLockedTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare} OF p, c_1{skipLocked}"));
		}

		[Test]
		public void QueryHintForKeyShareTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare}"));
		}

		[Test]
		public void QueryHintForKeyShareTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare} OF p, c_1"));
		}

		[Test]
		public void QueryHintForKeyShareNoWaitTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareNoWaitHint();

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare} {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForKeyShareNoWaitTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareNoWaitHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare} OF p, c_1 {PostgreSQLHints.NoWait}"));
		}

		[Test]
		public void QueryHintForKeyShareSkipLockedTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent
				join c in db.Child on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareSkipLockedHint();

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare}{skipLocked}"));
		}

		[Test]
		public void QueryHintForKeyShareSkipLockedTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

				var skipLocked = context == ProviderName.PostgreSQL95 ? " SkipLocked" : "";
				Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare} OF p, c_1{skipLocked}"));
		}

	}
}
