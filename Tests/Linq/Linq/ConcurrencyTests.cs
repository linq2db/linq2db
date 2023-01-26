﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Concurrency;
using LinqToDB.Mapping;
using NUnit.Framework;

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

		public class CustomConcurrencyPropertyAttribute : OptimisticLockPropertyBaseAttribute
		{
			public override LambdaExpression GetNextValue(ColumnDescriptor column, ParameterExpression record)
			{
				return Expression.Lambda(Expression.Constant(Guid.NewGuid().ToString("N")), record);
			}
		}

		[Test]
		public void TestAutoIncrement([DataSources] string context)
		{
			// lack of rowcount support by clickhouse makes this API useless with ClickHouse
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyAutoIncrement")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.AutoIncrement))
				.Build();

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
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = db.DeleteOptimistic(record);
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

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyAutoIncrement")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.AutoIncrement))
				.Build();

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
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = await db.UpdateOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = await db.DeleteOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = await db.DeleteOptimisticAsync(record);
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
		public void TestFiltered([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyFiltered")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.AutoIncrement))
				.Build();

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
			cnt = t.Where(r => r.Id == 2).UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(0, cnt);
			record.Value = "initial";
			AssertData(record);

			record.Value = "value 2";
			cnt = t.Where(r => r.Id == 1).UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = t.Where(r => r.Id == 2).DeleteOptimistic(record);
			if (!skipCnt) Assert.AreEqual(0, cnt);
			AssertData(record);

			cnt = t.Where(r => r.Id == 1).DeleteOptimistic(record);
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
		public async ValueTask TestFilteredAsync([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<int>>()
					.HasTableName("ConcurrencyFiltered")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.AutoIncrement))
				.Build();

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
			cnt = await t.Where(r => r.Id == 2).UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(0, cnt);
			record.Value = "initial";
			AssertData(record);

			record.Value = "value 2";
			cnt = await t.Where(r => r.Id == 1).UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			record.Stamp++;
			AssertData(record);

			cnt = await t.Where(r => r.Id == 2).DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(0, cnt);
			AssertData(record);

			cnt = await t.Where(r => r.Id == 1).DeleteOptimisticAsync(record);
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
		public void TestGuid([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<Guid>>()
					.HasTableName("ConcurrencyGuid")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();

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
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
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
		public void TestGuidString([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<string>>()
					.HasTableName("ConcurrencyGuidString")
					.Property(e => e.Stamp)
						.HasLength(36)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();

			using var _   = new DisableBaseline("guid used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<string>>();

			var record = new ConcurrencyTable<string>()
			{
				Id    = 1,
				Stamp = "-",
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid().ToString();
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid().ToString();

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
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

		// https://github.com/DarkWanderer/ClickHouse.Client/issues/138
		// https://github.com/ClickHouse/ClickHouse/issues/38790
		[ActiveIssue(Configurations = new[] { ProviderName.ClickHouseClient, ProviderName.ClickHouseMySql })]
		[Test]
		public void TestGuidBinary([DataSources(TestProvName.AllInformix)] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<byte[]>>()
					.HasTableName("ConcurrencyGuidBinary")
					.Property(e => e.Stamp)
						.HasLength(16)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();

			using var _   = new DisableBaseline("guid used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<byte[]>>();

			var record = new ConcurrencyTable<byte[]>()
			{
				Id    = 1,
				Stamp = Guid.NewGuid().ToByteArray(),
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid().ToByteArray();
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid().ToByteArray();

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
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
		public async ValueTask TestTestGuidAsync([DataSources] string context)
		{
			var skipCnt = context.IsAnyOf(TestProvName.AllClickHouse);
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<Guid>>()
					.HasTableName("ConcurrencyGuid")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();

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
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = await db.UpdateOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = await db.DeleteOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
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

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<string>>()
					.HasTableName("ConcurrencyCustom")
					.Property(e => e.Stamp)
						.HasAttribute(new CustomConcurrencyPropertyAttribute())
				.Build();

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
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
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

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<string>>()
					.HasTableName("ConcurrencyCustom")
					.Property(e => e.Stamp)
						.HasAttribute(new CustomConcurrencyPropertyAttribute())
				.Build();

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
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = await db.UpdateOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = await db.DeleteOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
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

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<byte[]>>()
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Auto))
						// don't set skip-on-update to test UpdateOptimistic skips it
						.HasSkipOnInsert()
						.HasDataType(DataType.Timestamp)
				.Build();

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
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = db.UpdateOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = db.DeleteOptimistic(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
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

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<byte[]>>()
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Auto))
						// don't set skip-on-update to test UpdateOptimistic skips it
						.HasSkipOnInsert()
						.HasDataType(DataType.Timestamp)
				.Build();

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
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.AreEqual(1, cnt);
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = await db.UpdateOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = await db.DeleteOptimisticAsync(record);
			Assert.AreEqual(0, cnt);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
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
