using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.DataProvider
{
	[TestFixture]
	public class PostgreSQLArrayTests : TestBase
	{
		[Table]
		sealed class AllTypesValueTable
		{
			[Column] public int      Id           { get; set; }
			[Column] public int      IntValue     { get; set; }
			[Column] public long     LongValue    { get; set; }
			[Column] public double   DoubleValue  { get; set; }
			[Column] public decimal  DecimalValue { get; set; }
			[Column] public string   StrValue     { get; set; } = null!;
			[Column] public bool     BoolValue    { get; set; }
			[Column] public short    ShortValue   { get; set; }
			[Column] public float    FloatValue   { get; set; }
			[Column] public Guid     GuidValue    { get; set; }
			[Column] public DateTime DateValue    { get; set; }

			public static AllTypesValueTable[] Seed()
			{
				return
				[
					new() { Id = 1, IntValue = 10, LongValue = 100, DoubleValue = 1.1, DecimalValue = 1.1m, StrValue = "A", BoolValue = true,  ShortValue = 1, FloatValue = 1.1f, GuidValue = new Guid("00000001-0000-0000-0000-000000000000"), DateValue = new DateTime(2024, 1, 1) },
					new() { Id = 2, IntValue = 20, LongValue = 200, DoubleValue = 2.2, DecimalValue = 2.2m, StrValue = "B", BoolValue = false, ShortValue = 2, FloatValue = 2.2f, GuidValue = new Guid("00000002-0000-0000-0000-000000000000"), DateValue = new DateTime(2024, 2, 1) },
					new() { Id = 3, IntValue = 30, LongValue = 300, DoubleValue = 3.3, DecimalValue = 3.3m, StrValue = "C", BoolValue = true,  ShortValue = 3, FloatValue = 3.3f, GuidValue = new Guid("00000003-0000-0000-0000-000000000000"), DateValue = new DateTime(2024, 3, 1) },
				];
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Int([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { 10, 20 } : new[] { 30 };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.IntValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Long([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { 100L, 200L } : new[] { 300L };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.LongValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Short([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new short[] { 1, 2 } : new short[] { 3 };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.ShortValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_String([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { "A", "B" } : new[] { "C" };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.StrValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Double([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { 1.1, 2.2 } : new[] { 3.3 };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.DoubleValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Decimal([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { 1.1m, 2.2m } : new[] { 3.3m };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.DecimalValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Float([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { 1.1f, 2.2f } : new[] { 3.3f };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.FloatValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Guid([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1
				? new[] { new Guid("00000001-0000-0000-0000-000000000000"), new Guid("00000002-0000-0000-0000-000000000000") }
				: new[] { new Guid("00000003-0000-0000-0000-000000000000") };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.GuidValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_DateTime([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1
				? new[] { new DateTime(2024, 1, 1), new DateTime(2024, 2, 1) }
				: new[] { new DateTime(2024, 3, 1) };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.DateValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}

		[Test]
		public void ArrayParameterCacheTest_Bool([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(AllTypesValueTable.Seed());

			var arr = iteration == 1 ? new[] { true } : new[] { false };

			var query  = table.Where(t => Sql.Ext.PostgreSQL().ValueIsEqualToAny(t.BoolValue, arr));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			if (iteration == 1)
				result.Length.ShouldBe(2);
			else
			{
				query.GetCacheMissCount().ShouldBe(miss);
				result.Length.ShouldBe(1);
			}
		}
	}
}
