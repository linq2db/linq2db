using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class TerritoriesMap : ClassMap<Territory>
	{
		public TerritoriesMap()
		{
			Table("Territories");
			Id(x => x.TerritoryId).GeneratedBy.Assigned().Column("TerritoryID");
			References(x => x.Region).Column("RegionID");
			Map(x => x.TerritoryDescription).Column("TerritoryDescription").Not.Nullable();
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
