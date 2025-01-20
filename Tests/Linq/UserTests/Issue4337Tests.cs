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
			public virtual ShelfInfoDTO? Shelf3 { get; set; }
			public virtual ShelfInfoDTO? Shelf4 { get; set; }
			public virtual ShelfInfoDTO? Shelf5 { get; set; }
			public virtual ShelfInfoDTO? Shelf6 { get; set; }
			public virtual ShelfInfoDTO? Shelf7 { get; set; }
			public virtual ShelfInfoDTO? Shelf8 { get; set; }
			public virtual ShelfInfoDTO? Shelf9 { get; set; }
			public virtual ShelfInfoDTO? Shelf10 { get; set; }
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
			using var s = new DataConnection(context);

			using var channelQry                          = s.CreateLocalTable<ChannelDTO>();
			using var inventoryResourceTbl                = s.CreateLocalTable<InventoryResourceDTO>();
			using var materialQry                         = s.CreateLocalTable<MaterialDTO>();
			using var resourceQry                         = s.CreateLocalTable<WmsLoadCarrierDTO>();
			using var refResourceStorageShelfDTO          = s.CreateLocalTable<RefResourceStorageShelfDTO>();
			using var aisleDTO                            = s.CreateLocalTable<AisleDTO>();
			using var refOutfeedTransportOrderResourceDTO = s.CreateLocalTable<RefOutfeedTransportOrderResourceDTO>();
			using var infeedAdvicePositionDTO             = s.CreateLocalTable<InfeedAdvicePositionDTO>();
			using var storageShelfDTO                     = s.CreateLocalTable<StorageShelfDTO>();

			var inventoryResourceQry = inventoryResourceTbl.Where(x => x.Status < 99);

			var query = from c in channelQry
						join a in aisleDTO on c.AisleID equals a.Id

						join m in materialQry on c.MaterialID equals m.Id into mn
						from m in mn.DefaultIfEmpty()

							/*
							join ss in s.GetTable<StorageShelfDTO>() on c.Id equals ss.ChannelID
							join rss in s.GetTable<RefResourceStorageShelfDTO>() on ss.Id equals rss.StorageShelfID into rssn
							*/

							#region Depth 1

						join a1 in storageShelfDTO on new { Id = c.Id, Depth = 1 } equals new { Id = a1.ChannelID, Depth = a1.DepthCoordinate } into a1n
						from a1 in a1n.DefaultIfEmpty()
						join b1 in refResourceStorageShelfDTO on a1.Id equals b1.StorageShelfID into b1n
						from b1 in b1n.DefaultIfEmpty()
						join c1 in resourceQry on b1.ResourceID equals c1.Id into c1n
						from c1 in c1n.DefaultIfEmpty()

						join i1 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b1.ResourceID).Id equals i1.Id into i1n
						from i1 in i1n.DefaultIfEmpty()

						join m1 in materialQry on i1.MaterialID equals m1.Id into m1n
						from m1 in m1n.DefaultIfEmpty()

						let ir1ic = inventoryResourceQry.Count(x => x.ResourceID == c1.Id)
						let ir1lm = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && x.ProductStatus > 0)

						let ror1 = refOutfeedTransportOrderResourceDTO.Any(x => x.ResourceID == c1.Id)
						let ir1mim = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && (x.InfeedAdviceID == null || infeedAdvicePositionDTO.Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 2

						join a2 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 2 } equals new { Id = a2.ChannelID, Depth = a2.DepthCoordinate } into a2n
						from a2 in a2n.DefaultIfEmpty()
						join b2 in refResourceStorageShelfDTO on a2.Id equals b2.StorageShelfID into b2n
						from b2 in b2n.DefaultIfEmpty()
						join c2 in resourceQry on b2.ResourceID equals c2.Id into c2n
						from c2 in c2n.DefaultIfEmpty()

						join i2 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b2.ResourceID).Id equals i2.Id into i2n
						from i2 in i2n.DefaultIfEmpty()

						join m2 in materialQry on i2.MaterialID equals m2.Id into m2n
						from m2 in m2n.DefaultIfEmpty()

						let ir2ic = inventoryResourceQry.Count(x => x.ResourceID == c2.Id)
						let ir2lm = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && x.ProductStatus > 0)

						let ror2 = refOutfeedTransportOrderResourceDTO.Any(x => x.ResourceID == c2.Id)
						let ir2mim = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && (x.InfeedAdviceID == null || infeedAdvicePositionDTO.Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 3

						join a3 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 3 } equals new { Id = a3.ChannelID, Depth = a3.DepthCoordinate } into a3n
						from a3 in a3n.DefaultIfEmpty()
						join b3 in s.GetTable<RefResourceStorageShelfDTO>() on a3.Id equals b3.StorageShelfID into b3n
						from b3 in b3n.DefaultIfEmpty()
						join c3 in s.GetTable<WmsLoadCarrierDTO>() on b3.ResourceID equals c3.Id into c3n
						from c3 in c3n.DefaultIfEmpty()

						join i3 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b3.ResourceID).Id equals i3.Id into i3n
						from i3 in i3n.DefaultIfEmpty()

						join m3 in s.GetTable<MaterialDTO>() on i3.MaterialID equals m3.Id into m3n
						from m3 in m3n.DefaultIfEmpty()

						let ir3ic = inventoryResourceQry.Count(x => x.ResourceID == c3.Id)
						let ir3lm = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && x.ProductStatus > 0)
						let ror3 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c3.Id)
						let ir3mim = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 4

						join a4 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 4 } equals new { Id = a4.ChannelID, Depth = a4.DepthCoordinate } into a4n
						from a4 in a4n.DefaultIfEmpty()
						join b4 in s.GetTable<RefResourceStorageShelfDTO>() on a4.Id equals b4.StorageShelfID into b4n
						from b4 in b4n.DefaultIfEmpty()
						join c4 in s.GetTable<WmsLoadCarrierDTO>() on b4.ResourceID equals c4.Id into c4n
						from c4 in c4n.DefaultIfEmpty()

						join i4 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b4.ResourceID).Id equals i4.Id into i4n
						from i4 in i4n.DefaultIfEmpty()

						join m4 in s.GetTable<MaterialDTO>() on i4.MaterialID equals m4.Id into m4n
						from m4 in m4n.DefaultIfEmpty()

						let ir4ic = inventoryResourceQry.Count(x => x.ResourceID == c4.Id)
						let ir4lm = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && x.ProductStatus > 0)
						let ror4 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c4.Id)
						let ir4mim = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 5

						join a5 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 5 } equals new { Id = a5.ChannelID, Depth = a5.DepthCoordinate } into a5n
						from a5 in a5n.DefaultIfEmpty()
						join b5 in s.GetTable<RefResourceStorageShelfDTO>() on a5.Id equals b5.StorageShelfID into b5n
						from b5 in b5n.DefaultIfEmpty()
						join c5 in s.GetTable<WmsLoadCarrierDTO>() on b5.ResourceID equals c5.Id into c5n
						from c5 in c5n.DefaultIfEmpty()

						join i5 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b5.ResourceID).Id equals i5.Id into i5n
						from i5 in i5n.DefaultIfEmpty()

						join m5 in s.GetTable<MaterialDTO>() on i5.MaterialID equals m5.Id into m5n
						from m5 in m5n.DefaultIfEmpty()

						let ir5ic = inventoryResourceQry.Count(x => x.ResourceID == c5.Id)
						let ir5lm = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && x.ProductStatus > 0)
						let ror5 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c5.Id)
						let ir5mim = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 6

						join a6 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 6 } equals new { Id = a6.ChannelID, Depth = a6.DepthCoordinate } into a6n
						from a6 in a6n.DefaultIfEmpty()
						join b6 in s.GetTable<RefResourceStorageShelfDTO>() on a6.Id equals b6.StorageShelfID into b6n
						from b6 in b6n.DefaultIfEmpty()
						join c6 in s.GetTable<WmsLoadCarrierDTO>() on b6.ResourceID equals c6.Id into c6n
						from c6 in c6n.DefaultIfEmpty()

						join i6 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b6.ResourceID).Id equals i6.Id into i6n
						from i6 in i6n.DefaultIfEmpty()

						join m6 in s.GetTable<MaterialDTO>() on i6.MaterialID equals m6.Id into m6n
						from m6 in m6n.DefaultIfEmpty()

						let ir6ic = inventoryResourceQry.Count(x => x.ResourceID == c6.Id)
						let ir6lm = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && x.ProductStatus > 0)
						let ror6 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c6.Id)
						let ir6mim = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 7
						
						join a7 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 7 } equals new { Id = a7.ChannelID, Depth = a7.DepthCoordinate } into a7n
						from a7 in a7n.DefaultIfEmpty()
						join b7 in s.GetTable<RefResourceStorageShelfDTO>() on a7.Id equals b7.StorageShelfID into b7n
						from b7 in b7n.DefaultIfEmpty()
						join c7 in s.GetTable<WmsLoadCarrierDTO>() on b7.ResourceID equals c7.Id into c7n
						from c7 in c7n.DefaultIfEmpty()

						join i7 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b7.ResourceID).Id equals i7.Id into i7n
						from i7 in i7n.DefaultIfEmpty()

						join m7 in s.GetTable<MaterialDTO>() on i7.MaterialID equals m7.Id into m7n
						from m7 in m7n.DefaultIfEmpty()

						let ir7ic = inventoryResourceQry.Count(x => x.ResourceID == c7.Id)
						let ir7lm = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && x.ProductStatus > 0)
						let ror7 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c7.Id)
						let ir7mim = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						#region Depth 8

						join a8 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 8 } equals new { Id = a8.ChannelID, Depth = a8.DepthCoordinate } into a8n
						from a8 in a8n.DefaultIfEmpty()
						join b8 in s.GetTable<RefResourceStorageShelfDTO>() on a8.Id equals b8.StorageShelfID into b8n
						from b8 in b8n.DefaultIfEmpty()
						join c8 in s.GetTable<WmsLoadCarrierDTO>() on b8.ResourceID equals c8.Id into c8n
						from c8 in c8n.DefaultIfEmpty()

						join i8 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b8.ResourceID).Id equals i8.Id into i8n
						from i8 in i8n.DefaultIfEmpty()

						join m8 in s.GetTable<MaterialDTO>() on i8.MaterialID equals m8.Id into m8n
						from m8 in m8n.DefaultIfEmpty()

						let ir8ic = inventoryResourceQry.Count(x => x.ResourceID == c8.Id)
						let ir8lm = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && x.ProductStatus > 0)
						let ror8 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c8.Id)
						let ir8mim = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						#endregion

						// commented as it causes stack overflow

						//#region Depth 9

						//join a9 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 9 } equals new { Id = a9.ChannelID, Depth = a9.DepthCoordinate } into a9n
						//from a9 in a9n.DefaultIfEmpty()
						//join b9 in s.GetTable<RefResourceStorageShelfDTO>() on a9.Id equals b9.StorageShelfID into b9n
						//from b9 in b9n.DefaultIfEmpty()
						//join c9 in s.GetTable<WmsLoadCarrierDTO>() on b9.ResourceID equals c9.Id into c9n
						//from c9 in c9n.DefaultIfEmpty()

						//join i9 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b9.ResourceID).Id equals i9.Id into i9n
						//from i9 in i9n.DefaultIfEmpty()

						//join m9 in s.GetTable<MaterialDTO>() on i9.MaterialID equals m9.Id into m9n
						//from m9 in m9n.DefaultIfEmpty()

						//let ir9ic = inventoryResourceQry.Count(x => x.ResourceID == c9.Id)
						//let ir9lm = inventoryResourceQry.Any(x => x.ResourceID == c9.Id && x.ProductStatus > 0)
						//let ror9 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c9.Id)
						//let ir9mim = inventoryResourceQry.Any(x => x.ResourceID == c9.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))

						//#endregion

						//#region Depth 10

						//join a10 in s.GetTable<StorageShelfDTO>() on new { Id = c.Id, Depth = 10 } equals new { Id = a10.ChannelID, Depth = a10.DepthCoordinate } into a10n
						//from a10 in a10n.DefaultIfEmpty()
						//join b10 in s.GetTable<RefResourceStorageShelfDTO>() on a10.Id equals b10.StorageShelfID into b10n
						//from b10 in b10n.DefaultIfEmpty()
						//join c10 in s.GetTable<WmsLoadCarrierDTO>() on b10.ResourceID equals c10.Id into c10n
						//from c10 in c10n.DefaultIfEmpty()

						//join i10 in inventoryResourceQry on inventoryResourceQry.First(dto => dto.ResourceID == b10.ResourceID).Id equals i10.Id into i10n
						//from i10 in i10n.DefaultIfEmpty()

						//join m10 in s.GetTable<MaterialDTO>() on i10.MaterialID equals m10.Id into m10n
						//from m10 in m10n.DefaultIfEmpty()

						//let ir10ic = inventoryResourceQry.Count(x => x.ResourceID == c10.Id)
						//let ir10lm = inventoryResourceQry.Any(x => x.ResourceID == c10.Id && x.ProductStatus > 0)
						//let ror10 = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c10.Id)
						//let ir10mim = inventoryResourceQry.Any(x => x.ResourceID == c10.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == 10)))
						
						//#endregion
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
							} : null),
							Shelf3 = ((a3 != null && a3.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a3.Id,
								LoadCarrierId = c3.Id,
								LoadCarrierResourceLabel = c3.ResourceLabel,
								LoadCarrierStatus = c3.Status,
								LoadCarrierCustomLong2 = c3.CustomLong2,
								LoadCarrierHeightClass = c3.HeightClass,
								LoadCarrierTypeId = c3.TypeID,

								MaterialId = m3.Id,
								MaterialNumber = m3.MaterialNumber,
								MaterialDescription_1 = m3.MaterialDescription_1,
								MaterialCategoryABC = m3.CategoryABC,

								StorageShelfId = a3.Id,
								StorageShelfStatus = a3.Status,
								StorageShelfName = a3.Name,
								StorageShelfHeightClass = a3.HeightClass,
								StorageShelfCategoryABC = a3.CategoryABC,
								StorageShelfAisleNumber = a3.AisleNumber,

								InventoryResourceId = i3.Id,
								InventoryResourceBatchNumber = i3.BatchNumber,
								InventoryResourceProductStatus = i3.ProductStatus,
								InventoryResourceExpiryDate = i3.ExpiryDate,
								InventoryResourceCustomDate1 = i3.CustomDate1,

								InventoryCount = ir3ic,
								ReservedForOutfeed = ror3,
								ManuallyInfeededMaterial = ir3mim
							} : null),
							Shelf4 = ((a4 != null && a4.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a4.Id,
								LoadCarrierId = c4.Id,
								LoadCarrierResourceLabel = c4.ResourceLabel,
								LoadCarrierStatus = c4.Status,
								LoadCarrierCustomLong2 = c4.CustomLong2,
								LoadCarrierHeightClass = c4.HeightClass,
								LoadCarrierTypeId = c4.TypeID,

								MaterialId = m4.Id,
								MaterialNumber = m4.MaterialNumber,
								MaterialDescription_1 = m4.MaterialDescription_1,
								MaterialCategoryABC = m4.CategoryABC,

								StorageShelfId = a4.Id,
								StorageShelfStatus = a4.Status,
								StorageShelfName = a4.Name,
								StorageShelfHeightClass = a4.HeightClass,
								StorageShelfCategoryABC = a4.CategoryABC,
								StorageShelfAisleNumber = a4.AisleNumber,

								InventoryResourceId = i4.Id,
								InventoryResourceBatchNumber = i4.BatchNumber,
								InventoryResourceProductStatus = i4.ProductStatus,
								InventoryResourceExpiryDate = i4.ExpiryDate,
								InventoryResourceCustomDate1 = i4.CustomDate1,

								InventoryCount = ir4ic,
								ReservedForOutfeed = ror4,
								ManuallyInfeededMaterial = ir4mim
							} : null),
							Shelf5 = ((a5 != null && a5.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a5.Id,
								LoadCarrierId = c5.Id,
								LoadCarrierResourceLabel = c5.ResourceLabel,
								LoadCarrierStatus = c5.Status,
								LoadCarrierCustomLong2 = c5.CustomLong2,
								LoadCarrierHeightClass = c5.HeightClass,
								LoadCarrierTypeId = c5.TypeID,

								MaterialId = m5.Id,
								MaterialNumber = m5.MaterialNumber,
								MaterialDescription_1 = m5.MaterialDescription_1,
								MaterialCategoryABC = m5.CategoryABC,

								StorageShelfId = a5.Id,
								StorageShelfStatus = a5.Status,
								StorageShelfName = a5.Name,
								StorageShelfHeightClass = a5.HeightClass,
								StorageShelfCategoryABC = a5.CategoryABC,
								StorageShelfAisleNumber = a5.AisleNumber,

								InventoryResourceId = i5.Id,
								InventoryResourceBatchNumber = i5.BatchNumber,
								InventoryResourceProductStatus = i5.ProductStatus,
								InventoryResourceExpiryDate = i5.ExpiryDate,
								InventoryResourceCustomDate1 = i5.CustomDate1,

								InventoryCount = ir5ic,
								ReservedForOutfeed = ror5,
								ManuallyInfeededMaterial = ir5mim
							} : null),
							Shelf6 = ((a6 != null && a6.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a6.Id,
								LoadCarrierId = c6.Id,
								LoadCarrierResourceLabel = c6.ResourceLabel,
								LoadCarrierStatus = c6.Status,
								LoadCarrierCustomLong2 = c6.CustomLong2,
								LoadCarrierHeightClass = c6.HeightClass,
								LoadCarrierTypeId = c6.TypeID,

								MaterialId = m6.Id,
								MaterialNumber = m6.MaterialNumber,
								MaterialDescription_1 = m6.MaterialDescription_1,
								MaterialCategoryABC = m6.CategoryABC,

								StorageShelfId = a6.Id,
								StorageShelfStatus = a6.Status,
								StorageShelfName = a6.Name,
								StorageShelfHeightClass = a6.HeightClass,
								StorageShelfCategoryABC = a6.CategoryABC,
								StorageShelfAisleNumber = a6.AisleNumber,

								InventoryResourceId = i6.Id,
								InventoryResourceBatchNumber = i6.BatchNumber,
								InventoryResourceProductStatus = i6.ProductStatus,
								InventoryResourceExpiryDate = i6.ExpiryDate,
								InventoryResourceCustomDate1 = i6.CustomDate1,

								InventoryCount = ir6ic,
								ReservedForOutfeed = ror6,
								ManuallyInfeededMaterial = ir6mim
							} : null),
							Shelf7 = ((a7 != null && a7.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a7.Id,
								LoadCarrierId = c7.Id,
								LoadCarrierResourceLabel = c7.ResourceLabel,
								LoadCarrierStatus = c7.Status,
								LoadCarrierCustomLong2 = c7.CustomLong2,
								LoadCarrierHeightClass = c7.HeightClass,
								LoadCarrierTypeId = c7.TypeID,

								MaterialId = m7.Id,
								MaterialNumber = m7.MaterialNumber,
								MaterialDescription_1 = m7.MaterialDescription_1,
								MaterialCategoryABC = m7.CategoryABC,

								StorageShelfId = a7.Id,
								StorageShelfStatus = a7.Status,
								StorageShelfName = a7.Name,
								StorageShelfHeightClass = a7.HeightClass,
								StorageShelfCategoryABC = a7.CategoryABC,
								StorageShelfAisleNumber = a7.AisleNumber,

								InventoryResourceId = i7.Id,
								InventoryResourceBatchNumber = i7.BatchNumber,
								InventoryResourceProductStatus = i7.ProductStatus,
								InventoryResourceExpiryDate = i7.ExpiryDate,
								InventoryResourceCustomDate1 = i7.CustomDate1,

								InventoryCount = ir7ic,
								ReservedForOutfeed = ror7,
								ManuallyInfeededMaterial = ir7mim
							} : null),
							Shelf8 = ((a8 != null && a8.Id != Guid.Empty) ? new ShelfInfoDTO()
							{
								Id = a8.Id,
								LoadCarrierId = c8.Id,
								LoadCarrierResourceLabel = c8.ResourceLabel,
								LoadCarrierStatus = c8.Status,
								LoadCarrierCustomLong2 = c8.CustomLong2,
								LoadCarrierHeightClass = c8.HeightClass,
								LoadCarrierTypeId = c8.TypeID,

								MaterialId = m8.Id,
								MaterialNumber = m8.MaterialNumber,
								MaterialDescription_1 = m8.MaterialDescription_1,
								MaterialCategoryABC = m8.CategoryABC,

								StorageShelfId = a8.Id,
								StorageShelfStatus = a8.Status,
								StorageShelfName = a8.Name,
								StorageShelfHeightClass = a8.HeightClass,
								StorageShelfCategoryABC = a8.CategoryABC,
								StorageShelfAisleNumber = a8.AisleNumber,

								InventoryResourceId = i8.Id,
								InventoryResourceBatchNumber = i8.BatchNumber,
								InventoryResourceProductStatus = i8.ProductStatus,
								InventoryResourceExpiryDate = i8.ExpiryDate,
								InventoryResourceCustomDate1 = i8.CustomDate1,

								InventoryCount = ir8ic,
								ReservedForOutfeed = ror8,
								ManuallyInfeededMaterial = ir8mim
							} : null),
							//Shelf9 = ((a9 != null && a9.Id != Guid.Empty) ? new ShelfInfoDTO()
							//{
							//	Id = a9.Id,
							//	LoadCarrierId = c9.Id,
							//	LoadCarrierResourceLabel = c9.ResourceLabel,
							//	LoadCarrierStatus = c9.Status,
							//	LoadCarrierCustomLong2 = c9.CustomLong2,
							//	LoadCarrierHeightClass = c9.HeightClass,
							//	LoadCarrierTypeId = c9.TypeID,

							//	MaterialId = m9.Id,
							//	MaterialNumber = m9.MaterialNumber,
							//	MaterialDescription_1 = m9.MaterialDescription_1,
							//	MaterialCategoryABC = m9.CategoryABC,

							//	StorageShelfId = a9.Id,
							//	StorageShelfStatus = a9.Status,
							//	StorageShelfName = a9.Name,
							//	StorageShelfHeightClass = a9.HeightClass,
							//	StorageShelfCategoryABC = a9.CategoryABC,
							//	StorageShelfAisleNumber = a9.AisleNumber,

							//	InventoryResourceId = i9.Id,
							//	InventoryResourceBatchNumber = i9.BatchNumber,
							//	InventoryResourceProductStatus = i9.ProductStatus,
							//	InventoryResourceExpiryDate = i9.ExpiryDate,
							//	InventoryResourceCustomDate1 = i9.CustomDate1,

							//	InventoryCount = ir9ic,
							//	ReservedForOutfeed = ror9,
							//	ManuallyInfeededMaterial = ir9mim
							//} : null),
							//Shelf10 = ((a10 != null && a10.Id != Guid.Empty) ? new ShelfInfoDTO()
							//{
							//	Id = a10.Id,
							//	LoadCarrierId = c10.Id,
							//	LoadCarrierResourceLabel = c10.ResourceLabel,
							//	LoadCarrierStatus = c10.Status,
							//	LoadCarrierCustomLong2 = c10.CustomLong2,
							//	LoadCarrierHeightClass = c10.HeightClass,
							//	LoadCarrierTypeId = c10.TypeID,

							//	MaterialId = m10.Id,
							//	MaterialNumber = m10.MaterialNumber,
							//	MaterialDescription_1 = m10.MaterialDescription_1,
							//	MaterialCategoryABC = m10.CategoryABC,

							//	StorageShelfId = a10.Id,
							//	StorageShelfStatus = a10.Status,
							//	StorageShelfName = a10.Name,
							//	StorageShelfHeightClass = a10.HeightClass,
							//	StorageShelfCategoryABC = a10.CategoryABC,
							//	StorageShelfAisleNumber = a10.AisleNumber,

							//	InventoryResourceId = i10.Id,
							//	InventoryResourceBatchNumber = i10.BatchNumber,
							//	InventoryResourceProductStatus = i10.ProductStatus,
							//	InventoryResourceExpiryDate = i10.ExpiryDate,
							//	InventoryResourceCustomDate1 = i10.CustomDate1,

							//	InventoryCount = ir10ic,
							//	ReservedForOutfeed = ror10,
							//	ManuallyInfeededMaterial = ir10mim
							//} : null)
						};

			var res = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2 }).ToList();
			var res2 = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2, Shelf3 = x.Shelf3, Shelf4 = x.Shelf4, Shelf5 = x.Shelf5, Shelf6 = x.Shelf6, Shelf7 = x.Shelf7, Shelf8 = x.Shelf8/*, Shelf9 = x.Shelf9, Shelf10 = x.Shelf10*/ });

		}
	}
}
