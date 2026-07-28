using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Components;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises &lt;component&gt; mapping: a value object whose properties are columns of the owning entity's table
	/// has to reach linq2db as those columns, rather than being dropped because the member spans more than one.
	/// </summary>
	[TestFixture]
	public class ComponentTests : NHTestBase
	{
		static void SeedContacts(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.GetTable<Contact>().Delete();

			session.Save(new Contact { Id = 1, Name = "Ada", Address = new PostalAddress { Street = "1 Main St", City = "London" } });
			session.Save(new Contact { Id = 2, Name = "Bob", Address = new PostalAddress { Street = "2 Side Rd", City = "Paris"  } });

			tx.Commit();
		}

		[Test]
		public void Component_Materializes(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedContacts(sf);

			using var session = sf.OpenSession();

			var contact = session.GetTable<Contact>().Single(c => c.Id == 1);

			contact.Address.ShouldNotBeNull();
			contact.Address.Street.ShouldBe("1 Main St");
			contact.Address.City.ShouldBe("London");

			// NHibernate's own provider must agree.
			var nhContact = session.Query<Contact>().Single(c => c.Id == 1);
			nhContact.Address.Street.ShouldBe(contact.Address.Street);
			nhContact.Address.City.ShouldBe(contact.Address.City);
		}

		[Test]
		public void Component_IsQueryableAndProjectable(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedContacts(sf);

			using var session = sf.OpenSession();

			// A component member has to be usable in a predicate and a projection, like any other column.
			var names = session.GetTable<Contact>()
				.Where(c => c.Address.City == "Paris")
				.Select(c => c.Name)
				.ToList();

			names.ShouldBe(new[] { "Bob" });

			var cities = session.GetTable<Contact>()
				.OrderBy(c => c.Id)
				.Select(c => c.Address.City)
				.ToList();

			cities.ShouldBe(new[] { "London", "Paris" });
		}
	}
}
