using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
	/// Pins the Windows impersonation boundary: configuration, external provider loading and SQL validation
	/// run under the original process account; provider version detection, connection opening and
	/// SQL execution run in the impersonation session. The session is the test environment's recorder, so
	/// these run on any OS.
	/// </summary>
	[TestFixture]
	public sealed class ImpersonationScopeTests
	{
		// Nothing listens on port 1, so connection attempts fail fast.
		//
		const string UnreachableSqlServer = "Server=127.0.0.1,1;Database=master;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2;ConnectRetryCount=0";

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
		public async Task QueryPassesCredentialsAndModeToSession()
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
			}
		}

		[Test]
		public async Task SqlServerVersionDetectionRunsInsideImpersonation()
		{
			var environment = new TestCliEnvironment();

			using var opens = new SqlClientOpenRecorder();

			// An unversioned provider name makes linq2db connect to detect the server version.
			//
			var connectionString = $"{UnreachableSqlServer};Application Name=linq2db-{Guid.NewGuid():N}";
			var result           = await RunCli(environment, "query", "--provider", "SqlServer", "--connection-string", connectionString, "--user", "DOMAIN\\user", "--password", "secret", "--impersonate", "--sql", "select 1 as Value");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("SQL execution failed:");

				// One open for version detection (during provider resolution) and one for execution, both in the session.
				//
				opens.Sessions.Count.ShouldBe(2);
				opens.Sessions.ShouldAllBe(s => s == session);
			}
		}

		[Test]
		public async Task QueryReadsRowsInDefaultModeSession()
		{
			var environment = new TestCliEnvironment();
			var database    = CreateSqliteDatabase();

			try
			{
				var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", $"Data Source={database};Pooling=False", "--user", "user", "--password", "secret", "--impersonate", "--output", "json", "--sql", "select Id, Name from Person");

				var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0, result.Error);
					result.Output.  ShouldBe("[{\"Id\":\"1\",\"Name\":\"original\"}]");

					session.Mode.ShouldBe(WindowsImpersonationMode.NetworkCleartext);
					session.Runs.Count.ShouldBe(2);
					session.Disposed.ShouldBeTrue();
				}
			}
			finally
			{
				File.Delete(database);
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

		[Test]
		public async Task QueryReportsFailedLogonBeforeValidation()
		{
			var environment = new TestCliEnvironment { StartImpersonationException = new Win32Exception(1326, "Windows impersonation logon failed.") };

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "wrong", "--impersonate", "--sql", "delete from Person");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("Windows impersonation logon failed.");
				result.Error.   ShouldNotContain("not read-only");
			}
		}

		[Test]
		public async Task UnknownProviderDisposesSession()
		{
			var environment = new TestCliEnvironment();

			var result = await RunCli(environment, "query", "--provider", "NoSuchProvider", "--connection-string", "Data Source=:memory:", "--user", "user", "--password", "secret", "--impersonate", "--sql", "select 1 as Value");

			var session = environment.ImpersonationSessions.ShouldHaveSingleItem();

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.   ShouldBe(-3);
				result.Error.      ShouldContain("Cannot create database provider 'NoSuchProvider'");
				session.Runs.Count.ShouldBe(1);
				session.Disposed.  ShouldBeTrue();
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
			var options      = new DataOptions()
				.UseConnectionString(dataProvider, $"Data Source={fileName};Pooling=False");

			using var db = new DataConnection(options);

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
			var exitCode = await new LinqToDBCliController()
				.Execute(arguments, environment)
				.ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
