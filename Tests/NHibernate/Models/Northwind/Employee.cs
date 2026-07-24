using System;
using System.Collections.Generic;

namespace LinqToDB.NHibernate.Tests.Models.Northwind
{
	public class Employee : BaseEntity
	{
		public Employee()
		{
			EmployeeTerritories = new HashSet<EmployeeTerritory>();
			InverseReportsToNavigation = new HashSet<Employee>();
			Orders = new HashSet<Order>();
		}

		public virtual int       EmployeeId      { get; set; }
		public virtual string    LastName        { get; set; } = null!;
		public virtual string    FirstName       { get; set; } = null!;
		public virtual string?   Title           { get; set; }
		public virtual string?   TitleOfCourtesy { get; set; }
		public virtual DateTime? BirthDate       { get; set; }
		public virtual DateTime? HireDate        { get; set; }
		public virtual string?   Address         { get; set; }
		public virtual string?   City            { get; set; }
		public virtual string?   Region          { get; set; }
		public virtual string?   PostalCode      { get; set; }
		public virtual string?   Country         { get; set; }
		public virtual string?   HomePhone       { get; set; }
		public virtual string?   Extension       { get; set; }
		public virtual byte[]?   Photo           { get; set; }
		public virtual string?   Notes           { get; set; }
		public virtual int?      ReportsTo       { get; set; }
		public virtual string?   PhotoPath       { get; set; }

		public virtual Employee? ReportsToNavigation { get; set; }
		public virtual ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }
		public virtual ICollection<Employee> InverseReportsToNavigation { get; set; }
		public virtual ICollection<Order> Orders { get; set; }
	}
}
