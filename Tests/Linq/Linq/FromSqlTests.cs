using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;
using LinqToDB.Linq.Internal;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class FromSqlTests : TestBase
	{
		[Table(Name = "sample_class")]
		sealed class SampleClass
		{
			[Column("id")]
			public int Id    { get; set; }

			[Column("value", Length = 50)]
			public string? Value { get; set; }

			public SomeOtherClass? AssociatedOne { get; set; }

		}

		[Table(Name = "sample_other_class")]
		sealed class SomeOtherClass
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

		sealed class ToTableName<T> : IToSqlConverter
			where T : notnull
		{
			public ToTableName(ITable<T> table)
			{
				_table = table;
			}

			readonly ITable<T> _table;

			public ISqlExpression ToSql(object value)
			{
				return new SqlTable(MappingSchema.Default.GetEntityDescriptor(typeof(T)))
				{
					TableName = new (_table.TableName)
				};
			}
		}

		ToTableName<T> GetName<T>(ITable<T> table)
			where T : notnull
		{
			return new ToTableName<T>(table);
		}

		sealed class ToColumnName : IToSqlConverter
		{
			public ToColumnName(string columnName)
			{
				_columnName = columnName;
			}

			readonly string _columnName;

			public ISqlExpression ToSql(object value)
			{
				return new SqlField(_columnName, _columnName);
			}
		}

		IToSqlConverter GetColumn(string columnName)
		{
			return new ToColumnName(columnName);
		}

		[Test]
		public void TestFormattable([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query = db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where {GetColumn("id")} >= {startId} and {GetColumn("id")} < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
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
					$"SELECT * FROM {GetName(table)} where {GetColumn("id")} >= {new DataParameter("startId", startId, DataType.Int64)} and {GetColumn("id")} < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
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
				var query = db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where {GetColumn("id")} >= {startIdParam} and {GetColumn("id")} < {endId}");
				var queryWithProjection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id });

				var projection = queryWithProjection.OrderBy(_ => _.Id).ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
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
					from s in db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where {GetColumn("id")} >= {startId} and {GetColumn("id")} < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
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
					from s in db.FromSql<SampleClass>($"SELECT * FROM {GetName(table)} where {GetColumn("id")} >= {new DataParameter("startId", startId, DataType.Int64)} and {GetColumn("id")} < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
			}
		}

		[Test]
		public void TestParameters([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(1, 2)] int iteration, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var query = db.FromSql<SampleClass>("SELECT * FROM\n{0}\nwhere {3} >= {1} and {3} < {2}",
					GetName(table), new DataParameter("startId", startId, DataType.Int64), endId, GetColumn("id"));

				var save = query.GetCacheMissCount();

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				if (iteration > 1)
				{
					query.GetCacheMissCount().Should().Be(save);
				}

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
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
					from s in db.FromSql<SampleClass>("SELECT * FROM {0} where {3} >= {1} and {3} < {2}",
						GetName(table), new DataParameter("startId", startId, DataType.Int64), endId, GetColumn("id")).InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
			}
		}

		[Test]
		public void TestParametersInExpr2([DataSources(ProviderName.DB2, TestProvName.AllSapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

				var parameters = new object[] { GetName(table), new DataParameter("startId", startId, DataType.Int64), endId, GetColumn("id") };

				var query =
					from t in table
					from s in db.FromSql<SampleClass>("SELECT * FROM {0} where {3} >= {1} and {3} < {2}", parameters).InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.OrderBy(_ => _.Id)
					.ToArray();

				Assert.That(projection, Is.EqualTo(expected));
			}
		}

		private const string someGeneratedSqlString = "SELECT * FROM sample_other_class where parent_id = {0} and id >= {1}";

		[Test]
		public void TestAssociation(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context,
			[Values(14, 15)] int startId
		)
		{
			var ms = new MappingSchema();

			var idFilter = 1;

			new FluentMappingBuilder(ms)
				.Entity<SampleClass>()
				.Association(x => x.AssociatedOne,
					(x, db) => db.FromSql<SomeOtherClass>(someGeneratedSqlString, x.Id, idFilter))
				.Build();

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(GenerateTestData());
			using var __    = db.CreateLocalTable<SomeOtherClass>();

			var query =
				from t in table
				select new
				{
					t.Id,
					t.AssociatedOne
				};

			_ = query.ToArray();
		}

		[Test]
		public void FluentMappingTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			Run();
			Run();

			void Run()
			{
				var ms = new MappingSchema();

				var idFilter = 1;

				new FluentMappingBuilder(ms)
					.Entity<SampleClass>()
						.Association(x => x.AssociatedOne, (x, db) => db.FromSql<SomeOtherClass>(someGeneratedSqlString, x.Id, idFilter))
					.Build();

				using var db    = GetDataContext(context, ms);
				using var table = db.CreateLocalTable(GenerateTestData());
				using var __    = db.CreateLocalTable<SomeOtherClass>();

				var query =
					from t in table
					select new
					{
						t.Id,
						t.AssociatedOne
					};

				_ = query.ToArray();
			}
		}

		[Test]
		public void TestTableValueFunction(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context,
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
		public void TestScalarSubquery(
			[IncludeDataSources(true, TestProvName.AllPostgreSQL93Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				// ::text hint needed for pgsql < 10
				var query =
					from c in db.SelectQuery(() => "hello world")
					from s in db.FromSqlScalar<string>($"regexp_split_to_table({c}::text, E'\\\\s+') {Sql.AliasExpr()}")
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
			[IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)]
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
			[IncludeDataSources(true, TestProvName.AllPostgreSQL95Plus)]
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

		class StringSplitTable
		{
			public string Value { get; set; } = default!;
		}

		[Test]
		public void TestSplitStringParametrized(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)]
			string context, [Values(1, 2)] int iteration)
		{
			using (var db = GetDataContext(context))
			{
				var values = iteration == 1
					? "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z"
					: "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20";

				var query  = db.FromSql<StringSplitTable>($"STRING_SPLIT({values},',')").Select(x => x.Value);

				var cacheMissCount = query.GetCacheMissCount();

				var result = query.ToArray();

				var expectedValues = values.Split(',').Select(x => x.Trim()).ToArray();

				AreEqual(expectedValues, result);

				if (iteration > 1)
				{
					query.GetCacheMissCount().Should().Be(cacheMissCount);
				}

				query.GetSelectQuery().HasQueryParameter().Should().BeTrue();
			}
		}

		[Test]
		public void TestSplitStringParametrizedExplicitParameter(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)]
			string context, [Values(1, 2)] int iteration)
		{
			using (var db = GetDataContext(context))
			{
				var values = iteration == 1
					? "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z"
					: "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20";

				var query = db.FromSql<StringSplitTable>($"STRING_SPLIT({new DataParameter("p", values, DataType.VarChar)},',')").Select(x => x.Value);

				var cacheMissCount = query.GetCacheMissCount();

				var result = query.ToArray();

				var expectedValues = values.Split(',').Select(x => x.Trim()).ToArray();

				AreEqual(expectedValues, result);

				if (iteration > 1)
				{
					query.GetCacheMissCount().Should().Be(cacheMissCount);
				}

				query.GetSelectQuery().HasQueryParameter().Should().BeTrue();
			}
		}

		[Test]
		public void TestInvaildAliasExprUsage(
			[IncludeDataSources(TestProvName.AllPostgreSQL15Minus)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from c in db.FromSql<int>($"select {1} {Sql.AliasExpr()}")
					select c;

				Assert.Throws<InvalidOperationException>(() => query.ToArray());
			}
		}

		[Test]
		public void TestQueryCaching_Interpolated_DataParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.True);

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {DataParameter.Int32("endId", endId)}");
				}
			}
		}

		[Test]
		public void TestQueryCaching_Interpolated_ValueParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.True);

				IQueryable<SampleClass> GetQuery(int startId, int endId)
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {endId}");
				}
			}
		}

		[Test]
		public void TestQueryCaching_InterpolatedCache_BySqlExpressionParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable(GenerateTestData()))
			using (var table2 = db.CreateLocalTable<SomeOtherClass>())
			{
				var qry1 = GetQuery(table1, 1, 114);
				var qry2 = GetQuery(table2, 1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.False);

				IQueryable<SampleClass> GetQuery<T>(ITable<T> table, int startId, int endId)
					where T : notnull
				{
					return db.FromSql<SampleClass>(
						$"SELECT * FROM {GetName(table)} where id >= {DataParameter.Int32("startId", startId)} and id < {DataParameter.Int32("endId", endId)}");
				}
			}
		}

		[Test]
		public void TestQueryCaching_Format_DataParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.True);

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
		public void TestQueryCaching_Format_ValueParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				var qry1 = GetQuery(1, 114);
				var qry2 = GetQuery(1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.True);

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
		public void TestQueryCaching_Format_BySqlExpressionParameter([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable(GenerateTestData()))
			using (var table2 = db.CreateLocalTable<SomeOtherClass>())
			{
				var qry1 = GetQuery(table1, 1, 114);
				var qry2 = GetQuery(table2, 1, 115);

				var expr1 = (IQueryExpressions)new RuntimeExpressionsContainer(qry1.Expression);
				var expr2 = (IQueryExpressions)new RuntimeExpressionsContainer(qry2.Expression);

				var query1 = Query<SampleClass>.GetQuery(db, ref expr1, out _);
				var query2 = Query<SampleClass>.GetQuery(db, ref expr2, out _);

				Assert.That(ReferenceEquals(query1, query2), Is.False);

				IQueryable<SampleClass> GetQuery<T>(ITable<T> table, int startId, int endId)
					where T : notnull
				{
					return db.FromSql<SampleClass>(
						"SELECT * FROM {0} where id >= {1} and id < {2}",
						GetName(table),
						new DataParameter("startId", startId, DataType.Int32),
						new DataParameter("endId", endId, DataType.Int32));
				}
			}
		}

		sealed class Scalar<T>
		{
			public T Value1 = default!;
		}

		sealed class Values<T>
		{
			public T Value1 = default!;
			public T Value2 = default!;
		}

		[Test]
		public void TestQueryCaching_ByParameter_Interpolation1([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var res = db.FromSql<Scalar<int?>>($"SELECT {new DataParameter("value", value, DataType.Int32)} as Value1 /*TestQueryCaching_ByParameter_Interpolation1*/").ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.EqualTo(value));
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted1([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var res = db.FromSql<Scalar<int?>>("SELECT {0} as Value1 /*TestQueryCaching_ByParameter_Formatted1*/", new DataParameter("value", value, DataType.Int32)).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.EqualTo(value));
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Interpolation2([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>($"SELECT {new DataParameter("value1", value1, DataType.Int32)} as Value1, {new DataParameter("value2", value2, DataType.Int32)} as Value2 /*TestQueryCaching_ByParameter_Interpolation2*/").ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted2([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);

				var saveCount = Query<Values<int?>>.CacheMissCount;

				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				Query<Values<int?>>.CacheMissCount.Should().Be(saveCount);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>("SELECT {0} as Value1, {1} as Value2 /*TestQueryCaching_ByParameter_Formatted2*/", new DataParameter("value1", value1, DataType.Int32), new DataParameter("value2", value2, DataType.Int32)).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted21([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>("SELECT {0} as Value1, {1} as Value2 /*TestQueryCaching_ByParameter_Formatted21*/", new object?[] { new DataParameter("value1", value1, DataType.Int32), new DataParameter("value2", value2, DataType.Int32) }).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Interpolation3([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var p = new DataParameter("value", value, DataType.Int32);
					var res = db.FromSql<Scalar<int?>>($"SELECT {p} as Value1 /*TestQueryCaching_ByParameter_Interpolation3*/").ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.EqualTo(value));
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted3([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var p = new DataParameter("value", value, DataType.Int32);
					var res = db.FromSql<Scalar<int?>>("SELECT {0} as Value1 /*TestQueryCaching_ByParameter_Formatted3*/", p).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.EqualTo(value));
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Interpolation4([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var p = new DataParameter("value", value, DataType.Int32);
					var res = db.FromSql<Scalar<int?>>($"SELECT {null} as Value1 /*TestQueryCaching_ByParameter_Interpolation4*/").ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.Null);
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted4([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null);
				Test(1);
				Test(null);
				Test(2);
				Test(3);

				void Test(int? value)
				{
					var p = new DataParameter("value", value, DataType.Int32);
					var res = db.FromSql<Scalar<int?>>("SELECT {0} as Value1 /*TestQueryCaching_ByParameter_Formatted4*/", new object?[] { null }).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.That(res[0].Value1, Is.Null);
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Interpolation5([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>($"SELECT {value1} as Value1, {value2} as Value2 /*TestQueryCaching_ByParameter_Interpolation5*/").ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted5([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>("SELECT {0} as Value1, {1} as Value2 /*TestQueryCaching_ByParameter_Formatted5*/", value1, value2).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted51([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>("SELECT {0} as Value1, {1} as Value2 /*TestQueryCaching_ByParameter_Formatted51*/", new object?[] { value1, value2 }).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		[Test]
		public void TestQueryCaching_ByParameter_Formatted52([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			// important: comment added to avoid use of cached query from other test
			var sql = "SELECT {0} as Value1, {1} as Value2 /*TestQueryCaching_ByParameter_Formatted52*/";

			using (var db = GetDataContext(context))
			{
				Test(null, null);
				Test(1, 2);
				Test(null, 2);
				Test(2, null);
				Test(3, 3);

				void Test(int? value1, int? value2)
				{
					var res = db.FromSql<Values<int?>>(sql, new object?[] { value1, value2 }).ToArray();
					Assert.That(res, Has.Length.EqualTo(1));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].Value1, Is.EqualTo(value1));
						Assert.That(res[0].Value2, Is.EqualTo(value2));
					});
				}
			}
		}

		const string MyTableNameStringConstant = "Person";

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3782 / https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue3782Test1([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			FormattableString statement = $@"
	SELECT CASE
		WHEN EXISTS (
			SELECT 1
			FROM information_schema.tables
			WHERE table_name = {MyTableNameStringConstant}
		)
		THEN true
		ELSE false
	END AS result";

			var exists = db.FromSqlScalar<bool>(statement).First();

			Assert.That(exists, Is.True);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3782 / https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue3782Test2([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			FormattableString statement = $@"
	SELECT CASE
		WHEN EXISTS (
			SELECT 1
			FROM information_schema.tables
			WHERE table_name = {MyTableNameStringConstant}
		)
		THEN true
		ELSE false
	END AS result";

			var query = from p in db.Person
						where db.FromSqlScalar<bool>(statement).Any()
						select p;

			query.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3782 / https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue3782Test3([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			FormattableString statement = $"SELECT IIF(EXISTS(SELECT * FROM [INFORMATION_SCHEMA].[TABLES] [x] WHERE [x].[TABLE_NAME] = {MyTableNameStringConstant}),1,0) ttt";

			var tableExists = db.FromSqlScalar<bool>(statement).Any();

			Assert.That(tableExists, Is.True);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3782 / https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue3782Test4([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus)] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			db.InlineParameters = inline;

			FormattableString statement = $"SELECT IIF(EXISTS(SELECT * FROM [INFORMATION_SCHEMA].[TABLES] [x] WHERE [x].[TABLE_NAME] = {MyTableNameStringConstant}),1,0) ttt";

			var query = from p in db.Person
						where db.FromSqlScalar<bool>(statement).Any()
						select p;

			query.ToArray();
		}
	}
}
