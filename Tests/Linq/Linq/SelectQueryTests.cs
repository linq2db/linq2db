﻿using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectQueryTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[ActiveIssue(Configuration = TestProvName.AllInformix, Details = "Informix interval cannot be created from non-literal value")]
		[Test]
		public void UnionTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var values1 = from t in db.GetTable<SampleClass>()
					where t.Value == 1
					select new
					{
						Value1 = Sql.DateAdd(Sql.DateParts.Day, t.Value, Sql.CurrentTimestamp),
						Value2 = Sql.DateAdd(Sql.DateParts.Day, 2, Sql.CurrentTimestamp)
					};

				var values2 = db.SelectQuery(() => new
				{
					Value1 = Sql.DateAdd(Sql.DateParts.Day, 3, Sql.CurrentTimestamp),
					Value2 = Sql.DateAdd(Sql.DateParts.Day, 4, Sql.CurrentTimestamp)
				});

				var query = values1.Union(values2);
				var result = query.ToArray();

				var result2 = query.Select(v => v.Value2).ToArray();
			}
		}

		[ActiveIssue(Configuration = TestProvName.AllInformix, Details = "Informix interval cannot be created from non-literal value")]
		[Test]
		public void SubQueryTest([DataSources(TestProvName.AllAccess)] string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (new AllowMultipleQuery())
			using (db.CreateLocalTable(data))
			{
				var values1 = from t in db.GetTable<SampleClass>()
					where t.Value == 1
					select new
					{
						Value1 = Sql.DateAdd(Sql.DateParts.Day, t.Value, Sql.CurrentTimestamp),
						Value2 = Sql.DateAdd(Sql.DateParts.Day, 2, Sql.CurrentTimestamp)
					};

				var values2 = db.SelectQuery(() => new
				{
					Value1 = Sql.DateAdd(Sql.DateParts.Day, 3, Sql.CurrentTimestamp),
					Value2 = Sql.DateAdd(Sql.DateParts.Day, 4, Sql.CurrentTimestamp)
				});

				var queryUnion = values1.Union(values2);

				var query = from t in db.GetTable<SampleClass>()
					select new
					{
						t,
						subQuery = queryUnion.FirstOrDefault()
					};

				var result = query.ToArray();
			}
		}

		[Test]
		public void JoinTest([DataSources(TestProvName.AllAccess)] string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (new AllowMultipleQuery())
			using (db.CreateLocalTable(data))
			{
				var query = from t in db.GetTable<SampleClass>()
					from s in db.SelectQuery(() => new { Key = Sql.AsSql(1), SecondValue = Sql.AsSql(3)}).InnerJoin(s => s.Key == t.Id)
					select new
					{
						t,
						s
					};

				var actual = query.ToArray();

				var expectedQuery = from t in data
					from s in new []{ new { Key = 1, SecondValue = 3}}.Where(s => s.Key == t.Id)
					select new
					{
						t,
						s
					};

				var expected = expectedQuery.ToArray();

				//TODO: Enable when merging new CompareBuilder
				//AreEqual(expected, actual);
			}
		}

		[Test]
		public void JoinScalarTest([DataSources(TestProvName.AllAccess)] string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (new AllowMultipleQuery())
			using (db.CreateLocalTable(data))
			{
				var query = from t in db.GetTable<SampleClass>()
					from s in db.SelectQuery(() => Sql.AsSql(1)).InnerJoin(s => s == t.Id)
					select new
					{
						t,
						s
					};

				var actual = query.ToArray();

				var expectedQuery = from t in data
					from s in new[] { 1 }.Where(s => s == t.Id)
					select new
					{
						t,
						s
					};

				var expected = expectedQuery.ToArray();

				//TODO: Enable when merging new CompareBuilder
				//AreEqual(expected, actual);
			}
		}


		private static SampleClass[] GenerateData()
		{
			return Enumerable.Range(1, 1).Select(i => new SampleClass() { Id = i, Value = i * 100 }).ToArray();
		}

		[Test]
		public void FirstTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var values = db.SelectQuery(() => new
				{
					Value1 = Sql.DateAdd(Sql.DateParts.Day, 1, Sql.CurrentTimestamp),
					Value2 = Sql.DateAdd(Sql.DateParts.Day, 2, Sql.CurrentTimestamp)
				}).First();
			}
		}

		[Test]
		public void TestAliasesCollision([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var sql = db.Child.Where(child => child.ChildID == -1).ToString();
				Assert.That(sql, Does.Contain("child_1"));
			}
		}

	}
}
