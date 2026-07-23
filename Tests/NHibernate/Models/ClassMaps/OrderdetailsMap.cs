using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class OrderdetailsMap : ClassMap<OrderDetail>
	{
		public OrderdetailsMap()
		{
			Table("Order Details");
			CompositeId().KeyProperty(x => x.OrderId, "OrderID")
				.KeyProperty(x => x.ProductId, "ProductID");
			References(x => x.Order).Column("OrderID").Not.Insert().Not.Update();
			References(x => x.Product).Column("ProductID").Not.Insert().Not.Update();
			Map(x => x.UnitPrice).Column("UnitPrice").Not.Nullable();
			Map(x => x.Quantity).Column("Quantity").Not.Nullable();
			Map(x => x.Discount).Column("Discount").Not.Nullable();
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
