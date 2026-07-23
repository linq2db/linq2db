using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class RegionMap : ClassMap<Region>
	{
		public RegionMap()
		{
			Table("Region");
			Id(x => x.RegionId).GeneratedBy.Identity().Column("RegionID");
			Map(x => x.RegionDescription).Column("RegionDescription").Not.Nullable();
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
