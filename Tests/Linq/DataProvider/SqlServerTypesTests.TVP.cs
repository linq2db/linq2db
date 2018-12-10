using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using Microsoft.SqlServer.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Tests.Tools;

namespace Tests.DataProvider
{
	public partial class SqlServerTypesTests
	{
		private const string TYPE_NAME = "TestTableType";
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
		private static ITable<TRecord> TableValue<TRecord>(DataParameter p)
		{
			throw new InvalidOperationException();
		}

		static readonly MethodInfo _methodInfo = MemberHelper.MethodOf(() => TableValue<object>(null)).GetGenericMethodDefinition();

		public static ITable<TRecord> TableValue<TRecord>(IDataContext ctx, DataParameter p)
			where TRecord : class
		{
			return ctx.GetTable<TRecord>(null, _methodInfo.MakeGenericMethod(typeof(TRecord)), p);
		}

		[Test]
		public void TableValuedParameterProcedureTest(
			[IncludeDataSources(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context,
			[ValueSource(nameof(DataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				var result = db.QueryProc<TVPRecord>("TableTypeTestProc", parameterGetter(external));

				AreEqual(TestData, result, ComparerBuilder<TVPRecord>.GetEqualityComparer(true));
			}
		}

		[Test]
		public void TableValuedParameterInQueryUsingFromSqlTest(
			[IncludeDataSources(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				// extra select is not required and just demonstrates how we can combine fromsql with linq query
				var result = from record in db.FromSql<TVPRecord>($"select * from  {parameterGetter(external)}")
							 select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqual(TestData, result, ComparerBuilder<TVPRecord>.GetEqualityComparer(true));
			}
		}

		[Test]
		public void TableValuedParameterInQueryUsingTableMethodTest(
			[IncludeDataSources(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context,
			[ValueSource(nameof(QueryDataParameterFactories))] Func<DataConnection, DataParameter> parameterGetter)
		{
			using (var external = new DataConnection(context))
			using (var db = new DataConnection(context))
			{
				// extra select is not required and just demonstrates how we can combine fromsql with linq query
				var result = from record in TableValue<TVPRecord>(db, parameterGetter(external))
							 select new TVPRecord() { Id = record.Id, Name = record.Name };

				AreEqual(TestData, result, ComparerBuilder<TVPRecord>.GetEqualityComparer(true));
			}
		}
	}
}
