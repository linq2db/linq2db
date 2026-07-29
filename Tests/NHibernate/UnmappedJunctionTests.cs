using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.UnmappedJunction;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises a many-to-many whose junction table is not mapped as an entity — the ordinary
	/// <c>HasManyToMany</c> shape, where NHibernate knows the junction only by table name.
	/// </summary>
	[TestFixture]
	public class UnmappedJunctionTests : NHTestBase
	{
		// Seeds two clubs over three members. Written through NHibernate: the junction rows belong to the
		// collection, so only NHibernate can create them (linq2db has no entity for that table).
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// Deleting the owners takes their collection rows with them, clearing the junction natively.
			foreach (var club in session.Query<Club>().ToList())
				session.Delete(club);

			session.Flush();

			foreach (var member in session.Query<Member>().ToList())
				session.Delete(member);

			session.Flush();

			var ada = new Member { Id = 1, Name = "Ada" };
			var bob = new Member { Id = 2, Name = "Bob" };
			var cid = new Member { Id = 3, Name = "Cid" };

			session.Save(ada);
			session.Save(bob);
			session.Save(cid);

			var chess = new Club { Id = 1, Name = "Chess" };
			chess.Members.Add(ada);
			chess.Members.Add(bob);

			var choir = new Club { Id = 2, Name = "Choir" };
			choir.Members.Add(cid);

			session.Save(chess);
			session.Save(choir);

			tx.Commit();
		}

		[Test]
		public void ManyToMany_NavigatesThroughUnmappedJunction(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			var names = session.GetTable<Club>()
				.Where(c => c.Id == 1)
				.SelectMany(c => c.Members)
				.Select(m => m.Name)
				.OrderBy(n => n)
				.ToList();

			names.ShouldBe(new[] { "Ada", "Bob" });

			// NHibernate's own provider navigates the same collection and must agree.
			var nhNames = session.Query<Club>()
				.Where(c => c.Id == 1)
				.SelectMany(c => c.Members)
				.Select(m => m.Name)
				.OrderBy(n => n)
				.ToList();

			nhNames.ShouldBe(names);
		}

		// Composite keys deliberately mirror each other — zone (1,7) against zone (7,1), facility (10,20) against
		// facility (20,10) — so pairing a junction column with the wrong key resolves to the other row and fails.
		static void SeedCompositeGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			foreach (var zone in session.Query<Zone>().ToList())
				session.Delete(zone);

			session.Flush();

			foreach (var facility in session.Query<Facility>().ToList())
				session.Delete(facility);

			session.Flush();

			var f1 = new Facility { SiteId = 10, FacilityNo = 20, Label = "F1" };
			var f2 = new Facility { SiteId = 11, FacilityNo = 21, Label = "F2" };
			var f3 = new Facility { SiteId = 20, FacilityNo = 10, Label = "F3" };

			session.Save(f1);
			session.Save(f2);
			session.Save(f3);

			var alpha = new Zone { ZoneId = 1, ZoneNo = 7, Name = "Alpha" };
			alpha.Facilities.Add(f1);
			alpha.Facilities.Add(f2);

			var beta = new Zone { ZoneId = 7, ZoneNo = 1, Name = "Beta" };
			beta.Facilities.Add(f3);

			session.Save(alpha);
			session.Save(beta);

			tx.Commit();
		}

		[Test]
		public void ManyToMany_CompositeKeys_ThroughUnmappedJunction(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCompositeGraph(sf);

			using var session = sf.OpenSession();

			var labels = session.GetTable<Zone>()
				.Where(z => z.ZoneId == 1 && z.ZoneNo == 7)
				.SelectMany(z => z.Facilities)
				.Select(f => f.Label)
				.OrderBy(l => l)
				.ToList();

			labels.ShouldBe(new[] { "F1", "F2" });

			// NHibernate's own provider builds the same four-column join and must agree.
			var nhLabels = session.Query<Zone>()
				.Where(z => z.ZoneId == 1 && z.ZoneNo == 7)
				.SelectMany(z => z.Facilities)
				.Select(f => f.Label)
				.OrderBy(l => l)
				.ToList();

			nhLabels.ShouldBe(labels);

			// The mirrored zone must see only its own facility.
			var betaLabels = session.GetTable<Zone>()
				.Where(z => z.ZoneId == 7 && z.ZoneNo == 1)
				.SelectMany(z => z.Facilities)
				.Select(f => f.Label)
				.ToList();

			betaLabels.ShouldBe(new[] { "F3" });
		}

		[Test]
		public void ManyToMany_UnmappedJunction_FiltersOtherSide(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			// The correlated predicate has to ride the junction, and must not leak the other club's members.
			var clubs = session.GetTable<Club>()
				.Where(c => c.Members.Any(m => m.Name == "Cid"))
				.Select(c => c.Name)
				.ToList();

			clubs.ShouldBe(new[] { "Choir" });
		}
	}
}
