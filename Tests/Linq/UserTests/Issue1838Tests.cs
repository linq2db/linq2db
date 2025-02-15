using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1838Tests : TestBase
	{
		public class Invoice
		{
			public long InvoiceID { get; set; }
			public long? InvoiceReferenceNumberID { get; set; }
			public decimal? SettlementTotalOnIssue { get; set; }
		}

		public class InvoiceLineItem
		{
			public long InvoiceLineItemID { get; set; }
			public decimal BillingAmountOverride { get; set; }
			public bool Suppressed { get; set; }
			public long OwningInvoiceID { get; set; }
		}

		public class InvoiceReferenceNumber
		{
			public long    InvoiceReferenceNumberID { get; set; }
			public string? ReferenceNumber          { get; set; }
		}

		[Test]
		public void ConditionalTests([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var invoices = new Invoice[]{new Invoice
			{
				InvoiceID = 1,
				InvoiceReferenceNumberID = 10,
				SettlementTotalOnIssue = 3
			}};

			using (var db = GetDataContext(context))
			using ( db.CreateLocalTable<Invoice>(invoices))
			using ( db.CreateLocalTable<InvoiceLineItem>())
			using ( db.CreateLocalTable<InvoiceReferenceNumber>())
			{
				var invoiceAmount =
					from i in db.GetTable<Invoice>()
					from ili in  db.GetTable<InvoiceLineItem>().Where(x => x.OwningInvoiceID == i.InvoiceID && !x.Suppressed)
					group new { i.InvoiceID, ili.BillingAmountOverride } by new { i.InvoiceID } into g
					select new
					{
						InvoiceId = g.Key.InvoiceID,
						Total = g.Sum(x => x.BillingAmountOverride)
					};

				var query = from i in  db.GetTable<Invoice>()
					from r in db.GetTable<InvoiceReferenceNumber>().Where(x => x.InvoiceReferenceNumberID == i.InvoiceReferenceNumberID).DefaultIfEmpty()
					from ia in invoiceAmount.Where(x => x.InvoiceId == i.InvoiceID).DefaultIfEmpty()
					select new 
					{
						InvoiceId = i.InvoiceID,
						ReferenceNumber = r == null ? null : r.ReferenceNumber,
						TotalSettlementAmount = i.SettlementTotalOnIssue != null ? i.SettlementTotalOnIssue : ia != null ? (decimal?)ia.Total : null
					};

				var query2 = from i in  db.GetTable<Invoice>()
					from r in db.GetTable<InvoiceReferenceNumber>().Where(x => x.InvoiceReferenceNumberID == i.InvoiceReferenceNumberID).DefaultIfEmpty()
					from ia in invoiceAmount.Where(x => x.InvoiceId == i.InvoiceID).DefaultIfEmpty()
					select new 
					{
						InvoiceId = i.InvoiceID,
						ReferenceNumber = r == null ? null : r.ReferenceNumber,
						TotalSettlementAmount = i.SettlementTotalOnIssue != null ? i.SettlementTotalOnIssue : null
					};

				var query3 = from i in  db.GetTable<Invoice>()
					from r in db.GetTable<InvoiceReferenceNumber>().Where(x => x.InvoiceReferenceNumberID == i.InvoiceReferenceNumberID).DefaultIfEmpty()
					from ia in invoiceAmount.Where(x => x.InvoiceId == i.InvoiceID).DefaultIfEmpty()
					select new 
					{
						InvoiceId = i.InvoiceID,
						ReferenceNumber = r == null ? null : r.ReferenceNumber,
						TotalSettlementAmount = ia != null ? (decimal?)ia.Total : null
					};

				var resut = query.ToArray();
				var resut2 = query2.ToArray();
				var resut3 = query3.ToArray();
			}
		}
	}
}
