using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;

using NUnit.Framework;

using Shouldly;

#pragma warning disable JSON002 // Allow JSON in test code for config file content.

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Pins the Windows impersonation boundary: configuration, provider loading, SQL validation and code
	/// loading run under the original process account; provider version detection, connection opening and
	/// SQL execution run in the impersonation session. The session is the test environment's recorder, so
	/// these run on any OS.
	/// </summary>
	[TestFixture]
	public sealed class ImpersonationScopeTests
	{
		const string ScriptDomAssembly = "Microsoft.SqlServer.TransactSql.ScriptDom";
		const string ColdCaseVariable     = "LINQ2DB_CLI_COLD_CASE";
		const string ColdDatabaseVariable = "LINQ2DB_CLI_COLD_SQLITE_DATABASE";

		// Nothing listens on port 1, so connection attempts fail fast.
		//
		const string UnreachableSqlServer  = "Server=127.0.0.1,1;Database=master;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2;ConnectRetryCount=0";
		const string UnreachablePostgreSql = "Host=127.0.0.1;Port=1;Username=user;Password=x;Timeout=2";

		[Test]
		public async Task QueryRejectsWriteSqlBeforeExecution()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--sql", "delete from Person");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("Query is not read-only: DeleteStatement is not allowed.");

				// Provider resolution only; the rejected SQL never reaches an impersonated run.
				//
				session.Runs.    Count.ShouldBe(1);
				session.Disposed.ShouldBeTrue();
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatementsBeforeExecution()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "secret", "--impersonate", "--sql", "select 1; select 2");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.     ShouldBe(-3);
				result.Error.        ShouldContain("Only single SQL statement is allowed.");
				session.Runs.  Count.ShouldBe(1);
				session.Disposed.    ShouldBeTrue();
			}
		}

		[Test]
		public async Task ExecuteRejectsMultipleStatementsBeforeExecution()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"user": "user",
						"password": "secret",
						"impersonate": true,
						"enableExecute": true
					}
				}
				""");

			var result = await RunCli(environment, "execute", "--config", config, "--sql", "select 1; drop table Person");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.     ShouldBe(-3);
				result.Error.        ShouldContain("Only single SQL statement is allowed.");
				session.Runs.  Count.ShouldBe(1);
				session.Disposed.    ShouldBeTrue();
			}
		}

		[Test]
		public async Task QueryValidatesSqlServerSqlOutsideImpersonation()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--impersonate-mode", "new-credentials", "--sql", "select 1 as Value");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				// The connection cannot be opened here; execution is the second run.
				//
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("SQL execution failed:");

				session.User.    ShouldBe("DOMAIN\\user");
				session.Password.ShouldBe("secret");
				session.Mode.    ShouldBe(WindowsImpersonationMode.NewCredentials);
				session.Disposed.ShouldBeTrue();
				session.Runs.    Count.ShouldBe(2);

				session.Runs[1].LoadedAtEntry.ShouldContain(ScriptDomAssembly);
				session.Runs.SelectMany(static r => r.LoadedInside).ShouldNotContain(ScriptDomAssembly);
			}
		}

		[Test]
		public async Task SqlServerVersionDetectionRunsInsideImpersonation()
		{
			var environment = new TestCliEnvironment();

			using var opens = new SqlClientOpenRecorder();

			// An unversioned provider name makes linq2db connect to detect the server version.
			//
			var result = await RunCli(environment, "query", "--provider", "SqlServer", "--connection-string", UnreachableSqlServer, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--sql", "select 1 as Value");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("SQL execution failed:");

				// One open for version detection (during provider resolution), one for execution.
				//
				opens.Sessions.Count.ShouldBe(2);
				opens.Sessions.ShouldAllBe(s => s == session);
			}
		}

		[Test]
		public async Task QueryLoadsToolAssembliesBeforeImpersonation()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "user", "--password", "secret", "--impersonate", "--output", "json", "--sql", "select Id, Name from Person");

				var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

				var toolAssemblies = Directory.GetFiles(AppContext.BaseDirectory, "*.dll")
					.Select(static f => Path.GetFileNameWithoutExtension(f))
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldBe("[{\"Id\":\"1\",\"Name\":\"original\"}]");

					session.Mode.ShouldBe(WindowsImpersonationMode.NetworkCleartext);
					session.Runs.Count.ShouldBe(2);
					session.Disposed.ShouldBeTrue();
					session.Runs.SelectMany(static r => r.LoadedInside).Where(toolAssemblies.Contains).ShouldBeEmpty();
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		/// <summary>
		/// Commands run by <see cref="NothingIsLoadedWhileImpersonating"/>, each in a new process: nothing in that
		/// process has initialized the client, so whatever the command needs would be loaded on first use.
		/// Unreachable servers still run the client's connection code and its error path (message resources).
		/// </summary>
		static readonly Dictionary<string, string[]> _coldCases = new(StringComparer.Ordinal)
		{
			["SQLite query"]                     = ["query",   "--provider", "SQLite",         "--connection-string", "Data Source=%DATABASE%;Pooling=False", "--output", "json", "--sql", "select Id, Name from Person"],
			["SQLite execute"]                   = ["execute", "--config",   "%CONFIG%",       "--output", "json-table", "--sql", "update Person set Name = 'updated' where Id = 1"],
			["SQLite schema"]                    = ["schema",  "--provider", "SQLite",         "--connection-string", "Data Source=%DATABASE%;Pooling=False"],
			["SqlServer query"]                  = ["query",   "--provider", "SqlServer",      "--connection-string", UnreachableSqlServer, "--sql", "select 1 as Value"],
			["SqlServer.2022 query"]             = ["query",   "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--sql", "select 1 as Value"],
			["SqlServer.2022 query, de-DE"]      = ["query",   "--provider", "SqlServer.2022", "--connection-string", UnreachableSqlServer, "--sql", "select 1 as Value"],
			["SqlServer schema"]                 = ["schema",  "--provider", "SqlServer",      "--connection-string", UnreachableSqlServer],
			["PostgreSQL query"]                 = ["query",   "--provider", "PostgreSQL",     "--connection-string", UnreachablePostgreSql, "--sql", "select 1 as Value"],
			["PostgreSQL.15 query"]              = ["query",   "--provider", "PostgreSQL.15",  "--connection-string", UnreachablePostgreSql, "--sql", "select 1 as Value"],
		};

		static IEnumerable<string> ColdCases => _coldCases.Keys;

		/// <summary>
		/// Runs <see cref="ColdProcess"/> for <paramref name="coldCase"/> in a new process.
		/// </summary>
		[TestCaseSource(nameof(ColdCases))]
		public async Task NothingIsLoadedWhileImpersonating(string coldCase)
		{
			var database   = CreateSqliteDatabase();
			var resultsDir = Path.Combine(Path.GetTempPath(), $"linq2db-cli-cold-{Guid.NewGuid():N}");

			try
			{
				var startInfo = new ProcessStartInfo("dotnet")
				{
					RedirectStandardOutput = true,
					RedirectStandardError  = true,
					UseShellExecute        = false,
				};

				startInfo.ArgumentList.Add(typeof(ImpersonationScopeTests).Assembly.Location);
				startInfo.ArgumentList.Add("--filter");
				startInfo.ArgumentList.Add($"FullyQualifiedName={typeof(ImpersonationScopeTests).FullName}.{nameof(ColdProcess)}");
				startInfo.ArgumentList.Add("--results-directory");
				startInfo.ArgumentList.Add(resultsDir);
				startInfo.Environment[ColdCaseVariable]     = coldCase;
				startInfo.Environment[ColdDatabaseVariable] = database;

				using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start test process.");

				var outputTask = process.StandardOutput.ReadToEndAsync();
				var errorTask  = process.StandardError.ReadToEndAsync();

				try
				{
					await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMinutes(2));
				}
				catch (TimeoutException)
				{
					process.Kill(entireProcessTree: true);
					throw;
				}

				var output = await outputTask + await errorTask;

				using (Assert.EnterMultipleScope())
				{
					process.ExitCode.ShouldBe(0, output);
					output.          ShouldContain("succeeded: 1", Case.Sensitive, output);
				}
			}
			finally
			{
				File.Delete(database);

				if (Directory.Exists(resultsDir))
					Directory.Delete(resultsDir, true);
			}
		}

		[Test, Explicit("Started in a new process by NothingIsLoadedWhileImpersonating.")]
		public async Task ColdProcess()
		{
			var coldCase = Environment.GetEnvironmentVariable(ColdCaseVariable);
			var database = Environment.GetEnvironmentVariable(ColdDatabaseVariable);

			if (coldCase == null || database == null)
				Assert.Ignore($"Runs only from {nameof(NothingIsLoadedWhileImpersonating)}.");

			// The SQL Server client ships German messages; the error below must come from them.
			//
			if (coldCase.EndsWith(", de-DE", StringComparison.Ordinal))
				CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=%DATABASE%;Pooling=False",
						"user": "user",
						"password": "secret",
						"impersonate": true,
						"enableExecute": true
					}
				}
				""".Replace("%DATABASE%", database.Replace("\\", "\\\\", StringComparison.Ordinal), StringComparison.Ordinal));

			var arguments = _coldCases[coldCase]
				.Select(a => a.Replace("%DATABASE%", database, StringComparison.Ordinal).Replace("%CONFIG%", config, StringComparison.Ordinal))
				.Concat(coldCase.Contains("execute", StringComparison.Ordinal) ? [] : ["--user", "user", "--password", "secret", "--impersonate"])
				.ToArray();

			var result  = await RunCli(environment, arguments);
			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			// Native libraries shipped with the tool or the .NET runtime. Those of the operating system (e.g. the
			// Kerberos libraries the runtime's GSSAPI shim loads on Linux) are readable by every account.
			//
			var shippedDirectories = new[] { AppContext.BaseDirectory, RuntimeEnvironment.GetRuntimeDirectory() };

			using (Assert.EnterMultipleScope())
			{
				if (coldCase.StartsWith("SQLite", StringComparison.Ordinal))
					result.ExitCode.ShouldBe(0, result.Error);
				else
					result.Error.ShouldContain(coldCase.Contains("schema", StringComparison.Ordinal) ? "Schema inspection failed:" : "SQL execution failed:");

				session.Runs.Count.ShouldBe(2);

				session.Runs.SelectMany(static r => r.LoadedInside).ShouldBeEmpty();
				session.Runs.SelectMany(static r => r.NativeModulesLoadedInside)
					.Where(m => shippedDirectories.Any(d => m.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
					.ShouldBeEmpty();

				if (coldCase.StartsWith("SQLite", StringComparison.Ordinal))
				{
					// Native SQLite is loaded before provider resolution starts.
					//
					session.Runs[0].NativeModulesAtEntry.Any(IsSqliteNative).ShouldBeTrue();
				}

				if (coldCase.EndsWith(", de-DE", StringComparison.Ordinal))
				{
					session.Runs[0].LoadedAtEntry.ShouldContain("Microsoft.Data.SqlClient.resources");
					result.Error.ShouldContain("SQL Server", Case.Sensitive);
					result.Error.ShouldNotContain("A network-related or instance-specific error", Case.Sensitive);
				}
			}

			// The native library itself, not managed assemblies such as SQLitePCLRaw.provider.e_sqlite3.dll, which
			// Windows lists among process modules too.
			//
			static bool IsSqliteNative(string module)
			{
				var name = Path.GetFileNameWithoutExtension(module);

				return name.Equals("e_sqlite3", StringComparison.OrdinalIgnoreCase) || name.Equals("libe_sqlite3", StringComparison.OrdinalIgnoreCase);
			}
		}

		[Test]
		public async Task ExecuteWritesNoticeBeforeExecution()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var config = AddConfigFile(environment, """
					{
						"default": {
							"provider": "SQLite",
							"connectionString": "Data Source=%DATABASE%;Pooling=False",
							"user": "user",
							"password": "secret",
							"impersonate": true,
							"enableExecute": true
						}
					}
					""".Replace("%DATABASE%", database.Replace("\\", "\\\\", StringComparison.Ordinal), StringComparison.Ordinal));

				var result = await RunCli(environment, "execute", "--config", config, "--output", "json-table", "--sql", "update Person set Name = 'updated' where Id = 1");

				var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldContain("\"recordsAffected\":1");

					session.Runs.Count.ShouldBe(2);
					session.Runs[0].ErrorOutputAtEntry.ShouldNotContain("Executing write-capable SQL");
					session.Runs[1].ErrorOutputAtEntry.ShouldContain("Executing write-capable SQL because profile 'default' has enableExecute=true. Provider: SQLite.");
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task SchemaReadsDatabaseInsideImpersonation()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var result = await RunCli(environment, "schema", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "user", "--password", "secret", "--impersonate", "--impersonate-mode", "interactive");

				var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldContain("\"Person\"");

					session.Mode.      ShouldBe(WindowsImpersonationMode.Interactive);
					session.Runs.Count.ShouldBe(2);
					session.Disposed.  ShouldBeTrue();
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task OverlappingCallsUseSeparateSessions()
		{
			var database = CreateSqliteDatabase();

			try
			{
				var first  = new TestCliEnvironment();
				var second = new TestCliEnvironment();

				var results = await Task.WhenAll(
					Task.Run(() => RunCli(first,  "query", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "first",  "--password", "one", "--impersonate", "--sql", "select Id from Person")),
					Task.Run(() => RunCli(second, "query", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "second", "--password", "two", "--impersonate", "--sql", "select Name from Person")));

				var firstSession  = first. ImpersonationSessions.ShouldHaveSingleItem();
				var secondSession = second.ImpersonationSessions.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					results[0].ExitCode.ShouldBe(0, results[0].Error);
					results[1].ExitCode.ShouldBe(0, results[1].Error);
					results[0].Output.  ShouldContain("\"Id\"");
					results[1].Output.  ShouldContain("\"Name\"");

					firstSession. User.ShouldBe("first");
					secondSession.User.ShouldBe("second");
					firstSession. Runs.Count.ShouldBe(2);
					secondSession.Runs.Count.ShouldBe(2);
					RecordingImpersonationSession.Current.ShouldBeNull();
				}
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task QueryWithoutImpersonateDoesNotStartSession()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "secret", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.                  ShouldBe(0, result.Error);
				environment.ImpersonationSessions.ShouldBeEmpty();
			}
		}

		/// <summary>
		/// Records, for every SqlClient connection open, the impersonation session it happened in.
		/// </summary>
		sealed class SqlClientOpenRecorder : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
		{
			readonly IDisposable       _allListeners;
			readonly List<IDisposable> _subscriptions = new();

			public List<RecordingImpersonationSession?> Sessions { get; } = new();

			public SqlClientOpenRecorder()
			{
				_allListeners = DiagnosticListener.AllListeners.Subscribe(this);
			}

			public void OnNext(DiagnosticListener value)
			{
				if (value.Name == "SqlClientDiagnosticListener")
					lock (_subscriptions)
						_subscriptions.Add(value.Subscribe(this));
			}

			public void OnNext(KeyValuePair<string, object?> value)
			{
				if (value.Key.EndsWith("WriteConnectionOpenBefore", StringComparison.Ordinal))
					lock (Sessions)
						Sessions.Add(RecordingImpersonationSession.Current);
			}

			public void OnCompleted()
			{
			}

			public void OnError(Exception error)
			{
			}

			public void Dispose()
			{
				_allListeners.Dispose();

				lock (_subscriptions)
					foreach (var subscription in _subscriptions)
						subscription.Dispose();
			}
		}

		private static string CreateSqliteDatabase()
		{
			var fileName     = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"impersonation-{Guid.NewGuid():N}.db");
			var dataProvider = DataConnection.GetDataProvider("SQLite", $"Data Source={fileName};Pooling=False")!;
			using var db     = new DataConnection(new DataOptions().UseConnectionString(dataProvider, $"Data Source={fileName};Pooling=False"));

			db.Execute("""
				create table Person
				(
					Id   integer not null primary key,
					Name text    not null
				)
				""");

			db.Execute("insert into Person (Id, Name) values (1, 'original')");

			return fileName;
		}

		private static string AddConfigFile(TestCliEnvironment environment, string content)
		{
			var fileName = $"impersonation-{environment.Files.Count + 1}.json";

			environment.Files.Add(fileName, content);

			return fileName;
		}

		private static async Task<CliResult> RunCli(TestCliEnvironment environment, params string[] arguments)
		{
			var exitCode = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
