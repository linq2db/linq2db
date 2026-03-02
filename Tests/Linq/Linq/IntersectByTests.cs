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
	public class IntersectByTests : TestBase
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
		public void IntersectBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var query = db.GetTable<TestTable>().IntersectBy(new[] { 20, 30 }, x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithNavigation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.IntersectBy(new[] { 1, 2, 3 }, x => x.ChildID)
			orderby c.ChildID
			select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithWhere([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.Where(x => x.ChildID > 0).IntersectBy(new[] { 1, 3 }, x => x.ChildID)
			select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithOrdering([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var query = db.GetTable<TestTable>()
			.IntersectBy(new[] { 20, 30 }, x => x.TestId)
			.OrderByDescending(x => x.Id);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByFromAnotherQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var exclude = db.GetTable<TestTable>().Where(x => x.Id <= 2).Select(x => x.TestId);
			var query = db.GetTable<TestTable>().IntersectBy(exclude, x => x.TestId).OrderBy(x => x.TestId);

			AssertQuery(query);
		}
	}
}

#endif
