using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class BulkUpdateTests : TestBase
	{
		public class BulkUpdateEntity
		{
			private sealed class BulkUpdateEntityEqualityComparer : IEqualityComparer<BulkUpdateEntity>
			{
				public bool Equals(BulkUpdateEntity x, BulkUpdateEntity y)
				{
					if (ReferenceEquals(x, y)) return true;
					if (ReferenceEquals(x, null)) return false;
					if (ReferenceEquals(y, null)) return false;
					if (x.GetType() != y.GetType()) return false;
					return x.ID1 == y.ID1 && x.ID2 == y.ID2 && x.Value1 == y.Value1 && x.Value2 == y.Value2;
				}

				public int GetHashCode(BulkUpdateEntity obj)
				{
					unchecked
					{
						var hashCode = obj.ID1;
						hashCode = (hashCode * 397) ^ obj.ID2;
						hashCode = (hashCode * 397) ^ obj.Value1;
						hashCode = (hashCode * 397) ^ obj.Value2;
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<BulkUpdateEntity> BulkUpdateEntityComparer { get; } = new BulkUpdateEntityEqualityComparer();

			[PrimaryKey(1)]
			public int ID1 { get; set; }

			[PrimaryKey(2)]
			public int ID2 { get; set; }

			public int Value1 { get; set; }
			public int Value2 { get; set; }
		}


		public static BulkUpdateEntity[] GenerateEntities(int count, int valuesSeed)
		{
			return Enumerable.Range(1, count)
				.Select(v => new BulkUpdateEntity { ID1 = v, ID2 = -v, Value1 = valuesSeed + v, Value2 = valuesSeed + v * 10 })
				.ToArray();
		}

		[Test, Combinatorial]
		public void BulkUpdatePureTest([IncludeDataSources(
			ProviderName.MySql
			)] string context)
		{
			using (var db = GetDataContext(context))
			using (var target = db.CreateLocalTable(GenerateEntities(10, 0)))
			using (var temp   = db.CreateLocalTable("tempTable", GenerateEntities(10, 100)))
			{
				var join = target.InnerJoin(temp, (o, i) => o.ID1 == i.ID1 && o.ID2 == i.ID2,
					(o, i) => new DynamicExtensions.JoinHelper<BulkUpdateEntity, BulkUpdateEntity>
					{
						Outer = o,
						Inner = i
					});

				var updatable = join.AsUpdatable();
				updatable = updatable.Set(pair => pair.Outer.Value1, pair => pair.Inner.Value1);
				updatable = updatable.Set(pair => pair.Outer.Value2, pair => pair.Inner.Value2);
				updatable.Update();

				AreEqual(target, temp, BulkUpdateEntity.BulkUpdateEntityComparer);
			}
		}

		[Test, Combinatorial]
		public void BulkUpdateDynamicTest([IncludeDataSources(false,
			ProviderName.MySql
			)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var target = db.CreateLocalTable(GenerateEntities(10, 0)))
			{
				var newEntities = GenerateEntities(10, 100);
				db.BulkUpdate(newEntities, target);

				AreEqual(target, newEntities, BulkUpdateEntity.BulkUpdateEntityComparer);
			}
		}

	}
}
