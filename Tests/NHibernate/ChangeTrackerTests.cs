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
	}
}
