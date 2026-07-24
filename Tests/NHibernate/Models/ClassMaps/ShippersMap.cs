using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class ShippersMap : ClassMap<Shipper>
	{
		public ShippersMap()
		{
			Table("Shippers");
			Id(x => x.ShipperId).GeneratedBy.Native().Column("ShipperID");
			Map(x => x.CompanyName).Column("CompanyName").Not.Nullable();
			Map(x => x.Phone).Column("Phone");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
