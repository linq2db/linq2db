﻿using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FromSqlTests : TestBase
	{
		[Table(Name = "sample_class")]
		class SampleClass
		{
			[Column("id")] 
			public int Id    { get; set; }

			[Column("value", Length = 50)] 
			public string Value { get; set; }
		}

		static SampleClass[] GenerateTestData()
		{
			return Enumerable.Range(1, 20).Select(i => new SampleClass {Id = i, Value = "Str_" + i}).ToArray();
		}

#if !NET45
		[Test]
		public void TestFormattable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var query = db.FromSql<SampleClass>($"SELECT * FROM sample_class where id >= {startId} and id < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var query = db.FromSql<SampleClass>($"SELECT * FROM sample_class where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}");
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattableSameParam([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var startIdParam = new DataParameter("startId", startId, DataType.Int64);
				var query = db.FromSql<SampleClass>($"SELECT * FROM sample_class where id >= {startIdParam} and id < {endId}");
				var queryWithProjection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id });

				var projection = queryWithProjection.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestFormattableInExpr([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var query = 
					from t in table 
					from s in db.FromSql<SampleClass>($"SELECT * FROM sample_class where id >= {startId} and id < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}	
		
		[Test]
		public void TestFormattableInExpr2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var query = 
					from t in table 
					from s in db.FromSql<SampleClass>($"SELECT * FROM sample_class where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}").InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

#endif

		[Test]
		public void TestParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var query = db.FromSql<SampleClass>("SELECT * FROM\nsample_class\nwhere id >= {0} and id < {1}", new DataParameter("startId", startId, DataType.Int64), endId);
				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

		[Test]
		public void TestParametersInExpr([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				void LocalTest(int startId, int endId)
				{
					var query =
						from t in table
						from s in db.FromSql<SampleClass>("SELECT * FROM sample_class where id >= {0} and id < {1}",
							new DataParameter("startId", startId, DataType.Int64), endId).InnerJoin(s => s.Id == t.Id)
						select s;

					var projection = query
						.Where(c => c.Id > 10)
						.Select(c => new { c.Value, c.Id })
						.ToArray();

					var expected = table
						.Where(t => t.Id >= startId && t.Id < endId)
						.Where(c => c.Id > 10)
						.Select(c => new { c.Value, c.Id })
						.ToArray();

					Assert.AreEqual(expected, projection);
				}

				LocalTest(5, 15);
				LocalTest(1, 6);

			}
		}

		[Test]
		public void TestParametersInExpr2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;
				int endId   = 15;

				var parameters = new object[] { new DataParameter("startId", startId, DataType.Int64), endId };

				var query =
					from t in table 
					from s in db.FromSql<SampleClass>("SELECT * FROM sample_class where id >= {0} and id < {1}", parameters).InnerJoin(s => s.Id == t.Id)
					select s;

				var projection = query
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				var expected = table
					.Where(t => t.Id >= startId && t.Id < endId)
					.Where(c => c.Id > 10)
					.Select(c => new { c.Value, c.Id })
					.ToArray();

				Assert.AreEqual(expected, projection);
			}
		}

	}
}
