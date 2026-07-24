using System.Linq;

using LinqToDB;
using LinqToDB.NHibernate.Tests.Models.Northwind;
using LinqToDB.NHibernate.Tests.Models.Org;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Demonstrates capabilities NHibernate's own LINQ provider lacks — set-based bulk UPDATE/DELETE, window
	/// functions, recursive CTEs, upserts, INSERT-from-SELECT and arbitrary cross-entity joins — working through
	/// the bridge over NHibernate-mapped entities, with no new integration code. Each is just a linq2db extension
	/// on the <c>ITable&lt;T&gt;</c> that <c>session.GetTable&lt;T&gt;()</c> (or a linq2db context) returns.
	/// </summary>
	[TestFixture]
	public class PowerFeaturesTests : NHTestBase
	{
		[Test]
		public void BulkUpdateAndDelete(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			using (var tx = session.BeginTransaction())
			{
				session.GetTable<Customer>().Where(c => c.CustomerId.StartsWith("PWR")).Delete();
				session.Save(new Customer { CustomerId = "PWR1", CompanyName = "A", Country = "Demoland",  City = "Old" });
				session.Save(new Customer { CustomerId = "PWR2", CompanyName = "B", Country = "Demoland",  City = "Old" });
				session.Save(new Customer { CustomerId = "PWR3", CompanyName = "C", Country = "Elsewhere", City = "Old" });
				tx.Commit();
			}

			// linq2db DML shares the session's transaction; Firebird in particular requires the command to run
			// inside one (its connection is always transactional).
			using (var tx = session.BeginTransaction())
			{
				// Set-based bulk UPDATE: no entity loading, no per-row round-trips — NHibernate LINQ cannot do this.
				var updated = session.GetTable<Customer>()
					.Where(c => c.Country == "Demoland")
					.Set(c => c.City, "New")
					.Update();

				updated.ShouldBe(2);
				session.GetTable<Customer>().Count(c => c.City == "New").ShouldBe(2);

				// Set-based bulk DELETE.
				var deleted = session.GetTable<Customer>().Where(c => c.CustomerId.StartsWith("PWR")).Delete();

				deleted.ShouldBe(3);
				session.GetTable<Customer>().Count(c => c.CustomerId.StartsWith("PWR")).ShouldBe(0);

				tx.Commit();
			}
		}

		[Test]
		public void WindowFunction_RowNumber(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			// ROW_NUMBER() OVER (ORDER BY CustomerId) — NHibernate LINQ cannot emit window functions at all.
			var ranked = session.GetTable<Customer>().AsReadOnly()
				.Select(c => new { c.CustomerId, Rn = Sql.Window.RowNumber(f => f.OrderBy(c.CustomerId)) })
				.OrderBy(x => x.Rn)
				.ToList();

			ranked.ShouldNotBeEmpty();
			ranked.Select(x => (int)x.Rn).ShouldBe(Enumerable.Range(1, ranked.Count));
		}

		[Test]
		public void RecursiveCte_OrgChart(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			using (var tx = session.BeginTransaction())
			{
				session.GetTable<OrgUnit>().Delete();
				session.Save(new OrgUnit { Id = 1, ParentId = null, Name = "CEO" });
				session.Save(new OrgUnit { Id = 2, ParentId = 1,    Name = "VP-A" });
				session.Save(new OrgUnit { Id = 3, ParentId = 1,    Name = "VP-B" });
				session.Save(new OrgUnit { Id = 4, ParentId = 2,    Name = "Lead" });
				tx.Commit();
			}

			using (var tx = session.BeginTransaction())
			{
				using var db = session.CreateLinqToDbContext();

				// Recursive CTE walking the tree from its roots, accumulating each node's depth as it
				// descends. The Level accumulator is what makes this genuinely recursive — a flat query
				// (and NHibernate's own LINQ provider) cannot compute it.
				var tree = db.GetCte<OrgLevel>(self =>
					db.GetTable<OrgUnit>()
						.Where(o => o.ParentId == null)
						.Select(o => new OrgLevel { Id = o.Id, Name = o.Name, Level = 1 })
						.Concat(
							from o   in db.GetTable<OrgUnit>()
							from par in self.InnerJoin(par => par.Id == o.ParentId)
							select new OrgLevel { Id = o.Id, Name = o.Name, Level = par.Level + 1 }));

				var rows = tree.OrderBy(x => x.Id).ToList();

				// Depth proves the recursion actually ran: CEO=1, its VPs=2, the VP-A's lead=3.
				rows.Select(x => (x.Name, x.Level)).ShouldBe(new[]
				{
					("CEO",  1),
					("VP-A", 2),
					("VP-B", 2),
					("Lead", 3),
				});

				session.GetTable<OrgUnit>().Delete();
				tx.Commit();
			}
		}

		[Test]
		public void Upsert_InsertOrUpdate(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			using var tx = session.BeginTransaction();

			var table = session.GetTable<OrgUnit>();
			table.Where(o => o.Id == 900).Delete();

			// First upsert inserts (no matching key).
			table.InsertOrUpdate(
				() => new OrgUnit { Id = 900, ParentId = null, Name = "First" },
				o  => new OrgUnit { Name = "Updated" });
			table.Single(o => o.Id == 900).Name.ShouldBe("First");

			// Second upsert updates (key now matches) — NHibernate has no equivalent.
			table.InsertOrUpdate(
				() => new OrgUnit { Id = 900, ParentId = null, Name = "Second" },
				o  => new OrgUnit { Name = "Updated" });
			table.Single(o => o.Id == 900).Name.ShouldBe("Updated");

			table.Where(o => o.Id == 900).Delete();
			tx.Commit();
		}

		[Test]
		public void InsertFromSelect(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			using (var tx = session.BeginTransaction())
			{
				var table = session.GetTable<Customer>();
				table.Where(c => c.Country == "Src" || c.Country == "Copy").Delete();
				session.Save(new Customer { CustomerId = "IFS1", CompanyName = "A", Country = "Src" });
				session.Save(new Customer { CustomerId = "IFS2", CompanyName = "B", Country = "Src" });
				tx.Commit();
			}

			using (var tx = session.BeginTransaction())
			{
				var table = session.GetTable<Customer>();

				// INSERT INTO Customers SELECT ... FROM Customers — a server-side copy, no rows pulled to the client.
				var inserted = table
					.Where(c => c.Country == "Src")
					.Insert(session.GetTable<Customer>(), c => new Customer
					{
						CustomerId  = "C" + c.CustomerId,
						CompanyName = c.CompanyName,
						Country     = "Copy",
						IsDeleted   = c.IsDeleted,
					});

				inserted.ShouldBe(2);
				table.Count(c => c.Country == "Copy").ShouldBe(2);

				table.Where(c => c.Country == "Src" || c.Country == "Copy").Delete();
				tx.Commit();
			}
		}

		[Test]
		public void CrossEntityJoin(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			using (var tx = session.BeginTransaction())
			{
				session.GetTable<Customer>().Where(c => c.Country == "JoinLand").Delete();
				session.GetTable<Employee>().Where(e => e.Country == "JoinLand").Delete();
				session.Save(new Customer { CustomerId = "JOIN1", CompanyName = "A", Country = "JoinLand" });
				session.Save(new Employee { LastName = "Smith", FirstName = "Joe", Country = "JoinLand" });
				tx.Commit();
			}

			using (var tx = session.BeginTransaction())
			{
				using var db = session.CreateLinqToDbContext();

				// Join two entities that have NO mapped association, on a non-key column — awkward or impossible
				// in NHibernate LINQ; trivial in linq2db.
				var pairs =
					(from c in db.GetTable<Customer>()
					 join e in db.GetTable<Employee>() on c.Country equals e.Country
					 where c.Country == "JoinLand"
					 select new { c.CustomerId, e.LastName }).ToList();

				pairs.Count.ShouldBe(1);
				pairs[0].CustomerId.ShouldBe("JOIN1");
				pairs[0].LastName.ShouldBe("Smith");

				db.GetTable<Customer>().Where(c => c.Country == "JoinLand").Delete();
				db.GetTable<Employee>().Where(e => e.Country == "JoinLand").Delete();
				tx.Commit();
			}
		}
	}
}
