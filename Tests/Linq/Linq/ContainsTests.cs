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

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse)]
		[Test]
		public void AllNulls(
			// Excluded Access from tests because it seems to have non compliant behavior.
			// It is the only DB that returns 1 for `WHERE Int NOT IN (null, null)`
			// Nope, Access is not alone anymore
			[DataSources(TestProvName.AllAccess)] string context,
			[Values]                              bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Int.In(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Int.NotIn(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse)]
		[Test]
		public void AllNullsEnum(
			// Excluded Access from tests because it seems to have non compliant behavior.
			// It is the only DB that returns 1 for `WHERE Enum NOT IN (null, null)`
			// Nope, Access is not alone anymore
			[DataSources(TestProvName.AllAccess)] string context,
			[Values]                              bool   withNullCompares)
		{
			using var db  = GetDataContext(context, o => o.UseMappingSchema(new MappingSchema()).UseCompareNulls(withNullCompares ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Enum.In(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Enum.NotIn(null, null));
			count.ShouldBe(withNullCompares ? 1 : 0);
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/38439", Configuration = TestProvName.AllClickHouse)]
		[Test]
		public void AllNullsCEnum(
			// Excluded Access from tests because it seems to have non compliant behavior.
			// It is the only DB that returns 1 for `WHERE CEnum NOT IN (null, null)`
			// Nope, Access is not alone anymore
			[DataSources(TestProvName.AllAccess)] string context,
			[Values]                              bool   withNullCompares)
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
	}
}
