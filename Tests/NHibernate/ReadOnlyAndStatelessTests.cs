using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Verifies the no-tracking entry points: <c>ISession.AsReadOnly()</c> queries over the session but leaves the
	/// materialised entities detached (never added to the first-level cache), and <c>IStatelessSession</c> — which
	/// has no first-level cache at all — can be queried through linq2db.
	/// </summary>
	[TestFixture]
	public class ReadOnlyAndStatelessTests : NHTestBase
	{
		[Test]
		public void AsReadOnly_LeavesEntitiesDetached(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			var customer = session.AsReadOnly().GetTable<Customer>().First(c => c.CustomerId == "ALFKI");

			customer.ShouldNotBeNull();
			customer.CustomerId.ShouldBe("ALFKI");
			// AsReadOnly() suppresses the change-tracker attach, so the entity must NOT be in the session.
			session.Contains(customer).ShouldBeFalse();
		}

		[Test]
		public void StatelessSession_QueriesThroughLinqToDb(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var stateless = sf.OpenStatelessSession();

			var customer = stateless.GetTable<Customer>().First(c => c.CustomerId == "ALFKI");

			customer.ShouldNotBeNull();
			customer.CustomerId.ShouldBe("ALFKI");
			customer.CompanyName.ShouldBe("Alfreds Futterkiste");
		}
	}
}
