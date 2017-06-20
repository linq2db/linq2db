using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

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
					throw new RetryLimitExceededException();
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
					throw new RetryLimitExceededException();
				}
			}
		}

		public class FakeClass
		{
			
		}

		[Test, DataContextSource(false)]
		public void RetryPoliceTest(string context)
		{
			var ret = new Retry();
			
			Assert.Throws<RetryLimitExceededException>(() =>
			{
				using (var db = new DataConnection(context, ret))
				{
					db.GetTable<FakeClass>().ToList();
				}
			});

			Assert.AreEqual(1, ret.Count);
		}

		[Test, DataContextSource(false)]
		public void RetryPoliceTestAsync(string context)
		{
			var ret = new Retry();

			try
			{
				using (var db = new DataConnection(context, ret))
				{
					var r = db.GetTable<FakeClass>().ToListAsync();
					r.Wait();
				}
			}
			catch (AggregateException ex)
			{
				Assert.IsNotNull(ex.InnerExceptions.OfType<RetryLimitExceededException>().Single());
			}

			Assert.AreEqual(1, ret.Count);
		}

	}
}
