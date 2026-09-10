using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.FSharp;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class FSharpTests : TestBase
	{
		[Test]
		public void LoadSingle([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSingle(db);
		}

		[Test]
		public void RecordParametersMapping([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordParametersMapping(db);
		}

#if NETFRAMEWORK
		// needs FSharp.Core 10.1, but we use v9 for netfx builds now
		[ActiveIssue("F# unnecessary converts sub-query to enumerable leading to client-side filtering")]
#endif
		// informix still struggle with non-ascii data in 2026
		[Test]
		public void RecordProjectionColumnsOnly([DataSources(TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordProjectionColumnsOnly(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

#if NETFRAMEWORK
		// needs FSharp.Core 10.1, but we use v9 for netfx builds now
		[ActiveIssue("F# unnecessary converts sub-query to enumerable leading to client-side filtering")]
#endif
		[Test]
		public void RecordComplexProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordComplexProjection(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

		[Test]
		public void RecordProjectionAll([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordProjectionAll(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

		[Test]
		public void ComplexRecordParametersMapping([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.ComplexRecordParametersMapping(db);
		}

		[Test]
		public void ComplexRecordParametersMappingUsingRecordReaderBuilder([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.WhereTest.ComplexRecordParametersMappingUsingRecordReaderBuilder(db);
		}

		[Test]
		public void UnionRecord1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.UnionRecord1(db);
		}

		[Test]
		public void UnionRecord2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.UnionRecord2(db);
		}

		[Test]
		public void LoadSinglesWithPatient([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSinglesWithPatient(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/195")]
		public void LoadSingleWithOptions([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[Test(Description = "Explicit MappingSchema option-type registration still works alongside UseFSharp auto-mapping")]
		public void LoadSingleWithExplicitOptionsMapping([DataSources] string context)
		{
			var ms = FSharp.MappingSchema.Initialize();

			using var db = GetDataContext(context, ms);
			FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4646")]
		public void Issue4646_OptionRoundtrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue4646.TestOptionRoundtrip(db);
		}

		[Test(Description = "Auto 'T option mapping must not override an explicit fluent DataType on an option column (#195 follow-up)")]
		public void OptionMapping_ExplicitDataTypePreserved([DataSources] string context)
		{
			var ms = FSharp.OptionMappingPrecedence.BuildExplicitSchema();

			using var db = GetDataContext(context, ms);
			FSharp.OptionMappingPrecedence.VerifyExplicitDataTypePreserved(db);
		}

		[Test(Description = "Nullable<_> element option must not produce Nullable<Nullable<_>> (#195)")]
		public void Option_NullableElementRoundtrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionTypes.TestNullableElementOptionRoundtrip(db);
		}

		[Test(Description = "F# struct value-options ('T voption) are auto-mapped like reference options (#195)")]
		public void Option_ValueOptionRoundtrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionTypes.TestValueOptionRoundtrip(db);
		}

		[Test(Description = "Auto 'decimal option' mapping resolves provider-faithful precision/scale - no scale truncation (#195)")]
		public void Option_DecimalRoundtrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionTypes.TestDecimalOptionRoundtrip(db);
		}

		[Test(Description = "An option over a complex/entity element is not auto-scalarized; only scalar-element options are (#195)")]
		public void Option_ComplexElementNotScalarized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionTypes.VerifyComplexElementOptionNotScalarized(db);
		}

		[ActiveIssueNew(ErrorMessage = "MyId option column was not mapped - auto-option-mapping did not recognise the user-registered scalar type",
			Details = "no-issue: F# option auto-mapping gate (IsScalarOption) consults MappingSchema.Default, so an option over a type that is scalar only in the user/provider schema is not auto-mapped. #195, which the Description cites, is the closed umbrella issue for F# option support and does not cover this gap.")]
		[Test(Description = "An option over a type that is scalar only in the user/provider schema (not MappingSchema.Default) must still auto-map (#195)")]
		public void Option_CustomScalarElementMapped([DataSources] string context)
		{
			var ms = FSharp.OptionTypes.BuildCustomScalarSchema();

			using var db = GetDataContext(context, ms);
			FSharp.OptionTypes.VerifyCustomScalarOptionMapped(db);
		}

		[Test]
		public void LoadSingleCLIMutable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSingleCLIMutable(db);
		}

		[Test]
		public void LoadSingleComplexPerson([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSingleComplexPerson(db);
		}

		[Test]
		public void LoadSingleDeeplyComplexPerson([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadSingleDeeplyComplexPerson(db);
		}

		[Test]
		public void LoadColumnOfDeeplyComplexPerson([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.LoadColumnOfDeeplyComplexPerson(db);
		}

		[Test]
		public void SelectField([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.SelectTest.SelectField(db);
		}

		[Test]
		public void SelectFieldDeeplyComplexPerson([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.SelectTest.SelectFieldDeeplyComplexPerson(db);
		}

		[Test]
		public void Insert1([DataSources(TestProvName.AllYdb, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.InsertTest.Insert1(db);
		}

		[Test]
		public void Insert2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.InsertTest.Insert2(db, context.IsAnyOf(TestProvName.AllClickHouse) ? 100 : 0);
		}

		[Test]
		public void SelectLeftJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.SelectTest.SelectLeftJoin(db);
		}

		[Test]
		public void TestIssue2678_SelectObject([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue2678.InsertAndSelectObject(db);
		}

		[Test]
		public void TestIssue2678_SelectRecord([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue2678.InsertAndSelectRecord(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union1(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union2(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union3(db);
		}

		[Test]
		public void Issue3699_Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.SelectTest.Issue3699Test(db);
		}

		[Test]
		public void Issue3743Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3743.Issue3743Test1(db, 1);
		}

		[Test]
		public void Issue3743Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3743.Issue3743Test2(db, 1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4132")]
		public void Issue4132Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue4132.Issue4132Test1(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4132")]
		public void Issue4132Test2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue4132.Issue4132Test2(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateSetsOnlyChangedColumn([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateSetsOnlyChangedColumn(db, true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateSetsOnlyChangedColumnYdb([IncludeDataSources(false, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateSetsOnlyChangedColumn(db, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public async Task Issue5598_UpdateSetsOnlyChangedColumnAsync([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			await FSharp.Issue5598.UpdateSetsOnlyChangedColumnAsync(db, true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public async Task Issue5598_UpdateSetsOnlyChangedColumnAsyncYdb([IncludeDataSources(false, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataConnection(context);
			await FSharp.Issue5598.UpdateSetsOnlyChangedColumnAsync(db, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateSetsOnlyChangedColumnNoPredicate([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateSetsOnlyChangedColumnNoPredicate(db, true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateSetsOnlyChangedColumnNoPredicateYdb([IncludeDataSources(false, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateSetsOnlyChangedColumnNoPredicate(db, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateNoOpExcludesPrimaryKey([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateNoOpExcludesPrimaryKey(db, true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5598")]
		public void Issue5598_UpdateNoOpExcludesPrimaryKeyYdb([IncludeDataSources(false, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.Issue5598.UpdateNoOpExcludesPrimaryKey(db, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test1(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test2(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test3(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test4(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test5(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test6(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test7(db);
		}

		// YDB rejects the constant carried into the join ON clause ("each equality predicate argument must depend on exactly one JOIN input").
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", TestProvName.AllYdb)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test8([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test8(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test9([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test9(db);
		}

		// The nullable-string join key makes linq2db emit the null-aware equality
		// (ON a.Text = b.Name OR a.Text IS NULL AND b.Name IS NULL), which YDB rejects ("JOIN ON expression must be
		// a conjunction of equality predicates"). IsComplexJoinConditionSupported=false cannot relocate it either:
		// the predicate references both join inputs, so it has to stay in ON.
		[ThrowsForProvider("Ydb.Sdk.Ado.YdbException", TestProvName.AllYdb)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test10([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test10(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test11(db);
		}

		// Declared on the expected row set: NUnit renders this assertion's own text as an empty "Assert.That(, )",
		// so the first line of the message carries nothing to match on.
		[ActiveIssueNew(5794, ErrorMessage = "Expected: \"1-1-0-2,1-4-0-2,2-0-2-3,2-0-3-3\"",
			Details = "the trailing join after chained groupJoins silently drops the unmatched rows, leaving two of the four.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5794")]
		public void Issue5794Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue5794Test(db);
		}

		[ActiveIssueNew(5790, ErrorTypeName = "LinqToDB.FSharp.FlattenInvariantException", ErrorMessage = "F# chained group join could not be flattened",
			Details = "the chained groupJoin's correlated inner sequence defeats the flattener - #5790's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5790")]
		public void Issue5790Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue5790Test(db);
		}

		// The refusal is a client-side translation decision, so one provider covers it.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5790")]
		public void Issue5790RefusalTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue5790RefusalTest(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5428")]
		public void ExpressionFunctionInCteTranslationTest1([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			FSharp.Issue5428.TestSimple(GetConnectionString(context));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5428")]
		public void ExpressionFunctionInCteTranslationTest2([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			FSharp.Issue5428.TestWindow(GetConnectionString(context));
		}

		[Test(Description = "F# option member access over a parameter (not a column) is refused rather than translated against the parameter's own nullness")]
		public void OptionQuery_ParameterOperandIsRefused([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			var act = () => { FSharp.OptionQueryTests.ParameterOperandIsRefused(db); };
			act.ShouldThrow<LinqToDBException>();
		}

		[Test(Description = "F# option .IsSome in a query predicate translates to IS NOT NULL")]
		public void OptionQuery_IsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.IsSome(db).ShouldBe(2);
		}

		[Test(Description = "F# option .IsNone in a query predicate translates to IS NULL")]
		public void OptionQuery_IsNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.IsNone(db).ShouldBe(1);
		}

		[Test(Description = "F# option .Value in a query predicate translates to the underlying value")]
		public void OptionQuery_Value([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.Value(db).ShouldBe(1);
		}

		[Test(Description = "F# option .Value in a projection translates to the underlying column")]
		public void OptionQuery_ValueProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.ValueProjection(db).ShouldBe(new[] { "a", "b" });
		}

		[Test(Description = "F# Option.isSome module function translates to IS NOT NULL")]
		public void OptionQuery_ModuleIsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.ModuleIsSome(db).ShouldBe(2);
		}

		[Test(Description = "F# Option.isNone module function translates to IS NULL")]
		public void OptionQuery_ModuleIsNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.ModuleIsNone(db).ShouldBe(1);
		}

		[Test(Description = "F# Option.get module function translates to the underlying value")]
		public void OptionQuery_ModuleGet([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.ModuleGet(db).ShouldBe(1);
		}

		[Test(Description = "F# ValueOption.isSome module function translates to IS NOT NULL")]
		public void OptionQuery_VOptionModuleIsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionModuleIsSome(db).ShouldBe(1);
		}

		[Test(Description = "F# ValueOption.isNone module function translates to IS NULL")]
		public void OptionQuery_VOptionModuleIsNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionModuleIsNone(db).ShouldBe(1);
		}

		[Test(Description = "F# ValueOption.get module function translates to the underlying value")]
		public void OptionQuery_VOptionModuleGet([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionModuleGet(db).ShouldBe(1);
		}

		[Test(Description = "F# option .IsSome in a projection materializes as a boolean value")]
		public void OptionQuery_IsSomeProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var (values, sql) = FSharp.OptionQueryTests.IsSomeProjection(db);

			values.ShouldBe(new[] { true, false, true });

			// The values alone cannot fail: a declined translation selects the bare column and evaluates
			// .IsSome client-side, returning the same array. Only the null test in the SELECT list shows
			// the projection was translated.
			if (sql is not null)
				sql.ShouldContain("IS NOT NULL");
		}

		[Test(Description = "F# option .Value over a row whose column is NULL materializes the element's default, as Nullable<T>.Value does - it does not raise the way Option.get does")]
		public void OptionQuery_ValueOverNoneRow([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.ValueOverNoneRow(db).ShouldBe(new[] { 5, 0, 7 });
		}

		[Test(Description = "F# int option .Value translates through the Nullable<int> provider type")]
		public void OptionQuery_IntValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.IntValue(db).ShouldBe(new[] { 5, 7 });
		}

		[Test(Description = "F# voption .IsSome in a query predicate translates to IS NOT NULL")]
		public void OptionQuery_VOptionIsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionIsSome(db).ShouldBe(1);
		}

		[Test(Description = "F# voption .IsNone in a query predicate translates to IS NULL")]
		public void OptionQuery_VOptionIsNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionIsNone(db).ShouldBe(1);
		}

		[Test(Description = "F# voption .IsValueSome (generated case-tester spelling) translates to IS NOT NULL")]
		public void OptionQuery_VOptionIsValueSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionIsValueSome(db).ShouldBe(1);
		}

		[Test(Description = "F# voption .IsValueNone (generated case-tester spelling) translates to IS NULL")]
		public void OptionQuery_VOptionIsValueNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionIsValueNone(db).ShouldBe(1);
		}

		[Test(Description = "F# voption .Value in a query predicate translates to the underlying value")]
		public void OptionQuery_VOptionValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.OptionQueryTests.VOptionValue(db).ShouldBe(1);
		}

		[Test(Description = "F# struct single-case DU cannot hold null, so a NULL read materializes the union wrapping the default (declared behaviour - use 'option' for a nullable column)")]
		public void DuQuery_StructNullRead([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.StructNullRead(db).ShouldBe(new[] { 10, 0 });
		}

		[Test(Description = "F# struct single-case DU wrapped in option round-trips, including None")]
		public void DuQuery_StructOptionRoundTrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.StructOptionRoundTrip(db).ShouldBe(new[] { 10, -1 });
		}

		[Test(Description = "F# auto-mapping claims single-case scalar unions (and options over them) and leaves multi-case DUs, lists and non-scalar wrappers alone")]
		public void DuQuery_MappingBoundary([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.MappingBoundary(db).ShouldBe("Id,Key:conv,OptKey:conv");
		}

		[Test(Description = "F# 'UserId option' column maps to the union's wrapped scalar and round-trips, including None")]
		public void DuQuery_OptionRoundTrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionRoundTrip(db).ShouldBe(new[] { 10, -1 });
		}

		[Test(Description = "F# option .IsSome over a single-case-union column translates to IS NOT NULL")]
		public void DuQuery_OptionIsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionIsSome(db).ShouldBe(1);
		}

		[Test(Description = "F# option .IsNone over a single-case-union column translates to IS NULL")]
		public void DuQuery_OptionIsNone([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionIsNone(db).ShouldBe(1);
		}

		// The option converter's source type is FSharpOption<UserId> while the constant beside the column is
		// a bare UserId, and ColumnDescriptor.ApplyConversions only bridges that gap for Nullable<>.
		[ActiveIssue(5886)]
		[Test(Description = "F# option .Value over a single-case-union column compares on the union's wrapped scalar")]
		public void DuQuery_OptionValueEquals([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionValueEquals(db).ShouldBe(1);
		}

		[Test(Description = "F# equality against 'Some (UserId 10)' over a single-case-union column")]
		public void DuQuery_OptionEqualsSome([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionEqualsSome(db).ShouldBe(1);
		}

		[Test(Description = "F# option .Value over a single-case-union column projects the reconstructed union")]
		public void DuQuery_OptionValueProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.OptionValueProjection(db).ShouldBe(new[] { 10 });
		}

		[Test(Description = "F# single-case DU column round-trips and equality translates to SQL")]
		public void DuQuery_EqualsLiteral([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.EqualsLiteral(db).ShouldBe(1);
		}

		[Test(Description = "F# single-case union with a private representation (the smart-constructor idiom) round-trips, so the converter's lambdas can reach the non-public case constructor and field")]
		public void DuQuery_PrivateRepresentationRoundTrip([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.PrivateRepresentationRoundTrip(db).ShouldBe(new[] { 7 });
		}

		[Test(Description = "F# single-case DU column reads back as the reconstructed union (from-provider converter)")]
		public void DuQuery_ReadBack([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.ReadBack(db).ShouldBe(new[] { 10, 20 });
		}

		[Test(Description = "F# single-case DU column read as NULL (LEFT JOIN unmatched row) must materialize as null, not a fabricated default")]
		public void DuQuery_NullReadKey([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.DuQueryTests.NullReadKey(db).ShouldBe(1);
		}

		[Test(Description = "UseFSharp must yield a stable ConfigurationID - the harness applies it to every context, so an unstable id defeats the query cache for all providers (#5704)")]
		public void UseFSharp_StableConfigurationID()
		{
			// Member translators are keyed by instance identity in DataContextOptions' ConfigurationID
			// (the query-cache key). UseFSharp must reuse one translator instance; a fresh instance per
			// call gives every context a distinct id, so the query cache never hits.
			var a = new DataOptions().UseFSharp();
			var b = new DataOptions().UseFSharp();

			a.ShouldBe(b);
		}
	}
}
