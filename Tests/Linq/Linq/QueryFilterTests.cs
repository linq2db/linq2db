﻿using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryFilterTests : TestBase
	{
		interface ISoftDelete
		{
			public bool IsDeleted { get; set; }
		}

		[Table]
		sealed class MasterClass : ISoftDelete
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
		sealed class InfoClass
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
			[Column] public bool    IsDeleted { get; set; }
			
			[Column] public int? MasterId { get; set; }
		}


		[Table]
		sealed class DetailClass : ISoftDelete
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }
			[Column] public bool    IsDeleted { get; set; }
			
			[Column] public int? MasterId { get; set; }
		}


		static MappingSchema _filterMappingSchema;

		static QueryFilterTests()
		{
			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter((q, dc) => q.Where(e => !((DcParams)((MyDataContext)dc).Params).IsSoftDeleteFilterEnabled || !e.IsDeleted));

			_filterMappingSchema = builder.MappingSchema;
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

		sealed class MyDataContext : DataConnection
		{
			public MyDataContext(string configuration, MappingSchema mappingSchema) : base(configuration, mappingSchema)
			{
				
			}

			public bool IsSoftDeleteFilterEnabled { get; set; } = true;

			public object Params { get; } = new DcParams();
		}

		sealed class DcParams
		{
			public bool IsSoftDeleteFilterEnabled { get; set; } = true;
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void EntityFilterTests([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));

			var ms = builder.MappingSchema;

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
			db.IsSoftDeleteFilterEnabled = true;
			var resultFiltered1 = query.ToArray();

			db.IsSoftDeleteFilterEnabled = false;
			query                        = Internals.CreateExpressionQueryInstance<T>(db, query.Expression);
			var resultNotFiltered1 = query.ToArray();

			Assert.That(resultFiltered1.Length, Is.LessThan(resultNotFiltered1.Length));

			var currentMissCount = Query<T>.CacheMissCount;

			db.IsSoftDeleteFilterEnabled = true;
			query                        = Internals.CreateExpressionQueryInstance<T>(db, query.Expression);
			var resultFiltered2 = query.ToArray();

			db.IsSoftDeleteFilterEnabled = false;
			query                        = Internals.CreateExpressionQueryInstance<T>(db, query.Expression);
			var resultNotFiltered2 = query.ToArray();

			Assert.That(resultFiltered2.Length, Is.LessThan(resultNotFiltered2.Length));

			AreEqualWithComparer(resultFiltered1,    resultFiltered2);
			AreEqualWithComparer(resultNotFiltered1, resultNotFiltered2);

			Assert.That(currentMissCount, Is.EqualTo(Query<T>.CacheMissCount), () => "Caching is wrong.");

		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void EntityFilterTestsCache([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int iteration, [Values] bool filtered)
		{
			var testData = GenerateTestData();

			using (var db = new MyDataContext(context, _filterMappingSchema))
			using (db.CreateLocalTable(testData.Item1))
			{

				var currentMissCount = Query<MasterClass>.CacheMissCount;

				var query = from m in db.GetTable<MasterClass>()
					select m;

				((DcParams)db.Params).IsSoftDeleteFilterEnabled = filtered;

				var result = query.ToList();

				if (filtered)
					result.Count.Should().BeLessThan(testData.Item1.Length);
				else
					result.Count.Should().Be(testData.Item1.Length);

				if (iteration > 1)
				{
					Query<MasterClass>.CacheMissCount.Should().Be(currentMissCount);
				}
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void AssociationToFilteredEntity([IncludeDataSources(false, ProviderName.SQLiteMS, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));

			var ms = builder.MappingSchema;

			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters()
					from d in m.Details!
					select d;

				CheckFiltersForQuery(db, query);
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void AssociationToFilteredEntityFunc([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			Expression<Func<ISoftDelete, MyDataContext, bool>> softDeleteCheck = (e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted;
			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));

			var ms = builder.MappingSchema;

			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters()
					from d in m.Details!
					select d;

				CheckFiltersForQuery(db, query);
			}
		}


		static IQueryable<T> FilterDeleted<T>(IQueryable<T> query)
			where T: ISoftDelete
		{
			query = query.Where(e => !e.IsDeleted);
			return query;
		}

		static IQueryable<T> FilterDeletedCondition<T>(IQueryable<T> query, MyDataContext dc)
		where T: ISoftDelete
		{
			if (dc.IsSoftDeleteFilterEnabled)
				query = FilterDeleted(query);
			return query;
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/56 + https://github.com/ClickHouse/ClickHouse/issues/37999", Configurations = new[] { ProviderName.ClickHouseMySql, ProviderName.ClickHouseOctonica })]
		[Test]
		public void AssociationToFilteredEntityMethod([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);

			var ms = builder.MappingSchema;

			using (var db = new MyDataContext(context, ms))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters()
					from d in m.Details!
					select d;

				CheckFiltersForQuery(db, query);
			}
		}

	}
}
