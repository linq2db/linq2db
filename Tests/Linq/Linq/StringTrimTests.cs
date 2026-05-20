using System.Linq;

using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class StringTrimTests : TestBase
	{
		[Table]
		sealed class StringTrimTable
		{
			[PrimaryKey]                                                          public int     Id             { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.Char)]     public string? CharColumn     { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NChar)]    public string? NCharColumn    { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.VarChar)]  public string? VarCharColumn  { get; set; }
			[Column(Length = 50, CanBeNull = true, DataType = DataType.NVarChar)] public string? NVarCharColumn { get; set; }
		}

		static readonly StringTrimTable[] SeedRows =
		{
			new() { Id = 1, CharColumn = "...hello",  NCharColumn = "...héllo",  VarCharColumn = "...hello",  NVarCharColumn = "...héllo"  },
			new() { Id = 2, CharColumn = "..++world", NCharColumn = "..++wörld", VarCharColumn = "..++world", NVarCharColumn = "..++wörld" },
			new() { Id = 3, CharColumn = "noprefix",  NCharColumn = "noprefix",  VarCharColumn = "noprefix",  NVarCharColumn = "noprefix"  },
			new() { Id = 4, CharColumn = ".+.+world", NCharColumn = ".+.+wörld", VarCharColumn = ".+.+world", NVarCharColumn = ".+.+wörld" },
		};

		const string TrimCharsUnsupported =
			TestProvName.AllSqlServer2019Minus + ","
			+ ProviderName.SqlCe              + ","
			+ TestProvName.AllSybase          + ","
			+ TestProvName.AllAccess          + ","
			+ TestProvName.AllFirebird        + ","   // TRIM(LEADING/TRAILING chars FROM val) is substring, not set; no native regex
			+ TestProvName.AllMySql57;                // no REGEXP_REPLACE (added in MySQL 8.0); same substring-vs-set issue

		// CHAR(n)/NCHAR(n) columns return space-padded values from the server while .NET
		// in-memory data is unpadded — AssertQuery sees a mismatch on these providers even
		// though the chars-trim SQL itself is well-formed. The VarChar/NVarChar variants
		// exercise the same translator path; SQL shape on the CHAR side is covered
		// separately by TrimChars_CharColumn_SqlShape_* below.
		const string CharColumnPaddingMismatch =
			TestProvName.AllOracle             + ","
			+ TestProvName.AllDB2              + ","
			+ TestProvName.AllInformix         + ","
			+ TestProvName.AllSqlServer2022Plus + ","
			+ TestProvName.AllClickHouse;

		#region Result-equivalence tests with forced translation

		[Test]
		public void TrimStartVarChar_NoArgs([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(("   " + t.VarCharColumn!).TrimStart()));

			AssertQuery(query);
		}

		[Test]
		public void TrimStartVarChar_EmptyArray([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(("   " + t.VarCharColumn!).TrimStart(new char[0])));

			AssertQuery(query);
		}

		[Test]
		public void TrimStartVarChar_NullArray([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			// .NET docs: TrimStart((char[])null) is equivalent to TrimStart() — whitespace trim.
			var query = table.Select(t => Sql.AsSql(("   " + t.VarCharColumn!).TrimStart((char[])null!)));

			AssertQuery(query);
		}

#if NET8_0_OR_GREATER
		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartVarChar_SingleChar([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(t.VarCharColumn!.TrimStart('.')));

			AssertQuery(query);
		}
#endif

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartVarChar_MultiCharSet_Literal([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(t.VarCharColumn!.TrimStart('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartVarChar_MultiCharSet_Param([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.Select(t => Sql.AsSql(t.VarCharColumn!.TrimStart(chars)));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartNVarChar_MultiCharSet_Param([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.Select(t => Sql.AsSql(t.NVarCharColumn!.TrimStart(chars)));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartChar_MultiCharSet([DataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(t.CharColumn!.TrimStart('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartNChar_MultiCharSet([DataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(t.NCharColumn!.TrimStart('.', '+')));

			AssertQuery(query);
		}

		[Test]
		public void TrimEndVarChar_NoArgs([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "   ").TrimEnd()));

			AssertQuery(query);
		}

		[Test]
		public void TrimEndVarChar_EmptyArray([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "   ").TrimEnd(new char[0])));

			AssertQuery(query);
		}

		[Test]
		public void TrimEndVarChar_NullArray([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			// .NET docs: TrimEnd((char[])null) is equivalent to TrimEnd() — whitespace trim.
			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "   ").TrimEnd((char[])null!)));

			AssertQuery(query);
		}

#if NET8_0_OR_GREATER
		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndVarChar_SingleChar([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "...").TrimEnd('.')));

			AssertQuery(query);
		}
#endif

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndVarChar_MultiCharSet_Literal([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "...++").TrimEnd('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndVarChar_MultiCharSet_Param([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.Select(t => Sql.AsSql((t.VarCharColumn! + "...++").TrimEnd(chars)));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndNVarChar_MultiCharSet_Param([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.Select(t => Sql.AsSql((t.NVarCharColumn! + "...++").TrimEnd(chars)));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndChar_MultiCharSet([DataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.CharColumn! + "...++").TrimEnd('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndNChar_MultiCharSet([DataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.NCharColumn! + "...++").TrimEnd('.', '+')));

			AssertQuery(query);
		}

		// Analogues of PR #5514's tests, using the local StringTrimTable model and
		// AssertQuery without Sql.AsSql. No [ThrowsCannotBeConverted] — providers
		// where chars-trim can't translate fall back to client-side projection eval,
		// which the framework supports out of the box now.

		[Test]
		public void TrimStart0Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => ("   " + t.VarCharColumn!).TrimStart(new char[0]));

			AssertQuery(query);
		}

#if NET8_0_OR_GREATER
		[Test]
		public void TrimStart1Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => ("..." + t.VarCharColumn!).TrimStart('.'));

			AssertQuery(query);
		}
#endif

		[Test]
		public void TrimStart2Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => ("...++" + t.VarCharColumn!).TrimStart('.', '+'));

			AssertQuery(query);
		}

		[Test]
		public void TrimEnd0Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => (t.VarCharColumn! + "   ").TrimEnd(new char[0]));

			AssertQuery(query);
		}

#if NET8_0_OR_GREATER
		[Test]
		public void TrimEnd1Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => (t.VarCharColumn! + "...").TrimEnd('.'));

			AssertQuery(query);
		}
#endif

		[Test]
		public void TrimEnd2Test([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => (t.VarCharColumn! + "...++").TrimEnd('.', '+'));

			AssertQuery(query);
		}

		#endregion

		// Mirrors PostgreSQLArrayTests.ArrayParameterCacheTest_Int, but inverted:
		// chars are baked as literal SqlValue (via MarkAsNonParameter), so different
		// chars must NOT share a cached query plan — running with new chars must
		// register as a new cache miss, otherwise the cached SQL would carry the
		// previous chars and the second invocation would emit / execute incorrectly.

		// Result correctness runs on every provider — including those in
		// TrimCharsUnsupported, where the projection falls back to client-side
		// evaluation. The result is compared against an in-memory-computed expected
		// set so the test passes whether the trim runs server-side or client-side.
		//
		// The cache-miss check is gated on providers where the chars literal is
		// actually baked into the SQL plan. On TrimCharsUnsupported providers the
		// chars become a regular parameter and reusing the cached plan with a
		// different chars value is safe — asserting a miss there would lock in
		// avoidable cache churn.

		[Test]
		public void TrimStartCharsCacheTest([DataSources] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars    = iteration == 1 ? new[] { '.', '+' } : new[] { 'a', 'b' };
			var expected = SeedRows.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart(chars)).ToArray();

			var query  = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart(chars));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			result.ShouldBe(expected);

			if (iteration == 2 && !context.IsAnyOf(TrimCharsUnsupported))
			{
				// chars value baked as literal — a new chars value must miss the cache
				query.GetCacheMissCount().ShouldBeGreaterThan(miss);
			}
		}

		[Test]
		public void TrimEndCharsCacheTest([DataSources] string context, [Values(1, 2)] int iteration)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars    = iteration == 1 ? new[] { '.', '+' } : new[] { 'a', 'b' };
			var expected = SeedRows.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimEnd(chars)).ToArray();

			var query  = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimEnd(chars));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			result.ShouldBe(expected);

			if (iteration == 2 && !context.IsAnyOf(TrimCharsUnsupported))
			{
				query.GetCacheMissCount().ShouldBeGreaterThan(miss);
			}
		}

		// MarkAsNonParameter's stored value must round-trip via the accessor at runtime —
		// stored type and accessor type must agree, otherwise the cache compare always
		// returns false and trim-with-chars queries miss cache on every execution.
		// Re-executing the same query must hit the cache.
		[Test]
		public void TrimEndCharsCache_HitsOnSameContent([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimEnd(chars));
			query.ToArray();
			var miss = query.GetCacheMissCount();

			query.ToArray();
			query.GetCacheMissCount().ShouldBe(miss);
		}

		// Same query shape constructed via a local function with chars passed as a
		// parameter. Each call creates a separate closure instance but with the same
		// display-class type and same field layout, so structural compare matches.
		// Sorted-string cache key gives set semantics, so a reordered chars argument
		// must still hit the cache.
		[Test]
		public void TrimEndCharsCache_LocalFunctionWithReorderedCharsHits([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			IQueryable<string?> BuildQuery(char[] chars) =>
				table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimEnd(chars));

			var query1 = BuildQuery(new[] { '.', '+' });
			query1.ToArray();
			var miss = query1.GetCacheMissCount();

			var query2 = BuildQuery(new[] { '+', '.' });
			query2.ToArray();
			query2.GetCacheMissCount().ShouldBe(miss);
		}

		// Companion to the cache-hit test: mutating the captured array to a different
		// chars set must register as a cache miss (different sorted-string key) so the
		// stale plan with the original chars literal isn't reused. Only meaningful on
		// providers that bake the chars value into the SQL — on TrimCharsUnsupported
		// providers the translator returns null and the trim runs client-side, where
		// the closure-captured chars is parameter-bound and the cache reuses the same
		// plan across mutations (no stale SQL to worry about).
		[Test]
		public void TrimEndCharsCache_MutationMissesCache([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };
			var query = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimEnd(chars));
			query.ToArray();
			var miss = query.GetCacheMissCount();

			// Mutate to a different chars set — sorted key differs → must miss cache.
			chars[0] = 'a';
			chars[1] = 'b';
			query.ToArray();
			if (!context.IsAnyOf(TrimCharsUnsupported))
				query.GetCacheMissCount().ShouldBeGreaterThan(miss);
		}

		// Cache key for chars-trim is built from a sorted copy of the chars value, so a
		// captured array mutated in place produces a fresh cache entry on next call
		// (different content → different sorted-string cache key). Inline `new[] {…}` array
		// literals don't share the cache across orderings — the expression-tree structure
		// itself differs (different constants in different array positions), and the cache
		// is keyed on structure first, MarkAsNonParameter value second.

		[Test]
		public void TrimStartCharsCache_MutatedCapturedArray([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var chars = new[] { '.', '+' };

			// First run baked into the cache.
			var query1    = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart(chars));
			var result1   = query1.ToArray();
			var expected1 = SeedRows.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart('.', '+')).ToArray();
			result1.ShouldBe(expected1);

			// Mutate the captured array. The second run must NOT reuse the previous plan
			// — the cache key must reflect the new content.
			chars[0] = 'a';
			chars[1] = 'b';

			var query2    = table.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart(chars));
			var result2   = query2.ToArray();
			var expected2 = SeedRows.OrderBy(t => t.Id).Select(t => t.VarCharColumn!.TrimStart('a', 'b')).ToArray();
			result2.ShouldBe(expected2);
		}

		// Obsolete Expressions.TrimLeft/TrimRight statics propagate null source via
		// `str?.TrimStart(trimChars)` — calling with a null source returns null.
		// LegacyMemberConverterBase rewrites the call to the instance method
		// `s.TrimStart(chars)`, which must preserve the null-propagation so projections
		// over nullable columns don't throw NRE on client-side fallback.

		[Test]
		public void LegacyTrimLeftPreservesNullSource([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			db.Insert(new StringTrimTable { Id = 100, VarCharColumn = null });

#pragma warning disable CS0618 // Expressions.TrimLeft is obsolete
			var chars  = new[] { '.', '+' };
			var result = table.Where(t => t.Id == 100).Select(t => Expressions.TrimLeft(t.VarCharColumn, chars)).ToArray();
#pragma warning restore CS0618

			result.Length.ShouldBe(1);
			result[0].ShouldBeNull();
		}

		[Test]
		public void LegacyTrimRightPreservesNullSource([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			db.Insert(new StringTrimTable { Id = 100, VarCharColumn = null });

#pragma warning disable CS0618 // Expressions.TrimRight is obsolete
			var chars  = new[] { '.', '+' };
			var result = table.Where(t => t.Id == 100).Select(t => Expressions.TrimRight(t.VarCharColumn, chars)).ToArray();
#pragma warning restore CS0618

			result.Length.ShouldBe(1);
			result[0].ShouldBeNull();
		}

		// SQL-shape tests for CHAR(n) / NCHAR(n) columns on providers excluded from the
		// AssertQuery path due to CHAR-padding read-back. These tests verify the trim
		// translator emits the expected provider-specific SQL form against the CHAR column.

		[Test]
		public void TrimEndChars_CharColumn_SqlShape([IncludeDataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			_ = table.Select(t => t.CharColumn!.TrimEnd('.', '+')).ToList();

			var sql       = db.LastQuery!;
			var trimToken = context.IsAnyOf(TestProvName.AllClickHouse) ? "trim(TRAILING" : "RTRIM(";
			sql.ShouldContain(trimToken);
		}

		[Test]
		public void TrimStartChars_CharColumn_SqlShape([IncludeDataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			_ = table.Select(t => t.CharColumn!.TrimStart('.', '+')).ToList();

			var sql       = db.LastQuery!;
			var trimToken = context.IsAnyOf(TestProvName.AllClickHouse) ? "trim(LEADING" : "LTRIM(";
			sql.ShouldContain(trimToken);
		}

		// Trim-chars literal must inherit the source column's DbDataType (NVarChar) so
		// SQL Server emits an nvarchar (N'…') literal — a regression where the literal
		// loses nvarchar typing would emit a varchar literal and corrupt non-ASCII chars.
		[Test]
		public void TrimEndNonAsciiChar_NVarCharColumn_LiteralIsNvarchar([IncludeDataSources(TestProvName.AllSqlServer2022Plus)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			_ = table.Select(t => t.NVarCharColumn!.TrimEnd('ö')).ToList();

			db.LastQuery!.ShouldContain("N'ö'");
		}

		[Test]
		public void TrimStartNonAsciiChar_NVarCharColumn_LiteralIsNvarchar([IncludeDataSources(TestProvName.AllSqlServer2022Plus)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			_ = table.Select(t => t.NVarCharColumn!.TrimStart('ö')).ToList();

			db.LastQuery!.ShouldContain("N'ö'");
		}

		// MySQL 8 / MariaDB chars-trim uses REGEXP_REPLACE, which inherits the column
		// collation's case sensitivity by default (utf8mb4_0900_ai_ci on MySQL 8,
		// utf8mb4_general_ci on MariaDB are both case-insensitive). The translator
		// prepends `(?-i)` to force case-sensitive matching so .NET `TrimStart('h')`
		// on "Hello" returns "Hello" (lowercase target, uppercase prefix preserved)
		// rather than "ello".
		[Test]
		public void TrimChars_MySql8_RegexIsCaseSensitive([IncludeDataSources(TestProvName.AllMySql8Plus)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			db.Insert(new StringTrimTable { Id = 100, VarCharColumn = "Hello" });

			// Lowercase 'h' must NOT remove the uppercase 'H' prefix.
			var result = table.Where(t => t.Id == 100).Select(t => t.VarCharColumn!.TrimStart('h')).Single();
			result.ShouldBe("Hello");

			// SQL-shape sentinel: a regression that drops the `(?-i)` flag would fail
			// here even if the test's runtime CI environment happened to use a
			// case-sensitive collation by accident.
			db.LastQuery!.ShouldContain("(?-i)");
		}

		// Oracle: LTRIM/RTRIM may return NULL (empty string => NULL) even with non-null
		// inputs. The translator marks the function nullable so an `IS NULL` predicate
		// on the result is preserved by the optimizer.
		[Test]
		public void TrimEndOracle_EmptyResultBecomesNull([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			db.Insert(new StringTrimTable { Id = 100, VarCharColumn = "aaa" });

			// RTRIM('aaa', 'a') on Oracle returns empty string → NULL.
			var rows = table.Where(t => t.Id == 100 && t.VarCharColumn!.TrimEnd('a') == null).ToList();

			rows.Count.ShouldBe(1);
		}

		[Test]
		public void TrimStartOracle_EmptyResultBecomesNull([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			db.Insert(new StringTrimTable { Id = 100, VarCharColumn = "aaa" });

			// LTRIM('aaa', 'a') on Oracle returns empty string → NULL.
			var rows = table.Where(t => t.Id == 100 && t.VarCharColumn!.TrimStart('a') == null).ToList();

			rows.Count.ShouldBe(1);
		}
	}
}
