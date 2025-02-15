using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2470Tests : TestBase
	{
		[Table(Schema = "dbo", Name = "Orders")]
		public class Order
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column, NotNull] public string Name { get; set; } = null!;

			[Association(ThisKey = "Id", OtherKey = "OrderId", CanBeNull = true)]
			public IEnumerable<OrderItem> Items { get; set; } = null!;
		}

		[Table(Schema = "dbo", Name = "OrderItems")]
		public class OrderItem
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column, NotNull] public int OrderId { get; set; }
			[Column, NotNull] public string Product { get; set; } = null!;

			[Association(ThisKey = "OrderId", OtherKey = "Id", CanBeNull = false)]
			public Order Order { get; set; } = null!;
		}

		[Test]
		public void GroupJoinTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Order>())
			using (db.CreateLocalTable<OrderItem>())
			{
				var query = db.GetTable<Order>()
					.GroupJoin(
						db.GetTable<Order>().SelectMany(
							Order => db.GetTable<OrderItem>()
								.Where(OrderItem => Order.Id == OrderItem.OrderId)
								.OrderBy(orderItem => orderItem.Id)
								.Take(1)
						),
						Order => Order.Id,
						OrderItem => OrderItem.OrderId,
						(outer, inner) => new { outer, inner })
					.SelectMany(
						t => t.inner.DefaultIfEmpty(),
						(source, collection) => new { source.outer, collection })
					.ToArray();
			}
		}
	}
}
