using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using Tests.Linq.Helpers.InExpression;
using Tests.Linq.Helpers.JsonObjectSet;
using Tests.Model;

using NUnit.Framework;

using Shouldly;

using Newtonsoft.Json;

using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1, Value = 2 } });
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

		[Test]
		public void FieldNameTests2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1, Value = 2 } });
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

		[Test]
		public void TableNameTests1([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1, Value = 2 } });
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

		[Test]
		public void TableNameTests2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void TableExprTests1([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1, Value = 2 } });
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

		[Test]
		public void TableExprTests2([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void ExprPredicateTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var sampleData = new[]
			{
				new SampleClass{Id = 1, Value = 2},
				new SampleClass{Id = 3, Value = 2}
			};

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", sampleData);
			var query = from t in table
						where Sql.Expr<bool>($"{t.Id} BETWEEN {new DataParameter("z", 0, DataType.Int32)} AND {Sql.FieldExpr(t.Value)}")
						select t;

			Assert.That(query.Count(), Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_FormatChanges([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1 }, new SampleClass { Id = 2 } });

			IQueryable<int> Query(string sql) =>
				from t in table
				where t.Id == Sql.Expr<int>(FormattableStringFactory.Create(sql))
				select t.Id;

			Query("1")
				.ToArray()
				.ShouldBe([1]);

			Query("2")
				.ToArray()
				.ShouldBe([2]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_RawSqlString_ArgumentsChange([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>("sample_table_temp", new[] { new SampleClass { Id = 1 }, new SampleClass { Id = 2 } });

			IQueryable<int> Query(params object[] arguments) =>
				from t in table
				where t.Id == Sql.Expr<int>("{0}", arguments)
				select t.Id;

			Query(1)
				.ToArray()
				.ShouldBe([1]);

			Query(2L)
				.ToArray()
				.ShouldBe([2]);

			Query(1, 3)
				.ToArray()
				.ShouldBe([1]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Compiled_Interpolated_BuildsOnce([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			var query = CompiledQuery.Compile((IDataContext dc, int id) =>
				dc.GetTable<Model.Person>()
					.Where(p => p.ID == Sql.Expr<int>($"{id}"))
					.Select(p => p.ID));

			var query1 = query(db, 1);
			query1.ToArray().ShouldBe([1]);

			var query2 = query(db, 2);
			query2.ToArray().ShouldBe([2]);

			query2.GetCompiledQueryInfo().ShouldBeSameAs(query1.GetCompiledQueryInfo());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Compiled_CapturedArgumentArray([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool reassign)
		{
			using var db = GetDataConnection(context);

			var args = new object[] { 1 };

			var query = CompiledQuery.Compile((IDataContext dc) =>
				dc.GetTable<Model.Person>()
					.Where(p => p.ID == Sql.Expr<int>("{0}", args))
					.Select(p => p.ID));

			query(db)
				.ToArray()
				.ShouldBe([1]);

			if (reassign)
				args = [2];
			else
				args[0] = 2;

			query(db)
				.ToArray()
				.ShouldBe([2]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Compiled_ArgumentArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			var query = CompiledQuery.Compile((IDataContext dc, object?[] args) =>
				dc.GetTable<Model.Person>()
					.Where(p => p.ID == Sql.Expr<int?>("{0}", args!))
					.Select(p => p.ID));

			var query1 = query(db, [1]);

			query1
				.ToArray()
				.ShouldBe([1]);

			query(db, [null])
				.ToArray()
				.ShouldBeEmpty();

			query(db, [2])
				.ToArray()
				.ShouldBe([2]);

			var query4 = query(db, [1]);

			query4
				.ToArray()
				.ShouldBe([1]);

			query4
				.GetCompiledQueryInfo()
				.ShouldBeSameAs(query1.GetCompiledQueryInfo());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Compiled_CapturedArgumentArray_Null([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			var args = new object?[] { null };

			var query = CompiledQuery.Compile((IDataContext dc) =>
				dc.GetTable<Model.Person>()
					.Where(p => p.ID == Sql.Expr<int?>("{0}", args!))
					.Select(p => p.ID));

			// Nothing keys the build on a captured array, so a null element must not be baked into it.
			// Like master, the first call cannot translate it.
			var firstCallFailed = false;

			try
			{
				query(db).ToArray();
			}
			catch (LinqToDBException)
			{
				firstCallFailed = true;
			}

			args[0] = 2;

			query(db)
				.ToArray()
				.ShouldBe([2]);

			firstCallFailed.ShouldBeTrue();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Compiled_CapturedFormattable_Null([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);

			FormattableString fs = $"COALESCE({(int?)null}, {2})";

			var query = CompiledQuery.Compile((IDataContext dc) =>
				dc.GetTable<Model.Person>()
					.Where(p => p.ID == Sql.Expr<int>(fs))
					.Select(p => p.ID));

			query(db)
				.ToArray()
				.ShouldBe([2]);

			query(db)
				.ToArray()
				.ShouldBe([2]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void Expr_Captured_AfterRowReference([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			db.Person
				.Where(p => p.ID == Sql.Expr<int>($"{p.ID}"))
				.Select(p => p.ID)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2, 3, 4]);

			FormattableString sql = $"{2}";

			db.Person
				.Where(p => p.ID == Sql.Expr<int>(sql))
				.Select(p => p.ID)
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([2]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000"), QueryCacheTest]
		public void Expr_Captured_ArgumentChanges([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(FormattableString sql) =>
				from p in db.Person
				where p.ID == Sql.Expr<int>(sql)
				select p.ID;

			Query($"{1}")
				.ToArray()
				.ShouldBe([1]);

			var query  = Query($"{2}");
			var misses = query.GetCacheMissCount();

			query.ToArray().ShouldBe([2]);
			query.GetCacheMissCount().ShouldBe(misses);

			Query($"{1}")
				.ToArray()
				.ShouldBe([1]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5857"), QueryCacheTest]
		public void Expr_Interpolated_ArgumentChanges_Cached([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int value) =>
				db.Person
					.Where(p => p.ID == 1)
					.Select(_ => Sql.Expr<int>($"{value}"));

			Query(1)
				.ToArray()
				.ShouldBe([1]);

			var query  = Query(2);
			var misses = query.GetCacheMissCount();

			query.ToArray().ShouldBe([2]);
			query.GetCacheMissCount().ShouldBe(misses);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void SqlQueryDependent_ObjectsEqual_FormattableString()
		{
			var attribute = new SqlQueryDependentAttribute();

			FormattableString value1      = $"SELECT {1}";
			FormattableString sameValue1  = $"SELECT {1}";
			FormattableString value2      = $"SELECT {2}";
			FormattableString otherFormat = $"SELECT {1} + 0";

			attribute.ObjectsEqual(value1, sameValue1) .ShouldBeTrue();
			attribute.ObjectsEqual(value1, value2)     .ShouldBeFalse();
			attribute.ObjectsEqual(value1, otherFormat).ShouldBeFalse();
		}

		sealed class InlineFormatBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var sql = builder.GetValue<FormattableString>("sql");
				builder.ResultExpression = new SqlExpression(builder.Mapping.GetDbDataType(typeof(int)), sql.ToString(CultureInfo.InvariantCulture), Precedence.Primary);
			}
		}

		[Sql.Extension("", BuilderType = typeof(InlineFormatBuilder), ServerSideOnly = true)]
		static int InlineFormat([SqlQueryDependent] FormattableString sql) => throw new ServerSideOnlyException(nameof(InlineFormat));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void CustomExtension_DependentFormattableString_ArgumentChanges([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			int[] Query(int value) =>
				db.Person
					.Where(p => p.ID == 1)
					.Select(_ => InlineFormat($"{value}"))
					.ToArray();

			Query(1).ShouldBe([1]);
			Query(2).ShouldBe([2]);
			Query(1).ShouldBe([1]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6000")]
		public void CustomExtension_DependentFormattableString_Compiled([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query = CompiledQuery.Compile((IDataContext dc, int value) =>
				dc.GetTable<Person>()
					.Where(p => p.ID == 1)
					.Select(_ => InlineFormat($"{value}")));

			query(db, 1)
				.ToArray()
				.ShouldBe([1]);

			query(db, 2)
				.ToArray()
				.ShouldBe([2]);

			query(db, 1)
				.ToArray()
				.ShouldBe([1]);
		}

		public class FreeTextKey<T>
		{
			public T   Key = default!;
			public int Rank;
		}

		[Test]
		public void FreeTextTableTest([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void TestSqlCollate1(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllYdb,
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				TestProvName.AllSapHana,
				TestProvName.AllOracle11,
				TestProvName.AllInformix,
				TestProvName.AllSybase)]
			string context)
		{
			var collation = TestUtils.GetValidCollationName(GetProviderName(context, out _));

			using var db = GetDataContext(context);
			db.Person.OrderBy(_ => "1" + Sql.Collate(_.FirstName, collation) + "2").ToList();
		}

		[Test]
		public void TestSqlCollate2(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllYdb,
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				TestProvName.AllSapHana,
				TestProvName.AllOracle11,
				TestProvName.AllInformix,
				TestProvName.AllSybase)]
			string context)
		{
			var collation = TestUtils.GetValidCollationName(GetProviderName(context, out _));

			using var db = GetDataContext(context);
			db.Person.Select(_ => new { CollatedName = "1" + Sql.Collate(_.FirstName, collation) + "2" }).ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3340")]
		public void CustomExtensionBuilderEmitsParametersForConstants([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<JsonModifySampleTable>();

			tb.AsUpdatable()
				.Set(t => t.Object!, t => JsonModifySampleObjectExtensions.Set(t.Object!, o => o.Sample, "test"))
				.Update();

			Assert.That(db.LastQuery, Does.Contain("@"));
		}

		public class JsonModifyExtensionCallBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				new JsonModifySample<JsonModifySampleObject>().BuildSet(builder);
			}
		}

		public class JsonModifySampleTable
		{
			[Column(DataType = DataType.NVarChar)]
			public JsonModifySampleObject? Object { get; set; }
		}

		public class JsonModifySampleObject
		{
			public string? Sample { get; set; }
		}

		public static class JsonModifySampleObjectExtensions
		{
			[Sql.Extension("Set", ServerSideOnly = true, BuilderType = typeof(JsonModifyExtensionCallBuilder))]
			public static JsonModifySampleObject Set<TValue>(JsonModifySampleObject sampleObject, Expression<Func<JsonModifySampleObject, TValue>> propertyExpression, TValue propertyValue)
			{
				return sampleObject;
			}
		}

		internal static class JsonModifyExpressionUtils
		{
			public static bool TryExtractConstant(Expression expression, out object? value)
			{
				var memberStack = new Stack<MemberInfo>();
				Expression currentExpression = expression;

				while (currentExpression.NodeType == ExpressionType.MemberAccess ||
					   currentExpression.NodeType == ExpressionType.Convert ||
					   currentExpression.NodeType == ExpressionType.ConvertChecked)
				{
					if (currentExpression.NodeType == ExpressionType.Convert ||
						currentExpression.NodeType == ExpressionType.ConvertChecked)
					{
						currentExpression = ((UnaryExpression)currentExpression).Operand;
					}
					else
					{
						var memberExpression = (MemberExpression) currentExpression;
						if (!(memberExpression.Member is PropertyInfo || memberExpression.Member is FieldInfo))
						{
							break;
						}

						memberStack.Push(memberExpression.Member);

						currentExpression = memberExpression.Expression!;
					}
				}

				if (currentExpression is ConstantExpression constant)
				{
					var currentValue = constant.Value;
					while (memberStack.Count > 0)
					{
						var member = memberStack.Pop();

						currentValue = member is PropertyInfo property
							? property.GetValue(currentValue)
							: ((FieldInfo)member).GetValue(currentValue);
					}

					value = currentValue;
					return true;
				}

				value = null;
				return false;
			}

			public static bool TryEvaluateExpression(Expression expression, out object? value)
			{
				if (TryExtractConstant(expression, out value))
				{
					return true;
				}

				if (expression.NodeType == ExpressionType.Convert ||
					expression.NodeType == ExpressionType.ConvertChecked)
				{
					expression = ((UnaryExpression)expression).Operand;
				}

				if (expression.NodeType == ExpressionType.MemberInit ||
					expression.NodeType == ExpressionType.New ||
					expression.NodeType == ExpressionType.NewArrayInit)
				{
					value = Expression.Lambda(expression).Compile().DynamicInvoke();
					return true;
				}

				value = null;
				return false;
			}
		}

		public class JsonModifySample<T>
		{
			private static readonly JsonSerializerSettings _defaultSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore
			};

			private const string DbObjectExpression = "JSON_MODIFY({source}, {path}, {value})";

			public void BuildSet(Sql.ISqlExtensionBuilder builder)
			{
				builder.Expression = DbObjectExpression;

				builder.AddParameter("source", builder.GetExpression(0)!);

				var (lastMember, path) = GetPropertyAccessPath(builder);
				builder.AddParameter("path", path);

				builder.AddParameter("value", GetPropertyValueExpression(builder, lastMember));
			}

			private static (MemberInfo lastMember, string path) GetPropertyAccessPath(Sql.ISqlExtensionBuilder builder)
			{
				var currentExression = ((LambdaExpression) ((UnaryExpression) builder.Arguments[1]).Operand).Body;
				var memberStack = new Stack<MemberInfo>();

				while (currentExression is MemberExpression memberExpression)
				{
					memberStack.Push(memberExpression.Member);

					currentExression = memberExpression.Expression;
				}

				if (!(currentExression is ParameterExpression) || memberStack.Count == 0)
				{
					throw new InvalidOperationException("Invalid property expression.");
				}

				var pathBuilder = new StringBuilder();
				if (memberStack.Count > 1)
				{
					throw new NotSupportedException("Modifying of inner objects is not supported.");
				}

				pathBuilder.Append('$');

				MemberInfo? lastMember = null;
				foreach (var member in memberStack)
				{
					pathBuilder.Append('.');
					pathBuilder.Append(member.Name);

					lastMember = member;
				}

				return (lastMember: lastMember!, path: pathBuilder.ToString()!);
			}

			private static ISqlExpression GetPropertyValueExpression(Sql.ISqlExtensionBuilder builder, MemberInfo member)
			{
				ISqlExpression ConstantToSqlExpression(object? constantValue, Type valueType)
				{
					// force using of sql parameters
					var expression = Expression.Convert(
						Expression.Property(
							Expression.Constant(new ValueContainer(constantValue)),
							nameof(ValueContainer.Value)),
						valueType);

					return builder.ConvertExpressionToSql(expression)!;
				}

				ISqlExpression ToSqlExpression<TValue>(TValue constantValue)
				{
					return ConstantToSqlExpression(constantValue, typeof(TValue));
				}

				var valueExpression = builder.Arguments[2];

				if (!JsonModifyExpressionUtils.TryEvaluateExpression(valueExpression, out var value))
				{
					return builder.ConvertExpressionToSql(valueExpression)!;
				}

				var valueType = Nullable.GetUnderlyingType(valueExpression.Type) ?? valueExpression.Type;
				if (value == null)
				{
					return new SqlValue(valueType, null);
				}

				if (valueType.IsPrimitive ||
					valueType == typeof(string))
				{
					return new SqlParameter(new DbDataType(valueExpression.Type), null, value);
					//return ConstantToSqlExpression(value, valueExpression.Type);
				}

				var serializer = JsonSerializer.CreateDefault(_defaultSettings);
				var converterAttribute = member?.GetAttribute<JsonConverterAttribute>(true);
				if (converterAttribute != null)
				{
					serializer.Converters.Add((JsonConverter)Activator.CreateInstance(converterAttribute.ConverterType, converterAttribute.ConverterParameters)!);
				}

				string serializedValue;
				using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
				using (var jsonTextWriter = new JsonTextWriter(stringWriter))
				{
					serializer.Serialize(jsonTextWriter, value);

					serializedValue = stringWriter.ToString();
				}

				if (valueType.IsEnum)
				{
					if (serializedValue.StartsWith("\""))
					{
						return ToSqlExpression(serializedValue.Trim('"'));
					}

					return ToSqlExpression(Convert.ToInt32(value));
				}

				if (valueType == typeof(Guid) ||
					valueType == typeof(DateTime))
				{
					return ToSqlExpression(serializedValue.Trim('"'));
				}

				return new SqlFragment("JSON_QUERY({0})", ToSqlExpression(serializedValue));
			}

			#region CLASSES

			private class ValueContainer
			{
				public ValueContainer(object? value)
				{
					Value = value;
				}

				public object? Value { get; }
			}

			#endregion CLASSES
		}

		[Sql.Extension("", BuilderType = typeof(JsonPathBuilder), ServerSideOnly = true, CanBeNull = true)]
		static string? JsonValue(string? value, List<string> path)
		{
			if (value == null)
				return null;

			using var document = JsonDocument.Parse(value);

			var element = document.RootElement;

			foreach (var name in path)
			{
				if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var property))
					return null;

				element = property;
			}

			return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
		}

		[Sql.Extension("", BuilderType = typeof(PathLiteralBuilder), ServerSideOnly = true)]
		static string PathLiteral(string path) => path;

		[Sql.Extension("", BuilderType = typeof(PathListLiteralBuilder), ServerSideOnly = true)]
		static string PathListLiteral(List<string> path) => string.Join(".", path);

		[Sql.Extension("", BuilderType = typeof(UnwrapArgBuilder), ServerSideOnly = true)]
		static int UnwrapArg(object value) => (int)value;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void BuilderValueIsPartOfCacheKey([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable([new CacheKeyJsonRow { Id = 1 }]);

			// the builder bakes the path into generated SQL, so the query has to return the current path back
			var path = "sub.name";

			table.Select(_ => PathLiteral(path)).First().ShouldBe("sub.name");

			path = "sub.name2";

			table.Select(_ => PathLiteral(path)).First().ShouldBe("sub.name2");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void EqualBuilderValueIsStillCached([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable([new CacheKeyJsonRow { Id = 1 }]);

			// same content, but a new list instance on each iteration: the collection has to be compared by content,
			// otherwise the second iteration rebuilds the query
			var path = new List<string> { "sub", "name" };

			var query     = table.Select(_ => PathListLiteral(path));
			var cacheMiss = query.GetCacheMissCount();

			query.First().ShouldBe("sub.name");

			if (iteration == 2)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void TranslatedUnwrappedArgumentStaysOutOfCacheKey([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable([new CacheKeyJsonRow { Id = 1 }]);

			// the builder translates the argument, so its value travels as a parameter and must not be compared
			// by value: a different value has to reuse the query cached by the previous iteration
			var value = iteration;

			var query     = table.Select(_ => UnwrapArg(value));
			var cacheMiss = query.GetCacheMissCount();

			query.First().ShouldBe(iteration);

			if (iteration == 2)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void TranslatedParamsArgumentsStayOutOfCacheKey([IncludeDataSources(ProviderName.SQLiteClassic)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable([new CacheKeyJsonRow { Id = 1 }]);

			// params arguments are translated element-wise, while registration walks the whole array, so a
			// different element value has to reuse the query cached by the previous iteration
			var value = iteration;

			var query     = table.Select(_ => Sql.Expr<int>("{0}", value));
			var cacheMiss = query.GetCacheMissCount();

			query.First().ShouldBe(iteration);

			if (iteration == 2)
				query.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5769"), QueryCacheTest]
		public void JsonPathIsPartOfCacheKey([IncludeDataSources(TestProvName.PostgreSQL16)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(
			[
				new CacheKeyJsonRow { Id = 1, Value = /*lang=json,strict*/ """{"sub":{"name":"findme","name2":"dontfindme"}}""" }
			]);

			// Single PostgreSQL version is enough: json -> / ->> operators need 9.3+ and the defect is version-independent.
			// PostgreSQL registers List<string> as a scalar (array) type, so builder argument value could be
			// erroneously treated as a parameter and excluded from query cache key.
			var path = new List<string> { "sub", "name" };

			AssertQuery(table.Where(r => JsonValue(r.Value, path) == "findme")).ShouldHaveSingleItem();

			path = ["sub", "name2"];

			AssertQuery(table.Where(r => JsonValue(r.Value, path) == "findme")).ShouldBeEmpty();
		}

		[Table]
		sealed class CacheKeyJsonRow
		{
			[Column, PrimaryKey              ] public int     Id    { get; set; }
			[Column                          ] public string? Value { get; set; }
		}

		sealed class JsonPathBuilder : Sql.IExtensionCallBuilder
		{
			/// <summary>
			/// Resolves the path out of the argument expression the way the builder in the issue does: the path is never
			/// translated to SQL, it is read here and baked into the generated expression.
			/// </summary>
			static List<string> GetPath(Expression argument)
			{
				switch (argument)
				{
					case ConstantExpression { Value: List<string> constantPath }:
						return constantPath;

					// captured local: a field of the closure object
					case MemberExpression { Expression: ConstantExpression { Value: { } closure } } member:
					{
						var field = closure.GetType().GetFields().SingleOrDefault(f => f.Name == member.Member.Name)
							?? throw new InvalidOperationException($"'{member.Member.Name}' not found on '{closure.GetType()}'");

						return (List<string>)field.GetValue(closure)!;
					}

					// inline collection initializer
					case ListInitExpression listInit:
						return listInit.Initializers.Select(i => (string)((ConstantExpression)i.Arguments[0]).Value!).ToList();

					default:
						throw new InvalidOperationException($"Cannot resolve JSON path from '{argument}'");
				}
			}

			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				var value = builder.GetExpression(0)!;
				var path  = GetPath(builder.Arguments[1]);

				var sb         = new StringBuilder("({0}::json");
				var parameters = new List<ISqlExpression>(path.Count);

				for (var i = 0; i < path.Count; i++)
				{
					parameters.Add(new SqlValue(path[i]));
					sb
						.Append(i == path.Count - 1 ? "->>" : "->")
						.Append('{')
						.Append((i + 1).ToString(CultureInfo.InvariantCulture))
						.Append('}');
				}

				sb.Append(')');

				builder.ResultExpression = new SqlExpression(builder.Mapping.GetDbDataType(typeof(string)), sb.ToString(), Precedence.Primary, [value, .. parameters]);
			}
		}

		sealed class PathLiteralBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<string>(0)!);
			}
		}

		sealed class PathListLiteralBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(string.Join(".", builder.GetValue<List<string>>(0)!));
			}
		}

		sealed class UnwrapArgBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				// unwrap: true translates the argument with its conversion stripped, so the recorded expression
				// is not the one registration walks
				builder.ResultExpression = builder.GetExpression(0, unwrap: true)!;
			}
		}

		private IQueryable<Parent> GetParentsWithInExtension(IDataContext db, ICollection<int?> values)
		{
			var param = 4;
			var resultQuery =
					from o in db.GetTable<Parent>()
					where Sql.Ext.In(o.ParentID, values) || o.ParentID == param
					select o;
			return resultQuery;
		}

		private IQueryable<Parent> GetParentsWithContains(IDataContext db, ICollection<int?> values)
		{
			var param = 4;
			var resultQuery =
					from o in db.GetTable<Parent>()
					where values.Contains(o.ParentID) || o.ParentID == param
					select o;
			return resultQuery;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/973")]
		public void InExtensionWithParameterizedValues([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var values1 = new int?[] { 1, 2, 3, null };
			var values2 = new int?[] { 4, 5, 6, null };

			AreEqual(
				GetParentsWithContains(db, values1),
				GetParentsWithInExtension(db, values1));

			AreEqual(
				GetParentsWithContains(db, values2),
				GetParentsWithInExtension(db, values2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/973"), QueryCacheTest]
		public void InExtensionQueryCachedByValues([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var values1 = new int?[] { 1, 2, 3, null };
			var values2 = new int?[] { 4, 5, 6, null };

			var query11 = GetParentsWithInExtension(db, values1);
			var result11 = query11.ToArray();

			var cm1 = query11.GetCacheMissCount();

			var query12 = GetParentsWithInExtension(db, values1);
			var result12 = query12.ToArray();

			query12.GetCacheMissCount().ShouldBe(cm1);

			var query21  = GetParentsWithInExtension(db, values2);
			var result21 = query21.ToArray();

			var cm2 = query21.GetCacheMissCount();

			var query22  = GetParentsWithInExtension(db, values2);
			var result22 = query22.ToArray();

			query22.GetCacheMissCount().ShouldBe(cm2);
		}

		[Table("JsonPayloadTable")]
		public class JsonPayloadTable
		{
			[Column]
			public Guid Id { get; set; }

			[Column(DataType = DataType.NVarChar)]
			public JsonDbObject<JsonPayload>? Json { get; set; }
		}

		public class JsonPayload
		{
			public int Number { get; set; }

			public string? String { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2418")]
		public async Task UpdateJsonValueWithNestedSetExtension([IncludeDataSources(false, TestProvName.AllSqlServer2016Plus)] string context)
		{
			var schema = new MappingSchema();

			schema.SetConverter<JsonDbObject<JsonPayload>, string>(v => System.Text.Json.JsonSerializer.Serialize(v.Value));
			schema.SetConverter<JsonDbObject<JsonPayload>, DataParameter>(v => new DataParameter
			{
				DataType = DataType.NVarChar,
				Value    = System.Text.Json.JsonSerializer.Serialize(v.Value)
			});
			schema.SetConverter<string, JsonDbObject<JsonPayload>>(json => new JsonDbObject<JsonPayload>(System.Text.Json.JsonSerializer.Deserialize<JsonPayload>(json)!));

			using var db = (DataConnection)GetDataContext(context, schema);
			using var table = db.CreateLocalTable<JsonPayloadTable>();
			var newRecord = new JsonPayloadTable()
			{
				Id   = TestData.Guid1,
				Json = new JsonDbObject<JsonPayload>(new JsonPayload
				{
					String = "Test",
					Number = 1
				})
			};

			db.Insert(newRecord);

			var savedRecord = await table.FirstAsync(x => x.Id == newRecord.Id).ConfigureAwait(false);

			var newJson = new JsonPayload()
			{
				String = "Test1",
				Number = 10
			};

			await table
				.Where(o => o.Id == savedRecord.Id)
				.Set(o => o.Json, o => o.Json!
					.Set(j => j.Number, newJson.Number)
					.Set(j => j.String, newJson.String)
				).UpdateAsync()
				.ConfigureAwait(false);

			var lastQuery = db.LastQuery;

			savedRecord = await table.FirstAsync(x => x.Id == newRecord.Id).ConfigureAwait(false);

			//Assert.AreEqual(savedRecord.Json.Value, newJson);

			Assert.That(lastQuery, Contains.Substring("JSON_MODIFY(JSON_MODIFY"));
		}
	}
}
