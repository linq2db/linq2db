using System.Linq;

using LinqToDB;
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
