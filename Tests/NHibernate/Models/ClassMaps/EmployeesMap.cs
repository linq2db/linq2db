using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
{
	public class EmployeesMap : ClassMap<Employee>
	{
		public EmployeesMap()
		{
			Table("Employees");
			Id(x => x.EmployeeId).GeneratedBy.Native().Column("EmployeeID");
			References(x => x.ReportsToNavigation).Column("ReportsTo");
			Map(x => x.LastName).Column("LastName").Not.Nullable();
			Map(x => x.FirstName).Column("FirstName").Not.Nullable();
			Map(x => x.Title).Column("Title");
			Map(x => x.TitleOfCourtesy).Column("TitleOfCourtesy");
			Map(x => x.BirthDate).Column("BirthDate");
			Map(x => x.HireDate).Column("HireDate");
			Map(x => x.Address).Column("Address");
			Map(x => x.City).Column("City");
			Map(x => x.Region).Column("Region");
			Map(x => x.PostalCode).Column("PostalCode");
			Map(x => x.Country).Column("Country");
			Map(x => x.HomePhone).Column("HomePhone");
			Map(x => x.Extension).Column("Extension");
			Map(x => x.Photo).Column("Photo");
			Map(x => x.Notes).Column("Notes");
			Map(x => x.PhotoPath).Column("PhotoPath");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
