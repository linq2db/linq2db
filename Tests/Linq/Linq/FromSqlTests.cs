using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Linq;
	using Model;

	[TestFixture, Parallelizable(ParallelScope.None)]
	public class FromSqlTests : TestBase
	{
		[Table(Name = "sample_class")]
		class SampleClass
		{
			[Column("id")]
			public int Id    { get; set; }

			[Column("value", Length = 50)]
			public string? Value { get; set; }


			public SomeOtherClass? AssociatedOne { get; set; }

		}

		[Table(Name = "sample_other_class")]
		class SomeOtherClass
		{
			[Column("id")]
			public int Id       { get; set; }

			[Column("parent_id")]
			public int ParentId { get; set; }

			[Column("value", Length = 50)]
			public string? Value { get; set; }
		}

		static SampleClass[] GenerateTestData()
		{
			return Enumerable.Range(1, 20).Select(i => new SampleClass {Id = i, Value = "Str_" + i}).ToArray();
		}

		class ToTableName<T> : IToSqlConverter
		{
			public ToTableName(ITable<T> table)
			{
				_table = table;
			}

			readonly ITable<T> _table;

			public ISqlExpression ToSql(Expression expression)
			{
				return new SqlExpression(null, _table.TableName, Precedence.Primary, false);
			}
		}

		ToTableName<T> GetName<T>(ITable<T> table)
		{
			return new ToTableName<T>(table);
		}

		[Test]
		public void TestFormattable([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query = db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where id >= {startId} and id < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattable2([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query = db.FromSql<SampleClass>(
					$"SELECT * FROM {GetName(table)} where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattableSameParam([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var startIdParam = new DataParameter("startId", startId, DataType.Int64);
				var query = db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where id >= {startIdParam} and id < {endId}");
				var queryWithProjection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id });

				var projection = queryWithProjection.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattableInExpr([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query =
					from t in table
					from s in db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where id >= {startId} and id < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattableInExpr2([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query =
					from t in table
					from s in db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestParameters([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query = db.FromSql<SampleClass>("SELECT * FROM\n{0}\nwhere id >= {1} and id < {2}",
					GetName(table), new DataParameter("startId", startId, DataType.Int64), endId);
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestParametersInExpr([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 1;

				var query =
					from t in table
					from s in db.FromSql<SampleClass>("SELECT * FROM {0} where id >= {1} and id < {2}",
						GetName(table), new DataParameter("startId", startId, DataType.Int64), endId).InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestParametersInExpr2([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var parameters = new object[] { GetName(table), new DataParameter("startId", startId, DataType.Int64), endId };

				var query =
					from t in table
					from s in db.FromSql<SampleClass>("SELECT * FROM {0} where id >= {1} and id < {2}", parameters).InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		private const string someGeneratedSqlString = "SELECT * FROM sample_other_class where parent_id = {0} and id >= {1}";

		[Test]
		public void TestAsosciation(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context, 
			[Values(14, 15)] int startId
		)
		{
			var ms = new MappingSchema();

			var idFilter = 1;

			ms.GetFluentMappingBuilder()
				.Entity<SampleClass>()
				.Association(x => x.AssociatedOne,
					(x, db) => db.FromSql<SomeOtherClass>(someGeneratedSqlString, x.Id, idFilter));

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			using (var other = db.CreateLocalTable<SomeOtherClass>())
			{

				var query = from t in table
					select new
					{
						t.Id,
						t.AssociatedOne
					};

				var result = query.ToArray();
			}
		}

		[Test]
		public void TestTableValueFunction(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context,
			[Values(0, 1)] int offset)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from t in db.Child
					from p in db.FromSql<Parent>($"GetParentByID({t.ParentID + offset})")
					select new
					{
						t,
						p
					};

				var expected =
					from t in db.Child
					from p in db.Parent.Where(p => p.ParentID == t.ParentID + offset)
					select new
					{
						t,
						p
					};

				AreEqual(expected, query);
			}
		}


		[Test]
		public void TestScalar(
			[IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllSqlServer)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.FromSql<int>($"select 1 as x").ToArray();
			}
		}

		[Test]
		public void TestScalarSubquery(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL93Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				// ::text hint needed for pgsql < 10
				var query =
					from c in db.SelectQuery(() => "hello world")
					from s in db.FromSql<string>($"regexp_split_to_table({c}::text, E'\\\\s+') {Sql.AliasExpr()}")
					select s;
				var result = query.ToArray();
				var expected = new[] { "hello", "world" };
				AreEqual(expected, result);
			}
		}

		public class UnnestEnvelope<T>
		{
			[Column("value")]
			public T Value = default!;
			[Column("index")]
			public int Index;
		}

		[ExpressionMethod(nameof(UnnestWithOrdinalityImpl))]
		static IQueryable<UnnestEnvelope<TValue>> UnnestWithOrdinality<TValue>(IDataContext db, TValue[] member)
			=> db.FromSql<UnnestEnvelope<TValue>>($"unnest({member}) with ordinality {Sql.AliasExpr()} (value, index)");

		static Expression<Func<IDataContext, TValue[], IQueryable<UnnestEnvelope<TValue>>>> UnnestWithOrdinalityImpl<TValue>()
			=> (db, member) => db.FromSql<UnnestEnvelope<TValue>>($"unnest({member}) with ordinality {Sql.AliasExpr()} (value, index)");


		[Sql.Expression("{0}", ServerSideOnly = true, Precedence = Precedence.Primary)]
		static T AsTyped<T>(string str)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void TestUnnest(
			// `with ordinality` added to pgsql 9.4
			[IncludeDataSources(TestProvName.AllPostgreSQL95Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from c in db.SelectQuery(() => Sql.Expr<int[]>("ARRAY[1,2]::int[]"))
					from s in db.FromSql<UnnestEnvelope<int>>($"unnest({c}) with ordinality {Sql.AliasExpr()} (value, index)")
					select s.Value;
				var result = query.ToArray();
				var expected = new []{1, 2};
				AreEqual(expected, result);
			}
		}

		[Test]
		public void TestUnnestFunction(
			// `with ordinality` added to pgsql 9.4
			[IncludeDataSources(TestProvName.AllPostgreSQL95Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from c in db.SelectQuery(() => Sql.Expr<int[]>("ARRAY[10,20]::int[]"))
					from s in UnnestWithOrdinality(db, c)
					select s;
				var result = query.ToArray();
				var expected = new[]
				{
					new UnnestEnvelope<int> { Value = 10, Index = 1 }, new UnnestEnvelope<int> { Value = 20, Index = 2 }
				};

				AreEqual(expected, result, ComparerBuilder.GetEqualityComparer<UnnestEnvelope<int>>());
			}
		}

		[Test]
		public void TestInvaildAliasExprUsage(
			[IncludeDataSources(TestProvName.AllPostgreSQL)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from c in db.FromSql<int>($"select {1} {Sql.AliasExpr()}")
					select c;
				Assert.Throws<Npgsql.PostgresException>(() => query.ToArray());
			}
		}

		[Test]
		public void TestQueryCaching_Interpolated_DataParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.True(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {DataParameter.Int32("endId", endId)}");
				}
			}
		}

		// TODO: right now we don't create parameter from endId, as expression compiler pass it by value
		[Test]
		public void TestQueryCaching_Interpolated_ValueParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.False(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {endId}");
				}
			}
		}

		[Test]
		public void TestQueryCaching_InterpolatedCache_BySqlExpressionParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable(GenerateTestData()))
			using (var table2 = db.CreateLocalTable<SomeOtherClass>())
			{
				var qry1 = GetQuery(table1, 1, 114);
				var qry2 = GetQuery(table2, 1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.False(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery<T>(ITable<T> table, int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {DataParameter.Int32("endId", endId)}");
				}
			}
		}

		[Test]
		public void TestQueryCaching_Format_DataParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.True(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						"SELECT * FROM {0} where id >= {1} and id < {2}",
						GetName(table),
						DataParameter.Int32("startId", startId),
						DataParameter.Int32("endId", endId));
				}
			}
		}

		[Test]
		public void TestQueryCaching_Format_ValueParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.False(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						"SELECT * FROM {0} where id >= {1} and id < {2}",
						GetName(table),
						DataParameter.Int32("startId", startId),
						endId);
				}
			}
		}

		[Test]
		public void TestQueryCaching_Format_BySqlExpressionParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable(GenerateTestData()))
			using (var table2 = db.CreateLocalTable<SomeOtherClass>())
			{
				var qry1 = GetQuery(table1, 1, 114);
				var qry2 = GetQuery(table2, 1, 115);

				var expr1 = qry1.Expression;
				var expr2 = qry2.Expression;

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2);

				Assert.False(ReferenceEquals(query1, query2));

				IQueryable<SampleClass> GetQuery<T>(ITable<T> table, int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						"SELECT * FROM {0} where id >= {1} and id < {2}",
						GetName(table),
						new DataParameter("startId", startId, DataType.Int32),
						new DataParameter("endId", endId, DataType.Int32));
				}
			}
		}
	}
}
