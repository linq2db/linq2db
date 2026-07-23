using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class SuppliersMap : ClassMap<Supplier>
	{
		public SuppliersMap()
		{
			Table("Suppliers");
			Id(x => x.SupplierId).GeneratedBy.Identity().Column("SupplierID");
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
			Map(x => x.HomePage).Column("HomePage");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
