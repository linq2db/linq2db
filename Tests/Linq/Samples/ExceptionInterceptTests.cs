using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.Data.SQLite;
#endif

namespace Tests.Samples
{
	[TestFixture]
	public class ExceptionInterceptTests : TestBase
	{
		private class Retry : IRetryPolicy
		{
			public int Count { get; private set; }

			TResult IRetryPolicy.Execute<TResult>(Func<TResult> operation)
			{
				Count++;
				try
				{
					return operation();
				}
				catch (Exception original)
				{
					throw Intercept(original);
				}
			}

			Task<TResult> IRetryPolicy.ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
			{
				Count++;
				try
				{
					var res = operation(cancellationToken);
					res.Wait(cancellationToken);
					return res;
				}
				catch (Exception original)
				{
					throw Intercept(original);
				}
			}

			void IRetryPolicy.Execute(Action operation)
			{
				Count++;
				try
				{
					operation();
				}
				catch (Exception original)
				{
					throw Intercept(original);
				}
			}

			Task IRetryPolicy.ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
			{
				Count++;
				try
				{
					var res = operation(cancellationToken);
					res.Wait(cancellationToken);
					return res;
				}
				catch (Exception original)
				{
					throw Intercept(original);
				}
			}

			static Func<Exception, Exception> defaultExceptionIntercept = (original) => original;
			private Func<Exception, Exception> _exceptionIntercept = defaultExceptionIntercept;
			public Func<Exception, Exception> Intercept
			{
				get { return _exceptionIntercept; }
				set { _exceptionIntercept = value ?? defaultExceptionIntercept; }
			}
		}

		public class TestTable
		{
			[Column(IsIdentity = true)]
			public int ID { get; set; }
		}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		[Test]
		public void StandardExceptionExecuteReader([IncludeDataSources(false, ProviderName.SQLiteClassic)]
			string context)
		{
			Assert.Throws<SQLiteException>(() =>
			{
				using (var db = new DataConnection(context))
				{
					db.GetTable<TestTable>().ToList();
				}
			});
		}
#endif

		[Test, DataContextSource(false)]
		public void InterceptedExceptionExecuteReader(string context)
		{
			var ret = new Retry();

			ret.Intercept = (ex) => new DivideByZeroException("Intercepted exception", ex);

			Assert.Throws<DivideByZeroException>(() =>
			{
				using (var db = new DataConnection(context))
				{
					db.RetryPolicy = ret;
					db.GetTable<TestTable>().ToList();
				}
			});
		}
	}
}
