using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.DataProvider.SqlServer;
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

		[Test, DataContextSource(false)]
		public void RetryPoliceTest(string context)
		{
			var ret = new Retry();
			Assert.Throws<TestException>(() =>
			{
				using (var db = new DataConnection(context) { RetryPolicy = ret })
				{
					// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.AreEqual(2, ret.Count); // 1 - open connection, 1 - execute command
		}

		[Test]
		public async Task ExecuteTestAsync([IncludeDataSources(false,
			ProviderName.SqlServer2008)]
			string context)
		{
			var ret = new Retry();

			using (var db = new DataConnection(context) { RetryPolicy = new SqlServerRetryPolicy() })
			{
				var i = await db.ExecuteAsync("SELECT 1");

				Assert.That(i, Is.EqualTo(-1));
			}
		}

		[Test, DataContextSource(false)]
		public void RetryPoliceTestAsync(string context)
		{
			var ret = new Retry();

			try
			{
				using (var db = new DataConnection(context) { RetryPolicy = ret })
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
	}
}
