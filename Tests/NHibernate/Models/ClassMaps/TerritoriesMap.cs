using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
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
