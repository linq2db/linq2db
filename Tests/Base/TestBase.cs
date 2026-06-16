using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using LinqToDB.Common;
using LinqToDB.Data;

using NUnit.Framework;
using NUnit.Framework.Internal;

using Tests.Model;

namespace Tests
{
	public abstract partial class TestBase
	{
		const int TRACES_LIMIT = 50000;

		// Per-test (stored in CustomTestContext) so parallel tests don't clobber each other's
		// last-executed-query; the trace sink writes it and tests read it back on their own thread.
		protected static string? LastQuery
		{
			get => CustomTestContext.Get().Get<string?>(CustomTestContext.LASTQUERY);
			set => CustomTestContext.Get().Set(CustomTestContext.LASTQUERY, value);
		}

		// Set when the parallel dispatcher is installed (TestsInitialization). Gates the
		// per-provider database-readiness wait below, so serial / filtered runs are unaffected.
		public static bool ParallelExecutionEnabled;

		// Under parallel execution a provider's tests wait until its CreateDatabase has populated
		// the schema. CreateDatabase runs off the provider lane (see ParallelDatabaseWorkItemDispatcher),
		// so this never deadlocks the lane. Keyed by the bare provider context (remote suffix stripped).
		static readonly ConcurrentDictionary<string, ManualResetEventSlim> _databaseReady =
			new ConcurrentDictionary<string, ManualResetEventSlim>(StringComparer.OrdinalIgnoreCase);

		static ManualResetEventSlim DatabaseReadyGate(string provider)
			=> _databaseReady.GetOrAdd(provider, static _ => new ManualResetEventSlim(false));

		// Signalled by CreateDatabase once a provider's schema exists (called even on failure so
		// waiters don't hang).
		public static void MarkDatabaseReady(string provider) => DatabaseReadyGate(provider).Set();

		static TestBase()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

			try
			{
				TestContext.Out.WriteLine("Tests started in {0}...", Environment.CurrentDirectory);
				TestContext.Out.WriteLine("CLR Version: {0}...", Environment.Version);

				var traceCount = 0;

				// trigger settings preload
				_ = TestConfiguration.DefaultProvider;

				DataConnection.WriteTraceLine = (message, name, level) =>
				{
					if (message?.StartsWith("BeforeExecute") == true)
					{
						LastQuery = message;
						BaselinesManager.LogQuery(message);
					}

					var ctx = CustomTestContext.Get();
					if (!ctx.Get<bool>(CustomTestContext.TRACE_DISABLED))
					{
						static StringBuilder GetTraceBuilder(CustomTestContext ctx)
						{
							var builder = ctx.Get<StringBuilder>(CustomTestContext.TRACE);
							if (builder is not null)
								return builder;

							builder = new();
							ctx.Set(CustomTestContext.TRACE, builder);
							return builder;
						}

						var trace = GetTraceBuilder(ctx);

						// necessary for multi-threaded tests like `Issue1398Tests.cs`
						lock (trace)
							trace.AppendLine(CultureInfo.InvariantCulture, $"{name}: {message}");

						if (traceCount < TRACES_LIMIT || level == TraceLevel.Error)
						{
							ctx.Set(CustomTestContext.LIMITED, true);
							TestContext.Out.WriteLine("{0}: {1}", name, message);
							Debug.WriteLine(message, name);
						}

						Interlocked.Increment(ref traceCount);
					}
				};

				Configuration.Linq.TraceMapperExpression = false;
				// Configuration.Linq.GenerateExpressionTest  = true;

#if NETFRAMEWORK
				var assemblyPath = Path.GetDirectoryName(typeof(TestBase).Assembly.CodeBase.Replace("file:///", ""))!;

				// this is needed for machine without GAC-ed sql types (e.g. machine without SQL Server installed or CI)
				try
				{
					SqlServerTypes.Utilities.LoadNativeAssemblies(assemblyPath);
				}
				catch // this can fail during tests discovering with NUnitTestAdapter
				{
					// ignore
				}
#else
				var assemblyPath = Path.GetDirectoryName(typeof(TestBase).Assembly.Location)!;
#endif

				Environment.CurrentDirectory = assemblyPath;

				TestExternals.Log($"CurrentDirectory          : {Environment.CurrentDirectory}");

				DatabaseUtils.CopyDatabases();

#if NETFRAMEWORK
				LinqToDB.Remote.LinqService.TypeResolver = str =>
				{
					return str switch
					{
						"Tests.Model.Gender" => typeof(Gender),
						"Tests.Model.Person" => typeof(Person),
						_ => null,
					};
				};
#endif
			}
			catch (Exception ex)
			{
				TestUtils.Log(ex);
				throw;
			}
		}

		static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception ex)
				TestUtils.Log(ex);
			else
				TestUtils.Log(e.ExceptionObject.ToString());
		}

		[SetUp]
		public virtual void OnBeforeTest()
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;
			var (provider, isRemote) = NUnitUtils.GetContext(test);

			// establish a fresh per-test context before anything in setup/test can log
			CustomTestContext.Begin(isRemote, provider);

			// Under parallel execution, wait until this provider's database has been created
			// (CreateDatabase runs off-lane and signals readiness). Serial / filtered runs skip this.
			if (ParallelExecutionEnabled && provider != null && !NUnitUtils.IsCreateDatabase(test))
				DatabaseReadyGate(provider).Wait(TimeSpan.FromMinutes(2));

			// SequentialAccess-enabled provider setup
			if (provider?.IsAnyOf(TestProvName.AllSqlServerSequentialAccess) == true)
			{
				Configuration.OptimizeForSequentialAccess = true;
			}
		}

		[TearDown]
		public virtual void OnAfterTest()
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;
			var (provider, isRemote) = NUnitUtils.GetContext(test);

			// release any tests waiting on this provider's database: signalled after CreateDatabase
			// runs (here, not inside the try, so a failed CreateDatabase still unblocks waiters)
			if (provider != null && NUnitUtils.IsCreateDatabase(test))
				MarkDatabaseReady(provider);

			try
			{
				// SequentialAccess-enabled provider cleanup
				if (provider?.IsAnyOf(TestProvName.AllSqlServerSequentialAccess) == true)
				{
					Configuration.OptimizeForSequentialAccess = false;
				}

				if (provider?.IsAnyOf(TestProvName.AllSapHana) == true)
				{
					using (new DisableLogging())
					using (new DisableBaseline("isn't baseline query"))
					using (var db = new TestDataConnection(provider))
					{
						// release memory
						db.Execute("ALTER SYSTEM CLEAR SQL PLAN CACHE");
					}
				}

				if (provider != null)
					AssertState(provider);

				BaselinesManager.Dump(isRemote);

				var ctx = CustomTestContext.Get();

				var trace = ctx.Get<StringBuilder>(CustomTestContext.TRACE);

				if (trace != null && TestContext.CurrentContext.Result.FailCount > 0 && ctx.Get<bool>(CustomTestContext.LIMITED))
				{
					// we need to set ErrorInfo.Message element text
					// because Azure displays only ErrorInfo node data
					TestExecutionContext.CurrentContext.CurrentResult.SetResult(
						TestExecutionContext.CurrentContext.CurrentResult.ResultState,
						TestExecutionContext.CurrentContext.CurrentResult.Message + "\r\n" + trace.ToString(),
						TestExecutionContext.CurrentContext.CurrentResult.StackTrace);
				}
			}
			finally
			{
				CustomTestContext.Release();
			}
		}
	}
}
