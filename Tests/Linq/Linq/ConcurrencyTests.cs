using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Concurrency;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp++;
			AssertData(record);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				}
			}
		}

		[Test]
		public async ValueTask TestAutoIncrementAsync([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			record.Stamp--;
			record.Value = "value 3";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp++;
			record.Value = "value 2";
			AssertData(record);
			record.Stamp--;

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp++;
			AssertData(record);

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				}
			}
		}

		[Test]
		public void TestFiltered([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = t.Where(r => r.Id == 2).UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Value = "initial";
			AssertData(record);

			record.Value = "value 2";
			cnt = t.Where(r => r.Id == 1).UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			cnt = t.Where(r => r.Id == 2).DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			AssertData(record);

			cnt = t.Where(r => r.Id == 1).DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				}
			}
		}

		[Test]
		public async ValueTask TestFilteredAsync([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = await t.Where(r => r.Id == 2).UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Value = "initial";
			AssertData(record);

			record.Value = "value 2";
			cnt = await t.Where(r => r.Id == 1).UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			record.Stamp++;
			AssertData(record);

			cnt = await t.Where(r => r.Id == 2).DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			AssertData(record);

			cnt = await t.Where(r => r.Id == 1).DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<int> record)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				}
			}
		}

		[Test]
		public void TestGuid([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<Guid> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestGuidString([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid().ToString();
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid().ToString();

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<string> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		// https://github.com/ClickHouse/ClickHouse/issues/38790
		[ActiveIssue(Configurations = new[] { ProviderName.ClickHouseMySql })]
		[Test]
		public void TestGuidBinary([DataSources(TestProvName.AllInformix)] string context)
		{
			var skipCnt = !context.SupportsRowcount();
			var ms      = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ConcurrencyTable<byte[]>>()
					.HasTableName("ConcurrencyGuidBinary")
					.Property(e => e.Stamp)
						.HasDataType(DataType.Binary)
						.HasLength(16)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();

			using var _   = new DisableBaseline("guid used");
			using var db  = GetDataContext(context, ms);
			using var t   = db.CreateLocalTable<ConcurrencyTable<byte[]>>();

			var record = new ConcurrencyTable<byte[]>()
			{
				Id    = 1,
				Stamp = TestData.Guid1.ToByteArray(),
				Value = "initial"
			};

			var cnt = db.Insert(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = TestData.Guid2.ToByteArray();
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = TestData.Guid3.ToByteArray();

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestTestGuidAsync([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = Guid.NewGuid();
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = Guid.NewGuid();

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<Guid> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestCustomStrategy([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<string> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestCustomStrategyAsync([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record, true);

			record.Value = "value 1";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp;
			record.Value = "value 3";
			record.Stamp = "unknown-value";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp = "unknown-value";

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<string> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestDbStrategy([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = db.UpdateOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = db.DeleteOptimistic(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public async ValueTask TestDbStrategyAsync([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = await db.UpdateOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = await db.DeleteOptimisticAsync(record);
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		[Test]
		public void TestFilterExtension([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = !context.SupportsRowcount();
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
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 1";
			cnt = t.WhereKeyOptimistic(record).Update(r => new ConcurrencyTable<byte[]>() { Value = record.Value });
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			record.Value = "value 2";
			cnt = t.WhereKeyOptimistic(record).Update(r => new ConcurrencyTable<byte[]>() { Value = record.Value });
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			AssertData(record);

			var dbStamp = record.Stamp.ToArray();
			record.Value = "value 3";
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);
			cnt = t.WhereKeyOptimistic(record).Update(r => new ConcurrencyTable<byte[]>() { Value = record.Value });
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			record.Value = "value 2";
			AssertData(record, true);
			record.Stamp[0] = (byte)(record.Stamp[0] + 1);

			cnt = t.WhereKeyOptimistic(record).Delete();
			if (!skipCnt) Assert.That(cnt, Is.Zero);
			record.Stamp = dbStamp;
			AssertData(record, true);

			cnt = t.WhereKeyOptimistic(record).Delete();
			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(t.ToArray(), Is.Empty);

			void AssertData(ConcurrencyTable<byte[]> record, bool equals = false)
			{
				var data = t.ToArray();

				Assert.That(data, Has.Length.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(record.Id));
					Assert.That(data[0].Value, Is.EqualTo(record.Value));
				}

				if (equals)
					Assert.That(data[0].Stamp, Is.EqualTo(record.Stamp));
				else
					Assert.That(data[0].Stamp, Is.Not.EqualTo(record.Stamp));

				record.Stamp = data[0].Stamp;
			}
		}

		// Regression for the shared-statement alias race surfaced by concurrent query execution:
		// a parameter-dependent query is cached and re-aliased on every execution. The aliasing pass
		// used to MUTATE the shared statement in place, so concurrent executions corrupted each other's
		// aliases - yielding a wrong column->value mapping or invalid SQL. Aliasing is now non-mutating
		// (finalized names live in AliasesContext), so the same cached query rendered from many threads
		// stays correct. Light load on purpose: a sometimes-red CI signal, not a 100% reproducer.
		sealed class AliasRaceRow
		{
			public int V00; public int V01; public int V02; public int V03;
			public int V04; public int V05; public int V06; public int V07;
			public int V08; public int V09; public int V10; public int V11;
			public int V12; public int V13; public int V14; public int V15;
		}

		[Test]
		public void ParallelExecutionDoesNotCorruptAliases([IncludeDataSources(false, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			using var _ = new DisableBaseline("raw SQL + concurrency stress");

			var fields = typeof(AliasRaceRow).GetFields();

			// SELECT {0} + 0 as "V00", {0} + 1 as "V01", ... - a distinct value per (quoted) column,
			// so a raced alias surfaces either as invalid SQL or as a wrong column -> value mapping.
			var sql = "SELECT " + string.Join(", ", fields.Select(f =>
			{
				var n = int.Parse(f.Name.Substring(1), CultureInfo.InvariantCulture);
				return FormattableString.Invariant($"{{0}} + {n} as \"{f.Name}\"");
			}));

			static DataParameter NewParam() => new() { Name = "p", Value = 0, DataType = DataType.Int32 };

			// prime once so subsequent executions hit the cached (re-alias) path
			using (var prime = GetDataContext(context))
				prime.FromSql<AliasRaceRow>(sql, NewParam()).Single();

			Exception? error = null;
			var        bad   = 0;

			void Worker()
			{
				for (var i = 0; i < 64 && Volatile.Read(ref error) == null && Volatile.Read(ref bad) == 0; i++)
				{
					try
					{
						using var db  = GetDataContext(context);
						var       row = db.FromSql<AliasRaceRow>(sql, NewParam()).Single();

						foreach (var f in fields)
						{
							var expected = int.Parse(f.Name.Substring(1), CultureInfo.InvariantCulture);
							if ((int)f.GetValue(row)! != expected)
							{
								Interlocked.Increment(ref bad);
								break;
							}
						}
					}
					catch (Exception ex)
					{
						Interlocked.CompareExchange(ref error, ex, null);
					}
				}
			}

			var threads = Enumerable.Range(0, 4).Select(_ => new Thread(Worker)).ToArray();
			foreach (var t in threads) t.Start();
			foreach (var t in threads) t.Join();

			error.ShouldBeNull($"concurrent execution threw: {error}");
			bad.ShouldBe(0, "a concurrent execution returned a corrupted alias mapping (wrong column value)");
		}
	}
}
