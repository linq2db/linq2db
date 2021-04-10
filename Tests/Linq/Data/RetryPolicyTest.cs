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
		class Retry : IRetryPolicy
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

		class TestException : Exception
		{}

		public class FakeClass
		{}

		[Test]
		public void RetryPoliceTest([DataSources(false)] string context)
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

			Assert.AreEqual(2, ret.Count); // 1 - open connection, 1 - execute command
		}

		[Test]
		public async Task ExecuteTestAsync([IncludeDataSources(ProviderName.SqlServer2008)] string context)
		{
			var ret = new Retry();

			using (var db = GetDataConnection(context, retryPolicy: new SqlServerRetryPolicy()))
			{
				var i = await db.ExecuteAsync("SELECT 1");

				Assert.That(i, Is.EqualTo(-1));
			}
		}

		[Test]
		public void RetryPoliceTestAsync([DataSources(false)] string context)
		{
			var ret = new Retry();

			try
			{
				using (var db = GetDataConnection(context, retryPolicy: ret))
				{
					var r = db.GetTable<FakeClass>().ToListAsync();
					r.Wait();
				}
			}
			catch (AggregateException ex)
			{
				Assert.IsNotNull(ex.InnerExceptions.OfType<TestException>().Single());
			}

			Assert.AreEqual(2, ret.Count); // 1 - open connection, 1 - execute command
		}

		[Test]
		public void Issue2672_RetryPolicyFactoryTest([DataSources(false)] string context)
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
				Assert.AreEqual("Execute", ex.Message);
			}
			finally
			{
				Configuration.RetryPolicy.Factory = null;
			}
		}

		[Test]
		public void Issue2672_ExternalConnection([DataSources(false)] string context)
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
					Assert.AreEqual("ExecuteT", ex.Message);
				}
				finally
				{
					Configuration.RetryPolicy.Factory = null;
				}
			}
		}

		[Table]
		class MyEntity
		{
			[Column                       ] public long   Id   { get; set; }
			[NotNull, Column(Length = 256)] public string Name { get; set; } = null!;
		}

		class DummyRetryPolicy : IRetryPolicy
		{
			public TResult       Execute<TResult>(Func<TResult> operation)                                                                                              => throw new NotImplementedException("ExecuteT");
			public void          Execute(Action operation)                                                                                                              => throw new NotImplementedException("Execute");
			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = new CancellationToken()) => throw new NotImplementedException("ExecuteAsyncT");
			public Task          ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = new CancellationToken())                   => throw new NotImplementedException("ExecuteAsync");
		}
	}
}
