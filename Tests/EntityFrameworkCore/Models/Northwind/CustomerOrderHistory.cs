
namespace LinqToDB.EntityFrameworkCore.BaseTests.Models.Northwind
{
	public class CustomerOrderHistory : BaseEntity
	{
		public string ProductName { get; set; } = null!;

		public int Total { get; set; }
	}
}
