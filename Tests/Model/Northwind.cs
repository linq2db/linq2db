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

		[Table("Categories")]
		public class Category
		{
			[Column(IsPrimaryKey=true, IsIdentity=true), PrimaryKey, NonUpdatable] public int    CategoryID;
			[Column(CanBeNull=false), NotNull]                                     public string CategoryName;
			[Column]                                                               public string Description;
			[Column]                                                               public Binary Picture;

			[Association(ThisKey="CategoryID", OtherKey="CategoryID")]
			public List<Product> Products;
		}

		[Table(Name="CustomerCustomerDemo")]
		public class CustomerCustomerDemo
		{
			[Column(IsPrimaryKey=true, CanBeNull=false), PrimaryKey, NotNull] public string CustomerID;
			[Column(IsPrimaryKey=true, CanBeNull=false), PrimaryKey, NotNull] public string CustomerTypeID;

			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public CustomerDemographic CustomerDemographics;

			[Association(ThisKey="CustomerID", OtherKey="CustomerID")]
			public Customer Customers;
		}

		[Table(Name="CustomerDemographics")]
		public class CustomerDemographic
		{
			[Column(IsPrimaryKey=true, CanBeNull=false), PrimaryKey, NotNull] public string CustomerTypeID;
			[Column]                                                          public string CustomerDesc;
			
			[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID")]
			public List<CustomerCustomerDemo> CustomerCustomerDemos;
		}

		[Table(Name="Customers")]
		public class Customer : EntityBase<string>
		{
			[Column(IsPrimaryKey=true)] [PrimaryKey] public string CustomerID;
			[Column(CanBeNull=false)]   [NotNull]    public string CompanyName;
			[Column]                                 public string ContactName;
			[Column]                                 public string ContactTitle;
			[Column]                                 public string Address;
			[Column]                                 public string City;
			[Column]                                 public string Region;
			[Column]                                 public string PostalCode;
			[Column]                                 public string Country;
			[Column]                                 public string Phone;
			[Column]                                 public string Fax;

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
			[Column(IsPrimaryKey=true, IsIdentity=true)] [PrimaryKey, NonUpdatable] public int       EmployeeID;
			[Column(CanBeNull=false)] [NotNull]                                     public string    LastName;
			[Column(CanBeNull=false)] [NotNull]                                     public string    FirstName;
			[Column]                                                                public string    Title;
			[Column]                                                                public string    TitleOfCourtesy;
			[Column]                                                                public DateTime? BirthDate;
			[Column]                                                                public DateTime? HireDate;
			[Column]                                                                public string    Address;
			[Column]                                                                public string    City;
			[Column]                                                                public string    Region;
			[Column]                                                                public string    PostalCode;
			[Column]                                                                public string    Country;
			[Column]                                                                public string    HomePhone;
			[Column]                                                                public string    Extension;
			[Column]                                                                public Binary    Photo;
			[Column]                                                                public string    Notes;
			[Column]                                                                public int?      ReportsTo;
			[Column]                                                                public string    PhotoPath;

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
			[Column(IsPrimaryKey=true)]                 [PrimaryKey]          public int    EmployeeID;
			[Column(IsPrimaryKey=true,CanBeNull=false)] [PrimaryKey, NotNull] public string TerritoryID;

			[Association(ThisKey="EmployeeID",  OtherKey="EmployeeID")]  public Employee  Employee;
			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")] public Territory Territory;
		}

		[Table(Name="Order Details")]
		public class OrderDetail
		{
			[Column(IsPrimaryKey=true)] [PrimaryKey] public int     OrderID;
			[Column(IsPrimaryKey=true)] [PrimaryKey] public int     ProductID;
			[Column]                                 public decimal UnitPrice;
			[Column]                                 public short   Quantity;
			[Column]                                 public float   Discount;

			[Association(ThisKey="OrderID",   OtherKey="OrderID")]   public Order   Order;
			[Association(ThisKey="ProductID", OtherKey="ProductID")] public Product Product;
		}

		[Table(Name="Orders")]
		public class Order : EntityBase<int>
		{
			[Column(IsPrimaryKey=true, IsIdentity=true)] [PrimaryKey, NonUpdatable] public int       OrderID;
			[Column]                                                                public string    CustomerID;
			[Column]                                                                public int?      EmployeeID;
			[Column]                                                                public DateTime? OrderDate;
			[Column]                                                                public DateTime? RequiredDate;
			[Column]                                                                public DateTime? ShippedDate;
			[Column]                                                                public int?      ShipVia;
			[Column]                                                                public decimal   Freight;
			[Column]                                                                public string    ShipName;
			[Column]                                                                public string    ShipAddress;
			[Column]                                                                public string    ShipCity;
			[Column]                                                                public string    ShipRegion;
			[Column]                                                                public string    ShipPostalCode;
			[Column]                                                                public string    ShipCountry;

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
			[Column(IsPrimaryKey=true, IsIdentity=true)] [PrimaryKey, NonUpdatable]    public int      ProductID;
			[Column(CanBeNull=false)] [NotNull]                                        public string   ProductName;
			[Column]                                                                   public int?     SupplierID;
			[Column]                                                                   public int?     CategoryID;
			[Column]                                                                   public string   QuantityPerUnit;
			[Column]                                                                   public decimal? UnitPrice;
			[Column]                                                                   public short?   UnitsInStock;
			[Column]                                                                   public short?   UnitsOnOrder;
			[Column]                                                                   public short?   ReorderLevel;
			[Column(IsDiscriminator=true)] [MapField(IsInheritanceDiscriminator=true)] public bool     Discontinued;

			[Association(ThisKey="ProductID",  OtherKey="ProductID")]  public List<OrderDetail> OrderDetails;
			[Association(ThisKey="CategoryID", OtherKey="CategoryID")] public Category          Category;
			[Association(ThisKey="SupplierID", OtherKey="SupplierID")] public Supplier          Supplier;
		}

		public class ActiveProduct       : Product {}
		public class DiscontinuedProduct : Product {}

		[Table(Name="Region")]
		public class Region
		{
			[Column(IsPrimaryKey=true)] [PrimaryKey] public int    RegionID;
			[Column(CanBeNull=false)] [NotNull]    public string RegionDescription;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public List<Territory> Territories;
		}

		[Table(Name="Shippers")]
		public class Shipper
		{
			[Column(IsPrimaryKey=true, IsIdentity=true)] [PrimaryKey, NonUpdatable] public int    ShipperID;
			[Column(CanBeNull=false)] [NotNull]                                     public string CompanyName;
			[Column]                                                                public string Phone;

			[Association(ThisKey="ShipperID", OtherKey="ShipVia")]
			public List<Order> Orders;
		}

		[Table(Name="Suppliers")]
		public class Supplier
		{
			[Column(IsPrimaryKey=true, IsIdentity=true)] [PrimaryKey, NonUpdatable] public int    SupplierID;
			[Column(CanBeNull=false)] [NotNull]                                     public string CompanyName;
			[Column]                                                                public string ContactName;
			[Column]                                                                public string ContactTitle;
			[Column]                                                                public string Address;
			[Column]                                                                public string City;
			[Column]                                                                public string Region;
			[Column]                                                                public string PostalCode;
			[Column]                                                                public string Country;
			[Column]                                                                public string Phone;
			[Column]                                                                public string Fax;
			[Column]                                                                public string HomePage;

			[Association(ThisKey="SupplierID", OtherKey="SupplierID")]
			public List<Product> Products;
		}

		[Table(Name="Territories")]
		public class Territory
		{
			[Column(IsPrimaryKey=true, CanBeNull=false)] [PrimaryKey, NotNull] public string TerritoryID;
			[Column(CanBeNull=false)] [NotNull]                                public string TerritoryDescription;
			[Column]                                                           public int    RegionID;

			[Association(ThisKey="TerritoryID", OtherKey="TerritoryID")]
			public List<EmployeeTerritory> EmployeeTerritories;

			[Association(ThisKey="RegionID", OtherKey="RegionID")]
			public Region Region;
		}
	}
}
