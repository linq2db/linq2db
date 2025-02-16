using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4394Tests : TestBase
	{
		[Table]
		public class AisleDTO
		{
			[Column] public int Id { get; set; }
			[Column] public int AisleNumber { get; set; }
			[Column] public int PlantID { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public bool StrippedDownDTO { get; set; }
		}

		[Table]
		public class ChannelDTO
		{
			[Column] public int Id { get; set; }

			[Column] public int AisleID { get; set; }

			[Column] public int MaterialID { get; set; }
		}

		public enum InventoryResourceStatus
		{
			Finished
		}

		[Table]
		public class InventoryResourceDTO
		{
			[Column] public int Id { get; set; }
			[Column] public int? InfeedAdviceID { get; set; }
			[Column] public int Quantity { get; set; }
			[Column] public int ResourceID { get; set; }
			[Column] public int MaterialID { get; set; }
			[Column] public int ProductStatus { get; set; }
			[Column] public string? BatchNumber { get; set; }
			[Column] public DateTime? ExpiryDate { get; set; }
			[Column] public DateTime? CustomDate1 { get; set; }
			[Column] public InventoryResourceStatus Status { get; set; }
		}

		[Table]
		public class MaterialDTO
		{
			[Column] public int Id { get; set; }
			[Column] public int MaterialNumber { get; set; }
			[Column] public bool StrippedDownDTO { get; set; }
			[Column] public string? MaterialDescription_1 { get; set; }
			[Column] public string? MaterialDescription_2 { get; set; }
			[Column] public string? MaterialDescription_3 { get; set; }
			[Column] public string? CategoryABC { get; set; }
			[Column] public string? CategoryCustoms { get; set; }
			[Column] public string? CategoryDimensions { get; set; }
			[Column] public string? CategoryQuality { get; set; }
			[Column] public string? CategoryTemperature { get; set; }
		}

		[Table]
		public class RefOutfeedTransportOrderResourceDTO
		{
			[Column] public int Id { get; set; }

			[Column] public int ResourceID { get; set; }
		}

		[Table]
		public class WmsLoadCarrierDTO
		{
			[Column] public int Id { get; set; }
			[Column] public string? ResourceLabel { get; set; }
			[Column] public int Status { get; set; }
			[Column] public long CustomLong2 { get; set; }
			[Column] public int HeightClass { get; set; }
			[Column] public int TypeID { get; set; }
		}

		[Table]
		public class StorageShelfDTO
		{
			[Column] public Guid Id { get; set; }
			[Column] public int ChannelID { get; set; }
			[Column] public int DepthCoordinate { get; set; }
			[Column] public int Status { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public int HeightClass { get; set; }
			[Column] public string? CategoryABC { get; set; }
			[Column] public int AisleNumber { get; set; }
		}

		[Table]
		public class RefResourceStorageShelfDTO
		{
			[Column] public int Id { get; set; }

			[Column] public Guid StorageShelfID { get; set; }

			[Column] public int ResourceID { get; set; }
		}

		public enum InfeedAdviceType
		{
			ManualGenerated
		}

		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column] public int Id { get; set; }

			[Column] public int Nr { get; set; }

			[Column] public InfeedAdviceType InfeedAdviceType { get; set; }
		}

		public class ChannelInfoDTO
		{
			public int Id { get; set; }
			public ChannelDTO? Channel { get; set; }
			public AisleDTO? Aisle { get; set; }
			public MaterialDTO? Material { get; set; }
		}

		public class ShelfInfoDTO
		{
			public Guid Id { get; set; }
			public int LoadCarrierId { get; set; }
			public string? LoadCarrierResourceLabel { get; set; }
			public int LoadCarrierStatus { get; set; }
			public long LoadCarrierCustomLong2 { get; set; }
			public int LoadCarrierHeightClass { get; set; }
			public int LoadCarrierTypeId { get; set; }
			public int MaterialId { get; set; }
			public int MaterialNumber { get; set; }
			public string? MaterialDescription_1 { get; set; }
			public string? MaterialCategoryABC { get; set; }
			public Guid StorageShelfId { get; set; }
			public int StorageShelfStatus { get; set; }
			public string? StorageShelfName { get; set; }
			public int StorageShelfHeightClass { get; set; }
			public string? StorageShelfCategoryABC { get; set; }
			public int StorageShelfAisleNumber { get; set; }
			public int InventoryResourceId { get; set; }
			public string? InventoryResourceBatchNumber { get; set; }
			public int InventoryResourceProductStatus { get; set; }
			public DateTime? InventoryResourceExpiryDate { get; set; }
			public DateTime? InventoryResourceCustomDate1 { get; set; }
			public int InventoryCount { get; set; }
			public bool ReservedForOutfeed { get; set; }
			public bool ManuallyInfeededMaterial { get; set; }
		}

		public class ChannelInfoCombinedDTO
		{
			public int Id { get; set; }
			public ChannelInfoDTO? ChannelInfo { get; set; }
			public ShelfInfoDTO? Shelf1 { get; set; }
			public ShelfInfoDTO? Shelf2 { get; set; }
			public ShelfInfoDTO? Shelf3 { get; set; }
			public ShelfInfoDTO? Shelf4 { get; set; }
			public ShelfInfoDTO? Shelf5 { get; set; }
			public ShelfInfoDTO? Shelf6 { get; set; }
			public ShelfInfoDTO? Shelf7 { get; set; }
			public ShelfInfoDTO? Shelf8 { get; set; }
		}

		class QueryStartedInterceptor : LinqToDB.Interceptors.CommandInterceptor
		{
			public Action? Action { get; set; }

			public override Option<DbDataReader> ExecuteReader(CommandEventData eventData, DbCommand command, CommandBehavior commandBehavior, Option<DbDataReader> result)
			{
				Action?.Invoke();
				return base.ExecuteReader(eventData, command, commandBehavior, result);
			}
		}

		[Test]
		public void QueryTakesForever4394([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using (var s = GetDataContext(context))
			using (s.CreateLocalTable<AisleDTO>())
			using (s.CreateLocalTable<ChannelDTO>())
			using (s.CreateLocalTable<InventoryResourceDTO>())
			using (s.CreateLocalTable<MaterialDTO>())
			using (s.CreateLocalTable<RefOutfeedTransportOrderResourceDTO>())
			using (s.CreateLocalTable<WmsLoadCarrierDTO>())
			using (s.CreateLocalTable<StorageShelfDTO>())
			using (s.CreateLocalTable<RefResourceStorageShelfDTO>())
			using (s.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				var channelQry = s.GetTable<ChannelDTO>();
				var inventoryResourceQry = s.GetTable<InventoryResourceDTO>().Where(x => x.Status < InventoryResourceStatus.Finished);
				var materialQry = s.GetTable<MaterialDTO>();
				var resourceQry = s.GetTable<WmsLoadCarrierDTO>();

				#region query
				var query = from c in channelQry
							join a in s.GetTable<AisleDTO>() on c.AisleID equals a.Id

							join m in s.GetTable<MaterialDTO>() on c.MaterialID equals m.Id into mn
							from m in mn.DefaultIfEmpty()

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
							let ir1mim = inventoryResourceQry.Any(x => x.ResourceID == c1.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir2mim = inventoryResourceQry.Any(x => x.ResourceID == c2.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir3mim = inventoryResourceQry.Any(x => x.ResourceID == c3.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir4mim = inventoryResourceQry.Any(x => x.ResourceID == c4.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir5mim = inventoryResourceQry.Any(x => x.ResourceID == c5.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir6mim = inventoryResourceQry.Any(x => x.ResourceID == c6.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir7mim = inventoryResourceQry.Any(x => x.ResourceID == c7.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

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
							let ir8mim = inventoryResourceQry.Any(x => x.ResourceID == c8.Id && (x.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.InfeedAdviceID && y.InfeedAdviceType == InfeedAdviceType.ManualGenerated)))

							#endregion

						   
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
							};

				var i = new QueryStartedInterceptor();
				s.AddInterceptor(i);
				var sw = Stopwatch.StartNew();
				var sw2 = Stopwatch.StartNew();
				i.Action = () => sw.Stop();
				var res = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1 }).ToList();
				var gt1 = sw2.ElapsedMilliseconds;
				var t1 = sw.ElapsedMilliseconds;

				sw = Stopwatch.StartNew();
				sw2 = Stopwatch.StartNew();
				i.Action = () => sw.Stop();
				var res8 = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2, Shelf3 = x.Shelf3, Shelf4 = x.Shelf4, Shelf5 = x.Shelf5, Shelf6 = x.Shelf6, Shelf7 = x.Shelf7, Shelf8 = x.Shelf8 }).ToList();
				var gt8 = sw2.ElapsedMilliseconds;
				var t8 = sw.ElapsedMilliseconds;
			}
		}
	}
}
