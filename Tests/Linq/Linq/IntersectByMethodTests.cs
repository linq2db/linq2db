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
	public class IntersectByMethodTests : TestBase
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = "Wrong result for remote")]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectBy([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.IntersectBy(new[] { 20, 30 }, x => x.TestId);

			AssertQuery(query);
		}

		[Test]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithNavigation([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
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
		public void IntersectByWithWhere([IncludeDataSources(TestProvName.WithApplyJoin)] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				from c in p.Children.Where(x => x.ChildID > 0).IntersectBy(new[] { 1, 3 }, x => x.ChildID)
				select new { p.ParentID, c.ChildID };

			AssertQuery(query);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = "Wrong result for remote")]
		[ThrowsCannotBeConverted([TestProvName.AllAccess, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllMySql57, TestProvName.AllFirebirdLess3])]
		public void IntersectByWithOrdering([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(CreateTestTableData());

			var query = table
				.IntersectBy(new[] { 20, 30 }, x => x.TestId)
				.OrderByDescending(x => x.Id);

			AssertQuery(query);
		}

		[ActiveIssue(Configurations = [TestProvName.AllOracle21Minus, TestProvName.AllClickHouse, TestProvName.AllMariaDB], Details = "Wrong result")]
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
				.IntersectBy(exclude, x => x.TestId)
				.OrderBy(x => x.TestId);

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
