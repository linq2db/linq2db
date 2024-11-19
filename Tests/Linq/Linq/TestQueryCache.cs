using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Tests.Model;

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
				throw new InvalidOperationException();
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

				Assert.Multiple(() =>
				{
					Assert.That(found, Is.True);
					Assert.That(foundKey, Is.True);
				});

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

				Assert.Multiple(() =>
				{
					Assert.That(found, Is.True);
					Assert.That(foundKey, Is.True);
				});

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

		[Test]
		public void TestSqlQueryDepended([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ManyFields>())
			{
				Query<ManyFields>.ClearCache();

				var currentMiss = Query<ManyFields>.CacheMissCount;

				int i;
				for (i = 1; i <= 5; i++)
				{
					var test = db
						.GetTable<ManyFields>()
						.Where(x => Helper.GetField(x, i) == i);

					var sqlStr = test.ToString();
					TestContext.Out.WriteLine(sqlStr);
				}

				Assert.That(Query<ManyFields>.CacheMissCount - currentMiss, Is.EqualTo(5));

				currentMiss = Query<ManyFields>.CacheMissCount;

				for (i = 1; i <= 5; i++)
				{
					var test = db
						.GetTable<ManyFields>()
						.Where(x => Helper.GetField(x, i) == i);

					var sqlStr = test.ToString();
				}

				Assert.That(Query<ManyFields>.CacheMissCount, Is.EqualTo(currentMiss));
			}
		}

		[Test]
		public void TestContextLeak([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ctxRef = ExecuteQuery(context);

			GC.Collect();
			Assert.That(ctxRef.TryGetTarget(out _), Is.False);

			WeakReference<IDataContext> ExecuteQuery(string ctx)
			{
				Query<Person>.ClearCache();
				using var db = GetDataContext(ctx);

				_ = db.Person.FirstOrDefault();

				return new WeakReference<IDataContext>(db);
			}
		}
	}
}
