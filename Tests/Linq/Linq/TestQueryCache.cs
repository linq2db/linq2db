using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class TestQueryCache : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			public int Id         { get; set; }
			public string? StrKey { get; set; }
			public string? Value  { get; set; }
		}

		[Table]
		sealed class SampleClassWithIdentity
		{
			[Identity]
			public int Id         { get; set; }
			public string? Value  { get; set; }
		}

		[Table]
		sealed class ManyFields
		{
			[PrimaryKey]
			public int  Id     { get; set; }
			[Column] public int? Field1 { get; set; }
			[Column] public int? Field2 { get; set; }
			[Column] public int? Field3 { get; set; }
			[Column] public int? Field4 { get; set; }
			[Column] public int? Field5 { get; set; }
		}

		static class Helper
		{
			[ExpressionMethod(nameof(GetFieldImpl))]
			public static int? GetField(ManyFields entity, [SqlQueryDependent] int i)
			{
				throw new ServerSideOnlyException(nameof(GetField));
			}

			private static Expression<Func<ManyFields, int, int?>> GetFieldImpl()
			{
				return (entity, i) => Sql.Property<int?>(entity, $"Field{i}");
			}
		}

		[Test]
		public void BasicOperations([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values("Value1", "Value2")] string columnName)
		{
			var ms = CreateMappingSchema(columnName);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<SampleClassWithIdentity>())
			{
				db.Insert(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
				db.Insert(new SampleClass() { Id = 2, StrKey = "K2", Value = "V2" });
				db.InsertOrReplace(new SampleClass() { Id = 3, StrKey = "K3", Value = "V3" });
				db.InsertWithIdentity(new SampleClassWithIdentity() { Value = "V4" });

				db.Delete(new SampleClass() { Id = 1, StrKey = "K1" });
				db.Update(new SampleClass() { Id = 2, StrKey = "K2", Value = "VU" });

				var found = null != table.GetSelectQuery().Find(columnName,
								static (columnName, e) => e is SqlField f && f.PhysicalName == columnName);

				var foundKey = null != table.GetSelectQuery().Find(columnName,
					               static (columnName, e) => e is SqlField f && f.PhysicalName == columnName);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(found, Is.True);
					Assert.That(foundKey, Is.True);
				}

				var result = table.ToArray();
			}
		}

		[Test]
		public async Task BasicOperationsAsync([IncludeDataSources(ProviderName.SQLiteMS)] string context, [Values("Value1", "Value2")] string columnName)
		{
			var ms = CreateMappingSchema(columnName);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<SampleClass>())
			using (db.CreateLocalTable<SampleClassWithIdentity>())
			{
				await db.InsertAsync(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
				await db.InsertAsync(new SampleClass() { Id = 2, StrKey = "K2", Value = "V2" });
				await db.InsertOrReplaceAsync(new SampleClass() { Id = 3, StrKey = "K3", Value = "V3" });
				await db.InsertWithIdentityAsync(new SampleClassWithIdentity() { Value = "V4" });

				await db.DeleteAsync(new SampleClass() { Id = 1, StrKey = "K1" });
				await db.UpdateAsync(new SampleClass() { Id = 2, StrKey = "K2", Value = "VU" });

				var found = null != table.GetSelectQuery().Find(columnName,
					            static (columnName, e) => e is SqlField f && f.PhysicalName == columnName);

				var foundKey = null != table.GetSelectQuery().Find(columnName,
								static (columnName, e) => e is SqlField f && f.PhysicalName == columnName);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(found, Is.True);
					Assert.That(foundKey, Is.True);
				}

				var result = await table.ToArrayAsync();
			}
		}

		[Test]
		public void TestSchema([IncludeDataSources(ProviderName.SQLiteMS, TestProvName.AllClickHouse)] string context)
		{
			void TestMethod(string columnName, string? schemaName = null)
			{
				var ms = CreateMappingSchema(columnName, schemaName);
				using (var db = (DataConnection)GetDataContext(context, ms))
				using (db.CreateLocalTable<SampleClass>())
				{
					db.Insert(new SampleClass() { Id = 1, StrKey = "K1", Value = "V1" });
					if (!db.LastQuery!.Contains(columnName))
						throw new AssertionException("Invalid schema");
				}
			}

			TestMethod("Value1");
			TestMethod("Value2");

			TestMethod("ValueF1", "FAIL");
			// Fluent mapping makes schema unique.
			TestMethod("ValueF2", "FAIL");
		}

		private static MappingSchema CreateMappingSchema(string columnName, string? schemaName = null)
		{
			var ms = new MappingSchema(schemaName);
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<SampleClass>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.StrKey).IsNullable(false).IsPrimaryKey().HasColumnName("Key" + columnName).HasLength(50)
				.Property(e => e.Value).HasColumnName(columnName).HasLength(50);

			builder.Entity<SampleClassWithIdentity>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Value).HasColumnName(columnName).HasLength(50);

			builder.Build();

			return ms;
		}

		[Test, QueryCacheTest]
		public void TestSqlQueryDepended([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<ManyFields>();
			tb.ClearCache();

			var currentMiss = tb.GetCacheMissCount();

			int i;
			for (i = 1; i <= 5; i++)
			{
				var test = db
						.GetTable<ManyFields>()
						.Where(x => Helper.GetField(x, i) == i);

				_ = test.ToSqlQuery();
			}

			Assert.That(tb.GetCacheMissCount() - currentMiss, Is.EqualTo(5));

			currentMiss = tb.GetCacheMissCount();

			for (i = 1; i <= 5; i++)
			{
				var test = db
						.GetTable<ManyFields>()
						.Where(x => Helper.GetField(x, i) == i);

				_ = test.ToSqlQuery();
			}

			Assert.That(tb.GetCacheMissCount(), Is.EqualTo(currentMiss));
		}

		[Test]
		public void TestContextLeak([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ctxRef = ExecuteQuery(context);

			GC.Collect();
			Assert.That(ctxRef.TryGetTarget(out _), Is.False);

			WeakReference<IDataContext> ExecuteQuery(string ctx)
			{
				using var db = GetDataContext(ctx);
				db.Person.ClearCache();

				_ = db.Person.FirstOrDefault();

				return new WeakReference<IDataContext>(db);
			}
		}

		[Test]
		[QueryCacheTest]
		public void LambdaParameterBindingIsPartOfTheKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			Expression<Func<Model.Person, bool>> P(bool correlated)
			{
				var a = Expression.Parameter(typeof(Model.Person), "p");
				var b = Expression.Parameter(typeof(Model.Person), "p");
				var inner = Expression.Lambda(
					Expression.GreaterThan(
						Expression.Field(b, nameof(Model.Person.ID)),
						Expression.Field(correlated ? a : b, nameof(Model.Person.ID))), b);
				var body = Expression.Call(typeof(Queryable), nameof(Queryable.Any),
					[typeof(Model.Person)], db.Person.Expression, Expression.Quote(inner));
				return Expression.Lambda<Func<Model.Person, bool>>(body, a);
			}

			var uncorrelated = db.Person
				.Where(P(false))
				.Select(p => p.ID);
			uncorrelated.ClearCache();
			var missCount = uncorrelated.GetCacheMissCount();
			uncorrelated
				.ToArray()
				.ShouldBeEmpty();
			uncorrelated
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);

			var correlated = db.Person
				.Where(P(true))
				.Select(p => p.ID);
			correlated
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2, 3]);
			correlated
				.GetCacheMissCount()
				.ShouldBe(missCount + 2);

			var correlatedAgain = db.Person
				.Where(P(true))
				.Select(p => p.ID);
			correlatedAgain
				.ToArray()
				.Length
				.ShouldBe(3);
			correlatedAgain
				.GetCacheMissCount()
				.ShouldBe(missCount + 2);

			var uncorrelatedAgain = db.Person
				.Where(P(false))
				.Select(p => p.ID);
			uncorrelatedAgain
				.ToArray()
				.ShouldBeEmpty();
			uncorrelatedAgain
				.GetCacheMissCount()
				.ShouldBe(missCount + 2);
		}

		[Test]
		[QueryCacheTest]
		public void CompilerLambdaParameterBindingsAndAccessors([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int minimumId, bool rename, bool reverse = false)
			{
				if (reverse)
					return db.Person
						.Where(p2 => db.Person.Any(p => p2.ID > p.ID) && p2.ID >= minimumId)
						.Select(p2 => p2.ID);

				if (rename)
					return db.Person
						.Where(p2 => db.Person.Any(p => p.ID > p2.ID) && p2.ID >= minimumId)
						.Select(p2 => p2.ID);

				return db.Person
					.Where(p => db.Person.Any(p2 => p2.ID > p.ID) && p.ID >= minimumId)
					.Select(p => p.ID);
			}

			var original = Query(1, false);
			original.ClearCache();
			var missCount = original.GetCacheMissCount();
			original
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2, 3]);
			original
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);

			// Swapping names preserves bindings, but the names become table aliases in the SQL, so this is a new entry.
			var renamed = Query(2, true);
			renamed
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([2, 3]);
			renamed
				.GetCacheMissCount()
				.ShouldBe(missCount + 2);

			var reversed = Query(2, true, reverse: true);
			reversed
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([2, 3, 4]);
			reversed
				.GetCacheMissCount()
				.ShouldBe(missCount + 3);

			var originalAgain = Query(3, false);
			originalAgain
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([3]);
			originalAgain
				.GetCacheMissCount()
				.ShouldBe(missCount + 3);

			var renamedAgain = Query(3, true);
			renamedAgain
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([3]);
			renamedAgain
				.GetCacheMissCount()
				.ShouldBe(missCount + 3);

			var reversedAgain = Query(3, true, reverse: true);
			reversedAgain
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([3, 4]);
			reversedAgain
				.GetCacheMissCount()
				.ShouldBe(missCount + 3);
		}

		[Test]
		[QueryCacheTest]
		public void CompilerLambdaShadowingIsPartOfTheKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var shadowed = db.Person
				.Where(p => db.Person.Any(p => p.ID > p.ID + 0))
				.Select(p => p.ID);
			shadowed.ClearCache();
			var missCount = shadowed.GetCacheMissCount();
			shadowed
				.ToArray()
				.ShouldBeEmpty();
			shadowed
				.GetCacheMissCount()
				.ShouldBe(missCount + 1);

			var correlated = db.Person
				.Where(p => db.Person.Any(p2 => p2.ID > p.ID + 0))
				.Select(p => p.ID);
			correlated
				.ToArray()
				.OrderBy(id => id)
				.ToArray()
				.ShouldBe([1, 2, 3]);
			correlated
				.GetCacheMissCount()
				.ShouldBe(missCount + 2);

			long Nested(int minimumId)
			{
				var query = db.Person
					.Where(p => db.Person.Any(p2 => p2.ID > p.ID) && p.ID >= minimumId)
					.Select(p => p.ID);
				query.ToArray();
				return query.GetCacheMissCount();
			}

			var nestedMisses = Nested(1);
			Nested(2).ShouldBe(nestedMisses);
			Nested(3).ShouldBe(nestedMisses);
		}

		[Test]
		[QueryCacheTest]
		public void CapturedQueryableInstanceHitsTheCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// The captured table and query are inlined as the same instances on every run, inside lambdas whose
			// parameters are new on every run.
			var children = db.GetTable<Model.Child>();
			var filtered = db.Child.Where(c => c.ChildID > 0);

			long Table(int id)
			{
				var query = db.Parent
					.Where(p => children.Any(c => c.ParentID == p.ParentID) && p.ParentID >= id)
					.Select(p => p.ParentID);
				query.ToArray();
				return query.GetCacheMissCount();
			}

			long Query(int id)
			{
				var query = db.Parent
					.Where(p => filtered.Any(c => c.ParentID == p.ParentID) && p.ParentID >= id)
					.Select(p => p.ParentID);
				query.ToArray();
				return query.GetCacheMissCount();
			}

			var tableMisses = Table(1);
			Table(2)
				.ShouldBe(tableMisses);
			Table(3)
				.ShouldBe(tableMisses);

			var queryMisses = Query(1);
			Query(2)
				.ShouldBe(queryMisses);
			Query(3)
				.ShouldBe(queryMisses);
		}

		static int ClientSide(Sql.IQueryableContainer? container) => container == null ? 1 : 2;

		// Query-cache test: provider-independent.
		[Test]
		[QueryCacheTest]
		public void NullQueryableContainerArgumentHitsCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query()
			{
				return db.Person
					.Where(p => p.ID == 1)
					.Select(p => ClientSide(null));
			}

			Query()
				.ToArray()
				.ShouldBe([1]);

			var missCount = Query().GetCacheMissCount();

			Query()
				.ToArray()
				.ShouldBe([1]);

			Query()
				.GetCacheMissCount()
				.ShouldBe(missCount);
		}
	}
}
