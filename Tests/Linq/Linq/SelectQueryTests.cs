using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectQueryTests : TestBase
	{
		[Table]
		sealed class SampleClass
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void SubQueryTest([DataSources(TestProvName.AllAccess)] string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
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
				var query = db.Child.Where(child => child.ChildID == -1);
				query.ToArray();
				var sql = query.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("child_1"));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4284")]
		public void Select_GroupBy_SelectAgain([DataSources(ProviderName.Firebird25, TestProvName.AllAccess, TestProvName.AllSqlServer2017, ProviderName.SqlCe, TestProvName.AllMySql57, TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.GroupBy(g => g.LastName)
					.Select(group => new
					{
						LastName         = group.Key,
						Count            = group.Count(),
						HighestFirstName = group.Max(x => x.FirstName)
					})
					.Where(summary => summary.Count > 5)
					.Skip(1).Take(1)
					.Select(x => new { Count = Sql.Ext.Count().Over().ToValue(), Value = x });

				query.ToArray();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2494")]
		public void Issue2494Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue2494Table>();

			var query = db.SelectQuery(() => tb.Any()
				? db.Insert(new Issue2494Table() { Value = 1 }, null, null, null, null, default)
				: 0);

			var res = query.ToArray();
			Assert.That(res[0], Is.Zero);
			res = query.ToArray();
			Assert.That(res[0], Is.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2494")]
		public void Issue2494Test2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue2494Table>();

			var query = db.SelectQuery(() => !tb.Any()
				? db.Insert(new Issue2494Table() { Value = 1 }, null, null, null, null, default)
				: 0);

			var res = query.ToArray();
			Assert.That(res[0], Is.EqualTo(1));
			res = query.ToArray();
			Assert.That(res[0], Is.Zero);
		}

		[Table]
		sealed class Issue2494Table
		{
			[Column] public int Value { get; set; }
		}

		#region Issue 2779
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test1([DataSources(false,
			TestProvName.AllAccess,
			TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllDB2
			)] string context)
		{
			using var db = GetDataConnection(context);

			var res = db.FromSqlScalar<int>($"SELECT 1 as value").ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0], Is.EqualTo(1));
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test2([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);

			var res = db.FromSql<int>("SELECT 1").ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0], Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test3([DataSources(false, TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllOracle21Minus, TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataConnection(context);

			var res = db.Query<int>("SELECT 1").ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0], Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test4([DataSources(false,
			TestProvName.AllAccess,
			TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSapHana,
			TestProvName.AllDB2
			)] string context)
		{
			using var db = GetDataConnection(context);

			var res = (from x in db.Person
					  where db.FromSqlScalar<int>($"SELECT 1 as value").Contains(x.ID)
					  select x).ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0].ID, Is.EqualTo(1));
		}
		#endregion
	}
}
