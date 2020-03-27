using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DataContextTests : TestBase
	{
		[Test]
		public void TestContext([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
		{
			var ctx = new DataContext(context);

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

		[Test]
		public void TestContextToString([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana)] string context)
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

		// Access and SAP HANA ODBC provider detectors use connection string sniffing
		[Test]
		public void ProviderConnectionStringConstructorTest1([DataSources(false, ProviderName.Access, ProviderName.SapHanaOdbc)] string context)
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
			var db = new DataContext(context);
			for (int i = 0; i < 1000; i++)
			{
				var items1 = db.GetTable<Child>().ToArray();
			}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public async Task LoopTestAsync([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			var db = new DataContext(context);
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
				var db     = new DataContext(context);
				var items1 = db.GetTable<Child>().ToArray();
			}
		}

		// sdanyliv: Disabled other providers for performance purposes
		[Test]
		public async Task LoopTestMultipleContextsAsync([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			for (int i = 0; i < 1000; i++)
			{
				var db     = new DataContext(context);
				var items1 = await db.GetTable<Child>().ToArrayAsync();
			}
		}

		[Test]
		public void CommandTimeoutTests([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			var db = new DataContext(context);

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
}
