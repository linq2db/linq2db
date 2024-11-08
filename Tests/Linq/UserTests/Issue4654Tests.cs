using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4654Tests : TestBase
	{
		[Table("Issue4654Customer")]
		public class Customer
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string? Name { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = nameof(Order.CustomerId))]
			public ICollection<Order>? Orders { get; set; }
		}

		[Table("Issue4654Order")]
		public class Order
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string ProductName { get; set; } = null!;
			[Column] public int Quantity { get; set; }
			[Column] public int CustomerId { get; set; }
			[Association(ThisKey = nameof(CustomerId), OtherKey = nameof(Customer.Id), CanBeNull = true)]
			public Customer? Customer { get; set; }
		}

		[Table("Issue4654Product")]
		public class Product
		{
			[Identity, PrimaryKey] public int Id { get; set; }
			[Column] public string Name { get; set; } = null!;
			[Column] public decimal Price { get; set; }
		}

		public class CombinedResults
		{
			public string Id { get; set; } = null!;

			public string? Name { get; set; }
		}

		[Test]
		public void Test([DataSources] string configuration)
		{
			using var db = GetDataContext(configuration);
			using var t1 = db.CreateLocalTable<Customer>();
			using var t2 = db.CreateLocalTable<Order>();
			using var t3 = db.CreateLocalTable<Product>();

			var customerResults = t1.Select(c => new CombinedResults()
			{
				Id = "" + c.Id,
				Name = c.Name
			});

			var orderResults = t2.Select(o => new CombinedResults()
			{
				Id ="" + o.Id,
				Name = o.ProductName
			});

			var productResults = t3.Select(p => new CombinedResults()
			{
				Id = "" + p.Id,
				Name = p.Name
			});

			// Union of all three results
			var combinedResults = customerResults
			.Union(orderResults)
			.Union(productResults);

			// Convert to list and return the results
			var result = combinedResults.ToArray();
		}
	}
}
