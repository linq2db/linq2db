using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
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
			public string? OrderName { get; set; }

			[Association(ThisKey = nameof(OrderId), OtherKey = nameof(OrderDetail.OrderId))]
			public IEnumerable<OrderDetail> Details { get; set; } = null!;
		}

		public class OrderType1 : Order;
		public class OrderType2 : Order;

		public class OrderDetail
		{
			[PrimaryKey]
			public int OrderDetailId { get; set; }

			public string? Title { get; set; }

			public int OrderId { get; set; }
		}

		[Test]
		public void TestLoadWithDiscriminator([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Order>(new []{new OrderType1() { OrderId = 1, OrderName = "Order1" }}))
			using (db.CreateLocalTable<OrderDetail>(new []{new OrderDetail() { OrderDetailId = 100, OrderId = 1, Title = "Detail1" }}))
			{
				var interceptor = new CountCommandsInterceptor();
				db.AddInterceptor(interceptor);
				var query = db.GetTable<Order>().LoadWith(o => o.Details).Where(o => o.OrderId == 1);
				var order = query.FirstOrDefault();

				// A First/Single over an eager LoadWith batches the main query and the eager query into one combined
				// multi-result-set command — when the UseCombinedCommands policy switch is on (off by default in 6.x) AND
				// the provider supports multi-statement batches + multiple result sets. Otherwise, and on remote contexts,
				// they still run as two separate commands.
				Assert.That(interceptor.Count, Is.EqualTo(db.UsesCombinedEagerLoading() ? 1 : 2));
			}
		}

		sealed class CountCommandsInterceptor : CommandInterceptor
		{
			public int Count { get; private set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Count++;
				return command;
			}
		}
	}
}
