using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class DropTableTests : TestBase
	{
		sealed class DropTableTest
		{
			public int ID { get; set; }
		}

		[Test]
		public void DropCurrentDatabaseTableTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// cleanup
				db.DropTable<DropTableTest>(throwExceptionIfNotExists: false);

				var table = db.CreateTable<DropTableTest>();

				table.Insert(() => new DropTableTest { ID = 123 });

				var data = table.ToList();

				table.Drop();

				Assert.That(data, Is.Not.Null);
				Assert.That(data, Has.Count.EqualTo(1));
				Assert.That(data[0].ID, Is.EqualTo(123));

				// check that table dropped
				var exception = Assert.Catch(() => table.ToList());
				Assert.That(exception, Is.Not.Null);
			}
		}

		sealed class DropTableTestID
		{
			[Identity, PrimaryKey]
			public int ID  { get; set; }
			public int ID1 { get; set; }
		}

		[Test]
		public void DropCurrentDatabaseTableWithIdentityTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// cleanup
				db.DropTable<DropTableTestID>(throwExceptionIfNotExists: false);
				db.Close();

				var table = db.CreateTable<DropTableTestID>();

				table.Insert(() => new DropTableTestID { ID1 = 2 });

				var data = table.Select(t => new { t.ID, t.ID1 }).ToList();

				table.Drop();

				Assert.That(data, Is.EquivalentTo(new[]
				{
					new { ID = 1, ID1 = 2 }
				}));

				// check that table dropped
				var exception = Assert.Catch(() => table.ToList());
				Assert.That(exception, Is.Not.Null);
			}
		}

		[Test]
		public void DropSpecificDatabaseTableTest([DataSources(false, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				// cleanup
				db.DropTable<DropTableTest>(throwExceptionIfNotExists: false);

				var schema = TestUtils.GetSchemaName(db, context);
				var database = TestUtils.GetDatabaseName(db, context);

				// no idea why, but Access ODBC needs database set in CREATE TABLE for INSERT to work
				// still it doesn't distinguish CREATE TABLE with and without database name
				var table = db.CreateTable<DropTableTest>(databaseName: context.IsAnyOf(TestProvName.AllAccessOdbc) ? database : null)
					.SchemaName(schema)
					.DatabaseName(database);

				table.Insert(() => new DropTableTest() { ID = 123 });

				var data = table.ToList();

				Assert.That(data, Is.Not.Null);
				Assert.That(data, Has.Count.EqualTo(1));
				Assert.That(data[0].ID, Is.EqualTo(123));

				table.Drop();

				var sql = db.LastQuery!;

				// check that table dropped
				var exception = Assert.Catch(() => table.ToList());
				Assert.That(exception is Exception, Is.True);

				// TODO: we need better assertion here
				// Right now we just check generated sql query, not that it is
				// executed properly as we use only one test database
				if (database != TestUtils.NO_DATABASE_NAME)
					Assert.That(sql, Does.Contain(database));

				if (schema != TestUtils.NO_SCHEMA_NAME)
					Assert.That(sql, Does.Contain(schema));
			}
		}

		[Table]
		sealed class Table
		{
			[Column] public int ID { get; set; }
		}

		// ! don't use it for other tests
		sealed class NotTable
		{
			public int ID { get; set; }
		}

#pragma warning disable CA1064 // Exceptions should be public
		sealed class CustomException() : Exception("You shall not pass!")
#pragma warning restore CA1064 // Exceptions should be public
		{
		}

		[Test]
		public void DropTable_Existing([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.CreateTable<Table>();
			db.DropTable<Table>(throwExceptionIfNotExists: true);
		}

		[Test]
		public void DropTable_Missing_Ignore([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.DropTable<Table>(throwExceptionIfNotExists: false);
		}

		[Test]
		public void DropTable_Missing_Fail([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(() => db.DropTable<Table>(throwExceptionIfNotExists: true), Throws.InstanceOf<Exception>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/798")]
		public void DropTable_Fail_NotFromExistCheck_EarlyError([DataSources] string context, [Values] bool throwIfNotExists)
		{
			using var db = GetDataContext(context, o => o.UseOnEntityDescriptorCreated((_, _) => throw new CustomException()));

			Assert.That(() => db.DropTable<NotTable>(throwExceptionIfNotExists: throwIfNotExists), Throws.InstanceOf<CustomException>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/798")]
		public void DropTable_Fail_NotFromExistCheck_LateError([DataSources] string context, [Values] bool throwIfNotExists)
		{
			using var db = GetDataContext(context, o => o.UseConnectionString("BAD").UseOnEntityDescriptorCreated((_, _) => throw new CustomException()));

			Assert.That(() => db.DropTable<NotTable>(throwExceptionIfNotExists: throwIfNotExists), Throws.InstanceOf<CustomException>());
		}

		[Test]
		public void Drop_Existing([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var t = db.CreateTable<Table>();
			t.Drop(throwExceptionIfNotExists: true);
		}

		[Test]
		public void Drop_Missing_Ignore([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.GetTable<Table>().Drop(throwExceptionIfNotExists: false);
		}

		[Test]
		public void Drop_Missing_Fail([DataSources] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(() => db.GetTable<Table>().Drop(throwExceptionIfNotExists: true), Throws.InstanceOf<Exception>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/798")]
		public void Drop_Fail_NotFromExistCheck_EarlyError([DataSources] string context, [Values] bool throwIfNotExists)
		{
			using var db = GetDataContext(context, o => o.UseOnEntityDescriptorCreated((_, _) => throw new CustomException()));

			Assert.That(() => db.GetTable<NotTable>().Drop(throwExceptionIfNotExists: throwIfNotExists), Throws.InstanceOf<CustomException>());
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/798")]
		public void Drop_Fail_NotFromExistCheck_LateError([DataSources] string context, [Values] bool throwIfNotExists)
		{
			using var db = GetDataContext(context, o => o.UseConnectionString("BAD").UseOnEntityDescriptorCreated((_, _) => throw new CustomException()));

			Assert.That(() => db.GetTable<NotTable>().Drop(throwExceptionIfNotExists: throwIfNotExists), Throws.InstanceOf<CustomException>());
		}
	}
}
