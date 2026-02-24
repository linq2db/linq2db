using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5362Tests : TestBase
	{
		public enum AisleStatus
		{
			Available,
			OutOfOrder,
		}

		public enum OptimizationLevel
		{
			Off,
			On
		}

		public enum ChannelStatus
		{
			Available,
		}

		[Table]
		public class AisleDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column] public AisleStatus Status { get; set; }
			[Column] public AisleStatus GroupStatus { get; set; }
			[Column] public OptimizationLevel OptimizationMaxLevel { get; set; }

			[Column] public string? AdditionalField1 { get; set; }
			[Column] public string? AdditionalField2 { get; set; }
			[Column] public string? AdditionalField3 { get; set; }
		}

		[Table]
		public class RefResPointAisleDTO
		{
			[Column(IsPrimaryKey = true)] public Guid AisleId { get; set; }
			[Column(IsPrimaryKey = true)] public Guid ResourcePointId { get; set; }
		}

		[Table]
		public class WmsResourcePointDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column] public int Nr { get; set; }
			[Column] public bool IsSrm { get; set; }
			[Column] public bool OutOfOrder { get; set; }
		}

		[Table]
		public class StorageShelfDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column] public Guid AisleID { get; set; }
			[Column] public bool ServerOnlyText { get; set; }
			[Column] public int DepthCoordinate { get; set; }
		}

		[Table]
		public class ChannelDTO
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column] public ChannelStatus Status { get; set; }
			[Column] public Guid AisleID { get; set; }
		}

		public class SRMInfeedPoint
		{
			public AisleDTO? Aisle;
			public readonly WmsResourcePointDTO WmsResourcePoint;
			public int SumCharge;
			public int SumMaterial;
			public int SumPallets;
			public int MaxAisleBalanceDifference;
			public int MaxDepthAvailableShelfs;
			public int MaxDepthAllShelfs;
			public int MaxHeightClassAllShelfs;
			public int MaxWidthClassAllShelfs;
			public int MaxLengthClassAllShelfs;
			public decimal? MaxWeightAllShelfs;
			public int CountFreeShelfs;
			public Dictionary<int, int>? CountFreeShelfsAtHeightClass;
			public int CountFreeChannels;
			public int FreeMoves;

			public SRMInfeedPoint(AisleDTO aisle, WmsResourcePointDTO wmsResourcePoint, int maxAisleBalanceDifference, int countFreeChannels)
			{
				Aisle = aisle;
				WmsResourcePoint = wmsResourcePoint;
				SumCharge = 0;
				SumMaterial = 0;
				SumPallets = 0;
				MaxAisleBalanceDifference = maxAisleBalanceDifference <= 0 ? 1 : maxAisleBalanceDifference;
				MaxDepthAvailableShelfs = Int32.MaxValue;
				MaxDepthAllShelfs = Int32.MaxValue;
				MaxHeightClassAllShelfs = Int32.MaxValue;
				MaxWidthClassAllShelfs = Int32.MaxValue;
				MaxLengthClassAllShelfs = Int32.MaxValue;
				MaxWeightAllShelfs = null;
				CountFreeShelfs = 0;
				CountFreeChannels = countFreeChannels;
				FreeMoves = Int32.MaxValue;
				CountFreeShelfsAtHeightClass = null;
			}
		}

		[Test]
		public void TestQueryWithGroupBy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var aisleId = TestData.Guid1;
			var rpId    = TestData.Guid2;
			var ssId    = TestData.Guid3;
			var chId    = TestData.Guid4;

			using var aisle = db.CreateLocalTable<AisleDTO>([new AisleDTO() { Id = aisleId, AdditionaField1 = "test", OptimizationMaxLevel = OptimizationLevel.On }]);
			using var refTable = db.CreateLocalTable<RefResPointAisleDTO>([new RefResPointAisleDTO() { AisleId = aisleId, ResourcePointId = rpId }]);
			using var rps = db.CreateLocalTable<WmsResourcePointDTO>([new WmsResourcePointDTO() { Id = rpId, IsSrm = true }]);
			using var shelfs = db.CreateLocalTable<StorageShelfDTO>([new StorageShelfDTO() { Id = ssId, AisleID = aisleId }]);
			using var channels = db.CreateLocalTable<ChannelDTO>([new ChannelDTO() { Id = chId, AisleID = aisleId }]);

			var dict = (
				from a in db.GetTable<AisleDTO>()
				join refrpa in db.GetTable<RefResPointAisleDTO>() on a.Id equals refrpa.AisleId
				join rp in db.GetTable<WmsResourcePointDTO>() on refrpa.ResourcePointId equals rp.Id
				join ss in db.GetTable<StorageShelfDTO>() on a.Id equals ss.AisleID
				where a.Status != AisleStatus.OutOfOrder &&
					  a.GroupStatus != AisleStatus.OutOfOrder &&
					  a.OptimizationMaxLevel > OptimizationLevel.Off &&
					  rp.IsSrm &&
					  !rp.OutOfOrder
				group new { a, rp, ss } by new { a, rp } into g
				select new SRMInfeedPoint(
					g.Key.a,
					g.Key.rp,
					g.Max(x => x.ss.DepthCoordinate),
					(from c in db.GetTable<ChannelDTO>()
					 where c.Status == ChannelStatus.Available &&
						   c.AisleID == g.Key.a.Id
					 select c).Count())
				).ToDictionary(srm => srm.Aisle!.Id);

			dict!.First().Value!.Aisle!.AdditionaField1.ShouldBe("test");
		}
	}
}
