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

		public Db(IDataProvider provider, QueryResult result) : base(provider, new MockDbConnection(result, ConnectionState.Open))
		{
		}

		public ITable<SalesOrderHeader> SalesOrderHeader => GetTable<SalesOrderHeader>();

		public ITable<CreditCard> CreditCards => GetTable<CreditCard>();

		public ITable<SalesOrderHeader> SalesOrderHeaders => GetTable<SalesOrderHeader>();
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
	}
}
