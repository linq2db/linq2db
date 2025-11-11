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
	public class DistinctByTests : TestBase
	{
		public class TestData
		{
			public int      Id       { get; set; }
			public string   Name     { get; set; } = null!;
			public string   Group    { get; set; } = null!;
			public DateTime Date     { get; set; }
			public decimal  Amount   { get; set; }
			public bool     IsActive { get; set; }

			public static List<TestData> Seed()
			{
				return 
				[
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 1), Amount  = 100.0m, IsActive = true },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 2), Amount  = 200.0m, IsActive = false },
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 3), Amount  = 150.0m, IsActive = true },
					new TestData { Id = 3, Name = "Charlie", Group = "A", Date = new DateTime(2023, 1, 4), Amount  = 300.0m, IsActive = true },
					new TestData { Id = 4, Name = "David", Group   = "B", Date = new DateTime(2023, 1, 5), Amount  = 400.0m, IsActive = false },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 6), Amount  = 250.0m, IsActive = false },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 7), Amount  = 500.0m, IsActive = true },
					new TestData { Id = 6, Name = "Frank", Group   = "C", Date = new DateTime(2023, 1, 8), Amount  = 600.0m, IsActive = true },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 9), Amount  = 550.0m, IsActive = true },
					new TestData { Id = 7, Name = "Grace", Group   = "D", Date = new DateTime(2023, 1, 10), Amount = 700.0m, IsActive = false }
				];
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase], ErrorMessage = "The LINQ expression could not be converted to SQL.")]
		[Test]
		public void DistinctBy([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(TestData.Seed()))
			{
				var query = table
					.OrderBy(t => t.Name)
					.ThenByDescending(t => t.Date)
					.DistinctBy(x => new { x.Id, x.Name });

				AssertQuery(query);
			}
		}

		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = ErrorHelper.Error_DistinctByRequiresOrderBy)]
		[Test]
		public void DistinctByNoOrder([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(TestData.Seed()))
			{
				var query = table
					.DistinctBy(x => new { x.Id, x.Name });

				AssertQuery(query);
			}
		}

	}
}

#endif
