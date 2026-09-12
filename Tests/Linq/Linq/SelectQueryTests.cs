using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class SelectQueryTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[ActiveIssue(Configuration = TestProvName.AllInformix, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "Non-numeric character in datetime or interval.",
			Details = "no-issue: Informix interval cannot be created from non-literal value")]
		[Test]
		public void UnionTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();
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

		[ActiveIssue(Configuration = TestProvName.AllInformix, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "Non-numeric character in datetime or interval.",
			Details = "no-issue: Informix interval cannot be created from non-literal value")]
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
		public void SubQueryAggregate([DataSources(TestProvName.AllAccess)]
			string context)
		{
			using var db = GetDataContext(context);

			var result = db.SelectQuery(() => new
			{
				Parents  = db.Parent.Count(),
				Children = db.Child.Count()
			}).Single();

			result.Parents.ShouldBe(Parent.Count());
			result.Children.ShouldBe(Child.Count());
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
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();
			var values = db.SelectQuery(() => new
			{
				Value1 = Sql.DateAdd(Sql.DateParts.Day, 1, Sql.CurrentTimestamp),
				Value2 = Sql.DateAdd(Sql.DateParts.Day, 2, Sql.CurrentTimestamp)
			}).First();
		}

		[Test]
		public void TestAliasesCollision([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Child.Where(child => child.ChildID == -1);
			query.ToArray();
			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Does.Contain("child_1"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4284")]
		public void Select_GroupBy_SelectAgain([DataSources(ProviderName.Firebird25, TestProvName.AllAccess, TestProvName.AllSqlServer2017, ProviderName.SqlCe, TestProvName.AllMySql57, TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
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
			using var db = GetDataContext(context);

			var res = db.FromSqlScalar<int>($"SELECT 1 as value").ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0], Is.EqualTo(1));
		}

		// FromSql<int>("SELECT 1") emits SELECT [t1].[value] FROM (SELECT 1) [t1], naming a column the inner
		// constant never had. Servers that reject it do so at their own stage, so the gate is grouped by that.
		// DuckDB, SqlServer.2022 and PostgreSQL.16 accept it and are deliberately not gated.
		[ActiveIssue(2779, Configuration = TestProvName.AllSQLite, ErrorMessage = "no such column: t1.value",
			Details = "One fragment covers both drivers: System.Data.SQLite reports 'SQL logic error' with this on the next line, Microsoft.Data.Sqlite as 'SQLite Error 1'. Verified on Classic, MS, MPU and MPM.")]
		[ActiveIssue(2779, Configuration = ProviderName.SqlCe,
			ErrorMessage = "Column names must be specified for constants, expressions or aggregate functions when they occur in a FROM sub query.")]
		[ActiveIssue(2779, Configuration = TestProvName.AllClickHouse,
			ErrorMessage = "Identifier 't1.value' cannot be resolved from subquery with name t1",
			Details = "All three drivers surface the same server text under three different exception types, so the message alone is the portable declaration.")]
		[ActiveIssue(2779, Configuration = TestProvName.AllYdb, ErrorTypeName = "Ydb.Sdk.Ado.YdbException",
			ErrorMessage = "Column value is not in source column set")]
		[ActiveIssue(2779, Configurations = [TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllOracle,
			TestProvName.AllFirebird, TestProvName.AllMySql57, ProviderName.DB2],
			Details = "no-declaration: these still fail. Sybase, Informix and the 8.0/MariaDB MySQL servers were dropped once CI exercised them - only 5.7 is left of the MySQL family.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test2([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			var res = db.FromSql<int>("SELECT 1").ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0], Is.EqualTo(1));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2779")]
		public void Issue2779Test3([DataSources(false, TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllOracle21Minus, TestProvName.AllSapHana)] string context)
		{
			using var db = GetDataContext(context);

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
			using var db = GetDataContext(context);

			var res = (from x in db.Person
					  where db.FromSqlScalar<int>($"SELECT 1 as value").Contains(x.ID)
					  select x).ToArray();

			Assert.That(res, Has.Length.EqualTo(1));
			Assert.That(res[0].ID, Is.EqualTo(1));
		}
		#endregion

	}
}
