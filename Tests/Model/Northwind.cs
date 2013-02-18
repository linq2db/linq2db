using System;
using System.Collections.Generic;
using System.Data.Linq;

using LinqToDB;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class Northwind
	{
		public abstract class EntityBase<T>
		{
			protected abstract T Key { get; }

			public override bool Equals(object obj)
			{
				return GetType() == obj.GetType() && Key.Equals(((EntityBase<T>)obj).Key);
			}

			public override int GetHashCode()
			{
				return Key.GetHashCode();
			}
		}

		[Table(Name="Categories")]
		public class Category
		{
			[PrimaryKey, NonUpdatable] public int    CategoryID;
			[NotNull]                  public string CategoryName;
			                           public string Description;
			                           public Binary Picture;

			[Association(ThisKey="CategoryID", OtherKey="CategoryID")]
			public List<Product> Products;
		}

		[Table(Name="CustomerCustomerDemo")]
		public class CustomerCustomerDemo
		{
			[PrimaryKey, NotNull] public string CustomerID;
			[PrimaryKey, NotNull] public string CustomerTypeID;

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public CustomerDemographic CustomerDemographics;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public Customer Customers;
		}

		[Table(Name="CustomerDemographics")]
		public class CustomerDemographic
		{
			[PrimaryKey, NotNull] public string CustomerTypeID;
			                      public string CustomerDesc;
			
			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos;
		}

		[Table(Name="Customers")]
		public class Customer : EntityBase<string>
		{
			[PrimaryKey] public string CustomerID;
			[NotNull]    public string CompanyName;
			             public string ContactName;
			             public string ContactTitle;
			             public string Address;
			             public string City;
			             public string Region;
			             public string PostalCode;
			             public string Country;
			             public string Phone;
			             public string Fax;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<Order> Orders;

			protected override string Key
			{
				get { return CustomerID; }
			}
		}

		[Table(Name="Employees")]
		public class Employee : EntityBase<int>
		{
			[PrimaryKey, NonUpdatable] public int       EmployeeID;
			[NotNull]                  public string    LastName;
			[NotNull]                  public string    FirstName;
			                           public string    Title;
			                           public string    TitleOfCourtesy;
			                           public DateTime? BirthDate;
			                           public DateTime? HireDate;
			                           public string    Address;
			                           public string    City;
			                           public string    Region;
			                           public string    PostalCode;
			                           public string    Country;
			                           public string    HomePhone;
			                           public string    Extension;
			                           public Binary    Photo;
			                           public string    Notes;
			                           public int?      ReportsTo;
			                           public string    PhotoPath;

			[Association(ThisKey="EmployeeID", OtherKey="ReportsTo")]  public List<Employee>          Employees;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<EmployeeTerritory> EmployeeTerritories;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<Order>             Orders;
			[Association(ThisKey="ReportsTo",  OtherKey="EmployeeID")] public Employee                ReportsToEmployee;

			//[MapIgnore]
			protected override int Key
			{
				get { return EmployeeID; }
			}
		}

		[Table(Name="EmployeeTerritories")]
		public class EmployeeTerritory
		{
			[PrimaryKey]          public int    EmployeeID;
			[PrimaryKey, NotNull] public string TerritoryID;

			[Association(ThisKey="EmployeeID",  OtherKey="EmployeeID")]  public Employee  Employee;
			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")] public Territory Territory;
		}

		[Table(Name="Order Details")]
		public class OrderDetail
		{
			[PrimaryKey] public int     OrderID;
			[PrimaryKey] public int     ProductID;
			             public decimal UnitPrice;
			             public short   Quantity;
			             public float   Discount;

			[Association(ThisKey="OrderID",   OtherKey="OrderID")]   public Order   Order;
			[Association(ThisKey="ProductID", OtherKey="ProductID")] public Product Product;
		}

		[Table(Name="Orders")]
		public class Order : EntityBase<int>
		{
			[PrimaryKey, NonUpdatable] public int       OrderID;
			                           public string    CustomerID;
			                           public int?      EmployeeID;
			                           public DateTime? OrderDate;
			                           public DateTime? RequiredDate;
			                           public DateTime? ShippedDate;
			                           public int?      ShipVia;
			                           public decimal   Freight;
			                           public string    ShipName;
			                           public string    ShipAddress;
			                           public string    ShipCity;
			                           public string    ShipRegion;
			                           public string    ShipPostalCode;
			                           public string    ShipCountry;

			[Association(ThisKey="OrderID",    OtherKey="OrderID")]                     public List<OrderDetail> OrderDetails;
			[Association(ThisKey="CustomerID", OtherKey="CustomerID", CanBeNull=false)] public Customer          Customer;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")]                  public Employee          Employee;
			[Association(ThisKey="ShipVia",    OtherKey="ShipperID")]                   public Shipper           Shipper;

			protected override int Key
			{
				get { return OrderID; }
			}
		}

		[Table(Name="Products")]
		[InheritanceMapping(Code=true,  Type=typeof(DiscontinuedProduct))]
		[InheritanceMapping(Code=false, Type=typeof(ActiveProduct))]
		public abstract class Product
		{
			[PrimaryKey, NonUpdatable]                  public int      ProductID;
			[NotNull]                                   public string   ProductName;
			                                            public int?     SupplierID;
			                                            public int?     CategoryID;
			                                            public string   QuantityPerUnit;
			                                            public decimal? UnitPrice;
			                                            public short?   UnitsInStock;
			                                            public short?   UnitsOnOrder;
			                                            public short?   ReorderLevel;
			[MapField(IsInheritanceDiscriminator=true)] public bool     Discontinued;

			[Association(ThisKey="ProductID",  OtherKey="ProductID")]  public List<OrderDetail> OrderDetails;
			[Association(ThisKey="CategoryID", OtherKey="CategoryID")] public Category          Category;
			[Association(ThisKey="SupplierID", OtherKey="SupplierID")] public Supplier          Supplier;
		}

		public class ActiveProduct       : Product {}
		public class DiscontinuedProduct : Product {}

		[Table(Name="Region")]
		public class Region
		{
			[PrimaryKey] public int    RegionID;
			[NotNull]    public string RegionDescription;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public List<Territory> Territories;
		}

		[Table(Name="Shippers")]
		public class Shipper
		{
			[PrimaryKey, NonUpdatable] public int    ShipperID;
			[NotNull]                  public string CompanyName;
			                           public string Phone;

			[Association(ThisKey="ShipperID", OtherKey="ShipVia")]
			public List<Order> Orders;
		}

		[Table(Name="Suppliers")]
		public class Supplier
		{
			[PrimaryKey, NonUpdatable] public int    SupplierID;
			[NotNull]                  public string CompanyName;
			                           public string ContactName;
			                           public string ContactTitle;
			                           public string Address;
			                           public string City;
			                           public string Region;
			                           public string PostalCode;
			                           public string Country;
			                           public string Phone;
			                           public string Fax;
			                           public string HomePage;

			[Association(ThisKey="SupplierID", OtherKey="SupplierID")]
			public List<Product> Products;
		}

		[Table(Name="Territories")]
		public class Territory
		{
			[PrimaryKey, NotNull] public string TerritoryID;
			[NotNull]             public string TerritoryDescription;
			                      public int    RegionID;

			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")]
			public List<EmployeeTerritory> EmployeeTerritories;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public Region Region;
		}
	}
}
