using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Newtonsoft.Json;

using NUnit.Framework;

using Tests.Model;

using DataType = LinqToDB.DataType;

namespace Tests.UserTests.Test5366
{
	[TestFixture]
	public class Issue5366Tests : TestBase
	{
		public enum InventoryResourceStatus
		{
			ReservedForInfeed,
			ReservedForOutfeed,
			Used
		}

		public enum ResourceStatus
		{
			Used
		}

		public enum AisleStatus
		{
			Available,
			OnlyOutfeed
		}

		public enum StockType
		{
			unkown
		}

		[Table]
		public class WmsResourcePointDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public DateTime? ExpiryDate { get; set; }
			[Column] public StockType StockType { get; set; }
			[Column] public bool IsStoragePlace { get; set; }
			[Column] public bool IsSrm { get; set; }
			[Column] public Guid PlantID { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public Guid ResourcePointID { get; set; }
			[Column] public DateTime? ExpiryDate { get; set; }
			[Column] public bool DontTouch { get; set; }
			[Column] public ResourceStatus Status { get; set; }
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public Guid ResourceID { get; set; }
			[Column] public DateTime? ExpiryDate { get; set; }
			[Column] public InventoryResourceStatus Status { get; set; }
			[Column] public Guid? MaterialID { get; set; }
			[Column] public int ProductStatus { get; set; }
			[Column] public Guid ConsignmentId { get; set; }
			[Column] public decimal Quantity { get; set; }
			[Column] public bool IsPicked { get; set; }
			[Column] public bool IsPacked { get; set; }
		}

		[Table]
		public class RefOutfeedTransportOrderResourceDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public Guid? InventoryResourceID { get; set; }
			[Column] public decimal Quantity { get; set; }
			[Column] public Guid ResourceID { get; set; }
		}

		[Table]
		public class RefResourceStorageShelfDTO
		{
			[Column] public Guid StorageShelfID { get; set; }
			[Column] public Guid ResourceID { get; set; }
		}

		[Table]
		public class StorageShelfDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public Guid ChannelID { get; set; }
			[Column] public Guid AisleID { get; set; }
		}

		[Table]
		public class ChannelDTO
		{
			[Column] public Guid Id { get; set; }
		}

		[Table]
		public class AisleDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public AisleStatus Status { get; set; }
			[Column] public AisleStatus GroupStatus { get; set; }
			[Column] public StockType StockType { get; set; }
		}

		[Table]
		public class OutfeedTransportOrderDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public Guid? MaterialID { get; set; }
			[Column] public Guid ConsignmentId { get; set; }
			[Column] public int ProductStatus { get; set; }
			[Column] public Guid PlantID { get; set; }
		}

		public class RoutePlanerOutfeedResultDto
		{
			public WmsLoadCarrierDTO? R { get; set; }
			public InventoryResourceDTO? IR { get; set; }
			public decimal TakeThisQuantityFromIR { get; set; }
			public ChannelDTO? C { get; set; }
			public StorageShelfDTO? SS { get; set; }
			public AisleStatus AisleStatus { get; set; }
			public StockType AisleStockType { get; set; }
			public WmsResourcePointDTO? RP { get; set; }
			public decimal RefQty { get; set; }
			public int RefCntIR { get; set; }
			public int RefCntLC { get; set; }
			public bool MixedStock { get; set; }
		}

		[Test]
		public void ComplexDistinctQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			using var t1 = db.CreateLocalTable<WmsResourcePointDTO>();
			using var t2 = db.CreateLocalTable<WmsLoadCarrierDTO>();
			using var t3 = db.CreateLocalTable<InventoryResourceDTO>();
			using var t4 = db.CreateLocalTable<RefOutfeedTransportOrderResourceDTO>();
			using var t5 = db.CreateLocalTable<RefResourceStorageShelfDTO>();
			using var t6 = db.CreateLocalTable<StorageShelfDTO>();
			using var t7 = db.CreateLocalTable<ChannelDTO>();
			using var t8 = db.CreateLocalTable<AisleDTO>();
			using var t9 = db.CreateLocalTable<OutfeedTransportOrderDTO>();

			OutfeedTransportOrderDTO orderDTO = new() { MaterialID = TestData.Guid1 };

			var refOtorQty = db.GetTable<RefOutfeedTransportOrderResourceDTO>()
				.GroupBy(refOtor => refOtor.InventoryResourceID)
				.Select(g => new
				{
					InventoryResourceID = g.Key,
					RefQty = (decimal?)g.Sum(x => x.Quantity) ?? 0m,
					RefCntIR = g.Count()
				});

			var refOtorCntLC = db.GetTable<RefOutfeedTransportOrderResourceDTO>()
				.GroupBy(refOtor => refOtor.ResourceID)
				.Select(g => new
				{
					ResourceID = g.Key,
					RefCntLC = g.Count()
				});

			var refOtorExists = db.GetTable<RefOutfeedTransportOrderResourceDTO>()
				.Where(r => !r.InventoryResourceID.HasValue)
				.Select(r => r.ResourceID)
				.Distinct();

			var irMixSubquery = db.GetTable<InventoryResourceDTO>()
				.Where(irMix =>
					irMix.Status >= InventoryResourceStatus.ReservedForInfeed &&
					irMix.Status <= InventoryResourceStatus.ReservedForOutfeed &&
					(irMix.MaterialID != orderDTO.MaterialID.Value || irMix.ProductStatus != 0))
				.Select(irMix => irMix.ResourceID)
				.Distinct();

			var baseQry =
				from rp in db.GetTable<WmsResourcePointDTO>()
				join r in db.GetTable<WmsLoadCarrierDTO>() on rp.Id equals r.ResourcePointID
				join ir in db.GetTable<InventoryResourceDTO>() on r.Id equals ir.ResourceID
				join roq in refOtorQty on ir.Id equals roq.InventoryResourceID into roqGroup
				from roq in roqGroup.DefaultIfEmpty()
				join roclc in refOtorCntLC on r.Id equals roclc.ResourceID into roclcGroup
				from roclc in roclcGroup.DefaultIfEmpty()
				join irm in irMixSubquery on r.Id equals irm into irmGroup
				from irm in irmGroup.DefaultIfEmpty()
				join refS in db.GetTable<RefResourceStorageShelfDTO>() on r.Id equals refS.ResourceID into refslst
				from refS in refslst.DefaultIfEmpty()
				join ss in db.GetTable<StorageShelfDTO>() on refS.StorageShelfID equals ss.Id into sslst
				from ss in sslst.DefaultIfEmpty()
				join c in db.GetTable<ChannelDTO>() on ss.ChannelID equals c.Id into clst
				from c in clst.DefaultIfEmpty()
				join aisle in db.GetTable<AisleDTO>() on ss.AisleID equals aisle.Id into aislelst
				from aisle in aislelst.DefaultIfEmpty()
				join roe in refOtorExists on ir.ResourceID equals roe into roeGroup
				from roe in roeGroup.DefaultIfEmpty()
				where ir.MaterialID == orderDTO.MaterialID.Value &&
					  ir.ConsignmentId == orderDTO.ConsignmentId &&
					  ir.ProductStatus == orderDTO.ProductStatus &&
					  ir.Quantity > 0 &&
					  !ir.IsPicked &&
					  !ir.IsPacked &&
					  !r.DontTouch
				select new RoutePlanerOutfeedResultDto
				{
					IR = ir,
					R = r,
					SS = ss,
					C = c,
					AisleStatus = aisle != null ? (aisle.Status == AisleStatus.Available ? aisle.GroupStatus : aisle.Status) : AisleStatus.Available,
					AisleStockType = aisle != null ? aisle.StockType : rp.StockType,
					RP = rp,
					RefQty = roq != null ? roq.RefQty : 0m,
					RefCntIR = roq != null ? roq.RefCntIR : 0,
					RefCntLC = roclc != null ? roclc.RefCntLC : 0,
					MixedStock = irm != Guid.Empty,
				};

			baseQry = baseQry.Where(cr =>
				  cr.RP!.IsStoragePlace || (cr.RP.IsSrm &&
										  db.GetTable<RefResourceStorageShelfDTO>().Any(refRSS => refRSS.ResourceID == cr.R!.Id)));
			baseQry = baseQry.Where(cr => cr.RP!.PlantID == orderDTO.PlantID);

			baseQry = baseQry
				  .Where(cr =>
					  !db.GetTable<RefOutfeedTransportOrderResourceDTO>()
						  .Any(refOtorExists => refOtorExists.ResourceID == cr.IR!.ResourceID && !refOtorExists.InventoryResourceID.HasValue) &&
					  cr.IR!.Quantity > cr.RefQty);
			var qry = baseQry;
			qry = qry.Where(cr => cr.AisleStatus == AisleStatus.Available || cr.AisleStatus == AisleStatus.OnlyOutfeed);

			var qryWithStatus = qry.Where(cr => cr.IR!.Status == InventoryResourceStatus.Used &&
														   cr.R!.Status == ResourceStatus.Used);

			var expiryDates = qryWithStatus.Select(x => x.IR!.ExpiryDate).Distinct().OrderBy(x => x ?? DateTime.MinValue).ToList();

			var sql = ((TestDataConnection)db).LastQuery;
		}

		[Test]
		public void SimpifiedDistinctQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			using var t1 = db.CreateLocalTable<WmsResourcePointDTO>();
			using var t2 = db.CreateLocalTable<WmsLoadCarrierDTO>();
			using var t3 = db.CreateLocalTable<InventoryResourceDTO>();

			var baseQry =
				from rp in db.GetTable<WmsResourcePointDTO>()
				join r in db.GetTable<WmsLoadCarrierDTO>() on rp.Id equals r.ResourcePointID
				join ir in db.GetTable<InventoryResourceDTO>() on r.Id equals ir.ResourceID
				select new RoutePlanerOutfeedResultDto
				{
					IR = ir,
					R = r,
					RP = rp
				};

			var expiryDates = baseQry.Select(x => x.IR!.ExpiryDate).Distinct().OrderBy(x => x ?? DateTime.MinValue).ToList();

			var sql = ((TestDataConnection)db).LastQuery;
		}
	}
}
