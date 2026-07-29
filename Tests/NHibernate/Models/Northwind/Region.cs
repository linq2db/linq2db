using System.Collections.Generic;

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public partial class Region : BaseEntity
	{
		public Region()
		{
			Territories = new HashSet<Territory>();
		}

		public virtual int RegionId { get; set; }
		public virtual string RegionDescription { get; set; } = null!;

		public virtual ICollection<Territory> Territories { get; set; }
	}
}
