using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Linq.Builder;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class CteMaterializedTests : TestBase
	{
		[Test]
		public void AsCte_Builder_NullCallback_Throws(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var source = db.GetTable<Child>();
			Action<ICteBuilder>? builder = null;

			Action act = () => source.AsCte(builder!);
			act.ShouldThrow<ArgumentNullException>();
		}

		[Test]
		public void AsCte_Builder_HasName_Null_LeavesNameUnset(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// HasName(null) documents that the CTE keeps its auto-generated name — the
			// generator produces an identifier matching "CTE_<digit>+".
			using var db = GetDataContext(context);

			var cte = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.HasName(null));
			var sql = (from p in db.Parent join c in cte on p.ParentID equals c.ParentID select p).ToSqlQuery().Sql;

			System.Text.RegularExpressions.Regex.IsMatch(sql, @"\bCTE_\d+\b").ShouldBeTrue(sql);
		}

		[Test]
		public void AsCte_Builder_HasName_Empty_LeavesNameUnset(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// HasName("") is documented to behave the same as HasName(null).
			using var db = GetDataContext(context);

			var cte = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.HasName(string.Empty));
			var sql = (from p in db.Parent join c in cte on p.ParentID equals c.ParentID select p).ToSqlQuery().Sql;

			System.Text.RegularExpressions.Regex.IsMatch(sql, @"\bCTE_\d+\b").ShouldBeTrue(sql);
		}

		[Test]
		public void IsMaterialized_OnCustomCteBuilder_Throws()
		{
			// Guards MAJ002: a hand-written ICteBuilder that doesn't implement
			// IAnnotatableBuilderInternal must surface NotSupportedException, not
			// InvalidCastException.
			var custom = new CustomCteBuilder();

			Action act = () => custom.IsMaterialized();
			act.ShouldThrow<NotSupportedException>();
		}

		sealed class CustomCteBuilder : ICteBuilder
		{
			public ICteBuilder HasName(string? name) => this;
		}

		[Test]
		public void AsCte_Materialized_EmitsKeyword(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized());
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("AS MATERIALIZED");

			// Execution verifies the emitted SQL is actually valid on the DB engine.
			query.ToList();
		}

		[Test]
		public void AsCte_NotMaterialized_EmitsKeyword(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized(false));
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("AS NOT MATERIALIZED");

			query.ToList();
		}

		[Test]
		public void AsCte_NotMaterialized_ClickHouse_FallsBackToPlainAs(
			[IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			// ClickHouse has no `NOT MATERIALIZED` keyword — CTEs are non-materialized by default.
			// The builder silently drops the hint and emits plain `AS`.
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized(false));
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldNotContain("NOT MATERIALIZED");

			query.ToList();
		}

		[Test]
		public void AsCte_Materialized_UnsupportedProvider_IgnoredSilently(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized());
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldNotContain("MATERIALIZED");

			query.ToList();
		}

		[Test]
		public void AsCte_Builder_InsideSelectMany_Works(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			// When AsCte appears inside a SelectMany lambda, the compiler captures the
			// whole call as an expression tree — the public AsCte(Action<ICteBuilder>)
			// method is never executed directly and must be desugared during exposure.
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent
				from c in db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized())
				where p.ParentID == c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("AS MATERIALIZED");

			query.ToList();
		}

		[Test]
		public void AsCte_Builder_HasName_SetsCteName(
			[IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.HasName("CustomName"));
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("CustomName");

			query.ToList();
		}

		[Test]
		public void AsCte_Builder_Empty_MatchesBaseline(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			// An empty builder callback must produce the same SQL as the parameterless overload —
			// no annotations emitted, no name override, no hint keyword.
			using var db = GetDataContext(context);

			var baseline = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte();
			var withEmpty = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(_ => { });

			var baselineSql = (from p in db.Parent join c in baseline  on p.ParentID equals c.ParentID select p).ToSqlQuery().Sql;
			var emptySql    = (from p in db.Parent join c in withEmpty on p.ParentID equals c.ParentID select p).ToSqlQuery().Sql;

			emptySql.ShouldBe(baselineSql);
			emptySql.ShouldNotContain("MATERIALIZED");
		}

		[Test]
		public void AsCte_Builder_IsMaterialized_Overwrite(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			// Repeated IsMaterialized calls overwrite — the last one wins.
			using var db = GetDataContext(context);

			var cte   = db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized().IsMaterialized(false));
			var query =
				from p in db.Parent
				join c in cte on p.ParentID equals c.ParentID
				select p;

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldContain("AS NOT MATERIALIZED");

			query.ToList();
		}

		[Test]
		public void AsCte_DifferentHints_ProduceDistinctCacheEntries(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Regression test: distinct builder configurations must NOT collide in the query
			// cache. Before IExpressionCacheKey was wired through, the container was stripped
			// out of the cache key and the second query served the first's compiled plan.
			using var db = GetDataContext(context);

			var q1 = (from p in db.Parent
			          join c in db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.HasName("FirstName"))
			                  on p.ParentID equals c.ParentID
			          select p).ToSqlQuery().Sql;

			var q2 = (from p in db.Parent
			          join c in db.GetTable<Child>().Where(c => c.ParentID > 1).AsCte(b => b.IsMaterialized(false))
			                  on p.ParentID equals c.ParentID
			          select p).ToSqlQuery().Sql;

			q1.ShouldContain("FirstName");
			q1.ShouldNotContain("MATERIALIZED");

			q2.ShouldNotContain("FirstName");
			q2.ShouldContain("AS NOT MATERIALIZED");
		}
	}
}
