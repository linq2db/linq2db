using System;
using System.Collections.Generic;
using System.Data.Linq;

using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Models
{
	public static partial class Northwind
	{
		public abstract class EntityBase<T>
		{
			protected abstract T Key { get; }

			public override bool Equals(object? obj)
			{
				if (obj == null)
					return false;

				return GetType() == obj.GetType() && Key!.Equals(((EntityBase<T>)obj).Key);
			}

			public override int GetHashCode()
			{
				return Key!.GetHashCode();
			}
		}

		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int     CategoryID;
			[Column, NotNull]      public string  CategoryName = null!;
			[Column]               public string? Description;
			[Column]               public Binary? Picture;

			[Association(ThisKey="CategoryID", OtherKey="CategoryID")]
			public List<Product> Products = null!;
		}

		[Table("CustomerCustomerDemo")]
		public class CustomerCustomerDemo
		{
			[PrimaryKey, NotNull] public string CustomerID = null!;
			[PrimaryKey, NotNull] public string CustomerTypeID = null!;

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public CustomerDemographic CustomerDemographics = null!;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public Customer Customers = null!;
		}

		[Table("CustomerDemographics")]
		public class CustomerDemographic
		{
			[PrimaryKey, NotNull] public string  CustomerTypeID = null!;
			[Column]              public string? CustomerDesc;

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos = null!;
		}

		[Table(Name="Customers")]
		public class Customer : EntityBase<string>
		{
			[PrimaryKey]      public string  CustomerID = null!;
			[Column, NotNull] public string  CompanyName = null!;
			[Column]          public string? ContactName;
			[Column]          public string? ContactTitle;
			[Column]          public string? Address;
			[Column]          public string? City;
			[Column]          public string? Region;
			[Column]          public string? PostalCode;
			[Column]          public string? Country;
			[Column]          public string? Phone;
			[Column]          public string? Fax;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos = null!;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<Order> Orders = null!;

			protected override string Key => CustomerID;
		}

		[Table(Name="Employees")]
		public class Employee : EntityBase<int>
		{
			[PrimaryKey, Identity] public int       EmployeeID;
			[Column, NotNull]      public string    LastName = null!;
			[Column, NotNull]      public string    FirstName = null!;
			[Column]               public string?   Title;
			[Column]               public string?   TitleOfCourtesy;
			[Column]               public DateTime? BirthDate;
			[Column]               public DateTime? HireDate;
			[Column]               public string?   Address;
			[Column]               public string?   City;
			[Column]               public string?   Region;
			[Column]               public string?   PostalCode;
			[Column]               public string?   Country;
			[Column]               public string?   HomePhone;
			[Column]               public string?   Extension;
			[Column]               public Binary?   Photo;
			[Column]               public string?   Notes;
			[Column]               public int?      ReportsTo;
			[Column]               public string?   PhotoPath;

			[Association(ThisKey="EmployeeID", OtherKey="ReportsTo")]  public List<Employee>          Employees = null!;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<EmployeeTerritory> EmployeeTerritories = null!;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<Order>             Orders = null!;
			[Association(ThisKey="ReportsTo",  OtherKey="EmployeeID")] public Employee?               ReportsToEmployee;

			public Employee?          Employee2         { get; set; }
			public Order?             Order             { get; set; }
			public EmployeeTerritory? EmployeeTerritory { get; set; }

			protected override int Key => EmployeeID;
		}

		[Table("EmployeeTerritories")]
		public class EmployeeTerritory
		{
			[PrimaryKey]          public int    EmployeeID;
			[PrimaryKey, NotNull] public string TerritoryID = null!;

			[Association(ThisKey="EmployeeID",  OtherKey="EmployeeID")]  public Employee  Employee = null!;
			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")] public Territory Territory = null!;
		}

		[Table(Name="Order Details")]
		public class OrderDetail
		{
			[PrimaryKey] public int     OrderID;
			[PrimaryKey] public int     ProductID;
			[Column]     public decimal UnitPrice;
			[Column]     public short   Quantity;
			[Column]     public double  Discount;

			[Association(ThisKey="OrderID",   OtherKey="OrderID")]   public Order   Order = null!;
			[Association(ThisKey="ProductID", OtherKey="ProductID")] public Product Product = null!;
		}

		[Table(Name="Orders")]
		public class Order : EntityBase<int>
		{
			[PrimaryKey, Identity] public int       OrderID;
			[Column]               public string?   CustomerID;
			[Column]               public int?      EmployeeID;
			[Column]               public DateTime? OrderDate;
			[Column]               public DateTime? RequiredDate;
			[Column]               public DateTime? ShippedDate;
			[Column]               public int?      ShipVia;
			[Column]               public decimal   Freight;
			[Column]               public string?   ShipName;
			[Column]               public string?   ShipAddress;
			[Column]               public string?   ShipCity;
			[Column]               public string?   ShipRegion;
			[Column]               public string?   ShipPostalCode;
			[Column]               public string?   ShipCountry;

			[Association(ThisKey="OrderID",    OtherKey="OrderID")]                     public List<OrderDetail> OrderDetails = null!;
			[Association(ThisKey="CustomerID", OtherKey="CustomerID", CanBeNull=false)] public Customer?         Customer;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")]                  public Employee?         Employee;
			[Association(ThisKey="ShipVia",    OtherKey="ShipperID")]                   public Shipper?          Shipper;

			public OrderDetail? OrderDetail { get; set; }

			protected override int Key => OrderID;
		}

		[Table(Name="Products")]
		[InheritanceMapping(Code=true,  Type=typeof(DiscontinuedProduct))]
		[InheritanceMapping(Code=false, Type=typeof(ActiveProduct))]
		public abstract class Product
		{
			[PrimaryKey, Identity]         public int      ProductID;
			[Column, NotNull]              public string   ProductName = null!;
			[Column]                       public int?     SupplierID;
			[Column]                       public int?     CategoryID;
			[Column]                       public string?  QuantityPerUnit;
			[Column]                       public decimal? UnitPrice;
			[Column]                       public short?   UnitsInStock;
			[Column]                       public short?   UnitsOnOrder;
			[Column]                       public short?   ReorderLevel;
			[Column(IsDiscriminator=true)] public bool     Discontinued;

			[Association(ThisKey="ProductID",  OtherKey="ProductID")]  public List<OrderDetail> OrderDetails = null!;
			[Association(ThisKey="CategoryID", OtherKey="CategoryID")] public Category?         Category;
			[Association(ThisKey="SupplierID", OtherKey="SupplierID")] public Supplier?         Supplier;
		}

		public class ActiveProduct       : Product {}
		public class DiscontinuedProduct : Product {}

		[Table(Name="Region")]
		public class Region
		{
			[PrimaryKey]      public int    RegionID;
			[Column, NotNull] public string RegionDescription = null!;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public List<Territory> Territories = null!;
		}

		[Table(Name="Shippers")]
		public class Shipper
		{
			[PrimaryKey, Identity] public int     ShipperID;
			[Column, NotNull]      public string  CompanyName = null!;
			[Column]               public string? Phone;

			[Association(ThisKey="ShipperID", OtherKey="ShipVia")]
			public List<Order> Orders = null!;
		}

		[Table(Name="Suppliers")]
		public class Supplier
		{
			[PrimaryKey, Identity] public int     SupplierID;
			[Column, NotNull]      public string  CompanyName = null!;
			[Column]               public string? ContactName;
			[Column]               public string? ContactTitle;
			[Column]               public string? Address;
			[Column]               public string? City;
			[Column]               public string? Region;
			[Column]               public string? PostalCode;
			[Column]               public string? Country;
			[Column]               public string? Phone;
			[Column]               public string? Fax;
			[Column]               public string? HomePage;

			[Association(ThisKey="SupplierID", OtherKey="SupplierID")]
			public List<Product> Products = null!;
		}

		[Table(Name="Territories")]
		public class Territory
		{
			[PrimaryKey, NotNull] public string TerritoryID = null!;
			[Column, NotNull]     public string TerritoryDescription = null!;
			[Column]              public int    RegionID;

			public EmployeeTerritory? EmployeeTerritory { get; set; }

			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")]
			public List<EmployeeTerritory> EmployeeTerritories = null!;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public Region Region = null!;
		}
	}
}
