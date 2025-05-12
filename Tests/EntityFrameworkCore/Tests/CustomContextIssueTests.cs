using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel;
using LinqToDB.Interceptors;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	// Use for issues that need custom database setup to not interfere with Issue database tests
	[TestFixture]
	public class CustomContextIssueTests : TestBase
	{
		protected override string GetConnectionString(string provider)
		{
			// as test corrupts database, we should mark it non-created for other fixtures
			var connectionString = base.GetConnectionString(provider);
			TestContextTracker.LastContexts.Remove(connectionString);
			return connectionString;
		}

		#region Issue 261
		public class Issue261Table
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/261")]
		public void Issue261Test([EFDataSources] string provider)
		{
			Test<Issue261Table>();

			void Test<T>()
				where T : class
			{
				var connectionString = GetConnectionString(provider);

				var optionsBuilder = new DbContextOptionsBuilder();

				optionsBuilder = provider switch
				{
					// UseNodaTime called due to bug in Npgsql v8, where UseNodaTime ignored, when UseNpgsql already called without it
					_ when provider.IsAnyOf(TestProvName.AllPostgreSQL)
						=> optionsBuilder.UseNpgsql(connectionString, o => o.UseNodaTime()).UseLinqToDB(builder => builder.AddCustomOptions(o => o.UseMappingSchema(NodaTimeSupport))),
					_ when provider.IsAnyOf(TestProvName.AllMySql) => optionsBuilder
#if !NETFRAMEWORK
						.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#else
						.UseMySql(connectionString),
#endif
					_ when provider.IsAnyOf(TestProvName.AllSQLite) => optionsBuilder.UseSqlite(connectionString),
					_ when provider.IsAnyOf(TestProvName.AllSqlServer) => optionsBuilder.UseSqlServer(connectionString),
					_ => throw new InvalidOperationException($"ProviderSetup is not implemented for provider {provider}")
				};

				using var ctx = new Issue261Context<T>(optionsBuilder.Options);

				using (new DisableBaseline("create db"))
				{
					ctx.Database.EnsureDeleted();
					ctx.Database.EnsureCreated();
				}

				using var db = ctx.CreateLinqToDBConnection();
				var result = db.GetTable<T>().ToArray();
			}
		}

		public sealed class Issue261Context<T>(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity(typeof(T), x =>
				{
				});
			}
		}
#endregion

		#region Issue 4657
#if !NETFRAMEWORK // requires UseCollation API

		public class Issue4657TempTable
		{
			public int Id { get; set; }
			public int Code { get; set; }
		}

		public class Issue4657Table
		{
			public int Id { get; set; }
			public int Code { get; set; }
		}

		public sealed class Issue4657Context(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.UseCollation("Latin1_General_CS_AS");
				modelBuilder.Entity<Issue4657Table>(e =>
				{
					e.Property(e => e.Id).HasColumnName("ID");
					e.Property(e => e.Code).HasColumnName("CODE");
				});
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4657")]
		public void Issue4657Test([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder().UseSqlServer(connectionString);

			using var ctx = new Issue4657Context(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			using var db = ctx.CreateLinqToDBConnection();

			using var tempTable1 = db.CreateTempTable([new Issue4657TempTable() { Id = 1, Code = 2 }], tableName: "#Issue4657TempTable1");
			using var tempTable2 = db.CreateTempTable([new Issue4657TempTable() { Id = 1, Code = 2 }], tableName: "#Issue4657TempTable2");

			var q = ctx.Set<Issue4657Table>()
				.AsCte()
				.Merge()
				.Using(tempTable1)
				.On((s, t) => s.Id == t.Id)
				.InsertWhenNotMatched(s => new Issue4657Table() { Code = s.Code })
				.MergeWithOutputInto(
					tempTable2,
					(s, t, o) => new Issue4657TempTable() { Id = o.Id, Code = o.Code });
		}
#endif
#endregion

		#region issue 306
		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/306")]
		public void Issue306Test([EFIncludeDataSources(TestProvName.AllSQLite)] string provider)
		{
			var interceptor = new DummyInterceptor();

			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseSqlite(connectionString);
			optionsBuilder.UseLinqToDB(builder =>
			{
				builder.AddInterceptor(interceptor);
			});

			using var ctx = new Issue306Context(optionsBuilder.Options);
			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var id = 1;
			var query = ctx.Set<Issue306Entity>().Where(e => e.Id == id).ToLinqToDB().ToArray();

			Assert.That(interceptor.Count, Is.GreaterThan(0));
		}

		public class Issue306Entity
		{
			public int Id { get; set; }
			public int Value { get; set; }
		}

		public class Issue306Context(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				base.OnModelCreating(modelBuilder);

				modelBuilder.Entity<Issue306Entity>(e =>
				{
					e.HasData(new Issue306Entity() { Id = 1, Value = 11 });
				});
			}
		}

		public class DummyInterceptor : UnwrapDataObjectInterceptor
		{
			public int Count { get; set; }

			public override DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection)
			{
				Count++;
				return connection;
			}

			public override DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
			{
				Count++;
				return transaction;
			}

			public override DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
			{
				Count++;
				return command;
			}

			public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
			{
				Count++;
				return dataReader;
			}
		}
		#endregion

		#region Issue 4783

#if NET9_0_OR_GREATER
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4783")]
		public async ValueTask Issue4783Test([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();

			var dataSource = new NpgsqlDataSourceBuilder(connectionString)
				.MapEnum<Issue4783DBStatus>()
				.Build();

			optionsBuilder = optionsBuilder.UseNpgsql(
				dataSource,
				o => o.UseNodaTime().MapEnum<Issue4783DBStatus>())
				// TODO: remove and fix connection detection logic to use existing connection without extra connection
				//.UseLinqToDB(builder => builder.AddCustomOptions(o => o.UsePostgreSQL(connectionString, PostgreSQLVersion.v15)))
				;

			using var ctx = new Issue4783Context(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var entities = new List<Issue4783RecordDb>()
			{
				new(0, "EF", Issue4783DBStatus.Open, Issue4783DBStatus.Open),
				new(0, "EF", Issue4783DBStatus.Closed, Issue4783DBStatus.Closed),
				new(0, "EF", Issue4783DBStatus.Closed, null)
			};

			ctx.Issue4783DBRecords.AddRange(entities);
			await ctx.SaveChangesAsync();

			await ctx.BulkCopyAsync(entities.Select(x => x with { Source = "linq2db" }));

			using var db = ctx.CreateLinqToDBConnection();
			var results = await db.GetTable<Issue4783RecordDbRaw>().OrderBy(r => r.Id).ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].Status,         Is.EqualTo(entities[i % entities.Count].Status),         $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].NullableStatus, Is.EqualTo(entities[i % entities.Count].NullableStatus), $"{results[i].Source}:({results[i].Id})");
				}
			}
		}

		public sealed class Issue4783Context(DbContextOptions options) : DbContext(options)
		{
			public DbSet<Issue4783RecordDb> Issue4783DBRecords { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<Issue4783RecordDb>();
			}
		}

		public record Issue4783RecordDb(
			int Id,
			string Source,
			Issue4783DBStatus Status,
			Issue4783DBStatus? NullableStatus);

		[LinqToDB.Mapping.Table("Issue4783DBRecords", IsColumnAttributeRequired = false)]
		public record Issue4783RecordDbRaw(
			int Id,
			string Source,
			object Status,
			object? NullableStatus);

		public enum Issue4783DBStatus
		{
			Open,
			Closed
		}
#endif

		#endregion
		
		#region Issue 4940
		
#if NET9_0_OR_GREATER
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4940")]
		public async ValueTask Issue4940Test_NotMapped([EFIncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();

			var dataSource = new NpgsqlDataSourceBuilder(connectionString)
				.MapEnum<Issue4940DBStatus>()
				.Build();

			optionsBuilder = optionsBuilder.UseNpgsql(
					dataSource,
					o => o.MapEnum<Issue4940DBStatus>());

			using var ctx = new Issue4940Context(optionsBuilder.Options);
			using var db  = ctx.CreateLinqToDBConnection();

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var notMappedEntitiesForTempTable = new List<Issue4940RecordNotMapped>
			{
				new("TempTable", Issue4940DBStatus.Open, Issue4940DBStatus.Open),
				new("TempTable", Issue4940DBStatus.Closed, null)
			};
			var tempTable = db.CreateTempTable(notMappedEntitiesForTempTable);
			
			var notMappedEntitiesForBulkCopy = new List<Issue4940RecordNotMapped>
			{
				new("BulkCopy", Issue4940DBStatus.Closed, Issue4940DBStatus.Closed),
				new("BulkCopy", Issue4940DBStatus.Open, null)
			};
			tempTable.BulkCopy(new BulkCopyOptions {BulkCopyType = BulkCopyType.ProviderSpecific}, notMappedEntitiesForBulkCopy);

			var notMappedEntitiesForMerge = new List<Issue4940RecordNotMapped>
			{
				new("Merge", Issue4940DBStatus.Open, Issue4940DBStatus.Closed),
				new("Merge", Issue4940DBStatus.Open, null)
			};
			tempTable
				.Merge()
				.Using(notMappedEntitiesForMerge)
				.On(s => s.Source, t => t.Source)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.Merge();

			var nullStatusItems = await tempTable.Where(x => x.NullableStatus == null).ToArrayAsync();
			var openStatusItems = await tempTable.Where(x => x.NullableStatus == Issue4940DBStatus.Open).ToArrayAsync();
			var allItems        = notMappedEntitiesForTempTable.Concat(notMappedEntitiesForBulkCopy).Concat(notMappedEntitiesForMerge).ToArray();
			var results         = await tempTable.ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				foreach (var item in nullStatusItems)
					Assert.That(item.NullableStatus, Is.Null);
				
				foreach (var item in openStatusItems)
					Assert.That(item.NullableStatus, Is.EqualTo(Issue4940DBStatus.Open));
				
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].Status,         Is.EqualTo(allItems[i].Status),         $"{results[i].Source}");
					Assert.That(results[i].NullableStatus, Is.EqualTo(allItems[i].NullableStatus), $"{results[i].Source}");
				}
			}
		}
		
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4940")]
		public async ValueTask Issue4940Test([EFIncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();

			var dataSource = new NpgsqlDataSourceBuilder(connectionString)
				.MapEnum<Issue4940DBStatus>()
				.Build();

			optionsBuilder = optionsBuilder.UseNpgsql(
					dataSource,
					o => o.MapEnum<Issue4940DBStatus>());

			using var ctx = new Issue4940Context(optionsBuilder.Options);
			using var db  = ctx.CreateLinqToDBConnection();

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			db.CreateTempTable<Issue4940RecordDb>(tableName: "issue_4940_temp_table");
			
			var entitiesForInsert = new List<Issue4940RecordDb>
			{
				new(1, "Insert", Issue4940DBStatus.Open, Issue4940DBStatus.Closed),
				new(2, "Insert", Issue4940DBStatus.Closed, null)
			};
			foreach (var entity in entitiesForInsert)
				db.Insert(entity);
			
			var entitiesForBulkCopy = new List<Issue4940RecordDb>
			{
				new(3, "BulkCopy", Issue4940DBStatus.Closed, Issue4940DBStatus.Closed),
				new(4, "BulkCopy", Issue4940DBStatus.Closed, null)
			};
			db.GetTable<Issue4940RecordDb>().BulkCopy(new BulkCopyOptions {BulkCopyType = BulkCopyType.ProviderSpecific}, entitiesForBulkCopy);

			var entitiesForMerge = new List<Issue4940RecordDb>
			{
				new(5, "Merge", Issue4940DBStatus.Open, Issue4940DBStatus.Open),
				new(6, "Merge", Issue4940DBStatus.Open, null)
			};
			db.GetTable<Issue4940RecordDb>()
				.Merge()
				.Using(entitiesForMerge)
				.On(s => s.Source, t => t.Source)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.Merge();

			var nullStatusItems = await db.GetTable<Issue4940RecordDb>().Where(x => x.NullableStatus == null).ToArrayAsync();
			var openStatusItems = await db.GetTable<Issue4940RecordDb>().Where(x => x.NullableStatus == Issue4940DBStatus.Open).ToArrayAsync();
			var allItems        = entitiesForInsert.Concat(entitiesForBulkCopy).Concat(entitiesForMerge).ToArray();
			var results         = await db.GetTable<Issue4940RecordDb>().ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				foreach (var item in nullStatusItems)
					Assert.That(item.NullableStatus, Is.Null);
				
				foreach (var item in openStatusItems)
					Assert.That(item.NullableStatus, Is.EqualTo(Issue4940DBStatus.Open));
				
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].Status,         Is.EqualTo(allItems[i].Status),         $"{results[i].Source}");
					Assert.That(results[i].NullableStatus, Is.EqualTo(allItems[i].NullableStatus), $"{results[i].Source}");
				}
			}
		}
		
		public sealed class Issue4940Context(DbContextOptions options) : DbContext(options)
		{
			public DbSet<Issue4940RecordDb> Issue4940DBRecords { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<Issue4940RecordDb>();
			}
		}

		public record Issue4940RecordDb(
			int                Id,
			string             Source,
			Issue4940DBStatus  Status,
			Issue4940DBStatus? NullableStatus);
		
		public record Issue4940RecordNotMapped(
			string             Source,
			Issue4940DBStatus  Status,
			Issue4940DBStatus? NullableStatus);

		public enum Issue4940DBStatus
		{
			Open,
			Closed
		}
#endif
		#endregion
	}
}
