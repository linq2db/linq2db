using FluentAssertions;
using LinqToDB;
using LinqToDB.Tools;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB.Expressions;

namespace Tests.Linq
{
	[TestFixture]
	public class ConcurrencyTests : TestBase
	{
		// table name overriden for each test to workaround
		// https://github.com/linq2db/linq2db/issues/3894
		public class ConcurrencyTable<TStamp>
			where TStamp: notnull
		{
			[PrimaryKey] public int Id    { get; set; }

			[Column] public TStamp Stamp  { get; set; } = default!;

			[Column] public string? Value { get; set; }
		}

		public class CustomConcurrencyPropertyAttribute : ConcurrencyPropertyAttribute
		{
			public CustomConcurrencyPropertyAttribute()
				: base(default)
			{
			}

			public override LambdaExpression GetNextValue(ColumnDescriptor column, ParameterExpression record)
			{
				return Expression.Lambda(
					Expression.Constant(Guid.NewGuid().ToString("N")),
					record);
			}
		}

		[Test]
		public void TestAutoIncrement([DataSources] string context)
		{
			// lack of rowcount support by clickhouse makes this API useless with ClickHouse
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyAutoIncrement")
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.AutoIncrement));

			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<int>>();

			var record = new ConcurrencyTable<int>()
			{
				Id    = 1,
				Stamp = -10,
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 1";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = db.UpdateConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = db.DeleteConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = db.DeleteConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);
				Assert.AreEqual(record.Stamp, data[0].Stamp);
			}
		}

		[Test]
		public async ValueTask TestAutoIncrementAsync([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyAutoIncrement")
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.AutoIncrement));

			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<int>>();

			var record = new ConcurrencyTable<int>()
			{
				Id    = 1,
				Stamp = -10,
				Value = "initial"
			};

			var cnt = await db.InsertAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 1";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = await db.UpdateConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = await db.DeleteConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = await db.DeleteConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);
				Assert.AreEqual(record.Stamp, data[0].Stamp);
			}
		}

		[Test]
		public async ValueTask TestGuid([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<Guid>>()
					.HasTableName("ConcurrencyGuid")
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.Guid));

			using var _   = new DisableBaseline("guid used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<Guid>>();

			var record = new ConcurrencyTable<Guid>()
			{
				Id    = 1,
				Stamp = default,
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);
			// still believe timestamp is good as unique value?
			await Task.Delay(TimeSpan.FromSeconds(1));

			record.Value = "value 2";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);
			await Task.Delay(TimeSpan.FromSeconds(1));

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = db.UpdateConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = db.DeleteConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<Guid> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestTestGuidAsync([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<Guid>>()
					.HasTableName("ConcurrencyGuid")
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.Guid));

			using var _   = new DisableBaseline("guid used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<Guid>>();

			var record = new ConcurrencyTable<Guid>()
			{
				Id    = 1,
				Stamp = default,
				Value = "initial"
			};

			var cnt = await db.InsertAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);
			// still believe timestamp is good as unique value?
			await Task.Delay(TimeSpan.FromSeconds(1));

			record.Value = "value 2";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);
			await Task.Delay(TimeSpan.FromSeconds(1));

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = await db.UpdateConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = await db.DeleteConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<Guid> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestCustomStrategy([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<string>>()
					.HasTableName("ConcurrencyCustom")
					.Property(e => e.Stamp)
						.HasAttribute(new CustomConcurrencyPropertyAttribute());

			using var _   = new DisableBaseline("random data used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<string>>();

			var record = new ConcurrencyTable<string>()
			{
				Id    = 1,
				Stamp = "hello",
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = db.UpdateConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = db.DeleteConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<string> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestCustomStrategyAsync([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<string>>()
					.HasTableName("ConcurrencyCustom")
					.Property(e => e.Stamp)
						.HasAttribute(new CustomConcurrencyPropertyAttribute());

			using var _   = new DisableBaseline("random data used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<string>>();

			var record = new ConcurrencyTable<string>()
			{
				Id    = 1,
				Stamp = "hello",
				Value = "initial"
			};

			var cnt = await db.InsertAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = await db.UpdateConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = await db.DeleteConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<string> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestDbStrategy([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<byte[]>>()
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.Auto))
						// don't set skip-on-update to test UpdateConcurrent skips it
						.HasSkipOnInsert()
						.HasDataType(DataType.Timestamp);

			using var _   = new DisableBaseline("timestamp used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<byte[]>>();

			var record = new ConcurrencyTable<byte[]>()
			{
				Id    = 1,
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 1";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = db.UpdateConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = db.DeleteConcurrent(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteConcurrent(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestDbStrategyAsync([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<ConcurrencyTable<byte[]>>()
					.Property(e => e.Stamp)
						.HasAttribute(new ConcurrencyPropertyAttribute(VersionBehavior.Auto))
						// don't set skip-on-update to test UpdateConcurrent skips it
						.HasSkipOnInsert()
						.HasDataType(DataType.Timestamp);

			using var _   = new DisableBaseline("timestamp used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<byte[]>>();

			var record = new ConcurrencyTable<byte[]>()
			{
				Id    = 1,
				Value = "initial"
			};

			var cnt = await db.InsertAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 1";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = await db.UpdateConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = await db.DeleteConcurrentAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteConcurrentAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			Assert.AreEqual(0, t.ToArray().Length);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.AreEqual(1, data.Length);
				Assert.AreEqual(record.Id, data[0].Id);
				Assert.AreEqual(record.Value, data[0].Value);

				if (equals)
					Assert.AreEqual(record.Stamp, data[0].Stamp);
				else
					Assert.AreNotEqual(record.Stamp, data[0].Stamp);

				record.Stamp = data[0].Stamp;
			}
		}
	}
}
