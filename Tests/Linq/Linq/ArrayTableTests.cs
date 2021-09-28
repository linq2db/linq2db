using System.Linq;

using LinqToDB;

using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class EnuemrableSourceTests : TestBase
	{
		[Test]
		public void ApplyJoinArray(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12)]
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
		public void InnerJoinArray([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] {"Janet", "Doe", "John", doe}.AsQueryable() on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] {"Janet", "Doe", "John", doe}.AsQueryable() on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray2([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[] {"Janet", "Doe", "John", doe} on p.LastName equals n
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[] {"Janet", "Doe", "John", doe} on p.LastName equals n
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray3([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
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
						join n in new[] {"Janet", "Doe", "John", doe} on p.LastName equals n
						select p;

					var result = q.ToList();

					var expected =
						from p in Person
						join n in new[] {"Janet", "Doe", "John", doe} on p.LastName equals n
						select p;

					AreEqual(expected, result);
				}
			}
		}

		[Test]
		public void InnerJoinArray4([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";
			var arr = new[] {"Janet", "Doe", "John", doe};

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
		public void InnerJoinArray5([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			var doe = "Doe";

			using (var db = GetDataContext(context))
			{
				var q =
					from n in new[] {"Janet", "Doe", "John", doe}.AsQueryable(db)
					join p in db.Person on n equals p.LastName
					select p;

				var result = q.ToList();
				var sql    = q.ToString();

				Assert.That(sql, Contains.Substring("JOIN"));

				var expected =
					from n in new[] {"Janet", "Doe", "John", doe}.AsQueryable()
					join p in Person on n equals p.LastName
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinArray6([DataSources(TestProvName.AllAccess, TestProvName.AllPostgreSQLLess10)] string context)
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
		public void InnerJoinArray6Postgres([IncludeDataSources(TestProvName.AllPostgreSQLLess10)]
			string context)
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
		public void ApplyJoinAnonymousClassArray([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					from n in (new[]
					{
						new { ID = 1, Name = "Janet", Sub = p.LastName },
						new { ID = 1, Name = "Doe",   Sub = p.LastName },
					}).Where(n => p.LastName == n.Name)
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					from n in (new[]
					{
						new { ID = 1, Name = "Janet", Sub = p.LastName },
						new { ID = 1, Name = "Doe",   Sub = p.LastName },
					}).Where(n => p.LastName == n.Name)
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void ApplyJoinClassArray([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL93Plus, TestProvName.AllOracle12)] string context)
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
		public void InnerJoinClassArray([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Person
					join n in new[]
					{
						new { ID = 1, Name = "Janet" },
						new { ID = 1, Name = "Doe" },
					} on p.LastName equals n.Name
					select p;

				var result = q.ToList();

				var expected =
					from p in Person
					join n in new[]
					{
						new { ID = 1, Name = "Janet" },
						new { ID = 1, Name = "Doe" },
					} on p.LastName equals n.Name
					select p;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void InnerJoinAnonymousClassRecords([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var records = new[]
				{
					new { ID = 1, Name = "Janet" },
					new { ID = 1, Name = "Doe" },
				};

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
		public void InnerJoinClassRecords([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var records = new Person[]
				{
					new() { ID = 1, FirstName = "Janet" },
					new() { ID = 2, FirstName = "Doe" },
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
		public void InnerJoinClassRecordsCache([DataSources(TestProvName.AllAccess, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var records = new Person[]
				{
					new() { ID = 1, FirstName = "Janet" },
					new() { ID = 2, FirstName = "Doe" },
				};

				var q =
					from p in db.Person
					join n in records on p equals n
					select p;

				var result = q.ToList();

				records = new Person[]
				{
					new() { ID = 3, FirstName = "Janet" },
					new() { ID = 4, FirstName = "Doe" },
				};

				var result2 = q.ToList();

				/*
				var expected =
					from p in Person
					join n in records on p.ID equals n.ID
					select p;
					*/

			}
		}

	}
}
