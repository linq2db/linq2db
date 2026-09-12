using System;
using System.Data;
using System.Linq;

using LinqToDB.Benchmarks.TestProvider;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

namespace LinqToDB.Benchmarks.QueryGeneration
{
	// Query-generation cost of the Tests.Linq JoinTests.StackOverflow shape: a self-join chain built by
	// re-joining Parent onto the previous Parent N times. Parent deliberately has no unique key, so
	// JoinsOptimizer cannot collapse the chain and every level survives into the generated SQL.
	//
	// No [Benchmark] attributes on purpose: one operation at depth 100 costs seconds, so BDN's
	// auto-iteration would dominate the default *.QueryGeneration.* run. Use the manual runner.
	public class DeepJoinChainBenchmark
	{
		[Table("Parent")]
		public sealed class Parent
		{
			[Column] public int  ParentID { get; set; }
			[Column] public int? Value1   { get; set; }
		}

		[Table("Child")]
		public sealed class Child
		{
			[PrimaryKey] public int ParentID { get; set; }
			[PrimaryKey] public int ChildID  { get; set; }
		}

		DataConnection _db = null!;

#pragma warning disable CA2000 // Dispose objects before losing scope
		public void Setup()
		{
			_db = new DataConnection(new DataOptions()
				.UseConnection(
					SqlServerTools.GetDataProvider(SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient),
					new MockDbConnection(Array.Empty<QueryResult>(), ConnectionState.Open))
				// the point of the benchmark is building the query, so it must not be served from the cache
				.UseDisableQueryCache(true));
		}
#pragma warning restore CA2000 // Dispose objects before losing scope

		public void Cleanup()
		{
			_db.Dispose();
		}

		public string Build(int depth)
		{
			var q =
					from c in _db.GetTable<Child>()
					join p in _db.GetTable<Parent>() on c.ParentID equals p.ParentID
					select new { p, c };

			for (var i = 0; i < depth; i++)
			{
				q =
					from c in q
					join p in _db.GetTable<Parent>() on c.p.ParentID equals p.ParentID
					select new { p, c.c };
			}

			return q.ToSqlQuery().Sql;
		}

		// Manual runner, mirroring WeakJoinScanBenchmark.RunManually: BDN's child-process toolchain cannot
		// restore its auto-generated project in this repo layout (NU1101), and one operation here costs
		// seconds. Sweeping the depth is the point - the shape of the curve says whether the cost is linear
		// in the number of joins or worse.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030", Justification = "Benchmark output requires Console")]
		public static void RunManually(int warmups = 1, int iterations = 3, int onlyDepth = 0)
		{
			if (warmups    < 0) warmups    = 0;
			if (iterations < 1) iterations = 1;

			var b = new DeepJoinChainBenchmark();
			b.Setup();

			var depths = onlyDepth > 0 ? new[] { onlyDepth } : new[] { 10, 25, 50, 100 };

			Console.WriteLine();
			Console.WriteLine("=== DeepJoinChainBenchmark manual run | warmups: " + warmups + " | iterations: " + iterations + " ===");
			Console.WriteLine();
			Console.WriteLine("| Depth | Joins | Mean (ms) | Median (ms) | Min (ms) | ms/join | SQL len |");
			Console.WriteLine("|---:|---:|---:|---:|---:|---:|---:|");

			foreach (var depth in depths)
			{
				for (var i = 0; i < warmups; i++)
					b.Build(depth);

				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();

				var samples = new double[iterations];
				var sw      = new System.Diagnostics.Stopwatch();
				var sqlLen  = 0;

				for (var i = 0; i < iterations; i++)
				{
					sw.Restart();
					var sql = b.Build(depth);
					sw.Stop();
					samples[i] = sw.Elapsed.TotalMilliseconds;
					sqlLen     = sql.Length;
				}

				var mean  = samples.Average();
				var joins = depth + 1;

				Array.Sort(samples);
				var mid    = samples.Length / 2;
				var median = samples.Length % 2 == 0
					? (samples[mid - 1] + samples[mid]) / 2.0
					: samples[mid];

				Console.WriteLine($"| {depth,5} | {joins,5} | {mean,9:F1} | {median,11:F1} | {samples[0],8:F1} | {mean / joins,7:F2} | {sqlLen,7} |");
			}

			Console.WriteLine();

			b.Cleanup();
		}
	}
}
