#if NET9_0_OR_GREATER
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4412Tests : TestBase
	{
		[Table]
		public class TestTable
		{
			[Column] public int    Id          { get; set; }
			[Column] public int    TestId      { get; set; }
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
		public void Net9MethodsMinBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().MinBy(x => x.Id);
			var compareData = CreateTestTableData().MinBy(x => x.Id);
			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsMaxBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().MaxBy(x => x.Id);
			var compareData = CreateTestTableData().MaxBy(x => x.Id);
			result!.Id.ShouldBe(compareData!.Id);
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllFirebirdLess3, TestProvName.MySql57Connector, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_RowNumber)]
		[Test]
		public void Net9MethodsExceptBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().ExceptBy(new[] { 20 }, x => x.TestId).ToList();
			var compareData = CreateTestTableData().ExceptBy(new[] { 20 }, x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllFirebirdLess3, TestProvName.MySql57Connector, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_RowNumber)]
		[Test]
		public void Net9MethodsIntersectBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().IntersectBy(new[] { 20, 30 }, x => x.TestId).ToList();
			var compareData = CreateTestTableData().IntersectBy(new[] { 20, 30 }, x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllFirebirdLess3, TestProvName.MySql57Connector, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_RowNumber)]
		[Test]
		public void Net9MethodsUnionBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var _ = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().UnionBy(db.GetTable<TestTable>(), x => x.TestId).ToList();
			var compareData = CreateTestTableData().UnionBy(CreateTestTableData(), x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllFirebirdLess3, TestProvName.MySql57Connector, TestProvName.AllSybase], ErrorMessage = ErrorHelper.Error_RowNumber)]
		[Test]
		public void Net9MethodsIndex([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = db.CreateLocalTable(CreateTestTableData());

			var query = db.GetTable<TestTable>()
				.OrderBy(x => x.Id)
				.Index();

			AssertQuery(query);
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

		//[ActiveIssue]
		//[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		//public void Net9MethodsAggregateBy([DataSources] string context)
		//{
		//	using var db = GetDataContext(context);

		//	using var _ = db.CreateLocalTable(CreateTestTableData());
		//	var result = db.GetTable<TestTable>().AggregateBy(x => x.TestId, new List<string>(), );
		//	var compareData = CreateTestTableData().AggregateBy(x => x.TestId);
		//	result.ShouldBe(compareData);
		//}
	}
}
#endif

