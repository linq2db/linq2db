// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
using System;
using System.Linq;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.T4.SQLiteNorthwind
{
	public partial class TestDataDB : DataConnection
	{
		public TestDataDB()
		{
			InitDataContext();
		}

		public TestDataDB(string configuration)
			: base(configuration)
		{
			InitDataContext();
		}

		public TestDataDB(DataContextOptions options)
			: base(options)
		{
			InitDataContext();
		}

		public TestDataDB(DataContextOptions<TestDataDB> options)
			: base(options)
		{
			InitDataContext();
		}

		partial void InitDataContext();

		public ITable<Category>                   Categories                   => this.GetTable<Category>();
		public ITable<CustomerCustomerDemo>       CustomerCustomerDemos        => this.GetTable<CustomerCustomerDemo>();
		public ITable<CustomerDemographic>        CustomerDemographics         => this.GetTable<CustomerDemographic>();
		public ITable<Customer>                   Customers                    => this.GetTable<Customer>();
		public ITable<Employee>                   Employees                    => this.GetTable<Employee>();
		public ITable<EmployeeTerritory>          EmployeeTerritories          => this.GetTable<EmployeeTerritory>();
		public ITable<OrderDetail>                OrderDetails                 => this.GetTable<OrderDetail>();
		public ITable<Order>                      Orders                       => this.GetTable<Order>();
		public ITable<Product>                    Products                     => this.GetTable<Product>();
		public ITable<Region>                     Regions                      => this.GetTable<Region>();
		public ITable<Shipper>                    Shippers                     => this.GetTable<Shipper>();
		public ITable<Supplier>                   Suppliers                    => this.GetTable<Supplier>();
		public ITable<Territory>                  Territories                  => this.GetTable<Territory>();
		public ITable<AlphabeticalListOfProduct>  AlphabeticalListOfProducts   => this.GetTable<AlphabeticalListOfProduct>();
		public ITable<CurrentProductList>         CurrentProductLists          => this.GetTable<CurrentProductList>();
		public ITable<CustomerAndSuppliersByCity> CustomerAndSuppliersByCities => this.GetTable<CustomerAndSuppliersByCity>();
		public ITable<OrderDetailsExtended>       OrderDetailsExtendeds        => this.GetTable<OrderDetailsExtended>();
		public ITable<OrderSubtotal>              OrderSubtotals               => this.GetTable<OrderSubtotal>();
		public ITable<SummaryOfSalesByQuarter>    SummaryOfSalesByQuarters     => this.GetTable<SummaryOfSalesByQuarter>();
		public ITable<SummaryOfSalesByYear>       SummaryOfSalesByYears        => this.GetTable<SummaryOfSalesByYear>();
		public ITable<OrdersQry>                  OrdersQries                  => this.GetTable<OrdersQry>();
		public ITable<ProductsAboveAveragePrice>  ProductsAboveAveragePrices   => this.GetTable<ProductsAboveAveragePrice>();
		public ITable<ProductsByCategory>         ProductsByCategories         => this.GetTable<ProductsByCategory>();
	}

	[Table("Categories")]
	public partial class Category
	{
		[Column("CategoryID"  , IsPrimaryKey = true )] public int     CategoryID   { get; set; } // int
		[Column("CategoryName", CanBeNull    = false)] public string  CategoryName { get; set; } = null!; // varchar(15)
		[Column("Description"                       )] public string? Description  { get; set; } // text(max)
		[Column("Picture"                           )] public byte[]? Picture      { get; set; } // blob
	}

	public static partial class ExtensionMethods
	{
		#region Table Extensions
		public static Category? Find(this ITable<Category> table, int categoryId)
		{
			return table.FirstOrDefault(e => e.CategoryID == categoryId);
		}

		public static CustomerCustomerDemo? Find(this ITable<CustomerCustomerDemo> table, string customerId, string customerTypeId)
		{
			return table.FirstOrDefault(e => e.CustomerID == customerId && e.CustomerTypeID == customerTypeId);
		}

		public static CustomerDemographic? Find(this ITable<CustomerDemographic> table, string customerTypeId)
		{
			return table.FirstOrDefault(e => e.CustomerTypeID == customerTypeId);
		}

		public static Customer? Find(this ITable<Customer> table, string customerId)
		{
			return table.FirstOrDefault(e => e.CustomerID == customerId);
		}

		public static Employee? Find(this ITable<Employee> table, int employeeId)
		{
			return table.FirstOrDefault(e => e.EmployeeID == employeeId);
		}

		public static EmployeeTerritory? Find(this ITable<EmployeeTerritory> table, int employeeId, string territoryId)
		{
			return table.FirstOrDefault(e => e.EmployeeID == employeeId && e.TerritoryID == territoryId);
		}

		public static OrderDetail? Find(this ITable<OrderDetail> table, int orderId, int productId)
		{
			return table.FirstOrDefault(e => e.OrderID == orderId && e.ProductID == productId);
		}

		public static Order? Find(this ITable<Order> table, int orderId)
		{
			return table.FirstOrDefault(e => e.OrderID == orderId);
		}

		public static Product? Find(this ITable<Product> table, int productId)
		{
			return table.FirstOrDefault(e => e.ProductID == productId);
		}

		public static Region? Find(this ITable<Region> table, int regionId)
		{
			return table.FirstOrDefault(e => e.RegionID == regionId);
		}

		public static Shipper? Find(this ITable<Shipper> table, int shipperId)
		{
			return table.FirstOrDefault(e => e.ShipperID == shipperId);
		}

		public static Supplier? Find(this ITable<Supplier> table, int supplierId)
		{
			return table.FirstOrDefault(e => e.SupplierID == supplierId);
		}

		public static Territory? Find(this ITable<Territory> table, string territoryId)
		{
			return table.FirstOrDefault(e => e.TerritoryID == territoryId);
		}
		#endregion
	}

	[Table("CustomerCustomerDemo")]
	public partial class CustomerCustomerDemo
	{
		[Column("CustomerID"    , CanBeNull = false, IsPrimaryKey = true, PrimaryKeyOrder = 0)] public string CustomerID     { get; set; } = null!; // varchar(5)
		[Column("CustomerTypeID", CanBeNull = false, IsPrimaryKey = true, PrimaryKeyOrder = 1)] public string CustomerTypeID { get; set; } = null!; // varchar(10)
	}

	[Table("CustomerDemographics")]
	public partial class CustomerDemographic
	{
		[Column("CustomerTypeID", CanBeNull = false, IsPrimaryKey = true)] public string  CustomerTypeID { get; set; } = null!; // varchar(10)
		[Column("CustomerDesc"                                          )] public string? CustomerDesc   { get; set; } // text(max)
	}

	[Table("Customers")]
	public partial class Customer
	{
		[Column("CustomerID"  , CanBeNull = false, IsPrimaryKey = true)] public string  CustomerID   { get; set; } = null!; // varchar(5)
		[Column("CompanyName" , CanBeNull = false                     )] public string  CompanyName  { get; set; } = null!; // varchar(40)
		[Column("ContactName"                                         )] public string? ContactName  { get; set; } // varchar(30)
		[Column("ContactTitle"                                        )] public string? ContactTitle { get; set; } // varchar(30)
		[Column("Address"                                             )] public string? Address      { get; set; } // varchar(60)
		[Column("City"                                                )] public string? City         { get; set; } // varchar(15)
		[Column("Region"                                              )] public string? Region       { get; set; } // varchar(15)
		[Column("PostalCode"                                          )] public string? PostalCode   { get; set; } // varchar(10)
		[Column("Country"                                             )] public string? Country      { get; set; } // varchar(15)
		[Column("Phone"                                               )] public string? Phone        { get; set; } // varchar(24)
		[Column("Fax"                                                 )] public string? Fax          { get; set; } // varchar(24)
	}

	[Table("Employees")]
	public partial class Employee
	{
		[Column("EmployeeID"     , IsPrimaryKey = true                      )] public int       EmployeeID      { get; set; } // int
		[Column("LastName"       , CanBeNull    = false                     )] public string    LastName        { get; set; } = null!; // varchar(20)
		[Column("FirstName"      , CanBeNull    = false                     )] public string    FirstName       { get; set; } = null!; // varchar(10)
		[Column("Title"                                                     )] public string?   Title           { get; set; } // varchar(30)
		[Column("TitleOfCourtesy"                                           )] public string?   TitleOfCourtesy { get; set; } // varchar(25)
		[Column("BirthDate"      , SkipOnInsert = true , SkipOnUpdate = true)] public DateTime? BirthDate       { get; set; } // timestamp
		[Column("HireDate"       , SkipOnInsert = true , SkipOnUpdate = true)] public DateTime? HireDate        { get; set; } // timestamp
		[Column("Address"                                                   )] public string?   Address         { get; set; } // varchar(60)
		[Column("City"                                                      )] public string?   City            { get; set; } // varchar(15)
		[Column("Region"                                                    )] public string?   Region          { get; set; } // varchar(15)
		[Column("PostalCode"                                                )] public string?   PostalCode      { get; set; } // varchar(10)
		[Column("Country"                                                   )] public string?   Country         { get; set; } // varchar(15)
		[Column("HomePhone"                                                 )] public string?   HomePhone       { get; set; } // varchar(24)
		[Column("Extension"                                                 )] public string?   Extension       { get; set; } // varchar(4)
		[Column("Photo"                                                     )] public byte[]?   Photo           { get; set; } // blob
		[Column("Notes"                                                     )] public string?   Notes           { get; set; } // text(max)
		[Column("ReportsTo"                                                 )] public int?      ReportsTo       { get; set; } // int
		[Column("PhotoPath"                                                 )] public string?   PhotoPath       { get; set; } // varchar(255)
	}

	[Table("EmployeeTerritories")]
	public partial class EmployeeTerritory
	{
		[Column("EmployeeID" , IsPrimaryKey = true , PrimaryKeyOrder = 0                        )] public int    EmployeeID  { get; set; } // int
		[Column("TerritoryID", CanBeNull    = false, IsPrimaryKey    = true, PrimaryKeyOrder = 1)] public string TerritoryID { get; set; } = null!; // varchar(20)
	}

	[Table("Order Details")]
	public partial class OrderDetail
	{
		[Column("OrderID"  , IsPrimaryKey = true, PrimaryKeyOrder = 0)] public int     OrderID   { get; set; } // int
		[Column("ProductID", IsPrimaryKey = true, PrimaryKeyOrder = 1)] public int     ProductID { get; set; } // int
		[Column("UnitPrice"                                          )] public double? UnitPrice { get; set; } // float
		[Column("Quantity"                                           )] public int?    Quantity  { get; set; } // int
		[Column("Discount"                                           )] public double? Discount  { get; set; } // float
	}

	[Table("Orders")]
	public partial class Order
	{
		[Column("OrderID"       , IsPrimaryKey = true                     )] public int       OrderID        { get; set; } // int
		[Column("CustomerID"                                              )] public string?   CustomerID     { get; set; } // varchar(5)
		[Column("EmployeeID"                                              )] public int?      EmployeeID     { get; set; } // int
		[Column("OrderDate"     , SkipOnInsert = true, SkipOnUpdate = true)] public DateTime? OrderDate      { get; set; } // timestamp
		[Column("RequiredDate"  , SkipOnInsert = true, SkipOnUpdate = true)] public DateTime? RequiredDate   { get; set; } // timestamp
		[Column("ShippedDate"   , SkipOnInsert = true, SkipOnUpdate = true)] public DateTime? ShippedDate    { get; set; } // timestamp
		[Column("ShipVia"                                                 )] public int?      ShipVia        { get; set; } // int
		[Column("Freight"                                                 )] public double?   Freight        { get; set; } // float
		[Column("ShipName"                                                )] public string?   ShipName       { get; set; } // varchar(40)
		[Column("ShipAddress"                                             )] public string?   ShipAddress    { get; set; } // varchar(60)
		[Column("ShipCity"                                                )] public string?   ShipCity       { get; set; } // varchar(15)
		[Column("ShipRegion"                                              )] public string?   ShipRegion     { get; set; } // varchar(15)
		[Column("ShipPostalCode"                                          )] public string?   ShipPostalCode { get; set; } // varchar(10)
		[Column("ShipCountry"                                             )] public string?   ShipCountry    { get; set; } // varchar(15)
	}

	[Table("Products")]
	public partial class Product
	{
		[Column("ProductID"      , IsPrimaryKey = true )] public int     ProductID       { get; set; } // int
		[Column("ProductName"    , CanBeNull    = false)] public string  ProductName     { get; set; } = null!; // varchar(40)
		[Column("SupplierID"                           )] public int?    SupplierID      { get; set; } // int
		[Column("CategoryID"                           )] public int?    CategoryID      { get; set; } // int
		[Column("QuantityPerUnit"                      )] public string? QuantityPerUnit { get; set; } // varchar(20)
		[Column("UnitPrice"                            )] public double? UnitPrice       { get; set; } // float
		[Column("UnitsInStock"                         )] public int?    UnitsInStock    { get; set; } // int
		[Column("UnitsOnOrder"                         )] public int?    UnitsOnOrder    { get; set; } // int
		[Column("ReorderLevel"                         )] public int?    ReorderLevel    { get; set; } // int
		[Column("Discontinued"                         )] public int     Discontinued    { get; set; } // int
	}

	[Table("Region")]
	public partial class Region
	{
		[Column("RegionID"         , IsPrimaryKey = true )] public int    RegionID          { get; set; } // int
		[Column("RegionDescription", CanBeNull    = false)] public string RegionDescription { get; set; } = null!; // varchar(50)
	}

	[Table("Shippers")]
	public partial class Shipper
	{
		[Column("ShipperID"  , IsPrimaryKey = true )] public int     ShipperID   { get; set; } // int
		[Column("CompanyName", CanBeNull    = false)] public string  CompanyName { get; set; } = null!; // varchar(40)
		[Column("Phone"                            )] public string? Phone       { get; set; } // varchar(24)
	}

	[Table("Suppliers")]
	public partial class Supplier
	{
		[Column("SupplierID"  , IsPrimaryKey = true )] public int     SupplierID   { get; set; } // int
		[Column("CompanyName" , CanBeNull    = false)] public string  CompanyName  { get; set; } = null!; // varchar(40)
		[Column("ContactName"                       )] public string? ContactName  { get; set; } // varchar(30)
		[Column("ContactTitle"                      )] public string? ContactTitle { get; set; } // varchar(30)
		[Column("Address"                           )] public string? Address      { get; set; } // varchar(60)
		[Column("City"                              )] public string? City         { get; set; } // varchar(15)
		[Column("Region"                            )] public string? Region       { get; set; } // varchar(15)
		[Column("PostalCode"                        )] public string? PostalCode   { get; set; } // varchar(10)
		[Column("Country"                           )] public string? Country      { get; set; } // varchar(15)
		[Column("Phone"                             )] public string? Phone        { get; set; } // varchar(24)
		[Column("Fax"                               )] public string? Fax          { get; set; } // varchar(24)
		[Column("HomePage"                          )] public string? HomePage     { get; set; } // text(max)
	}

	[Table("Territories")]
	public partial class Territory
	{
		[Column("TerritoryID"         , CanBeNull = false, IsPrimaryKey = true)] public string TerritoryID          { get; set; } = null!; // varchar(20)
		[Column("TerritoryDescription", CanBeNull = false                     )] public string TerritoryDescription { get; set; } = null!; // varchar(50)
		[Column("RegionID"                                                    )] public int    RegionID             { get; set; } // int
	}

	[Table("Alphabetical list of products", IsView = true)]
	public partial class AlphabeticalListOfProduct
	{
		[Column("ProductID"                         )] public int     ProductID       { get; set; } // int
		[Column("ProductName"    , CanBeNull = false)] public string  ProductName     { get; set; } = null!; // varchar(40)
		[Column("SupplierID"                        )] public int?    SupplierID      { get; set; } // int
		[Column("CategoryID"                        )] public int?    CategoryID      { get; set; } // int
		[Column("QuantityPerUnit"                   )] public string? QuantityPerUnit { get; set; } // varchar(20)
		[Column("UnitPrice"                         )] public double? UnitPrice       { get; set; } // float
		[Column("UnitsInStock"                      )] public int?    UnitsInStock    { get; set; } // int
		[Column("UnitsOnOrder"                      )] public int?    UnitsOnOrder    { get; set; } // int
		[Column("ReorderLevel"                      )] public int?    ReorderLevel    { get; set; } // int
		[Column("Discontinued"                      )] public int     Discontinued    { get; set; } // int
		[Column("CategoryName"   , CanBeNull = false)] public string  CategoryName    { get; set; } = null!; // varchar(15)
	}

	[Table("Current Product List", IsView = true)]
	public partial class CurrentProductList
	{
		[Column("ProductID"                     )] public int    ProductID   { get; set; } // int
		[Column("ProductName", CanBeNull = false)] public string ProductName { get; set; } = null!; // varchar(40)
	}

	[Table("Customer and Suppliers by City", IsView = true)]
	public partial class CustomerAndSuppliersByCity
	{
		[Column("City"                           )] public string? City         { get; set; } // varchar(15)
		[Column("CompanyName" , CanBeNull = false)] public string  CompanyName  { get; set; } = null!; // varchar(40)
		[Column("ContactName"                    )] public string? ContactName  { get; set; } // varchar(30)
		[Column("Relationship"                   )] public object? Relationship { get; set; } // NUMERIC
	}

	[Table("Order Details Extended", IsView = true)]
	public partial class OrderDetailsExtended
	{
		[Column("OrderID"                         )] public int     OrderID       { get; set; } // int
		[Column("ProductID"                       )] public int     ProductID     { get; set; } // int
		[Column("ProductName"  , CanBeNull = false)] public string  ProductName   { get; set; } = null!; // varchar(40)
		[Column("UnitPrice"                       )] public double? UnitPrice     { get; set; } // float
		[Column("Quantity"                        )] public int?    Quantity      { get; set; } // int
		[Column("Discount"                        )] public double? Discount      { get; set; } // float
		[Column("ExtendedPrice"                   )] public object? ExtendedPrice { get; set; } // NUMERIC
	}

	[Table("Order Subtotals", IsView = true)]
	public partial class OrderSubtotal
	{
		[Column("OrderID" )] public int     OrderID  { get; set; } // int
		[Column("Subtotal")] public object? Subtotal { get; set; } // NUMERIC
	}

	[Table("Summary of Sales by Quarter", IsView = true)]
	public partial class SummaryOfSalesByQuarter
	{
		[Column("ShippedDate", SkipOnInsert = true, SkipOnUpdate = true)] public DateTime? ShippedDate { get; set; } // timestamp
		[Column("OrderID"                                              )] public int       OrderID     { get; set; } // int
		[Column("Subtotal"                                             )] public object?   Subtotal    { get; set; } // NUMERIC
	}

	[Table("Summary of Sales by Year", IsView = true)]
	public partial class SummaryOfSalesByYear
	{
		[Column("ShippedDate", SkipOnInsert = true, SkipOnUpdate = true)] public DateTime? ShippedDate { get; set; } // timestamp
		[Column("OrderID"                                              )] public int       OrderID     { get; set; } // int
		[Column("Subtotal"                                             )] public object?   Subtotal    { get; set; } // NUMERIC
	}

	[Table("Orders Qry", IsView = true)]
	public partial class OrdersQry
	{
		[Column("OrderID"                                                  )] public int       OrderID        { get; set; } // int
		[Column("CustomerID"                                               )] public string?   CustomerID     { get; set; } // varchar(5)
		[Column("EmployeeID"                                               )] public int?      EmployeeID     { get; set; } // int
		[Column("OrderDate"     , SkipOnInsert = true , SkipOnUpdate = true)] public DateTime? OrderDate      { get; set; } // timestamp
		[Column("RequiredDate"  , SkipOnInsert = true , SkipOnUpdate = true)] public DateTime? RequiredDate   { get; set; } // timestamp
		[Column("ShippedDate"   , SkipOnInsert = true , SkipOnUpdate = true)] public DateTime? ShippedDate    { get; set; } // timestamp
		[Column("ShipVia"                                                  )] public int?      ShipVia        { get; set; } // int
		[Column("Freight"                                                  )] public double?   Freight        { get; set; } // float
		[Column("ShipName"                                                 )] public string?   ShipName       { get; set; } // varchar(40)
		[Column("ShipAddress"                                              )] public string?   ShipAddress    { get; set; } // varchar(60)
		[Column("ShipCity"                                                 )] public string?   ShipCity       { get; set; } // varchar(15)
		[Column("ShipRegion"                                               )] public string?   ShipRegion     { get; set; } // varchar(15)
		[Column("ShipPostalCode"                                           )] public string?   ShipPostalCode { get; set; } // varchar(10)
		[Column("ShipCountry"                                              )] public string?   ShipCountry    { get; set; } // varchar(15)
		[Column("CompanyName"   , CanBeNull    = false                     )] public string    CompanyName    { get; set; } = null!; // varchar(40)
		[Column("Address"                                                  )] public string?   Address        { get; set; } // varchar(60)
		[Column("City"                                                     )] public string?   City           { get; set; } // varchar(15)
		[Column("Region"                                                   )] public string?   Region         { get; set; } // varchar(15)
		[Column("PostalCode"                                               )] public string?   PostalCode     { get; set; } // varchar(10)
		[Column("Country"                                                  )] public string?   Country        { get; set; } // varchar(15)
	}

	[Table("Products Above Average Price", IsView = true)]
	public partial class ProductsAboveAveragePrice
	{
		[Column("ProductName", CanBeNull = false)] public string  ProductName { get; set; } = null!; // varchar(40)
		[Column("UnitPrice"                     )] public double? UnitPrice   { get; set; } // float
	}

	[Table("Products by Category", IsView = true)]
	public partial class ProductsByCategory
	{
		[Column("CategoryName"   , CanBeNull = false)] public string  CategoryName    { get; set; } = null!; // varchar(15)
		[Column("ProductName"    , CanBeNull = false)] public string  ProductName     { get; set; } = null!; // varchar(40)
		[Column("QuantityPerUnit"                   )] public string? QuantityPerUnit { get; set; } // varchar(20)
		[Column("UnitsInStock"                      )] public int?    UnitsInStock    { get; set; } // int
		[Column("Discontinued"                      )] public int     Discontinued    { get; set; } // int
	}
}