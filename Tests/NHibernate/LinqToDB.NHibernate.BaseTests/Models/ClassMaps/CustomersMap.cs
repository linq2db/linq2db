using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class CustomersMap : ClassMap<Customer>
	{
		public CustomersMap()
		{
			Table("Customers");
			Id(x => x.CustomerId).GeneratedBy.Assigned().Column("CustomerID");
			Map(x => x.CompanyName).Column("CompanyName").Not.Nullable();
			Map(x => x.ContactName).Column("ContactName");
			Map(x => x.ContactTitle).Column("ContactTitle");
			Map(x => x.Address).Column("Address");
			Map(x => x.City).Column("City");
			Map(x => x.Region).Column("Region");
			Map(x => x.PostalCode).Column("PostalCode");
			Map(x => x.Country).Column("Country");
			Map(x => x.Phone).Column("Phone");
			Map(x => x.Fax).Column("Fax");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
			
			HasMany(x => x.Orders);
		}
	}
}
