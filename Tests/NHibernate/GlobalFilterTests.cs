using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.GlobalFilter;

using NHibernate;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises the global-filter bridge: NHibernate filters enabled on the session (raw SQL-fragment
	/// conditions) are applied to linq2db queries over the attached session, with column references
	/// re-expressed as member accesses so linq2db qualifies them itself.
	/// </summary>
	[TestFixture]
	public class GlobalFilterTests : NHTestBase
	{
		// tenant 1: "A" (active), "B" (deleted); tenant 2: "C" (active).
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// linq2db command runs inside the NHibernate transaction (the attached connection shares it via UseTransaction).
			session.GetTable<Document>().Delete();

			session.Save(new Document { Title = "A", IsDeleted = 0, TenantId = 1 });
			session.Save(new Document { Title = "B", IsDeleted = 1, TenantId = 1 });
			session.Save(new Document { Title = "C", IsDeleted = 0, TenantId = 2 });

			tx.Commit();
		}

		static string[] Titles(ISession session) =>
			session.GetTable<Document>().Select(d => d.Title).OrderBy(t => t).ToArray();

		[Test]
		public void NoFilters_ReturnsAll(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			Titles(session).ShouldBe(new[] { "A", "B", "C" });
		}

		[Test]
		public void SoftDelete_FiltersDeletedRows(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			session.EnableFilter("softDelete");

			Titles(session).ShouldBe(new[] { "A", "C" });
		}

		[Test]
		public void Tenant_FiltersByParameter(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			session.EnableFilter("tenant").SetParameter("tenantId", 1);

			// tenant 1 only (soft-delete not enabled, so the deleted "B" is still returned).
			Titles(session).ShouldBe(new[] { "A", "B" });
		}

		[Test]
		public void BothFilters_Combine(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			session.EnableFilter("softDelete");
			session.EnableFilter("tenant").SetParameter("tenantId", 1);

			// tenant 1 AND not deleted.
			Titles(session).ShouldBe(new[] { "A" });
		}

		[Test]
		public void FilterAppliesAfterUnfilteredQuery_NotServedFromCache(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			// Query with no filter first, which populates linq2db's query cache with a no-filter shape.
			using (var unfiltered = sf.OpenSession())
				Titles(unfiltered).ShouldBe(new[] { "A", "B", "C" });

			// The same factory with the soft-delete filter enabled must still filter, not reuse the cached shape.
			using var session = sf.OpenSession();
			session.EnableFilter("softDelete");

			Titles(session).ShouldBe(new[] { "A", "C" });
		}

		[Test]
		public void PerEntityCondition_WithEmptyDefault_IsApplied(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			// ArchivedFilter has no default condition; DocumentMap supplies "is_deleted = 0" per-entity.
			session.EnableFilter("archived");

			Titles(session).ShouldBe(new[] { "A", "C" }); // deleted "B" excluded via the per-entity condition
		}

		[Test]
		public void FilterCondition_WithBracesInLiteral_BuildsCorrectSql(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using (var seed = sf.OpenSession())
			using (var tx = seed.BeginTransaction())
			{
				seed.Save(new Document { Title = "x{y}z", IsDeleted = 0, TenantId = 1 });
				tx.Commit();
			}

			using var session = sf.OpenSession();
			// Condition "Title <> 'x{y}z'": the braces must be escaped so Sql.Expr treats them as literal text,
			// otherwise query build throws a FormatException.
			session.EnableFilter("literalGuard");

			Titles(session).ShouldBe(new[] { "A", "B", "C" }); // only the 'x{y}z' row is excluded
		}

		[Test]
		public void IgnoreFilters_Bypasses(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			session.EnableFilter("softDelete");
			session.EnableFilter("tenant").SetParameter("tenantId", 1);

			var titles = session.GetTable<Document>().IgnoreFilters().Select(d => d.Title).OrderBy(t => t).ToArray();

			titles.ShouldBe(new[] { "A", "B", "C" });
		}
	}
}
