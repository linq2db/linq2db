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

		/// <summary>A dynamic projection written inline in a sub-query, over a source that is itself a query.</summary>
		[Test]
		[ActiveIssue(5953, ErrorType = typeof(InvalidOperationException), ErrorMessage = "variable 'n' of type 'System.String' referenced from scope '', but it is not defined")]
		public void SelectDynamicInlineInASubQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var names = new[] { "Usd", "Eur" };

			var ids =
				from a in t
				where t.Where(x => x.Id > 0)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, names, (x, n) => Sql.Property<decimal>(x, n))
					.Any(r => r.Id == a.Id && Sql.Property<decimal>(r, "Eur") > 30)
				select a.Id;

			ids
				.ToArray()
				.ShouldBe(new[] { 2 });
		}

		static readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _currentKeys = new();

		static int CurrentKey(string context) => _currentKeys[context];

		/// <summary>
		/// Written inline in a sub-query, the projection is built after the query cache key was taken, so what the name
		/// selector reads has to take part in the query's identity.
		/// </summary>
		// SQLite only: this pins how the query cache compares the name selector's reads, which no provider affects.
		[Test]
		public void InlineSelectDynamicRereadsWhatTheNameSelectorReads([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };

			_currentKeys[context] = 2;

			int[] Ids()
			{
				var query =
					from a in t
					where t.Where(x => x.Id > 0)
						.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, k => k == CurrentKey(context) ? "V" : "W" + k)
						.Any(r => r.Id == a.Id && Sql.Property<decimal>(r, "V") > 50)
					select a.Id;

				return query.ToArray();
			}

			Ids()
				.ShouldBe(new[] { 2 });

			_currentKeys[context] = 1;

			Ids()
				.ShouldBeEmpty();
		}

		/// <summary>Written inline in a sub-query, the same key set reuses the query and a set mutated in place does not.</summary>
		// SQLite only: a query-cache test.
		[Test, QueryCacheTest]
		public void InlineSelectDynamicCachesByKeySet([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };

			IQueryable<int> Build() =>
				from a in t
				where t.Where(x => x.Id > 0)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, k => "W" + k)
					.Any(r => r.Id == a.Id && Sql.Property<decimal>(r, "W2") > 50)
				select a.Id;

			var probe = Build();
			probe.ClearCache();
			var start = probe.GetCacheMissCount();

			for (var i = 0; i < 4; i++)
			{
				Build()
					.ToArray()
					.ShouldBe(new[] { 2 });
			}

			var misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(1, "the same key set must reuse the query");

			factors[0] = 2;
			factors[1] = 1;

			Build()
				.ToArray()
				.ShouldBe(new[] { 2 });

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2, "a set mutated in place must not reuse it");
		}

		/// <summary>
		/// Branches of a set operation with different key sets: each row carries the union of the sets, and a cell its
		/// branch did not select reads as <c>default(TCell)</c>.
		/// </summary>
		[Test]
		public void ConcatOfDifferentColumnSetsMaterializesTheUnion([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var rows = t.Where(x => x.Id == 1)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "Usd" }, (x, n) => Sql.Property<decimal>(x, n))
				.Concat(t.Where(x => x.Id == 2)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "Eur" }, (x, n) => Sql.Property<decimal>(x, n)))
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			rows[0].Values!["Usd"].ShouldBe(10.5m);
			rows[0].Values!["Eur"].ShouldBe(0m);
			rows[1].Values!["Usd"].ShouldBe(0m);
			rows[1].Values!["Eur"].ShouldBe(40m);
		}

		[Test]
		public void ConcatOfDifferentColumnSetsWithNullableCells([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var rows = t.Where(x => x.Id == 1)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { 1 }, (x, k) => (decimal?)(x.Usd * k))
				.Concat(t.Where(x => x.Id == 2)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { 2 }, (x, k) => (decimal?)(x.Usd * k)))
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			rows[0].Values!["1"].ShouldBe(10.5m);
			rows[0].Values!["2"].ShouldBeNull();
			rows[1].Values!["1"].ShouldBeNull();
			rows[1].Values!["2"].ShouldBe(60m);
		}

		/// <summary>
		/// Branches of <c>Concat</c> putting values of different types under one name: each row reads its own branch's
		/// value, rather than both reading one shared column as either type.
		/// </summary>
		[Test]
		public void ConcatOfOneNameWithDifferentCellTypes([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var rows = t.Where(x => x.Id == 1)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "A" }, (x, n) => x.Usd)
				.Concat(t.Where(x => x.Id == 2)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "A" }, (x, n) => x.Id))
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			rows[0].Values!["A"].ShouldBe(10.5m);
			rows[1].Values!["A"].ShouldBe(2);
		}

		/// <summary>A set operation that compares rows cannot compare values of different types under one name.</summary>
		// SQLite only: the builder throws before any SQL is built.
		[Test]
		public void UnionOfOneNameWithDifferentCellTypesThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var query = t.Where(x => x.Id == 1)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "A" }, (x, n) => x.Usd)
				.Union(t.Where(x => x.Id == 2)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { "A" }, (x, n) => x.Id));

			var act = () => query.ToList();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("store 'A' in different terms");
		}

		/// <summary>Keys passed as a compiled query's parameter: each call gets its own column set.</summary>
		// SQLite only: the SQL is the plain projection ProjectsRuntimeColumnSetIntoStore runs everywhere; this pins how
		// CompiledTable keys the column set.
		[Test]
		public void SelectDynamicInCompiledQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var query = CompiledQuery.Compile((IDataContext dc, int[] factors) => dc
				.GetTable<Amounts>()
				.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k));

			var first = query(db, new[] { 1 })
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			first[0].Values!.Keys.ShouldBe(new[] { "1" });

			var second = query(db, new[] { 2, 3 })
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			second[0].Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "2", "3" });

			second[0].Values!["3"].ShouldBe(31.5m);

			var third = query(db, new[] { 1 })
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			third.Values!.Keys.ShouldBe(new[] { "1" });
		}

		// SQLite only: the SQL is the plain projection ProjectsRuntimeColumnSetIntoStore runs everywhere; this pins how
		// CompiledTable keys the column set.
		[Test]
		public void SelectDynamicInCompiledQueryWithListKeys([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var query = CompiledQuery.Compile((IDataContext dc, List<string> names) => dc
				.GetTable<Amounts>()
				.SelectDynamic(x => new AmountsDto { Id = x.Id }, names, (x, n) => n == "Usd" ? x.Usd : x.Eur));

			var usd = query(db, new List<string> { "Usd" })
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			usd.Values!["Usd"].ShouldBe(10.5m);

			var both = query(db, new List<string> { "Usd", "Eur" })
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			both.Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "Eur", "Usd" });

			both.Values!["Eur"].ShouldBe(20.25m);
		}

		/// <summary>A name selector reading a compiled query's parameter: each value gets its own names.</summary>
		// SQLite only: the SQL is the plain projection ProjectsRuntimeColumnSetIntoStore runs everywhere; this pins how
		// CompiledTable keys the names.
		[Test]
		public void SelectDynamicInCompiledQueryWithNameSelectorParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };

			var query = CompiledQuery.Compile((IDataContext dc, string prefix) => dc
				.GetTable<Amounts>()
				.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, k => prefix + k));

			var a = query(db, "a_")
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			a.Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "a_1", "a_2" });

			var b = query(db, "b_")
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			b.Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "b_1", "b_2" });

			b.Values!["b_2"].ShouldBe(21m);
		}

		/// <summary>A name selector passed as a compiled query's parameter: each delegate gets its own names.</summary>
		// SQLite only: the SQL is the plain projection ProjectsRuntimeColumnSetIntoStore runs everywhere; this pins how
		// CompiledTable keys the names.
		[Test]
		public void SelectDynamicInCompiledQueryWithNameSelectorDelegate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };

			var query = CompiledQuery.Compile((IDataContext dc, Func<int, string> nameSelector) => dc
				.GetTable<Amounts>()
				.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, nameSelector));

			var first = query(db, k => "x" + k)
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			first.Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "x1", "x2" });

			var second = query(db, k => "y" + k)
				.ToList()
				.OrderBy(r => r.Id)
				.First();

			second.Values!.Keys
				.OrderBy(k => k)
				.ShouldBe(new[] { "y1", "y2" });
		}

		/// <summary>
		/// The name selector's reads are compared at every cache lookup, including a lazy read it never reaches: one that
		/// would throw must not escape the lookup.
		/// </summary>
		// SQLite only: this pins the query cache's lookup, which no provider affects.
		[Test]
		public void InlineNameSelectorUnreachedLazyReadDoesNotThrowOnLookup([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };
			var src     = new List<int> { 1, 2 };
			var lazy    = src.Where(x => 10 / x > 1);

			int[] Ids()
			{
				var query =
					from a in t
					where t.Where(x => x.Id > 0)
						.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, k => k < 0 && lazy.Contains(k) ? "X" : "W" + k)
						.Any(r => r.Id == a.Id && Sql.Property<decimal>(r, "W2") > 50)
					select a.Id;

				return query.ToArray();
			}

			Ids()
				.ShouldBe(new[] { 2 });

			// The selector never reaches the lazy read (every key is >= 0), so making that read throw must not matter.
			src.Add(0);

			Ids()
				.ShouldBe(new[] { 2 });
		}

		/// <summary>The column set is decided once per query, so keys read from an outer row are rejected with a diagnostic.</summary>
		// SQLite only: the builder throws before any SQL is built.
		[Test]
		public void InlineKeysFromOuterRangeVariableThrow([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var query =
				from a in t
				from r in t.Where(x => x.Id == a.Id)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, new[] { a.Id }, (x, k) => x.Usd * k)
				select r.Id;

			var act = () => query.ToArray();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("'keys' argument of SelectDynamic must not depend on the query's range variables");
		}

		// SQLite only: the builder throws before any SQL is built.
		[Test]
		public void InlineNameSelectorFromOuterRangeVariableThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var factors = new[] { 1, 2 };

			var query =
				from a in t
				from r in t.Where(x => x.Id == a.Id)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, factors, (x, k) => x.Usd * k, k => a.Id + "_" + k)
				select r.Id;

			var act = () => query.ToArray();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("'nameSelector' argument of SelectDynamic must not depend on the query's range variables");
		}

		[Test]
		[ActiveIssue(5953, ErrorType = typeof(InvalidOperationException), ErrorMessage = "variable 'n' of type 'System.String' referenced from scope '', but it is not defined")]
		public void SelectDynamicOverACorrelatedSource([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var names = new[] { "Usd", "Eur" };

			var correlated =
				from a in t
				from r in t.Where(x => x.Id == a.Id)
					.SelectDynamic(x => new AmountsDto { Id = x.Id }, names, (x, n) => Sql.Property<decimal>(x, n))
				select new { a.Id, Eur = Sql.Property<decimal>(r, "Eur") };

			correlated
				.ToArray()
				.OrderBy(r => r.Id)
				.Select(r => (r.Id, r.Eur))
				.ShouldBe(new (int, decimal)[] { (1, 20.25m), (2, 40m) });
		}

		/// <summary>
		/// A generated name is a store key, not SQL text: quote characters and spaces survive the round trip, and two
		/// names that differ only in such characters stay distinct.
		/// </summary>
		[Test]
		public void NamesNeedNoIdentifierSyntax([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			var result = t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd", "Eur" },
					(x, n) => Sql.Property<decimal>(x, n),
					n => n == "Usd" ? "U's \"[D]\"" : "Us D")
				.ToList()
				.OrderBy(r => r.Id)
				.ToList();

			result[0].Values!["U's \"[D]\""].ShouldBe(10.5m);
			result[0].Values!["Us D"].ShouldBe(20.25m);
			result[1].Values!["U's \"[D]\""].ShouldBe(30m);
			result[1].Values!["Us D"].ShouldBe(40m);
		}

		// SQLite only: the call throws before any SQL is built.
		[Test]
		public void ResultWithoutStoreThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			// Fails at the call, before the query is enumerated.
			Action act = () => t
				.SelectDynamic(
					x => new NoStoreDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n));

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain("has no dynamic column setter");
		}

		/// <summary>
		/// The runtime key set decides the output column set, so two different sets must never share a compiled
		/// query. The <see cref="List{T}"/> legs are the regression guard: a <c>List&lt;T&gt;</c> constant is
		/// dropped from the query cache key, after which the <c>SqlQueryDependent</c> comparison is skipped
		/// entirely - so if the operator ever stops materializing the keys to an array, the last leg collides.
		/// </summary>
		// SQLite only: a query-cache test.
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

			_ = Build(new[] { "Usd" })
				.ToSqlQuery();
			_ = Build(new[] { "Usd" })
				.ToSqlQuery();

			var misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(1, "the same column set must reuse the compiled query");

			_ = Build(new[] { "Usd", "Eur" })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2, "a different column set must not reuse it");

			_ = Build(new List<string> { "Usd" })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2, "a List with the same content must still hit");

			_ = Build(new List<string> { "Eur" })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(3, "a List with different content must not collide");
		}

		/// <summary>
		/// Column names are computed when the query is built, and they only affect materialization - the SQL can
		/// be identical. They must still take part in the cache key, or a second query materializes into the
		/// first one's store keys.
		/// </summary>
		// SQLite only: a query-cache test.
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

			var plain = Build(n => n)
				.ToList();
			var prefixed = Build(n => "c_" + n)
				.ToList();

			var misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2);

			plain[0].Values!.ShouldContainKey("Usd");
			prefixed[0].Values!.ShouldContainKey("c_Usd");
			prefixed[0].Values!.ShouldNotContainKey("Usd");
		}

		/// <summary>Same keys, different per-column expression - the template must take part in the cache key.</summary>
		// SQLite only: a query-cache test.
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

			_ = t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n))
				.ToSqlQuery();

			_ = t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n) * 2)
				.ToSqlQuery();

			var misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2);
		}

		// SQLite only: the call throws before any SQL is built.
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

			act.ShouldThrow<ArgumentException>().Message.ShouldContain("Duplicate dynamic column name");
		}

		// SQLite only: the call throws before any SQL is built.
		[Test]
		public void EmptyNameThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(_data);

			Action act = () => t
				.SelectDynamic(
					x => new AmountsDto { Id = x.Id },
					new[] { "Usd" },
					(x, n) => Sql.Property<decimal>(x, n),
					_ => "")
				.ToList();

			act.ShouldThrow<ArgumentException>().Message.ShouldContain("empty dynamic column name");
		}

		/// <summary>A generated column named after a real member would be shadowed by it, silently.</summary>
		// SQLite only: the call throws before any SQL is built.
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

			act.ShouldThrow<ArgumentException>().Message.ShouldContain("collides with a member");
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
			=> currencyIds
				.Select(AmountSet)
				.Aggregate(Or);

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
			rows
				.Select(r => r.Id)
				.ShouldBe(new[] { 1, 2 });

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
		// SQLite only: a query-cache test.
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

			_ = Build(new[] { 1, 2 })
				.ToSqlQuery();
			_ = Build(new[] { 1, 2 })
				.ToSqlQuery();

			var misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(1, "the same currency set must reuse the compiled query");

			_ = Build(new[] { 1, 3 })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(2, "a different currency set must not reuse it");

			_ = Build(new[] { 1, 2, 3 })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(3, "a wider currency set must not reuse it either");

			_ = Build(new[] { 1, 2 })
				.ToSqlQuery();

			misses = probe.GetCacheMissCount() - start;
			misses.ShouldBe(3, "the first set's entry must survive the other two");

			// Both halves track the set - the projected columns and the predicate.
			var sql = Build(new[] { 1, 3 })
				.ToSqlQuery()
				.Sql;

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
		// SQLite only: the test embeds literal FromSql text.
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
