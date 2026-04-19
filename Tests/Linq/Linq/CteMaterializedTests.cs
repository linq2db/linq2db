using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class CteMaterializedTests : TestBase
	{
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
	}
}
