
namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class CustomerOrderHistory : BaseEntity
	{
		public virtual string ProductName { get; set; } = null!;
		public virtual int Total { get; set; }
	}
}
