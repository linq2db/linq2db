using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;

using Microsoft.SqlServer.Server;

using NUnit.Framework;

namespace Tests.DataProvider
{
	public partial class SqlServerTypesTests
	{
		private const string TYPE_NAME = "[dbo].[TestTableType]";
		public class TVPRecord
		{
			public int?   Id   { get; set; }

			public string Name { get; set; }
		}

		private static TVPRecord[] TestData = new[]
		{
			new TVPRecord(),
			new TVPRecord() { Id = 1, Name = "Value1" },
			new TVPRecord() { Id = 2, Name = "Value2" }
		};

		public static DataTable GetDataTable()
		{
			var table = new DataTable();

			table.Columns.Add("Id",   typeof(int));
			table.Columns.Add("Name", typeof(string));

			foreach (var record in TestData)
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

			foreach (var record in TestData)
			{
				sqlRecord.SetValue(0, record.Id);
				sqlRecord.SetValue(1, record.Name);

				yield return sqlRecord;
			}
		}

		public static IEnumerable<Func<DataConnection, object>> ParameterFactories
		{
			get
			{
				// as DataTable
				yield return _ => GetDataTable();
				// as IEnumerable<SqlDataRecord>
				yield return _ => GetSqlDataRecords();

				// TODO: doesn't work now as DbDataReader converted to Lst<object> of DbDataRecordInternal somewhere in linq2db
				// before we can pass it to provider
				// as DbDataReader
				//var sql = new StringBuilder();
				//foreach (var record in TestData)
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

		public static IEnumerable<Func<DataConnection, DataParameter>> DataParameterFactories
		{
			get
			{
				foreach (var valueFactory in ParameterFactories)
				{
					yield return cn => new DataParameter("@table", valueFactory(cn));
					yield return cn => new DataParameter("@table", valueFactory(cn), DataType.Structured);
					yield return cn => new DataParameter("@table", valueFactory(cn)) { DbType = TYPE_NAME };
					yield return cn => new DataParameter("@table", valueFactory(cn), DataType.Structured) { DbType = TYPE_NAME };
				}
			}
		}

		public static IEnumerable<Func<DataConnection, DataParameter>> QueryDataParameterFactories
		{
			get
			{
				foreach (var valueFactory in ParameterFactories)
				{
					yield return cn => new DataParameter("table", valueFactory(cn)) { DbType = TYPE_NAME };
					yield return cn => new DataParameter("table", valueFactory(cn), DataType.Structured) { DbType = TYPE_NAME };
				}
			}
		}

		[Sql.TableExpression("select * from {0}")]
		private static ITable<TVPRecord> TableValue(DataParameter p)
		{
			throw new InvalidOperationException();
		}

		static readonly MethodInfo _methodInfo = MemberHelper.MethodOf(() => TableValue(null));

		public static ITable<TVPRecord> TableValue(IDataContext ctx, DataParameter p)
		{
			return ctx.GetTable<TVPRecord>(null, _methodInfo, p);
		}

		[Test]
		public void TableValuedParameterProcedureTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(DataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = db.QueryProc<TVPRecord>("TableTypeTestProc", parameterGetter(external));

				AreEqualWithComparer(TestData, result);
			}
		}

		[Test]
		public void TableValuedParameterInQueryUsingFromSqlTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = from record in db.FromSql<TVPRecord>($"{parameterGetter(external)}")
							 select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(TestData, result);
			}
		}

		[ActiveIssue("DataParameter not supported by TableExpressionAttribute")]
		[Test]
		public void TableValuedParameterInQueryUsingTableMethodTest(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result =
					from record in TableValue(db, parameterGetter(external))
					select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqualWithComparer(TestData, result);
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

				AreEqualWithComparer(TestData, result);
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
