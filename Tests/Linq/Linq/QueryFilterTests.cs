using System;
using System.Collections.Generic;
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

		class FilterBaseEntity : ISoftDelete
		{
			[Column] public int  Id        { get; set; }
			[Column] public bool IsDeleted { get; set; }
		}

		[Table]
		sealed class FilterDerivedEntity : FilterBaseEntity
		{
			[Column] public string? Value { get; set; }
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

		const string SoftDeleteKey = "SoftDelete";
		const string IdRangeKey    = "IdRange";

		[Test]
		public void NamedFilters_AllApplyAsAnd([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var result = db.GetTable<DetailClass>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted && e.Id < 500);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void NamedFilter_NullRemoves([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (Expression<Func<DetailClass, MyDataContext, bool>>?)null);

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var result = db.GetTable<DetailClass>().ToList();

				result.Count.ShouldBe(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ByKey_SkipsOnlyNamed([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var result = db.GetTable<DetailClass>().IgnoreFilters([SoftDeleteKey]).ToList();

				result.ShouldAllBe(e => e.Id < 500);
				result.ShouldContain(e => e.IsDeleted);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ByKey_Empty_DisablesNothing([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			// An explicit empty key list disables nothing (mirrors EF Core) — every filter, anonymous and named, stays.
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => e.Id < 750));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var result = db.GetTable<DetailClass>().IgnoreFilters(Array.Empty<string>()).ToList();

				result.ShouldAllBe(e => e.Id < 500 && !e.IsDeleted);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ByKey_Null_DisablesNothing([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				// A null key list disables nothing and does not throw.
				var result = db.GetTable<DetailClass>().IgnoreFilters((IEnumerable<string>)null!).ToList();

				result.ShouldAllBe(e => e.Id < 500 && !e.IsDeleted);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_EmptyKeys_WithTypes_DisablesNothing([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				// The key list gates: empty keys disable nothing even when entity types are supplied.
				var result = db.GetTable<DetailClass>().IgnoreFilters(Array.Empty<string>(), typeof(DetailClass)).ToList();

				result.ShouldAllBe(e => e.Id < 500 && !e.IsDeleted);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ByType_SkipsAllNamed([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var result = db.GetTable<DetailClass>().IgnoreFilters(typeof(DetailClass)).ToList();

				result.Count.ShouldBe(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ByKeyAndType_TargetsIntersection([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<MasterClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item3))
			{
				var masters = db.GetTable<MasterClass>().IgnoreFilters([SoftDeleteKey], [typeof(MasterClass)]).ToList();
				var details = db.GetTable<DetailClass>().IgnoreFilters([SoftDeleteKey], [typeof(MasterClass)]).ToList();

				masters.Count.ShouldBe(testData.Item1.Length);          // SoftDelete on MasterClass disabled
				details.ShouldAllBe(e => !e.IsDeleted);                 // SoftDelete on DetailClass still applies
				details.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void AnonymousAndNamed_Coexist([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			// anonymous (default key) — drop deleted rows
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((q, dc) => q.Where(e => !e.IsDeleted));
			// named — keep only rows with Id < 500 (so the predicate actually filters the seeded 1..1000 data)
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey, (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				// Both apply
				var both = db.GetTable<DetailClass>().ToList();
				both.ShouldAllBe(e => !e.IsDeleted && e.Id < 500);

				// Disabling anonymous via empty-string key keeps named filter active
				var onlyNamed = db.GetTable<DetailClass>().IgnoreFilters([""]).ToList();
				onlyNamed.ShouldAllBe(e => e.Id < 500);
				onlyNamed.ShouldContain(e => e.IsDeleted);

				// Disabling named keeps anonymous active
				var onlyAnonymous = db.GetTable<DetailClass>().IgnoreFilters([IdRangeKey]).ToList();
				onlyAnonymous.ShouldAllBe(e => !e.IsDeleted);
				onlyAnonymous.ShouldContain(e => e.Id >= 500);
			}
		}

		[Test]
		public void NamedFilter_FuncOverload([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (Func<IQueryable<DetailClass>, MyDataContext, IQueryable<DetailClass>>)((q, dc) => q.Where(e => e.Id < 500)));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var result = db.GetTable<DetailClass>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted && e.Id < 500);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void IgnoreFilters_ScopeAccumulation([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey,  (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = db.GetTable<DetailClass>()
					.IgnoreFilters([SoftDeleteKey])
					.IgnoreFilters([IdRangeKey]);

				var result = query.ToList();

				result.Count.ShouldBe(testData.Item3.Length);
			}
		}

		[Test]
		public void NamedFilter_DerivedOverridesBase([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = Enumerable.Range(1, 200)
				.Select(i => new FilterDerivedEntity
				{
					Id        = i,
					IsDeleted = i % 2 == 0,
					Value     = "v" + i,
				})
				.ToArray();

			var builder = new FluentMappingBuilder(new MappingSchema());

			// Base registers "Common" => drop soft-deleted rows.
			builder.Entity<FilterBaseEntity>()   .HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			// Derived overrides the same key with a different predicate.
			builder.Entity<FilterDerivedEntity>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => e.Id < 100));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(data))
			{
				var result = db.GetTable<FilterDerivedEntity>().ToList();

				// Derived filter wins: only rows with Id < 100, deleted-or-not.
				result.ShouldAllBe(e => e.Id < 100);
				result.ShouldContain(e => e.IsDeleted);
			}
		}

		[Test]
		public void NamedFilter_InlineAssociationKeepsParentRow([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Regression for AssociationHelper inline-association optional-result detection:
			// when an inline (single-target) navigation targets an entity with ONLY named filters,
			// the parent row must still surface with a null navigation when the target is filter-excluded.
			// Pre-fix the back-compat QueryFilterLambda shim was null for named-only setups and the join
			// became INNER instead of LEFT-with-DefaultIfEmpty, silently dropping the parent.

			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			// Named-only filter on the inline-association target — anonymous slot is intentionally empty
			// so QueryFilterLambda stays null while QueryFilters.Count > 0.
			builder.Entity<InfoClass>().HasQueryFilter<MyDataContext>("InfoSoftDelete", (q, dc) => q.Where(e => !e.IsDeleted));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			{
				var result = (from m in db.GetTable<MasterClass>()
				              select new { m.Id, InfoId = (int?)m.Info!.Id })
				             .ToList();

				// Every master row must survive even when its Info is filter-excluded or absent.
				result.Count.ShouldBe(testData.Item1.Length);
				result.ShouldContain(r => r.InfoId == null);
			}
		}

		[Test]
		public void NamedFilter_InterfaceTypedLambda_FastPath([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Regression: a filter lambda typed against an interface (Func<ISoftDelete, ...>) applied to a concrete
			// entity must be adapted to the entity type before Queryable.Where<DetailClass>, otherwise query build
			// throws ArgumentException. This declares only a lambda filter, exercising the lambda-only fast path.

			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasAttribute(new QueryFilterAttribute
			{
				FilterKey    = SoftDeleteKey,
				FilterLambda = (Expression<Func<ISoftDelete, MyDataContext, bool>>)((e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted)
			});

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var result = db.GetTable<DetailClass>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void NamedFilter_InterfaceTypedLambda_WithFunc_DynamicPath([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Same interface-typed lambda regression on the dynamic-accessor path: pairing the interface-typed
			// lambda filter with a func filter on the same entity forces the func branch of ApplyQueryFilters,
			// which must apply the same entity-type adaptation as the fast path.

			var testData = GenerateTestData();

			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasAttribute(new QueryFilterAttribute
			{
				FilterKey    = SoftDeleteKey,
				FilterLambda = (Expression<Func<ISoftDelete, MyDataContext, bool>>)((e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted)
			});
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey, (Func<IQueryable<DetailClass>, MyDataContext, IQueryable<DetailClass>>)((q, dc) => q.Where(e => e.Id < 500)));

			builder.Build();

			using var db = new MyDataContext(context, builder.MappingSchema);
			using (db.CreateLocalTable(testData.Item3))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var result = db.GetTable<DetailClass>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted && e.Id < 500);
				result.Count.ShouldBeLessThan(testData.Item3.Length);
			}
		}

		[Test]
		public void QueryFilters_Introspection_ShapeOrderAndDefaultSlot()
		{
			// Directly asserts the public introspection surface (EntityDescriptor.QueryFilters / EntityQueryFilter)
			// rather than going through query results: entry count, declaration order, per-entry key, and which
			// entries carry FilterLambda vs FilterFunc. No DB roundtrip needed.

			var builder = new FluentMappingBuilder(new MappingSchema());

			// Declaration order: anonymous predicate, named predicate, named func.
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>((e, dc) => !e.IsDeleted);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (e, dc) => !e.IsDeleted);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey, (q, dc) => q.Where(e => e.Id < 500));

			builder.Build();

			var ed = builder.MappingSchema.GetEntityDescriptor(typeof(DetailClass));

			ed.QueryFilters.Count.ShouldBe(3);
			ed.QueryFilters.Select(f => f.FilterKey).ShouldBe(new[] { "", SoftDeleteKey, IdRangeKey });

			var anon = ed.QueryFilters[0];
			anon.FilterLambda.ShouldNotBeNull();
			anon.FilterFunc.ShouldBeNull();

			var soft = ed.QueryFilters[1];
			soft.FilterLambda.ShouldNotBeNull();
			soft.FilterFunc.ShouldBeNull();

			var idRange = ed.QueryFilters[2];
			idRange.FilterLambda.ShouldBeNull();
			idRange.FilterFunc.ShouldNotBeNull();

			// Legacy single-slot accessors mirror the default (anonymous, empty-key) entry.
			ed.QueryFilterLambda.ShouldBeSameAs(anon.FilterLambda);
			ed.QueryFilterFunc.ShouldBeNull();
		}

		[Test]
		public void QueryFilters_Introspection_NullRemovesEntry()
		{
			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (e, dc) => !e.IsDeleted);
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(IdRangeKey, (q, dc) => q.Where(e => e.Id < 500));
			// Null under an existing key removes that entry from the collection.
			builder.Entity<DetailClass>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (Expression<Func<DetailClass, MyDataContext, bool>>?)null);

			builder.Build();

			var ed = builder.MappingSchema.GetEntityDescriptor(typeof(DetailClass));

			ed.QueryFilters.Count.ShouldBe(1);
			ed.QueryFilters.Select(f => f.FilterKey).ShouldBe(new[] { IdRangeKey });
		}

		[Test]
		public void QueryFilters_Introspection_DerivedOverridesBase()
		{
			var builder = new FluentMappingBuilder(new MappingSchema());

			// Base declares the key as a func; derived overrides the same key with a predicate lambda.
			builder.Entity<FilterBaseEntity>()   .HasQueryFilter<MyDataContext>(SoftDeleteKey, (q, dc) => q.Where(e => !e.IsDeleted));
			builder.Entity<FilterDerivedEntity>().HasQueryFilter<MyDataContext>(SoftDeleteKey, (e, dc) => !e.IsDeleted);

			builder.Build();

			var ms = builder.MappingSchema;

			// Base keeps its own func entry.
			var edBase = ms.GetEntityDescriptor(typeof(FilterBaseEntity));
			edBase.QueryFilters.Count.ShouldBe(1);
			edBase.QueryFilters[0].FilterFunc.ShouldNotBeNull();

			// Derived collapses to a single entry under the shared key, carrying the derived lambda (not the base func).
			var edDerived = ms.GetEntityDescriptor(typeof(FilterDerivedEntity));
			edDerived.QueryFilters.Count.ShouldBe(1);
			edDerived.QueryFilters[0].FilterKey.ShouldBe(SoftDeleteKey);
			edDerived.QueryFilters[0].FilterLambda.ShouldNotBeNull();
			edDerived.QueryFilters[0].FilterFunc.ShouldBeNull();
		}

		sealed class ConcreteSoftDeleteQueryFilterAttribute : QueryFilterAttribute
		{
			public ConcreteSoftDeleteQueryFilterAttribute()
			{
				FilterKey = SoftDeleteKey;

				FilterLambda =
					(Expression<Func<AttrFilteredEntity, MyDataContext, bool>>)
					((e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted);
			}
		}

		[Table]
		[ConcreteSoftDeleteQueryFilter]
		sealed class AttrFilteredEntity
		{
			[Column] public int  Id        { get; set; }
			[Column] public bool IsDeleted { get; set; }
		}

		[Test]
		public void QueryFilterAttribute_ConcreteTypedLambda([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Declarative query-filter coverage: a QueryFilterAttribute subclass applied to the entity via [Attribute]
			// and discovered from CLR metadata with a bare MappingSchema (the only declarative form, since a lambda
			// can't be an attribute argument). The lambda is typed to the concrete entity, so no interface adaptation
			// is involved — this isolates the attribute-discovery + named-filter application path.

			var data = Enumerable.Range(1, 10)
				.Select(i => new AttrFilteredEntity { Id = i, IsDeleted = i % 2 == 0 })
				.ToArray();

			using var db = new MyDataContext(context, new MappingSchema());
			using (db.CreateLocalTable(data))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var result = db.GetTable<AttrFilteredEntity>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted);
				result.Count.ShouldBe(5);
			}
		}

		sealed class InterfaceSoftDeleteQueryFilterAttribute : QueryFilterAttribute
		{
			public InterfaceSoftDeleteQueryFilterAttribute()
			{
				FilterKey = SoftDeleteKey;

				FilterLambda =
					(Expression<Func<ISoftDelete, MyDataContext, bool>>)
					((e, dc) => !dc.IsSoftDeleteFilterEnabled || !e.IsDeleted);
			}
		}

		[Table]
		[InterfaceSoftDeleteQueryFilter]
		sealed class InterfaceAttrFilteredEntity : ISoftDelete
		{
			[Column] public int  Id        { get; set; }
			[Column] public bool IsDeleted { get; set; }
		}

		[Test]
		public void QueryFilterAttribute_InterfaceTypedLambda([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			// Declarative + interface-typed: a QueryFilterAttribute subclass whose FilterLambda is typed against an
			// interface (Func<ISoftDelete, ...>), applied via [Attribute] to a concrete entity and discovered from CLR
			// metadata. Exercises both the declarative-attribute path and the TableBuilder interface->entityType lambda
			// adaptation; before that fix this threw ArgumentException at query build.

			var data = Enumerable.Range(1, 10)
				.Select(i => new InterfaceAttrFilteredEntity { Id = i, IsDeleted = i % 2 == 0 })
				.ToArray();

			using var db = new MyDataContext(context, new MappingSchema());
			using (db.CreateLocalTable(data))
			{
				db.IsSoftDeleteFilterEnabled = true;

				var result = db.GetTable<InterfaceAttrFilteredEntity>().ToList();

				result.ShouldAllBe(e => !e.IsDeleted);
				result.Count.ShouldBe(5);
			}
		}
	}
}
