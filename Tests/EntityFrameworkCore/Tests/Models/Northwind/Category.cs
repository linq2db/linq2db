using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class Category : BaseEntity
	{
		public Category()
		{
			Products = new HashSet<Product>();
		}

		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = null!;
		public string? Description { get; set; }
		public byte[]? Picture { get; set; }

		public ICollection<Product> Products { get; set; }
	}
}
