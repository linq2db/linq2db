using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.Data
{
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Transactions;
	using LinqToDB.Data.RetryPolicy;
	using LinqToDB.Mapping;
	using Model;

	[TestFixture]
	public class DataConnectionTests : TestBase
	{
		[Test]
		public void Test1([NorthwindDataContext] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider = DataConnection.GetDataProvider(context);

			using (var conn = new DataConnection(dataProvider, connectionString))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.Null);
			}
		}

		[Test]
		public void Test2()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(DataConnection.DefaultConfiguration));
			}
		}

		[Test]
		public void Test3([IncludeDataSources(
			ProviderName.SqlServer,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2008 + ".1",
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2005 + ".1",
			TestProvName.AllAccess)]
			string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(context));

				if (context.EndsWith(".2005"))
				{
					var sdp = conn.DataProvider;
					Assert.That(sdp.Name, Is.EqualTo("SqlServer.2005"));
				}

				if (context.EndsWith(".2008"))
				{
					var sdp = conn.DataProvider;
					Assert.That(sdp.Name, Is.EqualTo("SqlServer.2008"));
				}
			}
		}

		[Test]
		public void EnumExecuteScalarTest()
		{
			using (var dbm = new DataConnection())
			{
				var gender = dbm.Execute<Gender>("select 'M'");

				Assert.That(gender, Is.EqualTo(Gender.Male));
			}
		}

		[Test]
		public void CloneTest([DataSources(false)] string context)
		{
			using (var con = new DataConnection(context))
			{
				var dbName = con.Connection.Database;

				for (var i = 0; i < 150; i++)
					using (var clone = (DataConnection)con.Clone())
						dbName = clone.Connection.Database;
			}
		}

		[Test]
		public void GetDataProviderTest([IncludeDataSources(ProviderName.DB2, TestProvName.AllSqlServer2005Plus)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);

			IDataProvider dataProvider;

			switch (context)
			{
				case ProviderName.DB2:
				{
					dataProvider = DataConnection.GetDataProvider("DB2", connectionString)!;

					Assert.That(dataProvider, Is.TypeOf<DB2DataProvider>());

					var sqlServerDataProvider = (DB2DataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(DB2Version.LUW));

					break;
				}

				case ProviderName.SqlServer2005:
				{
					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", "MyConfig.2005", connectionString)!;

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2005));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2005));

					break;
				}

				case ProviderName.SqlServer2008:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer", connectionString)!;

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2008));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2008));

					break;
				}

				case ProviderName.SqlServer2012:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer.2012", connectionString)!;

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					break;
				}

				case ProviderName.SqlServer2014:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer", "SqlServer.2012", connectionString)!;

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					break;
				}

				case ProviderName.SqlServer2017:
					{
						dataProvider = DataConnection.GetDataProvider("SqlServer", "SqlServer.2017", connectionString)!;

						Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

						var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

						Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2017));

						dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
						sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

						Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2017));

						break;
					}
			}
		}

		[Test]
		public void TestOpenEvent()
		{
			var opened = false;
			var openedAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnConnectionOpened += (dc, cn) => opened = true;
				conn.OnConnectionOpenedAsync += async (dc, cn, token) => await Task.Run(() => openedAsync = true);
				Assert.False(opened);
				Assert.False(openedAsync);
				Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
				Assert.True(opened);
				Assert.False(openedAsync);
			}
		}

		[Test]
		public async Task TestAsyncOpenEvent()
		{
			var opened = false;
			var openedAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnConnectionOpened += (dc, cn) => opened = true;
				conn.OnConnectionOpenedAsync += async (dc, cn, token) => await Task.Run(() => openedAsync = true);
				Assert.False(opened);
				Assert.False(openedAsync);
				await conn.SelectAsync(() => 1);
				Assert.False(opened);
				Assert.True(openedAsync);
			}
		}

		[Test]
		public void TestOpenEventWithoutHandlers()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
			}
		}

		[Test]
		public async Task TestAsyncOpenEventWithoutHandlers()
		{
			using (var conn = new DataConnection())
			{
				await conn.SelectAsync(() => 1);
			}
		}

		// informix connection limits interfere with test
		[Test]
		[ActiveIssue("Fails due to connection limit for development version when run with nonmanaged provider", Configuration = ProviderName.SybaseManaged)]
		public void MultipleConnectionsTest([DataSources(TestProvName.AllInformix)] string context)
		{
			var exceptions = new ConcurrentBag<Exception>();

			var threads = Enumerable
				.Range(1, 10)
				.Select(n => new Thread(() =>
				{
					try
					{
						using (var db = GetDataContext(context))
							db.Parent.ToList();
					}
					catch (Exception e)
					{
						exceptions.Add(e);
					}
				}))
				.ToArray();

			foreach (var thread in threads) thread.Start();
			foreach (var thread in threads) thread.Join();

			if (exceptions.Count > 0)
				throw new AggregateException(exceptions);
		}

		[Test]
		public async Task DataConnectionCloseAsync([DataSources(false)] string context)
		{
			var db = new DataConnection(context);

			try
			{
				await db.GetTable<Parent>().ToListAsync();
			}
			finally
			{
				var tid = Thread.CurrentThread.ManagedThreadId;

				await db.CloseAsync();

				db.Dispose();

				if (tid == Thread.CurrentThread.ManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionDisposeAsync([DataSources(false)] string context)
		{
			var db = new DataConnection(context);

			try
			{
				await db.GetTable<Parent>().ToListAsync();
			}
			finally
			{
				var tid = Thread.CurrentThread.ManagedThreadId;

				await db.DisposeAsync();

				if (tid == Thread.CurrentThread.ManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public void TestOnBeforeConnectionOpenEvent()
		{
			var open = false;
			var openAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnBeforeConnectionOpen += (dc, cn) =>
				{
					if (cn.State == ConnectionState.Closed)
						open = true;
				};
				conn.OnBeforeConnectionOpenAsync += async (dc, cn, token) => await Task.Run(() =>
				{
					if (cn.State == ConnectionState.Closed)
						openAsync = true;
				});
				Assert.False(open);
				Assert.False(openAsync);
				Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
				Assert.True(open);
				Assert.False(openAsync);
			}
		}

		[Test]
		public async Task TestAsyncOnBeforeConnectionOpenEvent()
		{
			var open = false;
			var openAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnBeforeConnectionOpen += (dc, cn) =>
					{
						if (cn.State == ConnectionState.Closed)
							open = true;
					};
				conn.OnBeforeConnectionOpenAsync += async (dc, cn, token) => await Task.Run(() => 
						{
							if (cn.State == ConnectionState.Closed)
								openAsync = true;
						});
				Assert.False(open);
				Assert.False(openAsync);
				await conn.SelectAsync(() => 1);
				Assert.False(open);
				Assert.True(openAsync);
			}
		}

		[Test]
		[SkipCI]
		public void CommandTimeoutTest([IncludeDataSources(ProviderName.SqlServer2014)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var forUpdate = db.Person.First();
				db.QueryHints.Add("WAITFOR DELAY '00:01';");
				var start = DateTimeOffset.Now;
				try
				{
					db.Update(forUpdate);
				}
				catch { }
				finally
				{
					var time = DateTimeOffset.Now - start;
					Assert.True(time >= TimeSpan.FromSeconds(30));
					Assert.True(time < TimeSpan.FromSeconds(32));
				}

				start = DateTimeOffset.Now;
				try
				{
					db.CommandTimeout = 10;
					db.Update(forUpdate);
				}
				catch { }
				finally
				{
					var time = DateTimeOffset.Now - start;
					Assert.True(time >= TimeSpan.FromSeconds(10));
					Assert.True(time < TimeSpan.FromSeconds(12));
				}

				start = DateTimeOffset.Now;
				db.CommandTimeout = 0;
				db.Update(forUpdate);
				var time2 = DateTimeOffset.Now - start;
				Assert.True(time2 >= TimeSpan.FromSeconds(60));
				Assert.True(time2 < TimeSpan.FromSeconds(62));
			}
		}

		[Test]
		public void TestCloneOnEntityCreated([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var size = db.GetTable<Person>().ToList().Count;

				var counter = 0;

				db.GetTable<Person>().ToList();
				Assert.AreEqual(0, counter);

				db.OnEntityCreated = OnCreated;

				db.GetTable<Person>().ToList();
				Assert.AreEqual(size, counter);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					// tests different clone execution branches for MARS-enabled and disabled connections
					counter = 0;
					cdb.GetTable<Person>().ToList();
					Assert.AreEqual(size, counter);

					db.OnEntityCreated = null;

					counter = 0;
					db.GetTable<Person>().ToList();
					Assert.AreEqual(0, counter);

					// because we:
					// - don't track cloned connections
					// - clonned connections are used internally, so this scenario is not possible for linq2db itself
					cdb.GetTable<Person>().ToList();
					Assert.AreEqual(size, counter);
				}

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					counter = 0;
					cdb.GetTable<Person>().ToList();

					Assert.AreEqual(0, counter);
				}

				void OnCreated(EntityCreatedEventArgs args) => counter++;
			}
		}

		class TestRetryPolicy : IRetryPolicy
		{
			TResult IRetryPolicy.Execute<TResult>(Func<TResult> operation) => operation();
			void IRetryPolicy.Execute(Action operation) => operation();
			Task<TResult> IRetryPolicy.ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken) => operation(cancellationToken);
			Task IRetryPolicy.ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken) => operation(cancellationToken);
		}

		[Test]
		public void TestCloneCommandTimeout([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.AreEqual(-1, db.CommandTimeout);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(-1, cdb.CommandTimeout);
				}

				db.CommandTimeout = 0;

				Assert.AreEqual(0, db.CommandTimeout);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, cdb.CommandTimeout);
				}

				db.CommandTimeout = 10;

				Assert.AreEqual(10, db.CommandTimeout);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(10, cdb.CommandTimeout);
				}

				db.CommandTimeout = -5;
				Assert.AreEqual(-1, db.CommandTimeout);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(-1, cdb.CommandTimeout);
				}
			}
		}

		[Test]
		public void TestCloneInlineParameters([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.False(db.InlineParameters);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.False(cdb.InlineParameters);
				}

				db.InlineParameters = true;

				Assert.True(db.InlineParameters);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.True(cdb.InlineParameters);
				}

				db.InlineParameters = false;
				Assert.False(db.InlineParameters);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.False(cdb.InlineParameters);
				}
			}
		}

		[Test]
		public void TestCloneQueryHints([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.AreEqual(0, db.QueryHints.Count);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, cdb.QueryHints.Count);
				}

				db.QueryHints.Add("test");

				Assert.AreEqual(1, db.QueryHints.Count);
				Assert.AreEqual("test", db.QueryHints[0]);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(1, cdb.QueryHints.Count);
					Assert.AreEqual("test", cdb.QueryHints[0]);

					db.QueryHints.Clear();

					Assert.AreEqual(1, cdb.QueryHints.Count);
					Assert.AreEqual("test", cdb.QueryHints[0]);
				}

				Assert.AreEqual(0, db.QueryHints.Count);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, cdb.QueryHints.Count);
				}
			}
		}

		[Test]
		public void TestCloneThrowOnDisposed([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.IsNull(db.ThrowOnDisposed);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.IsNull(cdb.ThrowOnDisposed);
				}

				db.ThrowOnDisposed = false;

				Assert.False(db.ThrowOnDisposed);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.False(cdb.ThrowOnDisposed);
				}

				db.ThrowOnDisposed = true;

				Assert.True(db.ThrowOnDisposed);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.True(cdb.ThrowOnDisposed);
				}

				db.ThrowOnDisposed = null;
				Assert.IsNull(db.ThrowOnDisposed);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.IsNull(cdb.ThrowOnDisposed);
				}
			}
		}

		[Test]
		public void TestCloneOnTraceConnection([DataSources(false)] string context)
		{
			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;
				Action<TraceInfo> onTrace = OnTrace;

				Assert.AreEqual(DataConnection.OnTrace, db.OnTraceConnection);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(DataConnection.OnTrace, cdb.OnTraceConnection);
				}

				db.OnTraceConnection = onTrace;

				Assert.AreEqual(onTrace, db.OnTraceConnection);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(onTrace, cdb.OnTraceConnection);
				}

				db.OnTraceConnection = null;

				Assert.IsNull(db.OnTraceConnection);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.IsNull(cdb.OnTraceConnection);
				}

				db.OnTraceConnection = DataConnection.OnTrace;

				Assert.AreEqual(DataConnection.OnTrace, db.OnTraceConnection);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(DataConnection.OnTrace, cdb.OnTraceConnection);
				}
			}

			void OnTrace(TraceInfo ti) { };
		}

		[Test]
		public void TestCloneOnClosingOnClosed([DataSources(false)] string context)
		{
			var closing = 0;
			var closed  = 0;

			using (var db = new DataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.AreEqual(0, closing);
				Assert.AreEqual(0, closed);
				db.Close();
				Assert.AreEqual(0, closing);
				Assert.AreEqual(0, closed);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					_ = cdb.Connection;
					Assert.AreEqual(0, closing);
					Assert.AreEqual(0, closed);
					cdb.Close();
					Assert.AreEqual(0, closing);
					Assert.AreEqual(0, closed);
				}

				_ = db.Connection;
				db.OnClosing += OnClosing;
				db.OnClosed += OnClosed;
				Assert.AreEqual(0, closing);
				Assert.AreEqual(0, closed);
				db.Close();
				Assert.AreEqual(1, closing);
				Assert.AreEqual(1, closed);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					closing = 0;
					closed  = 0;
					_ = cdb.Connection;
					Assert.AreEqual(0, closing);
					Assert.AreEqual(0, closed);
					cdb.Close();
					Assert.AreEqual(1, closing);
					Assert.AreEqual(1, closed);

					closing = 0;
					closed  = 0;
					db.OnClosing -= OnClosing;
					db.OnClosed  -= OnClosed;
					_ = cdb.Connection;
					cdb.Close();
					Assert.AreEqual(1, closing);
					Assert.AreEqual(1, closed);
				}

				closing = 0;
				closed  = 0;
				_ = db.Connection;
				Assert.AreEqual(0, closing);
				Assert.AreEqual(0, closed);
				db.Close();
				Assert.AreEqual(0, closing);
				Assert.AreEqual(0, closed);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					_ = cdb.Connection;
					Assert.AreEqual(0, closing);
					Assert.AreEqual(0, closed);
					cdb.Close();
					Assert.AreEqual(0, closing);
					Assert.AreEqual(0, closed);
				}
			}

			void OnClosing(object sender, EventArgs e) => closing++;
			void OnClosed(object sender, EventArgs e) => closed++;
		}

		[Test]
		public void TestCloneOnBeforeConnectionOpenOnConnectionOpened([DataSources(false)] string context)
		{
			var open   = 0;
			var opened = 0;

			using (var db = new DataConnection(context))
			{
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				var _ = db.Connection;
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					_ = cdb.Connection;
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
				}

				db.Close();
				db.OnBeforeConnectionOpen += OnBeforeConnectionOpen;
				db.OnConnectionOpened     += OnConnectionOpened;
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				_ = db.Connection;
				Assert.AreEqual(1, open);
				Assert.AreEqual(1, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					open   = 0;
					opened = 0;
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					cdb.Connection.Close();
					open   = 0;
					opened = 0;
					_ = cdb.Connection;
					Assert.AreEqual(1, open);
					Assert.AreEqual(1, opened);

					open   = 0;
					opened = 0;
					cdb.Close();
					db.OnBeforeConnectionOpen -= OnBeforeConnectionOpen;
					db.OnConnectionOpened     -= OnConnectionOpened;
					_ = cdb.Connection;
					Assert.AreEqual(1, open);
					Assert.AreEqual(1, opened);
				}

				open   = 0;
				opened = 0;
				db.Close();
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				_ = db.Connection;
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					_ = cdb.Connection;
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
				}
			}

			void OnBeforeConnectionOpen(DataConnection dc, IDbConnection cn) => open++;
			void OnConnectionOpened    (DataConnection dc, IDbConnection cn) => opened++;
		}

		[Test]
		public async Task TestCloneOnBeforeConnectionOpenAsyncOnConnectionOpenedAsync([DataSources(false)] string context)
		{
			var open   = 0;
			var opened = 0;

			using (var db = new DataConnection(context))
			{
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				await db.EnsureConnectionAsync();
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					await db.EnsureConnectionAsync();
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
				}

				db.Close();
				db.OnBeforeConnectionOpenAsync += OnBeforeConnectionOpenAsync;
				db.OnConnectionOpenedAsync     += OnConnectionOpenedAsync;
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				await db.EnsureConnectionAsync();
				Assert.AreEqual(1, open);
				Assert.AreEqual(1, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					open   = 0;
					opened = 0;
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					cdb.Connection.Close();
					open   = 0;
					opened = 0;
					await cdb.EnsureConnectionAsync();
					Assert.AreEqual(1, open);
					Assert.AreEqual(1, opened);

					open   = 0;
					opened = 0;
					cdb.Close();
					db.OnBeforeConnectionOpenAsync -= OnBeforeConnectionOpenAsync;
					db.OnConnectionOpenedAsync     -= OnConnectionOpenedAsync;
					await cdb.EnsureConnectionAsync();
					Assert.AreEqual(1, open);
					Assert.AreEqual(1, opened);
				}

				open   = 0;
				opened = 0;
				db.Close();
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);
				await db.EnsureConnectionAsync();
				Assert.AreEqual(0, open);
				Assert.AreEqual(0, opened);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					await cdb.EnsureConnectionAsync();
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
				}
			}

			Task OnBeforeConnectionOpenAsync(DataConnection dc, IDbConnection cn, CancellationToken ct)
			{
				open++;
				return Task.CompletedTask;
			}

			Task OnConnectionOpenedAsync(DataConnection dc, IDbConnection cn, CancellationToken ct)
			{
				opened++;
				return Task.CompletedTask;
		}
		}

		// strange provider errors, review in v3 with more recent providers
		// also some providers remove credentials from connection string in non-design mode
		[ActiveIssue(Configurations = new[] 
		{ 
			ProviderName.MySqlConnector,
			ProviderName.SapHana,
			// Providers remove credentials in non-design mode:
			TestProvName.AllPostgreSQL,
			TestProvName.AllSqlServer,
			TestProvName.AllMySqlData
		})]
		[Test]
		public void TestDisposeFlagCloning([DataSources(false)] string context, [Values] bool dispose)
		{
			using (var db = new DataConnection(context))
			{
				var cn = db.Connection;
				using (var testDb = new DataConnection(db.DataProvider, cn, dispose))
				{
					Assert.AreEqual(ConnectionState.Open, cn.State);

					IDbConnection? clonedConnection = null;
					using (var clonedDb = (DataConnection)((IDataContext)testDb).Clone(true))
					{
						clonedConnection = clonedDb.Connection;

						// fails in v2 for MARS-enabled connections, already fixed in v3
						Assert.AreEqual(db.IsMarsEnabled, testDb.IsMarsEnabled);

						if (testDb.IsMarsEnabled)
						{
							// connection reused
							Assert.AreEqual(cn, clonedConnection);
							Assert.AreEqual(ConnectionState.Open, cn.State);
						}
						else
						{
							Assert.AreNotEqual(cn, clonedConnection);
							Assert.AreEqual(ConnectionState.Open, cn.State);
							Assert.AreEqual(ConnectionState.Open, clonedConnection.State);
						}
					}

					if (testDb.IsMarsEnabled)
					{
						// cloned DC doesn't dispose parent connection
						Assert.AreEqual(ConnectionState.Open, cn.State);
					}
					else
					{
						// cloned DC dispose own connection
						Assert.AreEqual(ConnectionState.Open, cn.State);
						try
						{
							Assert.AreEqual(ConnectionState.Closed, clonedConnection.State);
						}
						catch (ObjectDisposedException)
						{
							// API consistency FTW!
						}
					}
				}
			}
		}

		#region issue 962
		[Table("Categories")]
		public class Category
		{
			[PrimaryKey, Identity] public int     CategoryID;
			[Column, NotNull]      public string  CategoryName = null!;
			[Column]               public string? Description;

			[Association(ThisKey = "CategoryID", OtherKey = "CategoryID")]
			public List<Product> Products = null!;

			public static readonly Category[] Data = new[]
			{
				new Category() { CategoryID = 1, CategoryName = "Name 1", Description = "Desc 1" },
				new Category() { CategoryID = 2, CategoryName = "Name 2", Description = "Desc 2" },
			};
		}

		[Table(Name = "Products")]
		public class Product
		{
			[PrimaryKey, Identity]                                         public int       ProductID;
			[Column, NotNull]                                              public string    ProductName = null!;
			[Column]                                                       public int?      CategoryID;
			[Column]                                                       public string?   QuantityPerUnit;
			[Association(ThisKey = "CategoryID", OtherKey = "CategoryID")] public Category? Category;

			public static readonly Product[] Data = new[]
			{
				new Product() { ProductID = 1, ProductName = "Prod 1", CategoryID = 1, QuantityPerUnit = "q 1" },
				new Product() { ProductID = 2, ProductName = "Prod 2", CategoryID = 1, QuantityPerUnit = "q 2" },
				new Product() { ProductID = 3, ProductName = "Prod 3", CategoryID = 3, QuantityPerUnit = "q 3" },
				new Product() { ProductID = 4, ProductName = "Prod 4", CategoryID = 3, QuantityPerUnit = "q 4" },
				new Product() { ProductID = 5, ProductName = "Prod 5", CategoryID = 1, QuantityPerUnit = "q 5" },
				new Product() { ProductID = 6, ProductName = "Prod 6", CategoryID = 1, QuantityPerUnit = "q 6" },
			};
		}

		[Test]
		public void TestDisposeFlagCloning962Test1(
			[DataSources(false)] string context, [Values] bool withScope)
		{
			if (withScope && (
				context == ProviderName.DB2            ||
				context == ProviderName.InformixDB2    ||
				context == ProviderName.MySqlConnector ||
				context == ProviderName.SapHanaNative  ||
				context == ProviderName.SqlCe          ||
				context == ProviderName.Sybase         ||
				context.Contains("Firebird")           ||
				context.Contains("Oracle")             ||
				context.Contains("PostgreSQL")         ||
				context.Contains("SqlServer")          ||
				context.Contains("SqlAzure")           ||
				context.Contains(ProviderName.SQLiteClassic)
				))
			{
				// DB2: ERROR [58005] [IBM][DB2.NET] SQL0902 An unexpected exception has occurred in  Process: 22188 Thread 16 AppDomain: Name:domain-1b9769ae-linq2db.Tests.dll
				// Firebird: SQL error code = -204 Table unknown CATEGORIES
				// Informix DB2: ERROR [2E000] [IBM] SQL1001N  "<DBNAME>" is not a valid database name.  SQLSTATE=2E000
				// MySqlConnector: XAER_RMFAIL: The command cannot be executed when global transaction is in the  ACTIVE state
				// Oracle: Connection is already part of a local or a distributed transaction
				// PostgreSQL: Nested/Concurrent transactions aren't supported.
				// SQLite.Classic: No transaction is active on this connection
				// SAP HANA native: The rollback was caused by an unspecified reason: XA Transaction is rolled back.
				// SQL Server: Cannot drop the table 'Categories', because it does not exist or you do not have permission.
				// SQLCE: SqlCeConnection does not support nested transactions.
				// Sybase native: just crashes without details (as usual for this "provider")
				Assert.Inconclusive("Provider not configured or has issues with TransactionScope or doesn't support DDL in distributed transactions");
			}

			TransactionScope? scope = withScope ? new TransactionScope() : null;
			try
			{
				using (new AllowMultipleQuery())
				using (var db = new DataConnection(context))
				using (db.CreateLocalTable(Category.Data))
				using (db.CreateLocalTable(Product.Data))
				{
					var categoryDtos = db.GetTable<Category>().LoadWith(c => c.Products).ToList();

					scope?.Dispose();
					scope = null;
				}
			}
			finally
			{
				scope?.Dispose();
			}
		}

		[Test]
		public void TestDisposeFlagCloning962Test2(
			[DataSources(false)] string context, [Values] bool withScope)
		{
			if (withScope && (
				context == ProviderName.DB2                 ||
				context == ProviderName.InformixDB2         ||
				context == ProviderName.SapHanaOdbc         ||
				context == ProviderName.SqlCe               ||
				context == ProviderName.Sybase              ||
#if NETCOREAPP2_1
				(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && context == ProviderName.OracleManaged) ||
#endif
				TestProvName.AllMySqlData.Contains(context) ||
				context.StartsWith("Access")                ||
				context.Contains("SqlServer")               ||
				context.Contains("SqlAzure")                ||
				context.Contains("PostgreSQL")              ||
				context.Contains(ProviderName.SQLiteClassic)
				))
			{
				// Access: The ITransactionLocal interface is not supported by the 'Microsoft.Jet.OLEDB.4.0' provider.  Local transactions are unavailable with the current provider.
				// Access>ODBC: ERROR [HY092] [Microsoft][ODBC Microsoft Access Driver]Invalid attribute/option identifier
				// DB2: ERROR [58005] [IBM][DB2/NT64] SQL0998N  Error occurred during transaction or heuristic processing.  Reason Code = "16". Subcode = "2-8004D026".
				// Informix DB2: ERROR [2E000] [IBM] SQL1001N  "<DBNAME>" is not a valid database name.  SQLSTATE=2E000
				// MySql.Data: Multiple simultaneous connections or connections with different connection strings inside the same transaction are not currently supported.
				// PostgreSQL: 55000: prepared transactions are disabled
				// SQLite.Classic: The operation is not valid for the state of the transaction.
				// SAP HANA ODBC: ERROR [HYC00] [SAP AG][LIBODBCHDB32 DLL] Optional feature not implemented
				// SQLCE: The connection object can not be enlisted in transaction scope.
				// Sybase native: Only One Local connection allowed in the TransactionScope
				// Oracle managed: Operation is not supported on this platform.
				// SqlServer: The operation is not valid for the state of the transaction.
				Assert.Inconclusive("Provider not configured or has issues with TransactionScope");
			}

			TransactionScope? scope = withScope ? new TransactionScope() : null;
			try
			{
				using (new AllowMultipleQuery())
				using (var db = new DataConnection(context))
				{
					// test cloned data connection without LoadWith, as it doesn't use cloning in v3
					db.Select(() => "test1");
					using (var cdb = ((IDataContext)db).Clone(true))
					{
						cdb.Select(() => "test2");

						scope?.Complete();
					}
				}
			}
			finally
			{
				scope?.Dispose();
			}
		}
#endregion

	}
}
