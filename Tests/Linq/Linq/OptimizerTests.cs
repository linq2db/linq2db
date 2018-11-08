using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class OptimizerTests : TestBase
	{
		class OptimizerData
		{
			[PrimaryKey(1)]
			public int Key1 { get; set; }
			[PrimaryKey(2)]
			public int Key2 { get; set; }

			[Column]
			public int DataKey11 { get; set; }

			[Column]
			public int DataKey21 { get; set; }
			[Column]
			public int DataKey22 { get; set; }

			[Column]
			public int DataKey31 { get; set; }
			[Column]
			public int DataKey32 { get; set; }
			[Column]
			public int DataKey33 { get; set; }

			[Column(Length = 50)]
			public string ValueStr { get; set; }
		}

		static IEnumerable<T[]> GetPermutations<T>(IEnumerable<T> items, int count)
		{
		    int i = 0;
			var itemsCopy = items.ToArray();
		    foreach (var item in itemsCopy)
		    {
		        if (count == 1)
		            yield return new T[] { item };
		        else
		        {
		            foreach (var result in GetPermutations(itemsCopy.Skip(i + 1), count - 1))
		                yield return (new T[] { item }.Concat(result)).ToArray();
		        }

		        ++i;
		    }
		}

		OptimizerData[] GenerateTestData()
		{
			var keys = GetPermutations(new[] { 1, 2, 3, 4 }, 2).ToArray();

			var unique1 = GetPermutations(new[] { 10,   20,   30,   40, 50, 60 }, 1).Take(keys.Length).ToArray();
			var unique2 = GetPermutations(new[] { 100,  200,  300,  400 },        2).Take(keys.Length).ToArray();
			var unique3 = GetPermutations(new[] { 1000, 2000, 3000, 4000, 5000 }, 3).Take(keys.Length).ToArray();

			var result = Enumerable.Range(0, keys.Length)
				.Select(i =>
					new OptimizerData
					{
						Key1 = keys[i][0],
						Key2 = keys[i][1],

						DataKey11 = unique1[i][0],

						DataKey21 = unique2[i][0],
						DataKey22 = unique2[i][1],

						DataKey31 = unique3[i][0],
						DataKey32 = unique3[i][1],
						DataKey33 = unique3[i][2],

						ValueStr = "Str_" + i
					}
				)
				.ToArray();

			return result;
		}


		[Test]
		public void AsSubQueryTest([DataSources] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			{
				var sq = second.Where(v => v.Key1 != 1)
					.AsSubQuery();

				var query =
					from s in sq
					from f in first.LeftJoin(f => f.Key1 == s.Key1 && f.Key2 == s.Key2)
					select new
					{
						f,
						s
					};

				Console.WriteLine(query.ToString());
				Assert.AreEqual(2, query.EnumQueries().Count());

				// test that optimizer removes subquery

				var sqNormal = second.Where(v => v.Key1 != 1);

				var queryOptimized =
					from s in sqNormal
					from f in first.LeftJoin(f => f.Key1 == s.Key1 && f.Key2 == s.Key2)
					select new
					{
						f,
						s
					};

				Console.WriteLine(queryOptimized.ToString());
				Assert.AreEqual(1, queryOptimized.EnumQueries().Count());
			}
		}


		[Test]
		public void DistinctOptimization([DataSources] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			{
				var uniqueValues = first.Select(f => new { f.DataKey31, f.DataKey11 }).Distinct();

				var query = from s in second
					from d in uniqueValues.LeftJoin(d => d.DataKey11 == s.DataKey11 && d.DataKey31 == s.DataKey31)
					select new
					{
						s,
						d
					};

				Console.WriteLine(query.ToString());
				Assert.AreEqual(1, query.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected = query.Select(p => p.s);
				Console.WriteLine(projected.ToString());
				Assert.AreEqual(0, projected.EnumQueries().SelectMany(q => q.EnumJoins()).Count());
			}
		}

		[Test]
		public void GroupByOptimization([DataSources] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			{
				var uniqueValues = from f in first
					group f by new { f.Key1, f.Key2, f.DataKey21, f.DataKey22 }
					into g
					select new
					{
						g.Key.Key1,
						g.Key.Key2,
						g.Key.DataKey21,
						g.Key.DataKey22,

						Count = g.Count()
					};

				var query = from s in second
					from u in uniqueValues.LeftJoin(u => u.DataKey21 == s.DataKey21 && u.DataKey22 == s.DataKey22 && u.Key1 == s.Key1 && u.Key2 == s.Key2)
					from nu in uniqueValues.LeftJoin(nu => nu.DataKey21 == s.DataKey21 && nu.DataKey21 == s.DataKey22)
					select new
					{
						s,
						UCount = u.Count,
						MNUCount = nu.Count
					};

				Console.WriteLine(query.ToString());
				Assert.AreEqual(2, query.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected = query.Select(p => p.s);
				Console.WriteLine(projected.ToString());
				Assert.AreEqual(1, projected.EnumQueries().SelectMany(q => q.EnumJoins()).Count());
			}
		}

		[Test]
		public void PrimaryKeyOptimization([DataSources] string context, [Values] bool opimizerSwitch)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			using (new WithoutJoinOptimization(opimizerSwitch))
			{
				var uniqueValues = first.Select(f => new { f.Key1, f.Key2 });

				var query = from s in second
					from d in uniqueValues.LeftJoin(d => d.Key1 == s.Key1 && d.Key2 == s.Key2)
					select new
					{
						s,
						d
					};

				Console.WriteLine(query.ToString());
				Assert.AreEqual(1, query.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected = query.Select(p => p.s);
				Console.WriteLine(projected.ToString());
				Assert.AreEqual(opimizerSwitch ? 0 : 1, projected.EnumQueries().SelectMany(q => q.EnumJoins()).Count());
			}
		}

		[Test]
		public void HasKeyProjectionOptimization([DataSources] string context, [Values] bool opimizerSwitch)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			using (new WithoutJoinOptimization(opimizerSwitch))
			{
				var allKeys = first.Select(f => new { First = f })
					.HasUniqueKey(f => new {f.First.DataKey11})
					.HasUniqueKey(f => new {f.First.DataKey21, f.First.DataKey22})
					.HasUniqueKey(f => new {f.First.DataKey31, f.First.DataKey32, f.First.DataKey33})
					.Select(f => f.First);

				// With single key

				var query1 = from s in second
					from a in allKeys.LeftJoin(a => a.DataKey11 == s.DataKey11)
					select new
					{
						Second = s,
						First = a
					};

				Console.WriteLine(query1.ToString());
				Assert.AreEqual(1, query1.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected1 = query1.Select(p => p.Second);
				Console.WriteLine(projected1.ToString());
				Assert.AreEqual(opimizerSwitch ? 0 : 1, projected1.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				// With two keys

				var query2 = from s in second
					from a in allKeys.LeftJoin(a =>
						a.DataKey22 == s.DataKey22 && a.DataKey21 == s.DataKey21 && a.Key1 == s.Key1) 
					select new
					{
						Second = s,
						First = a
					};

				Console.WriteLine(query2.ToString());
				Assert.AreEqual(1, query2.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected2 = query2.Select(p => p.Second);
				Console.WriteLine(projected2.ToString());
				Assert.AreEqual(opimizerSwitch ? 0 : 1, projected2.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				// With three keys

				var query3 = from s in second
					from a in allKeys.LeftJoin(a =>
						a.DataKey31 == s.DataKey31 && a.DataKey32 == s.DataKey32 && a.DataKey33 == s.DataKey33) 
					select new
					{
						Second = s,
						First = a
					};

				Console.WriteLine(query3.ToString());
				Assert.AreEqual(1, query3.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

				var projected3 = query3.Select(p => p.Second);
				Console.WriteLine(projected3.ToString());
				Assert.AreEqual(opimizerSwitch ? 0 : 1, projected3.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

			}
		}

		[Test]
		public void HasKeyJoinOptimization([DataSources] string context, [Values] bool opimizerSwitch)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			using (new WithoutJoinOptimization(opimizerSwitch))
			{
				var allKeys = first.Select(f => new { First = f })
					.HasUniqueKey(f => new {f.First.DataKey11})
					.HasUniqueKey(f => new {f.First.DataKey21, f.First.DataKey22})
					.HasUniqueKey(f => new {f.First.DataKey31, f.First.DataKey32, f.First.DataKey33})
					.Select(f => f.First);

				var query = from s in second
					from p2 in allKeys.InnerJoin(p2 => p2.Key1 == s.Key1 && p2.Key2 == s.Key2)
					from f1 in allKeys.InnerJoin(f1 => f1.DataKey11 == s.DataKey11)
					from f2 in allKeys.InnerJoin(f2 => f2.DataKey21 == s.DataKey21 && f2.DataKey22 == s.DataKey22)
					from f3 in allKeys.InnerJoin(f3 => f3.DataKey31 == s.DataKey31 && f3.DataKey32 == s.DataKey32 && f3.DataKey33 == s.DataKey33)
					from pp2 in allKeys.InnerJoin(pp2 => pp2.Key1 == s.Key1 && pp2.Key2 == s.Key2)
					from ff1 in allKeys.InnerJoin(ff1 => ff1.DataKey11 == s.DataKey11 && ff1.ValueStr != null)
					from ff2 in allKeys.InnerJoin(ff2 => ff2.DataKey21 == s.DataKey21 && ff2.DataKey22 == s.DataKey22 && ff2.DataKey22 > 0)
					from ff3 in allKeys.InnerJoin(ff3 => ff3.DataKey31 == s.DataKey31 && ff3.DataKey32 == s.DataKey32 && ff3.DataKey33 == s.DataKey33 && ff3.Key1 > 0)
					select new
					{
						F1 = f1,
						F2 = f2,
						F3 = f3,
						FF1 = ff1,
						FF2 = ff2,
						FF3 = ff3,
					};

				Console.WriteLine(query.ToString());
				Assert.AreEqual(opimizerSwitch ? 4 : 8, query.EnumQueries().SelectMany(q => q.EnumJoins()).Count());

			}
		}

		[Test]
		public void UniqueKeysPropagation([DataSources] string context, [Values] bool opimizerSwitch)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (var first = db.CreateLocalTable("FirstOptimizerData", testData))
			using (var second = db.CreateLocalTable("SecondOptimizerData", testData))
			using (new WithoutJoinOptimization(opimizerSwitch))
			{
				var subqueryWhichWillBeOptimized =
					from f in first
					where f.ValueStr.StartsWith("Str")
					select f;

				subqueryWhichWillBeOptimized = subqueryWhichWillBeOptimized.HasUniqueKey(f => f.DataKey11);

				var query =
					from s in second.HasUniqueKey(s => new { s.Key1, s.Key2 })
					from f in subqueryWhichWillBeOptimized.InnerJoin(f => f.DataKey11 == s.DataKey11)
					select new
					{
						S = s,
						F = f
					};

				Console.WriteLine(query.ToString());

				var selectQuery = query.EnumQueries().Single();
				var table = selectQuery.From.Tables[0];
				var joinedTable = table.Joins[0].Table;
				Assert.IsTrue(joinedTable.HasUniqueKeys && table.HasUniqueKeys);
				
				Assert.AreEqual(2, joinedTable.UniqueKeys.Count + table.UniqueKeys.Count);
			}
		}

	}
}
