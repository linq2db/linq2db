﻿using System;
using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Northwind
{
	public class Employee : BaseEntity
	{
		public Employee()
		{
			EmployeeTerritories = new HashSet<EmployeeTerritory>();
			InverseReportsToNavigation = new HashSet<Employee>();
			Orders = new HashSet<Order>();
		}

		public int       EmployeeId      { get; set; }
		public string    LastName        { get; set; } = null!;
		public string    FirstName       { get; set; } = null!;
		public string?   Title           { get; set; }
		public string?   TitleOfCourtesy { get; set; }
		public DateTime? BirthDate       { get; set; }
		public DateTime? HireDate        { get; set; }
		public string?   Address         { get; set; }
		public string?   City            { get; set; }
		public string?   Region          { get; set; }
		public string?   PostalCode      { get; set; }
		public string?   Country         { get; set; }
		public string?   HomePhone       { get; set; }
		public string?   Extension       { get; set; }
		public byte[]?   Photo           { get; set; }
		public string?   Notes           { get; set; }
		public int?      ReportsTo       { get; set; }
		public string?   PhotoPath       { get; set; }

		public Employee? ReportsToNavigation { get; set; }
		public ICollection<EmployeeTerritory> EmployeeTerritories { get; set; }
		public ICollection<Employee> InverseReportsToNavigation { get; set; }
		public ICollection<Order> Orders { get; set; }
	}
}
