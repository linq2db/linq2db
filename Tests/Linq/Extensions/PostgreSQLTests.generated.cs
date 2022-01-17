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
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForUpdateSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForUpdate} OF p, c_1 {PostgreSQLHints.SkipLocked}"));
		}

		[Test]
		public void QueryHintForNoKeyUpdateTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
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
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForNoKeyUpdateSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForNoKeyUpdate} OF p, c_1 {PostgreSQLHints.SkipLocked}"));
		}

		[Test]
		public void QueryHintForShareTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
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
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForShareSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForShare} OF p, c_1 {PostgreSQLHints.SkipLocked}"));
		}

		[Test]
		public void QueryHintForKeyShareTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
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
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.ForKeyShareSkipLockedHint(Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"{PostgreSQLHints.ForKeyShare} OF p, c_1 {PostgreSQLHints.SkipLocked}"));
		}

	}
}
