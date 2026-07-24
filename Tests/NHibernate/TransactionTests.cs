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
	/// Verifies that a linq2db command over an attached session participates in the session's NHibernate
	/// transaction: because the connection is created with <c>UseTransaction</c>, the command shares the ADO
	/// transaction and therefore commits and rolls back together with NHibernate's own work.
	/// </summary>
	[TestFixture]
	public class TransactionTests : NHTestBase
	{
		static void SeedCustomer(ISessionFactory sf, string id, string company)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.GetTable<Customer>().Where(c => c.CustomerId == id).Delete();
			session.Save(new Customer { CustomerId = id, CompanyName = company });

			tx.Commit();
		}

		[Test]
		public void LinqToDbWrite_RolledBackWithTransaction(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCustomer(sf, "TXROLL", "Original Co");

			using (var session = sf.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				// linq2db delete inside the NHibernate transaction.
				session.GetTable<Customer>().Where(c => c.CustomerId == "TXROLL").Delete();
				tx.Rollback();
			}

			// The row must still exist: the linq2db delete was part of the rolled-back transaction.
			using var check = sf.OpenSession();
			check.Get<Customer>("TXROLL").ShouldNotBeNull();
		}

		[Test]
		public void LinqToDbWrite_CommittedWithTransaction(
			[IncludeDataSources(ProviderName.SQLiteClassic, TestProvName.AllSqlServer)] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedCustomer(sf, "TXCOMMIT", "Original Co");

			using (var session = sf.OpenSession())
			using (var tx = session.BeginTransaction())
			{
				session.GetTable<Customer>().Where(c => c.CustomerId == "TXCOMMIT").Delete();
				tx.Commit();
			}

			// The delete is committed together with the transaction.
			using var check = sf.OpenSession();
			check.Get<Customer>("TXCOMMIT").ShouldBeNull();
		}
	}
}
