using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DataContextTests : TestBase
	{
		sealed class EmptyDefaultSetingsScope : IDisposable
		{
			private readonly string? _oldValue;
#if !NETFRAMEWORK
			private readonly ILinqToDBSettings? _oldSettings;
#endif

			public EmptyDefaultSetingsScope()
			{
				_oldValue                           = DataConnection.DefaultConfiguration;
				DataConnection.DefaultConfiguration = null;

#if !NETFRAMEWORK
				// see TestConfiguration.cctor implementation:
				// netfx adds connections to DataConnection one-by-one
				// .net sets DefaultSettings instance
				// if we reset DefaultSettings, netfx will loose connection strings
				// We shouldn't reset it for netfx or change init implementation in TestConfiguration
				_oldSettings                   = DataConnection.DefaultSettings;
				DataConnection.DefaultSettings = null;
#endif
			}

			void IDisposable.Dispose()
			{
				DataConnection.DefaultConfiguration = _oldValue;
#if !NETFRAMEWORK
				DataConnection.DefaultSettings      = _oldSettings;
#endif
			}
		}

		[Test, NonParallelizable]
		public void TestNullConfiguration_Unset([Values] bool cleanDefault)
		{
			var connectionString = GetConnectionString(ProviderName.SQLiteClassic);

			using var scope = cleanDefault ? new EmptyDefaultSetingsScope() : null;

			using var db = new DataConnection(new DataOptions().UseSQLite(connectionString, SQLiteProvider.System));

			_ = db.GetTable<Person>().ToArray();
		}

		[Test, NonParallelizable]
		public void TestNullConfiguration_UnsetRemote([Values] bool cleanDefault)
		{
			if (TestConfiguration.DisableRemoteContext) Assert.Ignore("Remote context disabled");

			var connectionString = GetConnectionString(ProviderName.SQLiteClassic);

			using var scope = cleanDefault ? new EmptyDefaultSetingsScope() : null;

			using var db = GetServerContainer(DefaultTransport).CreateContext(
				(s, o) => o,
				(conf, ms) => new DataConnection(new DataOptions().UseSQLite(connectionString, SQLiteProvider.System)));

			_ = db.GetTable<Person>().ToArray();
		}

		[Test, NonParallelizable]
		public void TestNullConfiguration_SetNull([Values] bool cleanDefault)
		{
			var connectionString = GetConnectionString(ProviderName.SQLiteClassic);

			using var scope = cleanDefault ? new EmptyDefaultSetingsScope() : null;

			using var db = new DataConnection(new DataOptions().UseConfiguration(null).UseSQLite(connectionString, SQLiteProvider.System));

			_ = db.GetTable<Person>().ToArray();
		}

		[Test, NonParallelizable]
		public void TestNullConfiguration_SetNullRemote([Values] bool cleanDefault)
		{
			if (TestConfiguration.DisableRemoteContext) Assert.Ignore("Remote context disabled");

			var connectionString = GetConnectionString(ProviderName.SQLiteClassic);

			using var scope = cleanDefault ? new EmptyDefaultSetingsScope() : null;

			using var db = GetServerContainer(DefaultTransport).CreateContext(
				(s, o) => o,
				(conf, ms) => new DataConnection(new DataOptions().UseConfiguration(null).UseSQLite(connectionString, SQLiteProvider.System)));

			_ = db.GetTable<Person>().ToArray();
		}

		[Test]
		public void TestContext([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSapHana, TestProvName.AllClickHouse)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.GetTable<Person>().ToList();

				using (var _ = new KeepConnectionAliveScope(ctx))
				{
					ctx.GetTable<Person>().ToList();
					ctx.GetTable<Person>().ToList();
				}

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
				ctx.GetTable<Person>().ToArray();

				var q =
					from s in ctx.GetTable<Person>()
					select s.FirstName;

				q.ToArray();
			}
		}

		[Test]
		public void Issue210([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.SetKeepConnectionAlive(true);
				ctx.SetKeepConnectionAlive(false);
			}
		}

		// Access and SAP HANA ODBC provider detectors use connection string sniffing
		[Test]
		public void ProviderConnectionStringConstructorTest1([DataSources(false, TestProvName.AllAccess, ProviderName.SapHanaOdbc)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				Assert.Throws<LinqToDBException>(() => new DataContext(new DataOptions().UseConnectionString("BAD", db.ConnectionString!)));
			}

		}
		[Test]
		public void ProviderConnectionStringConstructorTest2([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			using (var db1 = new DataContext(new DataOptions().UseConnectionString(db.DataProvider.Name, "BAD")))
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
			using (var db1 = new DataContext(new DataOptions().UseConnectionString(db.DataProvider.Name, db.ConnectionString!)))
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db1.DataProvider.Name, Is.EqualTo(db.DataProvider.Name));
					Assert.That(db1.ConnectionString, Is.EqualTo(db.ConnectionString));
				}

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
			using var db = new TestDataContext(context);
			using var _ = new KeepConnectionAliveScope(db);

			db.CommandTimeout = 10;
			Assert.That(db.DataConnection, Is.Null);
			db.GetTable<Person>().ToList();
			Assert.That(db.DataConnection, Is.Not.Null);
			Assert.That(db.DataConnection!.CommandTimeout, Is.EqualTo(10));

			Assert.That(() => db.CommandTimeout = -10, Throws.InstanceOf<ArgumentOutOfRangeException>());

			db.ResetCommandTimeout();
			Assert.That(db.DataConnection.CommandTimeout, Is.EqualTo(-1));

			db.CommandTimeout = 11;
			var record = db.GetTable<Child>().First();

			Assert.That(db.DataConnection!.CommandTimeout, Is.EqualTo(11));
		}

		[Test]
		public void TestCreateConnection([DataSources(false)] string context)
		{
			using (var db = new NewDataContext(context))
			{
				Assert.That(db.CreateCalled, Is.Zero);

				using (var _ = new KeepConnectionAliveScope(db))
				{
					db.GetTable<Person>().ToList();
					Assert.That(db.CreateCalled, Is.EqualTo(1));
					db.GetTable<Person>().ToList();
					Assert.That(db.CreateCalled, Is.EqualTo(1));
				}

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
				IDataContext db = GetDataContext(context);
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4729")]
		public void Issue4729Test([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool closeAfterUse)
		{
			var interceptor = new CountingContextInterceptor();
			using var db = GetDataContext(context, o => o.UseInterceptor(interceptor));
			((IDataContext)db).CloseAfterUse = closeAfterUse;

			db.Query<int>("SELECT 1").SingleOrDefault();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(interceptor.OnClosedCount, Is.EqualTo(closeAfterUse ? 1 : 0));
				Assert.That(interceptor.OnClosedAsyncCount, Is.Zero);
			}
		}

	}
}
