using System;
using System.Linq;
using System.Runtime.CompilerServices;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
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

		private static string Fix(string sql, string context)
		{
			if (context.Contains("DB2") || context.Contains("SapHana"))
				return sql.Replace("sample_class", "\"sample_class\"").Replace(" id ", " \"id\" ");
			return sql;
		}

		private static FormattableString FixF(FormattableString sql, string context)
		{
			return FormattableStringFactory.Create(Fix(sql.Format, context), sql.GetArguments());
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

				var sql = FixF($"SELECT * FROM sample_class where id >= {startId} and id < {endId}", context);

				var query = db.FromSql<SampleClass>(sql);
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

				var sql = FixF($"SELECT * FROM sample_class where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}", context);

				var query = db.FromSql<SampleClass>(sql);
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
				var sql = FixF($"SELECT * FROM sample_class where id >= {startIdParam} and id < {endId}", context);

				var query = db.FromSql<SampleClass>(sql);
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

				var sql = FixF($"SELECT * FROM sample_class where id >= {startId} and id < {endId}", context);

				var query = 
					from t in table 
					from s in db.FromSql<SampleClass>(sql).InnerJoin(s => s.Id == t.Id)
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

				var sql = FixF($"SELECT * FROM sample_class where id >= {new DataParameter("startId", startId, DataType.Int64)} and id < {endId}", context);

				var query = 
					from t in table 
					from s in db.FromSql<SampleClass>(sql).InnerJoin(s => s.Id == t.Id)
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

				var sql = Fix("SELECT * FROM\nsample_class\nwhere id >= {0} and id < {1}", context);

				var query = db.FromSql<SampleClass>(sql, new DataParameter("startId", startId, DataType.Int64), endId);
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
					var sql = Fix("SELECT * FROM sample_class where id >= {0} and id < {1}", context);

					var query =
						from t in table
						from s in db.FromSql<SampleClass>(sql,
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

				var sql = Fix("SELECT * FROM sample_class where id >= {0} and id < {1}", context);

				var query =
					from t in table 
					from s in db.FromSql<SampleClass>(sql, parameters).InnerJoin(s => s.Id == t.Id)
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
			[IncludeDataSources(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var query =
					from t in db.Child
					from p in db.FromSql<Parent>($"GetParentByID({t.ParentID})")
					select new
					{
						t,
						p
					};

				var expected =
					from t in db.Child
					from p in db.Parent.Where(p => p.ParentID == t.ParentID)
					select new
					{
						t,
						p
					};

				AreEqual(expected, query);
			}
		}

	}
}
