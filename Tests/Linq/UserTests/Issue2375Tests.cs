using NUnit.Framework;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2375Tests : TestBase
	{
		public enum InventoryResourceStatus
		{
			Undefined = 0,
			Used = 40,
			Finished = 88
		}

		public enum InfeedStatus
		{
			Undefined = 0,
			Finished = 88
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column(DataType = DataType.Int32)]
			public InventoryResourceStatus Status { get; set; }

			[Column]
			public Guid ResourceID { get; set; }

			[Column]
			public DateTime? ModifiedTimeStamp { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public string? ResourceLabel { get; set; }
		}

		[Test]
		public void Issue2375Test(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (var itb = db.CreateLocalTable<InventoryResourceDTO>())
				using (var lctb = db.CreateLocalTable<WmsLoadCarrierDTO>())
				{
					var res = new WmsLoadCarrierDTO { Id = Guid.NewGuid(), ResourceLabel = "b" };
					db.Insert(res);
					var dto1 = new InventoryResourceDTO
					{
						ResourceID = res.Id,
						Status = InventoryResourceStatus.Used,
						ModifiedTimeStamp = DateTime.UtcNow - TimeSpan.FromHours(2),
						Id = Guid.NewGuid()
					};
					db.Insert(dto1);
					var dto2 = new InventoryResourceDTO
					{
						ResourceID = res.Id,
						Status = InventoryResourceStatus.Used,
						ModifiedTimeStamp = DateTime.UtcNow- TimeSpan.FromHours(2),
						Id = Guid.NewGuid()
					};
					db.Insert(dto2);

					var qry = from inventory in itb
							   join lc in lctb on inventory.ResourceID equals lc.Id
							   group inventory by new
							   {
								   inventory.Status,
								   lc.ResourceLabel
							   }
							into grp
							   where grp.Count() > 1
							   select grp;

					var groups = new List<KeyValuePair<string,IEnumerable<InventoryResourceDTO>>>();

					foreach (var group in qry)
					{
						groups.Add(new KeyValuePair<string, IEnumerable<InventoryResourceDTO>>(group.Key.ResourceLabel, group.OrderBy(x => x.ModifiedTimeStamp).ToList()));
					}

					var sql = ((DataConnection)db).LastQuery;
				}
			}
		}
	}
}
