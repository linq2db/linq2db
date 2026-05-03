using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class StringConcatTests : TestBase
	{
		[Table("ConcatTestEntity")]
		sealed class ConcatTestEntity
		{
			[PrimaryKey]          public int     Id     { get; set; }
			[Column,    Nullable] public string? Str1   { get; set; }
			[Column,    Nullable] public string? Str2   { get; set; }
			[Column]              public string  StrReq { get; set; } = string.Empty;
			[Column]              public int     Num    { get; set; }
		}

		static readonly ConcatTestEntity[] TestData =
		{
			new() { Id = 1, Str1 = "John",  Str2 = "Smith", StrReq = "Programmer", Num = 100 },
			new() { Id = 2, Str1 = "Jane",  Str2 = null,    StrReq = "Tester",     Num = 200 },
			new() { Id = 3, Str1 = "Bob",   Str2 = "Doe",   StrReq = "Engineer",   Num = 300 },
			new() { Id = 4, Str1 = "Alice", Str2 = null,    StrReq = "Anon",       Num = 400 },
		};

		[Test]
		public void Concat_TwoStrings_LiteralEquality([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			AreEqual(
				from e in TestData where string.Concat(e.StrReq, " I") == "Programmer I" select e.StrReq,
				from e in table    where string.Concat(e.StrReq, " I") == "Programmer I" select e.StrReq);
		}

		[Test]
		public void Concat_StringStringInt_MixedTypes([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			AreEqual(
				from e in TestData where string.Concat(e.StrReq, " ", 1) == "Programmer 1" select e.StrReq,
				from e in table    where string.Concat(e.StrReq, " ", 1) == "Programmer 1" select e.StrReq);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Concat_NullableArgs_StringConcat_TreatsNullAsEmpty([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// string.Concat is registered with PreserveNull=false: each null operand is wrapped
			// in COALESCE(x, '') by ConvertConcat, so the result is never null even when all
			// inputs are null. Every row should match `!= null` regardless of Str2 nullability.
			var cnt = table.Count(e => string.Concat(e.Str1, e.Str2) != null);

			Assert.That(cnt, Is.EqualTo(TestData.Length));
		}

		// Sql.Concat(params) routes through StringMemberTranslatorBase.TranslateConcatNullableList ->
		// TranslateStringJoin -> ExpressionBuilder.BuildArrayAggregationFunction, which trips an
		// ArgumentOutOfRangeException for fixed-arg-list calls. Separate from the SqlConcatExpression
		// lowering covered by the other tests in this fixture.
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Concat_BothArgsNonNull_SqlConcat_ReturnsNonNull([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var cnt = table.Count(e => Sql.Concat(e.StrReq, e.StrReq) != null);

			Assert.That(cnt, Is.EqualTo(TestData.Length));
		}

		[Test]
		public void Concat_FourArgs_Chain([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			AreEqual(
				from e in TestData where string.Concat(e.Str1, " ", e.StrReq, "!") == "John Programmer!" select e.Id,
				from e in table    where string.Concat(e.Str1, " ", e.StrReq, "!") == "John Programmer!" select e.Id);
		}

		[Test]
		public void Concat_MixedNumericAndString([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			AreEqual(
				from e in TestData where string.Concat((object)e.Num, "-", e.StrReq) == "100-Programmer" select e.Id,
				from e in table    where string.Concat((object)e.Num, "-", e.StrReq) == "100-Programmer" select e.Id);
		}

		[Test]
		public void Concat_InSelectProjection_ReturnsValue([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			AreEqual(
				from e in TestData orderby e.Id select string.Concat(e.Str1, "/", e.StrReq),
				from e in table    orderby e.Id select string.Concat(e.Str1, "/", e.StrReq));
		}
	}
}
