using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SqlExtensionsTests : TestBase
	{
		[Table("sample_table")]
		sealed class SampleClass
		{
			[Column("id")]    public int Id    { get; set; }
			[Column("value")] public int Value { get; set; }
		}

		[Test]
		public void FieldNameTests1([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[]{new SampleClass{Id = 1, Value = 2} }))
			{
				var query = from t in table
					select new
					{
						FieldIdName1  = Sql.FieldName(t.Id),
						FieldIdName2  = Sql.FieldName(t.Id, true),
						FieldIdNameNQ = Sql.FieldName(t.Id, false),
					};

				var result = query.First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.FieldIdName1, Is.EqualTo("[id]"));
					Assert.That(result.FieldIdName2, Is.EqualTo("[id]"));
					Assert.That(result.FieldIdNameNQ, Is.EqualTo("id"));
				}
			}
		}

		[Test]
		public void FieldNameTests2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[]{new SampleClass{Id = 1, Value = 2} }))
			{
				var query = from t in table
					select new
					{
						FieldIdName1  = Sql.FieldName(table, _ => _.Id),
						FieldIdName2  = Sql.FieldName(table, _ => _.Id, true),
						FieldIdNameNQ = Sql.FieldName(table, _ => _.Id, false),
					};

				var result = query.First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.FieldIdName1, Is.EqualTo("[id]"));
					Assert.That(result.FieldIdName2, Is.EqualTo("[id]"));
					Assert.That(result.FieldIdNameNQ, Is.EqualTo("id"));

					Assert.That(Sql.FieldName(table, _ => _.Value), Is.EqualTo("[value]"));
					Assert.That(Sql.FieldName(table, _ => _.Value, true), Is.EqualTo("[value]"));
					Assert.That(Sql.FieldName(table, _ => _.Value, false), Is.EqualTo("value"));
				}
			}
		}

		[Test]
		public void TableNameTests1([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[]{new SampleClass{Id = 1, Value = 2} }))
			{
				var tt = db.GetTable<SampleClass>().TableName("table_name").SchemaName("schema").DatabaseName("database");

				var query = from t in table
					select new
					{
						TableName1 = Sql.TableName(tt),
						TableName2 = Sql.TableName(tt,   Sql.TableQualification.Full),
						TableName3 = Sql.TableName(tt,   Sql.TableQualification.TableName),
						TableName4 = Sql.TableName(tt,   Sql.TableQualification.None),
						TableName_Schema   = Sql.TableName(tt,   Sql.TableQualification.SchemaName),
						TableName_Database = Sql.TableName(tt, Sql.TableQualification.DatabaseName),
					};

				var result = query.First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.TableName1, Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(result.TableName2, Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(result.TableName3, Is.EqualTo("[table_name]"));
					Assert.That(result.TableName4, Is.EqualTo("table_name"));
					Assert.That(result.TableName_Schema, Is.EqualTo("[schema].[table_name]"));
					Assert.That(result.TableName_Database, Is.EqualTo("[database]..[table_name]"));
				}
			}
		}

		[Test]
		public void TableNameTests2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var tt = db.GetTable<SampleClass>().TableName("table_name").SchemaName("schema").DatabaseName("database");

				var query = from t in tt
					select new
					{
						TableName1 = Sql.TableName(t),
						TableName2 = Sql.TableName(t,   Sql.TableQualification.Full),
						TableName3 = Sql.TableName(t,   Sql.TableQualification.TableName),
						TableName4 = Sql.TableName(t,   Sql.TableQualification.None),
						TableName_Schema   = Sql.TableName(t, Sql.TableQualification.SchemaName),
						TableName_Database = Sql.TableName(t, Sql.TableQualification.DatabaseName),
					};

				BaselinesManager.LogQuery(query.ToSqlQuery().Sql);

				var ast = query.GetSelectQuery();

				string GetColumnValue(int index)
				{
					return (string)((SqlValue)ast.Select.Columns[index].Expression).Value!;
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetColumnValue(0), Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(GetColumnValue(1), Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(GetColumnValue(2), Is.EqualTo("[table_name]"));
					Assert.That(GetColumnValue(3), Is.EqualTo("table_name"));
					Assert.That(GetColumnValue(4), Is.EqualTo("[schema].[table_name]"));
					Assert.That(GetColumnValue(5), Is.EqualTo("[database]..[table_name]"));
				}
			}
		}

		[Test]
		public void TableExprTests1([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[]{new SampleClass{Id = 1, Value = 2} }))
			{
				var tt = db.GetTable<SampleClass>().TableName("table_name").SchemaName("schema").DatabaseName("database");

				var query = from t in table
					select new
					{
						TableName1 = Sql.Expr<string>($"'{Sql.TableExpr(tt)}'"),
						TableName2 = Sql.Expr<string>($"'{Sql.TableExpr(tt,   Sql.TableQualification.Full)}'"),
						TableName3 = Sql.Expr<string>($"'{Sql.TableExpr(tt,   Sql.TableQualification.TableName)}'"),
						TableName4 = Sql.Expr<string>($"'{Sql.TableExpr(tt,   Sql.TableQualification.None)}'"),
						TableName_Schema   = Sql.Expr<string>($"'{Sql.TableExpr(tt, Sql.TableQualification.SchemaName)}'"),
						TableName_Database = Sql.Expr<string>($"'{Sql.TableExpr(tt, Sql.TableQualification.DatabaseName)}'")
					};

				var result = query.First();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result.TableName1, Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(result.TableName2, Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(result.TableName3, Is.EqualTo("[table_name]"));
					Assert.That(result.TableName4, Is.EqualTo("table_name"));
					Assert.That(result.TableName_Schema, Is.EqualTo("[schema].[table_name]"));
					Assert.That(result.TableName_Database, Is.EqualTo("[database]..[table_name]"));
				}
			}
		}

		[Test]
		public void TableExprTests2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var tt = db.GetTable<SampleClass>().TableName("table_name").SchemaName("schema").DatabaseName("database");

				var query = from t in tt
					select new
					{
						TableName1 = Sql.TableExpr(t),
						TableName2 = Sql.TableExpr(t,   Sql.TableQualification.Full),
						TableName3 = Sql.TableExpr(t,   Sql.TableQualification.TableName),
						TableName4 = Sql.TableExpr(t,   Sql.TableQualification.None),
						TableName_Schema   = Sql.TableExpr(t, Sql.TableQualification.SchemaName),
						TableName_Database = Sql.TableExpr(t, Sql.TableQualification.DatabaseName),
					};

				BaselinesManager.LogQuery(query.ToSqlQuery().Sql);

				var ast = query.GetSelectQuery();

				string GetColumnValue(int index)
				{
					return ((SqlFragment)ast.Select.Columns[index].Expression).Expr;
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(GetColumnValue(0), Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(GetColumnValue(1), Is.EqualTo("[database].[schema].[table_name]"));
					Assert.That(GetColumnValue(2), Is.EqualTo("[table_name]"));
					Assert.That(GetColumnValue(3), Is.EqualTo("table_name"));
					Assert.That(GetColumnValue(4), Is.EqualTo("[schema].[table_name]"));
					Assert.That(GetColumnValue(5), Is.EqualTo("[database]..[table_name]"));
				}
			}
		}

		[Test]
		public void ExprPredicateTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var sampleData = new[]
			{
				new SampleClass{Id = 1, Value = 2},
				new SampleClass{Id = 3, Value = 2}
			};

			using (var db = GetDataContext(context))
			{
				using (var table = db.CreateLocalTable<SampleClass>("sample_table_temp", sampleData))
				{
					var query = from t in table
						where Sql.Expr<bool>($"{t.Id} BETWEEN {new DataParameter("z", 0, DataType.Int32)} AND {Sql.FieldExpr(t.Value)}")
						select t;

					Assert.That(query.Count(), Is.EqualTo(1));
				}
			}
		}

		public class FreeTextKey<T>
		{
			public T   Key = default!;
			public int Rank;
		}

		[Test]
		public void FreeTextTableTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = db.GetTable<SampleClass>().TableName("table_name").SchemaName("schema").DatabaseName("database");

				var queryText = "some query text";

				var query1 = from t in table
					from ft in db.FromSql<FreeTextKey<int>>(
							$"FREETEXTTABLE({Sql.TableExpr(t)}, {Sql.FieldExpr(table, f => f.Value)}, {queryText})")
						.InnerJoin(ft => ft.Key == t.Id)
					select t;

				var query1Str = query1.ToSqlQuery().Sql;

				BaselinesManager.LogQuery(query1Str);

				var query2 = from t in table
					from ft in db.FromSql<FreeTextKey<int>>(
							$"FREETEXTTABLE({Sql.TableExpr(t)}, {Sql.FieldExpr(t.Value)}, {queryText})")
						.InnerJoin(ft => ft.Key == t.Id)
					select t;

				var query2Str = query2.ToSqlQuery().Sql;

				BaselinesManager.LogQuery(query2Str);

				var query3 = db.FromSql<FreeTextKey<int>>(
					$"FREETEXTTABLE({Sql.TableExpr(table)}, {Sql.FieldExpr(table, t => t.Value)}, {queryText})");

				var query3Str = query3.ToSqlQuery().Sql;

				BaselinesManager.LogQuery(query3Str);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query1Str, Does.Contain("FREETEXTTABLE([database].[schema].[table_name], [value],"));
					Assert.That(query2Str, Does.Contain("FREETEXTTABLE([database].[schema].[table_name], [value],"));
					Assert.That(query3Str, Does.Contain("FREETEXTTABLE([database].[schema].[table_name], [value],"));
				}
			}
		}

		[Test]
		public void TestSqlCollate1(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				TestProvName.AllSapHana,
				TestProvName.AllOracle11,
				TestProvName.AllInformix,
				TestProvName.AllSybase)]
			string context)
		{
			var collation = TestUtils.GetValidCollationName(GetProviderName(context, out _));

			using (var db = GetDataContext(context))
			{
				db.Person.OrderBy(_ => "1" + Sql.Collate(_.FirstName, collation) + "2").ToList();
			}
		}

		[Test]
		public void TestSqlCollate2(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				TestProvName.AllSapHana,
				TestProvName.AllOracle11,
				TestProvName.AllInformix,
				TestProvName.AllSybase)]
			string context)
		{
			var collation = TestUtils.GetValidCollationName(GetProviderName(context, out _));

			using (var db = GetDataContext(context))
			{
				db.Person.Select(_ => new { CollatedName = "1" + Sql.Collate(_.FirstName, collation) + "2" }).ToList();
			}
		}
	}
}
