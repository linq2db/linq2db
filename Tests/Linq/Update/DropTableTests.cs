using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class DropTableTests : TestBase
	{
		class DropTableTest
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

				table.Insert(() => new DropTableTest() { ID = 123 });

				var data = table.ToList();

				Assert.NotNull(data);
				Assert.AreEqual(1, data.Count);
				Assert.AreEqual(123, data[0].ID);

				table.Drop();

				// check that table dropped
				var exception = Assert.Catch(() => table.ToList());
				Assert.True(exception is Exception);
			}
		}

		[Test]
		public void DropSpecificDatabaseTableTest([DataSources(false, ProviderName.SapHana)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				// cleanup
				db.DropTable<DropTableTest>(throwExceptionIfNotExists: false);

				var schema = TestUtils.GetSchemaName(db);
				var database = TestUtils.GetDatabaseName(db);

				var table = db.CreateTable<DropTableTest>()
					.SchemaName(schema)
					.DatabaseName(database);

				table.Insert(() => new DropTableTest() { ID = 123 });

				var data = table.ToList();

				Assert.NotNull(data);
				Assert.AreEqual(1, data.Count);
				Assert.AreEqual(123, data[0].ID);

				table.Drop();

				var sql = db.LastQuery;

				// check that table dropped
				var exception = Assert.Catch(() => table.ToList());
				Assert.True(exception is Exception);

				// TODO: we need better assertion here
				// Right now we just check generated sql query, not that it is
				// executed properly as we use only one test database
				if (database != TestUtils.NO_DATABASE_NAME)
					Assert.True(sql.Contains(database));

				if (schema != TestUtils.NO_SCHEMA_NAME)
				    Assert.True(sql.Contains(schema));
			}
		}
	}
}
