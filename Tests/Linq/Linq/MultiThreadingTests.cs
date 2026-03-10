using System.Data;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class MultiThreadingTests : TestBase
	{
		[Table]
		sealed class MultiThreadedData
		{
			[Column(IsPrimaryKey = true)]
			public int Id    { get; set; }
			[Column] public int Value { get; set; }
			[Column(DataType = DataType.NVarChar, Configuration = ProviderName.ClickHouse)]
			[Column(Length = 50, DataType = DataType.Char)]
			public string StrValue { get; set; } = null!;

			public static MultiThreadedData[] TestData()
			{
				return Enumerable.Range(1, 100)
					.Select(i => new MultiThreadedData {Id = i, Value = i * 10, StrValue = "Strx" + i})
					.ToArray();
			}
		}

		[Test]
		public void StartsWithTests([DataSources(false, TestProvName.AllSybase)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using (var db = (DataConnection)GetDataContext(context))
			using (db.CreateLocalTable(testData, true))
			{
				ConcurrentRunner(db, context, 10,
					(threadDb, p) =>
					{
						var query = threadDb.GetTable<MultiThreadedData>().Where(x => x.StrValue.StartsWith(p));
						return query.Select(q => q.StrValue).ToArray();
					}, (result, p) =>
					{
						var query = testData.Where(x => x.StrValue.StartsWith(p));
						var expected = query.Select(q => q.StrValue).ToArray();
						AreEqual(expected, result);
					}, "Strx1", "Strx100", "Strx", "Strx33", "Strx2");
			}
		}

		[Test]
		public void EndsWithTests([DataSources(false, TestProvName.AllSybase)] string context)
		{
			var skipTrim = context.IsAnyOf(TestProvName.AllClickHouse);

			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable(testData, true);
			ConcurrentRunner(db, context, 10,
				(threadDb, p) =>
				{
					var query = skipTrim
							? threadDb.GetTable<MultiThreadedData>().Where(x => x.StrValue.EndsWith(p))
							: threadDb.GetTable<MultiThreadedData>().Where(x => x.StrValue.Trim().EndsWith(p));
					return query.Select(q => q.StrValue).ToArray();
				}, (result, p) =>
				{
					var query = testData.Where(x => x.StrValue.EndsWith(p));
					var expected = query.Select(q => q.StrValue).ToArray();
					AreEqual(expected, result);
				}, "1", "x1", "x11", "x33", "x2");
		}

		[Test]
		public void ParamOptimization([DataSources(false, TestProvName.AllSybase)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable(testData, true);
			ConcurrentRunner(db, context, 2,
				(threadDb, p) =>
				{
					var query = threadDb.GetTable<MultiThreadedData>().Where(x => (p % 2 == 0 && x.Id == p) || x.Id == p % 3 + 1);
					return query.Select(q => q.Id).ToArray();
				}, (result, p) =>
				{
					var query = testData.Where(x => (p % 2 == 0 && x.Id == p) || x.Id == p % 3  + 1);
					var expected = query.Select(q => q.Id).ToArray();
					AreEqual(expected, result);
				}, Enumerable.Range(1, 50).ToArray());
		}

		[Test]
		public void MergeInsert([MergeDataContextSource(false, TestProvName.AllSybase, TestProvName.AllInformix)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable(testData, true);
			ConcurrentRunner(db, context, 1,
				(threadDb, p) =>
				{
					var result = threadDb.GetTable<MultiThreadedData>()
							.Merge()
							.Using(testData.Where(x => x.Id <= p))
							.OnTargetKey()
							.InsertWhenNotMatched()
							.Merge();
					return result;
				}, (result, p) =>
				{

				}, Enumerable.Range(1, 100).ToArray());
		}

		[Test]
		public void EagerLoadMultiLevel([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable(testData, true);
			ConcurrentRunner(db, context, 1,
				(threadDb, p) =>
				{
					var param1 = p;
					var param2 = p;
					var result = threadDb.GetTable<MultiThreadedData>()
							.Where(t => t.Id > param1)
							.Select(t => new
							{
								t.Id,
								t.StrValue,
								t.Value,
								Others = threadDb.GetTable<MultiThreadedData>().Where(x => x.Id > t.Id && x.Id > param1 && x.Id > param2).OrderBy(x => x.Id)
									.Select(o => new
									{
										o.Id,
										o.StrValue,
										Param = param1,
										SubOthers = threadDb.GetTable<MultiThreadedData>()
											.Where(x => x.Id > o.Id)
											.OrderBy(x => x.Id)
											.ToArray()
									}).ToArray()
							})
							.ToList();
					return result;
				}, (result, p) =>
				{
					var param1 = p;
					var param2 = p;
					var expected = testData
							.Where(t => t.Id > param1)
							.Select(t => new
							{
								t.Id,
								t.StrValue,
								t.Value,
								Others = testData.Where(x => x.Id > t.Id && x.Id > param1 && x.Id > param2).OrderBy(x => x.Id)
									.Select(o => new
									{
										o.Id,
										o.StrValue,
										Param = param1,
										SubOthers = testData
											.Where(x => x.Id > o.Id)
											.OrderBy(x => x.Id)
											.ToArray()
									}).ToArray()
							})
							.ToList();

					result.Count.ShouldBe(expected.Count);

					if (expected.Count > 0)
						AreEqualWithComparer(result, expected);
				}, Enumerable.Range(1, 100).ToArray());
		}

		/*
		[Test]
		public void EagerLoadingX([DataSources(false)] string context, [Values(1, 2, 3)] int p)
		{
			var testData = MultiThreadedData.TestData();

			using (var threadDb    = (DataConnection)GetDataContext(context))
			using (var table = threadDb.CreateLocalTable(testData, true))
			{
				var param1 = p;
				var param2 = p;
				var result = threadDb.GetTable<MultiThreadedData>()
					.Where(t => t.Id > param1)
					.Select(t => new
					{
						t.Id,
						t.StrValue,
						t.Value,
						Others = threadDb.GetTable<MultiThreadedData>().Where(x => x.Id > t.Id && x.Id > param1 && x.Id > param2).OrderBy(x => x.Id)
							.Select(o => new
							{
								o.Id,
								o.StrValue,
								Param = param1,
								SubOthers = threadDb.GetTable<MultiThreadedData>()
									.Where(x => x.Id > o.Id)
									.OrderBy(x => x.Id)
									.ToArray()
							}).ToArray()
					})
					.ToList();

				var expected = testData
					.Where(t => t.Id > param1)
					.Select(t => new
					{
						t.Id,
						t.StrValue,
						t.Value,
						Others = testData.Where(x => x.Id > t.Id && x.Id > param1 && x.Id > param2).OrderBy(x => x.Id)
							.Select(o => new
							{
								o.Id,
								o.StrValue,
								Param = param1,
								SubOthers = testData
									.Where(x => x.Id > o.Id)
									.OrderBy(x => x.Id)
									.ToArray()
							}).ToArray()
					})
					.ToList();

				AreEqualWithComparer(result, expected);

			}
		}
		*/

	}
}
