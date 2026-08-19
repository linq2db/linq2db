using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5445Tests : TestBase
	{
		[Table]
		sealed class TestTable
		{
			[PrimaryKey]           public int  Id        { get; set; }
			[Column, Nullable]     public int? NullField { get; set; }
		}

		static readonly TestTable[] _testData =
		[
			new() { Id = 1, NullField = 1 },
			new() { Id = 2, NullField = null },
		];

		[Test]
		public void NullableHasValueWithNonNullParam([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(_testData);

			int? nullable = 1;
			Expression<Func<TestTable, bool>> filter = t => nullable.HasValue && t.NullField == nullable;

			var result = table.Where(filter).ToList();

			result.Count.ShouldBe(1);
			result[0].Id.ShouldBe(1);
		}

		[Test]
		public void NullableHasValueWithNullParam([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(_testData);

			int? nullable = null;
			Expression<Func<TestTable, bool>> filter = t => nullable.HasValue && t.NullField == nullable;

			// HasValue is false, so the entire predicate is false — no rows should be returned
			var result = table.Where(filter).ToList();

			result.ShouldBeEmpty();
		}

		[Test]
		public void NullableHasValueExtractedWithNullParam([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(_testData);

			int? nullable = null;
			bool hasValue = nullable.HasValue;

			// Control case: extracting HasValue before expression works correctly
			var result = table.Where(t => hasValue && t.NullField == nullable).ToList();

			result.ShouldBeEmpty();
		}
	}
}
