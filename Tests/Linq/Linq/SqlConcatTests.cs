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
	public class SqlConcatTests : TestBase
	{
		// On providers that treat empty string as NULL (Oracle, indicated by
		// SqlProviderFlags.DoesProviderTreatsEmptyStringAsNull), `||` skips NULL operands
		// when at least one is non-null but returns NULL when ALL operands are NULL
		// (the empty-string-is-NULL identity). Mirrors that semantic for the in-memory
		// expected — `Sql.Concat`'s strict any-null-→-null doesn't apply on those providers.
		static string? EmptyAsNullConcat(params string?[] args)
		{
			var nonNull = args.Where(a => a != null).ToArray();
			return nonNull.Length == 0 ? null : string.Concat(nonNull);
		}

		[Table("SqlConcatTestEntity")]
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
				where  Sql.Concat(e.StrReq, " I") == "Programmer I"
				select e.StrReq;

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringStringInt_MixedTypes([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Sql.Concat(object?[]) overload — passing a non-string literal as object boxes it.
			var query =
				from   e in table
				where  Sql.Concat((object?)e.StrReq, " ", 1) == "Programmer 1"
				select e.StrReq;

			AssertQuery(query);
		}

		[Test]
		public void Concat_NullableArgs_PropagatesNull([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Sql.Concat any-null → null. Rows where Str2 is null produce null, so `!= null`
			// filters them out — only Id=1 and Id=3 (Str2 non-null) remain.
			var query =
				from   e in table
				where  Sql.Concat(e.Str1, e.Str2) != null
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_BothArgsNonNull_ReturnsNonNull([DataSources] string context)
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
		public void Concat_NullableArgs_FilterByNull([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Inverse of Concat_NullableArgs_PropagatesNull: select rows where the concat IS
			// null. Optimizer should fold `Sql.Concat(a, b) == null` to `a IS NULL OR b IS
			// NULL` (because SqlConcatExpression(preserveNull: true) reports CanBeNullable
			// when any operand is nullable). Works on every provider — the IS NULL check
			// bypasses any provider-specific `||` quirks (e.g. Oracle's null-as-empty).
			var query =
				from   e in table
				where  Sql.Concat(e.Str1, e.Str2) == null
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_NullableArgs_CoalesceOnResult([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Coalesce-on-result projection: verifies the optimizer treats the result of
			// Sql.Concat as nullable (so `?? "X"` is meaningful) and emits SQL that yields
			// the fallback when any operand is null.
			//
			// Oracle exception: Oracle's `||` treats NULL as empty when at least one operand
			// is non-null — `Sql.Concat(t.Str1, null)` evaluates to t.Str1 (non-null), so
			// `?? "X"` doesn't fire. EmptyAsNullConcat mirrors that semantic.
			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				var actualOracle = (
					from    e in table
					orderby e.Id
					select  Sql.Concat(e.Str1, e.Str2) ?? "X").ToArray();
				var expectedOracle = TestData
					.OrderBy(t => t.Id)
					.Select(t => EmptyAsNullConcat(t.Str1, t.Str2) ?? "X")
					.ToArray();
				actualOracle.ShouldBe(expectedOracle);
				return;
			}

			var query =
				from    e in table
				orderby e.Id
				select  Sql.Concat(e.Str1, e.Str2) ?? "X";

			AssertQuery(query);
		}

		[Test]
		public void Concat_NonNullLiteralWithNullableCol_FoldsIsNullToOperand([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Sql.Concat("X", e.Str2) IS NULL — the literal "X" is provably non-null, so the
			// optimizer should be able to fold the predicate to `e.Str2 IS NULL`. Verifies
			// that CanBeNullable distinguishes per-operand nullability (literal is non-null
			// → result null iff the only nullable operand is null).
			var query =
				from   e in table
				where  Sql.Concat("X", e.Str2) == null
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_FourArgs_Chain([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Str1 nullable — equality only matches the non-null Id=1 row ("John").
			var query =
				from   e in table
				where  Sql.Concat(e.Str1, " ", e.StrReq, "!") == "John Programmer!"
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
				where  Sql.Concat((object?)e.Num, "-", e.StrReq) == "100-Programmer"
				select e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_InSelectProjection_ReturnsValue([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Rows with null Str1 produce null projection.
			var query =
				from    e in table
				orderby e.Id
				select  Sql.Concat(e.Str1, "/", e.StrReq);

			AssertQuery(query);
		}

		[Test]
		public void Concat_InOrderBy_GeneratesValidSql([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			var query =
				from    e in table
				orderby Sql.Concat(e.StrReq, "X")
				select  e.Id;

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringArray_FromArrayLiteral([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Inline array of non-null elements — equivalent to fixed-arity params call.
			var query =
				from   e in table
				where  Sql.Concat(new[] { e.StrReq, " ", "I" }) == "Programmer I"
				select e.Id;

			AssertQuery(query);
		}

		[Table("SqlConcatGroupedEntity")]
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
		public void Concat_OverGrouping_AggregateNotSupported([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(GroupedData);

			// Sql.Concat(IEnumerable<>) is not translatable: the translator returns an
			// SqlErrorExpression directing the caller to Sql.ConcatStringsNullable. Surfaces
			// as LinqToDBException at execution.
			var query = from t in table
				group t by t.GrpId into g
				orderby g.Key
				select new
				{
					Id    = g.Key,
					Value = Sql.Concat(g.OrderBy(x => x.PK).Select(x => x.Value)),
				};

			Action act = () => query.ToArray();
			act.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_SqlConcatAggregate);
		}

		[Table("SqlConcatParent")]
		sealed class ConcatParent
		{
			[PrimaryKey]          public int     Id   { get; set; }
			[Column]              public string  Name { get; set; } = string.Empty;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ConcatChild.ParentId), CanBeNull = true)]
			public List<ConcatChild> Children { get; set; } = null!;
		}

		[Table("SqlConcatChild")]
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
		public void Concat_AssociationSubquery_AggregateNotSupported([DataSources] string context)
		{
			using var db          = GetDataContext(context);
			using var parentTable = db.CreateLocalTable(ParentData);
			using var childTable  = db.CreateLocalTable(ChildData);

			// Sql.Concat over an association IEnumerable — same not-translatable rejection
			// as Concat_OverGrouping_AggregateNotSupported.
			var query =
				from p in parentTable.LoadWith(x => x.Children)
				orderby p.Id
				select new
				{
					p.Id,
					Children = Sql.Concat(p.Children.OrderBy(c => c.Id).Select(c => c.Value)),
				};

			Action act = () => query.ToArray();
			act.ShouldThrow<LinqToDBException>().Message.ShouldContain(ErrorHelper.Error_SqlConcatAggregate);
		}

		[Test]
		public void Concat_AggregateArrayPerRow([DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2016Minus, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(TestData);

			// Per-row inline array — Sql.Concat(string?[]) overload. Result is null for any
			// row that has a null in the array.
			//
			// Oracle exception: Oracle's `||` operator treats NULL as empty string, so its
			// SQL emission for `Sql.Concat` does NOT produce strict any-null → null. We
			// keep Oracle's native behavior unchanged; the in-memory comparison uses
			// string.Concat (null-as-empty) so AssertQuery's two sides line up on Oracle.
			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				var actualOracle = (
					from t in table
					orderby t.Id
					select new
					{
						t.Id,
						Aggregated = Sql.AsSql(Sql.Concat(new[] { t.Str1, t.Str2, t.StrReq })),
					}).ToArray();

				var expectedOracle = TestData
					.OrderBy(t => t.Id)
					.Select(t => new
					{
						t.Id,
						Aggregated = EmptyAsNullConcat(t.Str1, t.Str2, t.StrReq),
					})
					.ToArray();

				actualOracle.ShouldBe(expectedOracle);
				return;
			}

			var query =
				from t in table
				orderby t.Id
				select new
				{
					t.Id,
					Aggregated = Sql.AsSql(Sql.Concat(new[] { t.Str1, t.Str2, t.StrReq })),
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

		sealed class StringConcatThreeNullEntity
		{
			public int     ID     { get; set; }
			public string? Value1 { get; set; }
			public string? Value2 { get; set; }
			public string? Value3 { get; set; }
		}

		[Test]
		public void Concat_TwoArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
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

			// Sql.Concat any-null → null: rows where either value is null get null result on both
			// in-memory and SQL sides — AssertQuery passes without per-provider Sybase guards.
			//
			// Oracle exception: Oracle's `||` operator treats NULL as empty string. We keep that
			// native behavior; the in-memory comparison uses string.Concat (null-as-empty).
			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				var actualOracle   = table.OrderBy(t => t.ID).Select(t => Sql.Concat(t.Value1, t.Value2)).ToArray();
				var expectedOracle = data.OrderBy(t => t.ID).Select(t => EmptyAsNullConcat(t.Value1, t.Value2)).ToArray();
				actualOracle.ShouldBe(expectedOracle);
				return;
			}

			var query = table.OrderBy(t => t.ID).Select(t => Sql.Concat(t.Value1, t.Value2));

			AssertQuery(query);
		}

		[Test]
		public void Concat_ThreeArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable, [Values] bool value3Nullable)
		{
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

			// Oracle exception (see Concat_TwoArgs_NullableArgs).
			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				var actualOracle   = table.OrderBy(t => t.ID).Select(t => Sql.Concat(t.Value1, t.Value2, t.Value3)).ToArray();
				var expectedOracle = data.OrderBy(t => t.ID).Select(t => EmptyAsNullConcat(t.Value1, t.Value2, t.Value3)).ToArray();
				actualOracle.ShouldBe(expectedOracle);
				return;
			}

			var query = table.OrderBy(t => t.ID).Select(t => Sql.Concat(t.Value1, t.Value2, t.Value3));

			AssertQuery(query);
		}

		[Test]
		public void Concat_StringIntGuidObjectArgs_NullableArgs([DataSources] string context, [Values] bool value1Nullable, [Values] bool value2Nullable)
		{
			// SQLite and Oracle render Guid via raw CAST AS VarChar(36) which produces the
			// binary/hex representation (not the dashed string). Separate pre-existing issue
			// with the array-overload Guid path: the ConvertOperandToString rewrite that
			// fixed-arity string.Concat applies isn't applied for Sql.Concat(object?[]).
			// Skip on those providers until that's addressed independently.
			if (context.IsAnyOf(TestProvName.AllSQLite) || context.IsAnyOf(TestProvName.AllOracle))
				Assert.Ignore("Provider emits Guid as binary/hex when boxed via Sql.Concat(object?[]).");

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

			// Oracle exception (see Concat_TwoArgs_NullableArgs).
			if (context.IsAnyOf(TestProvName.AllOracle))
			{
				var actualOracle = table.OrderBy(t => t.ID)
					.Select(t => Sql.Concat((object?)t.Value1, (object?)t.Value2, (object?)t.Value3))
					.ToArray();
				var expectedOracle = data.OrderBy(t => t.ID)
					.Select(t => EmptyAsNullConcat(t.Value1, t.Value2?.ToString(), t.Value3?.ToString()))
					.ToArray();
				actualOracle.ShouldBe(expectedOracle);
				return;
			}

			// Sql.Concat(object?, object?, object?) — boxes int? and Guid? into object; any-null → null.
			var query = table.OrderBy(t => t.ID).Select(t => Sql.Concat((object?)t.Value1, (object?)t.Value2, (object?)t.Value3));

			AssertQuery(query);
		}
	}
}
