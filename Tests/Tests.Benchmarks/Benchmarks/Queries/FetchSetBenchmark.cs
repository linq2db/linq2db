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
	public class FetchSetBenchmark
	{
		private string ConnectionString = "test";
		private Func<Db, IQueryable<SalesOrderHeader>> _compiled = null!;
		private QueryResult _result = null!;
		private string CommandText = "SELECT [SalesOrderID],[RevisionNumber],[OrderDate],[DueDate],[ShipDate],[Status],[OnlineOrderFlag],[SalesOrderNumber],[PurchaseOrderNumber],[AccountNumber],[CustomerID],[SalesPersonID],[TerritoryID],[BillToAddressID],[ShipToAddressID],[ShipMethodID],[CreditCardID],[CreditCardApprovalCode],[CurrencyRateID],[SubTotal],[TaxAmt],[Freight],[TotalDue],[Comment],[rowguid],[ModifiedDate] FROM [Sales].[SalesOrderHeader]";
		private IDataProvider _provider = new SqlServerDataProvider(ProviderName.SqlServer2008, SqlServerVersion.v2008, SqlServerProvider.SystemDataSqlClient);

		[GlobalSetup]
		public void Setup()
		{
			_result = new QueryResult()
			{
				Schema     = SalesOrderHeader.SchemaTable,
				Names      = SalesOrderHeader.Names,
				FieldTypes = SalesOrderHeader.FieldTypes,
				DbTypes    = SalesOrderHeader.DbTypes,
				Data       = Enumerable.Range(0, 31465).Select(_ => SalesOrderHeader.SampleRow).ToArray()
				//Data       = Enumerable.Range(0, 100).Select(_ => SalesOrderHeader.SampleRow).ToArray()
			};

			_compiled = CompiledQuery.Compile((Db db) => db.SalesOrderHeader);
		}

		[Benchmark]
		public List<SalesOrderHeader> Linq()
		{
			using (var db = new Db(_provider, _result))
			{
				return db.SalesOrderHeader.ToList();
			}
		}

		[Benchmark]
		public List<SalesOrderHeader> Compiled()
		{
			using (var db = new Db(_provider, _result))
				return _compiled(db).ToList();
		}

		[Benchmark(Baseline = true)]
		public object? RawAdoNet()
		{
			return MaterializeSet(new MockDbCommand(CommandText, _result));
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
