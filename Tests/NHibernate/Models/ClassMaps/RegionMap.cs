using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
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
