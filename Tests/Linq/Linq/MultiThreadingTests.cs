using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;
using Tests.xUpdate;

namespace Tests.Linq
{
	[TestFixture]
	public class MultiThreadingTests : TestBase
	{
		public static void DumpObject(object? obj)
		{
			if (obj == null)
				return;

			TestContext.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
		}
		
		[Table]
		class MultiThreadedData
		{
			[Column(IsPrimaryKey = true)] 
			public int Id    { get; set; }
			[Column] public int Value { get; set; }
			[Column(Length = 50, DataType = DataType.Char)] 
			public string StrValue { get; set; } = null!;
			
			public static MultiThreadedData[] TestData()
			{
				return Enumerable.Range(1, 100)
					.Select(i => new MultiThreadedData {Id = i, Value = i * 10, StrValue = "Strx" + i})
					.ToArray();
			}
		}

		public void ConcurrentRunner<TParam, TResult>(DataConnection dc, string context, int threadsPerParam, Func<DataConnection, TParam, TResult> queryFunc,
			Action<TResult, TParam> checkAction, params TParam[] parameters)
		{
			var threadCount = threadsPerParam * parameters.Length;
			if (threadCount <= 0)
				throw new InvalidOperationException();

			// maximum Provider pool count
			const int poolCount = 10;

			var semaphore = new Semaphore(0, poolCount);
			
			var threads = new Thread[threadCount];
			var results = new Tuple<TParam, TResult, string, DbParameter[]?, Exception?>[threadCount];

			for (var i = 0; i < threadCount; i++)
			{
				var param = parameters[i % parameters.Length];
				var n = i;
				threads[i] = new Thread(() =>
				{
					semaphore.WaitOne();
					try
					{
						try
						{
							using (var threadDb = (DataConnection)GetDataContext(context))
							{
								DbParameter[]? parameters = null;
								threadDb.OnCommandInitialized += args =>
								{
									parameters = args.Command.Parameters.Cast<DbParameter>().ToArray();
								};

								var result = queryFunc(threadDb, param);
								results[n] = Tuple.Create(param, result, threadDb.LastQuery!, parameters, (Exception?)null);
							}
						}
						catch (Exception e)
						{
							results[n] = Tuple.Create(param, default(TResult), "", (DbParameter[]?)null, e)!;
						}

					}
					finally
					{
						semaphore.Release();
					}
				});
			}

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Start();
			}

			semaphore.Release(poolCount);

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Join();
			}

			for (int i = 0; i < threads.Length; i++)
			{
				var result = results[i];
				if (result.Item5 != null)
				{
					TestContext.WriteLine($"Exception in query ({result.Item1}):\n\n{result.Item5}");
					throw result.Item5;
				}
				try
				{
					checkAction(result.Item2, result.Item1);
				}
				catch
				{
					var testResult = queryFunc(dc, result!.Item1);

					TestContext.WriteLine($"Failed query ({result.Item1}):\n");
					if (result.Item4 != null)
					{
						var sb = new StringBuilder();
						dc.DataProvider.CreateSqlBuilder(dc.MappingSchema).PrintParameters(sb, result.Item4.OfType<IDbDataParameter>());
						TestContext.WriteLine(sb);
					}
					TestContext.WriteLine();
					TestContext.WriteLine(result.Item3);

					DumpObject(result.Item2);
					
					DumpObject(testResult);


					throw;
				}
			}
		}

		[Test]
		public void StartsWithTests([DataSources(false, ProviderName.Sybase)] string context)
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
		public void EndsWithTests([DataSources(false, ProviderName.Sybase)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(testData, true))
			{
				ConcurrentRunner(db, context, 10,
					(threadDb, p) =>
					{
						var query = threadDb.GetTable<MultiThreadedData>().Where(x => x.StrValue.Trim().EndsWith(p));
						return query.Select(q => q.StrValue).ToArray();
					}, (result, p) =>
					{
						var query = testData.Where(x => x.StrValue.EndsWith(p));
						var expected = query.Select(q => q.StrValue).ToArray();
						AreEqual(expected, result);
					}, "1", "x1", "x11", "x33", "x2");
			}
		}

		[Test]
		public void ParamOptimization([DataSources(false, ProviderName.Sybase)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using (var db    = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(testData, true))
			{
				ConcurrentRunner(db, context, 2,
					(threadDb, p) =>
					{
						var query = threadDb.GetTable<MultiThreadedData>().Where(x => p % 2 == 0 && x.Id == p || x.Id == p % 3 + 1);
						return query.Select(q => q.Id).ToArray();
					}, (result, p) =>
					{
						var query = testData.Where(x => p % 2 == 0 && x.Id == p || x.Id == p % 3  + 1);
						var expected = query.Select(q => q.Id).ToArray();
						AreEqual(expected, result);
					}, Enumerable.Range(1, 50).ToArray());
			}
		}		
		
		[Test]
		public void MergeInsert([MergeTests.MergeDataContextSource(false, ProviderName.Sybase, TestProvName.AllInformix)] string context)
		{
			using var d1 = new DisableBaseline("Multi-threading");
			using var d2 = new DisableLogging();

			var testData = MultiThreadedData.TestData();

			// transaction (or delay) required for Access and Firebird, otherwise it is possible for other threads
			// to read incomplete results, because inserted data is not made available yet to other threads by
			// database engine
			using (var db    = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(testData, true))
			{
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
		}

	}
}
