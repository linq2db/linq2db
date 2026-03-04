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
		public class InventoryResourceDTO
		{
			[PrimaryKey]
			public Guid Id { get; set; }
			public Guid ResourceID { get; set; }
		}

		public class WmsLoadCarrierDTO
		{
			[PrimaryKey]
			public Guid Id { get; set; }
		}

		[Test]
		public void GuidLiteralTyping([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			using var aisle = db.CreateLocalTable<InventoryResourceDTO>([new InventoryResourceDTO() { Id = TestData.Guid1, ResourceID = TestData.Guid2 }]);
			using var refTable = db.CreateLocalTable<WmsLoadCarrierDTO>([new WmsLoadCarrierDTO() { Id = TestData.Guid2 }]);

			var qryAssignment =
				from r in refTable
				select new InventoryResourceDTO()
				{
					Id = Guid.Empty,
					ResourceID = r.Id
				};

			var inventoryQry = aisle.UnionAll(qryAssignment);

			var qry =
				from r in refTable
				join inventory in inventoryQry on r.Id equals inventory.ResourceID into invlist
				from inventory in invlist.DefaultIfEmpty()
				select inventory;

			var _ = qry.ToList();
		}
	}
}
