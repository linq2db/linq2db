using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Interceptors;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using NUnit.Framework;

using Shouldly;

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

		public class TypeRegisteredEntity
		{
			public int Id { get; set; }
			public string? Name { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/261")]
		public void GetTableOfEntityRegisteredByType([EFDataSources] string provider)
		{
			Test<TypeRegisteredEntity>();

			void Test<T>()
				where T : class
			{
				var connectionString = GetConnectionString(provider);

				var optionsBuilder = new DbContextOptionsBuilder();
				optionsBuilder.UseLoggerFactory(LoggerFactory);

				optionsBuilder = provider switch
				{
					// UseNodaTime called due to bug in Npgsql v8, where UseNodaTime ignored, when UseNpgsql already called without it
					_ when provider.IsAnyOf(TestProvName.AllPostgreSQL)
						=> optionsBuilder.UseNpgsql(connectionString, o => o.UseNodaTime()).UseLinqToDB(builder => builder.AddCustomOptions(o => o.UseMappingSchema(NodaTimeSupport))),
#if EF_MYSQL
					_ when provider.IsAnyOf(TestProvName.AllMySql) => optionsBuilder
#if !NETFRAMEWORK
						.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#else
						.UseMySql(connectionString),
#endif
#endif
					_ when provider.IsAnyOf(TestProvName.AllSQLite) => optionsBuilder.UseSqlite(connectionString),
					_ when provider.IsAnyOf(TestProvName.AllSqlServer) => optionsBuilder.UseSqlServer(connectionString),
					_ => throw new InvalidOperationException($"ProviderSetup is not implemented for provider {provider}")
				};

				using var ctx = new TypeRegisteredEntityContext<T>(optionsBuilder.Options);

				using (new DisableBaseline("create db"))
				{
					ctx.Database.EnsureDeleted();
					ctx.Database.EnsureCreated();
				}

				using var db = ctx.CreateLinqToDBConnection();
				var result = db.GetTable<T>().ToArray();
			}
		}

		public sealed class TypeRegisteredEntityContext<T>(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity(typeof(T), x =>
				{
				});
			}
		}

		// Regression test for #4669 with its own DbContext. The equivalent test on the shared Northwind model
		// (ToolsTests.TestGlobalQueryFilters) cannot serve as one: it fails only when run alone, and passes
		// whenever ToolsTests.NavigationProperties has run first - which it always has in a full run, since it
		// sorts earlier. The masking is process-global state tied to the shared NorthwindContextBase model, so a
		// separate context type is what makes the outcome independent of execution order.
		public class QueryFilterItem
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		public sealed class QueryFilterContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<QueryFilterItem> Items { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				// Mirrors IssueContextBase's ShadowTable: IsDeleted is a SHADOW property, and the query filter
				// reaches it through EF.Property. Both tests that reproduce #4669 have a filter of this shape;
				// an earlier attempt here used EF.Property against a real CLR property and did not reproduce.
				modelBuilder.Entity<QueryFilterItem>(e =>
				{
					e.Property(x => x.Id).ValueGeneratedNever();
					e.Property<bool>("IsDeleted").IsRequired();
					e.HasQueryFilter(p => !EF.Property<bool>(p, "IsDeleted"));
				});
			}
		}

		// Named distinctly from IssueTests.EfQueryAfterLinqToDbQuery, which covers a different aspect of the same issue and
		// is not gated - a FullyQualifiedName~ filter would otherwise select both.
		[ActiveIssue(4669, Configuration = TestProvName.AllMySql, ErrorTypeName = "System.Diagnostics.UnreachableException")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4669")]
		public void ShadowQueryFilterEfQueryAfterLinqToDbCall([EFDataSources] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => optionsBuilder.UseNpgsql(connectionString),
#if EF_MYSQL
				_ when provider.IsAnyOf(TestProvName.AllMySql) => optionsBuilder
#if !NETFRAMEWORK
					.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)),
#else
					.UseMySql(connectionString),
#endif
#endif
				_ when provider.IsAnyOf(TestProvName.AllSQLite) => optionsBuilder.UseSqlite(connectionString),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer) => optionsBuilder.UseSqlServer(connectionString),
				_ => throw new InvalidOperationException($"ProviderSetup is not implemented for provider {provider}")
			};

			using var ctx = new QueryFilterContext(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			ctx.Add(new QueryFilterItem { Id = 1, Name = "TestOne" });
			ctx.Add(new QueryFilterItem { Id = 2, Name = "Other"   });
			ctx.SaveChanges();

			// Same shape as IssueTests.EfQueryAfterLinqToDbQuery: a linq2db call, then the EF call that throws.
			var linq2dbResult = ctx.Items.Where(x => x.Name!.Contains("Test")).OrderBy(x => x.Name).ToLinqToDB().ToList();
			var efResult      = ctx.Items.Where(x => x.Name!.StartsWith("Test")).ToList();

			linq2dbResult.Count.ShouldBe(efResult.Count);
		}

#if !NETFRAMEWORK // requires UseCollation API

		public class CollationMergeTempTable
		{
			public int Id { get; set; }
			public int Code { get; set; }
		}

		public class CollationMergeTable
		{
			public int Id { get; set; }
			public int Code { get; set; }
		}

		public sealed class CollationMergeContext(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.UseCollation("Latin1_General_CS_AS");
				modelBuilder.Entity<CollationMergeTable>(e =>
				{
					e.Property(e => e.Id).HasColumnName("ID");
					e.Property(e => e.Code).HasColumnName("CODE");
				});
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4657")]
		public void MergeCteWithOutputIntoTempTableUnderCaseSensitiveCollation([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder().UseSqlServer(connectionString);
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			using var ctx = new CollationMergeContext(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			using var db = ctx.CreateLinqToDBConnection();

			using var tempTable1 = db.CreateTempTable([new CollationMergeTempTable() { Id = 1, Code = 2 }], tableName: "#CollationMergeTempTable1");
			using var tempTable2 = db.CreateTempTable([new CollationMergeTempTable() { Id = 1, Code = 2 }], tableName: "#CollationMergeTempTable2");

			var q = ctx.Set<CollationMergeTable>()
				.AsCte()
				.Merge()
				.Using(tempTable1)
				.On((s, t) => s.Id == t.Id)
				.InsertWhenNotMatched(s => new CollationMergeTable() { Code = s.Code })
				.MergeWithOutputInto(
					tempTable2,
					(s, t, o) => new CollationMergeTempTable() { Id = o.Id, Code = o.Code });
		}
#endif

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/306")]
		public void UnwrapDataObjectInterceptorIsCalled([EFIncludeDataSources(TestProvName.AllSQLite)] string provider)
		{
			var interceptor = new DummyInterceptor();

			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);
			optionsBuilder.UseSqlite(connectionString);
			optionsBuilder.UseLinqToDB(builder =>
			{
				builder.AddInterceptor(interceptor);
			});

			using var ctx = new UnwrapInterceptorContext(optionsBuilder.Options);
			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var id = 1;
			var query = ctx.Set<UnwrapInterceptorEntity>().Where(e => e.Id == id).ToLinqToDB().ToArray();

			Assert.That(interceptor.Count, Is.GreaterThan(0));
		}

		public class UnwrapInterceptorEntity
		{
			public int Id { get; set; }
			public int Value { get; set; }
		}

		public class UnwrapInterceptorContext(DbContextOptions options) : DbContext(options)
		{
			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				base.OnModelCreating(modelBuilder);

				modelBuilder.Entity<UnwrapInterceptorEntity>(e =>
				{
					e.HasData(new UnwrapInterceptorEntity() { Id = 1, Value = 11 });
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5296")]
		public async ValueTask EfConnectionIsReleasedAfterProviderDetection([EFIncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => optionsBuilder.UseNpgsql(connectionString),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer)  => optionsBuilder.UseSqlServer(connectionString),
				_                                                   => throw new InvalidOperationException($"ProviderSetup is not implemented for provider {provider}")
			};

			using var ctx = new ConnectionReleaseContext(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();

				// creating the linq2db connection triggers provider version auto-detection,
				// which opens EF's shared DbConnection and (before the fix) leaves it open
				using (var db = ctx.CreateLinqToDBConnection())
				{
					await db.GetTable<ConnectionReleaseEntity>().ToListAsyncLinqToDB();
				}

				// must not throw: a leaked-open connection bound to the dropped database
				// breaks EF's drop/recreate cycle (SocketException / broken-connection recovery)
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}
		}

		public sealed class ConnectionReleaseContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<ConnectionReleaseEntity> Entities { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<ConnectionReleaseEntity>();
			}
		}

		public record ConnectionReleaseEntity(int Id, string Name);

#if NET9_0_OR_GREATER
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4783")]
		public async ValueTask PostgreSqlNativeEnumBulkCopyRoundTrip([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = optionsBuilder.UseNpgsql(
				connectionString,
				o => o.UseNodaTime().MapEnum<NativeEnumStatus>());

			using var ctx = new NativeEnumContext(optionsBuilder.Options);

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var entities = new List<NativeEnumRecord>()
			{
				new(0, "EF", NativeEnumStatus.Open, NativeEnumStatus.Open),
				new(0, "EF", NativeEnumStatus.Closed, NativeEnumStatus.Closed),
				new(0, "EF", NativeEnumStatus.Closed, null)
			};

			ctx.NativeEnumRecords.AddRange(entities);
			await ctx.SaveChangesAsync();

			await ctx.BulkCopyAsync(entities.Select(x => x with { Source = "linq2db" }));

			using var db = ctx.CreateLinqToDBConnection();
			var results = await db.GetTable<NativeEnumRecordRaw>().OrderBy(r => r.Id).ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].Status,         Is.EqualTo(entities[i % entities.Count].Status),         $"{results[i].Source}:({results[i].Id})");
					Assert.That(results[i].NullableStatus, Is.EqualTo(entities[i % entities.Count].NullableStatus), $"{results[i].Source}:({results[i].Id})");
				}
			}
		}

		public sealed class NativeEnumContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<NativeEnumRecord> NativeEnumRecords { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<NativeEnumRecord>();
			}
		}

		public record NativeEnumRecord(
			int Id,
			string Source,
			NativeEnumStatus Status,
			NativeEnumStatus? NullableStatus);

		[LinqToDB.Mapping.Table("NativeEnumRecords", IsColumnAttributeRequired = false)]
		public record NativeEnumRecordRaw(
			int Id,
			string Source,
			object Status,
			object? NullableStatus);

		public enum NativeEnumStatus
		{
			Open,
			Closed
		}
#endif

#if NET8_0_OR_GREATER
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4917")]
		public async ValueTask ContextCreatedFromNpgsqlDataSource([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			var connectionString = GetConnectionString(provider);
			var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
			var optionsBuilder = new DbContextOptionsBuilder().UseLoggerFactory(LoggerFactory).UseNpgsql(dataSource);

			using var ctx = new DataSourceContext(optionsBuilder.Options);
			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			using var db  = ctx.CreateLinqToDBConnection(); //should not throw an exception

			// the data source carries the connection string, so EF exposes none: the dialect still has to come
			// from this server and not from whichever PostgreSQL instance the process resolved first
			db.DataProvider.Name.ShouldBe(DataConnection.GetDataProvider(provider).Name);

			await db.GetTable<DataSourceRecord>().ToListAsyncLinqToDB();
		}

		public sealed class DataSourceContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<DataSourceRecord> DataSourceRecords { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<DataSourceRecord>();
			}
		}

		public record DataSourceRecord(int Id, string Name);
#endif
		
#if NET9_0_OR_GREATER
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4940")]
		public async ValueTask EnumDmlOnTempTableOfNotMappedEntity([EFIncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = optionsBuilder.UseNpgsql(
				connectionString,
				o => o.MapEnum<EnumDmlStatus>().MapEnum<EnumDmlNullableStatus>());

			using var ctx = new EnumDmlContext(optionsBuilder.Options);
			using var db  = ctx.CreateLinqToDBConnection();

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var notMappedEntitiesForTempTable = new List<EnumDmlRecordNotMapped>
			{
				new(1, "TempTable", EnumDmlStatus.Open, EnumDmlNullableStatus.Open),
				new(2, "TempTable", EnumDmlStatus.Closed, null)
			};
			using var tempTable = db.CreateTempTable(notMappedEntitiesForTempTable);//check valid sql for temp table and insert without parameters

			var notMappedEntityForInsert = new EnumDmlRecordNotMapped(3, "Insert", EnumDmlStatus.Open, null);
			db.Insert(notMappedEntityForInsert, tableName: tempTable.TableName, tableOptions: tempTable.TableOptions); //check enum as parameter
			
			var notMappedEntitiesForBulkCopy = new List<EnumDmlRecordNotMapped>
			{
				new(4, "BulkCopy", EnumDmlStatus.Closed, EnumDmlNullableStatus.Closed),
				new(5, "BulkCopy", EnumDmlStatus.Open, null)
			};
			tempTable.BulkCopy(new BulkCopyOptions {BulkCopyType = BulkCopyType.ProviderSpecific}, notMappedEntitiesForBulkCopy); //check bulk copy

			var notMappedEntitiesForMerge = new List<EnumDmlRecordNotMapped>
			{
				new(6, "Merge", EnumDmlStatus.Open, EnumDmlNullableStatus.Closed),
				new(7, "Merge", EnumDmlStatus.Open, null)
			};
			tempTable
				.Merge()
				.Using(notMappedEntitiesForMerge)
				.On(s => s.Source, t => t.Source)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.Merge(); // check merge

			//it is impossible to select raw data from temp table
			var results = await tempTable.OrderBy(x => x.Id).ToArrayAsync();
			var allItems = notMappedEntitiesForTempTable
				.Concat([notMappedEntityForInsert])
				.Concat(notMappedEntitiesForBulkCopy)
				.Concat(notMappedEntitiesForMerge)
				.ToArray();
			
			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < results.Length; i++)
				{
					Assert.That(results[i].Status,         Is.EqualTo(allItems[i].Status),         $"{results[i].Source}");
					Assert.That(results[i].NullableStatus, Is.EqualTo(allItems[i].NullableStatus), $"{results[i].Source}");
				}
			}
		}
		
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4940")]
		public async ValueTask EnumDmlOnMappedEntityAndTempTable([EFIncludeDataSources(TestProvName.AllPostgreSQL15Plus)] string provider)
		{
			var connectionString = GetConnectionString(provider);

			var optionsBuilder = new DbContextOptionsBuilder();
			optionsBuilder.UseLoggerFactory(LoggerFactory);

			optionsBuilder = optionsBuilder.UseNpgsql(
				connectionString,
				o => o.MapEnum<EnumDmlStatus>().MapEnum<EnumDmlNullableStatus>());

			using var ctx = new EnumDmlContext(optionsBuilder.Options);
			using var db  = ctx.CreateLinqToDBConnection();

			using (new DisableBaseline("create db"))
			{
				ctx.Database.EnsureDeleted();
				ctx.Database.EnsureCreated();
			}

			var entitiesForTempTable = new List<EnumDmlRecord>
			{
				new(1, "TempTable", EnumDmlStatus.Open, EnumDmlNullableStatus.Closed),
				new(2, "TempTable", EnumDmlStatus.Closed, null)
			};
			using var tempTable = db.CreateTempTable(entitiesForTempTable, tableName: "enum_dml_temp_table");
			
			var entitiesForInsert = new List<EnumDmlRecord>
			{
				new(1, "Insert", EnumDmlStatus.Open, EnumDmlNullableStatus.Closed),
				new(2, "Insert", EnumDmlStatus.Closed, null)
			};
			entitiesForInsert.ForEach(x => db.Insert(x));
			
			var entitiesForBulkCopy = new List<EnumDmlRecord>
			{
				new(3, "BulkCopy", EnumDmlStatus.Closed, EnumDmlNullableStatus.Closed),
				new(4, "BulkCopy", EnumDmlStatus.Closed, null)
			};
			db.GetTable<EnumDmlRecord>().BulkCopy(new BulkCopyOptions {BulkCopyType = BulkCopyType.ProviderSpecific}, entitiesForBulkCopy);

			var entitiesForMerge = new List<EnumDmlRecord>
			{
				new(5, "Merge", EnumDmlStatus.Open, EnumDmlNullableStatus.Open),
				new(6, "Merge", EnumDmlStatus.Open, null)
			};
			db.GetTable<EnumDmlRecord>()
				.Merge()
				.Using(entitiesForMerge)
				.On(s => s.Source, t => t.Source)
				.InsertWhenNotMatched()
				.UpdateWhenMatched()
				.Merge();

			var inMemoryRecords = entitiesForInsert.Concat(entitiesForBulkCopy).Concat(entitiesForMerge).ToArray();
			var rawDbRecords  = await db.GetTable<EnumDmlRecordRaw>().OrderBy(x => x.Id).ToArrayAsync();
			var tempTableRecords = await tempTable.OrderBy(x => x.Id).ToArrayAsync();

			using (Assert.EnterMultipleScope())
			{
				for (var i = 0; i < tempTableRecords.Length; i++)
				{
					Assert.That(tempTableRecords[i].Status,         Is.EqualTo(entitiesForTempTable[i].Status),         $"{tempTableRecords[i].Source}");
					Assert.That(tempTableRecords[i].NullableStatus, Is.EqualTo(entitiesForTempTable[i].NullableStatus), $"{tempTableRecords[i].Source}");
				}
				
				for (var i = 0; i < rawDbRecords.Length; i++)
				{
					Assert.That(rawDbRecords[i].Status,         Is.EqualTo(inMemoryRecords[i].Status),         $"{rawDbRecords[i].Source}");
					Assert.That(rawDbRecords[i].NullableStatus, Is.EqualTo(inMemoryRecords[i].NullableStatus), $"{rawDbRecords[i].Source}");
				}
			}
		}
		
		public sealed class EnumDmlContext(DbContextOptions options) : DbContext(options)
		{
			public DbSet<EnumDmlRecord> EnumDmlRecords { get; set; } = null!;

			protected override void OnModelCreating(ModelBuilder modelBuilder)
			{
				modelBuilder.Entity<EnumDmlRecord>().HasNoKey();
			}
		}

		public record EnumDmlRecord(
			int                    Id,
			string                 Source,
			EnumDmlStatus          Status,
			EnumDmlNullableStatus? NullableStatus);

		[LinqToDB.Mapping.Table("EnumDmlRecords", IsColumnAttributeRequired = false)]
		public record EnumDmlRecordRaw(
			int     Id,
			string  Source,
			object  Status,
			object? NullableStatus);

		public record EnumDmlRecordNotMapped(
			int                    Id,
			string                 Source,
			EnumDmlStatus          Status,
			EnumDmlNullableStatus? NullableStatus);

		public enum EnumDmlStatus
		{
			Open,
			Closed
		}
		
		public enum EnumDmlNullableStatus
		{
			Open,
			Closed
		}
#endif
	}
}
