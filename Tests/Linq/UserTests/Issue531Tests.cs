using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue531Tests : TestBase
	{
		[Table(Name = "Employees")]
		public class EmployeeWithList : Northwind.EntityBase<int>
		{
			[PrimaryKey, Identity] public int EmployeeID;
			[Column, NotNull] public string LastName;
			[Column, NotNull] public string FirstName;
			[Column] public string Title;
			[Column] public string TitleOfCourtesy;
			[Column] public DateTime? BirthDate;
			[Column] public DateTime? HireDate;
			[Column] public string Address;
			[Column] public string City;
			[Column] public string Region;
			[Column] public string PostalCode;
			[Column] public string Country;
			[Column] public string HomePhone;
			[Column] public List<string> Extension;
			[Column] public Binary Photo;
			[Column] public string Notes;
			[Column] public int? ReportsTo;
			[Column] public string PhotoPath;

			[Association(ThisKey = "EmployeeID", OtherKey = "ReportsTo")] public List<Northwind.Employee> Employees;
			[Association(ThisKey = "EmployeeID", OtherKey = "EmployeeID")] public List<Northwind.EmployeeTerritory> EmployeeTerritories;
			[Association(ThisKey = "EmployeeID", OtherKey = "EmployeeID")] public List<Northwind.Order> Orders;
			[Association(ThisKey = "ReportsTo", OtherKey = "EmployeeID")] public Northwind.Employee ReportsToEmployee;

			public Northwind.Employee Employee2 { get; set; }
			public Northwind.Order Order { get; set; }
			public Northwind.EmployeeTerritory EmployeeTerritory { get; set; }

			protected override int Key
			{
				get { return EmployeeID; }
			}
		}

		[Test]
		public void Test([NorthwindDataContext] string context)
		{
			MappingSchema.Default.SetConverter<List<string>, string>((obj) =>
			{
				if (obj == null)
					return null;
				return string.Join(";", obj);
			});
			MappingSchema.Default.SetConverter<List<string>, DataParameter>((obj) =>
			{
				if (obj == null)
					return new DataParameter();
				return new DataParameter { Value = string.Join(";", obj) };
			});
			MappingSchema.Default.SetConverter<string, List<string>>((txt) =>
			{
				if (string.IsNullOrEmpty(txt))
					return null;
				return txt.Split(';').ToList();
			});

			var names = new List<string>() { "Nancy", "Andrew" };

			var ext = new List<string>() { "5467" };

			using (var db = new NorthwindDB(context))
			{

				var jj = from e in db.GetTable<EmployeeWithList>()
						 where e.Extension == ext
						 select e;

			    var res1 = jj.ToList();

				var zz =
					from e in db.Employee
					where names.Contains(e.FirstName)
					select e;

				var res2 = zz.ToList();

				Assert.That(res2.Count, Is.EqualTo(2));
			}
		}
	}
}
