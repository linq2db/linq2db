using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

			var threads = new Thread[threadCount];
			var results = new Tuple<TParam, TResult, string, DbParameter[], Exception?>[threadCount];

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
							using (var threadDb = (DataConnection)GetDataContext(context))
							{
								var commandInterceptor = new SaveCommandInterceptor();
								threadDb.AddInterceptor(commandInterceptor);

								var result = queryFunc(threadDb, param);
								results[n] = Tuple.Create(param, result, threadDb.LastQuery!, commandInterceptor.Parameters, (Exception?)null);
							}
						}
						catch (Exception e)
						{
							results[n] = Tuple.Create(param, default(TResult), "", (DbParameter[]?)null, e)!;
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
				if (result.Item5 != null)
				{
					TestContext.Out.WriteLine($"Exception in query ({result.Item1}):\n\n{result.Item5}");
					throw result.Item5;
				}

				try
				{
					checkAction(result.Item2, result.Item1);
				}
				catch
				{
					var testResult = queryFunc(dc, result!.Item1);

					TestContext.Out.WriteLine($"Failed query ({result.Item1}):\n");
					if (result.Item4 != null)
					{
						var sb = new StringBuilder();
						dc.DataProvider.CreateSqlBuilder(dc.MappingSchema, dc.Options).PrintParameters(dc, sb, result.Item4.OfType<DbParameter>());
						TestContext.Out.WriteLine(sb);
					}

					TestContext.Out.WriteLine();
					TestContext.Out.WriteLine(result.Item3);

					DumpObject(result.Item2);

					DumpObject(testResult);

					throw;
				}
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
