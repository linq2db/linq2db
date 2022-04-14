using System;
using System.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2619Tests : TestBase
	{
		[Test]
		public void OrderByUnionTest ([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB (context))
			{
				var products = db.Product
						.OrderBy (c => c.ProductName)
					;

				var orders = db.Order;

				var orderDetails =
					db.OrderDetail
						.Join (products,
							orderDetail => orderDetail.ProductID, product => product.ProductID,
							(orderDetail, product) => orderDetail);

				var productOrders = orderDetails
					.Join (db.Order, orderDetail => orderDetail.OrderID, order => order.OrderID,
						(orderDetail, order) => order);

				var union = orders
						.Union (productOrders)
					;

				Assert.DoesNotThrow (() =>
				{
					var list = union.ToList ();
				});
			}
		}
	}
}
