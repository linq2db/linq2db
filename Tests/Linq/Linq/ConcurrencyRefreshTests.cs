using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Concurrency;
using LinqToDB.Internal.Concurrency;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConcurrencyRefreshTests : TestBase
	{
		// table name overriden for each test to workaround
		// https://github.com/linq2db/linq2db/issues/3894
		public class RefreshTable<TStamp>
			where TStamp : notnull
		{
			[PrimaryKey] public int     Id    { get; set; }
			[Column]     public TStamp  Stamp { get; set; } = default!;
			[Column]     public string? Value { get; set; }
		}

		private static MappingSchema GuidSchema(string tableName)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<RefreshTable<Guid>>()
					.HasTableName(tableName)
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Guid))
				.Build();
			return ms;
		}

		[Test]
		public void UpdateRefreshesVersion([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();

			using var _  = new DisableBaseline("guid used");
			using var db = GetDataContext(context, GuidSchema("ConcurrencyRefreshGuid"));
			using var t  = db.CreateLocalTable<RefreshTable<Guid>>();

			var record = new RefreshTable<Guid> { Id = 1, Stamp = default, Value = "initial" };
			db.Insert(record);

			// after insert the in-memory stamp is still default; sync it from the database
			record.Stamp = t.Single().Stamp;
			var before   = record.Stamp;

			record.Value = "updated";
			var cnt = db.UpdateOptimisticWithRefresh(record);

			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));

			// the regenerated version is written back onto the entity (issue #4194) ...
			Assert.That(record.Stamp, Is.Not.EqualTo(before));
			// ... and matches what's actually stored
			Assert.That(record.Stamp, Is.EqualTo(t.Single().Stamp));
		}

		[Test]
		public async Task UpdateRefreshesVersionAsync([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();

			using var _  = new DisableBaseline("guid used");
			using var db = GetDataContext(context, GuidSchema("ConcurrencyRefreshGuid"));
			using var t  = db.CreateLocalTable<RefreshTable<Guid>>();

			var record = new RefreshTable<Guid> { Id = 1, Stamp = default, Value = "initial" };
			await db.InsertAsync(record);

			record.Stamp = t.Single().Stamp;
			var before   = record.Stamp;

			record.Value = "updated";
			var cnt = await db.UpdateOptimisticWithRefreshAsync(record);

			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));

			Assert.That(record.Stamp, Is.Not.EqualTo(before));
			Assert.That(record.Stamp, Is.EqualTo(t.Single().Stamp));
		}

		[Test]
		public void UpdateViaQueryRefreshesVersion([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();

			using var _  = new DisableBaseline("guid used");
			using var db = GetDataContext(context, GuidSchema("ConcurrencyRefreshGuid"));
			using var t  = db.CreateLocalTable<RefreshTable<Guid>>();

			var record = new RefreshTable<Guid> { Id = 1, Stamp = default, Value = "initial" };
			db.Insert(record);
			record.Stamp = t.Single().Stamp;
			var before   = record.Stamp;

			// IQueryable receiver: an extra filter ANDs with the optimistic predicate
			record.Value = "updated";
			var cnt = t.Where(r => r.Id == 1).UpdateOptimisticWithRefresh(record);

			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));
			Assert.That(record.Stamp, Is.Not.EqualTo(before));
			Assert.That(record.Stamp, Is.EqualTo(t.Single().Stamp));
		}

		[Test]
		public void UpdateRefreshesAutoIncrement([DataSources] string context)
		{
			var skipCnt = !context.SupportsRowcount();
			var ms      = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<RefreshTable<int>>()
					.HasTableName("ConcurrencyRefreshAutoInc")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.AutoIncrement))
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable<RefreshTable<int>>();

			var record = new RefreshTable<int> { Id = 1, Stamp = 5, Value = "initial" };
			db.Insert(record);

			record.Value = "updated";
			var cnt = db.UpdateOptimisticWithRefresh(record);

			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));

			Assert.That(record.Stamp, Is.EqualTo(6));
			Assert.That(record.Stamp, Is.EqualTo(t.Single().Stamp));
		}

		[Test]
		public void UpdateRefreshesDatabaseGenerated([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			var skipCnt = !context.SupportsRowcount();
			var ms      = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<RefreshTable<byte[]>>()
					.HasTableName("ConcurrencyRefreshRowVersion")
					.Property(e => e.Stamp)
						.HasAttribute(new OptimisticLockPropertyAttribute(VersionBehavior.Auto))
						.HasSkipOnInsert()
						.HasDataType(DataType.Timestamp)
				.Build();

			using var _  = new DisableBaseline("timestamp used");
			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable<RefreshTable<byte[]>>();

			var record = new RefreshTable<byte[]> { Id = 1, Value = "initial" };
			db.Insert(record);
			record.Stamp = t.Single().Stamp;
			var before   = record.Stamp;

			record.Value = "updated";
			var cnt = db.UpdateOptimisticWithRefresh(record);

			if (!skipCnt) Assert.That(cnt, Is.EqualTo(1));

			// rowversion is purely database-generated and only obtainable via OUTPUT / SELECT
			Assert.That(record.Stamp, Is.Not.EqualTo(before));
			Assert.That(record.Stamp, Is.EqualTo(t.Single().Stamp));
		}

		[Test]
		public void UpdateConcurrencyFailureLeavesEntityUnchanged([DataSources] string context)
		{
			if (!context.SupportsRowcount())
				Assert.Ignore("Affected-records count required to detect concurrency failure.");

			using var _  = new DisableBaseline("guid used");
			using var db = GetDataContext(context, GuidSchema("ConcurrencyRefreshGuid"));
			using var t  = db.CreateLocalTable<RefreshTable<Guid>>();

			var record = new RefreshTable<Guid> { Id = 1, Stamp = default, Value = "initial" };
			db.Insert(record);

			// stale stamp -> no row matches the optimistic filter
			record.Stamp = TestData.Guid1;
			var stale    = record.Stamp;

			record.Value = "updated";
			var cnt = db.UpdateOptimisticWithRefresh(record);

			Assert.That(cnt, Is.Zero);
			Assert.That(record.Stamp, Is.EqualTo(stale), "entity must not be touched on concurrency failure");
		}

		// Guard test: keep ConcurrencyOutputSupport's declared provider set honest. It probes the provider's actual
		// UPDATE OUTPUT support and fails when reality diverges from the declaration, signalling that the support set
		// (and any required handling in ConcurrencyExtensions) needs updating.
		[Test]
		public void OutputSupportSurface([DataSources] string context)
		{
			using var _  = new DisableBaseline("probes provider capability");
			using var db = GetDataContext(context);

			var actual = ProbeUpdateOutput(db);

			Assert.That(
				actual,
				Is.EqualTo(ConcurrencyOutputSupport.IsUpdateOutputSupported(db.ContextName)),
				$"UPDATE OUTPUT support for '{db.ContextName}' diverged from ConcurrencyOutputSupport.IsUpdateOutputSupported; update the declared set and any handling in ConcurrencyExtensions.");
		}

		private static bool ProbeUpdateOutput(IDataContext db)
		{
			try
			{
				using var t = db.CreateLocalTable<RefreshTable<int>>("ConcurrencyRefreshProbe");
				db.Insert(new RefreshTable<int> { Id = 1, Stamp = 1, Value = "x" });
				_ = t.Where(r => r.Id == 1).UpdateWithOutput(r => new RefreshTable<int> { Stamp = 2 }, (deleted, inserted) => inserted.Stamp).ToList();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
