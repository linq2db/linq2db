namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class OrderDetail : BaseEntity
	{
		public int OrderId { get; set; }
		public int ProductId { get; set; }
		public decimal UnitPrice { get; set; }
		public short Quantity { get; set; }
		public float Discount { get; set; }

		public Order Order { get; set; } = null!;
		public Product Product { get; set; } = null!;
	}
}
