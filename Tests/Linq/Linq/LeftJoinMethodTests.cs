#if NET10_0_OR_GREATER

using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class LeftJoinMethodTests : TestBase
	{
		[Table]
		public class Order
		{
			[PrimaryKey] public int    Id         { get; set; }
			[Column]     public int    CustomerId { get; set; }
			[Column]     public string Name       { get; set; } = null!;
		}

		[Table]
		public class Customer
		{
			[PrimaryKey] public int    Id   { get; set; }
			[Column]     public string Name { get; set; } = null!;
		}

		static readonly Customer[] Customers =
		[
			new() { Id = 1, Name = "Alice" },
			new() { Id = 2, Name = "Bob" },
			new() { Id = 3, Name = "Carol" },
		];

		// CustomerId=1: 2 orders (matched)
		// CustomerId=2: 1 order  (matched)
		// CustomerId=3: 0 orders (unmatched — null on right)
		static readonly Order[] Orders =
		[
			new() { Id = 10, CustomerId = 1, Name = "Order-A" },
			new() { Id = 20, CustomerId = 1, Name = "Order-B" },
			new() { Id = 30, CustomerId = 2, Name = "Order-C" },
		];

		[Test]
		public void LeftJoinSimple([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(Customers);
			using var orders    = db.CreateLocalTable(Orders);

			var query = customers
				.LeftJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, OrderName = o!.Name });

			AssertQuery(query);
		}

		[Test]
		public void LeftJoinWithFilter([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(Customers);
			using var orders    = db.CreateLocalTable(Orders);

			var query = customers
				.Where(c => c.Id >= 2)
				.LeftJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, OrderName = o!.Name });

			AssertQuery(query);
		}

		[Test]
		public void LeftJoinWithSubquery([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(Customers);
			using var orders    = db.CreateLocalTable(Orders);

			var query = customers
				.Where(c => c.Id > 0)
				.Take(10)
				.LeftJoin(
					orders,
					c => c.Id,
					o => o.CustomerId,
					(c, o) => new { CustomerId = (int?)c!.Id, OrderId = (int?)o!.Id });

			AssertQuery(query);
		}

		[Test]
		public void LeftJoinWithProjection([DataSources] string context)
		{
			using var db        = GetDataContext(context);
			using var customers = db.CreateLocalTable(Customers);
			using var orders    = db.CreateLocalTable(Orders);

			var query = customers
				.LeftJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
				.Select(x => new { CustomerName = x.c.Name, OrderId = (int?)x.o!.Id })
				.ToList();


			var expected = customers.ToList()
				.LeftJoin(orders, c => c.Id, o => o.CustomerId, (c, o) => new { c, o })
				.Select(x => new { CustomerName = x.c.Name, OrderId = (int?)x.o?.Id })
				.ToList();

			AreEqual(expected, query);
		}
	}
}

#endif
