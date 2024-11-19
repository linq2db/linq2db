using System;
using System.Collections.Generic;
using System.Data.Linq;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public static class Northwind
	{
		public abstract class EntityBase<T>
			where T: notnull

		{
			protected abstract T Key { get; }

			public override bool Equals(object? obj)
			{
				if (obj == null)
					return false;

				return GetType() == obj.GetType() && Key.Equals(((EntityBase<T>)obj).Key);
			}

			public override int GetHashCode()
			{
				return Key.GetHashCode();
			}
		}

		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int     CategoryID   { get; set; }
			[Column, NotNull]      public string  CategoryName { get; set; } = null!;
			[Column]               public string? Description  { get; set; }
			[Column]               public Binary? Picture      { get; set; }

			[Association(ThisKey="CategoryID", OtherKey="CategoryID")]
			public List<Product> Products { get; set; } = null!;
		}

		[Table("CustomerCustomerDemo")]
		public class CustomerCustomerDemo
		{
			[PrimaryKey, NotNull] public string CustomerID     { get; set; } = null!;
			[PrimaryKey, NotNull] public string CustomerTypeID { get; set; } = null!;

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public CustomerDemographic CustomerDemographics { get; set; } = null!;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public Customer Customers { get; set; } = null!;
		}

		[Table("CustomerDemographics")]
		public class CustomerDemographic
		{
			[PrimaryKey, NotNull] public string  CustomerTypeID { get; set; } = null!;
			[Column]              public string? CustomerDesc   { get; set; }

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos { get; set; } = null!;
		}

		[Table(Name="Customers")]
		public class Customer : EntityBase<string>
		{
			[PrimaryKey, NotNull] public string  CustomerID = null!;
			[Column, NotNull]     public string  CompanyName = null!;
			[Column]              public string? ContactName;
			[Column]              public string? ContactTitle;
			[Column]              public string? Address;
			[Column]              public string? City;
			[Column]              public string? Region;
			[Column]              public string? PostalCode;
			[Column]              public string? Country;
			[Column]              public string? Phone;
			[Column]              public string? Fax;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos = null!;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public List<Order> Orders = null!;

			protected override string Key
			{
				get { return CustomerID; }
			}
		}

		[Table(Name="Employees")]
		public class Employee : EntityBase<int>
		{
			[PrimaryKey, Identity] public int       EmployeeID      { get; set; }
			[Column, NotNull]      public string    LastName        { get; set; } = null!;
			[Column, NotNull]      public string    FirstName       { get; set; } = null!;
			[Column]               public string?   Title           { get; set; }
			[Column]               public string?   TitleOfCourtesy { get; set; }
			[Column]               public DateTime? BirthDate       { get; set; }
			[Column]               public DateTime? HireDate        { get; set; }
			[Column]               public string?   Address         { get; set; }
			[Column]               public string?   City            { get; set; }
			[Column]               public string?   Region          { get; set; }
			[Column]               public string?   PostalCode      { get; set; }
			[Column]               public string?   Country         { get; set; }
			[Column]               public string?   HomePhone       { get; set; }
			[Column]               public string?   Extension       { get; set; }
			[Column]               public Binary?   Photo           { get; set; }
			[Column]               public string?   Notes           { get; set; }
			[Column]               public int?      ReportsTo       { get; set; }
			[Column]               public string?   PhotoPath       { get; set; }

			[Association(ThisKey="EmployeeID", OtherKey="ReportsTo")]  public List<Employee>          Employees           { get; set; } = null!;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<EmployeeTerritory> EmployeeTerritories { get; set; } = null!;
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")] public List<Order>             Orders              { get; set; } = null!;
			[Association(ThisKey="ReportsTo",  OtherKey="EmployeeID")] public Employee?               ReportsToEmployee   { get; set; }

			public Employee?          Employee2         { get; set; }
			public Order?             Order             { get; set; }
			public EmployeeTerritory? EmployeeTerritory { get; set; }

			protected override int Key
			{
				get { return EmployeeID; }
			}
		}

		[Table("EmployeeTerritories")]
		public class EmployeeTerritory
		{
			[PrimaryKey]          public int    EmployeeID  { get; set; }
			[PrimaryKey, NotNull] public string TerritoryID { get; set; } = null!;

			[Association(ThisKey="EmployeeID",  OtherKey="EmployeeID")]  public Employee   Employee  { get; set; } = null!;
			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")] public Territory? Territory { get; set; }
		}

		[Table(Name="Order Details")]
		public class OrderDetail
		{
			[PrimaryKey] public int     OrderID   { get; set; }
			[PrimaryKey] public int     ProductID { get; set; }
			[Column]     public decimal UnitPrice { get; set; }
			[Column]     public short   Quantity  { get; set; }
			[Column]     public double  Discount  { get; set; }

			[Association(ThisKey="OrderID",   OtherKey="OrderID")]   public Order   Order   { get; set; } = null!;
			[Association(ThisKey="ProductID", OtherKey="ProductID")] public Product Product { get; set; } = null!;
		}

		[Table(Name="Orders")]
		public class Order : EntityBase<int>
		{
			[PrimaryKey, Identity] public int       OrderID        { get; set; }
			[Column]               public string?   CustomerID     { get; set; }
			[Column]               public int?      EmployeeID     { get; set; }
			[Column]               public DateTime? OrderDate      { get; set; }
			[Column]               public DateTime? RequiredDate   { get; set; }
			[Column]               public DateTime? ShippedDate    { get; set; }
			[Column]               public int?      ShipVia        { get; set; }
			[Column]               public decimal   Freight        { get; set; }
			[Column]               public string?   ShipName       { get; set; }
			[Column]               public string?   ShipAddress    { get; set; }
			[Column]               public string?   ShipCity       { get; set; }
			[Column]               public string?   ShipRegion     { get; set; }
			[Column]               public string?   ShipPostalCode { get; set; }
			[Column]               public string?   ShipCountry    { get; set; }

			[Association(ThisKey="OrderID",    OtherKey="OrderID")]                     public List<OrderDetail> OrderDetails { get; set; } = null!;
			[Association(ThisKey="CustomerID", OtherKey="CustomerID", CanBeNull=false)] public Customer?         Customer     { get; set; }
			[Association(ThisKey="EmployeeID", OtherKey="EmployeeID")]                  public Employee?         Employee     { get; set; }
			[Association(ThisKey="ShipVia",    OtherKey="ShipperID")]                   public Shipper?          Shipper      { get; set; }

			public OrderDetail? OrderDetail { get; set; }

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
			[PrimaryKey, Identity]         public int      ProductID       { get; set; }
			[Column, NotNull]              public string   ProductName     { get; set; } = null!;
			[Column]                       public int?     SupplierID      { get; set; }
			[Column]                       public int?     CategoryID      { get; set; }
			[Column]                       public string?  QuantityPerUnit { get; set; }
			[Column]                       public decimal? UnitPrice       { get; set; }
			[Column]                       public short?   UnitsInStock    { get; set; }
			[Column]                       public short?   UnitsOnOrder    { get; set; }
			[Column]                       public short?   ReorderLevel    { get; set; }
			[Column(IsDiscriminator=true)] public bool     Discontinued    { get; set; }

			[Association(ThisKey="ProductID",  OtherKey="ProductID")]  public List<OrderDetail> OrderDetails { get; set; } = null!;
			[Association(ThisKey="CategoryID", OtherKey="CategoryID")] public Category?         Category     { get; set; }
			[Association(ThisKey="SupplierID", OtherKey="SupplierID")] public Supplier?         Supplier     { get; set; }
		}

		public class ActiveProduct       : Product {}
		public class DiscontinuedProduct : Product {}

		[Table(Name="Region")]
		public class Region
		{
			[PrimaryKey]      public int    RegionID          { get; set; }
			[Column, NotNull] public string RegionDescription { get; set; } = null!;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public List<Territory> Territories { get; set; } = null!;
		}

		[Table(Name="Shippers")]
		public class Shipper
		{
			[PrimaryKey, Identity] public int     ShipperID   { get; set; }
			[Column, NotNull]      public string  CompanyName { get; set; } = null!;
			[Column]               public string? Phone       { get; set; }

			[Association(ThisKey="ShipperID", OtherKey="ShipVia")]
			public List<Order> Orders = null!;
		}

		[Table(Name="Suppliers")]
		public class Supplier
		{
			[PrimaryKey, Identity] public int     SupplierID   { get; set; }
			[Column, NotNull]      public string  CompanyName  { get; set; } = null!;
			[Column]               public string? ContactName  { get; set; }
			[Column]               public string? ContactTitle { get; set; }
			[Column]               public string? Address      { get; set; }
			[Column]               public string? City         { get; set; }
			[Column]               public string? Region       { get; set; }
			[Column]               public string? PostalCode   { get; set; }
			[Column]               public string? Country      { get; set; }
			[Column]               public string? Phone        { get; set; }
			[Column]               public string? Fax          { get; set; }
			[Column]               public string? HomePage     { get; set; }

			[Association(ThisKey="SupplierID", OtherKey="SupplierID")]
			public List<Product> Products { get; set; } = null!;
		}

		[Table(Name="Territories")]
		public class Territory
		{
			[PrimaryKey, NotNull] public string TerritoryID          { get; set; } = null!;
			[Column, NotNull]     public string TerritoryDescription { get; set; } = null!;
			[Column]              public int    RegionID             { get; set; }

			public EmployeeTerritory? EmployeeTerritory { get; set; }

			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")]
			public List<EmployeeTerritory> EmployeeTerritories { get; set; } = null!;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public Region? Region { get; set; }
		}
	}
}
