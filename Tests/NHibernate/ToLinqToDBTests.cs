using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises query interception — a native NHibernate <see cref="ISession"/> query (<c>session.Query&lt;T&gt;()</c>)
	/// routed through linq2db via <c>ToLinqToDB()</c>, which recovers the session from the NHibernate query provider
	/// (<c>GetCurrentContext</c>) and rebuilds the expression on the linq2db pipeline. Runs once per enabled provider.
	/// </summary>
	[TestFixture]
	public class ToLinqToDBTests : NHTestBase
	{
		[OneTimeTearDown]
		public void TearDown() => DisposeFactories();

		[Test]
		public void NativeQuery_RoutesThroughLinqToDB(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			var names = session.Query<Customer>()
				.Where(c => c.CustomerId == "ALFKI")
				.ToLinqToDB()
				.Select(c => c.CompanyName)
				.ToList();

			names.ShouldHaveSingleItem();
			names[0].ShouldBe("Alfreds Futterkiste");
		}

		[Test]
		public async Task NativeQuery_RoutesThroughLinqToDB_Async(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			// ToListAsyncLinqToDB routes the native NHibernate query through linq2db and executes it
			// truly async via linq2db's AsyncExtensions (an unambiguous name vs NHibernate's own *Async).
			var names = await session.Query<Customer>()
				.Where (c => c.CustomerId == "ALFKI")
				.Select(c => c.CompanyName)
				.ToListAsyncLinqToDB();

			names.ShouldHaveSingleItem();
			names[0].ShouldBe("Alfreds Futterkiste");
		}

		[Test]
		public async Task NativeQuery_CoreAsyncRoutesThroughAdapter(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			// Calling linq2db's AsyncExtensions on a NATIVE NHibernate query routes through the wired
			// LinqToDBExtensionsAdapter, which delegates to NHibernate's own async (ToListAsync).
			var names = await LinqToDB.Async.AsyncExtensions.ToListAsync(
				session.Query<Customer>()
					.Where (c => c.CustomerId == "ALFKI")
					.Select(c => c.CompanyName));

			names.ShouldHaveSingleItem();
			names[0].ShouldBe("Alfreds Futterkiste");
		}

		[Test]
		public async Task NativeQuery_ToListAsyncNH_RunsViaNHibernate(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			// ToListAsyncNH is the unambiguous wrapper over NHibernate's own async LINQ extension.
			var names = await session.Query<Customer>()
				.Where (c => c.CustomerId == "ALFKI")
				.Select(c => c.CompanyName)
				.ToListAsyncNH();

			names.ShouldHaveSingleItem();
			names[0].ShouldBe("Alfreds Futterkiste");
		}
	}
}
