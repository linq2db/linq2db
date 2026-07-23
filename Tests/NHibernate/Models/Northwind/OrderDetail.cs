namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class OrderDetail : BaseEntity
	{
		public virtual int OrderId { get; set; }
		public virtual int ProductId { get; set; }
		public virtual decimal UnitPrice { get; set; }
		public virtual short Quantity { get; set; }
		public virtual float Discount { get; set; }

		public virtual Order Order { get; set; } = null!;
		public virtual Product Product { get; set; } = null!;

		protected bool Equals(OrderDetail other)
		{
			return OrderId == other.OrderId && ProductId == other.ProductId;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((OrderDetail) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (OrderId * 397) ^ ProductId;
			}
		}
	}
}
