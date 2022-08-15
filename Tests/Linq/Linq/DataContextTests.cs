using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Async;
	using LinqToDB.Configuration;
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
		public void ProviderConnectionStringConstructorTest1([DataSources(false, ProviderName.Access, ProviderName.SapHanaOdbc)] string context)
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
				NUnitAssert.ThrowsAny(() => db1.GetTable<Child>().ToList(), typeof(ArgumentException), typeof(InvalidOperationException));
			}
		}

		[Test]
		[ActiveIssue("Provider detector picks managed provider as we don't have separate provider name for native Sybase provider", Configuration = ProviderName.Sybase)]
		public void ProviderConnectionStringConstructorTest3([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(db.DataProvider.Name, db.ConnectionString!))
			{
				Assert.AreEqual(db.DataProvider.Name, db1.DataProvider.Name);
				Assert.AreEqual(db.ConnectionString, db1.ConnectionString);

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

		class TestDataContext: DataContext
		{
			public TestDataContext(string context)
				: base(context)
			{
			}

			public DataConnection? DataConnection { get; private set; }

			protected override DataConnection CreateDataConnection(LinqToDBConnectionOptions options)
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
				Assert.Null(db.DataConnection);
				db.GetTable<Person>().ToList();
				Assert.NotNull(db.DataConnection);
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
				Assert.AreEqual(0, db.CreateCalled);

				db.KeepConnectionAlive = true;
				db.GetTable<Person>().ToList();
				Assert.AreEqual(1, db.CreateCalled);
				db.GetTable<Person>().ToList();
				Assert.AreEqual(1, db.CreateCalled);
				db.KeepConnectionAlive = false;
				db.GetTable<Person>().ToList();
				Assert.AreEqual(2, db.CreateCalled);
			}
		}

		[Test]
		public void TestCloneConnection([DataSources(false)] string context)
		{
			using (var db = new NewDataContext(context))
			{
				Assert.AreEqual(0, db.CloneCalled);
				using (new NewDataContext(context))
				{
					using (((IDataContext)db).Clone(true))
					{
						Assert.False(db.IsMarsEnabled);
						Assert.AreEqual(0, db.CloneCalled);

						// create and preserve underlying dataconnection
						db.KeepConnectionAlive = true;
						db.GetTable<Person>().ToList();

						using (((IDataContext)db).Clone(true))
							Assert.AreEqual(db.IsMarsEnabled ? 1 : 0, db.CloneCalled);
					}
				}
			}
		}

		class NewDataContext : DataContext
		{
			public NewDataContext(string context)
				: base(context)
			{
			}

			public int CreateCalled;
			public int CloneCalled;

			protected override DataConnection CreateDataConnection(LinqToDBConnectionOptions options)
			{
				CreateCalled++;
				return base.CreateDataConnection(options);
			}

			protected override DataConnection CloneDataConnection(DataConnection currentConnection, LinqToDBConnectionOptions options)
			{
				CloneCalled++;
				return base.CloneDataConnection(currentConnection, options);
			}
		}

	}
}
