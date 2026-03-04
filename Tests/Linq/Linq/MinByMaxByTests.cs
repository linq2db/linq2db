#if NET6_0_OR_GREATER

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
	public class MinByMaxByTests : TestBase
	{
		[Table]
		public class TestTable
		{
			[Column] public int Id { get; set; }
			[Column] public int TestId { get; set; }
		}

		private TestTable[] CreateTestTableData()
		{
			return [
				new TestTable() { Id = 1, TestId = 20},
				new TestTable() { Id = 2, TestId = 20 },
				new TestTable() { Id = 3, TestId = 30 },
				new TestTable() { Id = 4, TestId = 30 },
				new TestTable() { Id = 5, TestId = 40 }
				];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void MinBy([DataSources] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result      = table.MinBy(x => x.Id);
			var compareData = testData.MinBy(x => x.Id);

			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void MaxBy([DataSources] string context)
		{
			var testData = CreateTestTableData();

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(testData);

			var result = table.OrderBy(x => x.TestId).MaxBy(x => x.Id);
			var compareData = testData.OrderBy(x => x.TestId).MaxBy(x => x.Id);

			result!.Id.ShouldBe(compareData!.Id);
		}
	}
}

#endif
