using System.Collections.Generic;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.Northwind
{
	public class Territory : BaseEntity
	{
		public Territory()
		{
			EmployeeTerritories = new HashSet<EmployeeTerritory>();
		}

		public virtual string TerritoryId          { get; set; } = null!;
		public virtual string TerritoryDescription { get; set; } = null!;
		public virtual int    RegionId             { get; set; }

		public virtual Region Region { get; set; } = null!;
		public virtual ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }
	}
}
