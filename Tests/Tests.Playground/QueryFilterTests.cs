using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class QueryFilterTests : TestBase
	{
		interface ISoftDelete
		{
			public bool IsDeleted { get; set; }
		}

		[Table]
		class MasterClass : ISoftDelete
		{
			[Column] public int     Id        { get; set; }
			[Column] public string? Value     { get; set; }
			[Column] public bool    IsDeleted { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(DetailClass.MasterId))]
			public DetailClass[]? Details { get; set; }

			public DetailClass[]? DetailsViaQuery { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(InfoClass.MasterId))]
			public InfoClass? Info { get; set; }
		}

		[Table]
		class InfoClass
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
			[Column] public bool    IsDeleted { get; set; }
			
			[Column] public int? MasterId { get; set; }
		}


		[Table]
		class DetailClass: ISoftDelete
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
			[Column] public bool    IsDeleted { get; set; }
			
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
		public void EntityFilterTests([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
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

				CheckFiltersForQuery(db, query);
			}
		}

		void CheckFiltersForQuery<T>(MyDataContext db, IQueryable<T> query)
		{
			db.IsSafeDeleteFilterEnabled = true;
			var resultFiltered1 = query.ToArray();

			db.IsSafeDeleteFilterEnabled = false;
			var resultNotFiltered1 = query.ToArray();

			Assert.That(resultFiltered1.Length, Is.LessThan(resultNotFiltered1.Length));

			var currentMissCount = Query<T>.CacheMissCount;

			db.IsSafeDeleteFilterEnabled = true;
			var resultFiltered2 = query.ToArray();

			db.IsSafeDeleteFilterEnabled = false;
			var resultNotFiltered2 = query.ToArray();

			Assert.That(resultFiltered2.Length, Is.LessThan(resultNotFiltered2.Length));

			AreEqualWithComparer(resultFiltered1,    resultFiltered2);
			AreEqualWithComparer(resultNotFiltered1, resultNotFiltered2);

			Assert.That(currentMissCount, Is.EqualTo(Query<T>.CacheMissCount), () => "Caching is wrong.");

		}

		[Test]
		public void AssociationToFilteredEntity([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
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
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters()
					from d in m.Details
					select d;

				CheckFiltersForQuery(db, query);
			}
		}

		[Test]
		public void AssociationToFilteredEntityFunc([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			Expression<Func<ISoftDelete, MyDataContext, bool>> softDeleteCheck = (e, dc) => !dc.IsSafeDeleteFilterEnabled || !e.IsDeleted;
			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));

			var ms = builder.MappingSchema;

			using (new AllowMultipleQuery())
			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters()
					from d in m.Details
					select d;

				CheckFiltersForQuery(db, query);
			}
		}


	}
}
