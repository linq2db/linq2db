#if NET9_0_OR_GREATER

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
	public class IndexTests : TestBase
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
		public void Index([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query = db.GetTable<TestTable>()
				.OrderBy(x => x.Id)
				.Index();

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IndexWithOffset([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query = db.GetTable<TestTable>()
			.OrderBy(x => x.Id)
			.Index();

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IndexWithWhere([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query = db.GetTable<TestTable>()
			.Where(x => x.TestId != 20)
			.OrderBy(x => x.Id)
			.Index();

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IndexWithNavigation([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.OrderBy(x => x.ChildID).Index()
			select new { p.ParentID, Index = c.Index, c.Item.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IndexWithNavigationOffset([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.OrderBy(x => x.ChildID).Index()
			where c.Index < 15
			select new { p.ParentID, Index = c.Index, c.Item.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IndexWithJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent
			from IndexedParent in db.Parent.OrderBy(x => x.ParentID).Index()
			where IndexedParent.Index < 5
			select new { p.ParentID, IndexedParent.Index, IndexedParent.Item.Value1 };

			AssertQuery(query);
		}
	}
}

#endif
