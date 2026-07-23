using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class OrdersMap : ClassMap<Order>
	{
		public OrdersMap()
		{
			Table("Orders");
			Id(x => x.OrderId).GeneratedBy.Identity().Column("OrderID");
			Map(x => x.CustomerId).Column("CustomerID");
			Map(x => x.EmployeeId).Column("EmployeeID");
			Map(x => x.ShipVia).Column("ShipVia");

			References(x => x.Customer).Column("CustomerID");
			References(x => x.Employee).Column("EmployeeID");
			References(x => x.ShipViaNavigation).Column("ShipVia");

			Map(x => x.OrderDate).Column("OrderDate");
			Map(x => x.RequiredDate).Column("RequiredDate");
			Map(x => x.ShippedDate).Column("ShippedDate");
			Map(x => x.Freight).Column("Freight");
			Map(x => x.ShipName).Column("ShipName");
			Map(x => x.ShipAddress).Column("ShipAddress");
			Map(x => x.ShipCity).Column("ShipCity");
			Map(x => x.ShipRegion).Column("ShipRegion");
			Map(x => x.ShipPostalCode).Column("ShipPostalCode");
			Map(x => x.ShipCountry).Column("ShipCountry");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();

			HasMany(x => x.OrderDetails);
		}
	}
}
