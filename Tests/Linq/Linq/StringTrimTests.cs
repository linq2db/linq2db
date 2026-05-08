using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
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
		};

		const string TrimCharsUnsupported =
			TestProvName.AllSqlServer2019Minus + ","
			+ ProviderName.SqlCe              + ","
			+ TestProvName.AllSybase          + ","
			+ TestProvName.AllAccess;

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
		public void TrimStartChar_MultiCharSet([DataSources(TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql(t.CharColumn!.TrimStart('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimStartNChar_MultiCharSet([DataSources(TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllInformix)] string context)
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
		public void TrimEndChar_MultiCharSet([DataSources(TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.CharColumn! + "...++").TrimEnd('.', '+')));

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted(TrimCharsUnsupported)]
		public void TrimEndNChar_MultiCharSet([DataSources(TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllInformix)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(SeedRows);

			var query = table.Select(t => Sql.AsSql((t.NCharColumn! + "...++").TrimEnd('.', '+')));

			AssertQuery(query);
		}

		#endregion

		#region Translation tests — DataType propagation

		[Test]
		public void TrimStartCharsType_VarChar([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.VarCharColumn!.TrimStart('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.VarChar);
		}

		[Test]
		public void TrimStartCharsType_NVarChar([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.NVarCharColumn!.TrimStart('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.NVarChar);
		}

		[Test]
		public void TrimStartCharsType_Char([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.CharColumn!.TrimStart('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.Char);
		}

		[Test]
		public void TrimStartCharsType_NChar([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.NCharColumn!.TrimStart('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.NChar);
		}

		[Test]
		public void TrimEndCharsType_VarChar([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.VarCharColumn!.TrimEnd('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.VarChar);
		}

		[Test]
		public void TrimEndCharsType_NVarChar([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var query = table.Select(t => t.NVarCharColumn!.TrimEnd('.', '+'));

			var sqlExpr = query.GetSelectQuery().Find(e => e is SqlValue sv && (sv.Value as string) == ".+");

			sqlExpr.ShouldNotBeNull();
			((SqlValue)sqlExpr!).ValueType.DataType.ShouldBe(DataType.NVarChar);
		}

		[Test]
		public void TrimStartChars_SqlEmitsCharsLiteral([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var sql = table.Select(t => t.VarCharColumn!.TrimStart('.', '+')).ToSqlQuery().Sql;

			sql.ShouldContain(".+");
		}

		[Test]
		public void TrimEndChars_SqlEmitsCharsLiteral([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var sql = table.Select(t => t.VarCharColumn!.TrimEnd('.', '+')).ToSqlQuery().Sql;

			sql.ShouldContain(".+");
		}

		[Test]
		public void TrimStartChars_CapturedVarChangesProduceDifferentSql([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var charsA = new[] { '.', '+' };
			var charsB = new[] { 'a', 'b' };

			var sqlA = table.Select(t => t.VarCharColumn!.TrimStart(charsA)).ToSqlQuery().Sql;
			var sqlB = table.Select(t => t.VarCharColumn!.TrimStart(charsB)).ToSqlQuery().Sql;

			sqlA.ShouldContain(".+");
			sqlB.ShouldContain("ab");
			sqlA.ShouldNotBe(sqlB);
		}

		[Test]
		public void TrimEndChars_CapturedVarChangesProduceDifferentSql([DataSources(TrimCharsUnsupported)] string context)
		{
			using var db = GetDataContext(context);
			var table = db.GetTable<StringTrimTable>();

			var charsA = new[] { '.', '+' };
			var charsB = new[] { 'a', 'b' };

			var sqlA = table.Select(t => t.VarCharColumn!.TrimEnd(charsA)).ToSqlQuery().Sql;
			var sqlB = table.Select(t => t.VarCharColumn!.TrimEnd(charsB)).ToSqlQuery().Sql;

			sqlA.ShouldContain(".+");
			sqlB.ShouldContain("ab");
			sqlA.ShouldNotBe(sqlB);
		}

		#endregion
	}
}
