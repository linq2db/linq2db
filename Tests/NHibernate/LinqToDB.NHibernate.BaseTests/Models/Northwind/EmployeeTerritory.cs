namespace LinqToDB.NHibernateExtension.BaseTests.Models.Northwind
{
	public class EmployeeTerritory : BaseEntity
	{
		public virtual int EmployeeId { get; set; }
		public virtual string TerritoryId { get; set; } = null!;

		public virtual Employee Employee { get; set; } = null!;
		public virtual Territory Territory { get; set; } = null!;

		protected bool Equals(EmployeeTerritory other)
		{
			return EmployeeId == other.EmployeeId && TerritoryId == other.TerritoryId;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((EmployeeTerritory) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (EmployeeId * 397) ^ TerritoryId.GetHashCode();
			}
		}
	}
}
