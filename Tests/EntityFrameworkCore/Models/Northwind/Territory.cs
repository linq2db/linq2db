using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class Territory : BaseEntity
	{
		public Territory()
		{
			EmployeeTerritories = new HashSet<EmployeeTerritory>();
		}

		public string TerritoryId          { get; set; } = null!;
		public string TerritoryDescription { get; set; } = null!;
		public int    RegionId             { get; set; }

		public Region Region { get; set; } = null!;
		public ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }
	}
}
