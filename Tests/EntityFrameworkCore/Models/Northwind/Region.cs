using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public partial class Region : BaseEntity
	{
		public Region()
		{
			Territories = new HashSet<Territory>();
		}

		public int RegionId { get; set; }
		public string RegionDescription { get; set; } = null!;

		public ICollection<Territory> Territories { get; set; }
	}
}
