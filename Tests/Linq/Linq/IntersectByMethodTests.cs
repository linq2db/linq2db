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
	public class IntersectByMethodTests : TestBase
	{
		[Table]
		public class TestTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int TestId { get; set; }
			[Column] public int? NullableTestId { get; set; }
		}

		TestTable[] CreateTestTableData()
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
		public void IntersectBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.OrderBy(x => x.Id)
				.IntersectBy(new[] { 20, 30 }, x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithNavigation([IncludeDataSources(TestProvName.WithApplyJoin, TestProvName.AllSQLite)] string context)
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
		public void IntersectByWithWhere([IncludeDataSources(TestProvName.WithApplyJoin, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				from c in p.Children.Where(x => x.ChildID > 0).IntersectBy(new[] { 1, 3 }, x => x.ChildID)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		public void IntersectByNavigationEdgeCases([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var duplicateAndMissingKeys =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.IntersectBy(new[] { 2, 2, int.MaxValue }, x => x.ChildID)
				orderby p.ParentID, c.ChildID
				select new { p.ParentID, c.ChildID };

			var emptyKeys =
				from p in db.Parent.LoadWith(x => x.Children)
				from c in p.Children.IntersectBy(Array.Empty<int>(), x => x.ChildID)
				orderby p.ParentID, c.ChildID
				select new { p.ParentID, c.ChildID };

			AssertQuery(duplicateAndMissingKeys);
			AssertQuery(emptyKeys);
		}

		[Test]
		public void IntersectBySQLiteNullableCompositeKey([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
				.IntersectBy(keys, x => new { x.TestId, x.NullableTestId })
				.Select(x => new { x.Id, x.TestId, x.NullableTestId })
				.OrderBy(x => x.Id));
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithOrdering([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.OrderBy(x => x.Id)
				.IntersectBy(new[] { 20, 30 }, x => x.TestId)
				.OrderByDescending(x => x.Id);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByFromAnotherQuery([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var exclude = table
				.Where(x => x.Id <= 2)
				.Select(x => x.TestId);

			var query = table
				.OrderBy(x => x.Id)
				.IntersectBy(exclude, x => x.TestId)
				.OrderByDescending(x => x.Id);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted]
		public void IntersectByWithComparerShouldFail([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var comparer = EqualityComparer<int>.Default;

			_ = table
				.IntersectBy(new[] { 20, 30 }, x => x.TestId, comparer)
				.ToList();
		}
	}
}

#endif
