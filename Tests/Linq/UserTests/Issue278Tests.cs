// mono 5.0.1.1-0xamarin5+debian7b1 crashes on those tests
// TODO: try to uncomment, when newer version used on Travis
#if !MONO
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue278Tests : TestBase
	{
		const int TOTAL_QUERIES_PER_RUN = 1000;

		enum CacheMode
		{
			CacheEnabled,
			CacheDisabled,
			ClearCache,
			NoCacheScope
		}

		class Issue278TestData : TestCaseSourceAttribute
		{
			public Issue278TestData(CacheMode mode)
				: base(typeof(Issue278TestData), nameof(TestData), new object[] { mode })
			{
			}

			static IEnumerable TestData(CacheMode mode)
			{
				foreach (var provider in UserProviders.Where(p => p == TestProvName.NoopProvider))
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
							var baseName = $"TestPerformance_set={set.Name}_threads={cnt:00}_cache={mode}";
							yield return new TestCaseData(provider, cnt, set.Action, baseName) { TestName = baseName };
						}
			}
		}

		[Issue278TestData(CacheMode.CacheEnabled)]
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

		[Issue278TestData(CacheMode.CacheDisabled)]
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

		[Issue278TestData(CacheMode.ClearCache)]
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

		[Issue278TestData(CacheMode.NoCacheScope)]
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

		[Test]
		public void TestQueryCacheFull([IncludeDataSources(false, TestProvName.NoopProvider)] string context)
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

		[Test]
		public void TestQueryCacheOverflow([IncludeDataSources(false, TestProvName.NoopProvider)] string context)
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
