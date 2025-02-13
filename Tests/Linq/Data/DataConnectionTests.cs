using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.SqlServer;
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
		public void UsingDataProvider([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider     = DataConnection.GetDataProvider(context);

			using (var conn = new DataConnection(dataProvider, connectionString))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
					Assert.That(conn.ConfigurationString, Is.Null);
				});
			}
		}

		[Test]
		public void UsingDefaultConfiguration()
		{
			using (var conn = new DataConnection())
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
					Assert.That(conn.ConfigurationString, Is.EqualTo(DataConnection.DefaultConfiguration));
				});
			}
		}

		[Test]
		public void Test3([IncludeDataSources(
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2005,
			TestProvName.AllAccess,
			TestProvName.AllClickHouse)]
			string context)
		{
			using (var conn = GetDataConnection(context))
			{
				Assert.Multiple(() =>
				{
					Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
					Assert.That(conn.ConfigurationString, Is.EqualTo(context));
				});

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
		public void GetDataProviderTest([IncludeDataSources(ProviderName.DB2, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
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

		private sealed class TestConnectionInterceptor : ConnectionInterceptor
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

				Assert.Multiple(() =>
				{
					Assert.That(opened, Is.False);
					Assert.That(openedAsync, Is.False);
					Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
				});
				Assert.Multiple(() =>
				{
					Assert.That(opened, Is.True);
					Assert.That(openedAsync, Is.False);
				});
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

				Assert.Multiple(() =>
				{
					Assert.That(opened, Is.False);
					Assert.That(openedAsync, Is.False);
				});
				await conn.SelectAsync(() => 1);
				Assert.Multiple(() =>
				{
					Assert.That(opened, Is.False);
					Assert.That(openedAsync, Is.True);
				});
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
		public void TestServiceCollection1([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDB((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<IDataContext>()!;
			Assert.Multiple(() =>
			{
				Assert.That(con is DataConnection, Is.True);
				Assert.That(((DataConnection)con).ConfigurationString, Is.EqualTo(context));
			});
		}

		[Test]
		public void TestServiceCollection2([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<DataConnection>((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<DataConnection>()!;
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestServiceCollection3([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddTransient<DummyService>();
			collection.AddLinqToDBContext<DbConnection3>((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<DbConnection3>()!;
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestServiceCollection4([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddTransient<DummyService>();
			collection.AddLinqToDBContext<DbConnection4>(serviceProvider => new DbConnection4(new DataOptions<IDataContext>(new DataOptions().UseConfigurationString(context))));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<DbConnection3>()!;
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestServiceCollection_Issue4326_Positive([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<IDataContext, DbConnection1>((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<IDataContext>()!;
			Assert.That(con, Is.TypeOf<DbConnection1>());
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestServiceCollection_Issue4326_Compat([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<IDataContext, DbConnection4>((serviceProvider, options) => options.UseConfigurationString(context));
			var provider = collection.BuildServiceProvider();
			var con = provider.GetService<IDataContext>()!;
			Assert.That(con, Is.TypeOf<DbConnection4>());
			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		public class DbConnection1 : DataConnection
		{
			public DbConnection1(DataOptions<DbConnection1> options) : base(options.Options)
			{
			}
		}

		public class DbConnection2 : DataConnection
		{
			public DbConnection2(DataOptions<DbConnection2> options) : base(options.Options)
			{
			}
		}

		public class DummyService { }

		public class DbConnection3 : DataConnection
		{
			public DbConnection3(DummyService service, DataOptions options) : base(options)
			{
			}
		}

		public class DbConnection4 : DataConnection
		{
			public DbConnection4(DataOptions<IDataContext> options) : base(options.Options)
			{
			}
		}

		public class DbConnection5 : DataConnection
		{
			public DbConnection5(DataOptions options) : base(options)
			{
				Assert.Multiple(() =>
				{
					Assert.That(options.DataContextOptions.CommandTimeout, Is.EqualTo(91));
					Assert.That(CommandTimeout,                            Is.EqualTo(91));
				});
			}
		}

		[Test]
		public void TestServiceCollection_Issue4476([DataSources(false)] string context)
		{
			var collection = new ServiceCollection();

			collection.AddLinqToDBContext<DbConnection5>((_, options) => options.UseConfigurationString(context).UseCommandTimeout(91));

			var provider = collection.BuildServiceProvider();
			var con      = provider.GetService<DbConnection5>()!;

			Assert.That(con.ConfigurationString, Is.EqualTo(context));
		}

		[Test]
		public void TestSettingsPerDb([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var collection = new ServiceCollection();
			collection.AddLinqToDBContext<DbConnection1>((provider, options) => options.UseConfigurationString(context));
			collection.AddLinqToDBContext<DbConnection2>((provider, options) => options);

			var serviceProvider = collection.BuildServiceProvider();
			var c1 = serviceProvider.GetService<DbConnection1>()!;
			var c2 = serviceProvider.GetService<DbConnection2>()!;
			Assert.Multiple(() =>
			{
				Assert.That(c1.ConfigurationString, Is.EqualTo(context));
				Assert.That(c2.ConfigurationString, Is.EqualTo(DataConnection.DefaultConfiguration));
			});
		}

		// informix connection limits interfere with test
		[Test]
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

				Assert.Multiple(() =>
				{
					Assert.That(open, Is.False);
					Assert.That(openAsync, Is.False);
					Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
				});
				Assert.Multiple(() =>
				{
					Assert.That(open, Is.True);
					Assert.That(openAsync, Is.False);
				});
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

				Assert.Multiple(() =>
				{
					Assert.That(open, Is.False);
					Assert.That(openAsync, Is.False);
				});
				await conn.SelectAsync(() => 1);
				Assert.Multiple(() =>
				{
					Assert.That(open, Is.False);
					Assert.That(openAsync, Is.True);
				});
			}
		}

		[Test]
		[Explicit]
		public void CommandTimeoutTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
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
					Assert.That(time, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(29)));
					Assert.That(time, Is.LessThan(TimeSpan.FromSeconds(32)));
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
					Assert.That(time, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(9)));
					Assert.That(time, Is.LessThan(TimeSpan.FromSeconds(12)));
				}

				start = DateTimeOffset.Now;
				db.CommandTimeout = 0;
				db.Update(forUpdate);
				var time2 = DateTimeOffset.Now - start;
				Assert.That(time2, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(59)));
				Assert.That(time2, Is.LessThan(TimeSpan.FromSeconds(62)));
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
			if (context.IsAnyOf(ProviderName.ClickHouseOctonica))
			{
				Assert.Inconclusive("Provider goes crazy");
			}

			if (withScope && (
				context == ProviderName.DB2                     ||
				context == ProviderName.InformixDB2             ||
				context == ProviderName.SapHanaNative           ||
				context == ProviderName.SqlCe                   ||
				context == ProviderName.Sybase                  ||
				context.IsAnyOf(TestProvName.AllMySqlConnector) ||
				context.IsAnyOf(TestProvName.AllClickHouse)     ||
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
				// ClickHouse doesn't support transactions
				Assert.Inconclusive("Provider not configured or has issues with TransactionScope or doesn't support DDL in distributed transactions");
			}

			using var nolog = context.IsAnyOf(TestProvName.AllAccess) ? new DisableBaseline("Access NETFX provider has issues with TS") : null;

			using var scope  = withScope ? new TransactionScope() : null;
			using var db     = GetDataContext(context);
			using var tc     = db.CreateLocalTable(Category.Data);
			using var tp     = db.CreateLocalTable(Product.Data);
			var categoryDtos = db.GetTable<Category>().LoadWith(c => c.Products).ToList();
		}
		#endregion

		[Table]
		sealed class TransactionScopeTable
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

					Assert.That(ids, Has.Length.EqualTo(3));
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

					Assert.That(ids, Has.Length.EqualTo(1));
					Assert.That(ids[0], Is.EqualTo(3));
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

					Assert.That(ids, Has.Length.EqualTo(2));
					Assert.Multiple(() =>
					{
						Assert.That(ids[0], Is.EqualTo(1));
						Assert.That(ids[1], Is.EqualTo(3));
					});
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
		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_MultipleDataReadersOnSameCommand_Supported(
			[IncludeDataSources(false,
				TestProvName.AllOracle,
				ProviderName.SqlCe,
				// depends on connection pool size
				//ProviderName.ClickHouseClient,
				ProviderName.ClickHouseOctonica,
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

							Assert.That(cnt3, Is.GreaterThan(0));
						}
					}
				}

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.GreaterThan(0));
					Assert.That(cnt2, Is.EqualTo(cnt1));
				});
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_MultipleDataReadersOnSameCommand_NotSupported(
			[DataSources(false,
				ProviderName.ClickHouseClient,
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
		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
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
				// disabled - depends on connection pool size
				// which is one for session-aware connection
				//ProviderName.ClickHouseClient,
				ProviderName.ClickHouseOctonica,
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

								Assert.That(cnt3, Is.GreaterThan(0));
							}
						}
					}
				}

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.GreaterThan(0));
					Assert.That(cnt2, Is.EqualTo(cnt1));
				});
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_NoDispose_NotSupported(
			[DataSources(false,
				TestProvName.AllAccess,
			ProviderName.ClickHouseClient,
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
		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
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
				// depends on connection pool size
				//ProviderName.ClickHouseClient,
				ProviderName.ClickHouseOctonica,
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

							Assert.That(cnt3, Is.GreaterThan(0));
						}
					}
				}

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.GreaterThan(0));
					Assert.That(cnt2, Is.EqualTo(cnt1));
				});
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_ProviderSupportsMultipleDataReadersOnNewCommand_Dispose_NotSupported(
			[DataSources(false,
				TestProvName.AllAccess,
				ProviderName.ClickHouseClient,
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

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_Supported(
			[DataSources(false,
				TestProvName.AllMySql,
				ProviderName.ClickHouseMySql,
				// depends on connection pool size
				ProviderName.ClickHouseClient,
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

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.GreaterThan(0));
					Assert.That(cnt2, Is.EqualTo(cnt1));
				});
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public void MARS_Unsupported(
			[IncludeDataSources(false,
				TestProvName.AllMySql,
				ProviderName.ClickHouseMySql,
				ProviderName.ClickHouseOctonica,
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
		public void MARS_ParametersPreservedAfterDispose([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var param = "test";

				db.Person.Where(_ => _.LastName == param).ToList();

				Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public async Task MARS_ParametersPreservedAfterDisposeAsync([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var commandInterceptor = new SaveCommandInterceptor();
				db.AddInterceptor(commandInterceptor);

				var param = "test";

				await db.Person.Where(_ => _.LastName == param).ToListAsync();

				Assert.That(commandInterceptor.Parameters, Has.Length.EqualTo(1));
			}
		}

#if !NETFRAMEWORK
		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public async Task MARS_SupportedAsync(
			[DataSources(false,
				TestProvName.AllMySql,
				ProviderName.ClickHouseMySql,
				// depends on connection pool size
				ProviderName.ClickHouseClient,
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

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.GreaterThan(0));
					Assert.That(cnt2, Is.EqualTo(cnt1));
				});
			}
		}

		[ActiveIssue("https://github.com/Octonica/ClickHouseClient/issues/59", Configuration = ProviderName.ClickHouseOctonica)]
		[Test]
		public async Task MARS_UnsupportedAsync(
			[IncludeDataSources(false,
				TestProvName.AllMySql,
				TestProvName.AllPostgreSQL,
				ProviderName.ClickHouseMySql,
				ProviderName.ClickHouseOctonica)] string context)
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

		[Test]
		public void MappingSchemaReuse([DataSources] string context)
		{
			using var cn1 = GetDataContext(context);
			using var cn2 = GetDataContext(context);

			Assert.That(cn2.MappingSchema, Is.EqualTo(cn1.MappingSchema));
		}

		[Test]
		public void CustomMappingSchemaCaching([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<string, int>(int.Parse);

			using var cn1 = GetDataContext(context, ms);
			using var cn2 = GetDataContext(context, ms);

			Assert.That(cn2.MappingSchema, Is.EqualTo(cn1.MappingSchema));
		}
	}
}
