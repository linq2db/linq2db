using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectDynamicTests : TestBase
	{
		sealed class Amounts
		{
			public int     Id  { get; set; }
			public decimal Usd { get; set; }
			public decimal Eur { get; set; }
		}

		sealed class AmountsDto
		{
			public int Id { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object>? Values { get; set; }
		}

		sealed class NoStoreDto
		{
			public int Id { get; set; }
		}

		static readonly Amounts[] _data =
		[
			new Amounts { Id = 1, Usd = 10.5m, Eur = 20.25m },
			new Amounts { Id = 2, Usd = 30m,   Eur = 40m    },
		];

		[Test]
		public void ProjectsRuntimeColumnSetIntoStore([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var names = new[] { "Usd", "Eur" };

			var result = t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					names,
					(x, n) => Sql.Property<decimal>(x, n))
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result.Count.ShouldBe(2);

			result[0].Id.ShouldBe(1);
			result[0].Values.ShouldNotBeNull();
			result[0].Values!["Usd"].ShouldBe(10.5m);
			result[0].Values!["Eur"].ShouldBe(20.25m);

			result[1].Id.ShouldBe(2);
			result[1].Values!["Usd"].ShouldBe(30m);
			result[1].Values!["Eur"].ShouldBe(40m);
		}

		[Test]
		public void ResultWithoutStoreThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			Action act = () => t
				.SelectDynamic(
					x => new NoStoreDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n))
				.ToList();

			act.ShouldThrow<LinqToDBException>();
		}

		/// <summary>
		/// The runtime key set decides the output column set, so two different sets must never share a compiled
		/// query. The <see cref="List{T}"/> legs are the regression guard: a <c>List&lt;T&gt;</c> constant is
		/// dropped from the query cache key, after which the <c>SqlQueryDependent</c> comparison is skipped
		/// entirely - so if the operator ever stops materializing the keys to an array, the last leg collides.
		/// </summary>
		[Test, QueryCacheTest]
		public void RuntimeColumnSetDiscriminatesQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			IQueryable<AmountsDto> Build(IEnumerable<string> names) => t.SelectDynamic(
				x => new AmountsDto { Id = x.Id },
				names,
				(x, n) => Sql.Property<decimal>(x, n));

			var probe = Build(new[] { "Usd" });
			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			_ = Build(new[] { "Usd" }).ToSqlQuery();
			_ = Build(new[] { "Usd" }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(1, "the same column set must reuse the compiled query");

			_ = Build(new[] { "Usd", "Eur" }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(2, "a different column set must not reuse it");

			_ = Build(new List<string> { "Usd" }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(2, "a List with the same content must still hit");

			_ = Build(new List<string> { "Eur" }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(3, "a List with different content must not collide");
		}

		/// <summary>
		/// Column names are computed when the query is built, and they only affect materialization - the SQL can
		/// be identical. They must still take part in the cache key, or a second query materializes into the
		/// first one's store keys.
		/// </summary>
		[Test, QueryCacheTest]
		public void ColumnNamesDiscriminateQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			IQueryable<AmountsDto> Build(Func<string, string> nameSelector) => t.SelectDynamic(
				x => new AmountsDto { Id = x.Id },
				new[] { "Usd" },
				(x, n) => Sql.Property<decimal>(x, n),
				nameSelector);

			var probe = Build(n => n);
			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			var plain    = Build(n => n).ToList();
			var prefixed = Build(n => "c_" + n).ToList();

			(probe.GetCacheMissCount() - start).ShouldBe(2);

			plain[0].Values!.ShouldContainKey("Usd");
			prefixed[0].Values!.ShouldContainKey("c_Usd");
			prefixed[0].Values!.ShouldNotContainKey("Usd");
		}

		/// <summary>Same keys, different per-column expression - the template must take part in the cache key.</summary>
		[Test, QueryCacheTest]
		public void ValueTemplateDiscriminatesQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var probe = t.SelectDynamic(
				x => new AmountsDto { Id = x.Id },
				new[] { "Usd" },
				(x, n) => Sql.Property<decimal>(x, n));

			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			_ = t.SelectDynamic(
				x => new AmountsDto { Id = x.Id },
				new[] { "Usd" },
				(x, n) => Sql.Property<decimal>(x, n)).ToSqlQuery();

			_ = t.SelectDynamic(
				x => new AmountsDto { Id = x.Id },
				new[] { "Usd" },
				(x, n) => Sql.Property<decimal>(x, n) * 2).ToSqlQuery();

			(probe.GetCacheMissCount() - start).ShouldBe(2);
		}

		[Test]
		public void DuplicateNameThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			Action act = () => t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd", "Usd" },
					(x, n) => Sql.Property<decimal>(x, n))
				.ToList();

			act.ShouldThrow<ArgumentException>();
		}
	}
}
