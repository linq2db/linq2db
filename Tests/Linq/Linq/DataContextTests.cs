using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Async;
	using LinqToDB.Data;
	using Model;
	using Tools;

	[TestFixture]
	public class DataContextTests : TestBase
	{
		[Test]
		public void TestContext([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.GetTable<Person>().ToList();

				ctx.KeepConnectionAlive = true;

				ctx.GetTable<Person>().ToList();
				ctx.GetTable<Person>().ToList();

				ctx.KeepConnectionAlive = false;

				using (var tran = new DataContextTransaction(ctx))
				{
					ctx.GetTable<Person>().ToList();

					tran.BeginTransaction();

					ctx.GetTable<Person>().ToList();
					ctx.GetTable<Person>().ToList();

					tran.CommitTransaction();
				}
			}
		}

		[Test]
		public void TestContextToString([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				NUnit.Framework.TestContext.WriteLine(ctx.GetTable<Person>().ToString());

				var q =
					from s in ctx.GetTable<Person>()
					select s.FirstName;

				NUnit.Framework.TestContext.WriteLine(q.ToString());
			}
		}

		[Test]
		public void Issue210([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.KeepConnectionAlive = true;
				ctx.KeepConnectionAlive = false;
			}
		}

		// Access and SAP HANA ODBC provider detectors use connection string sniffing
		[Test]
		public void ProviderConnectionStringConstructorTest1([DataSources(false, TestProvName.AllAccess, ProviderName.SapHanaOdbc)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				Assert.Throws<LinqToDBException>(() => new DataContext("BAD", db.ConnectionString!));
			}

		}
		[Test]
		public void ProviderConnectionStringConstructorTest2([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(db.DataProvider.Name, "BAD"))
			{
				Assert.That(
					() => db1.GetTable<Child>().ToList(),
					Throws.TypeOf<ArgumentException>().Or.TypeOf<InvalidOperationException>());
			}
		}

		[Test]
		[ActiveIssue("Provider detector picks managed provider as we don't have separate provider name for native Sybase provider", Configuration = ProviderName.Sybase)]
		public void ProviderConnectionStringConstructorTest3([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(db.DataProvider.Name, db.ConnectionString!))
			{
				Assert.Multiple(() =>
				{
					Assert.That(db1.DataProvider.Name, Is.EqualTo(db.DataProvider.Name));
					Assert.That(db1.ConnectionString, Is.EqualTo(db.ConnectionString));
				});

				AreEqual(
					db.GetTable<Child>().OrderBy(_ => _.ChildID).ToList(),
					db1.GetTable<Child>().OrderBy(_ => _.ChildID).ToList());
			}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public void LoopTest([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
				for (int i = 0; i < 1000; i++)
				{
					var items1 = db.GetTable<Child>().ToArray();
				}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public async Task LoopTestAsync([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new DataContext(context))
				for (int i = 0; i < 1000; i++)
				{
					var items1 = await db.GetTable<Child>().ToArrayAsync();
				}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public void LoopTestMultipleContexts([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			for (int i = 0; i < 1000; i++)
			{
				using (var db = new DataContext(context))
				{
					var items1 = db.GetTable<Child>().ToArray();
				}
			}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public async Task LoopTestMultipleContextsAsync([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			for (int i = 0; i < 1000; i++)
			{
				using (var db = new DataContext(context))
				{
					var items1 = await db.GetTable<Child>().ToArrayAsync();
				}
			}
		}

		sealed class TestDataContext : DataContext
		{
			public TestDataContext(string context)
				: base(context)
			{
			}

			public DataConnection? DataConnection { get; private set; }

			protected override DataConnection CreateDataConnection(DataOptions options)
			{
				return DataConnection = base.CreateDataConnection(options);
			}
		}

		[Test]
		public void CommandTimeoutTests([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = new TestDataContext(context))
			{
				db.KeepConnectionAlive = true;
				db.CommandTimeout = 10;
				Assert.That(db.DataConnection, Is.Null);
				db.GetTable<Person>().ToList();
				Assert.That(db.DataConnection, Is.Not.Null);
				Assert.That(db.DataConnection!.CommandTimeout, Is.EqualTo(10));

				db.CommandTimeout = -10;
				Assert.That(db.DataConnection.CommandTimeout, Is.EqualTo(-1));

				db.CommandTimeout = 11;
				var record = db.GetTable<Child>().First();

				Assert.That(db.DataConnection!.CommandTimeout, Is.EqualTo(11));
			}
		}

		[Test]
		public void TestCreateConnection([DataSources(false)] string context)
		{
			using (var db = new NewDataContext(context))
			{
				Assert.That(db.CreateCalled, Is.EqualTo(0));

				db.KeepConnectionAlive = true;
				db.GetTable<Person>().ToList();
				Assert.That(db.CreateCalled, Is.EqualTo(1));
				db.GetTable<Person>().ToList();
				Assert.That(db.CreateCalled, Is.EqualTo(1));
				db.KeepConnectionAlive = false;
				db.GetTable<Person>().ToList();
				Assert.That(db.CreateCalled, Is.EqualTo(2));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/971")]
		public void CloseAfterUse_DataContext([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			// test we don't leak connections here

			// save connection to list to prevent it from being collected
			var dbs = new List<IDataContext>();

			for (var i = 0; i < 101; i++)
			{
				IDataContext db = GetDataContext(context);
				db.CloseAfterUse = true;
				dbs.Add(db);

				foreach (var x in db.GetTable<Person>()) { }
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/971")]
		public void CloseAfterUse_DataConnection([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			// test we don't leak connections here

			// save connection to list to prevent it from being collected
			var dbs = new List<IDataContext>();

			// default pool size for SqlClient is 100
			for (var i = 0; i < 101; i++)
			{
				IDataContext db = GetDataConnection(context);
				db.CloseAfterUse = true;
				dbs.Add(db);

				foreach (var x in db.GetTable<Person>()) { }
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/971")]
		public void DontCloseAfterUse_DataContext([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			// test we don't leak connections here

			// save connection to list to prevent it from being collected
			var dbs = new List<IDataContext>();

			Assert.That(() =>
			{
				for (var i = 0; i < 101; i++)
				{
					IDataContext db = GetDataContext(context);
					db.CloseAfterUse = false;
					dbs.Add(db);

					foreach (var x in db.GetTable<Person>()) { }
				}
			}, Throws.Exception);

			foreach (var db in dbs)
			{
				db.Dispose();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/971")]
		public void DontCloseAfterUse_DataConnection([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			// test we don't leak connections here

			// save connection to list to prevent it from being collected
			var dbs = new List<IDataContext>();

			Assert.That(() =>
			{
				for (var i = 0; i < 101; i++)
				{
					IDataContext db = GetDataConnection(context);
					db.CloseAfterUse = false;
					dbs.Add(db);

					foreach (var x in db.GetTable<Person>()) { }
				}
			}, Throws.Exception);

			foreach (var db in dbs)
			{
				db.Dispose();
			}
		}

		sealed class NewDataContext : DataContext
		{
			public NewDataContext(string context)
				: base(context)
			{
			}

			public int CreateCalled;

			protected override DataConnection CreateDataConnection(DataOptions options)
			{
				CreateCalled++;
				return base.CreateDataConnection(options);
			}
		}

	}
}
