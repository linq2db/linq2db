using System;
using System.Collections.Generic;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace Tests.OrmBattle.Helper
{
	interface IIdentity<T>
	{
		T Id { get; }
	}

	class ObjectFactory<TK,TE> : IObjectFactory
		where TE : IIdentity<TK>
	{
		static readonly Dictionary<TK,TE> _objects = new Dictionary<TK,TE>();

		public object CreateInstance(TypeAccessor typeAccessor)
		{
			return typeAccessor.CreateInstance();
		}
	}

	[Table(Schema="dbo", Name="Categories")]
	public partial class Category
	{
		[Column("CategoryID"), PrimaryKey, Identity   ] public int    Id   { get; set; } // int
		[Column,     NotNull    ] public string CategoryName { get; set; } // nvarchar(15)
		[Column,        Nullable] public string Description  { get; set; } // ntext
		[Column,        Nullable] public byte[] Picture      { get; set; } // image

		#region Associations

		/// <summary>
		/// FK_Products_Categories_BackReference
		/// </summary>
		[Association(ThisKey="Id", OtherKey="CategoryID", CanBeNull=true, IsBackReference=true)]
		public IList<Product> Products { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="CustomerCustomerDemo")]
	public partial class CustomerCustomerDemo
	{
		[PrimaryKey(1), NotNull] public string CustomerID     { get; set; } // nchar(5)
		[PrimaryKey(2), NotNull] public string CustomerTypeID { get; set; } // nchar(10)

		#region Associations

		/// <summary>
		/// FK_CustomerCustomerDemo
		/// </summary>
		[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID", CanBeNull=false, KeyName="FK_CustomerCustomerDemo", BackReferenceName="CustomerCustomerDemoes")]
		public CustomerDemographic FK_CustomerCustomerDemo { get; set; }

		/// <summary>
		/// FK_CustomerCustomerDemo_Customers
		/// </summary>
		[Association(ThisKey="CustomerID", OtherKey="CustomerID", CanBeNull=false, KeyName="FK_CustomerCustomerDemo_Customers", BackReferenceName="CustomerCustomerDemoes")]
		public Customer Customer { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="CustomerDemographics")]
	public partial class CustomerDemographic
	{
		[PrimaryKey, NotNull    ] public string CustomerTypeID { get; set; } // nchar(10)
		[Column,        Nullable] public string CustomerDesc   { get; set; } // ntext

		#region Associations

		/// <summary>
		/// FK_CustomerCustomerDemo_BackReference
		/// </summary>
		[Association(ThisKey="CustomerTypeID", OtherKey="CustomerTypeID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<CustomerCustomerDemo> CustomerCustomerDemoes { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Customers")]
	public partial class Customer
	{
		protected bool Equals(Customer other)
		{
			return string.Equals(Id, other.Id);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Customer) obj);
		}

		public override int GetHashCode()
		{
			return (Id != null ? Id.GetHashCode() : 0);
		}

		[Column("CustomerID"), PrimaryKey, NotNull    ] public string Id   { get; set; } // nchar(5)
		[Column,     NotNull    ] public string CompanyName  { get; set; } // nvarchar(40)
		[Column,        Nullable] public string ContactName  { get; set; } // nvarchar(30)
		[Column,        Nullable] public string ContactTitle { get; set; } // nvarchar(30)
		[Column,        Nullable] public string Address      { get; set; } // nvarchar(60)
		[Column,        Nullable] public string City         { get; set; } // nvarchar(15)
		[Column,        Nullable] public string Region       { get; set; } // nvarchar(15)
		[Column,        Nullable] public string PostalCode   { get; set; } // nvarchar(10)
		[Column,        Nullable] public string Country      { get; set; } // nvarchar(15)
		[Column,        Nullable] public string Phone        { get; set; } // nvarchar(24)
		[Column,        Nullable] public string Fax          { get; set; } // nvarchar(24)

		#region Associations

		/// <summary>
		/// FK_Orders_Customers_BackReference
		/// </summary>
		[Association(ThisKey="Id", OtherKey="CustomerID", CanBeNull=true, IsBackReference=true)]
		public IList<Order> Orders { get; set; }

		/// <summary>
		/// FK_CustomerCustomerDemo_Customers_BackReference
		/// </summary>
		[Association(ThisKey="Id", OtherKey="CustomerID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<CustomerCustomerDemo> CustomerCustomerDemoes { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Employees")]
	public partial class Employee
	{
		protected bool Equals(Employee other)
		{
			return EmployeeID == other.EmployeeID;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Employee) obj);
		}

		public override int GetHashCode()
		{
			return EmployeeID;
		}

		[PrimaryKey, Identity   ] public int       EmployeeID      { get; set; } // int
		[Column,     NotNull    ] public string    LastName        { get; set; } // nvarchar(20)
		[Column,     NotNull    ] public string    FirstName       { get; set; } // nvarchar(10)
		[Column,        Nullable] public string    Title           { get; set; } // nvarchar(30)
		[Column,        Nullable] public string    TitleOfCourtesy { get; set; } // nvarchar(25)
		[Column,        Nullable] public DateTime? BirthDate       { get; set; } // datetime
		[Column,        Nullable] public DateTime? HireDate        { get; set; } // datetime
		[Column,        Nullable] public string    Address         { get; set; } // nvarchar(60)
		[Column,        Nullable] public string    City            { get; set; } // nvarchar(15)
		[Column,        Nullable] public string    Region          { get; set; } // nvarchar(15)
		[Column,        Nullable] public string    PostalCode      { get; set; } // nvarchar(10)
		[Column,        Nullable] public string    Country         { get; set; } // nvarchar(15)
		[Column,        Nullable] public string    HomePhone       { get; set; } // nvarchar(24)
		[Column,        Nullable] public string    Extension       { get; set; } // nvarchar(4)
		[Column,        Nullable] public byte[]    Photo           { get; set; } // image
		[Column,        Nullable] public string    Notes           { get; set; } // ntext
		[Column,        Nullable] public int?      ReportsTo       { get; set; } // int
		[Column,        Nullable] public string    PhotoPath       { get; set; } // nvarchar(255)

		#region Associations

		/// <summary>
		/// FK_Employees_Employees
		/// </summary>
		[Association(ThisKey="ReportsTo", OtherKey="EmployeeID", CanBeNull=true, KeyName="FK_Employees_Employees", BackReferenceName="FK_Employees_Employees_BackReferences")]
		public Employee FK_Employees_Employee { get; set; }

		/// <summary>
		/// FK_Employees_Employees_BackReference
		/// </summary>
		[Association(ThisKey="EmployeeID", OtherKey="ReportsTo", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<Employee> FK_Employees_Employees_BackReferences { get; set; }

		/// <summary>
		/// FK_Orders_Employees_BackReference
		/// </summary>
		[Association(ThisKey="EmployeeID", OtherKey="EmployeeID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<Order> Orders { get; set; }

		/// <summary>
		/// FK_EmployeeTerritories_Employees_BackReference
		/// </summary>
		[Association(ThisKey="EmployeeID", OtherKey="EmployeeID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<EmployeeTerritory> EmployeeTerritories { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="EmployeeTerritories")]
	public partial class EmployeeTerritory
	{
		[PrimaryKey(1), NotNull] public int    EmployeeID  { get; set; } // int
		[PrimaryKey(2), NotNull] public string TerritoryID { get; set; } // nvarchar(20)

		#region Associations

		/// <summary>
		/// FK_EmployeeTerritories_Employees
		/// </summary>
		[Association(ThisKey="EmployeeID", OtherKey="EmployeeID", CanBeNull=false, KeyName="FK_EmployeeTerritories_Employees", BackReferenceName="EmployeeTerritories")]
		public Employee Employee { get; set; }

		/// <summary>
		/// FK_EmployeeTerritories_Territories
		/// </summary>
		[Association(ThisKey="TerritoryID", OtherKey="TerritoryID", CanBeNull=false, KeyName="FK_EmployeeTerritories_Territories", BackReferenceName="EmployeeTerritories")]
		public Territory Territory { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Order Details")]
	public partial class OrderDetail
	{
		[PrimaryKey(1), NotNull] public int     OrderID   { get; set; } // int
		[PrimaryKey(2), NotNull] public int     ProductID { get; set; } // int
		[Column,        NotNull] public decimal UnitPrice { get; set; } // money
		[Column,        NotNull] public short   Quantity  { get; set; } // smallint
		[Column,        NotNull] public float   Discount  { get; set; } // real

		#region Associations

		/// <summary>
		/// FK_Order_Details_Orders
		/// </summary>
		[Association(ThisKey="OrderID", OtherKey="Id", CanBeNull=false, KeyName="FK_Order_Details_Orders", BackReferenceName="OrderDetails")]
		public Order Order { get; set; }

		/// <summary>
		/// FK_Order_Details_Products
		/// </summary>
		[Association(ThisKey="ProductID", OtherKey="Id", CanBeNull=false, KeyName="FK_Order_Details_Products", BackReferenceName="OrderDetails")]
		public Product Product { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Orders")]
	public partial class Order
	{
		protected bool Equals(Order other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Order) obj);
		}

		public override int GetHashCode()
		{
			return Id;
		}

		[Column("OrderID"), PrimaryKey, Identity] public int Id { get; set; } // int
		[Column,     Nullable] public string    CustomerID     { get; set; } // nchar(5)
		[Column,     Nullable] public int?      EmployeeID     { get; set; } // int
		[Column,     Nullable] public DateTime? OrderDate      { get; set; } // datetime
		[Column,     Nullable] public DateTime? RequiredDate   { get; set; } // datetime
		[Column,     Nullable] public DateTime? ShippedDate    { get; set; } // datetime
		[Column,     Nullable] public int?      ShipVia        { get; set; } // int
		[Column,     Nullable] public decimal   Freight        { get; set; } // money
		[Column,     Nullable] public string    ShipName       { get; set; } // nvarchar(40)
		[Column,     Nullable] public string    ShipAddress    { get; set; } // nvarchar(60)
		[Column,     Nullable] public string    ShipCity       { get; set; } // nvarchar(15)
		[Column,     Nullable] public string    ShipRegion     { get; set; } // nvarchar(15)
		[Column,     Nullable] public string    ShipPostalCode { get; set; } // nvarchar(10)
		[Column,     Nullable] public string    ShipCountry    { get; set; } // nvarchar(15)

		#region Associations

		/// <summary>
		/// FK_Orders_Customers
		/// </summary>
		[Association(ThisKey="CustomerID", OtherKey="Id", CanBeNull=true, KeyName="FK_Orders_Customers", BackReferenceName="Orders")]
		public Customer Customer { get; set; }

		/// <summary>
		/// FK_Orders_Employees
		/// </summary>
		[Association(ThisKey="EmployeeID", OtherKey="EmployeeID", CanBeNull=true, KeyName="FK_Orders_Employees", BackReferenceName="Orders")]
		public Employee Employee { get; set; }

		/// <summary>
		/// FK_Orders_Shippers
		/// </summary>
		[Association(ThisKey="ShipVia", OtherKey="ShipperID", CanBeNull=true, KeyName="FK_Orders_Shippers", BackReferenceName="Orders")]
		public Shipper Shipper { get; set; }

		/// <summary>
		/// FK_Order_Details_Orders_BackReference
		/// </summary>
		[Association(ThisKey="OrderID", OtherKey="OrderID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<OrderDetail> OrderDetails { get; set; }

		#endregion
	}

	[Table(Schema = "dbo", Name = "Products")]
	[InheritanceMapping(Code=true,  Type=typeof(DiscontinuedProduct))]
	[InheritanceMapping(Code=false, Type=typeof(ActiveProduct), IsDefault=true)]
	public class Product
	{
		[Column("ProductID"), PrimaryKey, Identity   ] public int      Id       { get; set; } // int
		[Column,     NotNull    ] public string   ProductName     { get; set; } // nvarchar(40)
		[Column,        Nullable] public int?     SupplierID      { get; set; } // int
		[Column,        Nullable] public int?     CategoryID      { get; set; } // int
		[Column,        Nullable] public string   QuantityPerUnit { get; set; } // nvarchar(20)
		[Column,        Nullable] public decimal? UnitPrice       { get; set; } // money
		[Column,        Nullable] public short?   UnitsInStock    { get; set; } // smallint
		[Column,        Nullable] public short?   UnitsOnOrder    { get; set; } // smallint
		[Column,        Nullable] public short?   ReorderLevel    { get; set; } // smallint
		[Column(IsDiscriminator = true),     NotNull    ] public bool     Discontinued    { get; set; } // bit

		#region Associations

		/// <summary>
		/// FK_Products_Categories
		/// </summary>
		[Association(ThisKey="CategoryID", OtherKey="Id", CanBeNull=true, KeyName="FK_Products_Categories", BackReferenceName="Products")]
		public Category Category { get; set; }

		/// <summary>
		/// FK_Products_Suppliers
		/// </summary>
		[Association(ThisKey="SupplierID", OtherKey="Id", CanBeNull=true, KeyName="FK_Products_Suppliers", BackReferenceName="Products")]
		public Supplier Supplier { get; set; }

		/// <summary>
		/// FK_Order_Details_Products_BackReference
		/// </summary>
		[Association(ThisKey="Id", OtherKey="ProductID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<OrderDetail> OrderDetails { get; set; }

		#endregion
	}

	public class ActiveProduct : Product
	{
	}

	public class DiscontinuedProduct : Product
	{
	}

	[Table(Schema="dbo", Name="Region")]
	public partial class Region
	{
		[PrimaryKey, NotNull] public int    RegionID          { get; set; } // int
		[Column,     NotNull] public string RegionDescription { get; set; } // nchar(50)

		#region Associations

		/// <summary>
		/// FK_Territories_Region_BackReference
		/// </summary>
		[Association(ThisKey="RegionID", OtherKey="RegionID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<Territory> Territories { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Shippers")]
	public partial class Shipper
	{
		[PrimaryKey, Identity   ] public int    ShipperID   { get; set; } // int
		[Column,     NotNull    ] public string CompanyName { get; set; } // nvarchar(40)
		[Column,        Nullable] public string Phone       { get; set; } // nvarchar(24)

		#region Associations

		/// <summary>
		/// FK_Orders_Shippers_BackReference
		/// </summary>
		[Association(ThisKey="ShipperID", OtherKey="ShipVia", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<Order> Orders { get; set; }

		#endregion
	}

	[Table(Schema="dbo", Name="Suppliers")]
	public partial class Supplier
	{
		[Column("SupplierID"), PrimaryKey, Identity   ] public int    Id   { get; set; } // int
		[Column,     NotNull    ] public string CompanyName  { get; set; } // nvarchar(40)
		[Column,        Nullable] public string ContactName  { get; set; } // nvarchar(30)
		[Column,        Nullable] public string ContactTitle { get; set; } // nvarchar(30)
		[Column,        Nullable] public string Address      { get; set; } // nvarchar(60)
		[Column,        Nullable] public string City         { get; set; } // nvarchar(15)
		[Column,        Nullable] public string Region       { get; set; } // nvarchar(15)
		[Column,        Nullable] public string PostalCode   { get; set; } // nvarchar(10)
		[Column,        Nullable] public string Country      { get; set; } // nvarchar(15)
		[Column,        Nullable] public string Phone        { get; set; } // nvarchar(24)
		[Column,        Nullable] public string Fax          { get; set; } // nvarchar(24)
		[Column,        Nullable] public string HomePage     { get; set; } // ntext

		#region Associations

		/// <summary>
		/// FK_Products_Suppliers_BackReference
		/// </summary>
		[Association(ThisKey="Id", OtherKey="SupplierID", CanBeNull=true, IsBackReference=true)]
		public IEnumerable<Product> Products { get; set; }

		#endregion
	}

	[Table(Schema = "dbo", Name = "Territories")]
	public partial class Territory
	{
		[PrimaryKey, NotNull]
		public string TerritoryID { get; set; } // nvarchar(20)
		[Column, NotNull]
		public string TerritoryDescription { get; set; } // nchar(50)
		[Column, NotNull]
		public int RegionID { get; set; } // int

		#region Associations

		/// <summary>
		/// FK_Territories_Region
		/// </summary>
		[Association(ThisKey = "RegionID", OtherKey = "RegionID", CanBeNull = false, KeyName = "FK_Territories_Region", BackReferenceName = "Territories")]
		public Region Region { get; set; }

		/// <summary>
		/// FK_EmployeeTerritories_Territories_BackReference
		/// </summary>
		[Association(ThisKey = "TerritoryID", OtherKey = "TerritoryID", CanBeNull = true, IsBackReference = true)]
		public IEnumerable<EmployeeTerritory> EmployeeTerritories { get; set; }

		#endregion
	}

}
