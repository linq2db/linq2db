#if !NETFRAMEWORK

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
			[PrimaryKey] public int Id { get; set; }
			[Column] public int TestId { get; set; }
			[Column] public int? NullableTestId { get; set; }
		}

		private TestTable[] CreateTestTableData()
		{
			return [
				new TestTable() { Id = 1, TestId = 20, NullableTestId = null },
				new TestTable() { Id = 2, TestId = 20, NullableTestId = null },
				new TestTable() { Id = 3, TestId = 30, NullableTestId = 30 },
				new TestTable() { Id = 4, TestId = 30, NullableTestId = 30 },
				new TestTable() { Id = 5, TestId = 40, NullableTestId = 40 }
				];
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.OrderBy(x => x.Id)
				.ExceptBy(new[] { 20 }, x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByWithNavigation([IncludeDataSources(TestProvName.WithApplyJoin, TestProvName.AllSQLite)] string context)
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
		public void ExceptByWithWhere([IncludeDataSources(TestProvName.WithApplyJoin, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.Where(x => x.ChildID > 0).ExceptBy(new[] { 2 }, x => x.ChildID)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		public void ExceptByNavigationEdgeCases([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var duplicateAndMissingKeys =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.ExceptBy(new[] { 2, 2, int.MaxValue }, x => x.ChildID)
				orderby p.ParentID, c.ChildID
				select new { p.ParentID, c.ChildID };

			var emptyKeys =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.ExceptBy(Array.Empty<int>(), x => x.ChildID)
				orderby p.ParentID, c.ChildID
				select new { p.ParentID, c.ChildID };

			AssertQuery(duplicateAndMissingKeys);
			AssertQuery(emptyKeys);
		}

		[Test]
		public void ExceptBySQLiteNullableCompositeKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var keys = new[]
			{
				new { TestId = 20, NullableTestId = (int?)null },
				new { TestId = 20, NullableTestId = (int?)null },
				new { TestId = 99, NullableTestId = (int?)99 }
			};

			AssertQuery(table
				.Where(x => x.Id > 0)
				.ExceptBy(keys, x => new { x.TestId, x.NullableTestId })
				.Select(x => new { x.Id, x.TestId, x.NullableTestId })
				.OrderBy(x => x.Id));
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
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void ExceptByOrderedResult([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.OrderBy(x => x.Id)
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
