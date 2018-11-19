// mono 5.0.1.1-0xamarin5+debian7b1 crashes on those tests
// TODO: try to uncomment, when newer version used on Travis
#if !MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{

	[TestFixture]
	public class Issue278Tests : TestBase
	{
		private const int TOTAL_QUERIES_PER_RUN = 1000;

		private static readonly int[] ThreadsCount = new[] { 1, 2, 5, 10, 20 };

		//private static IDictionary<string, TimeSpan> _results = new Dictionary<string, TimeSpan>();

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

		enum CacheMode
		{
			CacheEnabled,
			CacheDisabled,
			ClearCache,
			NoCacheScope
		}

		[AttributeUsage(AttributeTargets.Method)]
		class Issue278TestSourceAttribute : IncludeDataContextSourceAttribute
		{
			private readonly CacheMode _mode;

			public Issue278TestSourceAttribute(CacheMode mode)
				: base(TestProvName.NoopProvider)
			{
				// test using noop test provider to be not affected by provider side-effects
				_mode = mode;
			}

			protected override IEnumerable<Tuple<object[], string>> GetParameters(string provider)
			{
				foreach (var cnt in ThreadsCount)
				{
					foreach (var set in ActionSets)
					{
						var baseName = string.Format("TestPerformance.set={0}.threads={1:00}.cache={2}", set.Item1, cnt, _mode);
						yield return Tuple.Create(new object[] { provider, cnt, set.Item2, baseName }, baseName);
					}
				}
			}
		}

		[Issue278TestSource(CacheMode.CacheEnabled)]
		public void TestPerformanceWithCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, caseName, threadCount, actions, CacheMode.CacheEnabled);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[Issue278TestSource(CacheMode.CacheDisabled)]
		public void TestPerformanceWithoutCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = true;

				TestIt(context, caseName, threadCount, actions, CacheMode.CacheDisabled);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[Issue278TestSource(CacheMode.ClearCache)]
		public void TestPerformanceWithCacheClear(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, caseName, threadCount, actions, CacheMode.ClearCache);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[Issue278TestSource(CacheMode.NoCacheScope)]
		public void TestPerformanceWithNoCacheScope(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, caseName, threadCount, actions, CacheMode.NoCacheScope);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[IncludeDataContextSource(false, TestProvName.NoopProvider)]
		public void TestQueryCacheFull(string context)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			var actions = new Action<ITestDataContext>[100];

			var dbParam = Expression.Parameter(typeof(ITestDataContext), "db");
			var tableMethod = MemberHelper.MethodOf(() => DataExtensions.GetTable<LinqDataTypes2>(null));
			var table = Expression.Call(tableMethod, dbParam);

			var recordParam = Expression.Parameter(typeof(LinqDataTypes2), "record");
			var where = MemberHelper.MethodOf(() => Queryable.Where<LinqDataTypes2>(null, (Expression<Func<LinqDataTypes2, bool>>)null));

			var toListMethod = MemberHelper.MethodOf(() => Enumerable.ToList<LinqDataTypes2>(null));

			for (var i = 0; i < actions.Length; i++)
			{
				var predicateBody = Expression.Equal(Expression.PropertyOrField(recordParam, "ID"), Expression.Constant(i));
				var predicate = Expression.Lambda<Func<LinqDataTypes2, bool>>(predicateBody, recordParam);
				var body = Expression.Call(where, table, predicate);

				body = Expression.Call(toListMethod, body);

				actions[i] = Expression.Lambda<Action<ITestDataContext>>(body, dbParam).Compile();
			}

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, "TestQueryCacheOverflow", 10, actions, CacheMode.CacheEnabled);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		[IncludeDataContextSource(false, TestProvName.NoopProvider)]
		public void TestQueryCacheOverflow(string context)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			var actions = new Action<ITestDataContext>[100 + 50];

			var dbParam = Expression.Parameter(typeof(ITestDataContext), "db");
			var tableMethod = MemberHelper.MethodOf(() => DataExtensions.GetTable<LinqDataTypes2>(null));
			var table = Expression.Call(tableMethod, dbParam);

			var recordParam = Expression.Parameter(typeof(LinqDataTypes2), "record");
			var where = MemberHelper.MethodOf(() => Queryable.Where<LinqDataTypes2>(null, (Expression<Func<LinqDataTypes2, bool>>)null));

			var toListMethod = MemberHelper.MethodOf(() => Enumerable.ToList<LinqDataTypes2>(null));

			for (var i = 0; i < actions.Length; i++)
			{
				var predicateBody = Expression.Equal(Expression.PropertyOrField(recordParam, "ID"), Expression.Constant(i));
				var predicate = Expression.Lambda<Func<LinqDataTypes2, bool>>(predicateBody, recordParam);
				var body = Expression.Call(where, table, predicate);

				body = Expression.Call(toListMethod, body);

				actions[i] = Expression.Lambda<Action<ITestDataContext>>(body, dbParam).Compile();
			}

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, "TestQueryCacheOverflow", 10, actions, CacheMode.CacheEnabled);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		private void TestIt(string context, string caseName, int threadCount, Action<ITestDataContext>[] actions, CacheMode mode)
		{
#if !NETSTANDARD1_6
			ThreadPool.GetMaxThreads(out var workerThreads, out var iocpThreads);

			if (workerThreads < threadCount)
				ThreadPool.SetMaxThreads(threadCount, iocpThreads);
#endif

			var start = DateTimeOffset.Now;

			using (new DisableLogging())
				Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
				{
					var rnd = new Random();

					using (var db = GetDataContext(context))
						for (var i = 0; i < TOTAL_QUERIES_PER_RUN / threadCount; i++)
						{
							if (mode == CacheMode.ClearCache)
								Query<LinqDataTypes2>.ClearCache();

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
			db.Types2.Update(_ => new LinqDataTypes2()
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
#endif
