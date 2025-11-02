using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ExpressionTests : TestBase
	{
		public static class Functions
		{
			[Sql.Expression("DATE()", ServerSideOnly = true)]
			public static DateTime DateExpr(DataConnection db, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Expression("DATE({1})", ServerSideOnly = true)]
			public static DateTime DateExprKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Function("DATE", ArgIndices = new[] { 1 }, ServerSideOnly = true)]
			public static DateTime DateFuncKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Function("DATE", ServerSideOnly = true)]
			public static DateTime DateFuncFail(DataConnection db, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}

			[Sql.Expression("DATE({2})", ServerSideOnly = true)]
			public static DateTime DateExprKindFail(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new NotImplementedException();
			}
		}

		public class ExpressionTestsFakeType
		{

		}

		[Table]
		private sealed class ExpressionTestClass
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test]
		public void PostiveTest([IncludeDataSources(TestProvName.AllSQLite)]
			string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable<ExpressionTestClass>())
			{
				_ = db.Select(() => new
				{
					Date1 = Functions.DateExpr(db, new ExpressionTestsFakeType()),
					Date2 = Functions.DateExprKind(db, "now", new ExpressionTestsFakeType()),
					Date3 = Functions.DateFuncKind(db, "now", new ExpressionTestsFakeType()),
				});
			}
		}

		[Test]
		public void FailTest([IncludeDataSources(ProviderName.SQLiteMS)]
			string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable<ExpressionTestClass>())
			{
				Assert.Throws<LinqToDBException>(() => _ = db.Select(() => Functions.DateFuncFail(db, new ExpressionTestsFakeType())));
				Assert.Throws<LinqToDBException>(() => _ = db.Select(() => Functions.DateExprKindFail(db, "now", new ExpressionTestsFakeType())));
			}
		}

		sealed class MyContext : DataConnection
		{
			public MyContext(string configurationString) : base(configurationString)
			{
			}

			[Sql.Expression("10", ServerSideOnly = true)]
			public int SomeValue 
				=> this.SelectQuery(() => SomeValue).AsEnumerable().First();
		}

		[Test]
		public void TestAsProperty([DataSources(false)] string context)
		{
			using (var db = new MyContext(context))
			{
				db.SomeValue.ShouldBe(10);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4226")]
		public void Issue4226Test([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var fluentBuilder = new FluentMappingBuilder()
				.Entity<Issue4226Table>()
					.HasPrimaryKey(e => e.Id)
					.Property(e => e.Date)
						.HasDataType(DataType.NVarChar)
						.HasLength(11)
						.IsNullable(true)
						.HasConversionFunc(
							s => s!.Value.ToString("MM-dd-yyyy"),
							dt => DateTime.TryParseExact(dt, "MM-dd-yyyy", null, DateTimeStyles.None, out var result) ? result : null,
							false)
				.Build();

			using var db = GetDataContext(context, fluentBuilder.MappingSchema);
			using var tb = db.CreateLocalTable<Issue4226Table>();

			db.Insert(new Issue4226Table() { Id = 1, Date = TestData.Date });

			tb.Where(e => e.Date!.Value.Month == TestData.Date.Month).Single();
		}

		sealed class Issue4226Table
		{
			public int Id { get; set; }
			public DateTime? Date { get; set; }
		}

		#region Issue 3807
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Select(a => new
				{
					Id    = a.Id,
					Array = a.Array
				})
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(3));

			Assert.That(res[0].Array, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));

				Assert.That(res[1].Array, Is.Not.Null);
				Assert.That(res[1].Array.Count(), Is.EqualTo(2));

				Assert.That(res[2].Array, Is.Not.Null);
			}

			Assert.That(res[2].Array.Count(), Is.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Select(a => new Issue3807Table()
				{
					Id    = a.Id,
					Array = a.Array
				})
				.Where(r => r.Array.Contains("two"))
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test3([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Select(a => new Issue3807Table()
				{
					Id    = a.Id,
					Array = a.Array
				})
				.Where(r => "two".In(r.Array))
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test4([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Select(a => new Issue3807Table()
				{
					Id    = a.Id,
					Array = a.Array
				})
				.Where(r => r.Array.Any(i => i == "two"))
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test12([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Where(r => r.Array.Contains("two"))
				.Select(a => new
				{
					Id    = a.Id,
					Array = a.Array
				})
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test13([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Where(r => "two".In(r.Array))
				.Select(a => new
				{
					Id    = a.Id,
					Array = a.Array
				})
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3807")]
		public void Issue3807Test14([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue3807Table.Data);

			var res = tb
				.Where(r => r.Array.Any(i => i == "two"))
				.Select(a => new
				{
					Id    = a.Id,
					Array = a.Array
				})
				.OrderBy(e => e.Id)
				.ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res[0].Id, Is.EqualTo(1));
				Assert.That(res[0].Array.Count(), Is.EqualTo(3));
				Assert.That(res[0].Array, Is.EqualTo(new string[] { "one", "two", "three" }));
			}
		}

		static partial class SqlFnEx
		{
			[Sql.TableFunction("STRING_SPLIT")]
			public static IQueryable<StringValue> StringSplit(string? str, char delimiter)
			{
				return (str?.Split(delimiter
#if !NETFRAMEWORK
					, StringSplitOptions.None
#endif
					)
					.Select(e => new StringValue() { Value = e }) ?? Array.Empty<StringValue>()).AsQueryable();
			}
		}

		class StringValue
		{
			// function returns value column
			[Column("value")] public string Value { get; set; } = null!;
		}

		[Table]
		sealed class Issue3807Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? ArrayString { get; set; }

			[Association(QueryExpressionMethod = nameof(ArrayExpression))]
			public IEnumerable<string> Array { get; set; } = null!;

			private static Expression<Func<Issue3807Table, IDataContext, IQueryable<string>>> ArrayExpression()
			{
				return (e, dc) => SqlFnEx.StringSplit(e.ArrayString, ',').Select(r => r.Value);
			}

			public static readonly Issue3807Table[] Data =
			[
				new Issue3807Table() { Id = 1, ArrayString = "one,two,three" },
				new Issue3807Table() { Id = 2, ArrayString = "one,three" },
				new Issue3807Table() { Id = 3 },
			];
		}
		#endregion

		#region Issue 4622
		public record Issue4674StockItem(string TenantId, string Code, string Description);
		public record Issue4674StockRoomItem(string TenantId, string StockroomCode, string ItemCode, decimal Quantity);

		static IQueryable<T2> Issue4674JoinTable<T2>(IDataContext db, Expression<Func<T2, bool>> joinExpression)
		  where T2 : class
		{
			return db.GetTable<T2>().Where(joinExpression);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/discussions/4674")]
		public void Issue4674Test([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4674StockItem>();
			using var t2 = db.CreateLocalTable<Issue4674StockRoomItem>();

			var qry = from a in t1
					   from b in Issue4674JoinTable<Issue4674StockRoomItem>(db, b => b.TenantId == a.TenantId && b.StockroomCode == a.Code)
					   select new { a.TenantId, a.Code, a.Description, b.StockroomCode, b.Quantity };

			;
			Assert.That(() => qry.ToArray(), Throws.InstanceOf<LinqToDBException>()
				.With.Message.Contain("The LINQ expression could not be converted to SQL."));
		}
		#endregion

		[ExpressionMethod(nameof(GetValueNullableExpr))]
		static int? GetValue(int? value) => throw new InvalidOperationException();

		// this function should be a blackbox for linq2db
		[Sql.Expression("IIF({0} IS NULL, -1, {0} + 1)", ServerSideOnly = true)]
		static int GetValueFinal(int value) => throw new InvalidOperationException();

		static Expression<Func<int?, int?>> GetValueNullableExpr()
		{
			return value => value == null ? null : GetValueFinal(value.Value);
		}

		[Sql.Expression("{0}", ServerSideOnly = true, IgnoreGenericParameters = true)]
		static T Wrap<T>(T value) => throw new InvalidOperationException();

		[Test]
		//public void Test_ConditionalExpressionOptimization([IncludeDataSources(true, TestProvName.AllSqlServer)] string context, [Values] bool inline)
		public void Test_ConditionalExpressionOptimization(
			[DataSources(
			// no IIF or other syntax issues
			ProviderName.SqlCe,
			TestProvName.AllClickHouse,
			TestProvName.AllDB2,
			TestProvName.AllInformix,
			TestProvName.AllMySql,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllSapHana,
			TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(db.Person.Where(r => GetValue(Wrap<int?>(null)) == null).Count(), Is.EqualTo(4));
				Assert.That(db.Person.Where(r => GetValue(Wrap<int?>(null)) != null).Count(), Is.Zero);
				Assert.That(db.Person.Where(r => !(GetValue(Wrap<int?>(null)) != null)).Count(), Is.EqualTo(4));
			}
		}
	}
}
