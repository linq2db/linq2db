using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5256Tests : TestBase
	{
		[Table]
		public class Product
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
		}

		[Table]
		public class PurchaseOrderLineItem
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public int ProductId { get; set; }
			[Column] public int Quantity { get; set; }
		}

		[Test]
		public async Task NestedSubqueryWithGroupedAggregationsSumOfSums([DataSources] string context)
		{
			var productsData = new[]
			{
				new Product { Id = 1, Name = "Product1" },
				new Product { Id = 2, Name = "Product2" },
				new Product { Id = 3, Name = "Product3" }
			};

			var purchaseOrderLineItemsData = new[]
			{
				new PurchaseOrderLineItem { Id = 1, ProductId = 1, Quantity = 10 },
				new PurchaseOrderLineItem { Id = 2, ProductId = 1, Quantity = 20 },
				new PurchaseOrderLineItem { Id = 3, ProductId = 2, Quantity = 15 },
				new PurchaseOrderLineItem { Id = 4, ProductId = 2, Quantity = 25 },
				new PurchaseOrderLineItem { Id = 5, ProductId = 3, Quantity = 5 }
			};

			using var db = GetDataContext(context);
			using var products = db.CreateLocalTable(productsData);
			using var purchaseOrderLineItems = db.CreateLocalTable(purchaseOrderLineItemsData);

			// This query reproduces the issue: nested subquery with grouped aggregations
			// where Sum() is applied both inside the group selection and on the outer result set
			var query =
				from product in db.GetTable<Product>()
				select new
				{
					ProductId = product.Id,
					OnOrder = (
						from s in db.GetTable<PurchaseOrderLineItem>()
						group s by s.Id into testGroup
						select testGroup.Sum(x => (decimal?)x.Quantity)
					).Sum() ?? 0
				};

			var result = await query.ToListAsync();

			// Verify the query executes without SQL errors
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Has.Count.EqualTo(3));

			// Verify the aggregation results
			// Each product should have the sum of all quantities across all line items
			var totalQuantity = purchaseOrderLineItemsData.Sum(x => x.Quantity);
			foreach (var item in result)
			{
				Assert.That(item.OnOrder, Is.EqualTo(totalQuantity));
			}
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public async Task NestedSubqueryWithGroupedAggregationsFilteredSumOfSums([DataSources] string context)
		{
			var productsData = new[]
			{
				new Product { Id = 1, Name = "Product1" },
				new Product { Id = 2, Name = "Product2" },
				new Product { Id = 3, Name = "Product3" }
			};

			var purchaseOrderLineItemsData = new[]
			{
				new PurchaseOrderLineItem { Id = 1, ProductId = 1, Quantity = 10 },
				new PurchaseOrderLineItem { Id = 2, ProductId = 1, Quantity = 20 },
				new PurchaseOrderLineItem { Id = 3, ProductId = 2, Quantity = 15 },
				new PurchaseOrderLineItem { Id = 4, ProductId = 2, Quantity = 25 },
				new PurchaseOrderLineItem { Id = 5, ProductId = 3, Quantity = 5 }
			};

			using var db = GetDataContext(context);
			using var products = db.CreateLocalTable(productsData);
			using var purchaseOrderLineItems = db.CreateLocalTable(purchaseOrderLineItemsData);

			// Variation with filtering by ProductId in the subquery
			var query =
				from product in db.GetTable<Product>()
				select new
				{
					ProductId = product.Id,
					OnOrder = (
						from s in db.GetTable<PurchaseOrderLineItem>()
						where s.ProductId == product.Id
						group s by s.Id into testGroup
						select testGroup.Sum(x => (decimal?)x.Quantity)
					).Sum() ?? 0
				};

			var result = await query.ToListAsync();

			AssertQuery(query);

			// Verify the query executes without SQL errors
			Assert.That(result, Is.Not.Null);
			Assert.That(result, Has.Count.EqualTo(3));

			// Verify the aggregation results per product
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.First(x => x.ProductId == 1).OnOrder, Is.EqualTo(30)); // 10 + 20
				Assert.That(result.First(x => x.ProductId == 2).OnOrder, Is.EqualTo(40)); // 15 + 25
				Assert.That(result.First(x => x.ProductId == 3).OnOrder, Is.EqualTo(5));  // 5
			}
		}
	}
}
