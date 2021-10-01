using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class EnumerableSourceTests : TestBase
	{
		[Test]
		public void ApplyJoinArray(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus,
				TestProvName.AllOracle12)]
			string context)
		{
			var doe = "Doe";
			using (var db = GetDataContext(context))
			{
				for (var i = 0; i < 2; i++)
				{
					if (i > 0)
						doe += i;

					var q =
						from p in db.Person
						from n in new[] { p.FirstName, p.LastName, "John", doe }
						select n;

					var result = q.ToList();

					var expected =
						from p in Person
						from n in new[] { p.FirstName, p.LastName, "John", doe }
						select n;

					AreEqual(expected, result);
				}
			}
		}

		[Test]
		public void InnerJoinArray(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] { "Janet", "Doe", "John", doe }.AsQueryable() on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] { "Janet", "Doe", "John", doe }.AsQueryable() on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray2(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] { "Janet", "Doe", "John", doe } on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] { "Janet", "Doe", "John", doe } on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray3(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";

			using (var db = GetDataContext(context))
			{
				for (var i = 0; i < 2; i++)
				{
					if (i > 0)
						doe += i;

					var q =
						from p in db.Person
						join n in new[] { "Janet", "Doe", "John", doe } on p.LastName equals n
						select p;

					var result = q.ToList();

					var expected =
						from p in Person
						join n in new[] { "Janet", "Doe", "John", doe } on p.LastName equals n
						select p;

					AreEqual(expected, result);
				}
			}
		}

		[Test]
		public void InnerJoinArray4(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			var arr = new[] { "Janet", "Doe", "John", doe };

			using (var db = GetDataContext(context))
			{
				for (var i = 0; i < 2; i++)
				{
					if (i > 0)
						arr[1] += i;

					var q =
						from p in db.Person
						join n in arr on p.LastName equals n
						select p;

					var result = q.ToList();

					var expected =
						from p in Person
						join n in arr on p.LastName equals n
						select p;

					AreEqual(expected, result);
				}
			}
		}

		[Test]
		public void InnerJoinArray5(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";

			using (var db = GetDataContext(context))
			{
				var q =
					from n in new[] { "Janet", "Doe", "John", doe }.AsQueryable(db)
					join p in db.Person on n equals p.LastName
					select p;

				var result = q.ToList();
				var sql    = q.ToString();

				Assert.That(sql, Contains.Substring("JOIN"));

				var expected =
					from n in new[] { "Janet", "Doe", "John", doe }.AsQueryable()
					join p in Person on n equals p.LastName
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray6(
			[DataSources(TestProvName.AllAccess, TestProvName.AllPostgreSQLLess10)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] { "Doe" } on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] { "Doe" } on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}

		[ActiveIssue("PosgreSql needs type for literals. We have to rewise literals generation.")]
		[Test]
		public void InnerJoinArray6Postgres([IncludeDataSources(TestProvName.AllPostgreSQLLess10)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] { "Doe" } on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] { "Doe" } on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}


		[Test]
		public void ApplyJoinAnonymousClassArray(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus,
				TestProvName.AllOracle12)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					from n in (new[]
					{
						new { ID = 1, Name = "Janet", Sub = p.LastName },
						new { ID = 1, Name = "Doe", Sub   = p.LastName },
					}).Where(n => p.LastName == n.Name)
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					from n in (new[]
					{
						new { ID = 1, Name = "Janet", Sub = p.LastName },
						new { ID = 1, Name = "Doe", Sub   = p.LastName },
					}).Where(n => p.LastName == n.Name)
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void ApplyJoinClassArray(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus,
				TestProvName.AllOracle12)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					from n in new Person[]
					{
						new() { ID = 1, LastName = "Janet", FirstName = p.FirstName },
						new() { ID = 2, LastName = "Doe", },
					}.Where(n => p.LastName == n.LastName)
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					from n in new Person[]
					{
						new() { ID = 1, LastName = "Janet", FirstName = p.FirstName },
						new() { ID = 2, LastName = "Doe", },
					}.Where(n => p.LastName == n.LastName)
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinClassArray(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] { new { ID = 1, Name = "Janet" }, new { ID = 1, Name = "Doe" }, } on p.LastName
						equals n.Name
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] { new { ID = 1, Name = "Janet" }, new { ID = 1, Name = "Doe" }, } on p.LastName
						equals n.Name
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinAnonymousClassRecords(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var records = new[] { new { ID = 1, Name = "Janet" }, new { ID = 1, Name = "Doe" }, };

				var q =
					from p in db.Person
					join n in records on p.LastName equals n.Name
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in records on p.LastName equals n.Name
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinClassRecords(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var records = new Person[]
				{
					new() { ID = 1, FirstName = "Janet" }, new() { ID = 2, FirstName = "Doe" },
				};

				var q =
					from p in db.Person
					join n in records on p equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in records on p.ID equals n.ID
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinClassRecordsCache(
			[DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context,
			[Values(1, 2)] int iteration)
		{
			using (var db = GetDataContext(context))
			{
				var records1 = new Person[]
				{
					new() { ID = 1 + iteration, FirstName = "Janet" },
					new() { ID = 2 + iteration, FirstName = "Doe" },
				};

				var cacheMiss = Query<Person>.CacheMissCount;

				var q1 =
					from p in db.Person
					join n in records1 on p.ID equals n.ID
					select p;

				var result1 = q1.ToList();

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);

				cacheMiss = Query<Person>.CacheMissCount;

				var records2 = new Person[]
				{
					new() { ID = 3 + iteration, FirstName = "Janet" },
					new() { ID = 4 + iteration, FirstName = "Doe" },
				};

				var q2 =
					from p in db.Person
					join n in records2 on p.ID equals n.ID
					select p;

				var result2 = q2.ToList();

				Query<Person>.CacheMissCount.Should().Be(cacheMiss);

				result2.Count.Should().NotBe(result1.Count);
			}
		}


		class TableToInsert
		{
			[PrimaryKey]
			public int     Id    { get; set; }
			[Column]
			public string? Value { get; set; }

			protected bool Equals(TableToInsert other)
			{
				return Id == other.Id && Value == other.Value;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj))
				{
					return false;
				}

				if (ReferenceEquals(this, obj))
				{
					return true;
				}

				if (obj.GetType() != this.GetType())
				{
					return false;
				}

				return Equals((TableToInsert)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Id * 397) ^ (Value != null ? Value.GetHashCode() : 0);
				}
			}
		}

		[Test]
		public void InsertTest([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context, [Values(1, 2)] int iteration)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableToInsert>())
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var records = new TableToInsert[]
				{
					new() { Id = 1 + iteration, Value = "Janet" },
					new() { Id = 2 + iteration, Value = "Doe" },
				};

				var queryToInsert =
					from r in records.AsQueryable(db)
					from t in table.LeftJoin(t => t.Id == r.Id)
					where t == null
					select r;

				table.Insert(queryToInsert).Should().Be(2);
				table.Insert(queryToInsert).Should().Be(0);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}

		[Test]
		public void UpdateTest(
			[DataSources(
				TestProvName.AllAccess, 
				ProviderName.DB2, 
				TestProvName.AllSybase, 
				ProviderName.SqlCe,
				TestProvName.AllInformix)]
			string context, [Values(1, 2)] int iteration)
		{
			var records = new TableToInsert[]
			{
				new() { Id = 1 + iteration, Value = "Janet" },
				new() { Id = 2 + iteration, Value = "Doe" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(records))
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var upadedValue = new TableToInsert[]
				{
					new() { Id = 1 + iteration, Value = "Janet Updated" },
					new() { Id = 2 + iteration, Value = "Doe Updated" },
				};

				var queryToUpdate =
					from t in table
					join r in upadedValue on t.Id equals r.Id
					select new { t, r };

				queryToUpdate.Set(u => u.t.Value, u => u.r.Value)
					.Update().Should().Be(2);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);

				AreEqual(table, upadedValue);
			}
		}

		[Test]
		public void DeleteTest([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllSybase, TestProvName.AllInformix)] string context, [Values(1, 2)] int iteration)
		{
			var records = new TableToInsert[]
			{
				new() { Id = 1 + iteration, Value = "Janet" },
				new() { Id = 2 + iteration, Value = "Doe" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(records))
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var deleteValue = new TableToInsert[]
				{
					new() { Id = 1 + iteration },
					new() { Id = 2 + iteration },
				};

				var queryToDelete =
					from t in table
					join r in deleteValue on t.Id equals r.Id
					select t;

				queryToDelete.Delete().Should().Be(2);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}

		[Test]
		public void CTETest([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(1, 2)] int iteration)
		{
			var records = new TableToInsert[]
			{
				new() { Id = 1 + iteration, Value = "Janet" },
				new() { Id = 2 + iteration, Value = "Doe" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(records))
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var queryToSelect =
					from r in records.AsQueryable(db).AsCte()
					from t in table.InnerJoin(t => t.Id == r.Id && t.Value == r.Value)
					select t;

				var result = queryToSelect.ToArray();

				AreEqual(table, result);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}

		[Test]
		public void EmptyValues([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllSybase, TestProvName.AllInformix)] string context, [Values(1, 2)] int iteration)
		{
			var records = Array.Empty<TableToInsert>();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableToInsert>())
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var queryToSelect =
					from t in table
					join r in records on new {t.Id, t.Value} equals new {r.Id, r.Value}
					select t;

				queryToSelect.ToArray().Should().HaveCount(0);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}


		[Test]
		public void SubQuery([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllSybase, TestProvName.AllInformix)] string context, [Values(1, 2)] int iteration)
		{
			var records = new TableToInsert[]
			{
				new() { Id = 1 + iteration, Value = "Janet" },
				new() { Id = 2 + iteration, Value = "Doe" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(records))
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var queryToSelect =
					from t in table
					where records.Any(r => t.Id == r.Id && t.Value == r.Value)
					select t;

				var result = queryToSelect.ToArray();

				AreEqual(table, result);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}

		[Test]
		public void EmptySubQuery([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllSybase, TestProvName.AllInformix)] string context, [Values(1, 2)] int iteration)
		{
			var records = Array.Empty<TableToInsert>();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TableToInsert>())
			{
				var cacheMiss = Query<Person>.CacheMissCount;

				var queryToSelect =
					from t in table
					where records.Any(r => t.Id == r.Id && t.Value == r.Value)
					select t;

				queryToSelect.ToArray().Should().HaveCount(0);

				if (iteration > 1)
					Query<Person>.CacheMissCount.Should().Be(cacheMiss);
			}
		}

	}
}
