using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class QueryFilterTests : TestBase
	{
		[Table]
		class MasterClass
		{
			[Column] public int    Id        { get; set; }
			[Column] public string Value     { get; set; }
			[Column] public bool   IsDeleted { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(DetailClass.MasterId))]
			public DetailClass[] Details { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(InfoClass.MasterId))]
			public InfoClass? Info { get; set; }
		}

		[Table]
		class InfoClass
		{
			[Column] public int    Id    { get; set; }
			[Column] public string Value { get; set; }
			[Column] public bool   IsDeleted { get; set; }
			
			[Column] public int? MasterId { get; set; }
		}


		[Table]
		class DetailClass
		{
			[Column] public int    Id    { get; set; }
			[Column] public string Value { get; set; }
			[Column] public bool   IsDeleted { get; set; }
			
			[Column] public int? MasterId { get; set; }
		}

		static Tuple<MasterClass[], InfoClass[], DetailClass[]> GenerateTestData()
		{
			var masterRecords = Enumerable.Range(1, 10)
				.Select(i => new MasterClass
					{
						Id = i,
						Value = "MasterValue_" + i,
						IsDeleted = i % 3 == 0
					}
				)
				.ToArray();

			var infoRecords = Enumerable.Range(1, 10)
				.Select(i => new InfoClass
					{
						Id = i,
						Value = "InfoValue_" + i,
						IsDeleted = i % 3 == 0,
						MasterId = i % 4 == 0 ? (int?)i : null
					}
				)
				.ToArray();

			var detailRecords = Enumerable.Range(1, 1000)
				.Select(i => new DetailClass
				{
					Id = i,
					Value = "DetailValue_" + i,
					IsDeleted = i % 3 == 0,
					MasterId = i / 100
				})
				.ToArray();

			return Tuple.Create(masterRecords, infoRecords, detailRecords);
		}

		class MyDataContext : DataConnection
		{
			public MyDataContext(string configuration, MappingSchema mappingSchema) : base(configuration, mappingSchema)
			{
				
			}

			public bool IsSafeDeleteFilterEnabled { get; set; } = true;
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSafeDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSafeDeleteFilterEnabled || !e.IsDeleted));

			var ms = builder.MappingSchema;

			using (new AllowMultipleQuery())
			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>()
					select m;

				// db.IsSafeDeleteFilterEnabled = false;
				//
				// var result = query.ToArray();
				//

				db.IsSafeDeleteFilterEnabled = true;
				
				var resultNotFiltered = query.ToArray();

				// var queryDetails = from m in db.GetTable<MasterClass>()
				// 	select new
				// 	{
				// 		m,
				// 		m.Details
				// 	};
				//
				// var resultDetails = queryDetails.ToArray();

				// var queryNavigation = from m in query
				// 	select new
				// 	{
				// 		m,
				// 		m.Info
				// 	};
				//
				// var queryNavigationResult = queryNavigation.ToArray();
			}
		}

		[Test]
		public void Probes([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSafeDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSafeDeleteFilterEnabled || !e.IsDeleted));

			var ms = builder.MappingSchema;

			using (new AllowMultipleQuery())
			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>()
					from t in db.GetTable<InfoClass>().Where(t => t.MasterId == m.Id)
					select t;

				var result = query.ToArray();
			}
		}

	}
}
