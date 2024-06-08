using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3586Tests : TestBase
	{
		public enum Status
		{
			Active,
			Inactive
		}

		public class InventoryResourceCombinedDTO
		{
			public InventoryResourceDTO? InventoryResource { get; set; }
			public WmsLoadCarrierDTO? LoadCarrier { get; set; }
			public WmsResourceTypeDTO? ResourceType { get; set; }
			public MaterialDTO? Material { get; set; }
			public WmsBatchDTO? Batch { get; set; }
			public decimal Reservations { get; set; }

			public WmsResourcePointDTO? ResourcePoint { get; set; }
			public string? InfeedAdviceNumber { get; set; }
			public string? InfeedAdvicePosition { get; set; }
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; } 
			[Column] public Status Status { get; set; }
			[Column] public Guid ResourceID { get; set; }
			[Column] public Guid MaterialID { get; set; }
			[Column] public string? BatchNumber { get; set; }
			[Column] public Guid InfeedAdviceID { get; set; }
			[Column] public decimal Quantity { get; set; }
		}

		[Table]
		public class WmsResourceTypeDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
		}

		[Table]
		public class WmsResourcePointDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
		}

		[Table]
		public class RefOutfeedTransportOrderResourceDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid? InventoryResourceID { get; set; }
			[Column] public Guid? ResourceID { get; set; }
			[Column] public decimal Quantity { get; set; }
		}

		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid InventoryResourceID { get; set; }
			[Column] public string? InfeedAdviceNumber { get; set; }
			[Column] public string? InfeedAdvicePosition { get; set; }
		}

		public class MaterialDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
		}

		public class WmsBatchDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid MaterialID { get; set; }
			[Column] public string? BatchNumber { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column, PrimaryKey] public Guid Id { get; set; }
			[Column] public Guid TypeID { get; set; }
			[Column] public Guid ResourcePointID { get; set; }
		}

		[Test]
		public void ComplexWhereWithAny([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable<InventoryResourceDTO>())
			using (db.CreateLocalTable<WmsLoadCarrierDTO>())
			using (db.CreateLocalTable<RefOutfeedTransportOrderResourceDTO>())
			using (db.CreateLocalTable<WmsResourceTypeDTO>())
			using (db.CreateLocalTable<WmsResourcePointDTO>())
			using (db.CreateLocalTable<MaterialDTO>())
			using (db.CreateLocalTable<WmsBatchDTO>())
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				var inventoryQry = db.GetTable<InventoryResourceDTO>();
				var lcQry = db.GetTable<WmsLoadCarrierDTO>();
				var reservation = db.GetTable<RefOutfeedTransportOrderResourceDTO>();

				var qry =
					from inventory in inventoryQry
					join res in lcQry on inventory.ResourceID equals res.Id into reslist
					from r in reslist.DefaultIfEmpty()
					join restp in db.GetTable<WmsResourceTypeDTO>() on r.TypeID equals restp.Id into rtplst
					from rtp in rtplst.DefaultIfEmpty()
					join resp in db.GetTable<WmsResourcePointDTO>() on r.ResourcePointID equals resp.Id into resplst
					from rp in resplst.DefaultIfEmpty()
					join mat1 in db.GetTable<MaterialDTO>() on inventory.MaterialID equals mat1.Id into mat1list
					from material in mat1list.DefaultIfEmpty()
					join bat1 in db.GetTable<WmsBatchDTO>() on new {inventory.MaterialID, inventory.BatchNumber} equals new {bat1.MaterialID, bat1.BatchNumber} into bat1list
					from batch in bat1list.DefaultIfEmpty()
					join resvir in db.GetTable<RefOutfeedTransportOrderResourceDTO>() on inventory.Id equals resvir.InventoryResourceID into resvirlist
					join infAdv in db.GetTable<InfeedAdvicePositionDTO>() on inventory.InfeedAdviceID equals infAdv.Id into infList
					from infeed in infList.DefaultIfEmpty()
					where inventory.Status == Status.Inactive
					select new InventoryResourceCombinedDTO()
					{
						InventoryResource = inventory,
						LoadCarrier = r,
						ResourceType = rtp,
						Material = material,
						Batch = batch,
						Reservations =
							reservation.Any(resvr => resvr.ResourceID == inventory.ResourceID && !resvr.InventoryResourceID.HasValue) ? inventory.Quantity : ((decimal?)reservation.Where(resvir => resvir.InventoryResourceID == inventory.Id).Sum(resvir => resvir.Quantity) ?? 0m),
						ResourcePoint = rp,
						InfeedAdviceNumber = infeed.InfeedAdviceNumber,
						InfeedAdvicePosition = infeed.InfeedAdvicePosition
					};

				var anyTest = db.GetTable<InventoryResourceDTO>().Where(x => qry.Any(y => y.InventoryResource!.Id == x.Id))
					.ToArray();
			}
		}
	}

}
