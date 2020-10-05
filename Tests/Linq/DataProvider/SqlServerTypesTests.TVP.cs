﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using Microsoft.SqlServer.Server;

using NUnit.Framework;

using SqlDataRecordMS = Microsoft.Data.SqlClient.Server.SqlDataRecord;
using SqlMetaDataMS   = Microsoft.Data.SqlClient.Server.SqlMetaData;

namespace Tests.DataProvider
{
	public partial class SqlServerTypesTests
	{
		internal const string TYPE_NAME = "[dbo].[TestTableType]";
		public class TVPRecord
		{
			public int? Id { get; set; }

			public string? Name { get; set; }
		}

		internal static TVPRecord[] TestUDTData = new[]
		{
			new TVPRecord(),
			new TVPRecord() { Id = 1, Name = "Value1" },
			new TVPRecord() { Id = 2, Name = "Value2" }
		};

		public static DataTable GetDataTable()
		{
			var table = new DataTable();

			table.Columns.Add("Id", typeof(int));
			table.Columns.Add("Name", typeof(string));

			foreach (var record in TestUDTData)
			{
				table.Rows.Add(record.Id, record.Name);
			}

			return table;
		}

		public static IEnumerable<SqlDataRecord> GetSqlDataRecords()
		{
			var sqlRecord = new SqlDataRecord(
				new SqlMetaData("Id",   SqlDbType.Int),
				new SqlMetaData("Name", SqlDbType.NVarChar, 10));

			foreach (var record in TestUDTData)
			{
				sqlRecord.SetValue(0, record.Id);
				sqlRecord.SetValue(1, record.Name);

				yield return sqlRecord;
			}
		}

		public static IEnumerable<SqlDataRecordMS> GetSqlDataRecordsMS()
		{
			var sqlRecord = new SqlDataRecordMS(
				new SqlMetaDataMS("Id", SqlDbType.Int),
				new SqlMetaDataMS("Name", SqlDbType.NVarChar, 10));

			foreach (var record in TestUDTData)
			{
				sqlRecord.SetValue(0, record.Id);
				sqlRecord.SetValue(1, record.Name);

				yield return sqlRecord;
			}
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
				yield return new ParameterFactory("SqlDataRecords", _ => _.Connection is Microsoft.Data.SqlClient.SqlConnection ? (object)GetSqlDataRecordsMS() : GetSqlDataRecords());

				// TODO: doesn't work now as DbDataReader converted to Lst<object> of DbDataRecordInternal somewhere in linq2db
				// before we can pass it to provider
				// as DbDataReader
				//var sql = new StringBuilder();
				//foreach (var record in TestUDTData)
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
		private static ITable<TVPRecord> TableValue(DataParameter p)
		{
			throw new InvalidOperationException();
		}

		static readonly MethodInfo _methodInfo = MemberHelper.MethodOf(() => TableValue(null!));

		public static ITable<TVPRecord> TableValue(IDataContext ctx, DataParameter p)
		{
			return ctx.GetTable<TVPRecord>(null, _methodInfo, p);
		}

		[Test]
		public void TableValuedParameterProcedureTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(DataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (new DisableBaseline("Provider-specific output", IsMsProvider(context)))
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = db.QueryProc<TVPRecord>("TableTypeTestProc", testCase.Factory(external));

				AreEqualWithComparer(TestUDTData, result);
			}
		}

		[Test]
		public void TableValuedParameterInQueryUsingFromSqlTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (new DisableBaseline("Provider-specific output", IsMsProvider(context)))
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = from record in db.FromSql<TVPRecord>($"{testCase.Factory(external)}")
							 select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(TestUDTData, result);
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
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			using (var table = db.CreateTempTable<TestMergeTVPTable>())
			{
				var cnt = table
					.Merge()
					.Using(db.FromSql<TVPRecord>($"{testCase.Factory(external)}").Where(_ => _.Id != null))
					.On((t, s) => t.Id == s.Id)
					.InsertWhenNotMatched(s => new TestMergeTVPTable()
					{
						Id   = s.Id!.Value,
						Name = s.Name
					})
					.Merge();

				var data = table.OrderBy(_ => _.Id).ToArray();

				Assert.AreEqual(2, cnt);
				Assert.AreEqual(2, data.Length);
				Assert.AreEqual(1, data[0].Id);
				Assert.AreEqual("Value1", data[0].Name);
				Assert.AreEqual(2, data[1].Id);
				Assert.AreEqual("Value2", data[1].Name);
			}
		}

		[ActiveIssue("DataParameter not supported by TableExpressionAttribute")]
		[Test]
		public void TableValuedParameterInQueryUsingTableMethodTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] DataParameterFactoryTestCase testCase)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result =
					from record in TableValue(db, testCase.Factory(external))
					select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(TestUDTData, result);
			}
		}

		[Test]
		public void TableValuedParameterProcedureAsNullTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = db.QueryProc<TVPRecord>("TableTypeTestProc", new DataParameter("@table", null, DataType.Structured) {  DbType = TYPE_NAME});

				Assert.AreEqual(0, result.ToList().Count);
			}
		}

		[Test]
		public void TableValuedParameterAsNullInQueryUsingFromSqlTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = from record in db.FromSql<TVPRecord>($"select * from  {new DataParameter("table", null, DataType.Structured) { DbType = TYPE_NAME }}")
							 select new TVPRecord() { Id = record.Id, Name = record.Name };

				Assert.AreEqual(0, result.ToList().Count);
			}
		}

		[Test]
		public void TableValuedParameterProcedureT4Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = TableTypeTestProc(db, GetDataTable());

				AreEqualWithComparer(TestUDTData, result);
			}
		}

		// this is procedure, generated by T4 template (without db name and "this" for connection parameter)
		public static IEnumerable<TVPRecord> TableTypeTestProc(DataConnection dataConnection, DataTable @table)
		{
			return dataConnection.QueryProc<TVPRecord>("[TableTypeTestProc]",
				new DataParameter("@table", @table, DataType.Structured) { DbType = "[dbo].[TestTableType]" });
		}
	}
}
