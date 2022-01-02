using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryExtensionPostgreSQLTests : TestBase
	{
		[Test]
		public void HintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context,
			[Values(
				PostgreSQLHints.ForUpdate,
				PostgreSQLHints.ForNoKeyUpdate,
				PostgreSQLHints.ForShare,
				PostgreSQLHints.ForKeyShare
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.SubQueryHint(hint)
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"\n{hint}"));
		}

		[Test]
		public void SubQueryHintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context,
			[Values(
				PostgreSQLHints.ForUpdate,
				PostgreSQLHints.ForNoKeyUpdate,
				PostgreSQLHints.ForShare,
				PostgreSQLHints.ForKeyShare
			)] string hint)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in
						(
							from p in
								(
									from p in db.Parent
									from c in db.Child
									where c.ParentID == p.ParentID
									select p
								)
								.SubQueryHint(hint)
								.AsSubQuery()
							where p.ParentID < -100
							select p
						)
						.SubQueryHint(PostgreSQLHints.ForShare)
					select p
				)
				.SubQueryHint(PostgreSQLHints.ForKeyShare)
				;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"\n\t\t{hint}"));
			Assert.That(LastQuery, Contains.Substring("\nFOR SHARE"));
			Assert.That(LastQuery, Contains.Substring("\nFOR KEY SHARE"));
		}

		[Test]
		public void SubQueryHintTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in
						(
							from p in
								(
									from p in db.Parent
									join c in db.Child.SubQueryHint(PostgreSQLHints.ForUpdate) on p.ParentID equals c.ParentID
									select p
								)
							select p
						)
						.SubQueryHint(PostgreSQLHints.ForShare)
					select p
				)
				.SubQueryHint(PostgreSQLHints.ForKeyShare)
				;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("\nFOR UPDATE"));
			Assert.That(LastQuery, Contains.Substring("\nFOR SHARE"));
			Assert.That(LastQuery, Contains.Substring("\nFOR KEY SHARE"));
		}

		[Test]
		public void SubQueryHintTest3([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in
						(
							from p in
								(
									from p in db.Parent
									join c in db.Child.SubQueryHint(PostgreSQLHints.ForUpdate).AsSubQuery() on p.ParentID equals c.ParentID
									select p
								)
							select p
						)
						.SubQueryHint(PostgreSQLHints.ForShare)
					select p
				)
				.SubQueryHint(PostgreSQLHints.ForKeyShare + " NOWAIT")
				;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring($"\n\t\t\tFOR UPDATE"));
			Assert.That(LastQuery, Contains.Substring($"\nFOR SHARE"));
			Assert.That(LastQuery, Contains.Substring($"\nFOR KEY SHARE NOWAIT"));
		}

		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableID("Pr")
					.SubQueryTableHint(PostgreSQLHints.ForUpdate, Sql.TableID("Pr"))
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("FOR UPDATE OF p"));
		}

		[Test]
		public void TableHintTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.SubQueryTableHint(PostgreSQLHints.ForUpdate, PostgreSQLHints.SkipLocked, Sql.TableID("Pr"), Sql.TableID("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("FOR UPDATE OF p, c_1 SKIP LOCKED"));
		}
	}
}
