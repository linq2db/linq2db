using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
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
			// YDB requires every table to have a primary key
			[PrimaryKey(Configuration = ProviderName.Ydb)]
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
		public void ProjectsRuntimeColumnSetIntoStore([DataSources] string context)
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

		/// <summary>A generated column named after a real member would be shadowed by it, silently.</summary>
		[Test]
		public void NameCollidingWithAMemberThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			Action act = () => t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n),
					_ => nameof(AmountsDto.Id))
				.ToList();

			act.ShouldThrow<ArgumentException>();
		}

		#region The cases this operator was justified by

		[Table]
		sealed class Balance
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int      Id              { get; set; }
			[Column] public decimal? Currency1Amount { get; set; }
			[Column] public decimal? Currency2Amount { get; set; }
			[Column] public decimal? Currency3Amount { get; set; }

			public static readonly Balance[] Data =
			{
				new() { Id = 1, Currency1Amount = 10.5m },
				new() { Id = 2, Currency2Amount = 20m   },
				new() { Id = 3                          },
				new() { Id = 4, Currency3Amount = 40m   },
			};
		}

		sealed class BalanceDto
		{
			public int Id { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object>? Amounts { get; set; }
		}

		static string CurrencyColumn(int currencyId) => "Currency" + currencyId.ToString(CultureInfo.InvariantCulture) + "Amount";

		// The column name has to be a captured local rather than a lambda parameter: ExposeExpressionVisitor
		// evaluates Sql.Property's name argument before any enclosing lambda is substituted, so a name computed
		// from a lambda parameter throws before any builder runs.
		static Expression<Func<BalanceDto, bool>> AmountSet(int currencyId)
		{
			var name = CurrencyColumn(currencyId);

			return r => Sql.Property<decimal?>(r, name) != null;
		}

		// Currency1Amount != null || Currency2Amount != null || ... over whatever the runtime set holds.
		static Expression<Func<BalanceDto, bool>> AnyAmountSet(IEnumerable<int> currencyIds)
			=> currencyIds.Select(AmountSet).Aggregate(Or);

		static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
			=> Expression.Lambda<Func<T, bool>>(
				Expression.OrElse(left.Body, right.Body.Replace(right.Parameters[0], left.Parameters[0])),
				left.Parameters[0]);

		/// <summary>
		/// <a href="https://github.com/linq2db/linq2db/discussions/4992">Discussion #4992</a>: one column per
		/// currency, named by a pattern, with the set of currencies known only at runtime - selected into a
		/// single collection property, then filtered by the same test repeated across that runtime set.
		/// </summary>
		[Test]
		public void SelectsRuntimeCurrencyColumns([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Balance.Data);

			var currencyIds = new[] { 1, 2 };

			var rows = t
				.SelectDynamic(
					x => new BalanceDto { Id = x.Id },
					currencyIds,
					(x, currencyId) => Sql.Property<decimal?>(x, CurrencyColumn(currencyId)),
					CurrencyColumn)
				.Where(AnyAmountSet(currencyIds))
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			// Id 3 has nothing set, and id 4 only has a currency outside the runtime set.
			rows.Select(r => r.Id).ShouldBe(new[] { 1, 2 });

			rows[0].Amounts!.Keys.OrderBy(k => k, StringComparer.Ordinal)
				.ShouldBe(new[] { "Currency1Amount", "Currency2Amount" });

			rows[0].Amounts!["Currency1Amount"].ShouldBe(10.5m);
			rows[0].Amounts!["Currency2Amount"].ShouldBeNull();
			rows[1].Amounts!["Currency2Amount"].ShouldBe(20m);
		}

		/// <summary>
		/// The shape above composes a predicate over the same runtime set that decides the column set, so both
		/// halves have to take part in the query cache key. The predicate must be a hand-built tree: an
		/// <c>Any</c> over the set throws, because <c>Sql.Property</c> is rewritten while the name is still a
		/// bound lambda parameter.
		/// </summary>
		[Test, QueryCacheTest]
		public void RuntimeCurrencySetDiscriminatesQueryCache([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(Balance.Data);

			IQueryable<BalanceDto> Build(int[] currencyIds) => t
				.SelectDynamic(
					x => new BalanceDto { Id = x.Id },
					currencyIds,
					(x, id) => Sql.Property<decimal?>(x, CurrencyColumn(id)),
					CurrencyColumn)
				.Where(AnyAmountSet(currencyIds));

			var probe = Build(new[] { 1, 2 });
			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			_ = Build(new[] { 1, 2 }).ToSqlQuery();
			_ = Build(new[] { 1, 2 }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(1, "the same currency set must reuse the compiled query");

			_ = Build(new[] { 1, 3 }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(2, "a different currency set must not reuse it");

			_ = Build(new[] { 1, 2, 3 }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(3, "a wider currency set must not reuse it either");

			_ = Build(new[] { 1, 2 }).ToSqlQuery();
			(probe.GetCacheMissCount() - start).ShouldBe(3, "the first set's entry must survive the other two");

			// Both halves track the set - the projected columns and the predicate.
			var sql = Build(new[] { 1, 3 }).ToSqlQuery().Sql;

			sql.ShouldContain("Currency3Amount");
			sql.ShouldNotContain("Currency2Amount");
		}

		[Table("CustomerCustomValues")]
		sealed class CustomValuesPrototype
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int     Id            { get; set; }
			[Column] public int     CustomerId    { get; set; }
			[Column] public string? WorkLocation  { get; set; }
			[Column] public string? LastContacted { get; set; }

			public static readonly CustomValuesPrototype[] Data =
			{
				new() { Id = 1, CustomerId = 10, WorkLocation = "HQ",     LastContacted = "2024-01-01" },
				new() { Id = 2, CustomerId = 20, WorkLocation = "Remote", LastContacted = "2024-02-02" },
			};
		}

		// The shared model, which knows nothing about the per-customer columns.
		[Table("CustomerCustomValues")]
		sealed class CustomValues
		{
			[Column] public int Id         { get; set; }
			[Column] public int CustomerId { get; set; }
		}

		sealed class CustomValuesDto
		{
			public int CustomerId { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object>? Custom { get; set; }
		}

		/// <summary>
		/// <a href="https://github.com/linq2db/linq2db/discussions/4248">Discussion #4248</a>: a table deployed
		/// per customer carries extra columns that are absent from the mapping, and are referred to by name
		/// because they are not known upfront.
		/// </summary>
		[Test]
		public void SelectsColumnsMissingFromTheMapping([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(CustomValuesPrototype.Data);

			var columns = new[] { "WorkLocation", "LastContacted" };

			var rows = db.GetTable<CustomValues>()
				.SelectDynamic(
					x => new CustomValuesDto { CustomerId = x.CustomerId },
					columns,
					(x, name) => Sql.Property<string>(x, name))
				.ToList()
				.OrderBy(r => r.CustomerId)
				.ToList();

			rows.Count.ShouldBe(2);

			rows[0].Custom!["WorkLocation"] .ShouldBe("HQ");
			rows[0].Custom!["LastContacted"].ShouldBe("2024-01-01");
			rows[1].Custom!["WorkLocation"] .ShouldBe("Remote");
			rows[1].Custom!["LastContacted"].ShouldBe("2024-02-02");
		}

		[Table("RawRows")]
		sealed class RawRowPrototype
		{
			[PrimaryKey(Configuration = ProviderName.Ydb)]
			[Column] public int     Id   { get; set; }
			[Column] public string? Name { get; set; }

			public static readonly RawRowPrototype[] Data = { new() { Id = 1, Name = "first" } };
		}

		[Table("RawRows")]
		sealed class RawRow
		{
			[Column] public int Id { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object>? Props { get; set; }
		}

		/// <summary>
		/// <a href="https://github.com/linq2db/linq2db/issues/2953">Issue #2953</a>: the wrapper a
		/// <c>FromSql</c> query is built into selects only the mapped columns, so the store stays
		/// <see langword="null"/>. Naming the wanted columns fills it.
		/// </summary>
		[Test]
		public void SelectsDynamicColumnsOverFromSql([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(RawRowPrototype.Data);

			var rows = db.FromSql<RawRow>("select * from RawRows")
				.SelectDynamic(
					x => new RawRow { Id = x.Id },
					new[] { "Name" },
					(x, name) => Sql.Property<string>(x, name))
				.ToList();

			rows.Count.ShouldBe(1);

			rows[0].Id.ShouldBe(1);
			rows[0].Props!["Name"].ShouldBe("first");
		}

		#endregion
	}
}
