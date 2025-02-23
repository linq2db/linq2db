using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class FluentMappingBuildTests : TestBase
	{
		[Test]
		public void InsertOrUpdatePrimaryKeyTest([DataSources(TestProvName.AllClickHouse, TestProvName.AllOracle)] string context)
		{
			using var db  = GetDataContext(context);

			db.DropTable<int>("FluentTemp", throwExceptionIfNotExists:false);

			using var tmp = db.CreateTempTable(
				"FluentTemp",
				[new { ID = 1, Name = "John" }],
				mb => mb
					.Property(t => t.ID)
						.IsPrimaryKey()
						.HasSkipOnUpdate()
					.Property(t => t.Name)
						.HasLength(20),
				options      : new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow },
				tableOptions : TableOptions.CheckExistence);

			tmp.InsertOrUpdate(
				() => new { ID   = 1, Name = "John II" },
				s  => new { s.ID,   s.Name });
		}

		[Test]
		public void UpdateTest([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateTempTable(
				"FluentTemp",
				[new { ID = 1, Name = "John", LastName = "Doe" }],
				mb => mb
					.Property(t => t.Name)
						.HasLength(20)
						.HasColumnName("Value")
					.Property(t => t.LastName)
						.HasLength(20),
				options      : new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow },
				tableOptions : TableOptions.CheckExistence);

			tmp
				.Where(t => t.ID == 1)
				.Set(t => t.Name,     "John II")
				.Set(t => t.LastName, "Dory")
				.Update();
		}

		[Test]
		public async Task UpdateTestAsync([DataSources] string context)
		{
			await using var db  = GetDataContext(context);
			await using var tmp = await db.CreateTempTableAsync(
				"FluentTemp",
				[new { ID = 1, Name = "John", LastName = "Doe" }],
				mb => mb
					.Property(t => t.Name)
						.HasLength(20)
						.HasColumnName("Value")
					.Property(t => t.LastName)
						.HasLength(20),
				options      : new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow },
				tableOptions : TableOptions.CheckExistence);

			await tmp
				.Where(t => t.ID == 1)
				.Set(t => t.Name,     "John II")
				.Set(t => t.LastName, "Dory")
				.UpdateAsync();
		}

		[Test]
		public async Task MergeTestAsync([DataSources(
			ProviderName.SqlServer2005, ProviderName.SqlCe, TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllMySql, TestProvName.AllSQLite, TestProvName.AllPostgreSQL16Minus)]
			string context)
		{
			await using var db  = GetDataContext(context);

			await db.DropTableAsync<int>("FluentTemp", throwExceptionIfNotExists:false);

			await using var tmp = db.CreateTempTable(
				"FluentTemp",
				[new { ID = 1, Name = "John" }],
				mb => mb
					.Property(t => t.ID)
					.IsPrimaryKey()
					.HasSkipOnUpdate()
					.Property(t => t.Name)
					.HasLength(20),
				options      : new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow },
				tableOptions : TableOptions.CheckExistence);

			await tmp
				.Merge()
				.Using([new { ID = 1, Name = "John II" }])
				.OnTargetKey()
				.UpdateWhenMatched()
				.InsertWhenNotMatched()
				.MergeAsync();
		}

		[Test]
		public void CacheTest([DataSources] string context)
		{
			var count = Test("Value");

			Assert.That(count, Is.EqualTo(Test("Value")));

			var count2 = Test("Column");

			Assert.That(count2, Is.Not.EqualTo(count).And.EqualTo(Test("Column")));

			long Test(string name)
			{
				using var db  = GetDataContext(context);
				using var tmp = db.CreateTempTable(
					"FluentTemp",
					[new { ID = 1, Name = "John", LastName = "Doe" }],
					mb => mb
						.Property(t => t.Name)
						.HasLength(20)
						.HasColumnName(name)
						.Property(t => t.LastName)
						.HasLength(20),
					options      : new BulkCopyOptions { BulkCopyType = BulkCopyType.RowByRow },
					tableOptions : TableOptions.CheckExistence);

				var q = tmp.Where(t => t.ID == 1);
				var l = q.ToList();

				return q.GetCacheMissCount();
			}
		}
	}
}
