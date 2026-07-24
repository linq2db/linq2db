using System.Collections.Generic;

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class Customer : BaseEntity
	{
		public Customer()
		{
			CustomerCustomerDemo = new HashSet<CustomerCustomerDemo>();
			Orders = new HashSet<Order>();
		}

		public virtual string  CustomerId   { get; set; } = null!;
		public virtual string  CompanyName  { get; set; } = null!;
		public virtual string? ContactName  { get; set; }
		public virtual string? ContactTitle { get; set; }
		public virtual string? Address      { get; set; }
		public virtual string? City         { get; set; }
		public virtual string? Region       { get; set; }
		public virtual string? PostalCode   { get; set; }
		public virtual string? Country      { get; set; }
		public virtual string? Phone        { get; set; }
		public virtual string? Fax          { get; set; }

		public virtual ICollection<CustomerCustomerDemo> CustomerCustomerDemo { get; set; }
		public virtual ICollection<Order> Orders { get; set; }
	}
}
