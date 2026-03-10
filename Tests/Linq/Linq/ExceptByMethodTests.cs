#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ExceptByMethodTests : TestBase
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
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = "Wrong result for remote")]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.ExceptBy(new[] { 20 }, x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByWithNavigation([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.ExceptBy(new[] { 2 }, x => x.ChildID)
				orderby c.ChildID
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByWithWhere([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.Where(x => x.ChildID > 0).ExceptBy(new[] { 2 }, x => x.ChildID)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByMultipleValues([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.ExceptBy(new[] { 20, 30 }, x => x.TestId)
				.OrderBy(x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = "Wrong result for remote")]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByOrderedResult([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.ExceptBy(new[] { 20 }, x => x.TestId)
				.OrderByDescending(x => x.TestId)
				.ThenBy(x => x.Id);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted]
		public void ExceptByWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var comparer = EqualityComparer<int>.Default;

			_ = table
				.ExceptBy(new[] { 20 }, x => x.TestId, comparer)
				.ToList();
		}
	}
}

#endif
