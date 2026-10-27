using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
				throw new ServerSideOnlyException(nameof(DateExpr));
			}

			[Sql.Expression("DATE({1})", ServerSideOnly = true)]
			public static DateTime DateExprKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new ServerSideOnlyException(nameof(DateExprKind));
			}

			[Sql.Function("DATE", ArgIndices = new[] { 1 }, ServerSideOnly = true)]
			public static DateTime DateFuncKind(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new ServerSideOnlyException(nameof(DateFuncKind));
			}

			[Sql.Function("DATE", ServerSideOnly = true)]
			public static DateTime DateFuncFail(DataConnection db, ExpressionTestsFakeType fake)
			{
				throw new ServerSideOnlyException(nameof(DateFuncFail));
			}

			[Sql.Expression("DATE({2})", ServerSideOnly = true)]
			public static DateTime DateExprKindFail(DataConnection db, string kind, ExpressionTestsFakeType fake)
			{
				throw new ServerSideOnlyException(nameof(DateExprKindFail));
			}
		}

		public class ExpressionTestsFakeType;

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
			using var db = new MyContext(context);
			db.SomeValue.ShouldBe(10);
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
		public record Issue4674StockItem([property: PrimaryKey, Column(CanBeNull = false, Length = 50)] string TenantId, string Code, string Description);
		public record Issue4674StockRoomItem(string TenantId, string StockroomCode, [property:PrimaryKey, Column(CanBeNull = false, Length = 50)] string ItemCode, decimal Quantity);

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
			Assert.That(() => qry.ToArray(), Throws.InstanceOf<LinqToDBException>()
				.With.Message.Contain("The LINQ expression could not be converted to SQL."));
		}
		#endregion

		[ExpressionMethod(nameof(GetValueNullableExpr))]
		static int? GetValue(int? value) => throw new ServerSideOnlyException(nameof(GetValue));

		// this function should be a blackbox for linq2db
		[Sql.Expression("IIF({0} IS NULL, -1, {0} + 1)", ServerSideOnly = true)]
		static int GetValueFinal(int value) => throw new ServerSideOnlyException(nameof(GetValueFinal));

		static Expression<Func<int?, int?>> GetValueNullableExpr()
		{
			return value => value == null ? null : GetValueFinal(value.Value);
		}

		[Sql.Expression("{0}", ServerSideOnly = true, IgnoreGenericParameters = true)]
		static T Wrap<T>(T value) => throw new ServerSideOnlyException(nameof(Wrap));

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

		[Sql.Expression("PersonID", IsPure = true, ServerSideOnly = true)]
		static int PureRandom() => throw new ServerSideOnlyException(nameof(PureRandom));

		[Sql.Expression("PersonID", IsPure = false, ServerSideOnly = true)]
		static int ImpureRandom() => throw new ServerSideOnlyException(nameof(ImpureRandom));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5166")]
		public void PureExpressionDetection([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			_ = db.Person
				.Select(s => new { Entity = s, Random = (double)PureRandom() })
				.OrderByDescending(s => s.Random)
				.Take(10)
				.ToList();

			Assert.That(db.LastQuery, Does.Not.Contain("ORDER"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5166")]
		public void ImpureExpressionDetection([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			_ = db.Person
				.Select(s => new { Entity = s, Random = (double)ImpureRandom() })
				.OrderByDescending(s => s.Random)
				.Take(10)
				.ToList();

			Assert.That(db.LastQuery, Does.Contain("ORDER"));
		}

		[LinqToDB.Sql.ExpressionAttribute("{0} = ANY({1})", ServerSideOnly = true, IsPredicate = true)]
		static bool IsAnyOf1<T>(T left, IReadOnlyList<T> right) => throw new ServerSideOnlyException(nameof(IsAnyOf1));

		[LinqToDB.Sql.ExpressionAttribute("{0} = ANY({1})", ServerSideOnly = true, IsPredicate = true)]
		static bool IsAnyOf2<T>(T left, T[] right) => throw new ServerSideOnlyException(nameof(IsAnyOf2));

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5244")]
		public void CollectionParameterTranslation_IReadOnlyList([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			IReadOnlyList<int> ids = [10, 20];

			db.Person.Where(p => IsAnyOf1(p.ID, ids)).Delete();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5244")]
		public void CollectionParameterTranslation_Array([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			int[] ids = [10, 20];

			db.Person.Where(p => IsAnyOf2(p.ID, ids)).Delete();
		}

		sealed class WarehouseTableDto
		{
			[PrimaryKey]
			public int Id { get; set; }
			public SomeEnum Value { get; set; }

			public static readonly WarehouseTableDto[] Data =
			[
				new WarehouseTableDto() { Id = 1, Value = SomeEnum.SomeValue | SomeEnum.SomeOtherValue | SomeEnum.FifthValue }
			];

			[Flags]
			public enum SomeEnum
			{
				SomeValue      = 1,
				SecondValue    = 2,
				SomeOtherValue = 4,
				FourthValue    = 8,
				FifthValue     = 16,
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5286")]
		public void MapInvertOperator([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tb  = db.CreateLocalTable(WarehouseTableDto.Data);

			var addMask = WarehouseTableDto.SomeEnum.SomeValue | WarehouseTableDto.SomeEnum.SecondValue;
			var removeMask = WarehouseTableDto.SomeEnum.SomeOtherValue | WarehouseTableDto.SomeEnum.FourthValue;

			tb
				.Set(w => w.Value, w => (w.Value | addMask) & ~removeMask)
				.Update();

			var record = tb.Single();

			Assert.That(record.Value, Is.EqualTo(addMask | WarehouseTableDto.SomeEnum.FifthValue));
		}

		#region Hand-built IndexExpression

		// The C# compiler never emits IndexExpression: values[0] lowers to BinaryExpression{ArrayIndex}, list[0] to
		// a get_Item call and grid[i, j] to an Array.Get call. Each test builds the IndexExpression by hand and runs
		// the compiler's shape for the same source beside it, so a difference is attributable to the node itself.

		static readonly PropertyInfo _listItem = typeof(List<int>).GetProperty("Item")!;

		static Expression<Func<Model.Person, bool>> IdEquals(Expression value)
		{
			var p = Expression.Parameter(typeof(Model.Person), "p");

			return Expression.Lambda<Func<Model.Person, bool>>(
				Expression.Equal(Expression.Field(p, nameof(Model.Person.ID)), value),
				p);
		}

		static void ShouldReadEachValue<TArg, TResult>(Func<TArg, IQueryable<TResult>> build, TArg first, TResult expectedFirst, TArg second, TResult expectedSecond)
		{
			build(first)
				.ToArray()
				.ShouldBe([expectedFirst]);

			build(second)
				.ToArray()
				.ShouldBe([expectedSecond]);
		}

		static void ShouldHitCacheOnRebuild<TArg>(Func<TArg, IQueryable<int>> build, TArg first, int expectedFirst, TArg second, int expectedSecond)
		{
			var query = build(first);

			query
				.ToArray()
				.ShouldBe([expectedFirst]);

			var missCount = query.GetCacheMissCount();

			build(second)
				.ToArray()
				.ShouldBe([expectedSecond]);

			query
				.GetCacheMissCount()
				.ShouldBe(missCount);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		public void HandBuiltArrayAccess([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var ids = new[] { 2, 3 };

			Expression<Func<int[]>> captured = () => ids;

			db.Person
				.Where(IdEquals(Expression.ArrayAccess(captured.Body, Expression.Constant(0))))
				.Select(p => p.ID)
				.ToArray()
				.ShouldBe([2]);

			db.Person
				.Where(p => p.ID == ids[0])
				.Select(p => p.ID)
				.ToArray()
				.ShouldBe([2]);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		[QueryCacheTest]
		public void HandBuiltArrayAccess_RebuiltTreeHitsCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> HandBuilt(int[] ids)
			{
				Expression<Func<int[]>> captured = () => ids;

				return db.Person
					.Where(IdEquals(Expression.ArrayAccess(captured.Body, Expression.Constant(0))))
					.Select(p => p.ID);
			}

			IQueryable<int> CompilerBuilt(int[] ids)
			{
				return db.Person
					.Where(p => p.ID == ids[0])
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(CompilerBuilt, new[] { 2, 3 }, 2, new[] { 3, 4 }, 3);
			ShouldHitCacheOnRebuild(HandBuilt,     new[] { 2, 3 }, 2, new[] { 3, 4 }, 3);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		[QueryCacheTest]
		public void HandBuiltIndexerProperty([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> HandBuilt(List<int> ids)
			{
				Expression<Func<List<int>>> captured = () => ids;

				return db.Person
					.Where(IdEquals(Expression.MakeIndex(captured.Body, _listItem, [Expression.Constant(0)])))
					.Select(p => p.ID);
			}

			IQueryable<int> CompilerBuilt(List<int> ids)
			{
				return db.Person
					.Where(p => p.ID == ids[0])
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(CompilerBuilt, new List<int> { 2 }, 2, new List<int> { 3 }, 3);
			ShouldHitCacheOnRebuild(HandBuilt,     new List<int> { 2 }, 2, new List<int> { 3 }, 3);
		}

		// A collection held directly in a ConstantExpression has no compiler shape either, so the controls are the
		// hand-built get_Item and ArrayIndex forms. A list leaves the cache key and is read on reuse; an array stays in
		// the key and its element is inlined, so mutating it must invalidate the cached query.
		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		[QueryCacheTest]
		public void HandBuiltIndexOverConstantCollection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> IndexOverList(List<int> ids)
			{
				return db.Person
					.Where(IdEquals(Expression.MakeIndex(Expression.Constant(ids), _listItem, [Expression.Constant(0)])))
					.Select(p => p.ID);
			}

			IQueryable<int> GetItemOverList(List<int> ids)
			{
				return db.Person
					.Where(IdEquals(Expression.Call(Expression.Constant(ids), _listItem.GetGetMethod()!, Expression.Constant(0))))
					.Select(p => p.ID);
			}

			IQueryable<int> IndexOverArray(int[] ids)
			{
				return db.Person
					.Where(IdEquals(Expression.ArrayAccess(Expression.Constant(ids), Expression.Constant(0))))
					.Select(p => p.ID);
			}

			IQueryable<int> ArrayIndexOverArray(int[] ids)
			{
				return db.Person
					.Where(IdEquals(Expression.ArrayIndex(Expression.Constant(ids), Expression.Constant(0))))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(GetItemOverList, new List<int> { 2 }, 2, new List<int> { 3 }, 3);
			ShouldHitCacheOnRebuild(IndexOverList,   new List<int> { 2 }, 2, new List<int> { 3 }, 3);

			ShouldRebuildOnMutation(ArrayIndexOverArray);
			ShouldRebuildOnMutation(IndexOverArray);
		}

		static void ShouldRebuildOnMutation(Func<int[], IQueryable<int>> build)
		{
			var ids   = new[] { 2 };
			var query = build(ids);

			query
				.ToArray()
				.ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			ids[0] = 3;

			build(ids)
				.ToArray()
				.ShouldBe([3]);

			query
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);
		}

		// A client-side condition evaluated while the query is built and then folded out of the SQL
		// (`true AND p.ID = 2` -> `p.ID = 2`) still decides the SQL, so the cache must compare its value.
		static readonly int[]          _foldedElements = [2];
		static readonly List<int>      _foldedList     = [1];
		static readonly FoldedSettings _foldedSettings = new() { Enabled = true };

		sealed class FoldedSettings
		{
			public bool Enabled;
		}

		sealed class FoldedHolder
		{
			public int Value { get; init; }
		}

		static Expression<Func<Model.Person, bool>> IdIsTwoWhen(Expression condition)
		{
			var p = Expression.Parameter(typeof(Model.Person), "p");

			return Expression.Lambda<Func<Model.Person, bool>>(
				Expression.AndAlso(
					condition,
					Expression.Equal(Expression.Field(p, nameof(Model.Person.ID)), Expression.Constant(2))),
				p);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedStaticArrayElementSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedElements[0] > 0 && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query()
					.ToArray()
					.ShouldBe([2]);

				_foldedElements[0] = 0;

				Query()
					.ToArray()
					.ShouldBeEmpty();
			}
			finally
			{
				_foldedElements[0] = 2;
			}
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedStaticFieldSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedSettings.Enabled && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query()
					.ToArray()
					.ShouldBe([2]);

				_foldedSettings.Enabled = false;

				Query()
					.ToArray()
					.ShouldBeEmpty();
			}
			finally
			{
				_foldedSettings.Enabled = true;
			}
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedStaticListCountSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => _foldedList.Count > 0 && p.ID == 2)
					.Select(p => p.ID);
			}

			try
			{
				Query()
					.ToArray()
					.ShouldBe([2]);

				_foldedList.Clear();

				Query()
					.ToArray()
					.ShouldBeEmpty();
			}
			finally
			{
				_foldedList.Clear();
				_foldedList.Add(1);
			}
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantMemberSeesNewInstance([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(FoldedHolder holder)
			{
				var condition = Expression.GreaterThan(
					Expression.Property(Expression.Constant(holder), nameof(FoldedHolder.Value)),
					Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			Query(new FoldedHolder { Value = 1 })
				.ToArray()
				.ShouldBe([2]);

			Query(new FoldedHolder { Value = 0 })
				.ToArray()
				.ShouldBeEmpty();
		}

		static IQueryable<int> FoldedElementQuery(Model.ITestDataContext db, int[] ids)
		{
			var condition = Expression.GreaterThan(
				Expression.ArrayIndex(Expression.Constant(ids), Expression.Constant(0)),
				Expression.Constant(0));

			return db.Person
				.Where(IdIsTwoWhen(condition))
				.Select(p => p.ID);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantArrayElementSeesMutation([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var ids   = new[] { 2 };
			var query = FoldedElementQuery(db, ids);

			query
				.ToArray()
				.ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			ids[0] = 0;

			FoldedElementQuery(db, ids)
				.ToArray()
				.ShouldBeEmpty();

			query
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantArrayElementHitsOnEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			ShouldHitCacheOnRebuild(ids => FoldedElementQuery(db, ids), new[] { 2 }, 2, new[] { 2 }, 2);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantArrayElementBesideEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// Sql.AsSql keeps `folded[0] > 0` from being evaluated whole, so only the element reaches the SQL and is folded there.
			IQueryable<int> Query(int[] folded, int[] used)
			{
				var p = Expression.Parameter(typeof(Model.Person), "p");

				var predicate = Expression.Lambda<Func<Model.Person, bool>>(
					Expression.AndAlso(
						Expression.GreaterThan(
							Expression.Call(
								typeof(Sql).GetMethod(nameof(Sql.AsSql))!.MakeGenericMethod(typeof(int)),
								Expression.ArrayIndex(Expression.Constant(folded), Expression.Constant(0))),
							Expression.Constant(0)),
						Expression.Equal(
							Expression.Field(p, nameof(Model.Person.ID)),
							Expression.ArrayIndex(Expression.Constant(used), Expression.Constant(0)))),
					p);

				return db.Person
					.Where(predicate)
					.Select(p => p.ID);
			}

			var folded = new[] { 2 };
			var used   = new[] { 2 };

			Query(folded, used)
				.ToArray()
				.ShouldBe([2]);

			folded[0] = 0;

			Query(folded, used)
				.ToArray()
				.ShouldBeEmpty();
		}

		// A value pruned with its column did not decide the SQL, so changing it keeps the cached query.
		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void PrunedStaticFieldKeepsCachedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Select(p => new { p.ID, Flag = _foldedSettings.Enabled })
					.Select(x => x.ID)
					.Where(id => id == 2);
			}

			try
			{
				var query = Query();

				query
					.ToArray()
					.ShouldBe([2]);

				var missCount = query.GetCacheMissCount();

				_foldedSettings.Enabled = false;

				Query()
					.ToArray()
					.ShouldBe([2]);

				query
					.GetCacheMissCount()
					.ShouldBe(missCount);
			}
			finally
			{
				_foldedSettings.Enabled = true;
			}
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantArrayNullCheckHitsOnEqualArray([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[]? ids)
			{
				var condition = Expression.NotEqual(
					Expression.Constant(ids, typeof(int[])),
					Expression.Constant(null, typeof(int[])));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, new[] { 1, 2 }, 2, new[] { 1, 2 }, 2);

			Query(null)
				.ToArray()
				.ShouldBeEmpty();
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedConstantArrayLength([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[] ids)
			{
				var condition = Expression.GreaterThan(
					Expression.ArrayLength(Expression.Constant(ids)),
					Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, new[] { 1, 2 }, 2, new[] { 1, 2 }, 2);

			Query([])
				.ToArray()
				.ShouldBeEmpty();
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedScalarConstant([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int value)
			{
				var condition = Expression.GreaterThan(Expression.Constant(value), Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			var query = Query(5);

			query
				.ToArray()
				.ShouldBe([2]);

			var missCount = query.GetCacheMissCount();

			Query(-1)
				.ToArray()
				.ShouldBeEmpty();

			query
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);
		}

		// Query-cache test: provider-independent.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5843")]
		[QueryCacheTest]
		public void FoldedScalarListElement([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(List<int>));

			using var db = GetDataContext(context, ms);

			db.MappingSchema
				.IsScalarType(typeof(List<int>))
				.ShouldBeTrue();

			IQueryable<int> Query(List<int> ids)
			{
				var condition = Expression.GreaterThan(
					Expression.MakeIndex(Expression.Constant(ids), _listItem, [Expression.Constant(0)]),
					Expression.Constant(0));

				return db.Person
					.Where(IdIsTwoWhen(condition))
					.Select(p => p.ID);
			}

			ShouldHitCacheOnRebuild(Query, new List<int> { 2 }, 2, new List<int> { 2 }, 2);

			var ids = new List<int> { 2 };

			Query(ids)
				.ToArray()
				.ShouldBe([2]);

			ids[0] = 0;

			Query(ids)
				.ToArray()
				.ShouldBeEmpty();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		public void HandBuiltArrayAccessInProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<string> HandBuilt(string[] values)
			{
				Expression<Func<string[]>> captured = () => values;

				var person = Expression.Parameter(typeof(Model.Person), "p");

				var selector = Expression.Lambda<Func<Model.Person, string>>(
					Expression.ArrayAccess(captured.Body, Expression.Constant(0)),
					person);

				return db.Person
					.Where(p => p.ID == 1)
					.Select(selector);
			}

			IQueryable<string> CompilerBuilt(string[] values)
			{
				return db.Person
					.Where(p => p.ID == 1)
					.Select(p => values[0]);
			}

			ShouldReadEachValue(CompilerBuilt, new[] { "a" }, "a", new[] { "b" }, "b");
			ShouldReadEachValue(HandBuilt,     new[] { "a" }, "a", new[] { "b" }, "b");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		public void HandBuiltMultiDimArrayAccess([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var grid = new[,] { { 1, 2 }, { 3, 4 } };

			Expression<Func<int[,]>> captured = () => grid;

			db.Person
				.Where(IdEquals(Expression.ArrayAccess(captured.Body, Expression.Constant(1), Expression.Constant(0))))
				.Select(p => p.ID)
				.ToArray()
				.ShouldBe([3]);

			db.Person
				.Where(p => p.ID == grid[1, 0])
				.Select(p => p.ID)
				.ToArray()
				.ShouldBe([3]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		public void HandBuiltArrayAccessCompiledQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var ids = new[] { 2, 3 };

			Expression<Func<Model.ITestDataContext, IQueryable<int>>> compilerBuilt = dc => dc.Person
				.Where(p => p.ID == ids[0])
				.Select(p => p.ID);

			var handBuilt = (Expression<Func<Model.ITestDataContext, IQueryable<int>>>)new ArrayIndexToArrayAccess().Visit(compilerBuilt);

			foreach (var query in new[] { CompiledQuery.Compile(compilerBuilt), CompiledQuery.Compile(handBuilt) })
			{
				ids[0] = 2;

				query(db)
					.ToArray()
					.ShouldBe([2]);

				ids[0] = 3;

				query(db)
					.ToArray()
					.ShouldBe([3]);
			}
		}

		// The branches of a client-evaluable conditional whose test becomes a subquery are not exposed (#6138), so the
		// IndexExpression reaches the cache manager and the cache compare unrewritten.
		// The gap is in expression processing ahead of any provider code, so one provider family is enough.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6138")]
		public void HandBuiltArrayAccessInUnvisitedConditionalBranch([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var closureQuery = db.Person;

			IQueryable<string> HandBuilt(string[] values)
			{
				Expression<Func<string[]>> captured = () => values;
				Expression<Func<bool>>     test     = () => closureQuery.Any();

				var person = Expression.Parameter(typeof(Model.Person), "p");

				var selector = Expression.Lambda<Func<Model.Person, string>>(
					Expression.Condition(test.Body, Expression.ArrayAccess(captured.Body, Expression.Constant(0)), Expression.Constant("x")),
					person);

				return db.Person
					.Where(p => p.ID == 1)
					.Select(selector);
			}

			IQueryable<string> CompilerBuilt(string[] values)
			{
				return db.Person
					.Where(p => p.ID == 1)
					.Select(p => closureQuery.Any() ? values[0] : "x");
			}

			ShouldReadEachValue(CompilerBuilt, new[] { "a" }, "a", new[] { "b" }, "b");
			ShouldReadEachValue(HandBuilt,     new[] { "a" }, "a", new[] { "b" }, "b");
		}

		// The gap is in expression processing ahead of any provider code, so one provider family is enough.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6138")]
		public void HandBuiltArrayAccessInUnvisitedConditionalBranch_Where([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var closureQuery = db.Person;

			IQueryable<int> HandBuilt(int[] ids)
			{
				Expression<Func<int[]>> captured = () => ids;
				Expression<Func<bool>>  test     = () => closureQuery.Any();

				var value = Expression.Condition(test.Body, Expression.ArrayAccess(captured.Body, Expression.Constant(0)), Expression.Constant(0));

				return db.Person
					.Where(IdEquals(value))
					.Select(p => p.ID);
			}

			IQueryable<int> CompilerBuilt(int[] ids)
			{
				return db.Person
					.Where(p => p.ID == (closureQuery.Any() ? ids[0] : 0))
					.Select(p => p.ID);
			}

			ShouldReadEachValue(CompilerBuilt, new[] { 2 }, 2, new[] { 3 }, 3);
			ShouldReadEachValue(HandBuilt,     new[] { 2 }, 2, new[] { 3 }, 3);

			HandBuilt(new[] { 2 })
				.ToSqlQuery()
				.Parameters
				.Select(p => p.Name)
				.ShouldBe(["ids"]);
		}

		// Parity with the compiler's get_Item call over an association, which may itself be untranslatable: whatever
		// the compiler-built form does, the hand-built one must do the same.
		// Eager-load path over Children; Access is excluded as for LoadWith12.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5879")]
		public void HandBuiltIndexerOverAssociation([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var parent       = Expression.Parameter(typeof(Model.Parent), "p");
			var firstChildId = Expression.Property(
				Expression.MakeIndex(
					Expression.Property(parent, nameof(Model.Parent.Children)),
					typeof(List<Model.Child>).GetProperty("Item"),
					[Expression.Constant(0)]),
				nameof(Model.Child.ChildID));

			ShouldBehaveTheSame(
				db.Parent
					.Where(p => p.ParentID == 1)
					.Select(Expression.Lambda<Func<Model.Parent, int>>(firstChildId, parent)),
				db.Parent
					.Where(p => p.ParentID == 1)
					.Select(p => p.Children[0].ChildID));

			ShouldBehaveTheSame(
				db.Parent
					.Where(Expression.Lambda<Func<Model.Parent, bool>>(Expression.Equal(firstChildId, Expression.Constant(11)), parent))
					.Select(p => p.ParentID),
				db.Parent
					.Where(p => p.Children[0].ChildID == 11)
					.Select(p => p.ParentID));
		}

		static void ShouldBehaveTheSame<T>(IQueryable<T> handBuilt, IQueryable<T> compilerBuilt)
		{
			T[]?       expected      = null;
			Exception? expectedError = null;

			try
			{
				expected = compilerBuilt.ToArray();
			}
			catch (Exception ex)
			{
				expectedError = ex;
			}

			if (expectedError == null)
			{
				handBuilt
					.ToArray()
					.ShouldBe(expected!, ignoreOrder: true);
			}
			else
			{
				var error = Shouldly.Should.Throw<Exception>(() => handBuilt.ToArray());

				error
					.GetType()
					.ShouldBe(expectedError.GetType());

				error.Message.ShouldBe(expectedError.Message);
			}
		}

		sealed class ArrayIndexToArrayAccess : ExpressionVisitor
		{
			protected override Expression VisitBinary(BinaryExpression node)
			{
				return node.NodeType == ExpressionType.ArrayIndex
					? Expression.ArrayAccess(Visit(node.Left), Visit(node.Right))
					: base.VisitBinary(node);
			}
		}

		#endregion
	}
}
