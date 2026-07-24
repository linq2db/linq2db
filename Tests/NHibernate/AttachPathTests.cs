using System.Linq;

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
	/// Exercises the core attach path — linq2db querying over an open NHibernate <see cref="ISession"/>'s ADO
	/// connection, using mapping metadata synthesized from NHibernate by <c>NHMetadataReader</c> — across every
	/// enabled provider (SQLite via System.Data.SQLite; SQL Server via Microsoft.Data.SqlClient). Each provider
	/// runs against its own isolated database built by <see cref="NHTestBase"/>.
	/// </summary>
	[TestFixture]
	public class AttachPathTests : NHTestBase
	{
		[Test]
		public void GetTable_RunsSqlThroughSessionConnection(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();
			using var db      = session.CreateLinqToDbConnection();

			// Builds and executes real SQL over the session's ADO connection.
			var customers = db.GetTable<Customer>().ToList();

			customers.ShouldNotBeNull();

			// The same query through NHibernate's own LINQ provider returns the same customers (by key).
			var nhIds = session.Query<Customer>().Select(c => c.CustomerId).OrderBy(id => id).ToList();
			customers.Select(c => c.CustomerId).OrderBy(id => id).ShouldBe(nhIds);
		}

		[Test]
		public void GetTable_ReturnsSeededRow(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();
			using var db      = session.CreateLinqToDbConnection();

			var names = db.GetTable<Customer>()
				.Where (c => c.CustomerId == "ALFKI")
				.Select(c => c.CompanyName)
				.ToList();

			names.ShouldHaveSingleItem();
			names[0].ShouldBe("Alfreds Futterkiste");

			// The same query through NHibernate's own LINQ provider must return the same result.
			var nhNames = session.Query<Customer>()
				.Where (c => c.CustomerId == "ALFKI")
				.Select(c => c.CompanyName)
				.ToList();

			nhNames.ShouldBe(names);
		}
	}
}
