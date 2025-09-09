using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data.RetryPolicy;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Samples
{
	[TestFixture]
	public class ExceptionInterceptTests : TestBase
	{
		private sealed class Retry : IRetryPolicy
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

		[Test]
		public void StandardExceptionExecuteReader([IncludeDataSources(TestProvName.AllSQLiteClassic)]
			string context)
		{
			Assert.Throws<SQLiteException>(() =>
			{
				using (var db = GetDataContext(context))
				{
					db.GetTable<TestTable>().ToList();
				}
			});
		}

		[Test]
		public void InterceptedExceptionExecuteReader([DataSources(false)] string context)
		{
			var ret = new Retry();

			ret.Intercept = (ex) => new DivideByZeroException("Intercepted exception", ex);

			Assert.Throws<DivideByZeroException>(() =>
			{
				using (var db = GetDataContext(context, o => o.UseRetryPolicy(ret)))
				{
					db.GetTable<TestTable>().ToList();
				}
			});
		}
	}
}
