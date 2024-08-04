using System;
using System.Linq;
using FluentAssertions;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore.BaseTests;
using LinqToDB.EntityFrameworkCore.Tests.PostgreSQL.Models.NpgSqlEntities;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.Tests.PostgreSQL
{
	public class NpgSqlTests : TestsBase
	{
		private DbContextOptions<NpgSqlEntitiesContext> _options;

		static NpgSqlTests()
		{
			LinqToDBForEFTools.Initialize();
			DataConnection.TurnTraceSwitchOn();
		}

		public NpgSqlTests()
		{
			var optionsBuilder = new DbContextOptionsBuilder<NpgSqlEntitiesContext>();

			//optionsBuilder.UseNpgsql("Server=DBHost;Port=5432;Database=TestData;User Id=postgres;Password=TestPassword;Pooling=true;MinPoolSize=10;MaxPoolSize=100;", o => o.UseNodaTime());
			optionsBuilder.UseNpgsql("Server=localhost;Port=5415;Database=TestData;User Id=postgres;Password=Password12!;Pooling=true;MinPoolSize=10;MaxPoolSize=100;", o => o.UseNodaTime());
			optionsBuilder.UseLoggerFactory(TestUtils.LoggerFactory);

			_options = optionsBuilder.Options;
		}

		private NpgSqlEntitiesContext CreateNpgSqlEntitiesContext()
		{
			var ctx = new NpgSqlEntitiesContext(_options);
			ctx.Database.EnsureDeleted();
			ctx.Database.EnsureCreated();
			ctx.Database.ExecuteSqlRaw("create schema \"views\"");
			ctx.Database.ExecuteSqlRaw("create view \"views\".\"EventsView\" as select \"Name\" from \"Events\"");
			return ctx;
		}

		[Test]
		public void TestFunctionsMapping()
		{
			using (var db = CreateNpgSqlEntitiesContext())
			{
				var date = DateTime.Now;

				var query = db.Events.Where(e =>
					e.Duration.Contains(date) || e.Duration.LowerBound == date || e.Duration.UpperBound == date ||
					e.Duration.IsEmpty || e.Duration.Intersect(e.Duration).IsEmpty);

				var efResult = query.ToArray();
				var l2dbResult = query.ToLinqToDB().ToArray();
			}
		}

		[Test]
		public void TestViewMapping()
		{
			using (var db = CreateNpgSqlEntitiesContext())
			{
				var query = db.Set<EventView>().Where(e =>
					e.Name.StartsWith("any"));

				var efResult = query.ToArray();
				var l2dbResult = query.ToLinqToDB().ToArray();
			}
		}

		[Test]
		public void TestArray()
		{
			using (var db = CreateNpgSqlEntitiesContext())
			{
				var guids = new Guid[] { Guid.Parse("271425b1-ebe8-400d-b71d-a6e47a460ae3"),
					Guid.Parse("b75de94e-6d7b-4c70-bfa1-f8639a6a5b35") };

				var query = 
						from m in db.EntityWithArrays.ToLinqToDBTable()
						where Sql.Ext.PostgreSQL().Overlaps(m.Guids, guids)
						select m;

				query.Invoking(q => q.ToArray()).Should().NotThrow();
			}
		}

		[Test]
		public void TestConcurrencyToken()
		{
			using var db = CreateNpgSqlEntitiesContext();

			var toInsert = Enumerable.Range(1, 10)
				.Select(i => new EntityWithXmin { Value = FormattableString.Invariant($"Str{i}") })
				.ToArray();

			db.BulkCopy(toInsert);
		}

		[Test]
		public void TestUnnest()
		{
			using var db = CreateNpgSqlEntitiesContext();
			using var dc = db.CreateLinqToDBConnection();

			var guids = new Guid[] { Guid.Parse("271425b1-ebe8-400d-b71d-a6e47a460ae3"),
				Guid.Parse("b75de94e-6d7b-4c70-bfa1-f8639a6a5b35") };

			var query = 
				from m in db.EntityWithArrays.ToLinqToDBTable()
				from g in dc.Unnest(m.Guids) 
				where Sql.Ext.PostgreSQL().Overlaps(m.Guids, guids)
				select m;

			query.Invoking(q => q.ToArray()).Should().NotThrow();
		}

		[Test]
		public void TestDateTimeKind([Values] DateTimeKind kind)
		{
			using var db = CreateNpgSqlEntitiesContext();
			using var dc = db.CreateLinqToDBConnection();

			var dt  = new DateTime(DateTime.Now.Ticks, kind);
			var dto = DateTimeOffset.Now;
			var ins = Instant.FromDateTimeOffset(dto);
			var ldt = LocalDateTime.FromDateTime(DateTime.Now);

			db.TimeStamps.Where(e => e.Timestamp1 == dt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.Timestamp2 == ldt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ1 == dt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ2 == dto).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ3 == ins).ToLinqToDB().ToArray();
		}

	}
}
