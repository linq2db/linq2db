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
	/// Exercises the change-tracker parity feature: an entity materialised by a linq2db query over an
	/// attached NHibernate <see cref="ISession"/> is locked into that session (becomes managed) when
	/// <see cref="LinqToDBForNHibernateTools.EnableChangeTracker"/> is on.
	/// </summary>
	[TestFixture]
	public class ChangeTrackerTests : NHTestBase
	{
		[Test]
		public void QueriedEntity_IsAttachedToSession(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			var customer = session.GetTable<Customer>().First(c => c.CustomerId == "ALFKI");

			// With EnableChangeTracker on (the default), the linq2db-materialised entity is attached
			// to the NHibernate session, so the session now manages it.
			session.Contains(customer).ShouldBeTrue();
		}

		[Test]
		public void QueriedEntity_IsNotAttached_WhenChangeTrackerDisabled(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var previous = LinqToDBForNHibernateTools.EnableChangeTracker;
			LinqToDBForNHibernateTools.EnableChangeTracker = false;
			try
			{
				var sf = GetSessionFactory(provider);

				using var session = sf.OpenSession();

				var customer = session.GetTable<Customer>().First(c => c.CustomerId == "ALFKI");

				session.Contains(customer).ShouldBeFalse();
			}
			finally
			{
				LinqToDBForNHibernateTools.EnableChangeTracker = previous;
			}
		}

		[Test]
		public void QueriedEntity_ModificationIsPersisted_WhenTrackerEnabled(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCustomer(sf, "CTPOS", "Original Co");

			using (var session = sf.OpenSession())
			{
				var customer = session.GetTable<Customer>().First(c => c.CustomerId == "CTPOS");
				customer.CompanyName = "Tracked Change";

				// The change-tracked entity must be dirty-checked and flushed as an UPDATE on commit.
				using var tx = session.BeginTransaction();
				tx.Commit();
			}

			// A fresh session proves the UPDATE actually reached the database.
			ReadCompanyName(sf, "CTPOS").ShouldBe("Tracked Change");
		}

		[Test]
		public void QueriedEntity_ModificationIsNotPersisted_WhenTrackerDisabled(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCustomer(sf, "CTNEG", "Original Co");

			var previous = LinqToDBForNHibernateTools.EnableChangeTracker;
			LinqToDBForNHibernateTools.EnableChangeTracker = false;
			try
			{
				using (var session = sf.OpenSession())
				{
					var customer = session.GetTable<Customer>().First(c => c.CustomerId == "CTNEG");
					customer.CompanyName = "Should Not Persist";

					// The entity is not attached, so nothing is flushed for it.
					using var tx = session.BeginTransaction();
					tx.Commit();
				}

				ReadCompanyName(sf, "CTNEG").ShouldBe("Original Co");
			}
			finally
			{
				LinqToDBForNHibernateTools.EnableChangeTracker = previous;
			}
		}

		// Delete-first + insert so each test owns an isolated customer that stays deterministic across re-runs.
		static void SeedCustomer(ISessionFactory sf, string id, string company)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			// The linq2db command runs inside the NHibernate transaction — the connection's interceptor enlists it.
			session.GetTable<Customer>().Where(c => c.CustomerId == id).Delete();
			session.Save(new Customer { CustomerId = id, CompanyName = company });

			tx.Commit();
		}

		static string ReadCompanyName(ISessionFactory sf, string id)
		{
			using var session = sf.OpenSession();
			return session.Get<Customer>(id).CompanyName;
		}
	}
}
