using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
			+ TestProvName.AllFirebird        + ","   // TRIM(LEADING/TRAILING chars FROM val) treats chars as substring, not set
			+ TestProvName.AllMySql;                  // same — covers MySql.Data, MySqlConnector, MariaDB

		// CHAR(n)/NCHAR(n) columns return space-padded values from the server while .NET
		// in-memory data is unpadded — AssertQuery sees a mismatch even though the trim
		// translation itself is correct (verified via the generated SQL). Skip Char/NChar
		// tests on these providers.
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
			var expected = SeedRows.Select(t => t.VarCharColumn!.TrimStart(chars)).ToArray();

			var query  = table.Select(t => t.VarCharColumn!.TrimStart(chars));
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
			var expected = SeedRows.Select(t => t.VarCharColumn!.TrimEnd(chars)).ToArray();

			var query  = table.Select(t => t.VarCharColumn!.TrimEnd(chars));
			var miss   = query.GetCacheMissCount();
			var result = query.ToArray();

			result.ShouldBe(expected);

			if (iteration == 2)
			{
				query.GetCacheMissCount().ShouldBeGreaterThan(miss);
			}
		}
	}
}
