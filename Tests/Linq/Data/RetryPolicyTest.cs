using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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

			public TResult Execute<TResult>([NotNull] Func<TResult> operation)
			{
				Count++;
				try
				{
					return operation();
				}
				catch
				{
					throw new RetryException();
				}
			}

			public Task<TResult> ExecuteAsync<TResult>([NotNull] Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default(CancellationToken))
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
					throw new RetryException();
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
			
			Assert.Throws<RetryException>(() =>
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
				Assert.IsNotNull(ex.InnerExceptions.OfType<RetryException>().Single());
			}

			Assert.AreEqual(1, ret.Count);
		}

	}
}
