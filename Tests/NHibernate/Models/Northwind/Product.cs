using System.Collections.Generic;

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class Product : BaseEntity
	{
		public Product()
		{
			OrderDetails = new HashSet<OrderDetail>();
		}

		public virtual int      ProductId       { get; set; }
		public virtual string   ProductName     { get; set; } = null!;
		public virtual int?     SupplierId      { get; set; }
		public virtual int?     CategoryId      { get; set; }
		public virtual string?  QuantityPerUnit { get; set; }
		public virtual decimal? UnitPrice       { get; set; }
		public virtual short?   UnitsInStock    { get; set; }
		public virtual short?   UnitsOnOrder    { get; set; }
		public virtual short?   ReorderLevel    { get; set; }
		public virtual bool Discontinued    { get; set; }

		public virtual Category? Category { get; set; }
		public virtual Supplier? Supplier { get; set; }
		public virtual ICollection<OrderDetail> OrderDetails { get; set; }
	}
}
