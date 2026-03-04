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
	public class CountByMethodTests : TestBase
	{
		[Table]
		public class TestTable
		{
			[Column] public int Id { get; set; }
			[Column] public int TestId { get; set; }
		}

		TestTable[] CreateTestTableData()
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

		[Test]
		public void CountByWithNavigation([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.CountBy(x => x.ParentID)
			orderby c.Key
			select new { p, c.Value };

			AssertQuery(query);
		}

		[Test]
		public void CountByWithNavigationAndWhere([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.CountBy(x => x.ParentID)
			where c.Value > 0
			select new { p.ParentID, ChildCount = c.Value };

			AssertQuery(query);
		}

		[Test]
		public void CountByWithMultipleGrouping([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.Where(x => x.ChildID > 0).CountBy(x => x.ParentID)
			select new { p.ParentID, c.Key, c.Value };

			AssertQuery(query);
		}

		[Test]
		public void CountByWithNavigationSelectKey([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.CountBy(x => x.ChildID)
			orderby c.Key, c.Value
			select new { ParentID = p.ParentID, ChildCount = c.Value };

			AssertQuery(query);
		}

		[Test]
		public void CountByNestedWithJoin([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
			from p in db.Parent.LoadWith(p => p.Children)
			from c in p.Children.CountBy(x => x.ChildID)
			join p2 in db.Parent on p.ParentID equals p2.ParentID
			select new { p2.ParentID, ChildIDCount = c.Value };

			AssertQuery(query);
		}
	}
}

#endif
