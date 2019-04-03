﻿using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Tools;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1412Tests : TestBase
	{
		public class BasicDTO : BasicDTOwithoutID
		{
			public Guid Id { get; set; }
		}

		public class BasicDTOwithExtensionData : BasicDTO
		{ }

		public class BasicDTOwithoutID
		{ }

		public abstract class WmsBasicDTO<T> : WmsBasicWithoutCustomFieldsDTO<T>
		{ }

		public abstract class WmsBasicWithoutCustomFieldsDTO<T> : BasicDTOwithExtensionData
		{ }

		public enum ResourceStatus
		{
			Undefined = 0,
			Captured = 10
		}

		public class WmsLoadCarrierDTO : WmsBasicDTO<WmsLoadCarrierDTO>
		{
			public ResourceStatus Status { get; set; }

			public string ResourceLabel { get; set; }

			public string ResourceLabelNVE { get; set; }

			public Guid? ParentResourceID { get; set; }

			public Guid? TypeID { get; set; }

			public int? HeightClass { get; set; }

			public Decimal? CurrentWeightOfResource { get; set; }

			public int? WidthClass { get; set; }

			public int? LengthClass { get; set; }

			public Guid? OriginalResourceID { get; set; }

			public Guid? LastGlobalTaskID { get; set; }

			public DateTime? WashingDate { get; set; }

			public Guid? ResourcePointID { get; set; }

			public decimal? Height { get; set; }

			public decimal? Width { get; set; }

			public decimal? Length { get; set; }

			public string TechnicalValues { get; set; }

			public int RearrangementCount { get; set; }

			public bool IsVirtual { get; set; }

			public string ErrorMessage { get; set; }

			public decimal? FillingDegree { get; set; }

			public DateTime? LastInventoryCheckTimeStamp { get; set; }

			public string Segmentation { get; set; }

			public bool DontTouch { get; set; }

			protected bool Equals(WmsLoadCarrierDTO other)
			{
				return Id.Equals(other.Id) && TypeID.Equals(other.TypeID);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((WmsLoadCarrierDTO)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Id.GetHashCode() * 397) ^ TypeID.GetHashCode();
				}
			}
		}

		public class WmsResourceCombinedDTO 
		{
			public WmsLoadCarrierDTO LoadCarrier { get; set; }

			public WmsResourceTypeDTO ResourceType { get; set; }
		}

		public class WmsResourceTypeDTO : BasicDTOwithExtensionData
		{
			public string Name { get; set; }

			public string ShortName { get; set; }

			public int Height { get; set; }

			public int Depth { get; set; }

			public int Width { get; set; }

			protected bool Equals(WmsResourceTypeDTO other)
			{
				return Id.Equals(other.Id) && string.Equals(Name, other.Name) && string.Equals(ShortName, other.ShortName) && Height == other.Height && Depth == other.Depth && Width == other.Width;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((WmsResourceTypeDTO)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = Id.GetHashCode();
					hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (ShortName != null ? ShortName.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ Height;
					hashCode = (hashCode * 397) ^ Depth;
					hashCode = (hashCode * 397) ^ Width;
					return hashCode;
				}
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var typeId = Guid.NewGuid();

			var resources = new[]{ new WmsResourceTypeDTO{Depth = 256, Height = 110, Id = typeId, Name = "Resource Name", ShortName = "RN", Width = 333 } };

			var carriersA = new[] { new WmsLoadCarrierDTO { Id = Guid.NewGuid(), TypeID = typeId } };
			var carriersB = new[] { new WmsLoadCarrierDTO { Id = Guid.NewGuid(), TypeID = typeId } };

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<WmsResourceTypeDTO>(resources))
			using (db.CreateLocalTable<WmsLoadCarrierDTO>(carriersA))
			using (db.CreateLocalTable<WmsLoadCarrierDTO>("WMS_ResourceA", carriersB))
			{
				var qryUnion = from res in db.GetTable<WmsLoadCarrierDTO>().Union(db.GetTable<WmsLoadCarrierDTO>().TableName("WMS_ResourceA"))
					join type in db.GetTable<WmsResourceTypeDTO>() on res.TypeID equals type.Id into tpList
					from tp in tpList.DefaultIfEmpty()
					select new WmsResourceCombinedDTO { LoadCarrier = res, ResourceType = tp };

				var staticResult = from res in carriersA.Union(carriersB)
					join type in resources on res.TypeID equals type.Id into tpList
					from tp in tpList.DefaultIfEmpty()
					select new WmsResourceCombinedDTO { LoadCarrier = res, ResourceType = tp };

				var actual   = qryUnion.ToArray();
				var expected = staticResult.ToArray();

				AreEqualWithComparer(expected, actual);
			}
		}
	}
}
