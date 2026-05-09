using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
					Value = string.Concat(g.OrderBy(x => x.PK).Select(x => x.Value)),
				};

			AssertQuery(query);
		}

		[Table("ConcatGroupedTypedEntity")]
		sealed class ConcatGroupedTypedEntity
		{
			[PrimaryKey] public int  PK    { get; set; }
			[Column]     public int  GrpId { get; set; }
			[Column]     public Guid GuidV { get; set; }
			[Column]     public int  IntV  { get; set; }
		}

		static readonly ConcatGroupedTypedEntity[] GroupedTypedData =
		{
			new() { PK = 1, GrpId = 1, GuidV = Tests.TestData.Guid1, IntV = 10 },
			new() { PK = 2, GrpId = 1, GuidV = Tests.TestData.Guid2, IntV = 20 },
			new() { PK = 3, GrpId = 2, GuidV = Tests.TestData.Guid3, IntV = 30 },
			new() { PK = 4, GrpId = 2, GuidV = Tests.TestData.Guid4, IntV = 40 },
			new() { PK = 5, GrpId = 3, GuidV = Tests.TestData.Guid5, IntV = 50 },
		};

		// `string.Concat(g.Select(x => x.GuidCol))` — aggregate-over-grouping where the
		// projection is a non-string column. The base CONCAT_WS / LISTAGG / STRING_AGG path
		// translates raw column references; without per-element ToString rewriting a Guid
		// column produces binary representation on SQLite, hex on Oracle, etc. The fix
		// (ConfigureAggregate.TransformValue(ConvertOperandToString) on each provider) routes
		// each Guid through GuidMemberTranslator → Lower(UUID_TO_CHAR(...)) / hex-and-substr.
		[Test]
		public void Concat_OverGrouping_GuidColumn_EmitsToString([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedTypedData);

			var query =
				from g in table.GroupBy(e => e.GrpId)
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.OrderBy(x => x.PK).Select(x => x.GuidV)),
				};

			AssertQuery(query);
		}

		[Test]
		public void Concat_OverGrouping_IntColumn_EmitsToString([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedTypedData);

			var query =
				from g in table.GroupBy(e => e.GrpId)
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = string.Concat(g.OrderBy(x => x.PK).Select(x => x.IntV)),
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
					Value = string.Concat(g.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null)),
				};

			AssertQuery(query);
		}

		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllDB2],      ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllOracle11], ErrorMessage = ErrorHelper.Oracle.Error_ColumnSubqueryShouldNotContainParentIsNotNull)]
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

			actual.ShouldBe(expected);
		}

		[Test]
		public void Concat_AggregateExecute_NullableFiltered([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = table.AggregateExecute(e => string.Concat(e.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null));

			actual.ShouldBe(expected);
		}

		[Test]
		public async Task Concat_AggregateExecute_NullableFilteredAsync([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = await table.AggregateExecuteAsync(e => string.Concat(e.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Select(x => x.Value).Where(x => x != null));

			actual.ShouldBe(expected);
		}

		[Test]
		public void Concat_AggregateExecute_OuterFilter([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			var actual   = table.AggregateExecute(e => string.Concat(e.OrderBy(x => x.PK).Where(x => x.Value != null).Select(x => x.Value)));
			var expected = string.Concat(GroupedData.OrderBy(x => x.PK).Where(x => x.Value != null).Select(x => x.Value));

			actual.ShouldBe(expected);
		}

		// MySQL 8+ (incl. MySQL 9) supports the `.Take(2)`-over-grouping shape via a derived
		// table; only MySQL 5.7 and MariaDB still hit Error_OUTER_Joins, so scope the throws
		// list to AllMySql57 (not AllMySql) — CI on MySQL 9 was tripping "expected exception
		// not thrown" otherwise.
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllDB2, TestProvName.AllOracle11, TestProvName.AllMySql57, TestProvName.AllMariaDB], ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ThrowsForProvider(typeof(LinqToDBException), providers: [TestProvName.AllClickHouse],                                                                      ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
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

		sealed class StringConcatNullEntity
		{
			public int     ID     { get; set; }
			public string? Value1 { get; set; }
			public string? Value2 { get; set; }
		}

		sealed class StringConcatIntGuidNullEntity
		{
			public int     ID     { get; set; }
			public string? Value1 { get; set; }
			public int?    Value2 { get; set; }
			public Guid?   Value3 { get; set; }
		}

		[Test]
		public void Concat_BinaryAddOperator_StringString_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			// Sybase ASE treats the `''` literal as a single-space CHAR(1), so `Coalesce(NULL, '')`
			// evaluates to `' '` rather than an empty string — every null operand contributes a
			// stray leading/trailing space, diverging from C# `string.Concat` semantics. Skip
			// whenever any column is nullable; only the no-null cell is testable on Sybase.
			if ((value1Nullable || value2Nullable) && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase ASE pads `Coalesce(NULL, '')` to a single space — diverges from C# string.Concat for any nullable operand.");

			var data = new[]
				{
					new StringConcatNullEntity { ID = 1, Value1 = "A1", Value2 = "A2" },
					new StringConcatNullEntity { ID = 2, Value1 = null, Value2 = "B2" },
					new StringConcatNullEntity { ID = 3, Value1 = "C1", Value2 = null },
					new StringConcatNullEntity { ID = 4, Value1 = null, Value2 = null }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || t.Value2 != null))
				.ToArray();

			var fb = new FluentMappingBuilder();
			fb.Entity<StringConcatNullEntity>()
				.Property(t => t.ID)    .IsPrimaryKey()
				.Property(t => t.Value1).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value1Nullable)
				.Property(t => t.Value2).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value2Nullable);
			var ms = fb.Build().MappingSchema;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(data);

			var query = table.OrderBy(t => t.ID).Select(t => t.Value1 + t.Value2);

			AssertQuery(query);
		}

		[Test]
		public void Concat_BinaryAddOperator_StringIntGuid_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			if ((value1Nullable || value2Nullable) && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase ASE pads `Coalesce(NULL, '')` to a single space — diverges from C# string.Concat for any nullable operand.");

			var data = new[]
				{
					new StringConcatIntGuidNullEntity { ID = 1, Value1 = "A",  Value2 = 1,    Value3 = Tests.TestData.Guid1 },
					new StringConcatIntGuidNullEntity { ID = 2, Value1 = null, Value2 = 2,    Value3 = Tests.TestData.Guid2 },
					new StringConcatIntGuidNullEntity { ID = 3, Value1 = "C",  Value2 = null, Value3 = null         },
					new StringConcatIntGuidNullEntity { ID = 4, Value1 = null, Value2 = null, Value3 = null         }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || (t.Value2 != null && t.Value3 != null)))
				.ToArray();

			var fb = new FluentMappingBuilder();
			fb.Entity<StringConcatIntGuidNullEntity>()
				.Property(t => t.ID)    .IsPrimaryKey()
				.Property(t => t.Value1).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value1Nullable)
				.Property(t => t.Value2).IsNullable(value2Nullable)
				.Property(t => t.Value3).IsNullable(value2Nullable);
			var ms = fb.Build().MappingSchema;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(data);

			// chained binary `+` mixes string, int? and Guid?; left-fold builds (string + int?) + Guid?.
			var query = table.OrderBy(t => t.ID).Select(t => t.Value1 + t.Value2 + t.Value3);

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringConcat_TwoArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			if ((value1Nullable || value2Nullable) && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase ASE pads `Coalesce(NULL, '')` to a single space — diverges from C# string.Concat for any nullable operand.");

			var data = new[]
				{
					new StringConcatNullEntity { ID = 1, Value1 = "A1", Value2 = "A2" },
					new StringConcatNullEntity { ID = 2, Value1 = null, Value2 = "B2" },
					new StringConcatNullEntity { ID = 3, Value1 = "C1", Value2 = null },
					new StringConcatNullEntity { ID = 4, Value1 = null, Value2 = null }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || t.Value2 != null))
				.ToArray();

			var fb = new FluentMappingBuilder();
			fb.Entity<StringConcatNullEntity>()
				.Property(t => t.ID)    .IsPrimaryKey()
				.Property(t => t.Value1).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value1Nullable)
				.Property(t => t.Value2).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value2Nullable);
			var ms = fb.Build().MappingSchema;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(data);

			var query = table.OrderBy(t => t.ID).Select(t => string.Concat(t.Value1, t.Value2));

			AssertQuery(query);
		}

		sealed class StringConcatThreeNullEntity
		{
			public int     ID     { get; set; }
			public string? Value1 { get; set; }
			public string? Value2 { get; set; }
			public string? Value3 { get; set; }
		}

		[Test]
		public void Concat_StringConcat_ThreeArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable, [Values] bool value3Nullable)
		{
			if ((value1Nullable || value2Nullable || value3Nullable) && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase ASE pads `Coalesce(NULL, '')` to a single space — diverges from C# string.Concat for any nullable operand.");

			var data = new[]
				{
					new StringConcatThreeNullEntity { ID = 1, Value1 = "A1", Value2 = "A2", Value3 = "A3" },
					new StringConcatThreeNullEntity { ID = 2, Value1 = null, Value2 = "B2", Value3 = "B3" },
					new StringConcatThreeNullEntity { ID = 3, Value1 = "C1", Value2 = null, Value3 = "C3" },
					new StringConcatThreeNullEntity { ID = 4, Value1 = "D1", Value2 = "D2", Value3 = null },
					new StringConcatThreeNullEntity { ID = 5, Value1 = null, Value2 = null, Value3 = null }
				}
				.Where(t => (value1Nullable || t.Value1 != null)
				         && (value2Nullable || t.Value2 != null)
				         && (value3Nullable || t.Value3 != null))
				.ToArray();

			var fb = new FluentMappingBuilder();
			fb.Entity<StringConcatThreeNullEntity>()
				.Property(t => t.ID)    .IsPrimaryKey()
				.Property(t => t.Value1).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value1Nullable)
				.Property(t => t.Value2).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value2Nullable)
				.Property(t => t.Value3).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value3Nullable);
			var ms = fb.Build().MappingSchema;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(data);

			var query = table.OrderBy(t => t.ID).Select(t => string.Concat(t.Value1, t.Value2, t.Value3));

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringConcat_StringIntGuidObjectArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			if ((value1Nullable || value2Nullable) && context.IsAnyOf(TestProvName.AllSybase))
				Assert.Ignore("Sybase ASE pads `Coalesce(NULL, '')` to a single space — diverges from C# string.Concat for any nullable operand.");

			var data = new[]
				{
					new StringConcatIntGuidNullEntity { ID = 1, Value1 = "A",  Value2 = 1,    Value3 = Tests.TestData.Guid1 },
					new StringConcatIntGuidNullEntity { ID = 2, Value1 = null, Value2 = 2,    Value3 = Tests.TestData.Guid2 },
					new StringConcatIntGuidNullEntity { ID = 3, Value1 = "C",  Value2 = null, Value3 = null         },
					new StringConcatIntGuidNullEntity { ID = 4, Value1 = null, Value2 = null, Value3 = null         }
				}
				.Where(t => (value1Nullable || t.Value1 != null) && (value2Nullable || (t.Value2 != null && t.Value3 != null)))
				.ToArray();

			var fb = new FluentMappingBuilder();
			fb.Entity<StringConcatIntGuidNullEntity>()
				.Property(t => t.ID)    .IsPrimaryKey()
				.Property(t => t.Value1).HasDataType(DataType.NVarChar).HasLength(50).IsNullable(value1Nullable)
				.Property(t => t.Value2).IsNullable(value2Nullable)
				.Property(t => t.Value3).IsNullable(value2Nullable);
			var ms = fb.Build().MappingSchema;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable(data);

			// string.Concat(object, object, object) — boxes int? and Guid? into object; uses the (object, object, object) overload.
			var query = table.OrderBy(t => t.ID).Select(t => string.Concat((object?)t.Value1, (object?)t.Value2, (object?)t.Value3));

			AssertQuery(query);
		}

		// Non-translatable client-only helper. Used in the partial-translation tests to force the
		// translator to bail out so the framework can partition the projection into a server-side
		// fragment + client-side post-processing step.
		static string? PartialTranslation_LocalHelper(string? value)
		{
			return value?.ToUpperInvariant();
		}

		// Regression: `Sql.Concat(translatable, nonTranslatable)` in a projection — the array-overload
		// path through `ConfigureConcat` (which sets `IsServerSideOnly(false)`). When the surrounding
		// visitor is in Expression mode, `AggregateFunctionBuilder.Combine` returns `Skipped()` on the
		// non-translatable arg and `Build` propagates null up — the framework then partitions the
		// projection so the per-row Sql.Concat runs client-side via EagerLoading + the BCL Sql.Concat
		// body. Locks in the new IsServerSideOnly + isExpression bail-out for the array path.
		[Test]
		public void Concat_SqlConcatArray_PartialTranslation_LetBoundNonTranslatable([DataSources] string context)
		{
			var data = new[]
				{
					new StringConcatNullEntity { ID = 1, Value1 = "alpha", Value2 = "x"  },
					new StringConcatNullEntity { ID = 2, Value1 = "beta",  Value2 = null },
					new StringConcatNullEntity { ID = 3, Value1 = null,    Value2 = "y"  },
				};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				let local = PartialTranslation_LocalHelper(t.Value1)
				orderby t.ID
				select Sql.Concat((object?)t.Value1, (object?)local, (object?)t.Value2);

			AssertQuery(query);
		}

		// Regression: `string + nonTranslatableLetBound` (LengthFromNonTranslatable shape, co-located).
		// Exercises the binary `+` path: TranslateBinaryStringConcat → string.Concat method-call
		// dispatch → `TranslateConcat` arg loop → on the non-translatable operand, the
		// `TranslationFlags.Expression` bail-out returns null → `HandleBinaryMath` partitions.
		[Test]
		public void Concat_BinaryAdd_PartialTranslation_LetBoundNonTranslatable([DataSources] string context)
		{
			var data = new[]
				{
					new StringConcatNullEntity { ID = 1, Value1 = "alpha", Value2 = "x"  },
					new StringConcatNullEntity { ID = 2, Value1 = "beta",  Value2 = null },
				};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				let local = PartialTranslation_LocalHelper(t.Value1)
				orderby t.ID
				select "[" + local + "]";

			AssertQuery(query);
		}

		// Regression: `string.Join(separator, nonTranslatable, …)` — exercises the array-overload
		// path through ConfigureConcatWs / ConfigureConcatWsEmulation (also opted in to
		// `IsServerSideOnly(false)`). Same flow as Sql.Concat: builder bails out on the
		// non-translatable operand in Expression mode → framework partitions → BCL string.Join
		// runs client-side via EagerLoading.
		[Test]
		public void StringJoin_PartialTranslation_LetBoundNonTranslatable([DataSources] string context)
		{
			var data = new[]
				{
					new StringConcatNullEntity { ID = 1, Value1 = "alpha", Value2 = "x"  },
					new StringConcatNullEntity { ID = 2, Value1 = "beta",  Value2 = null },
					new StringConcatNullEntity { ID = 3, Value1 = null,    Value2 = "y"  },
				};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			var query =
				from t in table
				let local = PartialTranslation_LocalHelper(t.Value1)
				orderby t.ID
				select string.Join(", ", new[] { t.Value1, local, t.Value2 });

			AssertQuery(query);
		}
	}
}
