using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Async;
	using LinqToDB.Data;
	using Model;

	[TestFixture]
	public class DataContextTests : TestBase
	{
		[Test]
		public void TestContext([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
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
		public void TestContextToString([IncludeDataSources(TestProvName.AllSqlServer2008Plus, ProviderName.SapHana)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				Console.WriteLine(ctx.GetTable<Person>().ToString());

				var q =
					from s in ctx.GetTable<Person>()
					select s.FirstName;

				Console.WriteLine(q.ToString());
			}
		}

		[Test]
		public void Issue210([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.KeepConnectionAlive = true;
				ctx.KeepConnectionAlive = false;
			}
		}

		[Test]
		public void ProviderConnectionStringConstructorTest1([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				Assert.Throws(typeof(LinqToDBException), () => new DataContext("BAD", db.ConnectionString));
			}

		}
		[Test]
		public void ProviderConnectionStringConstructorTest2([DataSources(false)] string context)
		{
			using (var db  = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(db.DataProvider.Name, "BAD"))
			{
				Assert.Throws(typeof(ArgumentException), () => db1.GetTable<Child>().ToList());
			}
		}

		[Test]
		[ActiveIssue("Unstable issue with Sybase vs Sybase.Managed DataProvider.Name", Configuration = TestProvName.AllSybase)]
		public void ProviderConnectionStringConstructorTest3([DataSources(false)] string context)
		{
			using (var db  = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(db.DataProvider.Name, db.ConnectionString))
			{
				Assert.AreEqual(db.DataProvider.Name, db1.DataProvider.Name);
				Assert.AreEqual(db.ConnectionString , db1.ConnectionString);

				AreEqual(
					db .GetTable<Child>().OrderBy(_ => _.ChildID).ToList(),
					db1.GetTable<Child>().OrderBy(_ => _.ChildID).ToList());
			}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public void LoopTest([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataContext(context))
				for (int i = 0; i < 1000; i++)
				{
					var items1 = db.GetTable<Child>().ToArray();
				}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public async Task LoopTestAsync([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataContext(context))
				for (int i = 0; i < 1000; i++)
				{
					var items1 = await db.GetTable<Child>().ToArrayAsync();
				}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public void LoopTestMultipleContexts([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
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
		public async Task LoopTestMultipleContextsAsync([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			for (int i = 0; i < 1000; i++)
			{
				using (var db = new DataContext(context))
				{
					var items1 = await db.GetTable<Child>().ToArrayAsync();
				}
			}
		}

		[Test]
		public void CommandTimeoutTests([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataContext(context))
			{

				db.CommandTimeout = 10;
				var dataConnection = db.GetDataConnection();
				Assert.That(dataConnection.CommandTimeout, Is.EqualTo(10));

				db.CommandTimeout = -10;
				Assert.That(dataConnection.CommandTimeout, Is.EqualTo(-1));

				db.CommandTimeout = 11;
				var record = db.GetTable<Child>().First();

				dataConnection = db.GetDataConnection();
				Assert.That(dataConnection.CommandTimeout, Is.EqualTo(11));
			}
		}

		[Test]
		public void TestCreateConnection([DataSources(false)] string context)
		{
			using (var db = new NewDataContext(context))
			{
				Assert.AreEqual(0, db.CreateCalled);
				using (db.GetDataConnection())
				{
					Assert.AreEqual(1, db.CreateCalled);
					using (db.GetDataConnection())
					{
						Assert.AreEqual(1, db.CreateCalled);
					}
				}
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

						using (db.GetDataConnection())
						{
							using (((IDataContext)db).Clone(true))
								Assert.AreEqual(db.IsMarsEnabled ? 1 : 0, db.CloneCalled);
						}
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

			protected override DataConnection CreateDataConnection()
			{
				CreateCalled++;
				return base.CreateDataConnection();
			}

			protected override DataConnection CloneDataConnection(DataConnection currentConnection, IAsyncDbTransaction dbTransaction, IAsyncDbConnection dbConnection)
			{
				CloneCalled++;
				return base.CloneDataConnection(currentConnection, dbTransaction, dbConnection);
			}
		}

	}
}
