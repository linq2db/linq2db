using System.Collections.Generic;

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class Shipper : BaseEntity
	{
		public Shipper()
		{
			Orders = new HashSet<Order>();
		}

		public virtual int ShipperId { get; set; }
		public virtual string CompanyName { get; set; } = null!;
		public virtual string? Phone { get; set; }

		public virtual ICollection<Order> Orders { get; set; }
	}
}
