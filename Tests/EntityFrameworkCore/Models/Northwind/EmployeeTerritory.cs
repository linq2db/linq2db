namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class EmployeeTerritory : BaseEntity
	{
		public int EmployeeId { get; set; }
		public string TerritoryId { get; set; } = null!;

		public Employee Employee { get; set; } = null!;
		public Territory Territory { get; set; } = null!;
	}
}
