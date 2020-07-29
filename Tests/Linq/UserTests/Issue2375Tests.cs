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

		public class InventoryResourceDTO
		{
			public Guid Id { get; set; }

			[Column(DataType = DataType.Int32)]
			public InventoryResourceStatus Status { get; set; }

			public Guid ResourceID { get; set; }

			public Guid MaterialID { get; set; }

			public string? Unit { get; set; }

			public Guid? InfeedAdviceID { get; set; }

			public DateTime? ModifiedTimeStamp { get; set; }

			public DateTime? ProductionDate { get; set; }

			public long CustomLong { get; set; }

			public DateTime? ExpiryDate { get; set; }

			public string? BatchNumber { get; set; }

			public decimal? QuantityPerBundle { get; set; }

			public string? BundleUnit { get; set; }

			public int ProductStatus { get; set; }

			public string? ReasonForRestriction { get; set; }

			public string? RestrictionLabel { get; set; }

			public string? RestrictionText { get; set; }

			public int ToHostStatus { get; set; }

			public string? CustomField1 { get; set; }
			public string? CustomField2 { get; set; }
			public string? CustomField3 { get; set; }
			public string? CustomField4 { get; set; }
			public string? CustomField5 { get; set; }
			public string? CustomField6 { get; set; }
			public string? CustomField7 { get; set; }
			public string? CustomField8 { get; set; }
			public string? CustomField9 { get; set; }
			public DateTime? CustomDate1 { get; set; }
			public DateTime? CustomDate2 { get; set; }
			public DateTime? CustomDate3 { get; set; }
			public DateTime? CustomDate4 { get; set; }
			public DateTime? CustomDate5 { get; set; }
			public DateTime? CustomDate6 { get; set; }
			public DateTime? CustomDate7 { get; set; }
			public DateTime? CustomDate8 { get; set; }
			public DateTime? CustomDate9 { get; set; }
			public long? CustomLong1 { get; set; }
			public long? CustomLong2 { get; set; }
			public long? CustomLong3 { get; set; }
			public long? CustomLong4 { get; set; }
			public long? CustomLong5 { get; set; }
			public long? CustomLong6 { get; set; }
			public long? CustomLong7 { get; set; }
			public long? CustomLong8 { get; set; }
			public long? CustomLong9 { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public string? ResourceLabel { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Table]
		public class InfeedAdviceDTO
		{
			public Guid Id { get; set; }

			[Column(DataType = DataType.Int32)]
			public InfeedStatus Status { get; set; }

			public DateTime? ModifiedTimeStamp { get; set; }
		}

		[Table]
		public class MaterialDTO
		{
			[Column]
			public Guid Id { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Test]
		public void Issue2375Test(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (var itb = db.CreateLocalTable<InventoryResourceDTO>())
				using (var lctb = db.CreateLocalTable<WmsLoadCarrierDTO>())
				using (var intb = db.CreateLocalTable<InfeedAdviceDTO>())
				using (var mtb = db.CreateLocalTable<MaterialDTO>())
				{
					var res = new WmsLoadCarrierDTO { Id = Guid.NewGuid(), Name = "a" };
					lctb.Insert(() => res);
					var dto1 = new InventoryResourceDTO
					{
						Status = InventoryResourceStatus.Used,
						MaterialID = Guid.NewGuid(),
						ResourceID = res.Id,
						ModifiedTimeStamp = DateTime.UtcNow,
						Id = Guid.NewGuid(),
						InfeedAdviceID = Guid.NewGuid()
					};
					itb.Insert(() => dto1);
					var dto2 = new InventoryResourceDTO
					{
						Status = InventoryResourceStatus.Used,
						MaterialID = Guid.NewGuid(),
						ResourceID = res.Id,
						ModifiedTimeStamp = DateTime.UtcNow,
						Id = Guid.NewGuid(),
						InfeedAdviceID = Guid.NewGuid()
					};
					itb.Insert(() => dto2);

					var qry = from inventory in itb
							   join lc in lctb on inventory.ResourceID equals lc.Id
							   join material in mtb on inventory.MaterialID equals material.Id
							   join infeeds in intb on inventory.InfeedAdviceID equals infeeds.Id into infeedList
							   from infeed in infeedList.DefaultIfEmpty()
							   where inventory.Status < InventoryResourceStatus.Finished
										  && inventory.ModifiedTimeStamp < DateTime.UtcNow - TimeSpan.FromHours(1)
										  && inventory.ProductionDate == null
										  && inventory.Unit != "m" && inventory.Unit != "M"
										  && (infeed == null || (infeed.Status >= InfeedStatus.Undefined && infeed.ModifiedTimeStamp < DateTime.UtcNow - TimeSpan.FromHours(1)))
							   group inventory by new
							   {
								   inventory.Status,
								   inventory.MaterialID,
								   inventory.ResourceID,
								   inventory.ExpiryDate,
								   inventory.BatchNumber,
								   inventory.Unit,
								   inventory.QuantityPerBundle,
								   inventory.BundleUnit,
								   inventory.ProductStatus,
								   inventory.ReasonForRestriction,
								   inventory.RestrictionLabel,
								   inventory.RestrictionText,
								   inventory.ToHostStatus,
								   inventory.CustomField1,
								   inventory.CustomField2,
								   inventory.CustomField3,
								   inventory.CustomField4,
								   inventory.CustomField5,
								   inventory.CustomField6,
								   inventory.CustomField7,
								   inventory.CustomField8,
								   inventory.CustomField9,
								   inventory.CustomDate1,
								   inventory.CustomDate2,
								   inventory.CustomDate3,
								   inventory.CustomDate4,
								   inventory.CustomDate5,
								   inventory.CustomDate6,
								   inventory.CustomDate7,
								   inventory.CustomDate8,
								   inventory.CustomDate9,
								   inventory.CustomLong1,
								   inventory.CustomLong2,
								   inventory.CustomLong3,
								   inventory.CustomLong4,
								   inventory.CustomLong5,
								   inventory.CustomLong6,
								   inventory.CustomLong7,
								   inventory.CustomLong8,
								   inventory.CustomLong9,
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
