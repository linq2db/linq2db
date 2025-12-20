using System;
using System.Globalization;
using System.Linq;

using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.EntityFrameworkCore.Tests.Models.NpgSqlEntities;

using Microsoft.EntityFrameworkCore;

using NodaTime;

using NUnit.Framework;

using Shouldly;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public class NpgSqlTests : ContextTestBase<NpgSqlEntitiesContext>
	{
		protected override NpgSqlEntitiesContext CreateProviderContext(string provider, DbContextOptions<NpgSqlEntitiesContext> options)
		{
			return new NpgSqlEntitiesContext(options);
		}

		protected override void OnDatabaseCreated(string provider, NpgSqlEntitiesContext context)
		{
			context.Database.ExecuteSqlRaw("create schema \"views\"");
			context.Database.ExecuteSqlRaw("create view \"views\".\"EventsView\" as select \"Name\" from \"Events\"");
		}

		[Test]
		public void TestFunctionsMapping([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var date = TestData.DateTime;

			var query = db.Events.Where(e =>
					e.Duration.Contains(date) || e.Duration.LowerBound == date || e.Duration.UpperBound == date ||
					e.Duration.IsEmpty || e.Duration.Intersect(e.Duration).IsEmpty);

			var efResult = query.ToArray();
			var l2dbResult = query.ToLinqToDB().ToArray();
		}

		[Test]
		public void TestViewMapping([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var query = db.Set<EventView>().Where(e => e.Name.StartsWith("any"));

			var efResult = query.ToArray();
			var l2dbResult = query.ToLinqToDB().ToArray();
		}

		[Test]
		public void TestArray([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var guids = new Guid[] { Guid.Parse("271425b1-ebe8-400d-b71d-a6e47a460ae3"),
					Guid.Parse("b75de94e-6d7b-4c70-bfa1-f8639a6a5b35") };

			var query =
						from m in db.EntityWithArrays.ToLinqToDBTable()
						where Sql.Ext.PostgreSQL().Overlaps(m.Guids, guids)
						select m;

			query.ToArray();
		}

		[Test]
		public void TestConcurrencyToken([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var toInsert = Enumerable.Range(1, 10)
				.Select(i => new EntityWithXmin { Value = string.Create(CultureInfo.InvariantCulture, $"Str{i}") })
				.ToArray();

			db.BulkCopy(toInsert);
		}

		[Test]
		public void TestUnnest([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);
			using var dc = db.CreateLinqToDBConnection();

			var guids = new Guid[] { Guid.Parse("271425b1-ebe8-400d-b71d-a6e47a460ae3"),
				Guid.Parse("b75de94e-6d7b-4c70-bfa1-f8639a6a5b35") };

			var query =
				from m in db.EntityWithArrays.ToLinqToDBTable()
				from g in dc.Unnest(m.Guids)
				where Sql.Ext.PostgreSQL().Overlaps(m.Guids, guids)
				select m;

			query.ToArray();
		}

		[Test]
		public void TestDateTimeKind([EFIncludeDataSources(TestProvName.AllPostgreSQL)] string provider, [Values] DateTimeKind kind)
		{
			using var db = CreateContext(provider);
			using var dc = db.CreateLinqToDBConnection();

			var dt  = new DateTime(TestData.DateTime.Ticks, kind);
			var dto = TestData.DateTimeOffset;
			var ins = Instant.FromDateTimeOffset(dto);
			var ldt = LocalDateTime.FromDateTime(TestData.DateTime);

			db.TimeStamps.Where(e => e.Timestamp1 == dt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.Timestamp2 == ldt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ1 == dt).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ2 == dto).ToLinqToDB().ToArray();
			db.TimeStamps.Where(e => e.TimestampTZ3 == ins).ToLinqToDB().ToArray();
		}

	}
}
