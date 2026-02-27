using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5379Tests : TestBase
	{
		[Table]
		public class InventoryResourceDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column] public Guid ResourceID { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
		}

		[Test]
		public void QueryWithUnionAllDoesNotWork([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			using var aisle = db.CreateLocalTable<InventoryResourceDTO>([new InventoryResourceDTO() { Id = TestData.Guid1, ResourceID = TestData.Guid2 }]);
			using var refTable = db.CreateLocalTable<WmsLoadCarrierDTO>([new WmsLoadCarrierDTO() { Id =  TestData.Guid2 }]);

			var inventoryQry = db.GetTable<InventoryResourceDTO>().AsQueryable();
			var lcQry = db.GetTable<WmsLoadCarrierDTO>();

			var qryAssignment =
				from r in lcQry
				select new InventoryResourceDTO()
				{
					Id = Guid.Empty,
					ResourceID = r.Id
				};

			inventoryQry = inventoryQry.UnionAll(qryAssignment);

			var qry =
				from r in lcQry
				join inventory in inventoryQry on r.Id equals inventory.ResourceID into invlist
				from inventory in invlist.DefaultIfEmpty()
				select inventory;

			var _ = qry.ToList();
		}
	}
}
