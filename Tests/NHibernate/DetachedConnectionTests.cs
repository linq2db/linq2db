using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;

using NHibernate;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises <c>CreateLinq2DbConnectionDetached</c>. Unlike <c>CreateLinqToDbConnection</c> /
	/// <c>CreateLinqToDbContext</c>, which reuse the session's already-open connection and transaction, the detached
	/// variant opens its own connection from the session's connection string — so it runs independently of the
	/// session's transaction while still using the session's provider and NHibernate-derived mapping metadata.
	/// </summary>
	[TestFixture]
	public class DetachedConnectionTests : NHTestBase
	{
		const string Id = "DETCH";

		static void SeedCustomer(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// Delete-first so a persisted (server) database stays deterministic across re-runs; committed so the
			// separate detached connection can see it.
			session.GetTable<Customer>().Where(c => c.CustomerId == Id).Delete();
			session.Save(new Customer { CustomerId = Id, CompanyName = "Detached Co" });

			tx.Commit();
		}

		// Scoped to SQLite: the detached connection reconnects from the session connection's own connection string,
		// which round-trips intact only where it carries no credentials the ADO provider strips after opening
		// (server providers drop the password unless PersistSecurityInfo is set). SQLite has none, so the reconnect
		// is reliable here.
		[Test]
		public void CreateLinq2DbConnectionDetached_OpensOwnConnection_AndReadsCommittedData(
			[NHIncludeDataSources(ProviderName.SQLiteClassic)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCustomer(sf);

			using var session = sf.OpenSession();

			using var dc = session.CreateLinq2DbConnectionDetached();

			// Detached: a distinct physical connection, not the session's own.
			dc.OpenDbConnection().ShouldNotBeSameAs(session.Connection);

			// It still queries the same database through linq2db, using the session's provider and mapping metadata.
			var name = dc.GetTable<Customer>()
				.Where(c => c.CustomerId == Id)
				.Select(c => c.CompanyName)
				.Single();

			name.ShouldBe("Detached Co");
		}
	}
}
