using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue278Tests : TestBase
	{	
		private const int TOTAL_QUERIES_PER_RUN = 100000;

		private static readonly int[] ThreadsCount = new[] { 1, 2, 5, 10, 20 };

		private static IDictionary<string, TimeSpan> _results = new Dictionary<string, TimeSpan>();

		private static readonly Tuple<string, Action<ITestDataContext>[]>[] ActionSets = new[]
		{
			// linq queries
			Tuple.Create("Select"                 , new Action<ITestDataContext>[] { Select                                                             }),
			Tuple.Create("Insert"                 , new Action<ITestDataContext>[] { Insert                                                             }),
			Tuple.Create("InsertWithIdentity"     , new Action<ITestDataContext>[] { InsertWithIdentity                                                 }),
			Tuple.Create("InsertOrUpdate"         , new Action<ITestDataContext>[] { InsertOrUpdate                                                     }),
			Tuple.Create("Update"                 , new Action<ITestDataContext>[] { Update                                                             }),
			Tuple.Create("Delete"                 , new Action<ITestDataContext>[] { Delete                                                             }),
			Tuple.Create("MixedLinq"              , new Action<ITestDataContext>[] { Select, Insert, InsertWithIdentity, InsertOrUpdate, Update, Delete }),

			// object queries
			Tuple.Create("InsertObject"             , new Action<ITestDataContext>[] { InsertObject                                                                             }),
			Tuple.Create("InsertWithIdentityObject" , new Action<ITestDataContext>[] { InsertWithIdentityObject                                                                 }),
			Tuple.Create("InsertOrUpdateObject"     , new Action<ITestDataContext>[] { InsertOrUpdateObject                                                                     }),
			Tuple.Create("UpdateObject"             , new Action<ITestDataContext>[] { UpdateObject                                                                             }),
			Tuple.Create("DeleteObject"             , new Action<ITestDataContext>[] { DeleteObject                                                                             }),
			Tuple.Create("MixedObject"              , new Action<ITestDataContext>[] { InsertObject, InsertWithIdentityObject, InsertOrUpdateObject, UpdateObject, DeleteObject }),

			// linq and object queries mixed
			Tuple.Create("MixedAll", new Action<ITestDataContext>[] { Select, Insert, InsertWithIdentity, InsertOrUpdate, Update, Delete, InsertObject, InsertWithIdentityObject, InsertOrUpdateObject, UpdateObject, DeleteObject }),
		};

		[AttributeUsage(AttributeTargets.Method)]
		class Issue278TestSourceAttribute : IncludeDataContextSourceAttribute
		{
			private readonly bool _withCache;
			private readonly bool _withClear;

			public Issue278TestSourceAttribute(bool withCache, bool withClear)
				: base(TestProvName.NoopProvider)
			{
				// test using noop test provider to be not affected by provider side-effects
				_withCache = withCache;
				_withClear = withClear;
			}

			protected override IEnumerable<Tuple<object[], string>> GetParameters(string provider)
			{
				foreach (var cnt in ThreadsCount)
				{
					foreach (var set in ActionSets)
					{
						var baseName = string.Format("TestPerformance.set={0}.threads={1:00}.cache={2}.clear={3}", set.Item1, cnt, _withCache ? 1 : 0, _withClear ? 1 : 0);
						yield return Tuple.Create(new object[] { provider, cnt, set.Item2, baseName }, baseName);
					}
				}
			}
		}

		[Issue278TestSource(true, false)]
		public void TestPerformanceWithCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, caseName, threadCount, actions, false);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[Issue278TestSource(false, false)]
		public void TestPerformanceWithoutCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = true;

				TestIt(context, caseName, threadCount, actions, false);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[Issue278TestSource(true, true)]
		public void TestPerformanceWithCacheClear(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, caseName, threadCount, actions, true);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		private void TestIt(string context, string caseName, int threadCount, Action<ITestDataContext>[] actions, bool clear)
		{
#if !NETSTANDARD
			int workerThreads;
			int iocpThreads;
			ThreadPool.GetMaxThreads(out workerThreads, out iocpThreads);

			if (workerThreads < threadCount)
				ThreadPool.SetMaxThreads(threadCount, iocpThreads);
#endif

			var start = DateTimeOffset.Now;

			Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
			{
				var rnd = new Random();

				using (new DisableLogging())
				using (var db = GetDataContext(context))
					for (var i = 0; i < TOTAL_QUERIES_PER_RUN / threadCount; i++)
					{
						if (clear)
							Query<LinqDataTypes2>.ClearCache();

						actions[rnd.Next() % actions.Length](db);
					}
			});

			// precision of this approach is more than enough for this test
			var runTime = DateTimeOffset.Now - start;

			_results.Add(caseName, runTime);
		}

		[OneTimeTearDown]
		public void WriteResults()
		{
			// debug output
			//var sb = new StringBuilder();
			//foreach (var key in _results.Keys.OrderBy(_ => _))
			//{
			//	sb.AppendFormat("{0}: {1}", key, _results[key]).AppendLine();
			//}

			//File.WriteAllText(@"c:\1\testrun.txt", sb.ToString());

			//_results.Clear();
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
				DateTimeValue = DateTime.Now
			});
		}

		private static void InsertWithIdentity(ITestDataContext db)
		{
			db.Types2.InsertWithIdentity(() => new LinqDataTypes2()
			{
				DateTimeValue = DateTime.Now
			});
		}

		private static void InsertOrUpdate(ITestDataContext db)
		{
			db.Types2.InsertOrUpdate(() => new LinqDataTypes2()
			{
				ID = 100500,
				DateTimeValue = DateTime.Now
			}, r => new LinqDataTypes2()
			{
				DateTimeValue = DateTime.Now
			});
		}

		private static void Update(ITestDataContext db)
		{
			db.Update(new LinqDataTypes2()
			{
				ID = 100500,
				DateTimeValue = DateTime.Now
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
	}
}
