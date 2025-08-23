using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.DataProvider
{
	public partial class SqlServerTypesTests
	{
		internal const string TYPE_NAME = "[dbo].[TestTableType]";

		private static DataTable GetDataTable()
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(int));
			table.Columns.Add("Name", typeof(string));

			foreach (var record in SqlServerTestUtils.TestUDTData)
			{
				table.Rows.Add(record.Id, record.Name);
			}

			return table;
		}

		public class ParameterFactory
		{
			public ParameterFactory(string name, Func<DataConnection, object> factory)
			{
				Name    = name;
				Factory = factory;
			}

			public string Name                          { get; }
			public Func<DataConnection, object> Factory { get; }
		}

		public static IEnumerable<ParameterFactory> ParameterFactories
		{
			get
			{
				// as DataTable
				yield return new ParameterFactory("DataTable", _ => GetDataTable());

				// as IEnumerable<SqlDataRecord>
				yield return new ParameterFactory("SqlDataRecords", db =>
				{
					var connection = db.OpenDbConnection();
					return connection is Microsoft.Data.SqlClient.SqlConnection ? (object)SqlServerTestUtils.GetSqlDataRecordsMS() : SqlServerTestUtils.GetSqlDataRecords();
				});

				// TODO: doesn't work now as DbDataReader converted to Lst<object> of DbDataRecordInternal somewhere in linq2db
				// before we can pass it to provider
				// as DbDataReader
				//var sql = new StringBuilder();
				//foreach (var record in SqlServerTestUtils.TestUDTData)
				//{
				//	if (sql.Length > 0)
				//		sql.Append(" UNION ALL ");
				//	sql.AppendFormat(
				//		"SELECT {0} as Id, {1} as Name",
				//		record.Id == null ? "NULL" : record.Id.Value.ToString(),
				//		record.Name == null ? "NULL" : "'" + record.Name + "'");
				//}
				//yield return cn => cn.ExecuteReader(sql.ToString()).Reader;
			}
		}

		public class DataParameterFactoryTestCase
		{
			private readonly string _name;

			public DataParameterFactoryTestCase(string testCaseName, Func<DataConnection, DataParameter> factory)
			{
				_name   = testCaseName;
				Factory = factory;
			}

			public Func<DataConnection, DataParameter> Factory { get; }

			public override string ToString() => _name;
		}

		public static IEnumerable<DataParameterFactoryTestCase> DataParameterFactories
		{
			get
			{
				foreach (var valueFactory in ParameterFactories)
				{
					yield return new DataParameterFactoryTestCase($"Parameter - untyped ({valueFactory.Name})"          , cn => new DataParameter("@table", valueFactory.Factory(cn)));
					yield return new DataParameterFactoryTestCase($"Parameter - DataType ({valueFactory.Name})"         , cn => new DataParameter("@table", valueFactory.Factory(cn), DataType.Structured));
					yield return new DataParameterFactoryTestCase($"Parameter - DbType ({valueFactory.Name})"           , cn => new DataParameter("@table", valueFactory.Factory(cn)) { DbType = TYPE_NAME });
					yield return new DataParameterFactoryTestCase($"Parameter - DataType + DbType ({valueFactory.Name})", cn => new DataParameter("@table", valueFactory.Factory(cn), DataType.Structured) { DbType = TYPE_NAME });
				}
			}
		}

		public static IEnumerable<DataParameterFactoryTestCase> QueryDataParameterFactories
		{
			get
			{
				foreach (var valueFactory in ParameterFactories)
				{
					yield return new DataParameterFactoryTestCase($"Query - DbType ({valueFactory.Name})"           , cn => new DataParameter("table", valueFactory.Factory(cn)) { DbType = TYPE_NAME });
					yield return new DataParameterFactoryTestCase($"Query - DataType + DbType ({valueFactory.Name})", cn => new DataParameter("table", valueFactory.Factory(cn), DataType.Structured) { DbType = TYPE_NAME });
				}
			}
		}

		[Sql.TableExpression("select * from {0}")]
		private static ITable<SqlServerTestUtils.TVPRecord> TableValue(DataParameter p)
		{
			throw new InvalidOperationException();
		}

		private static ITable<SqlServerTestUtils.TVPRecord> TableValue(IDataContext ctx, DataParameter p)
		{
			return ctx.TableFromExpression<SqlServerTestUtils.TVPRecord>(() => TableValue(p));
		}

		[Test]
		public void TableValuedParameterProcedureTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(DataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (new DisableBaseline("Provider-specific output", IsMsProvider(context)))
			using (var external = GetDataConnection(context))
			using (var db = GetDataContext(context))
			{
				var result = db.QueryProc<SqlServerTestUtils.TVPRecord>("TableTypeTestProc", testCase.Factory(external));

				AreEqualWithComparer(SqlServerTestUtils.TestUDTData, result);
			}
		}

		[Test]
		public void TableValuedParameterInQueryUsingFromSqlTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (new DisableBaseline("Provider-specific output", IsMsProvider(context)))
			using (var external = GetDataConnection(context))
			using (var db = GetDataConnection(context))
			{
				var result = from record in db.FromSql<SqlServerTestUtils.TVPRecord>($"{testCase.Factory(external)}")
							 select new SqlServerTestUtils.TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(SqlServerTestUtils.TestUDTData, result);
			}
		}

		[Table]
		public class TestMergeTVPTable
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Test]
		public void TableValuedParameterInMergeSource(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (new DisableBaseline("Provider-specific output", IsMsProvider(context)))
			using (var external = GetDataConnection(context))
			using (var db = GetDataConnection(context))
			using (var table = db.CreateTempTable<TestMergeTVPTable>())
			{
				var cnt = table
					.Merge()
					.Using(db.FromSql<SqlServerTestUtils.TVPRecord>($"{testCase.Factory(external)}").Where(_ => _.Id != null))
					.On((t, s) => t.Id == s.Id)
					.InsertWhenNotMatched(s => new TestMergeTVPTable()
					{
						Id   = s.Id!.Value,
						Name = s.Name
					})
					.Merge();

				var data = table.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(cnt, Is.EqualTo(2));
					Assert.That(data, Has.Length.EqualTo(2));
					Assert.That(data[0].Id, Is.EqualTo(1));
					Assert.That(data[0].Name, Is.EqualTo("Value1"));
					Assert.That(data[1].Id, Is.EqualTo(2));
					Assert.That(data[1].Name, Is.EqualTo("Value2"));
				}
			}
		}

		[ActiveIssue("DataParameter not supported by TableExpressionAttribute")]
		[Test]
		public void TableValuedParameterInQueryUsingTableMethodTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (var external = GetDataConnection(context))
			using (var db = GetDataConnection(context))
			{
				var result =
					from record in TableValue(db, testCase.Factory(external))
					select new SqlServerTestUtils.TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(SqlServerTestUtils.TestUDTData, result);
			}
		}

		[Test]
		public void TableValuedParameterProcedureAsNullTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = GetDataConnection(context))
			using (var db = GetDataContext(context))
			{
				var result = db.QueryProc<SqlServerTestUtils.TVPRecord>("TableTypeTestProc", new DataParameter("@table", null, DataType.Structured) {  DbType = TYPE_NAME});

				Assert.That(result.ToList(), Is.Empty);
			}
		}

		[Test]
		public void TableValuedParameterAsNullInQueryUsingFromSqlTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = GetDataConnection(context))
			using (var db = GetDataConnection(context))
			{
				var result = from record in db.FromSql<SqlServerTestUtils.TVPRecord>($"select * from  {new DataParameter("table", null, DataType.Structured) { DbType = TYPE_NAME }}")
							 select new SqlServerTestUtils.TVPRecord() { Id = record.Id, Name = record.Name };

				Assert.That(result.ToList(), Is.Empty);
			}
		}

		public class Result
		{
			public int[]? Ints { get; set; }
		}
		
		[Test]
		public void TVPCachingIssue(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				Result[] GetResult(params int[] values)
				{
					using var table = new DataTable();

					table.Columns.Add("Id", typeof(int));
					table.Columns.Add("Name", typeof(string));
				
					foreach (var value in values)
						table.Rows.Add(value, "_");

					var parameter = new DataParameter("table", table, DataType.Structured) { DbType = TYPE_NAME };

					var query = from x in db.FromSql<SqlServerTestUtils.TVPRecord>($"{parameter}") select x.Id!.Value;

					return db.GetTable<Person>()
						.Where(p => query.Contains(p.ID))
						.Select(p1 => new Result
						{
							Ints = db.GetTable<Person>()
								.Where(p2 => p2.ID > p1.ID)
								.Select(p => p.ID)
								.ToArray()
						}).ToArray();
				}

				void AssertResult(Result[] r1, Result[] r2)
				{
					Assert.That(r2, Has.Length.EqualTo(r1.Length));
					for (var i = 0; i < r1.Length; i++)
					{
						var ints1 = r1[i].Ints!;
						var ints2 = r2[i].Ints!;
						Assert.That(ints2, Has.Length.EqualTo(ints1.Length));
					
						for (var j = 0; j < ints1.Length; j++)
							Assert.That(ints2[j], Is.EqualTo(ints1[j]));
					}
				}
				
				Result[] res1;
				Result[] res2;
				
				// workaround
				using (NoLinqCache.Scope())
				{
					res1 = GetResult(1, 2);
					res2 = GetResult(2, 3); 
				}
				
				var res3 = GetResult(1, 2);
				var res4 = GetResult(2, 3);
				
				AssertResult(res1, res3); // pass
				AssertResult(res2, res4); // fail
			}
		}

		[Test]
		public void TableValuedParameterProcedureT4Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = GetDataConnection(context))
			using (var db = GetDataConnection(context))
			{
				using var table = GetDataTable();
				var result = TableTypeTestProc(db, table);

				AreEqualWithComparer(SqlServerTestUtils.TestUDTData, result);
			}
		}

		// this is procedure, generated by T4 template (without db name and "this" for connection parameter)
		private static IEnumerable<SqlServerTestUtils.TVPRecord> TableTypeTestProc(IDataContext dataConnection, DataTable @table)
		{
			return dataConnection.QueryProc<SqlServerTestUtils.TVPRecord>("[TableTypeTestProc]",
				new DataParameter("@table", @table, DataType.Structured) { DbType = "[dbo].[TestTableType]" });
		}
	}
}
