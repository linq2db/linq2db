using System;
using System.Collections.Generic;
using System.Data;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.Mappings
{
	public class Db : Data.DataConnection
	{
		public Db(string connectionString) : base(ProviderName.SqlServer2008, connectionString)
		{
		}

#pragma warning disable CA2000 // Dispose objects before losing scope
		public Db(IDataProvider provider, QueryResult result) : base(provider, new MockDbConnection(result, ConnectionState.Open))
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
		}

#pragma warning disable CA2000 // Dispose objects before losing scope
		public Db(IDataProvider provider, QueryResult[] results) : base(provider, new MockDbConnection(results, ConnectionState.Open))
#pragma warning restore CA2000 // Dispose objects before losing scope
		{
		}

		public ITable<SalesOrderHeader> SalesOrderHeader => this.GetTable<SalesOrderHeader>();

		public ITable<CreditCard> CreditCards => this.GetTable<CreditCard>();

		public ITable<SalesOrderHeader> SalesOrderHeaders => this.GetTable<SalesOrderHeader>();
	}

	[Table(Schema = "Sales", Name = "CreditCard")]
	public class CreditCard
	{
		[PrimaryKey, Identity]
		public int CreditCardID { get; set; }

		[Column]
		public string CardNumber { get; set; } = null!;

		[Column]
		public string CardType { get; set; } = null!;

		[Column]
		public byte ExpMonth { get; set; }

		[Column]
		public short ExpYear { get; set; }

		[Column]
		public DateTime ModifiedDate { get; set; }

		[Association(ThisKey = nameof(CreditCardID), OtherKey = nameof(SalesOrderHeader.CreditCardID))]
		public List<SalesOrderHeader> SalesOrderHeaders { get; set; } = null!;
	}

	[Table(Schema = "Sales", Name = "SalesOrderHeader")]
	public class SalesOrderHeader
	{
		[PrimaryKey, Identity]
		public int SalesOrderID { get; set; }

		[Column]
		public string? AccountNumber { get; set; }

		[Column]
		public string? Comment { get; set; }

		[Column]
		public string? CreditCardApprovalCode { get; set; }

		[Column]
		public DateTime DueDate { get; set; }

		[Column]
		public decimal Freight { get; set; }

		[Column]
		public DateTime ModifiedDate { get; set; }

		[Column]
		public bool OnlineOrderFlag { get; set; }

		[Column]
		public DateTime OrderDate { get; set; }

		[Column]
		public string? PurchaseOrderNumber { get; set; }

		[Column]
		public byte RevisionNumber { get; set; }

		[Column]
		public Guid Rowguid { get; set; }

		[Column]
		public string SalesOrderNumber { get; set; } = null!;

		[Column]
		public DateTime? ShipDate { get; set; }

		[Column]
		public byte Status { get; set; }

		[Column]
		public decimal SubTotal { get; set; }

		[Column]
		public decimal TaxAmt { get; set; }

		[Column]
		public decimal TotalDue { get; set; }

		[Column]
		public int CustomerID { get; set; }

		[Column]
		public int? SalesPersonID { get; set; }

		[Column]
		public int? TerritoryID { get; set; }

		[Column]
		public int BillToAddressID { get; set; }

		[Column]
		public int ShipToAddressID { get; set; }

		[Column]
		public int ShipMethodID { get; set; }

		[Column]
		public int? CreditCardID { get; set; }

		[Column]
		public int? CurrencyRateID { get; set; }

		[Association(ThisKey = nameof(SalesOrderID), OtherKey = nameof(SalesOrderDetail.SalesOrderID))]
		public List<SalesOrderDetail> SalesOrderDetails { get; set; } = null!;

		[Association(ThisKey = nameof(CustomerID), OtherKey = nameof(Mappings.Customer.CustomerID))]
		public Customer Customer { get; set; } = null!;

		public static DataTable SchemaTable;
		public static string[]  Names      = new[] { "SalesOrderID", "AccountNumber", "Comment", "CreditCardApprovalCode", "DueDate", "Freight", "ModifiedDate", "OnlineOrderFlag", "OrderDate", "PurchaseOrderNumber", "RevisionNumber", "Rowguid", "SalesOrderNumber", "ShipDate", "Status", "SubTotal", "TaxAmt", "TotalDue", "CustomerID", "SalesPersonID", "TerritoryID", "BillToAddressID", "ShipToAddressID", "ShipMethodID", "CreditCardID", "CurrencyRateID" };
		public static Type[]    FieldTypes = new[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(decimal), typeof(DateTime), typeof(bool), typeof(DateTime), typeof(string), typeof(byte), typeof(Guid), typeof(string), typeof(DateTime), typeof(byte), typeof(decimal), typeof(decimal), typeof(decimal), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) };
		public static string[]  DbTypes    = new[] { "int", "nvarchar", "nvarchar", "varchar", "datetime", "money", "datetime", "bit", "datetime", "nvarchar", "tinyint", "uniqueidentifier", "nvarchar", "datetime", "tinyint", "money", "money", "money", "int", "int", "int", "int", "int", "int", "int", "int" };
		public static object?[] SampleRow  = new object?[] { 123, "100500", "nothing to see here, please disperse", "666", DateTime.Now, 12.34m, DateTime.Now, true, DateTime.Now, "1123787", (byte)4, Guid.NewGuid(), "sdfsdfsd", DateTime.Now, (byte)12, 1.1m, 4.2m, 423.222m, 1, 2, 3, 4, 5, 6, 7, 8 };

		static SalesOrderHeader()
		{
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);

			SchemaTable = schema;
		}
	}

	[Table(Schema = "Sales", Name = "SalesOrderDetail")]
	public class SalesOrderDetail
	{
		[PrimaryKey]
		public int SalesOrderID { get; set; }

		[PrimaryKey, Identity]
		public int SalesOrderDetailID { get; set; }

		[Column]
		public string? CarrierTrackingNumber { get; set; }

		[Column]
		public short OrderQty { get; set; }

		[Column]
		public int ProductID { get; set; }

		[Column]
		public int SpecialOfferID { get; set; }

		[Column]
		public decimal UnitPrice { get; set; }

		[Column]
		public decimal UnitPriceDiscount { get; set; }

		[Column]
		public decimal LineTotal { get; set; }

		[Column]
		public Guid rowguid { get; set; }

		[Column]
		public DateTime ModifiedDate { get; set; }

		public static DataTable SchemaTable;
		public static string[]  Names      = new[] { "SalesOrderID", "SalesOrderDetailID", "CarrierTrackingNumber", "OrderQty", "ProductID", "SpecialOfferID", "UnitPrice", "UnitPriceDiscount", "LineTotal", "rowguid", "ModifiedDate" };
		public static Type[]    FieldTypes = new[] { typeof(int), typeof(int), typeof(string), typeof(short), typeof(int), typeof(int), typeof(decimal), typeof(decimal), typeof(decimal), typeof(Guid), typeof(DateTime) };
		public static string[]  DbTypes    = new[] { "int", "int", "nvarchar", "smallint", "int", "int", "money", "money", "numeric", "uniqueidentifier", "datetime" };
		public static object?[] SampleRow  = new object?[] { 123, 22, "nothing to see here, please disperse", (short)267, 23, 33, 2.2m, 3.3m, 2.2m, Guid.NewGuid(), DateTime.Now };

		static SalesOrderDetail()
		{
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			
			SchemaTable = schema;
		}
	}

	[Table(Schema = "Sales", Name = "Customer")]
	public class Customer
	{
		[PrimaryKey, Identity]
		public int CustomerID { get; set; }

		[Column]
		public int? PersonID { get; set; }

		[Column]
		public int? StoreID { get; set; }

		[Column]
		public int? TerritoryID { get; set; }

		[Column]
		public string AccountNumber { get; set; } = null!;

		[Column]
		public Guid rowguid { get; set; }

		[Column]
		public DateTime ModifiedDate { get; set; }

		public static DataTable SchemaTable;
		public static string[]  Names      = new[] { "CustomerID", "PersonID", "StoreID", "TerritoryID", "AccountNumber", "rowguid", "ModifiedDate" };
		public static Type[]    FieldTypes = new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(string), typeof(Guid), typeof(DateTime) };
		public static string[]  DbTypes    = new[] { "int", "int", "int", "int", "varchar", "uniqueidentifier", "datetime" };
		public static object?[] SampleRow  = new object?[] { 1, 2, 3, 4, "1348", Guid.NewGuid(), DateTime.Now };

		static Customer()
		{
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);

			SchemaTable = schema;
		}
	}

	public static class EagerLoad
	{
		public static DataTable SchemaTable_SalesOrderDetails;
		public static string[]  Names_SalesOrderDetails      = new[] { "SalesOrderID", "SalesOrderID", "SalesOrderDetailID", "CarrierTrackingNumber", "OrderQty", "ProductID", "SpecialOfferID", "UnitPrice", "UnitPriceDiscount", "LineTotal", "rowguid", "ModifiedDate" };
		public static Type[]    FieldTypes_SalesOrderDetails = new[] { typeof(int), typeof(int), typeof(int), typeof(string), typeof(short), typeof(int), typeof(int), typeof(decimal), typeof(decimal), typeof(decimal), typeof(Guid), typeof(DateTime) };
		public static string[]  DbTypes_SalesOrderDetails    = new[] { "int", "int", "int", "nvarchar", "smallint", "int", "int", "money", "money", "numeric", "uniqueidentifier", "datetime" };
		public static object?[] SampleRow_SalesOrderDetails(int id) => new object?[] { id, id, 22, "nothing to see here, please disperse", (short)267, 23, 33, 2.2m, 3.3m, 2.2m, Guid.NewGuid(), DateTime.Now };

		public static DataTable SchemaTable_HeaderCustomer;
		public static string[]  Names_HeaderCustomer = new[] { "SalesOrderID", "AccountNumber", "Comment", "CreditCardApprovalCode", "DueDate", "Freight", "ModifiedDate", "OnlineOrderFlag", "OrderDate", "PurchaseOrderNumber", "RevisionNumber", "Rowguid", "SalesOrderNumber", "ShipDate", "Status", "SubTotal", "TaxAmt", "TotalDue", "CustomerID", "SalesPersonID", "TerritoryID", "BillToAddressID", "ShipToAddressID", "ShipMethodID", "CreditCardID", "CurrencyRateID", "CustomerID", "PersonID", "StoreID", "TerritoryID", "AccountNumber", "rowguid", "ModifiedDate" };
		public static Type[]    FieldTypes_HeaderCustomer = new[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(decimal), typeof(DateTime), typeof(bool), typeof(DateTime), typeof(string), typeof(byte), typeof(Guid), typeof(string), typeof(DateTime), typeof(byte), typeof(decimal), typeof(decimal), typeof(decimal), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(string), typeof(Guid), typeof(DateTime) };
		public static string[]  DbTypes_HeaderCustomer = new[] { "int", "nvarchar", "nvarchar", "varchar", "datetime", "money", "datetime", "bit", "datetime", "nvarchar", "tinyint", "uniqueidentifier", "nvarchar", "datetime", "tinyint", "money", "money", "money", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "int", "varchar", "uniqueidentifier", "datetime" };
		public static object?[] SampleRow_HeaderCustomer(int id) => new object?[] { id, "100500", "nothing to see here, please disperse", "666", DateTime.Now, 12.34m, DateTime.Now, true, DateTime.Now, "1123787", (byte)4, Guid.NewGuid(), "sdfsdfsd", DateTime.Now, (byte)12, 1.1m, 4.2m, 423.222m, id, 2, 3, 4, 5, 6, 7, 8, id, 2, 3, 4, "1348", Guid.NewGuid(), DateTime.Now };

		static EagerLoad()
		{
			var schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);

			SchemaTable_SalesOrderDetails = schema;

			schema = new DataTable();
			schema.Columns.Add("AllowDBNull", typeof(bool));
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(true);
			schema.Rows.Add(true);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);
			schema.Rows.Add(false);

			SchemaTable_HeaderCustomer = schema;
		}
	}
}
