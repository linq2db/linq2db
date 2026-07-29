#if NET5_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class DistinctByMethodTests : TestBase
	{
		public class TestData
		{
			public int      Id       { get; set; }
			public string   Name     { get; set; } = null!;
			public string   Group    { get; set; } = null!;
			// YDB requires a primary key on every table; Id has intentional duplicates, so key on the unique Date column.
			[PrimaryKey]
			public DateTime Date     { get; set; }
			public decimal  Amount   { get; set; }
			public bool     IsActive { get; set; }
			public int?     Priority { get; set; }

			public static List<TestData> Seed()
			{
				return
				[
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 1), Amount  = 100.0m, IsActive = true,  Priority = 5    },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 2), Amount  = 200.0m, IsActive = false, Priority = null },
					new TestData { Id = 1, Name = "Alice", Group   = "A", Date = new DateTime(2023, 1, 3), Amount  = 150.0m, IsActive = true,  Priority = null },
					new TestData { Id = 3, Name = "Charlie", Group = "A", Date = new DateTime(2023, 1, 4), Amount  = 300.0m, IsActive = true,  Priority = 3    },
					new TestData { Id = 4, Name = "David", Group   = "B", Date = new DateTime(2023, 1, 5), Amount  = 400.0m, IsActive = false, Priority = 1    },
					new TestData { Id = 2, Name = "Bob", Group     = "B", Date = new DateTime(2023, 1, 6), Amount  = 250.0m, IsActive = false, Priority = 2    },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 7), Amount  = 500.0m, IsActive = true,  Priority = null },
					new TestData { Id = 6, Name = "Frank", Group   = "C", Date = new DateTime(2023, 1, 8), Amount  = 600.0m, IsActive = true,  Priority = 4    },
					new TestData { Id = 5, Name = "Eve", Group     = "C", Date = new DateTime(2023, 1, 9), Amount  = 550.0m, IsActive = true,  Priority = 6    },
					new TestData { Id = 7, Name = "Grace", Group   = "D", Date = new DateTime(2023, 1, 10), Amount = 700.0m, IsActive = false, Priority = null }
				];
			}
		}

		public class NullableKeyData
		{
			[PrimaryKey]
			public int     Id         { get; set; }
			public string  CustomerId { get; set; } = null!;
			[Nullable]
			public string? Country    { get; set; }
			public string  Region     { get; set; } = null!;

			public static List<NullableKeyData> Seed()
			{
				return
				[
					new NullableKeyData { Id = 1, CustomerId = "DST01", Country = "UK",     Region = "North" },
					new NullableKeyData { Id = 2, CustomerId = "DST02", Country = "USA",    Region = "South" },
					new NullableKeyData { Id = 3, CustomerId = "DST03", Country = "UK",     Region = "North" },
					new NullableKeyData { Id = 4, CustomerId = "DST04", Country = null,     Region = "South" },
					new NullableKeyData { Id = 5, CustomerId = "DST05", Country = "France", Region = "North" },
					new NullableKeyData { Id = 6, CustomerId = "OTH01", Country = "USA",    Region = "South" },
					new NullableKeyData { Id = 7, CustomerId = "DST06", Country = null,     Region = "North" }
				];
			}
		}

		public class RelatedData
		{
			[PrimaryKey]
			public int    Id      { get; set; }
			public int    OwnerId { get; set; }
			public string Tag     { get; set; } = null!;

			public static List<RelatedData> Seed()
			{
				return
				[
					new RelatedData { Id = 1, OwnerId = 1, Tag = "a" },
					new RelatedData { Id = 2, OwnerId = 1, Tag = "b" },
					new RelatedData { Id = 3, OwnerId = 2, Tag = "c" },
					new RelatedData { Id = 4, OwnerId = 3, Tag = "d" },
					new RelatedData { Id = 5, OwnerId = 5, Tag = "e" },
					new RelatedData { Id = 6, OwnerId = 7, Tag = "f" }
				];
			}
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByNullableKeyAfterWhere([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			// A nullable key column combined with a preceding Where: the ROW_NUMBER rewrite must not leave a
			// dangling column reference behind ("Table not found for '...'" at SQL-build time).
			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.CustomerId)
				.DistinctBy(c => c.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByNullableKeyNoFilter([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.OrderBy(c => c.CustomerId)
				.DistinctBy(c => c.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByNonNullableKeyAfterWhere([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.CustomerId);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByFilterAfterOrderBy([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			// Same wrappers as DistinctByNullableKeyAfterWhere, stacked in the other order.
			var query = table
				.OrderBy(c => c.CustomerId)
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.DistinctBy(c => c.Country);

			AssertQuery(query);
		}

		[Test]
		public void DistinctByAfterWhereKeepsFilterAndSingleTable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country);

			AssertQuery(query);

			// The ROW_NUMBER rewrite re-enters the builder with a reference to the already-built source. Resolving
			// that reference used to peel every SubQueryContext wrapper off it, dropping the wrapper that carried the
			// preceding Where and leaving the OVER clause pointing at a query no longer in the tree. Pin both halves:
			// the filter still reaches the SQL, and the source is built once (a second build would leak an extra
			// table reference into the ROW_NUMBER subquery).
			var sql = query.ToSqlQuery().Sql;

			Assert.That(sql, Does.Contain("ROW_NUMBER"));
			Assert.That(sql, Does.Contain("LIKE"));
			Assert.That(sql.Split("NullableKeyData").Length - 1, Is.EqualTo(1));
		}

		// Each test below stacks a different wrapper shape between the source and DistinctBy. The ROW_NUMBER
		// rewrite re-enters the builder with a reference to the already-built sequence, so every wrapper in
		// that stack has to survive the re-entry — dropping one silently loses whatever it carried.

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterTwoFilters([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.Where  (c => c.Id > 1)
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterProjection([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			// SelectContext + an IsSelectWrapper SubQueryContext between the filter and DistinctBy.
			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.Select (c => new { c.Id, c.Country, c.Region })
				.OrderBy(x => x.Id)
				.DistinctBy(x => x.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterAsSubQuery([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.AsSubQuery()
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterDistinct([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.Select (c => new { c.Country, c.Region })
				.Distinct()
				.OrderBy(x => x.Region)
				.DistinctBy(x => x.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByCompositeKeyAfterWhere([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => new { c.Country, c.Region });

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterJoin([DataSources] string context)
		{
			using var db      = GetDataContext(context);
			using var table   = db.CreateLocalTable(NullableKeyData.Seed());
			using var related = db.CreateLocalTable(RelatedData.Seed());

			var query =
				(from c in table
				 join r in related on c.Id equals r.OwnerId
				 where c.CustomerId.StartsWith("DST")
				 orderby c.Id, r.Id
				 select new { c.Id, c.Country, r.Tag })
				.DistinctBy(x => x.Country);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByFilteredOnBothSides([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			// A filter before the rewrite (inside the ROW_NUMBER subquery) and one after it (outside).
			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country)
				.Where  (c => c.Id > 1);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void NestedDistinctByWithFilters([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country)
				.Where  (c => c.Id < 100)
				.OrderBy(c => c.CustomerId)
				.DistinctBy(c => c.Region);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByAfterWhereThenTake([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(NullableKeyData.Seed());

			var query = table
				.Where  (c => c.CustomerId.StartsWith("DST"))
				.OrderBy(c => c.Id)
				.DistinctBy(c => c.Country)
				.OrderBy(c => c.Id)
				.Take(2);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.OrderBy(t => t.Name)
				.ThenByDescending(t => t.Date)
				.DistinctBy(x => new { x.Id, x.Name });

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByOrderByNulls(
			[DataSources] string context,
			[Values(Sql.NullsPosition.First, Sql.NullsPosition.Last)] Sql.NullsPosition nulls,
			[Values] bool descending)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// DistinctBy lowers the preceding OrderBy into ROW_NUMBER(); the NULLS position must reach the OVER
			// clause and select which row survives per group.
			var ordered = descending
				? table.OrderByDescending(t => t.Priority, nulls)
				: table.OrderBy          (t => t.Priority, nulls);

			var query = ordered
				.ThenBy(t => t.Id)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group);

			AssertQuery(query);
		}

		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		[Test]
		public void DistinctByDefaultNullsPosition([DataSources] string context)
		{
			// The preceding plain OrderBy is extracted for the ROW_NUMBER rewrite and bypasses OrderByBuilder,
			// so the configured default NULLS position must still be applied — same survivor per group as the
			// explicit Sql.NullsPosition.Last overload.
			using var db    = GetDataContext(context, o => o.UseDefaultNullsPosition(Sql.NullsPosition.Last));
			using var table = db.CreateLocalTable(TestData.Seed());

			var byDefault = table
				.OrderBy(t => t.Priority).ThenBy(t => t.Id)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Group).Select(x => x.Id).ToList();

			var byExplicit = table
				.OrderBy(t => t.Priority, Sql.NullsPosition.Last).ThenBy(t => t.Id)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Group).Select(x => x.Id).ToList();

			byDefault.ShouldBe(byExplicit);
		}

		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = ErrorHelper.Error_DistinctByRequiresOrderBy)]
		[Test]
		public void DistinctByNoOrder([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.DistinctBy(x => new { x.Id, x.Name });

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted]
		public void DistinctByWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var comparer = EqualityComparer<int>.Default;

			_ = table
				.OrderBy(t => t.Name)
				.DistinctBy(x => x.Id, comparer)
				.ToList();
		}

		[ThrowsForProvider(typeof(LinqToDBException), ErrorMessage = ErrorHelper.Error_DistinctByRequiresOrderBy)]
		[Test]
		public void DistinctByWithComparerOrderShouldFail([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// A preceding OrderBy that carries a custom IComparer<T> has no SQL form, so it must not be
			// extracted as a plain ordering for the ROW_NUMBER rewrite (which would silently drop the comparer).
			var query = table
				.OrderBy(t => t.Name, Comparer<string>.Default)
				.DistinctBy(x => x.Group);

			AssertQuery(query);
		}

		[Test]
		public void DistinctByEmitsDistinctOn([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group);

			AssertQuery(query);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("DISTINCT ON"));
			Assert.That(sql, Does.Not.Contain("ROW_NUMBER"));
		}

		[Test]
		public void DistinctByCompositeKeyEmitsDistinctOn([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.OrderBy(t => t.Name)
				.ThenByDescending(t => t.Date)
				.DistinctBy(x => new { x.Id, x.Name });

			AssertQuery(query);

			// Both composite-key columns must reach the ON list: a bare Does.Contain("DISTINCT ON") would still
			// pass if one key column were dropped (DISTINCT ON (Id) instead of DISTINCT ON (Id, Name)).
			Assert.That(query.ToSqlQuery().Sql, Does.Match(@"DISTINCT ON \([^)]*\bId\b[^)]*,[^)]*\bName\b[^)]*\)"));
		}

		[Test]
		public void DistinctByUsesRowNumberWhenDistinctOnUnsupported([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group);

			AssertQuery(query);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Not.Contain("DISTINCT ON"));
			Assert.That(sql, Does.Contain("ROW_NUMBER"));
		}

		[Test]
		public void DistinctByConstantKeyFallsBackToRowNumber([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// A constant key collects to an empty ON list, which has no valid DISTINCT ON form — even on a
			// DISTINCT-ON-capable provider it must fall back to the ROW_NUMBER emulation, not emit DISTINCT ON ().
			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => 1);

			AssertQuery(query);

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Not.Contain("DISTINCT ON"));
			Assert.That(sql, Does.Contain("ROW_NUMBER"));
			// The constant-key fall-through must reuse the single built sequence; a second build would leak an extra
			// table reference into the ROW_NUMBER subquery (FROM TestData e, TestData e_1 — a cartesian product).
			Assert.That(sql.Split("TestData").Length - 1, Is.EqualTo(1));
		}

		[Test]
		public void DistinctByThenTake([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// DISTINCT ON followed by an outer ORDER BY + LIMIT: the inner ON ordering must survive.
			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Group)
				.Take(2);

			AssertQuery(query);
			Assert.That(query.ToSqlQuery().Sql, Does.Contain("DISTINCT ON"));
		}

		[Test]
		public void DistinctByThenWhere([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// A filter applied after DistinctBy must wrap the DISTINCT ON query as a subquery, not push into it.
			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group)
				.Where(x => x.Amount > 100m);

			AssertQuery(query);
			Assert.That(query.ToSqlQuery().Sql, Does.Contain("DISTINCT ON"));
		}

		[Test]
		public void DistinctByInSubQuery([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// Forcing the DISTINCT ON result into a derived table must preserve its ORDER BY (the optimizer
			// must not strip a DISTINCT ON query's ordering when it becomes a subquery).
			var query = table
				.OrderBy(t => t.Group)
				.ThenBy(t => t.Date)
				.DistinctBy(x => x.Group)
				.AsSubQuery()
				.Where(x => x.Amount > 100m);

			AssertQuery(query);
			Assert.That(query.ToSqlQuery().Sql, Does.Contain("DISTINCT ON"));
		}

		[Test]
		public void NestedDistinctBy([IncludeDataSources(TestProvName.AllPostgreSQL, TestProvName.AllDuckDB)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData.Seed());

			// Two stacked DistinctBy stages: the inner DISTINCT ON must remain a nested derived table.
			var query = table
				.OrderBy(t => t.Date)
				.DistinctBy(x => x.Group)
				.OrderBy(x => x.Id)
				.DistinctBy(x => x.Name);

			AssertQuery(query);
			// Both stages must survive as separate DISTINCT ON queries: a bare Does.Contain would pass even if the
			// optimizer collapsed the two nested DISTINCT ONs into one.
			Assert.That(query.ToSqlQuery().Sql.Split("DISTINCT ON").Length - 1, Is.EqualTo(2));
		}
	}
}

#endif
