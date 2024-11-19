using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
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

		[ActiveIssue("Investigation required. Timeouts on CI", Configurations = [ TestProvName.AllSqlServer2008Minus ])]
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
				Configuration.RetryPolicy.Factory = cn => new DummyRetryPolicy();
				using (var db = GetDataConnection(context))
				using (db.CreateLocalTable<MyEntity>())
				{
					Assert.Fail("Exception expected");
				}
			}
			catch (NotImplementedException ex)
			{
				Assert.That(ex.Message, Is.EqualTo("Execute"));
			}
			finally
			{
				Configuration.RetryPolicy.Factory = null;
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
			using (var db1 = GetDataConnection(context))
			{
				try
				{
					Configuration.RetryPolicy.Factory = cn => new DummyRetryPolicy();
					using (var db = new DataConnection(db1.DataProvider, db1.Connection))
					using (db.CreateLocalTable<MyEntity>())
					{
						Assert.Fail("Exception expected");
					}
				}
				catch (NotImplementedException ex)
				{
					Assert.That(ex.Message, Is.EqualTo("ExecuteT"));
				}
				finally
				{
					Configuration.RetryPolicy.Factory = null;
				}
			}
		}

		[Test]
		public void ExternalConnectionOption([DataSources(false)] string context)
		{
			using (var db1 = GetDataConnection(context))
			{
				try
				{
					using (var db = GetDataConnection(context,
						o => o
							.UseConnection(db1.DataProvider, db1.Connection)
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
	}
}
