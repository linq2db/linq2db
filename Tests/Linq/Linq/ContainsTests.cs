using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ContainsTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
#pragma warning disable CA2263 // Prefer generic overload when type is known
#pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring'
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<Src>()
					.Property(e => e.CEnum)
						.HasDataType(DataType.VarChar)
						.HasLength(20)
						.HasConversion(v => $"___{v}___", v => (ConvertedEnum)Enum.Parse(typeof(ConvertedEnum), v.Substring(3, v.Length - 6)))
				.Build();
#pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
#pragma warning restore CA2263 // Prefer generic overload when type is known

			var data = new[]
			{
				new Src { Id = 1 },
				new Src { Id = 2, Int = 2, Enum = ContainsEnum.Value2, CEnum = ConvertedEnum.Value2 },
			};

			var src  = db.CreateLocalTable(data);
			return src;
		}

		[Test]
		public void Functional(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.Int.In(-1, -2));
			result.ShouldBe(0);

			result = FetchId(s => s.Int.In(-1, null));
			result.ShouldBe(withNullCompares ? 1 : 0);

			result = FetchId(s => s.Int.In(-1, 2));
			result.ShouldBe(2);

			result = FetchId(s => s.Int.NotIn(null, 2));
			result.ShouldBe(0);

			result = FetchId(s => s.Int.NotIn(-1, 2));
			result.ShouldBe(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void FunctionalEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, ContainsEnum.Value4));
			result.ShouldBe(0);

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, null));
			result.ShouldBe(withNullCompares ? 1 : 0);

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, ContainsEnum.Value2));
			result.ShouldBe(2);

			result = FetchId(s => s.Enum.NotIn(null, ContainsEnum.Value2));
			result.ShouldBe(0);

			result = FetchId(s => s.Enum.NotIn(ContainsEnum.Value3, ContainsEnum.Value2));
			result.ShouldBe(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void FunctionalCEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, ConvertedEnum.Value4));
			result.ShouldBe(0);

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, null));
			result.ShouldBe(withNullCompares ? 1 : 0);

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, ConvertedEnum.Value2));
			result.ShouldBe(2);

			result = FetchId(s => s.CEnum.NotIn(null, ConvertedEnum.Value2));
			result.ShouldBe(0);

			result = FetchId(s => s.CEnum.NotIn(ConvertedEnum.Value3, ConvertedEnum.Value2));
			result.ShouldBe(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void Empty(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Int.In(Array.Empty<int?>()));
			count.ShouldBe(0);

			count = src.Count(s => s.Int.NotIn(Array.Empty<int?>()));
			count.ShouldBe(2);

			count = src.Count(s => !s.Int.In(Array.Empty<int?>()));
			count.ShouldBe(2);
		}

		[Test]
		public void EmptyEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Enum.In(Array.Empty<ContainsEnum?>()));
			count.ShouldBe(0);

			count = src.Count(s => s.Enum.NotIn(Array.Empty<ContainsEnum?>()));
			count.ShouldBe(2);

			count = src.Count(s => !s.Enum.In(Array.Empty<ContainsEnum?>()));
			count.ShouldBe(2);
		}

		[Test]
		public void EmptyCEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.CEnum.In(Array.Empty<ConvertedEnum?>()));
			count.ShouldBe(0);

			count = src.Count(s => s.CEnum.NotIn(Array.Empty<ConvertedEnum?>()));
			count.ShouldBe(2);

			count = src.Count(s => !s.CEnum.In(Array.Empty<ConvertedEnum?>()));
			count.ShouldBe(2);
		}

		// Excluded Access from tests because it seems to have non compliant behavior.
		// It is the only DB that returns 1 for `WHERE Int NOT IN (null, null)`
		// Nope, Access is not alone anymore
		[Test]
		public void AllNullsLikeClr([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNulls(context, withNullCompares: true);
		}

		// Split from the LikeClr case: only LikeSql fails on ClickHouse, and a gate targets a provider rather
		// than one value of a [Values] axis. Placeholders because Shouldly breaks the expectation over lines,
		// and the wording stops at "but was" because Shouldly writes "but was not" instead of the actual value
		// whenever it cannot resolve the asserted expression's source text - which it does non-deterministically.
		// The two sibling LikeSql gates below carry the same declaration for the same reason.
		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}0{1}but was")]
		[Test]
		public void AllNullsLikeSql([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNulls(context, withNullCompares: false);
		}

		void AllNulls(string context, bool withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Int.In(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Int.NotIn(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);
		}

		// Excluded Access from tests because it seems to have non compliant behavior.
		// It is the only DB that returns 1 for `WHERE Enum NOT IN (null, null)`
		// Nope, Access is not alone anymore
		[Test]
		public void AllNullsEnumLikeClr([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNullsEnum(context, withNullCompares: true);
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}0{1}but was")]
		[Test]
		public void AllNullsEnumLikeSql([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNullsEnum(context, withNullCompares: false);
		}

		void AllNullsEnum(string context, bool withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Enum.In(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Enum.NotIn(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);
		}

		// Excluded Access from tests because it seems to have non compliant behavior.
		// It is the only DB that returns 1 for `WHERE CEnum NOT IN (null, null)`
		// Nope, Access is not alone anymore
		[Test]
		public void AllNullsCEnumLikeClr([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNullsCEnum(context, withNullCompares: true);
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}0{1}but was")]
		[Test]
		public void AllNullsCEnumLikeSql([DataSources(TestProvName.AllAccess)] string context)
		{
			AllNullsCEnum(context, withNullCompares: false);
		}

		void AllNullsCEnum(string context, bool withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.CEnum.In(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);

			count = src.Count(s => s.CEnum.NotIn(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);
		}

		sealed class Src
		{
			[PrimaryKey]
			public int            Id    { get; set; }
			public int?           Int   { get; set; }
			public ContainsEnum?  Enum  { get; set; }
			public ConvertedEnum? CEnum { get; set; }
		}

		enum ContainsEnum
		{
			[MapValue("ONE")  ] Value1,
			[MapValue("TWO")  ] Value2,
			[MapValue("THREE")] Value3,
			[MapValue("FOUR") ] Value4,
		}

		enum ConvertedEnum
		{
			Value1,
			Value2,
			Value3,
			Value4,
		}

		private static readonly string?[][] _issue3986Cases1 = new string?[][]
		{
			new string?[] { null, "Ko" },
			new string?[] { "Ko", null },
			new string?[] { null, "Ko", null },
			new string?[] { "Ko", null, null },
			new string?[] { null, null, "Ko" },
			new string?[] { "123", null, "Ko" },
			new string?[] { "123", "Ko", null },
			new string?[] { null, "123", "Ko" },
			new string?[] { null, null },
			Array.Empty<string?>()
		};

		[Test]
		public void Issue3986Test1([DataSources] string context, [ValueSource(nameof(_issue3986Cases1))] string?[] values)
		{
			using var db = GetDataContext(context);

			var result = db.Person.Where(r => r.ID == 3 && values.Contains(r.MiddleName)).ToArray();

			if (values.Length == 0)
				Assert.That(result, Is.Empty);
			else
			{
				Assert.That(result, Has.Length.EqualTo(1));
				Assert.That(result[0].ID, Is.EqualTo(3));
			}
		}

		private static readonly string?[][] _issue3986Cases2 = new string?[][]
		{
			new string?[] { null, "222" },
			new string?[] { "222", null },
			new string?[] { null, "222", null },
			new string?[] { "222", null, null },
			new string?[] { null, null, "222" },
			new string?[] { "123", null, "222" },
			new string?[] { "123", "222", null },
			new string?[] { null, "123", "222" },
			new string?[] { null, null },
			Array.Empty<string?>()
		};

		[Test]
		public void Issue3986Test2([DataSources] string context, [ValueSource(nameof(_issue3986Cases2))] string?[] values)
		{
			using var db = GetDataContext(context);

			var result = db.Person.Where(r => r.ID == 4 && !values.Contains(r.MiddleName)).ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].ID, Is.EqualTo(4));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2608")]
		public void Issue2608Test([DataSources(TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));

			var faze = new List<int>() { 11, 18, 19, 20, 21, 22, 23, 24, 26, 29, 28 };

			var today = TestData.Date;
			var code = 1;
			var site = 2;
			var table = db.Types2;

			var query = (from ugovori in table.Where(x => x.BoolValue == false && ((x.IntValue == code && x.IntValue == site) || code == 0))
						 join o in table.Where(x => x.BoolValue == false) on new { ugovori.IntValue } equals new { o.IntValue } into oo
						 from o in oo
						 join u in table.Where(x => x.BoolValue == false) on new { o.IntValue } equals new { u.IntValue }
						 join r in table on new { c = u.IntValue!.Value, s = u.IntValue.Value, BoolValue = (bool?)false } equals new { c = r.IntValue!.Value, s = r.IntValue.Value, r.BoolValue }
						 join f in table on new { r.IntValue } equals new { f.IntValue }

						 select new
						 {
							 StatusPhase = short.Parse(f.StringValue!)
						 });

			query = query.Where(x => !faze.Contains(x.StatusPhase));

			query.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4317")]
		public void Issue4317Test1([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);

			var (ids, expected) = testCase switch
			{
				1 => (null, 4),
				2 => (Array.Empty<int?>(), 4),
				3 => (new int?[] { 1, 2 }, 2),
				_ => throw new InvalidOperationException()
			};

			var res = db.Person.Where(p => ids == null || !ids.Any() || ids.Contains(p.ID)).Count();

			Assert.That(res, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4317")]
		public void Issue4317Test2([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);

			var (ids, expected) = testCase switch
			{
				1 => (null, 4),
				2 => (Array.Empty<int?>(), 4),
				3 => (new int?[] { 1, 2 }, 0),
				_ => throw new InvalidOperationException()
			};

			var res = db.Person.Where(p => ids == null || !ids.Any()).Count();

			Assert.That(res, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4317")]
		public void Issue4317Test3([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);

			var (ids, expected) = testCase switch
			{
				1 => (null, 4),
				2 => (Array.Empty<int?>(), 0),
				3 => (new int?[] { 1, 2 }, 2),
				_ => throw new InvalidOperationException()
			};

			var res = db.Person.Where(p => ids == null || ids.Contains(p.ID)).Count();

			Assert.That(res, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4317")]
		public void Issue4317Test4([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);

			var (ids, expected) = testCase switch
			{
				1 => (null, 4),
				2 => (Array.Empty<int?>(), 4),
				3 => (new int?[] { 1, 2 }, 2),
				_ => throw new InvalidOperationException()
			};

			var res = db.Person.Where(p => ids == null || ids.Contains(p.ID) || !ids.Any()).Count();

			Assert.That(res, Is.EqualTo(expected));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6067")]
		public void ArrayContainsValueType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var ids = new[] { 1, 2, 3 };

			var items = db.Person
				.Where(p => ids.Contains(p.ID))
				.Select(p => p.ID)
				.ToList();

			items
				.OrderBy(i => i)
				.ShouldBe([1, 2, 3]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6067")]
		public void ArrayContainsReferenceType([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var names = new[] { "John", "Jane" };

			var items = db.Person
				.Where(p => names.Contains(p.FirstName))
				.Select(p => p.FirstName)
				.ToList();

			items
				.OrderBy(i => i)
				.ShouldBe(["Jane", "John"]);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void ContainsSubqueryTest([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from a in db.Parent
				from t in
					from t in db.Child
					where db.GrandChild
						.Where(c => c.ChildID == t.ChildID)
						.Select(c => c.ParentID)
						.Contains(a.ParentID)
					select t
				select a.ParentID;

			AssertQuery(query);
		}

		sealed class ScaledDecimalRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 2)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 2)]
			public decimal Value { get; set; }
		}

		/// <summary>
		/// A captured value finer than an aggregated column's scale is compared as written, not narrowed to the column's
		/// scale: MAX is a computed value with no stored scale to match.
		/// </summary>
		/// <remarks>
		/// SqlCe is left out because it compares a decimal literal at the other side's scale (#6062).
		/// </remarks>
		[Test]
		public void CapturedListBesideAggregateKeepsValueScale([DataSources(TestProvName.AllSqlCe)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new ScaledDecimalRow { Id = 1, Value = 1.00m } });

			var finer = new[] { 1.004m };

			var count = t
				.GroupBy(r => r.Id)
				.Count(g => finer.Contains(g.Max(r => r.Value)));

			count.ShouldBe(0);
		}

		sealed class ComputedDecimalRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 12)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 12)]
			public decimal Fine { get; set; }

			[Column(DataType = DataType.Decimal, Precision = 18, Scale = 2)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Decimal64, Precision = 18, Scale = 2)]
			public decimal Wide { get; set; }
		}

		/// <summary>
		/// A list beside a computed decimal - captured or written inline - keeps the digits the computed value's own type
		/// holds, and agrees with <c>==</c>.
		/// </summary>
		/// <remarks>
		/// The rounded arm asserts agreement rather than a count, because the rounded value's type is the provider's.
		/// SqlCe is left out because it compares a decimal literal at the other side's scale (#6062).
		/// </remarks>
		[Test]
		public void ListBesideComputedDecimalKeepsDigits([DataSources(TestProvName.AllSqlCe)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new ComputedDecimalRow { Id = 1, Fine = 1.000000000005m, Wide = 12345678901234.50m } });

			var fine = new[] { 1.000000000005m };
			var wide = new[] { 12345678901234.50m };

			var fineCaptured = t.Count(r => fine.Contains(Math.Abs(r.Fine)));

			fineCaptured.ShouldBe(1);

			var fineInline = t
				.Count(r => Math.Abs(r.Fine).In(1.000000000005m));

			fineInline.ShouldBe(1);

			var wideCaptured = t.Count(r => wide.Contains(Math.Abs(r.Wide)));

			wideCaptured.ShouldBe(1);

			var wideInline = t
				.Count(r => Math.Abs(r.Wide).In(12345678901234.50m));

			wideInline.ShouldBe(1);

			var finer = new[] { 1.004m };

			var roundedEqual = t.Count(r => Math.Round(r.Fine, 2) == 1.004m);

			var roundedCaptured = t.Count(r => finer.Contains(Math.Round(r.Fine, 2)));

			var roundedInline = t
				.Count(r => Math.Round(r.Fine, 2).In(1.004m));

			roundedCaptured.ShouldBe(roundedEqual);
			roundedInline.ShouldBe(roundedEqual);
		}

#if SUPPORTS_DATEONLY
		sealed class DateOnlyRow
		{
			[PrimaryKey] public int      Id  { get; set; }
			[Column]     public DateOnly Day { get; set; }
		}

		/// <summary>
		/// A captured <see cref="DateOnly"/> list beside an aggregate over a <see cref="DateOnly"/> column matches the day.
		/// </summary>
		[Test]
		public void CapturedDateOnlyListBesideAggregate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new DateOnlyRow { Id = 1, Day = new DateOnly(2026, 6, 1) } });

			var days = new[] { new DateOnly(2026, 6, 1) };

			var count = t
				.GroupBy(r => r.Id)
				.Count(g => days.Contains(g.Max(r => r.Day)));

			count.ShouldBe(1);
		}
#endif

		sealed class DayRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.Date)]
			[Column(Configuration = ProviderName.ClickHouse, DataType = DataType.Date32)]
			public DateTime Day { get; set; }
		}

		/// <summary>
		/// A captured list beside an aggregate over a date column matches the day at midnight and not a time within it.
		/// </summary>
		[Test]
		public void CapturedListBesideAggregateOfADateColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new DayRow { Id = 1, Day = new DateTime(2026, 6, 1) } });

			var midnight = new[] { new DateTime(2026, 6, 1) };
			var withTime = new[] { new DateTime(2026, 6, 1, 10, 0, 0) };

			var atMidnight = t
				.GroupBy(r => r.Id)
				.Count(g => midnight.Contains(g.Max(r => r.Day)));

			atMidnight.ShouldBe(1);

			var atTime = t
				.GroupBy(r => r.Id)
				.Count(g => withTime.Contains(g.Max(r => r.Day)));

			atTime.ShouldBe(0);
		}

		sealed class OffsetRow
		{
			[PrimaryKey] public int Id { get; set; }

			[Column(DataType = DataType.DateTime)]
			public DateTimeOffset Value { get; set; }
		}

		/// <summary>
		/// A captured <see cref="DateTimeOffset"/> list beside an aggregate over a whole-second column matches a whole-second
		/// value and not a sub-second one.
		/// </summary>
		/// <remarks>
		/// ClickHouse only: it is where the operand is widened to <c>DateTime64</c> for the list, which refused these shapes
		/// with <c>Type mismatch in IN or VALUES section</c> before.
		/// </remarks>
		[Test]
		public void CapturedDateTimeOffsetListBesideAggregate([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new OffsetRow { Id = 1, Value = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero) } });

			var whole = new[] { new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero) };
			var half  = new[] { new DateTimeOffset(2026, 6, 1, 10, 0, 0, 500, TimeSpan.Zero) };

			var atWhole = t
				.GroupBy(r => r.Id)
				.Count(g => whole.Contains(g.Max(r => r.Value)));

			atWhole.ShouldBe(1);

			var atHalf = t
				.GroupBy(r => r.Id)
				.Count(g => half.Contains(g.Max(r => r.Value)));

			atHalf.ShouldBe(0);
		}
	}
}
