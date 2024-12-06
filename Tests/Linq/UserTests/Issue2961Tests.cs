using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2961Tests : TestBase
	{
		[Table(Name = "Condos")]
		public class Condo
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey]
			public int Id { get; set; } // int

			[Column(DataType = DataType.Int32)]
			[NotNull]
			public int LocationId { get; set; } // int
		}

		[Table(Name = "CategoryCondos")]
		public class CategoryCondo
		{
			[Column]
			[PrimaryKey(1)]
			public int CategoryId { get; set; } // uniqueidentifier

			[Column(DataType = DataType.Int32)]
			[PrimaryKey(2)]
			[NotNull]
			public int CondoId { get; set; } // int
		}

		[Table(Name = "CondoTags")]
		public class CondoTag
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey(1)]
			[NotNull]
			public int CondoId { get; set; } // int

			[Column(DataType = DataType.Int32)]
			[PrimaryKey(2)]
			[NotNull]
			public int TagId { get; set; } // int
		}

		[Table(Name = "Locations")]
		public class Location
		{
			[Column(DataType = DataType.Int32)]
			[PrimaryKey]
			public int Id { get; set; } // int

			[Column(DataType = DataType.NVarChar, Length = 100)]
			[NotNull]
			public string LocationName { get; set; } = null!; // nvarchar(100)
		}

		[Test]
		public void CountEqualityTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Condo>())
			using (db.CreateLocalTable<CategoryCondo>())
			using (db.CreateLocalTable<CondoTag>())
			using (db.CreateLocalTable<Location>())
			{
				var condoCategories = from ccr in db.GetTable<CategoryCondo>()
					group ccr by ccr.CondoId
					into g1
					select new {CondoId = g1.Key, CountCondoCategories = g1.Count()};

				var condoTags = from ctr in db.GetTable<CondoTag>()
					group ctr by ctr.CondoId
					into g2
					select new {CondoId = g2.Key, CountCondoTags = g2.Count()};

				var sqlCondos = from c in db.GetTable<Condo>()
					join l in db.GetTable<Location>() on c.LocationId equals l.Id
					join ct in condoTags on c.Id equals ct.CondoId into ctleft
					from subct in ctleft.DefaultIfEmpty()
					join cc in condoCategories on c.Id equals cc.CondoId into ccleft
					from subcc in ccleft.DefaultIfEmpty()
					select new
					{
						Condo           = c,
						CategoriesCount = subcc.CountCondoCategories,
						TagsCount       = subct.CountCondoTags,
						LocationName    = l.LocationName,
						LocationId      = l.Id
					};


				sqlCondos.Invoking(q => q.ToList()).Should().NotThrow();

				sqlCondos.ToSqlQuery().Sql.Should().Contain("COUNT(*)", AtLeast.Twice());
			}
		}
	}
}
