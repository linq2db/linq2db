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
		[OneTimeTearDown]
		public void TearDown() => DisposeFactories();

		// tenant 1: "A" (active), "B" (deleted); tenant 2: "C" (active).
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.CreateQuery("delete from Document").ExecuteUpdate();

			session.Save(new Document { Title = "A", IsDeleted = false, TenantId = 1 });
			session.Save(new Document { Title = "B", IsDeleted = true,  TenantId = 1 });
			session.Save(new Document { Title = "C", IsDeleted = false, TenantId = 2 });

			tx.Commit();
		}

		static string[] Titles(ISession session) =>
			session.GetTable<Document>().Select(d => d.Title).OrderBy(t => t).ToArray();

		[Test]
		public void NoFilters_ReturnsAll(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			Titles(session).ShouldBe(new[] { "A", "B", "C" });
		}

		[Test]
		public void SoftDelete_FiltersDeletedRows(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();
			session.EnableFilter("softDelete");

			Titles(session).ShouldBe(new[] { "A", "C" });
		}

		[Test]
		public void Tenant_FiltersByParameter(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
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
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
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
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
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
		public void IgnoreFilters_Bypasses(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
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
