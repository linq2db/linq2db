using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
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

		public sealed class TestException : Exception
		{}

		public class FakeClass
		{}

		[Test]
		public void TestRetryPolicy([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.Throws<TestException>(() =>
			{
				using (var db = GetDataConnection(context, retryPolicy: ret))
				{
					// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void RetryPolicyOptions1([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.Throws<TestException>(() =>
			{
				using (var db = GetDataConnection(context, o => o.UseRetryPolicy(ret)))
				{
					// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void RetryPolicyOptions2([DataSources(false)] string context)
		{
			var ret = new Retry();
			Assert.Throws<TestException>(() =>
			{
				using (var db = GetDataConnection(context, o => o.UseRetryPolicy(ret)))
				{
					// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.That(ret.Count, Is.EqualTo(2)); // 1 - open connection, 1 - execute command
		}

		[Test]
		public async Task ExecuteAsync([IncludeDataSources(TestProvName.AllSqlServer2008)] string context)
		{
			using (var db = GetDataConnection(context, retryPolicy: new SqlServerRetryPolicy()))
			{
				var i = await db.ExecuteAsync("SELECT 1");

				Assert.That(i, Is.EqualTo(-1));
			}
		}

		[Test]
		public async Task ExecuteAsyncOption([IncludeDataSources(TestProvName.AllSqlServer2008)] string context)
		{
			using (var db = GetDataConnection(context, o => o.UseRetryPolicy(new SqlServerRetryPolicy())))
			{
				var i = await db.ExecuteAsync("SELECT 1");

				Assert.That(i, Is.EqualTo(-1));
			}
		}

		[Test]
		public void RetryPolicyAsync([DataSources(false)] string context)
		{
			var ret = new Retry();

			try
			{
				using (var db = GetDataConnection(context, o => o.UseRetryPolicy(ret)))
				{
					var r = db.GetTable<FakeClass>().ToListAsync();
					r.Wait();
				}
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
				using (var db = GetDataConnection(context, o => o.UseFactory(connection => new DummyRetryPolicy())))
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
				using (var db = GetDataConnection(context, o => o.UseRetryPolicy(new DummyRetryPolicy())))
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
			using (var db1 = GetDataConnection(context))
			{
				var connection = db1.OpenDbConnection();
				try
				{
					using (var db = GetDataConnection(context,
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

		#region Issue 3431

		// issue reproduced on Open for MySqlConnector
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3431")]
		public void Issue3431Test1([IncludeDataSources(TestProvName.AllMySqlConnector)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var provider = DataConnection.GetDataProvider(context);

			using var db = GetDataContext(
				context,
				o => o
					.UseRetryPolicy(new Issue3431RetryPolicy())
					.UseConnectionString(provider, "BAD" + connectionString));

			db.Person.ToArray();
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3431")]
		public async Task Issue3431Test2([IncludeDataSources(TestProvName.AllMySqlConnector)] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var provider = DataConnection.GetDataProvider(context);

			using var db = GetDataContext(
				context,
				o => o
					.UseRetryPolicy(new Issue3431RetryPolicy())
					.UseConnectionString(provider, "BAD" + connectionString));

			await db.Person.ToArrayAsync();
		}

		sealed class Issue3431RetryPolicy : RetryPolicyBase
		{
			public Issue3431RetryPolicy()
				: base(1, default, 1, 1, default)
			{
			}

			protected override bool ShouldRetryOn(Exception exception) => true;
		}

		#endregion
	}
}
