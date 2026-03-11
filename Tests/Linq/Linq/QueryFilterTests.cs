using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

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
		sealed class InfoClass : ISoftDelete
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

		static MappingSchema _filterMappingSchema = GetFilterMappingSchema();

		static MappingSchema GetFilterMappingSchema()
		{
			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter((q, dc) => q.Where(e => !((DcParams)((MyDataContext)dc).Params).IsSoftDeleteFilterEnabled || !e.IsDeleted));

			builder.Build();

			return builder.MappingSchema;
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
						IsDeleted = i % 2 == 0,
						MasterId = i % 4 == 0 ? (int?)i : null
					}
				)
				.ToArray();

			var detailRecords = Enumerable.Range(1, 1000)
				.Select(i => new DetailClass
				{
					Id = i,
					Value = "DetailValue_" + i,
					IsDeleted = i % 4 == 0,
					MasterId = i / 100
				})
				.ToArray();

			return Tuple.Create(masterRecords, infoRecords, detailRecords);
		}

		sealed class MyDataContext : DataConnection
		{
			public MyDataContext(string configuration, MappingSchema mappingSchema) : base(new DataOptions().UseConfiguration(configuration, mappingSchema))
			{

			}

			public bool IsSoftDeleteFilterEnabled { get; set; } = true;

			public object Params { get; } = new DcParams();
		}

		sealed class DcParams
		{
			public bool IsSoftDeleteFilterEnabled { get; set; } = true;
		}

		[Test]
		public void EntityFilterTests([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
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
			var resultNotFiltered1 = query.ToArray();

			Assert.That(resultFiltered1, Has.Length.LessThan(resultNotFiltered1.Length));

			var currentMissCount = query.GetCacheMissCount();

			db.IsSoftDeleteFilterEnabled = true;
			query                        = Internals.CreateExpressionQueryInstance<T>(db, query.Expression);
			var resultFiltered2 = query.ToArray();

			db.IsSoftDeleteFilterEnabled = false;
			query                        = Internals.CreateExpressionQueryInstance<T>(db, query.Expression);
			var resultNotFiltered2 = query.ToArray();

			Assert.That(resultFiltered2, Has.Length.LessThan(resultNotFiltered2.Length));

			AreEqualWithComparer(resultFiltered1,    resultFiltered2);
			AreEqualWithComparer(resultNotFiltered1, resultNotFiltered2);

			Assert.That(currentMissCount, Is.EqualTo(query.GetCacheMissCount()), () => "Caching is wrong.");
		}

		[Test]
		public void EntityFilterTestsCache([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int iteration, [Values] bool filtered)
		{
			var testData = GenerateTestData();

			using var db = new MyDataContext(context, _filterMappingSchema);
			using var tb = db.CreateLocalTable(testData.Item1);

				var currentMissCount = tb.GetCacheMissCount();

				var query =
					from m in db.GetTable<MasterClass>()
					from d in db.GetTable<MasterClass>().Where(d => d.Id == m.Id) // for ensuring that we do not cache two dynamic filters comparators. See ParametersContext.RegisterDynamicExpressionAccessor
					select m;

				((DcParams)db.Params).IsSoftDeleteFilterEnabled = filtered;

				var result = query.ToList();

				if (filtered)
					result.Count.ShouldBeLessThan(testData.Item1.Length);
				else
					result.Count.ShouldBe(testData.Item1.Length);

				if (iteration > 1)
				{
					tb.GetCacheMissCount().ShouldBe(currentMissCount);
				}
			}

		[Test]
		public void AssociationToFilteredEntity([IncludeDataSources(false, ProviderName.SQLiteMS, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters(typeof(MasterClass))
					from d in m.Details!
					select d;

				CheckFiltersForQuery(db, query);
			}
		}

		[Test]
		public void AssociationToFilteredEntityFunc([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			Expression<Func<ISoftDelete, MyDataContext, bool>> softDeleteCheck = (e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted;
			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => softDeleteCheck.Compile()(e, dc)));

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters(typeof(MasterClass))
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

		[Test]
		public void AssociationToFilteredEntityMethod([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>().IgnoreFilters(typeof(MasterClass))
					from d in m.Details!
					select d;

				CheckFiltersForQuery(db, query);
			}
		}

		[Test]
		public void AssociationNesting([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<InfoClass>()  .HasQueryFilter<MyDataContext>(FilterDeletedCondition);

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from m in db.GetTable<MasterClass>()
						.LoadWith(x => x.Info)
						.IgnoreFilters(typeof(InfoClass))
					where m.Info != null && m.Info.IsDeleted == true
					select m;

				var result = query.ToArray();

				result.ShouldAllSatisfy(m =>
				{
					m.IsDeleted.ShouldBeFalse();
					m.Info?.IsDeleted.ShouldBeTrue();
				});
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4496")]
		public void Issue4496Test([DataSources] string context)
		{
			var builder = new FluentMappingBuilder(new MappingSchema());

			builder
				.Entity<Child>()
				.HasQueryFilter((q, ctx) => q.InnerJoin(
					ctx.GetTable<Parent>(),
					(p, u) => p.ParentID == u.ParentID && u.Value1 > 5,
					(p, u) => p)
				.Distinct());

			builder.Build();

			using var db = GetDataContext(context, builder.MappingSchema);

			var query = db.Child.Where(x => x.ChildID > 30);

			query.ToArray();

			// StackOverflow on query comparison
			query.ToArray();
		}

		int Issue4508Test_Id;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4508")]
		public void Issue4508Test([DataSources] string context)
		{
			Test(context);
			Test(context);

			void Test(string context)
			{
				var builder = new FluentMappingBuilder(new MappingSchema());

				Issue4508Test_Id = 0;

				builder
					.Entity<Person>()
					.HasQueryFilter((q, ctx) =>
					{
						var idCopy = Issue4508Test_Id++;
						return q.Where(p => p.ID > idCopy);
					});

				builder.Build();
				using var db = GetDataContext(context, builder.MappingSchema);

				var query = db.Person;

				var arr1 = query.ToArray();
				var arr2 = query.ToArray();

				Assert.That(arr1, Has.Length.EqualTo(arr2.Length + 1));

				Issue4508Test_Id = 0;

				arr1 = query.ToArray();
				arr2 = query.ToArray();

				Assert.That(arr1, Has.Length.EqualTo(arr2.Length + 1));
			}
		}

		[Table]
		public partial class Issue5289Table
		{
			[PrimaryKey] public int  Id        { get; set; }
			[Column    ] public int? PictureId { get; set; }
			[Column    ] public bool  Deleted  { get; init; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5289")]
		public void InsertOrUpdate([InsertOrUpdateDataSources] string context)
		{
			var builder = new FluentMappingBuilder();
			builder.Entity<Issue5289Table>().HasQueryFilter((r, db) => !r.Deleted);
			builder.Build();

			using var db = GetDataContext(context, builder.MappingSchema);
			using var tb = db.CreateLocalTable<Issue5289Table>();

			tb.InsertOrUpdate(
				() => new Issue5289Table()
				{
					Id = 1,
					PictureId = 2,
					Deleted = false
				},
				r => new Issue5289Table()
				{
					PictureId = 3,
				});

			var record = tb.SingleOrDefault(r => r.Id == 1);

			Assert.That(record, Is.Not.Null);
			Assert.That(record.PictureId, Is.EqualTo(2));

			tb.InsertOrUpdate(
				() => new Issue5289Table()
				{
					Id = 1,
					PictureId = 2,
					Deleted = false
				},
				r => new Issue5289Table()
				{
					PictureId = 3
				});

			record = tb.SingleOrDefault(r => r.Id == 1);

			Assert.That(record, Is.Not.Null);
			Assert.That(record.PictureId, Is.EqualTo(3));
		}

		[Test]
		public void NestedIgnoreFiltersAccumulation([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);
			builder.Entity<InfoClass>().HasQueryFilter<MyDataContext>(FilterDeletedCondition);

			builder.Build();

			var ms = builder.MappingSchema;

			using var db = new MyDataContext(context, ms);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var query =
					from m in db.GetTable<MasterClass>()
					select new { m, DetailCount = m.Details!.Count() };

				query = query.IgnoreFilters(typeof(MasterClass));
				query = query.IgnoreFilters(typeof(DetailClass));

				var result = query.ToList();

				result.Count.ShouldBe(10);
				result.ShouldAllBe(item => item.DetailCount > 0);
	}
}
	}
}
