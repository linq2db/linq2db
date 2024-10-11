using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ExpandTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		private static SampleClass[] GenerateData()
		{
			var sampleData = new[]
			{
				new SampleClass { Id = 1, Value = 1 },
				new SampleClass { Id = 2, Value = 2 },
				new SampleClass { Id = 3, Value = 3 },
			};
			return sampleData;
		}

		Expression<Func<SampleClass, bool>> GetTestPredicate(int v)
		{
			return c => c.Value == v;
		}

		[Test]
		public void InvocationTestLocal([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int param)
		{
			Expression<Func<SampleClass,bool>> predicate = c => c.Value > param;
			var sampleData = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var query = from t in table
					where predicate.Compile()(t)
					select t;
				var expected = from t in sampleData
					where predicate.Compile()(t)
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		[Test]
		public void CompileTestLocal([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int param)
		{
			Expression<Func<SampleClass, bool>> predicate = c => c.Value > param;
			var sampleData = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var query = from t in table
					from t2 in table.Where(predicate.Compile())
					select t;

				var expected = from t in sampleData
					from t2 in sampleData.Where(predicate.Compile())
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		[Test]
		public void NonCompileTestLocal([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int param)
		{
			Expression<Func<SampleClass, bool>> predicate = c => c.Value > param;
			var sampleData = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var query = from t in table
					from t2 in table.Where(predicate)
					select t;

				//DO NOT REMOVE, it forces caching query
				var str = query.ToString();
				TestContext.Out.WriteLine(str);

				var expected = from t in sampleData
					from t2 in sampleData.Where(predicate.Compile())
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		[Test]
		public void InvocationTestFunction([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int param)
		{
			var sampleData = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var query = from t in table
					where GetTestPredicate(param).Compile()(t)
					select t;
				var expected = from t in sampleData
					where GetTestPredicate(param).Compile()(t)
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		[Test]
		public void LocalInvocation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(2, 3)] int param)
		{
			var sampleData = GenerateData();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var ids = new[] { 1, 2, 3, 4, 5, 6 };

				var query = from t in table
					where ids.Where(i => i < param).GroupBy(i => i).Select(i => i.Key).Contains(t.Id)
					select t;
				var expected = from t in sampleData
					where ids.Where(i => i < param).GroupBy(i => i).Select(i => i.Key).Contains(t.Id)
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		[Test]
		public void InvocationTestByInvoke([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int param)
		{
			var sampleData = GenerateData();

			Expression<Func<int, int, int>> func = (p1, p2) => p1 * 10 + p2 * 2;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(sampleData))
			{
				var query = table.AsQueryable();

				query.Set(q => q.Value, q => func.Compile()(param, q.Value)).Update();

				var compiled = func.Compile();
				foreach (var sd in sampleData)
				{
					sd.Value = compiled(param, sd.Value);
				}

				AreEqualWithComparer(sampleData, query);
			}
		}

	}
}
