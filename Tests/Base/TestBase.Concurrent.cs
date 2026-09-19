using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;

using LinqToDB.Data;

using NUnit.Framework;

namespace Tests
{
	public partial class TestBase
	{
		static readonly JsonSerializerOptions _dumpObjectOptions = new JsonSerializerOptions { WriteIndented = true };

		/// <summary>What one <see cref="ConcurrentRunner{TParam,TResult}"/> thread recorded. Everything but
		/// <see cref="Param"/> is only populated for a failure - a passing result is dropped immediately
		/// rather than retained until the run finishes.</summary>
		sealed record ConcurrentRunOutcome<TParam, TResult>(
			TParam         Param,
			TResult?       Result,
			string         LastQuery,
			DbParameter[]? Parameters,
			Exception?     Failure);

		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		protected void ConcurrentRunner<TParam, TResult>(DataConnection dc, string context, int threadsPerParam, Func<DataConnection, TParam, TResult> queryFunc,
			Action<TResult, TParam> checkAction, params TParam[] parameters)
		{
			var threadCount = threadsPerParam * parameters.Length;
			if (threadCount <= 0)
				throw new InvalidOperationException();

			// maximum Provider pool count
			const int poolCount = 10;

			using var semaphore = new Semaphore(0, poolCount);

			var threads     = new Thread[threadCount];
			var results     = new ConcurrentRunOutcome<TParam, TResult>[threadCount];
			var queryFailed = new bool[threadCount];

			for (var i = 0; i < threadCount; i++)
			{
				var param = parameters[i % parameters.Length];
				var n = i;
				threads[i] = new Thread(() =>
				{
					semaphore.WaitOne();
					try
					{
						try
						{
							using var threadDb = (DataConnection)GetDataContext(context);
							var commandInterceptor = new SaveCommandInterceptor();
							threadDb.AddInterceptor(commandInterceptor);

							var result = queryFunc(threadDb, param);

							// Check here rather than after every thread joined: a passing result is dropped
							// immediately instead of being retained until the whole run finishes. Retaining
							// all of them peaked at ~570MB for one EagerLoadMultiLevel lane, and the SQL
							// Server legs run four lanes at once.
							try
							{
								checkAction(result, param);
								results[n] = new ConcurrentRunOutcome<TParam, TResult>(param, default, "", null, null);
							}
							catch (Exception checkFailure)
							{
								results[n] = new ConcurrentRunOutcome<TParam, TResult>(param, result, threadDb.LastQuery!, commandInterceptor.Parameters, checkFailure);
							}
						}
						catch (Exception e)
						{
							results[n] = new ConcurrentRunOutcome<TParam, TResult>(param, default, "", null, e);
							queryFailed[n] = true;
						}

					}
					finally
					{
						semaphore.Release();
					}
				});
			}

			for (var i = 0; i < threads.Length; i++)
			{
				threads[i].Start();
			}

			semaphore.Release(poolCount);

			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Join();
			}

			for (var i = 0; i < threads.Length; i++)
			{
				var result = results[i];
				if (queryFailed[i])
				{
					TestContext.Out.WriteLine($"Exception in query ({result.Param}):\n\n{result.Failure}");

					// Capture rather than a bare throw, as the check-failure path below does: this one reports
					// the provider's own exception, where the worker thread's original stack is the most
					// useful part of the report and a rethrow would reset it to this line.
					ExceptionDispatchInfo.Capture(result.Failure!).Throw();
				}

				if (result.Failure == null)
					continue;

				// Re-run the query on the shared connection purely to dump what it returns now. It is the
				// same query that just produced the failing result, so it throwing is the likely case rather
				// than an exotic one - and an escape here would replace the check failure we are reporting.
				// (It would have on master too, where this ran unguarded inside the catch block.)
				var reran      = true;
				var testResult = default(TResult);

				try
				{
					testResult = queryFunc(dc, result.Param);
				}
				catch (Exception rerunFailure)
				{
					reran = false;
					TestContext.Out.WriteLine($"Diagnostic re-run also failed ({result.Param}):\n\n{rerunFailure}");
				}

				TestContext.Out.WriteLine($"Failed query ({result.Param}):\n");
				if (result.Parameters != null)
				{
					var sb = new StringBuilder();
					dc.DataProvider.CreateSqlBuilder(dc.MappingSchema, dc.Options).PrintParameters(dc, sb, result.Parameters.OfType<DbParameter>());
					TestContext.Out.WriteLine(sb);
				}

				TestContext.Out.WriteLine();
				TestContext.Out.WriteLine(result.LastQuery);

				DumpObject(result.Result);

				if (reran)
					DumpObject(testResult);

				// The original check failure is what the test is reporting, so it is rethrown whether or not
				// the diagnostic re-run above succeeded.
				ExceptionDispatchInfo.Capture(result.Failure).Throw();
			}
		}

		void DumpObject(object? obj)
		{
			if (obj == null)
				return;

			TestContext.Out.WriteLine(JsonSerializer.Serialize(obj, _dumpObjectOptions));
		}
	}
}
