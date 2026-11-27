using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Data
{
	[TestFixture]
	public class RetryPolicyTest : TestBase
	{
		sealed class Retry : IRetryPolicy
		{
			public int Count { get; private set; }

			public TResult Execute<TResult>(Func<TResult> operation)
			{
				Count++;

				try
				{
					return operation();
				}
				catch
				{
					throw new TestException();
				}
			}

			public void Execute(Action operation)
			{
				Count++;

				try
				{
					operation();
				}
				catch
				{
					throw new TestException();
				}
			}

			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default(CancellationToken))
			{
				Count++;
				try
				{
					var res = operation(cancellationToken);
					res.Wait(cancellationToken);
					return res;
				}
				catch
				{
					throw new TestException();
				}
			}

			public Task ExecuteAsync(Func<CancellationToken,Task> operation, CancellationToken cancellationToken = new CancellationToken())
			{
				Count++;
				try
				{
					var res = operation(cancellationToken);
					res.Wait(cancellationToken);
					return res;
				}
				catch
				{
					throw new TestException();
				}
			}
		}

		public sealed class TestException : Exception;

		public class FakeClass;

		[Test]
		public void TestRetryPolicy([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.That(() =>
			{
				using var db = GetDataConnection(context, retryPolicy: ret);
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				db.GetTable<FakeClass>().ToList();
			}, Throws.TypeOf<TestException>());

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void RetryPolicyOptions1([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.That(() =>
			{
				using var db = GetDataContext(context, o => o.UseRetryPolicy(ret));
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				db.GetTable<FakeClass>().ToList();
			}, Throws.TypeOf<TestException>());

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void RetryPolicyOptions2([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.That(() =>
			{
				using var db = GetDataContext(context, o => o.UseRetryPolicy(ret));
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				db.GetTable<FakeClass>().ToList();
			}, Throws.TypeOf<TestException>());

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public async Task ExecuteAsync([IncludeDataSources(TestProvName.AllSqlServer2008)] string context)
		{
			using var db = GetDataConnection(context, retryPolicy: new SqlServerRetryPolicy());
			var i = await db.ExecuteAsync("SELECT 1");

			Assert.That(i, Is.EqualTo(-1));
		}

		[Test]
		public async Task ExecuteAsyncOption([IncludeDataSources(TestProvName.AllSqlServer2008)] string context)
		{
			using var db = GetDataContext(context, o => o.UseRetryPolicy(new SqlServerRetryPolicy()));
			var i = await db.ExecuteAsync("SELECT 1");

			Assert.That(i, Is.EqualTo(-1));
		}

		[Test]
		public void RetryPolicyAsync([DataSources(false)] string context)
		{
			var ret = new Retry();

			try
			{
				using var db = GetDataContext(context, o => o.UseRetryPolicy(ret));
				var r = db.GetTable<FakeClass>().ToListAsync();
				r.Wait();
			}
			catch (AggregateException ex)
			{
				Assert.That(ex.InnerExceptions.OfType<TestException>().Single(), Is.Not.Null);
			}

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void RetryPolicyFactory([DataSources(false)] string context)
		{
			try
			{
				using (var db = GetDataContext(context, o => o.UseFactory(connection => new DummyRetryPolicy())))
				using (db.CreateLocalTable<MyEntity>())
				{
					Assert.Fail("Exception expected");
				}
			}
			catch (NotImplementedException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("Execute"));
			}
		}

		[Test]
		public void RetryPolicyFactoryOption([DataSources(false)] string context)
		{
			try
			{
				using (var db = GetDataContext(context, o => o.UseRetryPolicy(new DummyRetryPolicy())))
				using (db.CreateLocalTable<MyEntity>())
				{
					Assert.Fail("Exception expected");
				}
			}
			catch (NotImplementedException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("Execute"));
			}
		}

		[Test]
		public void ExternalConnection([DataSources(false)] string context)
		{
			using var db1 = GetDataConnection(context);

			var connection = db1.OpenDbConnection();

			try
			{
				using (var db = new DataConnection(new DataOptions().UseConnection(db1.DataProvider, connection).UseFactory(connection => new DummyRetryPolicy())))
				using (db.CreateLocalTable<MyEntity>())
				{
					Assert.Fail("Exception expected");
				}
			}
			catch (NotImplementedException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("ExecuteT"));
			}
		}

		[Test]
		public void ExternalConnectionOption([DataSources(false)] string context)
		{
			using var db1 = GetDataConnection(context);
			var connection = db1.OpenDbConnection();
			try
			{
				using (var db = GetDataContext(context,
					o => o
						.UseConnection(db1.DataProvider, connection)
						.UseRetryPolicy(new DummyRetryPolicy())))

				using (db.CreateLocalTable<MyEntity>())
				{
					Assert.Fail("Exception expected");
				}
			}
			catch (NotImplementedException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("ExecuteT"));
			}
		}

		[Table]
		sealed class MyEntity
		{
			[Column                       ] public long   Id   { get; set; }
			[NotNull, Column(Length = 256)] public string Name { get; set; } = null!;
		}

		sealed class DummyRetryPolicy : IRetryPolicy
		{
			public TResult       Execute<TResult>(Func<TResult> operation)                                                                                              => throw new NotImplementedException("ExecuteT");
			public void          Execute(Action operation)                                                                                                              => throw new NotImplementedException("Execute");
			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException("ExecuteAsyncT");
			public Task          ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = new CancellationToken())                   => throw new NotImplementedException("ExecuteAsync");
		}

		// issue reproduced on Open for MySqlConnector
		[ActiveIssue(3431, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "Cannot Open when State is Connecting.",
			Details = "the retry policy reopens a connection that is already opening - #3431's subject.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3431")]
		public void RetryPolicyDoesNotReopenConnectionThatIsOpening([IncludeDataSources(TestProvName.AllMySqlConnector)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var provider = DataConnection.GetDataProvider(context);

			using var db = GetDataContext(
				context,
				o => o
					.UseRetryPolicy(new RetryOnAnyExceptionPolicy())
					.UseConnectionString(provider, "BAD" + connectionString));

			db.Person.ToArray();
		}

		[ActiveIssue(3431, ErrorTypeName = "System.InvalidOperationException", ErrorMessage = "Cannot Open when State is Connecting.",
			Details = "as RetryPolicyDoesNotReopenConnectionThatIsOpening, through the async path.")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3431")]
		public async Task RetryPolicyDoesNotReopenConnectionThatIsOpeningAsync([IncludeDataSources(TestProvName.AllMySqlConnector)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var provider = DataConnection.GetDataProvider(context);

			using var db = GetDataContext(
				context,
				o => o
					.UseRetryPolicy(new RetryOnAnyExceptionPolicy())
					.UseConnectionString(provider, "BAD" + connectionString));

			await db.Person.ToArrayAsync();
		}

		sealed class RetryOnAnyExceptionPolicy : RetryPolicyBase
		{
			public RetryOnAnyExceptionPolicy()
				: base(1, default, 1, 1, default)
			{
			}

			protected override bool ShouldRetryOn(Exception exception) => true;
		}

		sealed class TestDelayRetryPolicy(double expBase) : RetryPolicyBase(
			10,
			Configuration.RetryPolicy.DefaultMaxDelay,
			Configuration.RetryPolicy.DefaultRandomFactor,
			expBase,
			Configuration.RetryPolicy.DefaultCoefficient)
		{
			protected override bool ShouldRetryOn(Exception exception) => exception is InvalidOperationException;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4913")]
		public void GetNextDelay_Invalid_ExpBase()
		{
			Assert.That(() =>
			{
				IRetryPolicy policy = new TestDelayRetryPolicy(0.999);
				policy.Execute(() => throw new InvalidOperationException());
			}, Throws.InstanceOf<ArgumentOutOfRangeException>().With.Property(nameof(ArgumentOutOfRangeException.ParamName)).EqualTo("exponentialBase"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4913")]
		public void GetNextDelay_MinAllowed_ExpBase()
		{
			Assert.That(() =>
			{
				IRetryPolicy policy = new TestDelayRetryPolicy(1.0);
				policy.Execute(() => throw new InvalidOperationException());
			}, Throws.InstanceOf<InvalidOperationException>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1174")]
		public async Task ConcurrentAsyncSelectsWithRetryPolicy([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			var dbFactory = RetryTestConnection.CreateFactory(context, 1, TimeSpan.FromSeconds(1), LinqToDB.Common.Configuration.RetryPolicy.DefaultRandomFactor, LinqToDB.Common.Configuration.RetryPolicy.DefaultExponentialBase, LinqToDB.Common.Configuration.RetryPolicy.DefaultCoefficient);

			var users = new[]
			{
				new RetryUserRecord { Id = TestData.Guid1, Name = "User1" },
				new RetryUserRecord { Id = TestData.Guid2, Name = "User2" }
			};

			// Ensures that concurrent async queries for multiple db instances are handled correctly
			using (var db = dbFactory())
			using (db.CreateLocalTable(users))
			{
				using var db1 = dbFactory();
				using var db2 = dbFactory();
				var user1Task = db1.Users.FirstAsync();
				var user2Task = db2.Users.FirstAsync();

				await Task.WhenAll(user1Task, user2Task);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(user1Task.Result, Is.Not.Null);
					Assert.That(user2Task.Result, Is.Not.Null);
				}
			}
		}

		public class RetryTestConnection : DataConnection
		{
			public static Func<RetryTestConnection> CreateFactory(string configuration, int retryCount, TimeSpan delay, double randomFactor, double exponentialBase, TimeSpan coefficient)
			{
				return () =>
				{
					var db = new RetryTestConnection(new DataOptions().UseConfiguration(configuration).UseRetryPolicy(new SqlServerRetryPolicy(retryCount, delay, randomFactor, exponentialBase, coefficient, null)));
					return db;
				};
			}
			public ITable<RetryUserRecord> Users => this.GetTable<RetryUserRecord>();

			public RetryTestConnection()
			{
			}

			public RetryTestConnection(DataOptions options)
				: base(options)
			{
			}

			public RetryTestConnection(IDataProvider dataProvider, string connectionString) : base(new DataOptions().UseConnectionString(dataProvider, connectionString))
			{
			}
		}

		public class RetryUserRecord
		{
			[PrimaryKey, NotNull] public Guid Id { get; set; }
			[Column, Nullable] public string? Name { get; set; }
		}
	}
}
