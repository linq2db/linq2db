using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class EmployeeterritoriesMap : ClassMap<EmployeeTerritory>
	{
		public EmployeeterritoriesMap()
		{
			Table("EmployeeTerritories");
			CompositeId().KeyProperty(x => x.EmployeeId, "EmployeeID")
				.KeyProperty(x => x.TerritoryId, "TerritoryID");
			References(x => x.Employee).Column("EmployeeID").Not.Insert().Not.Update();
			References(x => x.Territory).Column("TerritoryID").Not.Insert().Not.Update();
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
