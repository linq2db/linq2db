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
	public class ExceptByTests : TestBase
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
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var query = db.GetTable<TestTable>().ExceptBy(new[] { 20 }, x => x.TestId);

			AssertQuery(query);
		}
	}
}

#endif
