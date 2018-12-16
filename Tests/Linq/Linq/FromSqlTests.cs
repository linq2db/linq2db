using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

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
		public void TestFormattable([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestFormattable2([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestFormattableSameParam([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestFormattableInExpr([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestFormattableInExpr2([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestParameters([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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
		public void TestParametersInExpr([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 1;

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
		}

		[Test]
		public void TestParametersInExpr2([DataSources(ProviderName.DB2, ProviderName.SapHana)] string context, [Values(14, 15)] int endId)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(GenerateTestData()))
			{
				int startId = 5;

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

		[Test]
		public void TestTableValueFunction(
			[IncludeDataSources(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)] string context, 
			[Values(0, 1)] int offset)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from t in db.Child
					from p in db.FromSql<Parent>($"GetParentByID({t.ParentID + offset})")
					select new
					{
						t,
						p
					};

				var expected =
					from t in db.Child
					from p in db.Parent.Where(p => p.ParentID == t.ParentID + offset)
					select new
					{
						t,
						p
					};

				AreEqual(expected, query);
			}
		}


		[Test]
		public void TestScalar(
			[IncludeDataSources(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.Throws<LinqToDBException>(() => db.FromSql<int>("select 1 as value").ToArray());
			}
		}

	}
}
