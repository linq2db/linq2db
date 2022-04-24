using FluentAssertions;
using LinqToDB;
using LinqToDB.Tools;
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
			[Values(true, false)] bool   withNullCompares)
		{
			using var nulls = withNullCompares ? null : new WithoutComparisonNullCheck();
			using var db    = GetDataContext(context);
			using var src   = SetupSrcTable(db);

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

			count = src.Count(s => !s.Int.In(Array.Empty<int?>()));
			count.Should().Be(2);
		}

		[Test]
		public void AllNulls(
			// Excluded Access from tests because it seems to have non compliant behavior.
			// It is the only DB that returns 1 for `WHERE Int NOT IN (null, null)`
			[DataSources(TestProvName.AllAccess)] string context,
			[Values(true, false)]                 bool   withNullCompares)
		{
			using var nulls = withNullCompares ? null : new WithoutComparisonNullCheck();
			using var db    = GetDataContext(context);
			using var src   = SetupSrcTable(db);

			int count; 
			
			count = src.Count(s => s.Int.In(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);

			count = src.Count(s => s.Int.NotIn(null, null));
			count.Should().Be(withNullCompares ? 1 : 0);
		}

		class Src
		{
			public int  Id  { get; set; }
			public int? Int { get; set; }
		}
	}
}
