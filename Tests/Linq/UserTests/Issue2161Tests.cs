using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2161Tests : TestBase
	{
		public enum OrderType
		{
			Type1,Type2
		}

		[InheritanceMapping(Code = OrderType.Type1, Type = typeof(OrderType1))]
		[InheritanceMapping(Code = OrderType.Type2, Type = typeof(OrderType2))]
		public class Order
		{
			[PrimaryKey]
			public int OrderId { get; set; }

			[Column(IsDiscriminator = true)]
			public OrderType OrderType { get; set; }
			public string OrderName { get; set; }

			[Association(ThisKey = nameof(OrderId), OtherKey = nameof(OrderDetail.OrderId))]
			public IEnumerable<OrderDetail> Details { get; set; }
		}

		public class OrderType1 : Order
		{
		}
		public class OrderType2 : Order
		{
		}

		public class OrderDetail
		{
			[PrimaryKey]
			public int OrderDetailId { get; set; }

			public string Title { get; set; }

			public int OrderId { get; set; }
		}

		[Test]
		public void TestLoadWithDiscriminator([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Order>(new []{new OrderType1() { OrderId = 1, OrderName = "Order1" }}))
			using (db.CreateLocalTable<OrderDetail>(new []{new OrderDetail() { OrderDetailId = 100, OrderId = 1, Title = "Detail1" }}))
			{
				//Below line makes same join queries twice
				var query = db.GetTable<Order>().LoadWith(o => o.Details).Where(o => o.OrderId == 1);
				var order = query.FirstOrDefault();

				Assert.That(query.GetPreamblesCount(), Is.EqualTo(1));
			}
		}
	}
}
