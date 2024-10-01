using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3305Tests : TestBase
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
			public virtual int MaterialID { get; set; }
		}

		public class ShelfInfoDTO
		{
			public virtual int Id { get; set; }
			public virtual WmsLoadCarrierDTO? LoadCarrier { get; set; }
			public virtual InventoryResourceDTO? InventoryResource { get; set; }
			public virtual MaterialDTO? Material { get; set; }
			public virtual StorageShelfDTO? StorageShelf { get; set; }
			public virtual int InventoryCount { get; set; }
			public virtual int LockedMaterial { get; set; }
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
			public virtual int Id { get; set; }
			public virtual int Status { get; set; }
			public virtual int ResourceID { get; set; }
			public virtual int ProductStatus { get; set; }
			public virtual int BatchNumber { get; set; }
			public virtual int BundleUnit { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int MaterialID { get; set; }
			public virtual int? InfeedAdviceID { get; set; }
		}

		public class MaterialDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int MaterialNumber { get; set; }
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
			public virtual int Id { get; set; }
			public virtual int Status { get; set; }
			public virtual int ResourceLabel { get; set; }
			public virtual int CustomField1 { get; set; }
			public virtual int CustomField2 { get; set; }
			public virtual int CustomField3 { get; set; }
			public virtual int CustomLong1 { get; set; }
			public virtual int CustomLong2 { get; set; }
			public virtual int CustomLong3 { get; set; }
		}

		public class RefResourceStorageShelfDTO
		{
			public virtual int Id { get; set; }
			public virtual int StorageShelfID { get; set; }
			public virtual int ResourceID { get; set; }
		}

		public class RefOutfeedTransportOrderResourceDTO
		{
			public virtual int Id { get; set; }
			public virtual int ResourceID { get; set; }
		}

		public class StorageShelfDTO
		{
			[NotColumn]
			public virtual bool StrippedDownDTO { get; set; }
			public virtual int Id { get; set; }
			public virtual int ChannelID { get; set; }
			public virtual int Status { get; set; }
			public virtual int CategoryABC { get; set; }
			public virtual int HeightClass { get; set; }
			public virtual int DepthCoordinate { get; set; }
		}

		[ActiveIssue(Configuration = TestProvName.AllSqlServer)]
		[Test]
		public void TestComplexQueryWms([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var s = new DataConnection(context);
			using var t1 = s.CreateTempTable<ChannelDTO>();
			using var t2 = s.CreateTempTable<InventoryResourceDTO>();
			using var t3 = s.CreateTempTable<MaterialDTO>();
			using var t4 = s.CreateTempTable<WmsLoadCarrierDTO>();
			using var t5 = s.CreateTempTable<RefResourceStorageShelfDTO>();
			using var t6 = s.CreateTempTable<AisleDTO>();
			using var t7 = s.CreateTempTable<RefOutfeedTransportOrderResourceDTO>();
			using var t8 = s.CreateTempTable<InfeedAdvicePositionDTO>();
			using var t9 = s.CreateTempTable<StorageShelfDTO>();

			var channelQry = s.GetTable<ChannelDTO>();
			var inventoryResourceQry = s.GetTable<InventoryResourceDTO>().Where(x => x.Status < 99);
			var materialQry = s.GetTable<MaterialDTO>().HasUniqueKey(m => m.Id);
			var resourceQry = s.GetTable<WmsLoadCarrierDTO>().HasUniqueKey(c => c.Id);
			var refResourceStorageShelfDtos = s.GetTable<RefResourceStorageShelfDTO>().HasUniqueKey(b => b.StorageShelfID).HasUniqueKey(b => b.ResourceID);
			var storageShelfDtos = s.GetTable<StorageShelfDTO>().HasUniqueKey(f => new { f.ChannelID, f.DepthCoordinate });

			var inventoryResourceQryUsed = inventoryResourceQry
			   .Select(ir => new
			   {
				   RN = Sql.Ext.RowNumber().Over().PartitionBy(ir.ResourceID).OrderBy(ir.ResourceID).ToValue(),
				   Count = Sql.Ext.Count().Over().PartitionBy(ir.ResourceID).ToValue(),
				   CountLocked = Sql.Ext.Count(ir.ProductStatus > 0).Over().PartitionBy(ir.ResourceID).ToValue(),
				   IR = ir
			   }).AsCte()
			   .HasUniqueKey(ir => new { ir.IR.ResourceID, ir.RN });

			var query = from c in channelQry
						from a in s.GetTable<AisleDTO>().InnerJoin(a => c.AisleID == a.Id)
						from m in materialQry.LeftJoin(m => c.MaterialID == m.Id)

							#region Depth 1

						from a1 in storageShelfDtos.LeftJoin(a1 => c.Id == a1.ChannelID && 1 == a1.DepthCoordinate)
						from b1 in refResourceStorageShelfDtos.LeftJoin(b1 => a1.Id == b1.StorageShelfID)
						from c1 in resourceQry.LeftJoin(c1 => c1.Id == b1.ResourceID)
						from i1 in inventoryResourceQryUsed.LeftJoin(i1 => i1.IR.ResourceID == b1.ResourceID && i1.RN == 1)
						from m1 in materialQry.LeftJoin(m1 => m1.Id == i1.IR.MaterialID)

							#endregion

							#region Depth 2

						from a2 in storageShelfDtos.LeftJoin(a2 => c.Id == a2.ChannelID && 2 == a2.DepthCoordinate)
						from b2 in refResourceStorageShelfDtos.LeftJoin(b2 => a2.Id == b2.StorageShelfID)
						from c2 in resourceQry.LeftJoin(c2 => c2.Id == b2.ResourceID)
						from i2 in inventoryResourceQryUsed.LeftJoin(i2 => i2.IR.ResourceID == b2.ResourceID && i2.RN == 1)
						from m2 in materialQry.LeftJoin(m2 => m2.Id == i2.IR.MaterialID)

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
							Shelf1 = (a1 != null ? new ShelfInfoDTO()
							{
								Id = a1.Id,
								LoadCarrier = new WmsLoadCarrierDTO { StrippedDownDTO = true, Id = c1.Id, Status = c1.Status, ResourceLabel = c1.ResourceLabel, CustomField1 = c1.CustomField1, CustomField2 = c1.CustomField2, CustomField3 = c1.CustomField3, CustomLong1 = c1.CustomLong1, CustomLong2 = c1.CustomLong2, CustomLong3 = c1.CustomLong3 },
								Material = new MaterialDTO { StrippedDownDTO = true, Id = m1.Id, MaterialNumber = m1.MaterialNumber, MaterialDescription_1 = m1.MaterialDescription_1, MaterialDescription_2 = m1.MaterialDescription_2, MaterialDescription_3 = m1.MaterialDescription_3, CategoryABC = m1.CategoryABC, CategoryCustoms = m1.CategoryCustoms, CategoryDimensions = m1.CategoryDimensions, CategoryQuality = m1.CategoryQuality, CategoryTemperature = m1.CategoryTemperature },
								InventoryResource = new InventoryResourceDTO { StrippedDownDTO = true, Id = i1.IR.Id, BatchNumber = i1.IR.BatchNumber, BundleUnit = i1.IR.BundleUnit, ProductStatus = i1.IR.ProductStatus, CustomField1 = i1.IR.CustomField1, CustomField2 = i1.IR.CustomField2, CustomField3 = i1.IR.CustomField3 },
								StorageShelf = new StorageShelfDTO { StrippedDownDTO = true, Id = a1.Id, Status = a1.Status, CategoryABC = a1.CategoryABC, HeightClass = a1.HeightClass },
								InventoryCount = (int)i1.Count,
								LockedMaterial = i1.CountLocked,
								ReservedForOutfeed = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c1.Id),
								ManuallyInfeededMaterial = inventoryResourceQryUsed.Any(x => x.IR.ResourceID == c1.Id && (x.IR.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.IR.InfeedAdviceID && y.InfeedAdviceType ==1)))
							} : null),
							Shelf2 = (a2 != null ? new ShelfInfoDTO()
							{
								Id = a2.Id,
								LoadCarrier = new WmsLoadCarrierDTO { StrippedDownDTO = true, Id = c2.Id, Status = c2.Status, ResourceLabel = c2.ResourceLabel, CustomField1 = c2.CustomField1, CustomField2 = c2.CustomField2, CustomField3 = c2.CustomField3, CustomLong1 = c2.CustomLong1, CustomLong2 = c2.CustomLong2, CustomLong3 = c2.CustomLong3 },
								Material = new MaterialDTO { StrippedDownDTO = true, Id = m2.Id, MaterialNumber = m2.MaterialNumber, MaterialDescription_1 = m2.MaterialDescription_1, MaterialDescription_2 = m2.MaterialDescription_2, MaterialDescription_3 = m2.MaterialDescription_3, CategoryABC = m2.CategoryABC, CategoryCustoms = m2.CategoryCustoms, CategoryDimensions = m2.CategoryDimensions, CategoryQuality = m2.CategoryQuality, CategoryTemperature = m2.CategoryTemperature },
								InventoryResource = new InventoryResourceDTO { StrippedDownDTO = true, Id = i2.IR.Id, BatchNumber = i2.IR.BatchNumber, BundleUnit = i2.IR.BundleUnit, ProductStatus = i2.IR.ProductStatus, CustomField1 = i2.IR.CustomField1, CustomField2 = i2.IR.CustomField2, CustomField3 = i2.IR.CustomField3 },
								StorageShelf = new StorageShelfDTO { StrippedDownDTO = true, Id = a2.Id, Status = a2.Status, CategoryABC = a2.CategoryABC, HeightClass = a2.HeightClass },
								InventoryCount = (int)i2.Count,
								LockedMaterial = i2.CountLocked,
								ReservedForOutfeed = s.GetTable<RefOutfeedTransportOrderResourceDTO>().Any(x => x.ResourceID == c2.Id),
								ManuallyInfeededMaterial = inventoryResourceQryUsed.Any(x => x.IR.ResourceID == c2.Id && (x.IR.InfeedAdviceID == null || s.GetTable<InfeedAdvicePositionDTO>().Any(y => y.Id == x.IR.InfeedAdviceID && y.InfeedAdviceType ==1)))
							} : null),
						};

			var maxDepth = 1;
			var qry = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1 });

			if (maxDepth > 1)
				qry = query.Select(x => new ChannelInfoCombinedDTO() { Id = x.Id, ChannelInfo = x.ChannelInfo, Shelf1 = x.Shelf1, Shelf2 = x.Shelf2 });

			var lst = qry.ToList();

			var sql = s.LastQuery;
		}
	}
}
