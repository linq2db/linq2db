using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.NHibernate.Tests.Models.Inheritance;

using NHibernate;
using NHibernate.Linq;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises table-per-hierarchy mapping: subclasses share one table and are told apart by a discriminator
	/// column, so a linq2db query for a subclass has to restrict itself to that subclass's discriminator values
	/// rather than returning the whole table.
	/// </summary>
	[TestFixture]
	public class InheritanceTests : NHTestBase
	{
		static void SeedDocuments(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.GetTable<Voucher>().Delete();

			session.Save(new Invoice { Id = 1, Title = "Invoice 1", InvoiceNo = "I-1" });
			session.Save(new Receipt { Id = 2, Title = "Receipt 2", ReceiptNo = "R-2" });

			tx.Commit();
		}

		[Test]
		public void TablePerSubclass_IsRefusedWithExplanation(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			// A <joined-subclass>'s own columns live in its own table, so reading it from one table would ask the
			// base table for columns it does not have — the database's complaint about that explains nothing.
			var ex = Should.Throw<LinqToDBForNHibernateToolsException>(() => session.GetTable<Car>().ToList());

			ex.Message.ShouldContain(nameof(Car));
			ex.Message.ShouldContain("table-per-subclass");
		}

		[Test]
		public void TablePerConcreteClass_SubclassIsQueryable(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			using (var seed = sf.OpenSession())
			using (var tx   = seed.BeginTransaction())
			{
				seed.GetTable<Square>().Delete();
				seed.Save(new Square { Id = 1, Name = "S1", Side = 4 });
				tx.Commit();
			}

			using var session = sf.OpenSession();

			// A concrete subclass carries every column in its own table, so it reads like any other entity.
			var squares = session.GetTable<Square>().OrderBy(s => s.Id).ToList();

			squares.Count.ShouldBe(1);
			squares[0].Name.ShouldBe("S1");
			squares[0].Side.ShouldBe(4);
		}

		[Test]
		public void Subclass_QueryReturnsOnlyItsOwnRows(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedDocuments(sf);

			using var session = sf.OpenSession();

			var invoices = session.GetTable<Invoice>().OrderBy(d => d.Id).ToList();

			invoices.Count.ShouldBe(1);
			invoices[0].Id.ShouldBe(1);
			invoices[0].InvoiceNo.ShouldBe("I-1");

			// NHibernate's own provider restricts by discriminator; linq2db must agree.
			var nhInvoices = session.Query<Invoice>().OrderBy(d => d.Id).Select(d => d.Id).ToList();
			nhInvoices.ShouldBe(invoices.Select(i => i.Id).ToList());
		}

		[Test]
		public void Subclass_PredicateCombinesWithDiscriminator(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedDocuments(sf);

			using var session = sf.OpenSession();

			// A receipt shares the table but must never satisfy an Invoice query, whatever else is asked for.
			var titles = session.GetTable<Invoice>()
				.Where(d => d.Title != null)
				.Select(d => d.Title)
				.ToList();

			titles.ShouldBe(new[] { "Invoice 1" });

			var receipts = session.GetTable<Receipt>().Select(d => d.ReceiptNo).ToList();
			receipts.ShouldBe(new[] { "R-2" });
		}

		[Test]
		public void RootClass_SeesWholeHierarchy(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedDocuments(sf);

			using var session = sf.OpenSession();

			// The root is not filtered: NHibernate returns every row of the hierarchy for it, and so must linq2db.
			var ids = session.GetTable<Voucher>().Select(d => d.Id).OrderBy(id => id).ToList();

			ids.ShouldBe(new[] { 1, 2 });
		}
	}
}
