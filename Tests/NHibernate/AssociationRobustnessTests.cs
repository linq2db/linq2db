using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Associations;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises a many-to-one whose foreign-key member on the source (<see cref="Widget.Gid"/>) is named
	/// differently from the target's primary-key member (<see cref="Gadget.GadgetId"/>). linq2db must resolve
	/// the association's <c>ThisKey</c> from the source's foreign-key column, not from the target PK member name,
	/// so the navigation joins the right rows.
	/// </summary>
	[TestFixture]
	public class AssociationRobustnessTests : NHTestBase
	{
		// Seeds a Widget whose Id (1) differs from the Gadget it references (GadgetId 100), plus a decoy Gadget whose
		// GadgetId equals the Widget's Id — so a join that mistakenly used Widget.Id would pick the decoy.
		static void SeedGraph(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.GetTable<Widget>().Delete();
			session.GetTable<Gadget>().Delete();

			session.Save(new Gadget { GadgetId = 100, Name = "G100" });
			session.Save(new Gadget { GadgetId = 1,   Name = "G1-decoy" });
			session.Save(new Widget { Id = 1, Gid = 100, Name = "W1" });

			tx.Commit();
		}

		[Test]
		public void ManyToOne_DifferentlyNamedForeignKey_Navigates(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedGraph(sf);

			using var session = sf.OpenSession();

			// Navigate the many-to-one from linq2db: Widget.Gadget joins Widget.Gid -> Gadget.GadgetId.
			var name = session.GetTable<Widget>()
				.Where(w => w.Id == 1)
				.Select(w => w.Gadget!.Name)
				.First();

			name.ShouldBe("G100");

			// The same navigation through NHibernate's own LINQ provider must agree.
			var nhName = session.Query<Widget>()
				.Where(w => w.Id == 1)
				.Select(w => w.Gadget!.Name)
				.First();

			nhName.ShouldBe(name);

			// The association also carries a predicate: filtering on the target must ride the same join.
			var filtered = session.GetTable<Widget>()
				.Where(w => w.Gadget!.Name == "G100")
				.Select(w => w.Name)
				.ToList();

			filtered.ShouldBe(new[] { "W1" });
		}
	}
}
