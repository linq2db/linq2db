using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Benchmarks
{
	// TODO: move to benchmarks project after benchmarks project PR merged
	/// <summary>
	/// Evaluate different connections performance and memory use in LINQ query cache scenario.
	/// </summary>
	[MemoryDiagnoser]
	public class QueryCacheCollectionsBenchmark
	{
		private const int QUERIES_PER_RUN = 10000;

		private string[] Queries;

		[Params(1, 2, 5, 10, 20)]
		public int Threads { get; set; }

		[Params(10, 100)]
		public int CacheSize { get; set; }

		[Params(0.5f, 1.5f)]
		public float FillFactor { get; set; }

		[Params("List", "LinkedList")]
		public string Collection { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			Queries = new string[(int)(CacheSize * FillFactor)];

			for (var i = 0; i < Queries.Length; i++)
				Queries[i] = "query" + i;
		}

		[Benchmark]
		public void Benchmark()
		{
			if (Collection == "List")
				TestList(new List<string>(CacheSize), Threads, Queries);
			else
				TestLinkedList(new LinkedList<string>(), Threads, Queries);
		}

		private void TestLinkedList<TElement>(LinkedList<TElement> cache, int threadCount, TElement[] queries)
		{
			var sync = new object();
			var version = 0;

			Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
			{
				var rnd = new Random();

				for (var i = 0; i < QUERIES_PER_RUN / threadCount; i++)
				{
					var query = queries[rnd.Next() % queries.Length];

					if (!FindQueryLinkedList(sync, ref version, cache, query))
					{
						var oldVersion = version;
						lock (sync)
						{
							if (oldVersion == version || !FindQueryLinkedList(sync, ref version, cache, query))
							{
								if (cache.Count == CacheSize)
									cache.RemoveLast();

								cache.AddFirst(query);
								version++;
							}
						}
					}
				}
			});
		}

		private bool FindQueryLinkedList<TElement>(object sync, ref int version, LinkedList<TElement> cache, TElement searchedQuery)
		{
			TElement[] queries;

			lock (sync)
				queries = cache.ToArray();

			foreach (var query in queries)
			{
				if (query.Equals(searchedQuery))
				{
					// move found query up in cache
					lock (sync)
					{
						var queryInCache = cache.Find(query);
						if (queryInCache == null)
						{
							// query were evicted from cache - readd it
							if (cache.Count == CacheSize)
								cache.RemoveLast();

							cache.AddFirst(query);
							version++;
						}
						else if (queryInCache.Previous != null)
						{
							var previous = queryInCache.Previous.Value;
							queryInCache.Previous.Value = query;
							queryInCache.Value = previous;
						}
					}

					return true;
				}
			}

			return false;
		}

		private void TestList<TElement>(IList<TElement> cache, int threadCount, TElement[] queries)
		{
			var sync = new object();
			var version = 0;

			Parallel.ForEach(Enumerable.Range(1, threadCount), _ =>
			{
				var rnd = new Random();

				for (var i = 0; i < QUERIES_PER_RUN / threadCount; i++)
				{
					var query = queries[rnd.Next() % queries.Length];

					if (!FindQuery(sync, ref version, cache, query))
					{
						var oldVersion = version;
						lock (sync)
						{
							if (oldVersion == version || !FindQuery(sync, ref version, cache, query))
							{
								if (cache.Count == CacheSize)
									cache.RemoveAt(CacheSize - 1);

								cache.Insert(0, query);
								version++;
							}
						}
					}
				}
			});
		}

		private bool FindQuery<TElement>(object sync, ref int version, IList<TElement> cache, TElement searchedQuery)
		{
			TElement[] queries;

			lock (sync)
				queries = cache.ToArray();

			foreach (var query in queries)
			{
				if (query.Equals(searchedQuery))
				{
					// move found query up in cache
					lock (sync)
					{
						var oldIndex = cache.IndexOf(query);
						if (oldIndex > 0)
						{
							var prev = cache[oldIndex - 1];
							cache[oldIndex - 1] = query;
							cache[oldIndex] = prev;
						}
						else if (oldIndex == -1)
						{
							// query were evicted from cache - readd it
							if (cache.Count == CacheSize)
								cache.RemoveAt(CacheSize - 1);

							cache.Insert(0, query);
							version++;
						}
					}

					return true;
				}
			}

			return false;
		}
	}
}
