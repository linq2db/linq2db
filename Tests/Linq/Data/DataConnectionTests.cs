﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;

	[TestFixture]
	public class DataConnectionTests : TestBase
	{
		[Test]
		public void Test1([IncludeDataSources(TestProvName.AllSQLite)] string context)
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
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2005,
			TestProvName.AllAccess)]
			string context)
		{
			using (var conn = GetDataConnection(context))
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
			using (var con = GetDataConnection(context))
			{
				var dbName = con.Connection.Database;

				for (var i = 0; i < 150; i++)
					using (var clone = (DataConnection)con.Clone())
						dbName = clone.Connection.Database;
			}
		}

		[Test]
		public void GetDataProviderTest([IncludeDataSources(ProviderName.DB2, TestProvName.AllSqlServer)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);

			IDataProvider dataProvider;

			switch (context)
			{
				case ProviderName.DB2:
				{
					dataProvider = DataConnection.GetDataProvider("DB2", connectionString)!;

					Assert.That(dataProvider, Is.InstanceOf<DB2DataProvider>());

					var sqlServerDataProvider = (DB2DataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(DB2Version.LUW));

					break;
				}

				case ProviderName.SqlServer2005:
				{
					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", "MyConfig.2005", connectionString)!;

					Assert.That(dataProvider, Is.InstanceOf<SqlServerDataProvider>());

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

					Assert.That(dataProvider, Is.InstanceOf<SqlServerDataProvider>());

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

					Assert.That(dataProvider, Is.InstanceOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					break;
				}

				case ProviderName.SqlServer2014:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer", "SqlServer.2014", connectionString)!;

					Assert.That(dataProvider, Is.InstanceOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2014));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2014));

					break;
				}

				case ProviderName.SqlServer2017:
					{
						dataProvider = DataConnection.GetDataProvider("SqlServer", "SqlServer.2017", connectionString)!;

						Assert.That(dataProvider, Is.InstanceOf<SqlServerDataProvider>());

						var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

						Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2017));

						dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString)!;
						sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

						Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2017));

						break;
					}
			}
		}

		private class TestConnectionInterceptor : ConnectionInterceptor
		{
			private readonly Action<ConnectionEventData, DbConnection>? _onConnectionOpening;
			private readonly Action<ConnectionEventData, DbConnection>? _onConnectionOpened;

			private readonly Func<ConnectionEventData, DbConnection, CancellationToken, Task>? _onConnectionOpeningAsync;
			private readonly Func<ConnectionEventData, DbConnection, CancellationToken, Task>?  _onConnectionOpenedAsync;

			public TestConnectionInterceptor(
				Action<ConnectionEventData, DbConnection>? onConnectionOpening,
				Action<ConnectionEventData, DbConnection>? onConnectionOpened,
				Func<ConnectionEventData, DbConnection, CancellationToken, Task>? onConnectionOpeningAsync,
				Func<ConnectionEventData, DbConnection, CancellationToken, Task>? onConnectionOpenedAsync)
			{
				_onConnectionOpening = onConnectionOpening;
				_onConnectionOpened  = onConnectionOpened;
				_onConnectionOpeningAsync = onConnectionOpeningAsync;
				_onConnectionOpenedAsync = onConnectionOpenedAsync;
			}

			public override void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
			{
				_onConnectionOpened?.Invoke(eventData, connection);
				base.ConnectionOpened(eventData, connection);
			}

			public override async Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				if (_onConnectionOpenedAsync != null)
					await _onConnectionOpenedAsync(eventData, connection, cancellationToken);

				await base.ConnectionOpenedAsync(eventData, connection, cancellationToken);
			}

			public override void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
			{
				_onConnectionOpening?.Invoke(eventData, connection);
				base.ConnectionOpening(eventData, connection);
			}

			public override async Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				if (_onConnectionOpeningAsync != null)
					await _onConnectionOpeningAsync(eventData, connection, cancellationToken);

				await base.ConnectionOpeningAsync(eventData, connection, cancellationToken);
			}
		}

		[Test]
		public void TestOpenEvent()
		{
			var opened = false;
			var openedAsync = false;
			using (var conn = new DataConnection())
			{
				conn.AddInterceptor(new TestConnectionInterceptor(
					null,
					(args, cn) => opened = true,
					null,
					async (args, cn, се) => await Task.Run(() => openedAsync = true)));

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
				conn.AddInterceptor(new TestConnectionInterceptor(
					null,
					(args, cn) => opened = true,
					null,
					async (args, cn, ct) => await Task.Run(() => openedAsync = true, ct)));

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

		[Test]
		public void TestServiceCollection1([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDB((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<IDataContext>()!;
			Assert.True(con is DataConnection);
			Assert.That(((DataConnection)con).ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestServiceCollection2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<DataConnection>((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<DataConnection>()!;
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		public class DbConnection1 : DataConnection
		{
			public DbConnection1(LinqToDBConnectionOptions options) : base(options)
			{
			}
		}

		public class DbConnection2 : DataConnection
		{
			public DbConnection2(LinqToDBConnectionOptions<DbConnection2> options) : base(options)
			{
			}
		}

		[Test]
		public void TestSettingsPerDb([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<DbConnection1>((provider, options) => options.UseConfigurationString(context));
			collection.AddLinqToDBContext<DbConnection2>((provider, options) => {});

			var serviceProvider = collection.BuildServiceProvider();
			var c1 = serviceProvider.GetService<DbConnection1>()!;
			var c2 = serviceProvider.GetService<DbConnection2>()!;
			Assert.That(c1.ConfigurationString, Is.EqualTo(context));
			Assert.That(c2.ConfigurationString, Is.EqualTo(DataConnection.DefaultConfiguration));
		}

		[Test]
		public void TestConstructorThrowsWhenGivenInvalidSettings()
		{
			Assert.Throws<LinqToDBException>(() => new DbConnection1(new LinqToDBConnectionOptionsBuilder().Build<DbConnection2>()));
		}

		// informix connection limits interfere with test
		[Test]
		[ActiveIssue("Fails due to connection limit for development version when run with nonmanaged provider", Configuration = ProviderName.SybaseManaged)]
		public void MultipleConnectionsTest([DataSources(TestProvName.AllInformix)] string context)
		{
			using var psr = new Tests.Remote.ServerContainer.PortStatusRestorer(_serverContainer, false);

			using (new DisableBaseline("Multi-threading"))
			{
				var exceptions = new ConcurrentStack<Exception>();

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
							exceptions.Push(e);
						}
					}))
					.ToArray();

				foreach (var thread in threads) thread.Start();
				foreach (var thread in threads) thread.Join();

				if (!exceptions.IsEmpty)
					throw new AggregateException(exceptions);
			}
		}

		[Test]
		public async Task DataConnectionCloseAsync([DataSources(false)] string context)
		{
			var db = GetDataConnection(context);

			try
			{
				await db.GetTable<Parent>().ToListAsync();
			}
			finally
			{
				var tid = Environment.CurrentManagedThreadId;

				await db.CloseAsync();

				db.Dispose();

				if (tid == Environment.CurrentManagedThreadId)
					Assert.Inconclusive("Executed synchronously due to lack of async support or there were no underlying async operations");
			}
		}

		[Test]
		public async Task DataConnectionDisposeAsync([DataSources(false)] string context)
		{
			var db = GetDataConnection(context);

			try
			{
				await db.GetTable<Parent>().ToListAsync();
			}
			finally
			{
				var tid = Environment.CurrentManagedThreadId;

				await db.DisposeAsync();

				if (tid == Environment.CurrentManagedThreadId)
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
				conn.AddInterceptor(new TestConnectionInterceptor(
					(args, cn) =>
				{
					if (cn.State == ConnectionState.Closed)
						open = true;
					},
					null,
					async (args, cn, ct) => await Task.Run(() =>
				{
					if (cn.State == ConnectionState.Closed)
						openAsync = true;
					}, ct),
					null));

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
				conn.AddInterceptor(new TestConnectionInterceptor(
					(args, cn) =>
					{
						if (cn.State == ConnectionState.Closed)
							open = true;
					},
					null,
					async (args, cn, ct) => await Task.Run(() =>
						{
							if (cn.State == ConnectionState.Closed)
								openAsync = true;
					}, ct),
					null));

				Assert.False(open);
				Assert.False(openAsync);
				await conn.SelectAsync(() => 1);
				Assert.False(open);
				Assert.True(openAsync);
			}
		}

		[Test]
		[SkipCI]
		public void CommandTimeoutTest([IncludeDataSources(TestProvName.AllSqlServer2014)] string context)
		{
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
			{
				var personsCount = db.GetTable<Person>().Count();

				_ = db.GetTable<Person>().ToList();

				var interceptor = new TestEntityServiceInterceptor();

				db.AddInterceptor(interceptor);

				_ = db.GetTable<Person>().ToList();
				Assert.AreEqual(personsCount, interceptor.EntityCreatedCallCounter);

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					interceptor.EntityCreatedCallCounter = 0;
					_ = cdb.GetTable<Person>().ToList();
					Assert.AreEqual(personsCount, interceptor.EntityCreatedCallCounter);

					interceptor.EntityCreatedCallCounter = 0;
					_ = db.GetTable<Person>().ToList();
					Assert.AreEqual(personsCount, interceptor.EntityCreatedCallCounter);

					_ = cdb.GetTable<Person>().ToList();
					Assert.AreEqual(personsCount * 2, interceptor.EntityCreatedCallCounter);
				}

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					interceptor.EntityCreatedCallCounter = 0;
					_ = cdb.GetTable<Person>().ToList();

					Assert.AreEqual(personsCount, interceptor.EntityCreatedCallCounter);
				}
			}
		}

		class TestEntityServiceInterceptor : EntityServiceInterceptor
		{
			public int EntityCreatedCallCounter { get; set; }
			public override object EntityCreated(EntityCreatedEventData eventData, object entity)
			{
				EntityCreatedCallCounter++;
				return base.EntityCreated(eventData, entity);
			}
		}

		class TestDataContextInterceptor : DataContextInterceptor
		{
			public int OnClosingCallCounter { get; set; }
			public int OnClosedCallCounter { get; set; }
			public int OnClosedAsyncCallCounter { get; set; }
			public int OnClosingAsyncCallCounter { get; set; }

			public override void OnClosing(DataContextEventData eventData)
			{
				OnClosingCallCounter++;
				base.OnClosing(eventData);
				}

			public override void OnClosed(DataContextEventData eventData)
				{
				OnClosedCallCounter++;
				base.OnClosed(eventData);
			}

			public override Task OnClosedAsync(DataContextEventData eventData)
			{
				OnClosedAsyncCallCounter++;
				return base.OnClosedAsync(eventData);
				}

			public override Task OnClosingAsync(DataContextEventData eventData)
			{
				OnClosingAsyncCallCounter++;
				return base.OnClosingAsync(eventData);
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
		[Obsolete("DataConnection.OnTrace")]
		public void TestCloneOnTraceConnection([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
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
			var interceptor = new TestDataContextInterceptor();

			using (var db = GetDataConnection(context))
			{
				// to enable MARS-enabled cloning branch
				var _ = db.Connection;

				Assert.AreEqual(0, interceptor.OnClosingCallCounter);
				Assert.AreEqual(0, interceptor.OnClosedCallCounter);
				db.Close();
				Assert.AreEqual(0, interceptor.OnClosingCallCounter);
				Assert.AreEqual(0, interceptor.OnClosedCallCounter);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					_ = cdb.Connection;
					Assert.AreEqual(0, interceptor.OnClosingCallCounter);
					Assert.AreEqual(0, interceptor.OnClosedCallCounter);
					cdb.Close();
					Assert.AreEqual(0, interceptor.OnClosingCallCounter);
					Assert.AreEqual(0, interceptor.OnClosedCallCounter);
				}

				_ = db.Connection;
				db.AddInterceptor(interceptor);
				Assert.AreEqual(0, interceptor.OnClosingCallCounter);
				Assert.AreEqual(0, interceptor.OnClosedCallCounter);
				db.Close();
				Assert.AreEqual(1, interceptor.OnClosingCallCounter);
				Assert.AreEqual(1, interceptor.OnClosedCallCounter);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					interceptor.OnClosingCallCounter = 0;
					interceptor.OnClosedCallCounter = 0;
					_ = cdb.Connection;
					Assert.AreEqual(0, interceptor.OnClosingCallCounter);
					Assert.AreEqual(0, interceptor.OnClosedCallCounter);
					cdb.Close();
					Assert.AreEqual(1, interceptor.OnClosingCallCounter);
					Assert.AreEqual(1, interceptor.OnClosedCallCounter);

					interceptor.OnClosingCallCounter = 0;
					interceptor.OnClosedCallCounter = 0;
					_ = cdb.Connection;
					cdb.Close();
					Assert.AreEqual(1, interceptor.OnClosingCallCounter);
					Assert.AreEqual(1, interceptor.OnClosedCallCounter);
				}

				interceptor.OnClosingCallCounter = 0;
				interceptor.OnClosedCallCounter = 0;
				_ = db.Connection;
				Assert.AreEqual(0, interceptor.OnClosingCallCounter);
				Assert.AreEqual(0, interceptor.OnClosedCallCounter);
				db.Close();
				Assert.AreEqual(1, interceptor.OnClosingCallCounter);
				Assert.AreEqual(1, interceptor.OnClosedCallCounter);
				_ = db.Connection;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					_ = cdb.Connection;
					Assert.AreEqual(1, interceptor.OnClosingCallCounter);
					Assert.AreEqual(1, interceptor.OnClosedCallCounter);
					cdb.Close();
					Assert.AreEqual(2, interceptor.OnClosingCallCounter);
					Assert.AreEqual(2, interceptor.OnClosedCallCounter);
				}
			}
		}

		[Test]
		public void TestCloneOnBeforeConnectionOpenOnConnectionOpened([DataSources(false)] string context)
		{
			var open   = 0;
			var opened = 0;

			using (var db = GetDataConnection(context))
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
				db.AddInterceptor(new TestConnectionInterceptor(
					(args, cn) => OnBeforeConnectionOpen(args.DataConnection, cn),
					(args, cn) => OnConnectionOpened(args.DataConnection, cn),
					null,
					null));

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
				Assert.AreEqual(1, open);
				Assert.AreEqual(1, opened);
				open   = 0;
				opened = 0;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					cdb.Close();
					_ = cdb.Connection;
					// with MARS cloned data connection inherit connection from parent and close do nothing
					var expected = db.IsMarsEnabled ? 0 : 1;
					Assert.AreEqual(expected, open);
					Assert.AreEqual(expected, opened);
				}
			}

			void OnBeforeConnectionOpen(DataConnection dc, DbConnection cn) => open++;
			void OnConnectionOpened    (DataConnection dc, DbConnection cn) => opened++;
		}

		[Test]
		public async Task TestCloneOnBeforeConnectionOpenAsyncOnConnectionOpenedAsync([DataSources(false)] string context)
		{
			var open   = 0;
			var opened = 0;

			using (var db = GetDataConnection(context))
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
				db.AddInterceptor(new TestConnectionInterceptor(
					null,
					null,
					(args, cn, ct) => OnBeforeConnectionOpenAsync(args.DataConnection, cn, ct),
					(args, cn, ct) => OnConnectionOpenedAsync(args.DataConnection, cn, ct)));
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
				Assert.AreEqual(1, open);
				Assert.AreEqual(1, opened);
				open   = 0;
				opened = 0;

				using (var cdb = (DataConnection)((IDataContext)db).Clone(true))
				{
					Assert.AreEqual(0, open);
					Assert.AreEqual(0, opened);
					cdb.Close();
					await cdb.EnsureConnectionAsync();
					// with MARS cloned data connection inherit connection from parent and close do nothing
					var expected = db.IsMarsEnabled ? 0 : 1;
					Assert.AreEqual(expected, open);
					Assert.AreEqual(expected, opened);
				}
			}

			Task OnBeforeConnectionOpenAsync(DataConnection dc, DbConnection cn, CancellationToken ct)
			{
				open++;
				return Task.CompletedTask;
			}

			Task OnConnectionOpenedAsync(DataConnection dc, DbConnection cn, CancellationToken ct)
			{
				opened++;
				return Task.CompletedTask;
		}
		}

		// strange provider errors, review in v3 with more recent providers
		// also some providers remove credentials from connection string in non-design mode
		[ActiveIssue(Configurations = new[]
		{
			//ProviderName.MySqlConnector,
			ProviderName.SapHanaNative, // HanaException: error while parsing protocol
			// Providers remove credentials in non-design mode:
			TestProvName.AllPostgreSQL,
			TestProvName.AllSqlServer,
			TestProvName.AllMySqlData
		})]
		[Test]
		public void TestDisposeFlagCloning([DataSources(false)] string context, [Values] bool dispose)
		{
			using (var db = GetDataConnection(context))
			{
				var cn = db.Connection;
				using (var testDb = new DataConnection(db.DataProvider, cn, dispose))
				{
					Assert.AreEqual(ConnectionState.Open, cn.State);

					DbConnection? clonedConnection = null;
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
							var c = ((IDataContext)db).UnwrapDataObjectInterceptor?.UnwrapConnection(db, clonedConnection);

							// bug in Miniprofiler.
							//
							if (c != null)
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
				context == ProviderName.SapHanaNative  ||
				context == ProviderName.SqlCe          ||
				context == ProviderName.Sybase         ||
				context.IsAnyOf(TestProvName.AllMySqlConnector) ||
				context.IsAnyOf(TestProvName.AllFirebird)       ||
				context.IsAnyOf(TestProvName.AllOracle)         ||
				context.IsAnyOf(TestProvName.AllPostgreSQL)     ||
				context.IsAnyOf(TestProvName.AllSqlServer)      ||
				context.IsAnyOf(TestProvName.AllSQLiteClassic)))
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
				using (var db = GetDataContext(context))
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
		public void TestDisposeFlagCloning962Test2([DataSources(false)] string context, [Values] bool withScope)
		{
			if (withScope && (
				context == ProviderName.DB2                 ||
				context == ProviderName.InformixDB2         ||
				context == ProviderName.SapHanaOdbc         ||
				context == ProviderName.SqlCe               ||
				context == ProviderName.Sybase              ||
#if !NET472
				context.IsAnyOf(TestProvName.AllOracleManaged)              ||
				context.IsAnyOf(ProviderName.SapHanaNative)                 ||
#endif
				context.IsAnyOf(TestProvName.AllMySqlData)                  ||
				context.IsAnyOf(TestProvName.AllAccess)                     ||
				context.IsAnyOf(TestProvName.AllSqlServer)                  ||
				context.IsAnyOf(TestProvName.AllPostgreSQL)                 ||
				context.IsAnyOf(TestProvName.AllSQLiteClassic)
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
				// SAP.Native: Operation is not supported on this platform.
				// SqlServer: The operation is not valid for the state of the transaction.
				Assert.Inconclusive("Provider not configured or has issues with TransactionScope");
			}

			var scope = withScope ? new TransactionScope() : null;

			try
			{
				using var db = GetDataConnection(context);

					// test cloned data connection without LoadWith, as it doesn't use cloning in v3
					db.Select(() => "test1");

				using var cdb = ((IDataContext)db).Clone(true);

						cdb.Select(() => "test2");

						scope?.Complete();
					}
			finally
			{
				scope?.Dispose();
			}
		}
		#endregion

		[Table]
		class TransactionScopeTable
		{
			[Column] public int Id { get; set; }
		}

		[Test]
		public void Issue2676TransactionScopeTest1([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				db.CreateTable<TransactionScopeTable>();
			}

			try
			{
				using (var db = GetDataConnection(context))
				{
					db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 1 });
					using (var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
					{
						// this query will be executed outside of TransactionScope transaction as it wasn't enlisted into connection
						// will change when https://github.com/linq2db/linq2db/issues/2676 implemented
						db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 2 });

						Transaction.Current!.Rollback();
					}

					db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 3 });

					var ids = db.GetTable<TransactionScopeTable>().Select(_ => _.Id).OrderBy(_ => _).ToArray();

					Assert.AreEqual(3, ids.Length);
				}
			}
			finally
			{
				using (var db = GetDataConnection(context))
				{
					db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public void Issue2676TransactionScopeTest2([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				db.CreateTable<TransactionScopeTable>();
			}

			try
			{
				using (var db = GetDataConnection(context))
				{
					using (var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
					{
						db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 2 });

						Transaction.Current!.Rollback();
					}

					db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 3 });

					var ids = db.GetTable<TransactionScopeTable>().Select(_ => _.Id).OrderBy(_ => _).ToArray();

					Assert.AreEqual(1, ids.Length);
					Assert.AreEqual(3, ids[0]);
				}
			}
			finally
			{
				using (var db = GetDataConnection(context))
				{
					db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public void Issue2676TransactionScopeTest3([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				db.CreateTable<TransactionScopeTable>();
			}

			try
			{
				using (var db = GetDataConnection(context))
				{
					db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 1 });
					using (var transaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
					{
						db.Connection.EnlistTransaction(Transaction.Current);
						db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 2 });

						Transaction.Current!.Rollback();
					}

					db.GetTable<TransactionScopeTable>().Insert(() => new TransactionScopeTable() { Id = 3 });

					var ids = db.GetTable<TransactionScopeTable>().Select(_ => _.Id).OrderBy(_ => _).ToArray();

					Assert.AreEqual(2, ids.Length);
					Assert.AreEqual(1, ids[0]);
					Assert.AreEqual(3, ids[1]);
				}
			}
			finally
			{
				using (var db = GetDataConnection(context))
				{
					db.DropTable<TransactionScopeTable>(throwExceptionIfNotExists: false);
				}
			}
		}

		#region MARS Support Tests (https://github.com/linq2db/linq2db/issues/2643)

		// Following providers allow multiple active data readers on same command:
		// ORACLE: Oracle.DataAccess
		// ORACLE: Oracle.ManagedDataAccess(.Core)
		// SQLCE : System.Data.SqlServerCe
		// SQLITE: Microsoft.Data.Sqlite (prior to v2.1.0)
		// SYBASE: AdoNetCore.AseClient
		[Test]
		public void MARS_MultipleDataReadersOnSameCommand_Supported(
			[IncludeDataSources(false,
				TestProvName.AllOracle,
				ProviderName.SqlCe,
				ProviderName.SybaseManaged)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				var cnt1 = db.Person.Count();
				var cnt2 = 0;
				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				using (var cmd = db.CreateCommand())
				{
					cmd.CommandText = sql;
					using (var reader1 = cmd.ExecuteReader())
					{
						while (reader1.Read())
						{
							cnt2++;

							// open another reader on same command
							var cnt3 = 0;
							using (var reader2 = cmd.ExecuteReader())
							{
								while (reader2.Read())
								{
									cnt3++;
								}
							}

							Assert.True(cnt3 > 0);
						}
					}
				}

				Assert.True(cnt1 > 0);
				Assert.AreEqual(cnt1, cnt2);
			}
		}

		[Test]
		public void MARS_MultipleDataReadersOnSameCommand_NotSupported(
			[DataSources(false,
				TestProvName.AllOracle,
				ProviderName.SqlCe,
				ProviderName.SQLiteMS,
				ProviderName.SybaseManaged)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				using (var cmd = db.CreateCommand())
				{
					cmd.CommandText = sql;
					try
					{
						using (var reader1 = cmd.ExecuteReader())
						{
							while (reader1.Read())
							{
								// open another reader on same command
								using (var reader2 = cmd.ExecuteReader())
								{
									while (reader2.Read())
									{
									}
								}
							}
						}
					}
					catch
					{
						Assert.Pass();
					}
				}
			}

			Assert.Fail("Failure expected");
		}

		// Following providers allow multiple active data readers with own command:
		// ACCESS   : System.Data.OleDb
		// ACCESS   : System.Data.Odbc
		// DB2      : IBM.Data.DB2(.Core)
		// Firebird : FirebirdSql.Data.FirebirdClient
		// Informix : IBM.Data.DB2(.Core)
		// Informix : IBM.Data.Informix
		// ORACLE   : Oracle.DataAccess
		// ORACLE   : Oracle.ManagedDataAccess(.Core)
		// SAP HANA : Sap.Data.Hana.v4.5/Sap.Data.Hana.Core.v2.1
		// SAP HANA : System.Data.Odbc
		// SQLCE    : System.Data.SqlServerCe
		// SQLITE   : System.Data.Sqlite
		// SQLITE   : Microsoft.Data.Sqlite (prior to v2.1.0)
		// SQLServer: System.Data.SqlClient (with MARS enabled)
		// SQLServer: Microsoft.Data.SqlClient (with MARS enabled)
		// SYBASE   : Sybase.AdoNet45.AseClient
		// SYBASE   : AdoNetCore.AseClient
		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_NoDispose_Supported(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				var cnt1 = db.Person.Count();
				var cnt2 = 0;
				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				using (var cmd = db.CreateCommand())
				{
					cmd.CommandText = sql;
					using (var reader1 = cmd.ExecuteReader())
					{
						while (reader1.Read())
						{
							cnt2++;

							// open another reader on new command
							using (var cmd2 = db.CreateCommand())
							{
								var cnt3 = 0;
								cmd2.CommandText = sql;

								using (var reader2 = cmd2.ExecuteReader())
								{
									while (reader2.Read())
									{
										cnt3++;
									}
								}

								Assert.True(cnt3 > 0);
							}
						}
					}
				}

				Assert.True(cnt1 > 0);
				Assert.AreEqual(cnt1, cnt2);
			}
		}

		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_NoDispose_NotSupported(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				using (var cmd = db.CreateCommand())
				{
					cmd.CommandText = sql;
					try
					{
						using (var reader1 = cmd.ExecuteReader())
						{
							while (reader1.Read())
							{
								// open another reader on new command
								using (var cmd2 = db.CreateCommand())
								{
									cmd2.CommandText = sql;

									using (var reader2 = cmd2.ExecuteReader())
									{
										while (reader2.Read())
										{
										}
									}
								}
							}
						}
					}
					catch
					{
						Assert.Pass();
					}
				}
			}

			Assert.Fail("Failure expected");
		}

		// Following providers allow multiple active data readers with own command (disposed):
		// ACCESS   : System.Data.OleDb
		// ACCESS   : System.Data.Odbc
		// DB2      : IBM.Data.DB2(.Core)
		// Informix : IBM.Data.DB2(.Core)
		// Informix : IBM.Data.Informix
		// ORACLE   : Oracle.DataAccess
		// ORACLE   : Oracle.ManagedDataAccess(.Core)
		// SAP HANA : Sap.Data.Hana.v4.5/Sap.Data.Hana.Core.v2.1
		// SAP HANA : System.Data.Odbc
		// SQLCE    : System.Data.SqlServerCe
		// SQLITE   : System.Data.Sqlite
		// SQLITE   : Microsoft.Data.Sqlite (prior to v2.1.0)
		// SQLServer: System.Data.SqlClient (with MARS enabled)
		// SQLServer: Microsoft.Data.SqlClient (with MARS enabled)
		// SYBASE   : Sybase.AdoNet45.AseClient
		// SYBASE   : AdoNetCore.AseClient
		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_Dispose_Supported(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLiteClassic,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				var cnt1 = db.Person.Count();
				var cnt2 = 0;
				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				var cmd = db.CreateCommand();
				cmd.CommandText = sql;
				using (var reader1 = cmd.ExecuteReader())
				{
					cmd.Dispose();
					while (reader1.Read())
					{
						cnt2++;

						// open another reader on new command
						using (var cmd2 = db.CreateCommand())
						{
							var cnt3 = 0;
							cmd2.CommandText = sql;

							using (var reader2 = cmd2.ExecuteReader())
							{
								while (reader2.Read())
								{
									cnt3++;
								}
							}

							Assert.True(cnt3 > 0);
						}
					}
				}

				Assert.True(cnt1 > 0);
				Assert.AreEqual(cnt1, cnt2);
			}
		}

		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_Dispose_NotSupported(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				db.Person.ToList();
				var sql = db.LastQuery!;

				// we need to use raw ADO.NET for this test, as we ADO.NET test provider behavior without linq2db
				var cmd = db.CreateCommand();
				cmd.CommandText = sql;
				using (var reader1 = cmd.ExecuteReader())
				{
					cmd.Dispose();
					try
					{
						while (reader1.Read())
						{
							// open another reader on new command
							using (var cmd2 = db.CreateCommand())
							{
								cmd2.CommandText = sql;

								using (var reader2 = cmd2.ExecuteReader())
								{
									while (reader2.Read())
									{
									}
								}
							}
						}
					}
					catch
					{
						Assert.Pass();
					}
				}
			}

			Assert.Fail("Failure expected");
		}

		[Test]
		public void MARS_Supported(
			[DataSources(false,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				var cnt1 = db.Person.Count();
				var cnt2 = 0;
				foreach (var p in db.Person)
				{
					db.Doctor.Where(_ => _.PersonID == p.ID).ToList();
					cnt2++;
				}

				Assert.True(cnt1 > 0);
				Assert.AreEqual(cnt1, cnt2);
			}
		}

		[Test]
		public void MARS_Unsupported(
			[IncludeDataSources(false,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && db.IsMarsEnabled)
					Assert.Ignore("MARS enabled");

				var failed = false;
				try
				{
					foreach (var p in db.Person)
						db.Doctor.Where(_ => _.PersonID == p.ID).ToList();
				}
				catch { failed = true; }

				if (!failed)
					Assert.Fail("Failure expected");
			}
		}

		[Test]
		public void MARS_ParametersPreservedAfterDispose([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var param = "test";

				db.Person.Where(_ => _.LastName == param).ToList();

				Assert.AreEqual(1, commandInterceptor.Parameters.Length);
			}
		}

		[Test]
		public async Task MARS_ParametersPreservedAfterDisposeAsync([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var param = "test";

				await db.Person.Where(_ => _.LastName == param).ToListAsync();

				Assert.AreEqual(1, commandInterceptor.Parameters.Length);
			}
		}

#if !NET472
		[Test]
		public async Task MARS_SupportedAsync(
			[DataSources(false,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && !db.IsMarsEnabled)
					Assert.Ignore("MARS not enabled");

				var cnt1 = await db.Person.CountAsync();
				var cnt2 = 0;
				await foreach(var p in db.Person.AsAsyncEnumerable())
				{
					await db.Doctor.Where(_ => _.PersonID == p.ID).ToListAsync();
					cnt2++;
				}

				Assert.True(cnt1 > 0);
				Assert.AreEqual(cnt1, cnt2);
			}
		}

		[Test]
		public async Task MARS_UnsupportedAsync(
			[IncludeDataSources(false,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				if (db.DataProvider is SqlServerDataProvider && db.IsMarsEnabled)
					Assert.Ignore("MARS enabled");

				var failed = false;
				try
				{
					await foreach (var p in db.Person.AsAsyncEnumerable())
						await db.Doctor.Where(_ => _.PersonID == p.ID).ToListAsync();
				}
				catch { failed = true; }

				if (!failed)
					Assert.Fail("Failure expected");
			}
		}
#endif
#endregion
	}
}
