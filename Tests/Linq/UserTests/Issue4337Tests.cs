using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4337Tests : TestBase
	{
		public class AisleDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int AisleNumber { get; set; }
			public virtual int PlantID { get; set; }
			public virtual string? Name { get; set; }
		}

		public class ChannelDTO
		{
			public virtual int Id { get; set; }
			public virtual int AisleID { get; set; }
			public virtual Guid MaterialID { get; set; }
		}

		public class ShelfInfoDTO
		{
			public virtual Guid Id { get; set; }
			//Loadcarrier
			public virtual Guid LoadCarrierId { get; set; }
			public string? LoadCarrierResourceLabel { get; set; }
			public int LoadCarrierStatus { get; set; }
			public long? LoadCarrierCustomLong2 { get; set; }
			public int? LoadCarrierHeightClass { get; set; }
			public virtual Guid? LoadCarrierTypeId { get; set; }


			//material
			public virtual Guid MaterialId { get; set; }
			public string? MaterialNumber { get; set; }
			public string? MaterialDescription_1 { get; set; }
			public virtual string? MaterialCategoryABC { get; set; }


			//storageshelf
			public virtual Guid StorageShelfId { get; set; }
			public int StorageShelfStatus { get; set; }
			public string? StorageShelfName { get; set; }
			public int StorageShelfHeightClass { get; set; }
			public string? StorageShelfCategoryABC { get; set; }
			public int StorageShelfAisleNumber { get; set; }


			//inventoryResource
			public virtual Guid InventoryResourceId { get; set; }
			public string? InventoryResourceBatchNumber { get; set; }
			public int InventoryResourceProductStatus { get; set; }
			public DateTime? InventoryResourceExpiryDate { get; set; }
			public DateTime? InventoryResourceCustomDate1 { get; set; }


			public virtual int InventoryCount { get; set; }
			public virtual bool ReservedForOutfeed { get; set; }
			public virtual bool ManuallyInfeededMaterial { get; set; }
		}

		public class ChannelInfoCombinedDTO
		{
			public virtual int Id { get; set; }
			public virtual ChannelInfoDTO? ChannelInfo { get; set; }
			public virtual ShelfInfoDTO? Shelf1 { get; set; }
			public virtual ShelfInfoDTO? Shelf2 { get; set; }
		}

		public class ChannelInfoDTO
		{
			public virtual int Id { get; set; }
			public virtual ChannelDTO? Channel { get; set; }
			public virtual AisleDTO? Aisle { get; set; }
			public virtual MaterialDTO? Material { get; set; }
		}

		public class InfeedAdvicePositionDTO
		{
			public virtual int Id { get; set; }
			public virtual int InfeedAdviceType { get; set; }
			public virtual AisleDTO? Aisle { get; set; }
			public virtual MaterialDTO? Material { get; set; }
		}

		public class InventoryResourceDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int Status { get; set; }
			public virtual Guid ResourceID { get; set; }
			public virtual int ProductStatus { get; set; }
			public virtual string? BatchNumber { get; set; }
			public virtual int BundleUnit { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual Guid MaterialID { get; set; }
			public virtual int? InfeedAdviceID { get; set; }
			public virtual DateTime ExpiryDate { get; set; }
			public virtual DateTime CustomDate1 { get; set; }
		}

		public class MaterialDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual string? MaterialNumber { get; set; }
			public virtual string? MaterialDescription_1 { get; set; }
			public virtual string? MaterialDescription_2 { get; set; }
			public virtual string? MaterialDescription_3 { get; set; }
			public virtual string? CategoryABC { get; set; }
			public virtual string? CategoryCustoms { get; set; }
			public virtual string? CategoryDimensions { get; set; }
			public virtual string? CategoryQuality { get; set; }
			public virtual string? CategoryTemperature { get; set; }
		}

		public class WmsLoadCarrierDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int Status { get; set; }
			public virtual string? ResourceLabel { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int CustomLong1 { get; set; }
			public virtual int CustomLong2 { get; set; }
			public virtual int CustomLong3 { get; set; }
			public int HeightClass { get; set; }
			public Guid TypeID { get; set; }
		}

		public class RefResourceStorageShelfDTO
		{
			public virtual int Id { get; set; }
			public virtual Guid StorageShelfID { get; set; }
			public virtual Guid ResourceID { get; set; }
		}

		public class RefOutfeedTransportOrderResourceDTO
		{
			public virtual int Id { get; set; }
			public virtual Guid ResourceID { get; set; }
		}


		public class StorageShelfDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual Guid Id { get; set; }
			public virtual int ChannelID { get; set; }
			public virtual string? Name { get; set; }
			public virtual int Status { get; set; }
			public virtual int AisleNumber { get; set; }
			public virtual string? CategoryABC { get; set; }
			public virtual int HeightClass { get; set; }
			public virtual int DepthCoordinate { get; set; }
		}

		[Test]
		public void Issue4337_TestComplexQueryWms([IncludeDataSources(ProviderName.SQLiteMS, ProviderName.SqlServer2019)] string context)
		{
			using (var s = new DataConnection(context))
			{
				s.CreateTempTable<ChannelDTO>();
				s.CreateTempTable<InventoryResourceDTO>();
				s.CreateTempTable<MaterialDTO>();
				s.CreateTempTable<WmsLoadCarrierDTO>();
				s.CreateTempTable<RefResourceStorageShelfDTO>();
				s.CreateTempTable<AisleDTO>();
				s.CreateTempTable<RefOutfeedTransportOrderResourceDTO>();
				s.CreateTempTable<InfeedAdvicePositionDTO>();
				s.CreateTempTable<StorageShelfDTO>();


				var channelQry = s.GetTable<ChannelDTO>();
				var inventoryResourceQry = s.GetTable<InventoryResourceDTO>().Where(x => x.Status < 99);
				var materialQry = s.GetTable<MaterialDTO>();
				var resourceQry = s.GetTable<WmsLoadCarrierDTO>();

				var query = from c in channelQry
				join a in s.GetTable<AisleDTO>() on c.AisleID equals a.Id

				join m in s.GetTable<MaterialDTO>() on c.MaterialID equals m.Id into mn
				from m in mn.DefaultIfEmpty()

					/*
					join ss in s.Query<StorageShelfDTO>() on c.Id equals ss.ChannelID
					join rss in s.Query<RefResourceStorageShelfDTO>() on ss.Id equals rss.StorageShelfID into rssn
					*/

					#region Depth 1

				join a1 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 1 } equals new { Id = a1.ChannelID, Depth = a1.DepthCoordinate } into a1n
				from a1 in a1n.DefaultIfEmpty()
				join b1 in s.GetTable<RefResourceStorageShelfDTO>() on a1.Id equals b1.StorageShelfID into b1n
				from b1 in b1n.DefaultIfEmpty()
				join c1 in s.GetTable<WmsLoadCarrierDTO>() on b1.ResourceID equals c1.Id into c1n
				from c1 in c1n.DefaultIfEmpty()

				join i1 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b1.ResourceID).Id equals i1.Id into i1n
				from i1 in i1n.DefaultIfEmpty()

				join m1 in s.GetTable<MaterialDTO>() on i1.MaterialID equals m1.Id into m1n
				from m1 in m1n.DefaultIfEmpty()

				let ir1ic = inventoryResourceQry.Count(x => x.ResourceID == c1.Id)
				let ir1lm = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && x.ProductStatus > 0)

				let ror1 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c1.Id)
				let ir1mim = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

				#endregion

				#region Depth 2

				join a2 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 2 } equals new { Id = a2.ChannelID, Depth = a2.DepthCoordinate } into a2n
				from a2 in a2n.DefaultIfEmpty()
				join b2 in s.GetTable<RefResourceStorageShelfDTO>() on a2.Id equals b2.StorageShelfID into b2n
				from b2 in b2n.DefaultIfEmpty()
				join c2 in s.GetTable<WmsLoadCarrierDTO>() on b2.ResourceID equals c2.Id into c2n
				from c2 in c2n.DefaultIfEmpty()

				join i2 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b2.ResourceID).Id equals i2.Id into i2n
				from i2 in i2n.DefaultIfEmpty()

				join m2 in s.GetTable<MaterialDTO>() on i2.MaterialID equals m2.Id into m2n
				from m2 in m2n.DefaultIfEmpty()

				let ir2ic = inventoryResourceQry.Count(x => x.ResourceID == c2.Id)
				let ir2lm = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && x.ProductStatus > 0)

				let ror2 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c2.Id)
				let ir2mim = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

				#endregion

				select new ChannelInfoCombinedDTO
				{
					Id = c.Id,
					ChannelInfo = new ChannelInfoDTO
					{
						Id = c.Id,
						Channel = c,
						Aisle = new AisleDTO() { StrippedDownDTO = true, Id = a.Id, AisleNumber = a.AisleNumber, PlantID = a.PlantID, Name = a.Name },
						Material = new MaterialDTO { StrippedDownDTO = true, Id = m.Id, MaterialNumber = m.MaterialNumber, MaterialDescription_1 = m.MaterialDescription_1, MaterialDescription_2 = m.MaterialDescription_2, MaterialDescription_3 = m.MaterialDescription_3, CategoryABC = m.CategoryABC, CategoryCustoms = m.CategoryCustoms, CategoryDimensions = m.CategoryDimensions, CategoryQuality = m.CategoryQuality, CategoryTemperature = m.CategoryTemperature },
					},
					Shelf1 = ((a1 != null && a1.Id != Guid.Empty) ? new ShelfInfoDTO()
					{
						Id = a1.Id,
						LoadCarrierId = c1.Id,
						LoadCarrierResourceLabel = c1.ResourceLabel,
						LoadCarrierStatus = c1.Status,
						LoadCarrierCustomLong2 = c1.CustomLong2,
						LoadCarrierHeightClass = c1.HeightClass,
						LoadCarrierTypeId = c1.TypeID,

						MaterialId = m1.Id,
						MaterialNumber = m1.MaterialNumber,
						MaterialDescription_1 = m1.MaterialDescription_1,
						MaterialCategoryABC = m1.CategoryABC,

						StorageShelfId = a1.Id,
						StorageShelfStatus = a1.Status,
						StorageShelfName = a1.Name,
						StorageShelfHeightClass = a1.HeightClass,
						StorageShelfCategoryABC = a1.CategoryABC,
						StorageShelfAisleNumber = a1.AisleNumber,

						InventoryResourceId = i1.Id,
						InventoryResourceBatchNumber = i1.BatchNumber,
						InventoryResourceProductStatus = i1.ProductStatus,
						InventoryResourceExpiryDate = i1.ExpiryDate,
						InventoryResourceCustomDate1 = i1.CustomDate1,

						InventoryCount = ir1ic,
						ReservedForOutfeed = ror1,
						ManuallyInfeededMaterial = ir1mim
					}: null),
					Shelf2 = ((a2 != null && a2.Id != Guid.Empty) ? new ShelfInfoDTO()
					{
						Id = a2.Id,
						LoadCarrierId = c2.Id,
						LoadCarrierResourceLabel = c2.ResourceLabel,
						LoadCarrierStatus = c2.Status,
						LoadCarrierCustomLong2 = c2.CustomLong2,
						LoadCarrierHeightClass = c2.HeightClass,
						LoadCarrierTypeId = c2.TypeID,

						MaterialId = m2.Id,
						MaterialNumber = m2.MaterialNumber,
						MaterialDescription_1 = m2.MaterialDescription_1,
						MaterialCategoryABC = m2.CategoryABC,

						StorageShelfId = a2.Id,
						StorageShelfStatus = a2.Status,
						StorageShelfName = a2.Name,
						StorageShelfHeightClass = a2.HeightClass,
						StorageShelfCategoryABC = a2.CategoryABC,
						StorageShelfAisleNumber = a2.AisleNumber,

						InventoryResourceId = i2.Id,
						InventoryResourceBatchNumber = i2.BatchNumber,
						InventoryResourceProductStatus = i2.ProductStatus,
						InventoryResourceExpiryDate = i2.ExpiryDate,
						InventoryResourceCustomDate1 = i2.CustomDate1,

						InventoryCount = ir2ic,
						ReservedForOutfeed = ror2,
						ManuallyInfeededMaterial = ir2mim
					} : null)
				};

				var res = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2 }).ToList();
			}
		}
	}
}
