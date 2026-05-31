#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class DistinctByMethodTests : TestBase
	{
		public class TestData
		{
			public int      Id       { get; set; }
			public string   Name     { get; set; } = null!;
			public string   Group    { get; set; } = null!;
			public DateTime Date     { get; set; }
			public decimal  Amount   { get; set; }
			public bool     IsActive { get; set; }
			public int?     Priority { get; set; }

			public static List<TestData> Seed()
			{
				return
				[
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 1), Amount  = 100.0m, IsActive = true,  Priority = 5    },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 2), Amount  = 200.0m, IsActive = false, Priority = null },
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 3), Amount  = 150.0m, IsActive = true,  Priority = null },
					new TestData { Id = 3, Name = "Charlie", Group = "A", Date = new DateTime(2023, 1, 4), Amount  = 300.0m, IsActive = true,  Priority = 3    },
					new TestData { Id = 4, Name = "David", Group   = "B", Date = new DateTime(2023, 1, 5), Amount  = 400.0m, IsActive = false, Priority = 1    },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 6), Amount  = 250.0m, IsActive = false, Priority = 2    },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 7), Amount  = 500.0m, IsActive = true,  Priority = null },
					new TestData { Id = 6, Name = "Frank", Group   = "C", Date = new DateTime(2023, 1, 8), Amount  = 600.0m, IsActive = true,  Priority = 4    },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 9), Amount  = 550.0m, IsActive = true,  Priority = 6    },
					new TestData { Id = 7, Name = "Grace", Group   = "D", Date = new DateTime(2023, 1, 10), Amount = 700.0m, IsActive = false, Priority = null }
				];
			}
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.OrderBy(t => t.Name)
				.ThenByDescending(t => t.Date)
				.DistinctBy(x => new { x.Id, x.Name });

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByOrderByNulls(
			[DataSources] string context,
			[Values(Sql.NullsPosition.First, Sql.NullsPosition.Last)] Sql.NullsPosition nulls,
			[Values] bool descending)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// DistinctBy lowers the preceding OrderBy into ROW_NUMBER(); the NULLS position must reach the OVER
			// clause and select which row survives per group.
			var ordered = descending
				? table.OrderByDescending(t => t.Priority, nulls)
				: table.OrderBy          (t => t.Priority, nulls);

			var query = ordered
				.ThenBy(t => t.Id)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByDefaultNullsPosition([DataSources] string context)
		{
			// The preceding plain OrderBy is extracted for the ROW_NUMBER rewrite and bypasses OrderByBuilder,
			// so the configured default NULLS position must still be applied — same survivor per group as the
			// explicit Sql.NullsPosition.Last overload.
			using var db    = GetDataContext(context, o => o.UseDefaultNullsPosition(Sql.NullsPosition.Last));
			using var table = db.CreateLocalTable(TestData.Seed());

			var byDefault = table
				.OrderBy(t => t.Priority).ThenBy(t => t.Id)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Group).Select(x => x.Id).ToList();

			var byExplicit = table
				.OrderBy(t => t.Priority, Sql.NullsPosition.Last).ThenBy(t => t.Id)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Group).Select(x => x.Id).ToList();

			Assert.That(byDefault, Is.EqualTo(byExplicit));
		}

		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = ErrorHelper.Error_DistinctByRequiresOrderBy)]
		[Test]
		public void DistinctByNoOrder([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.DistinctBy(x => new { x.Id, x.Name });

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted]
		public void DistinctByWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var comparer = EqualityComparer<int>.Default;

			_ = table
				.OrderBy(t => t.Name)
				.DistinctBy(x => x.Id, comparer)
				.ToList();
		}
	}
}

#endif
