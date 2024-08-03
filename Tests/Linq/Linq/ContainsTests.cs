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
#pragma warning disable CA2263 // Prefer generic overload when type is known
			new FluentMappingBuilder(db.MappingSchema)
				.Entity<Src>()
					.Property(e => e.CEnum)
						.HasDataType(DataType.VarChar)
						.HasLength(20)
						.HasConversion(v => $"___{v}___", v => (ConvertedEnum)Enum.Parse(typeof(ConvertedEnum), v.Substring(3, v.Length - 6)))
				.Build();
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
			using var _   = new CompareNullsOption(withNullCompares);
			using var db  = GetDataContext(context, new MappingSchema());
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
	}
}
