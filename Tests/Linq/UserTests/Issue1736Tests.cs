using System;
using System.Linq;
using System.Runtime.Serialization;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests.Issue1736
{
	[TestFixture]
	public class UserTest : TestBase
	{
		public interface IModifiedTimeStamp
		{
		}

		public class BasicDTO : BasicDTOwithoutID, IDTOWithId
		{
			public Guid Id { get; set; }
		}

		public class BasicDTOwithExtensionData : BasicDTO, IExtensionData
		{
		}

		public class BasicDTOwithoutID : IMccEntityState
		{
		}

		public interface ICreatedTimeStamp
		{
		}

		public interface IDTOWithId
		{
		}

		public interface IHasArchiveTable
		{
		}

		public interface IMccEntityState
		{
		}

		public interface IExtensionData
		{
		}

		public class AisleDTO : BasicDTOwithExtensionData
		{
			public AisleStatus Status { get; set; }
		}

		public class ChannelDTO : BasicDTOwithExtensionData, IModifiedTimeStamp
		{
		}

		public class InventoryResourceDTO : WmsBasicDTO<InventoryResourceDTO>, IHasArchiveTable
		{
			public InventoryResourceStatus Status { get; set; }

			public Guid MaterialID { get; set; }

			public Guid ResourceID { get; set; }

			public decimal Quantity { get; set; }

			public int ProductStatus { get; set; }
		}

		public class OutfeedTransportOrderDTO : WmsBasicDTO<OutfeedTransportOrderDTO>, IHasArchiveTable
		{
			public Guid? MaterialID { get; set; }
		}

		public class RefOutfeedTransportOrderResourceDTO : BasicDTO
		{
			public Guid ResourceID { get; set; }

			public Guid? InventoryResourceID { get; set; }

			public decimal Quantity { get; set; }
		}

		public class RefResourceStorageShelfDTO : BasicDTOwithoutID
		{
			public Guid ResourceID { get; set; }

			public Guid StorageShelfID { get; set; }
		}

		public class RefResPointAisleDTO : BasicDTOwithoutID
		{
			public Guid ResourcePointId { get; set; }

			public Guid AisleId { get; set; }
		}

		public class StorageShelfDTO : BasicDTOwithExtensionData, IModifiedTimeStamp
		{
			public Guid AisleID { get; set; }

			public Guid ChannelID { get; set; }
		}

		public abstract class WmsBasicDTO<T> : WmsBasicWithoutCustomFieldsDTO<T>
		{
		}

		public abstract class WmsBasicWithoutCustomFieldsDTO<T> : BasicDTOwithExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
		{
		}

		public class WmsLoadCarrierDTO : WmsBasicDTO<WmsLoadCarrierDTO>, IHasArchiveTable
		{
			public Guid? ResourcePointID { get; set; }
		}

		public class WmsResourcePointDTO : WmsBasicDTO<WmsResourcePointDTO>
		{
			[DataMember]
			public bool IsStoragePlace { get; set; }
		}

		public enum AisleStatus
		{
			Available
		}

		public enum InventoryResourceStatus
		{
			ReservedForInfeed,
			ReservedForOutfeed
		}

		public class RoutePlanerOutfeedResultDto
		{
			public WmsLoadCarrierDTO    R           { get; set; } = null!;
			public InventoryResourceDTO IR          { get; set; } = null!;
			public ChannelDTO?          C           { get; set; }
			public StorageShelfDTO?     SS          { get; set; }
			public AisleStatus          AisleStatus { get; set; }
			public WmsResourcePointDTO  RP          { get; set; } = null!;
			public decimal              RefQty      { get; set; }
			public bool                 MixedStock  { get; set; }
		}

		[Test]
		public void Issue1736Tests([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			var orderDTO = new OutfeedTransportOrderDTO();

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<StorageShelfDTO>())
			using (db.CreateLocalTable<ChannelDTO>())
			using (db.CreateLocalTable<RefResourceStorageShelfDTO>())
			using (db.CreateLocalTable<RefOutfeedTransportOrderResourceDTO>())
			using (db.CreateLocalTable<AisleDTO>())
			using (db.CreateLocalTable<RefResPointAisleDTO>())
			using (db.CreateLocalTable<WmsResourcePointDTO>())
			using (db.CreateLocalTable<WmsLoadCarrierDTO>())
			using (db.CreateLocalTable<InventoryResourceDTO>())
			{
				var subqryRefOtoR = db.GetTable<RefOutfeedTransportOrderResourceDTO>();

				var baseQry1 =
					from ss in db.GetTable<StorageShelfDTO>()
					join c in db.GetTable<ChannelDTO>() on ss.ChannelID equals c.Id
					join refS in db.GetTable<RefResourceStorageShelfDTO>() on ss.Id equals refS.StorageShelfID
					join aisle in db.GetTable<AisleDTO>() on ss.AisleID equals aisle.Id
					join aislerp in db.GetTable<RefResPointAisleDTO>() on ss.AisleID equals aislerp.AisleId
					join rp in db.GetTable<WmsResourcePointDTO>() on aislerp.ResourcePointId equals rp.Id
					join r in db.GetTable<WmsLoadCarrierDTO>() on refS.ResourceID equals r.Id
					join ir in db.GetTable<InventoryResourceDTO>() on r.Id equals ir.ResourceID
					where ir.MaterialID == orderDTO.MaterialID!.Value &&
					      ir.ProductStatus == 0 &&
					      ir.Quantity > 0
					select new RoutePlanerOutfeedResultDto
					{
						IR = ir,
						R = r,
						SS = ss,
						C = c,
						AisleStatus = aisle.Status,
						RP = rp,
						RefQty = ((decimal?)subqryRefOtoR.Where(x => x.InventoryResourceID == ir.Id)
							          .Sum(x => x.Quantity) ?? 0m) +
						         subqryRefOtoR.Count(x => x.ResourceID == r.Id && !x.InventoryResourceID.HasValue) *
						         ir.Quantity,
						MixedStock = db.GetTable<InventoryResourceDTO>()
							.Any(irMix => irMix.ResourceID == r.Id &&
							              irMix.Status >= InventoryResourceStatus.ReservedForInfeed &&
							              irMix.Status <= InventoryResourceStatus.ReservedForOutfeed &&
							              (irMix.MaterialID != orderDTO.MaterialID!.Value ||
							               irMix.ProductStatus != 0))
					};

				var baseQry2 =
					from rp in db.GetTable<WmsResourcePointDTO>()
					join r in db.GetTable<WmsLoadCarrierDTO>() on rp.Id equals r.ResourcePointID
					join ir in db.GetTable<InventoryResourceDTO>() on r.Id equals ir.ResourceID
					where rp.IsStoragePlace &&
					      ir.MaterialID == orderDTO.MaterialID!.Value &&
					      ir.ProductStatus == 0 &&
					      ir.Quantity > 0
					select new RoutePlanerOutfeedResultDto
					{
						IR = ir,
						R = r,
						SS = null,
						C = null,
						AisleStatus = AisleStatus.Available,
						RP = rp,
						RefQty = 0,
						MixedStock = false
					};

				var baseQry = baseQry1.Union(baseQry2);
				baseQry = baseQry.Where(cr => cr.IR.Quantity > cr.RefQty);

				Assert.DoesNotThrow(() => baseQry.ToList());
			}
		}
	}
}
