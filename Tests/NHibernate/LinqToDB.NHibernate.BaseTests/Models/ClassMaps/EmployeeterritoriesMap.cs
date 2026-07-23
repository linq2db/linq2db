using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class EmployeeterritoriesMap : ClassMap<EmployeeTerritory>
	{
		public EmployeeterritoriesMap()
		{
			Table("EmployeeTerritories");
			CompositeId().KeyProperty(x => x.EmployeeId, "EmployeeID")
				.KeyProperty(x => x.TerritoryId, "TerritoryID");
			References(x => x.Employee).Column("EmployeeID");
			References(x => x.Territory).Column("TerritoryID");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
