using System.Linq;
using System.Data;
using BenchmarkDotNet.Attributes;
using LinqToDB.Benchmarks.Mappings;
using LinqToDB.Benchmarks.TestProvider;
using System;
using System.Collections.Generic;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider;

namespace LinqToDB.Benchmarks.Queries
{
	// test FetchIndividualBenchmark case from https://github.com/FransBouma/RawDataAccessBencher
	public class FetchIndividualBenchmark
	{
		private string ConnectionString = "test";
		private Func<Db, int, SalesOrderHeader> _compiled = null!;
		private QueryResult _result = null!;
		private int _key = 124;
		private string CommandText = "SELECT [SalesOrderID],[RevisionNumber],[OrderDate],[DueDate],[ShipDate],[Status],[OnlineOrderFlag],[SalesOrderNumber],[PurchaseOrderNumber],[AccountNumber],[CustomerID],[SalesPersonID],[TerritoryID],[BillToAddressID],[ShipToAddressID],[ShipMethodID],[CreditCardID],[CreditCardApprovalCode],[CurrencyRateID],[SubTotal],[TaxAmt],[Freight],[TotalDue],[Comment],[rowguid],[ModifiedDate] FROM [Sales].[SalesOrderHeader]";
		private IDataProvider _provider = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient);

		[GlobalSetup]
		public void Setup()
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


			_result = new QueryResult()
			{
				Schema     = schema,

				Names      = new[] { "SalesOrderID", "AccountNumber", "Comment", "CreditCardApprovalCode", "DueDate", "Freight", "ModifiedDate", "OnlineOrderFlag", "OrderDate", "PurchaseOrderNumber", "RevisionNumber", "Rowguid", "SalesOrderNumber", "ShipDate", "Status", "SubTotal", "TaxAmt", "TotalDue", "CustomerID", "SalesPersonID", "TerritoryID", "BillToAddressID", "ShipToAddressID", "ShipMethodID", "CreditCardID", "CurrencyRateID" },
				FieldTypes = new[] { typeof(int), typeof(string), typeof(string), typeof(string), typeof(DateTime), typeof(decimal), typeof(DateTime), typeof(bool), typeof(DateTime), typeof(string), typeof(byte), typeof(Guid), typeof(string), typeof(DateTime), typeof(byte), typeof(decimal), typeof(decimal), typeof(decimal), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) },
				DbTypes    = new[] { "int", "nvarchar", "nvarchar", "varchar", "datetime", "money", "datetime", "bit", "datetime", "nvarchar", "tinyint", "uniqueidentifier", "nvarchar", "datetime", "tinyint", "money", "money", "money", "int", "int", "int", "int", "int", "int", "int", "int" },

				Data       = new object?[][]
				{
					new object?[] { 123, "100500", "nothing to see here, please disperse", "666", DateTime.Now, 12.34m, DateTime.Now, true, DateTime.Now, "1123787", (byte)4, Guid.NewGuid(), "sdfsdfsd", DateTime.Now, (byte)12, 1.1m, 4.2m, 423.222m, 1, 2, 3, 4, 5, 6, 7, 8 }
				},
			};

			_compiled = CompiledQuery.Compile((Db db, int id) => db.SalesOrderHeader
				.Where(p => p.SalesOrderID == id)
				.FirstOrDefault());
		}

		[Benchmark]
		public SalesOrderHeader? Linq()
		{
			using (var db = new Db(_provider, _result))
			{
				return db.SalesOrderHeader
					.Where(p => p.SalesOrderID == _key)
					.FirstOrDefault();
			}
		}

		[Benchmark]
		public SalesOrderHeader? Compiled()
		{
			using (var db = new Db(_provider, _result))
				return _compiled(db, _key);
		}

		[Benchmark(Baseline = true)]
		public object? RawAdoNet()
		{
			var toExecute = new MockDbCommand(CommandText + " WHERE SalesOrderId=@p", _result);
			toExecute.Parameters.Add(new MockDbParameter("@p", _key));

			var results = MaterializeSet(toExecute);
			return results.FirstOrDefault();
		}

		private IEnumerable<SalesOrderHeader> MaterializeSet(MockDbCommand toExecute)
		{
			var headers = new List<SalesOrderHeader>();
			using (var con = new MockDbConnection(ConnectionString, _result))
			{
				toExecute.Connection = con;
				con.Open();
				var reader = toExecute.ExecuteReader();
				while (reader.Read())
				{
					var soh = new SalesOrderHeader();
					// using IsDBNull(ordinal) is slow, however it allows the usage of the typed Get<type>(ordinal) methods. This avoids
					// boxing / unboxing of the value again, which enhances performance more than IsDBNull can slow it down. 
					soh.SalesOrderID = reader.GetInt32(0);
					if (!reader.IsDBNull(1))
					{
						soh.AccountNumber = reader.GetString(1);
					}
					if (!reader.IsDBNull(2))
					{
						soh.Comment = reader.GetString(2);
					}
					if (!reader.IsDBNull(3))
					{
						soh.CreditCardApprovalCode = reader.GetString(3);
					}
					soh.DueDate = reader.GetDateTime(4);
					soh.Freight = reader.GetDecimal(5);
					soh.ModifiedDate = reader.GetDateTime(6);
					soh.OnlineOrderFlag = reader.GetBoolean(7);
					soh.OrderDate = reader.GetDateTime(8);
					if (!reader.IsDBNull(9))
					{
						soh.PurchaseOrderNumber = reader.GetString(9);
					}
					soh.RevisionNumber = reader.GetByte(10);
					soh.Rowguid = reader.GetGuid(11);
					soh.SalesOrderNumber = reader.GetString(12);
					if (!reader.IsDBNull(13))
					{
						soh.ShipDate = reader.GetDateTime(13);
					}
					soh.Status = reader.GetByte(14);
					soh.SubTotal = reader.GetDecimal(15);
					soh.TaxAmt = reader.GetDecimal(16);
					soh.TotalDue = reader.GetDecimal(17);
					soh.CustomerID = reader.GetInt32(18);
					if (!reader.IsDBNull(19))
					{
						soh.SalesPersonID = reader.GetInt32(19);
					}
					if (!reader.IsDBNull(20))
					{
						soh.TerritoryID = reader.GetInt32(20);
					}
					soh.BillToAddressID = reader.GetInt32(21);
					soh.ShipToAddressID = reader.GetInt32(22);
					soh.ShipMethodID = reader.GetInt32(23);
					if (!reader.IsDBNull(24))
					{
						soh.CreditCardID = reader.GetInt32(24);
					}
					if (!reader.IsDBNull(25))
					{
						soh.CurrencyRateID = reader.GetInt32(25);
					}
					headers.Add(soh);
				}
				reader.Close();
				reader.Dispose();
				con.Close();
			}
			return headers;
		}
	}
}
