namespace LinqToDB.NHibernateExtension.BaseTests.Models.Northwind
{
	public class CustomerCustomerDemo : BaseEntity
	{
		public virtual string CustomerId     { get; set; } = null!;
		public virtual string CustomerTypeId { get; set; } = null!;

		public virtual Customer Customer                 { get; set; } = null!;
		public virtual CustomerDemographics CustomerType { get; set; } = null!;

		protected bool Equals(CustomerCustomerDemo other)
		{
			return CustomerId == other.CustomerId && CustomerTypeId == other.CustomerTypeId;
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((CustomerCustomerDemo) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (CustomerId.GetHashCode() * 397) ^ CustomerTypeId.GetHashCode();
			}
		}
	}
}
