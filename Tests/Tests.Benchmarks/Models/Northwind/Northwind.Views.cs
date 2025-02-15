using System;

using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Models
{
	public partial class Northwind
	{
		[Table(Schema = "dbo", Name = "Alphabetical list of products", IsView = true)]
		public partial class AlphabeticalListOfProduct
		{
			[Column, NotNull]  public int      ProductID       { get; set; } // int
			[Column, NotNull]  public string   ProductName     { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public int?     SupplierID      { get; set; } // int
			[Column, Nullable] public int?     CategoryID      { get; set; } // int
			[Column, Nullable] public string?  QuantityPerUnit { get; set; } // nvarchar(20)
			[Column, Nullable] public decimal? UnitPrice       { get; set; } // money
			[Column, Nullable] public short?   UnitsInStock    { get; set; } // smallint
			[Column, Nullable] public short?   UnitsOnOrder    { get; set; } // smallint
			[Column, Nullable] public short?   ReorderLevel    { get; set; } // smallint
			[Column, NotNull]  public bool     Discontinued    { get; set; } // bit
			[Column, NotNull]  public string   CategoryName    { get; set; } = null!; // nvarchar(15)
		}

		[Table(Schema = "dbo", Name = "Category Sales for 1997", IsView = true)]
		public partial class CategorySalesFor1997
		{
			[Column, NotNull]  public string   CategoryName  { get; set; } = null!; // nvarchar(15)
			[Column, Nullable] public decimal? CategorySales { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Current Product List", IsView = true)]
		public partial class CurrentProductList
		{
			[Identity]        public int    ProductID   { get; set; } // int
			[Column, NotNull] public string ProductName { get; set; } = null!; // nvarchar(40)
		}

		[Table(Schema = "dbo", Name = "Customer and Suppliers by City", IsView = true)]
		public partial class CustomerAndSuppliersByCity
		{
			[Column, Nullable] public string? City         { get; set; } // nvarchar(15)
			[Column, NotNull]  public string  CompanyName  { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public string? ContactName  { get; set; } // nvarchar(30)
			[Column, NotNull]  public string  Relationship { get; set; } = null!; // varchar(9)
		}

		[Table(Schema="dbo", Name="Invoices", IsView=true)]
		public partial class Invoice
		{
			[Column,    Nullable] public string?   ShipName       { get; set; } // nvarchar(40)
			[Column,    Nullable] public string?   ShipAddress    { get; set; } // nvarchar(60)
			[Column,    Nullable] public string?   ShipCity       { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   ShipRegion     { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   ShipPostalCode { get; set; } // nvarchar(10)
			[Column,    Nullable] public string?   ShipCountry    { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   CustomerID     { get; set; } // nchar(5)
			[Column, NotNull    ] public string    CustomerName   { get; set; } = null!; // nvarchar(40)
			[Column,    Nullable] public string?   Address        { get; set; } // nvarchar(60)
			[Column,    Nullable] public string?   City           { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   Region         { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   PostalCode     { get; set; } // nvarchar(10)
			[Column,    Nullable] public string?   Country        { get; set; } // nvarchar(15)
			[Column, NotNull    ] public string    Salesperson    { get; set; } = null!; // nvarchar(31)
			[Column, NotNull    ] public int       OrderID        { get; set; } // int
			[Column,    Nullable] public DateTime? OrderDate      { get; set; } // datetime
			[Column,    Nullable] public DateTime? RequiredDate   { get; set; } // datetime
			[Column,    Nullable] public DateTime? ShippedDate    { get; set; } // datetime
			[Column, NotNull    ] public string    ShipperName    { get; set; } = null!; // nvarchar(40)
			[Column, NotNull    ] public int       ProductID      { get; set; } // int
			[Column, NotNull    ] public string    ProductName    { get; set; } = null!; // nvarchar(40)
			[Column, NotNull    ] public decimal   UnitPrice      { get; set; } // money
			[Column, NotNull    ] public short     Quantity       { get; set; } // smallint
			[Column, NotNull    ] public double    Discount       { get; set; } // real
			[Column,    Nullable] public decimal?  ExtendedPrice  { get; set; } // money
			[Column,    Nullable] public decimal?  Freight        { get; set; } // money
		}

		[Table(Schema="dbo", Name="Order Details Extended", IsView=true)]
		public partial class OrderDetailsExtended
		{
			[Column, NotNull    ] public int      OrderID       { get; set; } // int
			[Column, NotNull    ] public int      ProductID     { get; set; } // int
			[Column, NotNull    ] public string   ProductName   { get; set; } = null!; // nvarchar(40)
			[Column, NotNull    ] public decimal  UnitPrice     { get; set; } // money
			[Column, NotNull    ] public short    Quantity      { get; set; } // smallint
			[Column, NotNull    ] public double   Discount      { get; set; } // real
			[Column,    Nullable] public decimal? ExtendedPrice { get; set; } // money
		}

		[Table(Schema="dbo", Name="Orders Qry", IsView=true)]
		public partial class OrdersQry
		{
			[Column, NotNull    ] public int       OrderID        { get; set; } // int
			[Column,    Nullable] public string?   CustomerID     { get; set; } // nchar(5)
			[Column,    Nullable] public int?      EmployeeID     { get; set; } // int
			[Column,    Nullable] public DateTime? OrderDate      { get; set; } // datetime
			[Column,    Nullable] public DateTime? RequiredDate   { get; set; } // datetime
			[Column,    Nullable] public DateTime? ShippedDate    { get; set; } // datetime
			[Column,    Nullable] public int?      ShipVia        { get; set; } // int
			[Column,    Nullable] public decimal?  Freight        { get; set; } // money
			[Column,    Nullable] public string?   ShipName       { get; set; } // nvarchar(40)
			[Column,    Nullable] public string?   ShipAddress    { get; set; } // nvarchar(60)
			[Column,    Nullable] public string?   ShipCity       { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   ShipRegion     { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   ShipPostalCode { get; set; } // nvarchar(10)
			[Column,    Nullable] public string?   ShipCountry    { get; set; } // nvarchar(15)
			[Column, NotNull    ] public string    CompanyName    { get; set; } = null!; // nvarchar(40)
			[Column,    Nullable] public string?   Address        { get; set; } // nvarchar(60)
			[Column,    Nullable] public string?   City           { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   Region         { get; set; } // nvarchar(15)
			[Column,    Nullable] public string?   PostalCode     { get; set; } // nvarchar(10)
			[Column,    Nullable] public string?   Country        { get; set; } // nvarchar(15)
		}

		[Table(Schema="dbo", Name="Order Subtotals", IsView=true)]
		public partial class OrderSubtotal
		{
			[Column, NotNull    ] public int      OrderID  { get; set; } // int
			[Column,    Nullable] public decimal? Subtotal { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Products Above Average Price", IsView = true)]
		public partial class ProductsAboveAveragePrice
		{
			[Column, NotNull]  public string   ProductName { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public decimal? UnitPrice   { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Product Sales for 1997", IsView = true)]
		public partial class ProductSalesFor1997
		{
			[Column, NotNull]  public string   CategoryName { get; set; } = null!; // nvarchar(15)
			[Column, NotNull]  public string   ProductName  { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public decimal? ProductSales { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Products by Category", IsView = true)]
		public partial class ProductsByCategory
		{
			[Column, NotNull]  public string  CategoryName    { get; set; } = null!; // nvarchar(15)
			[Column, NotNull]  public string  ProductName     { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public string? QuantityPerUnit { get; set; } // nvarchar(20)
			[Column, Nullable] public short?  UnitsInStock    { get; set; } // smallint
			[Column, NotNull]  public bool    Discontinued    { get; set; } // bit
		}

		[Table(Schema = "dbo", Name = "Quarterly Orders", IsView = true)]
		public partial class QuarterlyOrder
		{
			[Column, Nullable] public string? CustomerID  { get; set; } // nchar(5)
			[Column, Nullable] public string? CompanyName { get; set; } // nvarchar(40)
			[Column, Nullable] public string? City        { get; set; } // nvarchar(15)
			[Column, Nullable] public string? Country     { get; set; } // nvarchar(15)
		}

		[Table(Schema = "dbo", Name = "Sales by Category", IsView = true)]
		public partial class SalesByCategory
		{
			[Column, NotNull]  public int      CategoryID   { get; set; } // int
			[Column, NotNull]  public string   CategoryName { get; set; } = null!; // nvarchar(15)
			[Column, NotNull]  public string   ProductName  { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public decimal? ProductSales { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Sales Totals by Amount", IsView = true)]
		public partial class SalesTotalsByAmount
		{
			[Column, Nullable] public decimal?  SaleAmount  { get; set; } // money
			[Column, NotNull]  public int       OrderID     { get; set; } // int
			[Column, NotNull]  public string    CompanyName { get; set; } = null!; // nvarchar(40)
			[Column, Nullable] public DateTime? ShippedDate { get; set; } // datetime
		}

		[Table(Schema = "dbo", Name = "Summary of Sales by Quarter", IsView = true)]
		public partial class SummaryOfSalesByQuarter
		{
			[Column, Nullable] public DateTime? ShippedDate { get; set; } // datetime
			[Column, NotNull]  public int       OrderID     { get; set; } // int
			[Column, Nullable] public decimal?  Subtotal    { get; set; } // money
		}

		[Table(Schema = "dbo", Name = "Summary of Sales by Year", IsView = true)]
		public partial class SummaryOfSalesByYear
		{
			[Column, Nullable] public DateTime? ShippedDate { get; set; } // datetime
			[Column, NotNull]  public int       OrderID     { get; set; } // int
			[Column, Nullable] public decimal?  Subtotal    { get; set; } // money
		}
	}
}
