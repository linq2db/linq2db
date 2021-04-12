using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Tools;
using NUnit.Framework;
using System.Linq;
using FluentAssertions;
using System.Linq.Expressions;
using System;

namespace Tests.Linq
{
	[TestFixture]
	public class ContainsTests : TestBase
	{
		private TempTable<Src> SetupSrcTable(IDataContext db)
		{
			var data = new[]
			{
				new Src { Id = 1, Int = null },
				new Src { Id = 2, Int = 2 },
			};

			var src  = db.CreateLocalTable(data);
			return src;
		}

		[Test]
		public void Functional(
			[DataSources]         string context, 
			[Values(true, false)] bool   withNullContains,
			[Values(true, false)] bool   withNullCompares)
		{
			using var null1 = withNullContains ? null : new WithoutContainsNullCheck();
			using var null2 = withNullCompares ? null : new WithoutComparisonNullCheck();
			using var db    = GetDataContext(context);
			using var src   = SetupSrcTable(db);

			int? result;

			result = FetchId(s => s.Int.In(-1, null));
			result.Should().Be(withNullContains ? 1 : 0);

			result = FetchId(s => s.Int.In(-1, 2));
			result.Should().Be(2);

			result = FetchId(s => s.Int.NotIn(2, null));
			result.Should().Be(withNullContains ? 0 : 1);

			result = FetchId(s => s.Int.NotIn(-1, 2));
			result.Should().Be(withNullCompares ? 1 : 0);

			int FetchId(Expression<Func<Src, bool>> predicate)
				=> src.Where(predicate).Select(x => x.Id).FirstOrDefault();
		}

		[Test]
		public void Empty(
			[DataSources]         string context,
			[Values(true, false)] bool   withNullCompares)
		{
			using var nulls = withNullCompares ? null : new WithoutComparisonNullCheck();
			using var db    = GetDataContext(context);
			using var src   = SetupSrcTable(db);

			int count; 
			
			count = src.Count(s => s.Int.In(Array.Empty<int?>()));
			count.Should().Be(0);

			count = src.Count(s => s.Int.NotIn(Array.Empty<int?>()));
			count.Should().Be(2);
		}

		private static (bool withNullCompares, bool withNullContains, int? x, int? y, bool @in, bool notIn)[] ClientSideValues = new[]
		{
			(true,  true,  1,          1,          true,  false),
			(true,  true,  2,          1,          false, true ),
			(true,  true,  (int?)null, 1,          false, true ),
			(false, true,  (int?)null, 1,          false, false),
			(true,  true,  1,          (int?)null, false, true ),
			(true,  false, 1,          (int?)null, false, true ),
			(true,  true,  (int?)null, (int?)null, true,  false),
			(true,  false, (int?)null, (int?)null, false, true ),
			(false, true,  (int?)null, (int?)null, true,  false),
			(false, false, (int?)null, (int?)null, false, false),
		};

		[Test]
		public void ClientSide([ValueSource(nameof(ClientSideValues))] (bool, bool, int?, int?, bool, bool) values)
		{
			var (withNullCompares, withNullContains, x, y, @in, notIn) = values;

			using var null1 = withNullContains ? null : new WithoutContainsNullCheck();
			using var nulls = withNullCompares ? null : new WithoutComparisonNullCheck();

			bool result;

			result = x.In(-1, y);
			result.Should().Be(@in);

			result = x.NotIn(-1, y);
			result.Should().Be(notIn);
		}

		class Src
		{
			public int  Id  { get; set; }
			public int? Int { get; set; }
		}
	}
}
