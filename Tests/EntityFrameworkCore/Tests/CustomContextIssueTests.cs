using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;

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

	}
}
