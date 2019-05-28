using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;


namespace Tests.UserTests.Issue1736
{
	public interface IModifiedTimeStamp
	{
	}
	public class BasicDTO : BasicDTOwithoutID, IMccEntityState, IInterlinqDTO, IDTOWithId
	{
		public Guid Id { get; set; }
	}
	public class BasicDTOwithExtensionData : BasicDTO, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData
	{
	}
	public class BasicDTOwithoutID : IMccEntityState, IInterlinqDTO
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

	public interface IInterlinqDTO
	{
	}

	public interface IMccEntityState
	{
	}

	public interface IExtensionData
	{
	}

	public interface IGetQueryableFromType
	{
	}


	public class AisleDTO : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData
	{
		public AisleStatus Status { get; set; }
	}

	public class ChannelDTO : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp
	{
	}


	public class InventoryResourceDTO : WmsBasicDTO<InventoryResourceDTO>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public InventoryResourceStatus Status { get; set; }

		public Guid MaterialID { get; set; }

		public Guid ResourceID { get; set; }

		public Decimal Quantity { get; set; }

		public int ProductStatus { get; set; }
	}


	public class OutfeedTransportOrderDTO : WmsBasicDTO<OutfeedTransportOrderDTO>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public Guid? MaterialID { get; set; }
	}


	public class RefOutfeedTransportOrderResourceDTO : BasicDTO, IMccEntityState, IInterlinqDTO, IDTOWithId
	{
		public Guid ResourceID { get; set; }

		public Guid? InventoryResourceID { get; set; }

		public Decimal Quantity { get; set; }
	}


	public class RefResourceStorageShelfDTO : BasicDTOwithoutID, IMccEntityState, IInterlinqDTO
	{
		public Guid ResourceID { get; set; }

		public Guid StorageShelfID { get; set; }
	}
	public class RefResPointAisleDTO : BasicDTOwithoutID, IMccEntityState, IInterlinqDTO
	{
		public Guid ResourcePointId { get; set; }

		public Guid AisleId { get; set; }
	}

	public class StorageShelfDTO : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp
	{
		public Guid AisleID { get; set; }

		public Guid ChannelID { get; set; }
	}

	public abstract class WmsBasicDTO<T> : WmsBasicWithoutCustomFieldsDTO<T>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
	{
	}

	public abstract class WmsBasicWithoutCustomFieldsDTO<T> : BasicDTOwithExtensionData, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
	{
	}

	public class WmsLoadCarrierDTO : WmsBasicDTO<WmsLoadCarrierDTO>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp, IHasArchiveTable
	{
		public Guid? ResourcePointID { get; set; }
	}

	public class WmsResourcePointDTO : WmsBasicDTO<WmsResourcePointDTO>, IMccEntityState, IInterlinqDTO, IDTOWithId, IExtensionData, IModifiedTimeStamp, ICreatedTimeStamp
	{
		[System.Runtime.Serialization.DataMemberAttribute()]
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
		public WmsLoadCarrierDTO R { get; set; }

		public InventoryResourceDTO IR { get; set; }

		public ChannelDTO C { get; set; }

		public StorageShelfDTO SS { get; set; }

		public AisleStatus AisleStatus { get; set; }

		public WmsResourcePointDTO RP { get; set; }

		public Decimal RefQty { get; set; }

		public bool MixedStock { get; set; }
	}

	[TestFixture]
	public class UserTest : TestBase
	{
		[Test]
		public void Issue1736Tests([DataSources(ProviderName.SQLite)] string context)
		{
			var orderDTO = new OutfeedTransportOrderDTO();

			using (var db = GetDataContext(context))
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
			   where ir.MaterialID == orderDTO.MaterialID.Value &&
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
				   RefQty = ((decimal?)subqryRefOtoR.Where(x => x.InventoryResourceID == ir.Id).Sum(x => x.Quantity) ?? 0m) +
							(subqryRefOtoR.Count(x => x.ResourceID == r.Id && !x.InventoryResourceID.HasValue) * ir.Quantity),
				   MixedStock = db.GetTable<InventoryResourceDTO>()
									.Any(irMix => irMix.ResourceID == r.Id &&
												  irMix.Status >= InventoryResourceStatus.ReservedForInfeed && irMix.Status <= InventoryResourceStatus.ReservedForOutfeed &&
												  (irMix.MaterialID != orderDTO.MaterialID.Value ||
												   irMix.ProductStatus != 0))
			   };

				var baseQry2 =
						from rp in db.GetTable<WmsResourcePointDTO>()
						join r in db.GetTable<WmsLoadCarrierDTO>() on rp.Id equals r.ResourcePointID
						join ir in db.GetTable<InventoryResourceDTO>() on r.Id equals ir.ResourceID
						where rp.IsStoragePlace &&
							  ir.MaterialID == orderDTO.MaterialID.Value &&
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
				baseQry.ToList();
				//var test = baseQry.GenerateTestString();	
			}
		}
	}
}
