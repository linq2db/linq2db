using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Threading;

namespace Tests.Samples
{
	[TestFixture]
	public class ExceptionInterceptTests : TestBase
	{
#if !MONO
		private class Retry : IRetryPolicy
		{
			public int Count { get; private set; }

			public TResult Execute<TResult>(Func<TResult> operation)
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

			public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default(CancellationToken))
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

		[Test, IncludeDataContextSource(false, ProviderName.SQLite)]
		public void StandardExceptionExecuteReader(string context)
		{
			Assert.Throws<SQLiteException>(() =>
			{
				using (var db = new DataConnection(context))
				{
					db.GetTable<TestTable>().ToList();
				}
			});
		}

		[Test, IncludeDataContextSource(false, ProviderName.SQLite)]
		public void InterceptedExceptionExecuteReader(string context)
		{
			var ret = new Retry();

			ret.Intercept = (ex) => { return new DivideByZeroException("Intercepted exception", ex); };

			Assert.Throws<DivideByZeroException>(() =>
			{
				using (var db = new DataConnection(context, ret))
				{
					db.GetTable<TestTable>().ToList();
				}
			});
		}
#endif
	}
}