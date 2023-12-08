using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public partial class PostgreSQLTests : TestBase
	{
		[Test]
		public void HintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context,
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

			Assert.That(LastQuery, Contains.Substring($"\n{hint}").Using(StringComparison.Ordinal));
		}

		[Test]
		public void SubQueryHintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context,
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

			Assert.That(LastQuery, Contains.Substring($"\n\t\t{hint}").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring("\nFOR SHARE").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring("\nFOR KEY SHARE").Using(StringComparison.Ordinal));
		}

		[Test]
		public void SubQueryHintTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in
						(
							from p in
								(
									from p in db.Parent
									join c in db.Child
										.SubQueryHint(PostgreSQLHints.ForUpdate)
										on p.ParentID equals c.ParentID
									select p
								)
							select p
						)
						.SubQueryHint(PostgreSQLHints.ForShare)
					select p
				)
				.QueryName("aa")
				.SubQueryHint(PostgreSQLHints.ForKeyShare)
				;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("\tFOR UPDATE").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring("\nFOR SHARE").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring("\nFOR KEY SHARE").Using(StringComparison.Ordinal));
		}

		[Test]
		public void SubQueryHintTest3([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
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

			Assert.That(LastQuery, Contains.Substring($"\n\t\t\tFOR UPDATE").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring($"\nFOR SHARE").Using(StringComparison.Ordinal));
			Assert.That(LastQuery, Contains.Substring($"\nFOR KEY SHARE NOWAIT").Using(StringComparison.Ordinal));
		}

		[Test]
		public void TableHintTest([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person.TableID("Pr")
					.AsPostgreSQL()
					.SubQueryTableHint(PostgreSQLHints.ForUpdate, Sql.TableAlias("Pr"))
				where p.ID > 0
				select p;

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("FOR UPDATE OF p"));
		}

		[Test]
		public void TableHintTest2([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from p in db.Parent.TableID("Pr")
				join c in db.Child.TableID("Ch") on p.ParentID equals c.ParentID
				select p
			)
			.AsPostgreSQL()
			.SubQueryTableHint(PostgreSQLHints.ForUpdate, PostgreSQLHints.SkipLocked, Sql.TableAlias("Pr"), Sql.TableAlias("Ch"));

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("FOR UPDATE OF p, c_1 SKIP LOCKED"));
		}

		[Test]
		public void PostgreSQLUnionTest([IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)] string context)
		{
			void Test()
			{
				using var db = GetDataContext(context);

				var q =
						(
							from p in db.Parent.TableID("cc")
							select p
						)
						.AsPostgreSQL()
						.ForShareHint()
						.Union
						(
							from c in db.Child
							select c.Parent
						)
						.Union
						(
							from p in db.Parent
							from c in db.Child.TableID("pp")
								.AsPostgreSQL()
								.ForShareHint()
							select p
						)
						.AsPostgreSQL()
						.ForShareHint()
					;

				_ = q.ToList();
			}

			Assert.That(Test, Throws.Exception.With.Message.Contains("FOR SHARE is not allowed with UNION"));

			Assert.That(LastQuery, Should.Contain(
				"FOR SHARE",
				")",
				"UNION",
				"UNION",
				"FOR SHARE",
				")",
				"FOR SHARE",
				")"));
		}
	}
}
