using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.ManyToMany;

using NHibernate;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises many-to-many association support: a collection mapped in NHibernate through a junction table
	/// (<c>HasManyToMany</c>) is exposed to linq2db as an <see cref="LinqToDB.Mapping.AssociationAttribute"/>
	/// whose query expression joins through the junction entity, so the collection can be navigated from a
	/// linq2db query over an attached session.
	/// </summary>
	[TestFixture]
	public class ManyToManyTests : NHTestBase
	{
		// Seeds two authors and three books linked through the AuthorBook junction. Delete-first so re-runs
		// against a persisted (SQL Server) database stay deterministic.
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// linq2db commands run inside the NHibernate transaction (enlisted by the connection's interceptor).
			session.GetTable<AuthorBook>().Delete();
			session.GetTable<Book>().Delete();
			session.GetTable<Author>().Delete();

			var asimov = new Author { Name = "Asimov" };
			var clarke = new Author { Name = "Clarke" };
			session.Save(asimov);
			session.Save(clarke);

			var foundation = new Book { Title = "Foundation" };
			var robots     = new Book { Title = "I, Robot" };
			var odyssey    = new Book { Title = "2001" };
			session.Save(foundation);
			session.Save(robots);
			session.Save(odyssey);

			session.Save(new AuthorBook { AuthorId = asimov.Id, BookId = foundation.Id });
			session.Save(new AuthorBook { AuthorId = asimov.Id, BookId = robots.Id });
			session.Save(new AuthorBook { AuthorId = clarke.Id, BookId = odyssey.Id });

			tx.Commit();
		}

		[Test]
		public void ManyToMany_NavigatesThroughJunction(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			var asimovId = session.GetTable<Author>().Where(a => a.Name == "Asimov").Select(a => a.Id).First();

			// Navigate the many-to-many association from linq2db: Author.Books hops through the AuthorBook junction.
			var titles = session.GetTable<Author>()
				.Where(a => a.Id == asimovId)
				.SelectMany(a => a.Books)
				.Select(b => b.Title)
				.OrderBy(t => t)
				.ToList();

			titles.ShouldBe(new[] { "Foundation", "I, Robot" });
		}

		[Test]
		public void ManyToMany_FiltersOtherSide(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			// Authors that have a book whose title starts with 'F' — the predicate rides the junction join.
			var authors = session.GetTable<Author>()
				.Where(a => a.Books.Any(b => b.Title.StartsWith("F")))
				.Select(a => a.Name)
				.ToList();

			authors.ShouldBe(new[] { "Asimov" });
		}
	}
}
