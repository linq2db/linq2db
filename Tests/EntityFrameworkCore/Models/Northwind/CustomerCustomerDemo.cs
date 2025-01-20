namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class CustomerCustomerDemo : BaseEntity
	{
		public string CustomerId     { get; set; } = null!;
		public string CustomerTypeId { get; set; } = null!;

		public Customer             Customer     { get; set; } = null!;
		public CustomerDemographics CustomerType { get; set; } = null!;
	}
}
