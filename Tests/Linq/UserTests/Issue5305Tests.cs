using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	public class Issue5305Tests : TestBase
	{
		[Table("Lines")]
		public class Line
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column]               public string  Order  { get; set; } = "";
			[Column]               public decimal Amount { get; set; }
		}

		[Table("InvoiceLines")]
		public class InvoiceLine
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column]               public string  Order  { get; set; } = "";
			[Column]               public decimal Amount { get; set; }

			// If you comment this out so they have the same amount of columns it works again
			[Column] public string ExtraColumnSoTheyDontAlign { get; set; } = "";
		}

		// The DTO both sides of UNION project to
		public class Row
		{
			public string  Order  { get; set; } = "";
			public decimal Amount { get; set; }
		}

		[Test]
		public void CountInUnionAll([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db               = GetDataContext(context);
			using var lineTable        = db.CreateLocalTable<Line>([new Line { Amount               = 10m }]);
			using var invoiceLineTable = db.CreateLocalTable<InvoiceLine>([new InvoiceLine { Amount = 5m }]);

			var queryable =
				db.GetTable<Line>()
					.Select(o => new Row
					{
						Order  = o.Id.ToString(),
						Amount = o.Amount
					})
					.UnionAll(
						db.GetTable<InvoiceLine>()
							.Select(x => new Row
							{
								Order  = x.Order,
								Amount = x.Amount,
							})
					);

			var pagedQuery =
				queryable.Select(x => new
					{
						Value      = x,
						TotalCount = queryable.Count(), // <-- triggers the malformed COUNT-subquery
					})
					.Skip(0).Take(10);

			AssertQuery(pagedQuery);
		}
	}
}
