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

			var query =
				from   e in table
				where  string.Concat(e.StrReq, " I") == "Programmer I"
				select e.StrReq;

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringStringInt_MixedTypes([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  string.Concat(e.StrReq, " ", 1) == "Programmer 1"
				select e.StrReq;

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Concat_NullableArgs_StringConcat_TreatsNullAsEmpty([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// string.Concat is registered with PreserveNull=false: each null operand is wrapped
			// in COALESCE(x, '') by ConvertConcat, so the result is never null even when all
			// inputs are null. Every row should match `!= null` regardless of Str2 nullability.
			var query =
				from   e in table
				where  string.Concat(e.Str1, e.Str2) != null
				select e.Id;

			AssertQuery(query);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1916")]
		public void Concat_BothArgsNonNull_SqlConcat_ReturnsNonNull([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  Sql.Concat(e.StrReq, e.StrReq) != null
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_FourArgs_Chain([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  string.Concat(e.Str1, " ", e.StrReq, "!") == "John Programmer!"
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_MixedNumericAndString([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  string.Concat((object)e.Num, "-", e.StrReq) == "100-Programmer"
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_InSelectProjection_ReturnsValue([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from    e in table
				orderby e.Id
				select  string.Concat(e.Str1, "/", e.StrReq);

			AssertQuery(query);
		}

		[Test]
		public void Concat_InOrderBy_GeneratesValidSql([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from    e in table
				orderby string.Concat(e.StrReq, "X")
				select  e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringArray_FromArrayLiteral([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  string.Concat(new[] { e.StrReq, " ", "I" }) == "Programmer I"
				select e.Id;

			AssertQuery(query);
		}

		[Table("ConcatGroupedEntity")]
		sealed class ConcatGroupedEntity
		{
			[PrimaryKey]          public int     PK    { get; set; }
			[Column]              public int     GrpId { get; set; }
			[Column,    Nullable] public string? Value { get; set; }
		}

		static readonly ConcatGroupedEntity[] GroupedData =
		{
			new() { PK = 1, GrpId = 1, Value = "A" },
			new() { PK = 2, GrpId = 1, Value = "B" },
			new() { PK = 3, GrpId = 2, Value = "C" },
			new() { PK = 4, GrpId = 2, Value = null },
			new() { PK = 5, GrpId = 3, Value = "E" },
		};

		[Test]
		public void Concat_OverGrouping_EmitsAggregate([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var query =
				from g in table.GroupBy(e => e.GrpId)
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.Select(x => x.Value)),
				};

			AssertQuery(query);
		}

		[Test]
		public void Concat_OverGrouping_FiltersNullValues([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var query =
				from g in table.GroupBy(e => e.GrpId)
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.Select(x => x.Value).Where(x => x != null)),
				};

			AssertQuery(query);
		}

		// .Where(...).Distinct().OrderBy(...) inside string.Concat trips an upstream overload-resolution
		// bug at ExpressionBuilder.Aggregation.cs:421 — Expression.Call(typeof(string), "Concat", ...) is
		// ambiguous between string.Concat(IEnumerable<string?>), string.Concat(params string?[]), and
		// string.Concat<T>(IEnumerable<T>). MySQL happens to dodge the path; SQLite/SqlServer/Oracle hit
		// it. Separate from the SqlConcatExpression/withoutSeparator translation path covered here.
		[ActiveIssue(Configurations = [TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllOracle])]
		[Test]
		public void Concat_OverGrouping_DistinctNullableValues([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var query =
				from g in table.GroupBy(e => e.GrpId)
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.Select(x => x.Value).Where(x => x != null).Distinct().OrderBy(x => x)),
				};

			AssertQuery(query);
		}
	}
}
