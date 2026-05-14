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

		// Runs on every provider — including those in TrimCharsUnsupported, where the
		// projection falls back to client-side evaluation. The result is compared
		// against an in-memory-computed expected set so the test passes whether the
		// trim runs server-side or client-side; the cache-miss check verifies that a
		// new chars value produces a fresh cache entry rather than reusing the
		// previous chars baked into a stale plan.

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

			if (iteration == 2)
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

			if (iteration == 2)
			{
				query.GetCacheMissCount().ShouldBeGreaterThan(miss);
			}
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
			Assert.That(sql, Contains.Substring(trimToken));
		}

		[Test]
		public void TrimStartChars_CharColumn_SqlShape([IncludeDataSources(CharColumnPaddingMismatch)] string context)
		{
			using var db    = (TestDataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<StringTrimTable>();

			_ = table.Select(t => t.CharColumn!.TrimStart('.', '+')).ToList();

			var sql       = db.LastQuery!;
			var trimToken = context.IsAnyOf(TestProvName.AllClickHouse) ? "trim(LEADING" : "LTRIM(";
			Assert.That(sql, Contains.Substring(trimToken));
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
	}
}
