using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class CustomerCustomerDemoMap : ClassMap<CustomerCustomerDemo>
	{
		public CustomerCustomerDemoMap()
		{
			Table("CustomerCustomerDemo");
			CompositeId().KeyProperty(x => x.CustomerId, "CustomerID")
				.KeyProperty(x => x.CustomerTypeId, "CustomerTypeID");
			//References(x => x.Customers).Column("CustomerID");
			//References(x => x.Customerdemographics).Column("CustomerTypeID");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
