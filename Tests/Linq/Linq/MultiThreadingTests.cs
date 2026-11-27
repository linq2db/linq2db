using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	[NoBaselines("Multi-threading")]
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

		// disabled providers cannot handle concurrent queries well
		// TODO: disabled providers lacks connections
		// Access: works, but unstable due to lock conflicts in access
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1398")]
		public void InsertFromConcurrentConnections([DataSources(false, TestProvName.AllNativeAccess, TestProvName.AllFirebird, TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllOracle12, TestProvName.AllSQLiteClassic)] string context)
		{
			const int recordsCount = 20;

			// sqlite connection pooling is not compatible with tested template
			SQLiteTools.ClearAllPools(provider: null);

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ConcurrentInsertRecord>())
			{
				var tasks = new List<Task>();

				for (var iteration = 0; iteration < recordsCount; iteration++)
				{
					var local = iteration;
					tasks.Add(Task.Run(() => InsertFromNewConnection(context, local)));
				}

				Task.WaitAll(tasks.ToArray());

				Assert.That(db.GetTable<ConcurrentInsertRecord>().GroupBy(_ => _.Value).Count(), Is.EqualTo(db.GetTable<ConcurrentInsertRecord>().Count()));
			}
		}

		private void InsertFromNewConnection(string context, int value)
		{
			using var db = GetDataContext(context);
			db.GetTable<ConcurrentInsertRecord>().Insert(() => new ConcurrentInsertRecord { Value = value });
		}

		[Retry(3)] // could fail due to deadlock
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1398")]
		public void MergeFromConcurrentConnections([MergeDataContextSource(
			TestProvName.AllFirebird, ProviderName.SybaseManaged, TestProvName.AllInformix)]
			string context)
		{
			const int repeatsCount = 20;
			var rnd = new Random();

			var mammals  = new[] { "Elephant", "Cat" };
			var reptiles = new[] { "Snake", "Lizard" };

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ConcurrentMergeAnimal>())
			using (db.CreateLocalTable<ConcurrentMergeAnimalUpdate>())
			{
				for (var iteration = 0; iteration < repeatsCount; iteration++)
				{
					var mammalsUpdate = new AnimalUpdateBatch()
					{
						Color   = "Grey",
						Updates = mammals
							.Select(name => new ConcurrentMergeAnimalUpdate()
							{
								Name      = name,
								Length    = rnd.Next(),
								Iteration = iteration
							})
							.ToList()
					};

					var reptilesUpdate = new AnimalUpdateBatch()
					{
						Color   = "Green",
						Updates = reptiles
							.Select(name => new ConcurrentMergeAnimalUpdate()
							{
								Name      = name,
								Length    = rnd.Next(),
								Iteration = iteration + repeatsCount
							})
							.ToList()
					};

					foreach (var record in mammalsUpdate.Updates)
						db.Insert(record);
					foreach (var record in reptilesUpdate.Updates)
						db.Insert(record);

					var updateMammalsTask  = Task.Run(() => MergeFromNewConnection(context, mammalsUpdate, iteration));

					var updateReptilesTask = Task.Run(() => MergeFromNewConnection(context, reptilesUpdate, iteration + repeatsCount));

					Task.WaitAll(updateMammalsTask, updateReptilesTask);

					var greenAnimalsCount = db.GetTable<ConcurrentMergeAnimal>().Count(animal => animal.Color == "Green");

					if (greenAnimalsCount != reptiles.Length)
					{
						var error = new StringBuilder($"Error on iteration {iteration + 1}:");

						foreach (var animal in db.GetTable<ConcurrentMergeAnimal>())
						{
							error.AppendLine($"{animal.Name} is {animal.Color}");
						}

						Assert.Fail(error.ToString());
					}
				}
			}
		}

		private void MergeFromNewConnection(string context, AnimalUpdateBatch data, int iteration)
		{
			using var db = GetDataContext(context);
			db.GetTable<ConcurrentMergeAnimal>()
				.Merge()
				.Using(db.GetTable<ConcurrentMergeAnimalUpdate>().Where(_ => _.Iteration == iteration))
				.On((target, source) => target.Name == source.Name)
				.UpdateWhenMatched(
					(target, source) => new ConcurrentMergeAnimal()
					{
						Color = data.Color,
						Length = source.Length
					})
				.InsertWhenNotMatched(
					source => new ConcurrentMergeAnimal()
					{
						Name = source.Name,
						Color = data.Color,
						Length = source.Length
					})
				.Merge();
		}

		[Table("ConcurrentMergeAnimal")]
		internal sealed class ConcurrentMergeAnimal
		{
			[PrimaryKey, Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Name { get; set; } = null!;

			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Color { get; set; } = null!;

			[Column(CanBeNull = false)]
			public int Length { get; set; }
		}

		[Table("ConcurrentMergeAnimalUpdate")]
		internal sealed class ConcurrentMergeAnimalUpdate
		{
			[Column(CanBeNull = false, DataType = DataType.NVarChar, Length = 100)]
			public string Name { get; set; } = null!;

			[Column(CanBeNull = false)]
			public int Length { get; set; }

			[Column(CanBeNull = false)]
			public int Iteration { get; set; }
		}

		internal sealed class AnimalUpdateBatch
		{
			public string Color { get; set; } = null!;

			public List<ConcurrentMergeAnimalUpdate> Updates { get; set; } = null!;
		}

		[Table("ConcurrentInsertRecord")]
		internal sealed class ConcurrentInsertRecord
		{
			[Column(CanBeNull = false), PrimaryKey]
			public int Value { get; set; }
		}

		const int TOTAL_QUERIES_PER_RUN = 1000;

		[QueryCacheTestData(CacheMode.CacheEnabled)]
		public void TestPerformanceWithCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			TestIt(context, caseName, threadCount, actions, CacheMode.CacheEnabled, false);
		}

		[QueryCacheTestData(CacheMode.CacheDisabled)]
		public void TestPerformanceWithoutCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			TestIt(context, caseName, threadCount, actions, CacheMode.CacheDisabled, true);
		}

		[QueryCacheTestData(CacheMode.ClearCache)]
		public void TestPerformanceWithCacheClear(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			TestIt(context, caseName, threadCount, actions, CacheMode.ClearCache, false);
		}

		[QueryCacheTestData(CacheMode.NoCacheScope)]
		public void TestPerformanceWithNoCacheScope(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			TestIt(context, caseName, threadCount, actions, CacheMode.NoCacheScope, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/278")]
		public void ParallelQueriesFillingQueryCache([IncludeDataSources(TestProvName.NoopProvider)] string context)
		{
			var actions     = new Action<ITestDataContext>[100];

			var dbParam     = Expression.Parameter(typeof(ITestDataContext), "db");
			var tableMethod = MemberHelper.MethodOf(() => DataExtensions.GetTable<LinqDataTypes2>(null!));
			var table       = Expression.Call(tableMethod, dbParam);

			var recordParam = Expression.Parameter(typeof(LinqDataTypes2), "record");
			var where       = MemberHelper.MethodOf(() => Queryable.Where<LinqDataTypes2>(null!, (Expression<Func<LinqDataTypes2, bool>>?)null!));

			var toListMethod = MemberHelper.MethodOf(() => Enumerable.ToList<LinqDataTypes2>(null!));

			for (var i = 0; i < actions.Length; i++)
			{
				var predicateBody = Expression.Equal(Expression.PropertyOrField(recordParam, "ID"), Expression.Constant(i));
				var predicate     = Expression.Lambda<Func<LinqDataTypes2, bool>>(predicateBody, recordParam);
				var body          = Expression.Call(where, table, predicate);

				body = Expression.Call(toListMethod, body);

				actions[i] = Expression.Lambda<Action<ITestDataContext>>(body, dbParam).CompileExpression();
			}

			TestIt(context, "ParallelQueriesOverflowingQueryCache", 10, actions, CacheMode.CacheEnabled, false);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/278")]
		public void ParallelQueriesOverflowingQueryCache([IncludeDataSources(TestProvName.NoopProvider)] string context)
		{
			var actions     = new Action<ITestDataContext>[100 + 50];

			var dbParam     = Expression.Parameter(typeof(ITestDataContext), "db");
			var tableMethod = MemberHelper.MethodOf(() => DataExtensions.GetTable<LinqDataTypes2>(null!));
			var table       = Expression.Call(tableMethod, dbParam);

			var recordParam = Expression.Parameter(typeof(LinqDataTypes2), "record");
			var where       = MemberHelper.MethodOf(() => Queryable.Where<LinqDataTypes2>(null!, (Expression<Func<LinqDataTypes2, bool>>?)null!));

			var toListMethod = MemberHelper.MethodOf(() => Enumerable.ToList<LinqDataTypes2>(null!));

			for (var i = 0; i < actions.Length; i++)
			{
				var predicateBody = Expression.Equal(Expression.PropertyOrField(recordParam, "ID"), Expression.Constant(i));
				var predicate     = Expression.Lambda<Func<LinqDataTypes2, bool>>(predicateBody, recordParam);
				var body          = Expression.Call(where, table, predicate);

				body = Expression.Call(toListMethod, body);

				actions[i] = Expression.Lambda<Action<ITestDataContext>>(body, dbParam).CompileExpression();
			}

			TestIt(context, "ParallelQueriesOverflowingQueryCache", 10, actions, CacheMode.CacheEnabled, false);
		}

		private void TestIt(string context, string caseName, int threadCount, Action<ITestDataContext>[] actions, CacheMode mode, bool disableQueryCache)
		{
			ThreadPool.GetMaxThreads(out var workerThreads, out var iocpThreads);

			if (workerThreads < threadCount)
				ThreadPool.SetMaxThreads(threadCount, iocpThreads);

			var start = DateTimeOffset.Now;

			using (new DisableLogging())
				Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
				{
					var rnd = new Random();

					using var db = GetDataContext(context, o => o.UseDisableQueryCache(disableQueryCache));
					for (var i = 0; i < TOTAL_QUERIES_PER_RUN / threadCount; i++)
					{
						if (mode == CacheMode.ClearCache)
							db.GetTable<LinqDataTypes2>().ClearCache();

						if (mode == CacheMode.NoCacheScope && (rnd.Next() % 2 == 0))
							using (NoLinqCache.Scope())
								actions[rnd.Next() % actions.Length](db);
						else
							actions[rnd.Next() % actions.Length](db);
					}
				});

			// precision of this approach is more than enough for this test
			var runTime = DateTimeOffset.Now - start;

			//_results.Add(caseName, runTime);
		}

		private static void Select(ITestDataContext db)
		{
			db.Types2.Where(_ => _.ID == 100500).ToList();
		}

		private static void Delete(ITestDataContext db)
		{
			db.Types2.Delete(_ => _.ID != 100500);
		}

		private static void Insert(ITestDataContext db)
		{
			db.Types2.Insert(() => new LinqDataTypes2()
			{
				DateTimeValue = TestData.DateTime
			});
		}

		private static void InsertWithIdentity(ITestDataContext db)
		{
			db.Types2.InsertWithIdentity(() => new LinqDataTypes2()
			{
				DateTimeValue = TestData.DateTime
			});
		}

		private static void InsertOrUpdate(ITestDataContext db)
		{
			db.Types2.InsertOrUpdate(() => new LinqDataTypes2()
			{
				ID = 100500,
				DateTimeValue = TestData.DateTime
			}, r => new LinqDataTypes2()
			{
				DateTimeValue = TestData.DateTime
			});
		}

		private static void Update(ITestDataContext db)
		{
			db.Types2.Update(_ => new LinqDataTypes2()
			{
				ID = 100500,
				DateTimeValue = TestData.DateTime
			});
		}

		private static void DeleteObject(ITestDataContext db)
		{
			db.Delete(new LinqDataTypes2());
		}

		private static void InsertObject(ITestDataContext db)
		{
			db.Insert(new LinqDataTypes2());
		}

		private static void InsertWithIdentityObject(ITestDataContext db)
		{
			db.InsertWithIdentity(new LinqDataTypes2());
		}

		private static void InsertOrUpdateObject(ITestDataContext db)
		{
			db.InsertOrReplace(new LinqDataTypes2());
		}

		private static void UpdateObject(ITestDataContext db)
		{
			db.Update(new LinqDataTypes2());
		}

		enum CacheMode
		{
			CacheEnabled,
			CacheDisabled,
			ClearCache,
			NoCacheScope
		}

		sealed class QueryCacheTestDataAttribute : TestCaseSourceAttribute
		{
			public QueryCacheTestDataAttribute(CacheMode mode)
				: base(typeof(QueryCacheTestDataAttribute), nameof(TestData), new object[] { mode })
			{
			}

			static IEnumerable TestData(CacheMode mode)
			{
				foreach (var provider in TestConfiguration.UserProviders.Where(p => p == TestProvName.NoopProvider))
					foreach (var cnt in new[] { 1, 2, 5, 10, 20 })
						foreach (var set in new[]
						{
							// linq queries
							new { Name = "Select",                   Action = new Action<ITestDataContext>[] { Select                                                             } },
							new { Name = "Insert",                   Action = new Action<ITestDataContext>[] { Insert                                                             } },
							new { Name = "InsertWithIdentity",       Action = new Action<ITestDataContext>[] { InsertWithIdentity                                                 } },
							new { Name = "InsertOrUpdate",           Action = new Action<ITestDataContext>[] { InsertOrUpdate                                                     } },
							new { Name = "Update",                   Action = new Action<ITestDataContext>[] { Update                                                             } },
							new { Name = "Delete",                   Action = new Action<ITestDataContext>[] { Delete                                                             } },
							new { Name = "MixedLinq",                Action = new Action<ITestDataContext>[] { Select, Insert, InsertWithIdentity, InsertOrUpdate, Update, Delete } },

							// object queries
							new { Name = "InsertObject",             Action = new Action<ITestDataContext>[] { InsertObject                                                                             } },
							new { Name = "InsertWithIdentityObject", Action = new Action<ITestDataContext>[] { InsertWithIdentityObject                                                                 } },
							new { Name = "InsertOrUpdateObject",     Action = new Action<ITestDataContext>[] { InsertOrUpdateObject                                                                     } },
							new { Name = "UpdateObject",             Action = new Action<ITestDataContext>[] { UpdateObject                                                                             } },
							new { Name = "DeleteObject",             Action = new Action<ITestDataContext>[] { DeleteObject                                                                             } },
							new { Name = "MixedObject",              Action = new Action<ITestDataContext>[] { InsertObject, InsertWithIdentityObject, InsertOrUpdateObject, UpdateObject, DeleteObject } },

							// linq and object queries mixed
							new { Name = "MixedAll",                 Action = new Action<ITestDataContext>[] { Select, Insert, InsertWithIdentity, InsertOrUpdate, Update, Delete, InsertObject, InsertWithIdentityObject, InsertOrUpdateObject, UpdateObject, DeleteObject } },
						})
						{
							var baseName = $"TestPerformance_set-{set.Name}_threads-{cnt:00}_cache-{mode}";
							yield return new TestCaseData(provider, cnt, set.Action, baseName) { TestName = baseName };
						}
			}
		}

		// Informix disabled due to issue, described here (but it reproduced with client 4.1):
		// https://www-01.ibm.com/support/docview.wss?uid=swg1IC66046
		[Test(Description = "https://github.com/linq2db/linq2db/issues/513")]
		[NoBaselines("Multi-threading")]
		public void ConcurrentDistinctOverInheritanceAssociation([DataSources(TestProvName.AllInformix)] string context)
		{
			using var semaphore = new Semaphore(0, 10);
			var tasks = new Task[10];
			for (var i = 0; i < 10; i++)
				tasks[i] = new Task(() => RunConcurrentDistinctQuery(context, semaphore));

			for (var i = 0; i < 10; i++)
				tasks[i].Start();

			Thread.Sleep(100);
			semaphore.Release(10);

			Task.WaitAll(tasks);
		}

		void RunConcurrentDistinctQuery(string context, Semaphore semaphore)
		{
			try
			{
				using var db = GetDataContext(context);
				semaphore.WaitOne();

				AreEqual(
					   InheritanceChild.Select(_ => _.Parent).Distinct(),
					db.InheritanceChild.Select(_ => _.Parent).Distinct());
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}
