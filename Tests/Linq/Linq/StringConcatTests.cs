using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

		// C# compiler emits `a + b` on strings as BinaryExpression(Add, a, b, Method = string.Concat).
		// Regression test for the registration-handler fix that synthesizes a MethodCallExpression
		// from such a BinaryExpression.
		[Test]
		public void Concat_BinaryAddOperator_StringConcat([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from   e in table
				where  e.StrReq + " I" == "Programmer I"
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

		[Test]
		public void Concat_AggregateExecute_OverWholeTable([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = table.AggregateExecute(e => string.Concat(e.OrderBy(x => x.PK).Select(x => x.Value)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Select(x => x.Value));

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void Concat_AggregateExecute_NullableFiltered([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = table.AggregateExecute(e => string.Concat(e.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null));

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public async Task Concat_AggregateExecute_NullableFilteredAsync([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = await table.AggregateExecuteAsync(e => string.Concat(e.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null));

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void Concat_AggregateExecute_OuterFilter([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = table.AggregateExecute(e => string.Concat(e.OrderBy(x => x.PK).Where(x => x.Value != null).Select(x => x.Value)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Where(x => x.Value != null).Select(x => x.Value));

			Assert.That(actual, Is.EqualTo(expected));
		}

		[Test]
		public void Concat_OverGroupingWithTake([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var query = from t in table
				group t by t.GrpId
				into g
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.OrderBy(x => x.PK).Select(x => x.Value).Take(2)),
				}
				into s
				orderby s.Id
				select s;

			AssertQuery(query);
		}

		[Test]
		public void Concat_AggregateArrayPerRow([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from t in table
				orderby t.Id
				select new
				{
					t.Id,
					Aggregated = Sql.AsSql(string.Concat(new[] { t.Str1, t.Str2, t.StrReq })),
				};

			AssertQuery(query);
		}

		[Test]
		public void Concat_AggregateArrayPerRow_NotNull([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from t in table
				orderby t.Id
				select new
				{
					t.Id,
					NotNull = Sql.AsSql(string.Concat(new[] { t.Str1, t.Str2, t.StrReq }.Where(x => x != null))),
				};

			AssertQuery(query);
		}

		[Table("ConcatParent")]
		sealed class ConcatParent
		{
			[PrimaryKey]          public int     Id   { get; set; }
			[Column]              public string  Name { get; set; } = string.Empty;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ConcatChild.ParentId), CanBeNull = true)]
			public List<ConcatChild> Children { get; set; } = null!;
		}

		[Table("ConcatChild")]
		sealed class ConcatChild
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     ParentId { get; set; }
			[Column,    Nullable] public string? Value    { get; set; }
		}

		static readonly ConcatParent[] ParentData =
		{
			new() { Id = 1, Name = "P1" },
			new() { Id = 2, Name = "P2" },
		};

		static readonly ConcatChild[] ChildData =
		{
			new() { Id = 1, ParentId = 1, Value = "A" },
			new() { Id = 2, ParentId = 1, Value = "B" },
			new() { Id = 3, ParentId = 2, Value = null },
			new() { Id = 4, ParentId = 2, Value = "C" },
		};

		[Test]
		public void Concat_AssociationSubquery([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db          = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(ParentData);
			using var childTable  = db.CreateLocalTable(ChildData);

			var query =
				from p in parentTable.LoadWith(x => x.Children)
				orderby p.Id
				select new
				{
					p.Id,
					Children = string.Concat(p.Children.OrderBy(c => c.Id).Select(c => c.Value)),
				};

			AssertQuery(query);
		}
	}
}
