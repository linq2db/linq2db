using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class ShippersMap : ClassMap<Shipper>
	{
		public ShippersMap()
		{
			Table("Shippers");
			Id(x => x.ShipperId).GeneratedBy.Identity().Column("ShipperID");
			Map(x => x.CompanyName).Column("CompanyName").Not.Nullable();
			Map(x => x.Phone).Column("Phone");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
