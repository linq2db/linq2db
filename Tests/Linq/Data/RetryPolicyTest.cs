using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;

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
					throw new ApplicationException();
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
					throw new ApplicationException();
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
					throw new ApplicationException();
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
					throw new ApplicationException();
				}
			}
		}

		public class FakeClass
		{}

		[Test, DataContextSource(false)]
		public void RetryPoliceTest(string context)
		{
			var ret = new Retry();
			Assert.Throws<ApplicationException>(() =>
			{
				using (var db = new DataConnection(context) { RetryPolicy = ret })
				{
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.AreEqual(2, ret.Count); // 1 - open connection, 1 - execute command
		}

#if !NOASYNC
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
				Assert.IsNotNull(ex.InnerExceptions.OfType<ApplicationException>().Single());
			}

			Assert.AreEqual(2, ret.Count); // 1 - open connection, 1 - execute command
		}
#endif
	}
}
