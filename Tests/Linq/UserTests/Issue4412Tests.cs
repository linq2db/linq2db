using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
#if NET9_0_OR_GREATER
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

		[Test]
		public void Net9MethodsDistinctBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().OrderBy(x => x.Id).DistinctBy(x => x.TestId).ToList();
			var compareData = CreateTestTableData().OrderBy(x => x.Id).DistinctBy(x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsMinBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().MinBy(x => x.Id);
			var compareData = CreateTestTableData().MinBy(x => x.Id);
			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsMaxBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().MaxBy(x => x.Id);
			var compareData = CreateTestTableData().MaxBy(x => x.Id);
			result!.Id.ShouldBe(compareData!.Id);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsExceptBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().ExceptBy(new[] { 20 }, x => x.TestId).ToList();
			var compareData = CreateTestTableData().ExceptBy(new[] { 20 }, x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsIntersectBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().IntersectBy(new[] { 20, 30 }, x => x.TestId).ToList();
			var compareData = CreateTestTableData().IntersectBy(new[] { 20, 30 }, x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsUnionBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().UnionBy(db.GetTable<TestTable>(), x => x.TestId).ToList();
			var compareData = CreateTestTableData().UnionBy(CreateTestTableData(), x => x.TestId).ToList();
			result.Count.ShouldBe(compareData.Count);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsIndex([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().OrderBy(x => x.Id).Index().ToList();
			var compareData = CreateTestTableData().OrderBy(x => x.Id).Index().ToList();
			result.Count.ShouldBe(compareData.Count);
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Index.ShouldBe(compareData[i].Index);
				result[i].Item.Id.ShouldBe(compareData[i].Item.Id);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		public void Net9MethodsCountBy([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
			var result = db.GetTable<TestTable>().CountBy(x => x.TestId).OrderBy(x => x.Key).ToList();
			var compareData = CreateTestTableData().CountBy(x => x.TestId).OrderBy(x => x.Key).ToList();
			result.Count.ShouldBe(compareData.Count);
			for (int i = 0; i < result.Count; i++)
			{
				result[i].Key.ShouldBe(compareData[i].Key);
				result[i].Value.ShouldBe(compareData[i].Value);
			}
		}

		//[ActiveIssue]
		//[Test(Description = "https://github.com/linq2db/linq2db/issues/4412")]
		//public void Net9MethodsAggregateBy([DataSources] string context)
		//{
		//	using var db = GetDataContext(context);

		//	using var testeeCoverageTable = db.CreateLocalTable(CreateTestTableData());
		//	var result = db.GetTable<TestTable>().AggregateBy(x => x.TestId, new List<string>(), );
		//	var compareData = CreateTestTableData().AggregateBy(x => x.TestId);
		//	result.ShouldBe(compareData);
		//}
	}
#endif
}
