using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// Performance regression with query compilation — array parameters in Sql.Extension cause cache misses.
	/// https://github.com/linq2db/linq2db/issues/5302
	/// </summary>
	[TestFixture]
	public class Issue5302Tests : TestBase
	{
		[Table]
		public class Warehouse
		{
			[Column, PrimaryKey] public int     Id                   { get; set; }
			[Column            ] public int     ClearingWarehouseId  { get; set; }
			[Column            ] public bool    IsActive             { get; set; }
		}

		/// <summary>
		/// Reproduces the issue: query using ValueIsEqualToAny with array causes cache miss on every execution.
		/// </summary>
		[Test]
		public void ValueIsEqualToAnyCacheTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values(1, 2)] int iteration)
		{
			var testData = new[]
			{
				new Warehouse { Id = 1, ClearingWarehouseId = 10, IsActive = true  },
				new Warehouse { Id = 2, ClearingWarehouseId = 20, IsActive = true  },
				new Warehouse { Id = 3, ClearingWarehouseId = 30, IsActive = false },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var isActive     = iteration == 1;
			var warehouseIds = iteration == 1 ? new[] { 10, 20 } : new[] { 30 };

			var query = table
				.Where(w => w.IsActive == isActive)
				.Where(w => Sql.Ext.PostgreSQL().ValueIsEqualToAny(w.ClearingWarehouseId, warehouseIds));

			var cacheMiss = query.GetCacheMissCount();
			var result    = query.ToArray();

			if (iteration == 1)
			{
				result.Length.ShouldBe(2);
				result.Select(r => r.Id).OrderBy(id => id).ToArray().ShouldBe(new[] { 1, 2 });
			}
			else
			{
				query.GetCacheMissCount().ShouldBe(cacheMiss);
				result.Length.ShouldBe(1);
				result[0].Id.ShouldBe(3);
			}
		}
	}
}
