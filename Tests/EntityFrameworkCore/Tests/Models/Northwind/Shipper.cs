using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class Shipper : BaseEntity
	{
		public Shipper()
		{
			Orders = new HashSet<Order>();
		}

		public int ShipperId { get; set; }
		public string CompanyName { get; set; } = null!;
		public string? Phone { get; set; }

		public ICollection<Order> Orders { get; set; }
	}
}
