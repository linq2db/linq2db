using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Linq;
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

		private static readonly Tuple<string, Action<ITestDataContext>[]>[] ActionSets = new[]
		{
			Tuple.Create("Select"             , new Action<ITestDataContext>[] { Select                                                     }),
			Tuple.Create("Insert"             , new Action<ITestDataContext>[] { Insert                                                     }),
			Tuple.Create("InsertWithIdentity" , new Action<ITestDataContext>[] { InsertWithIdentity                                         }),
			Tuple.Create("InsertOrUpdate"     , new Action<ITestDataContext>[] { InsertOrUpdate                                             }),
			Tuple.Create("Update"             , new Action<ITestDataContext>[] { Update                                                     }),
			Tuple.Create("Mixed"              , new Action<ITestDataContext>[] { Select, Insert, InsertWithIdentity, InsertOrUpdate, Update }),
		};

		[AttributeUsage(AttributeTargets.Method)]
		class Issue278TestSourceAttribute : IncludeDataContextSourceAttribute
		{
			private readonly bool _withCache;

			public Issue278TestSourceAttribute(bool withCache)
				: base(TestProvName.NoopProvider)
			{
				// test using noop test provider to be not affected by provider side-effects
				_withCache = withCache;
			}

			protected override IEnumerable<Tuple<object[], string>> GetParameters(string provider)
			{
				foreach (var cnt in ThreadsCount)
				{
					foreach (var set in ActionSets)
					{
						var baseName = string.Format("TestPerformance.set={0}.threads={1:00}.cache={2}", set.Item1, cnt, _withCache ? 1 : 0);
						yield return Tuple.Create(new object[] { provider, cnt, set.Item2, baseName }, baseName);
					}
				}
			}
		}

		[Issue278TestSource(true)]
		public void TestPerformanceWithCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = false;

				TestIt(context, threadCount, actions);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		private void TestIt(string context, int threadCount, Action<ITestDataContext>[] actions)
		{
			int workerThreads;
			int iocpThreads;
			ThreadPool.GetMaxThreads(out workerThreads, out iocpThreads);

			if (workerThreads < threadCount)
				ThreadPool.SetMaxThreads(threadCount, iocpThreads);

			Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
			{
				var rnd = new Random();

				using (var db = GetDataContext(context))
					for (var i = 0; i < TOTAL_QUERIES_PER_RUN / threadCount; i++)
						actions[rnd.Next() % actions.Length](db);
			});
		}

		[Issue278TestSource(false)]
		public void TestPerformanceWithoutCache(string context, int threadCount, Action<ITestDataContext>[] actions, string caseName)
		{
			var oldValue = Configuration.Linq.DisableQueryCache;

			try
			{
				Configuration.Linq.DisableQueryCache = true;

				TestIt(context, threadCount, actions);
			}
			finally
			{
				Configuration.Linq.DisableQueryCache = oldValue;
			}
		}

		private static void Select(ITestDataContext db)
		{
			db.Types.ToList();
		}

		private static void Insert(ITestDataContext db)
		{
			db.Types.Insert(() => new LinqDataTypes()
			{
				DateTimeValue = DateTime.Now
			});
		}

		private static void InsertWithIdentity(ITestDataContext db)
		{
			db.Types.InsertWithIdentity(() => new LinqDataTypes()
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
			db.Types.Update(r => new LinqDataTypes()
			{
				ID = 100500,
				DateTimeValue = DateTime.Now
			});
		}
	}
}
