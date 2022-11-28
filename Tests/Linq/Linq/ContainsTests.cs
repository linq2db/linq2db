using FluentAssertions;
using LinqToDB;
using LinqToDB.Tools;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Tests.Linq
{
	[TestFixture]
	public class ContainsTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			db.GetFluentMappingBuilder()
				.Entity<Src>()
					.Property(e => e.CEnum)
						.HasDataType(DataType.VarChar)
						.HasLength(20)
						.HasConversion(v => $"___{v}___", v => (ConvertedEnum)Enum.Parse(typeof(ConvertedEnum), v.Substring(3, v.Length - 6)));

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
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.Int.In(-1, -2));
			result.Should().Be(0);

			result = FetchId(s => s.Int.In(-1, null));
			result.Should().Be(withNullCompares ? 1 : 0);

			result = FetchId(s => s.Int.In(-1, 2));
			result.Should().Be(2);

			result = FetchId(s => s.Int.NotIn(null, 2));
			result.Should().Be(0);

			result = FetchId(s => s.Int.NotIn(-1, 2));
			result.Should().Be(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void FunctionalEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, ContainsEnum.Value4));
			result.Should().Be(0);

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, null));
			result.Should().Be(withNullCompares ? 1 : 0);

			result = FetchId(s => s.Enum.In(ContainsEnum.Value3, ContainsEnum.Value2));
			result.Should().Be(2);

			result = FetchId(s => s.Enum.NotIn(null, ContainsEnum.Value2));
			result.Should().Be(0);

			result = FetchId(s => s.Enum.NotIn(ContainsEnum.Value3, ContainsEnum.Value2));
			result.Should().Be(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void FunctionalCEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, ConvertedEnum.Value4));
			result.Should().Be(0);

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, null));
			result.Should().Be(withNullCompares ? 1 : 0);

			result = FetchId(s => s.CEnum.In(ConvertedEnum.Value3, ConvertedEnum.Value2));
			result.Should().Be(2);

			result = FetchId(s => s.CEnum.NotIn(null, ConvertedEnum.Value2));
			result.Should().Be(0);

			result = FetchId(s => s.CEnum.NotIn(ConvertedEnum.Value3, ConvertedEnum.Value2));
			result.Should().Be(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void Empty(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Int.In(Array.Empty<int?>()));
			count.Should().Be(0);

			count = src.Count(s => s.Int.NotIn(Array.Empty<int?>()));
			count.Should().Be(2);

			count = src.Count(s => !s.Int.In(Array.Empty<int?>()));
			count.Should().Be(2);
		}

		[Test]
		public void EmptyEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Enum.In(Array.Empty<ContainsEnum?>()));
			count.Should().Be(0);

			count = src.Count(s => s.Enum.NotIn(Array.Empty<ContainsEnum?>()));
			count.Should().Be(2);

			count = src.Count(s => !s.Enum.In(Array.Empty<ContainsEnum?>()));
			count.Should().Be(2);
		}

		[Test]
		public void EmptyCEnum(
			[DataSources] string context,
			[Values]      bool   withNullCompares)
		{
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.CEnum.In(Array.Empty<ConvertedEnum?>()));
			count.Should().Be(0);

			count = src.Count(s => s.CEnum.NotIn(Array.Empty<ConvertedEnum?>()));
			count.Should().Be(2);

			count = src.Count(s => !s.CEnum.In(Array.Empty<ConvertedEnum?>()));
			count.Should().Be(2);
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
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Int.In(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Int.NotIn(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);
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
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.Enum.In(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Enum.NotIn(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);
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
			using var _   = new CompareNullsAsValuesOption(withNullCompares);
			using var db  = GetDataContext(context);
			using var src = SetupSrcTable(db);

			int count;

			count = src.Count(s => s.CEnum.In(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);

			count = src.Count(s => s.CEnum.NotIn(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);
		}

		sealed class Src
		{
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
	}
}
