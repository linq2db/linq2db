using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Data;

using NUnit.Framework;
using NUnit.Framework.Internal;

using Tests.Model;

namespace Tests
{
	public partial class TestBase
	{
		const int TRACES_LIMIT = 50000;

		protected static string? LastQuery { get; set; }

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
					if (ctx.Get<bool>(CustomTestContext.TRACE_DISABLED) != true)
					{
						var trace = ctx.Get<StringBuilder>(CustomTestContext.TRACE);
						if (trace == null)
						{
							trace = new StringBuilder();
							ctx.Set(CustomTestContext.TRACE, trace);
						}

						lock (trace)
							trace.AppendLine($"{name}: {message}");

						if (traceCount < TRACES_LIMIT || level == TraceLevel.Error)
						{
							ctx.Set(CustomTestContext.LIMITED, true);
							TestContext.Out.WriteLine("{0}: {1}", name, message);
							Debug.WriteLine(message, name);
						}

						traceCount++;
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
			// SequentialAccess-enabled provider setup
			var (provider, _) = NUnitUtils.GetContext(TestExecutionContext.CurrentContext.CurrentTest);
			if (provider?.IsAnyOf(TestProvName.AllSqlServerSequentialAccess) == true)
			{
				Configuration.OptimizeForSequentialAccess = true;
			}
		}

		[TearDown]
		public virtual void OnAfterTest()
		{
			// SequentialAccess-enabled provider cleanup
			var (provider, _) = NUnitUtils.GetContext(TestExecutionContext.CurrentContext.CurrentTest);
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

			BaselinesManager.Dump();

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

			CustomTestContext.Release();
		}
	}
}
