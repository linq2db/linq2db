#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class CountByTests : TestBase
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

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void CountByFinal([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query = db.GetTable<TestTable>()
				.CountBy(x => x.TestId)
				.OrderBy(x => x.Key);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void CountBySubquery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query =
				from t in db.GetTable<TestTable>()
				let count = db.GetTable<TestTable>().CountBy(x => x.TestId).Where(c => c.Key == t.TestId).Select(c => c.Value).Single()
				select new
				{
					t.TestId,
					Count = count
				};

			AssertQuery(query);
		}
	}
}

#endif
