using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.Data;

using NUnit.Framework;

#pragma warning disable JSON002 // Allow JSON in test code for config file content.

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class QueryCommandTests
	{
		[Test]
		public async Task QueryRequiresSqlOrSqlFile()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Either '--sql' or '--sql-file' option must be specified."));
			}
		}

		[Test]
		public async Task QueryRejectsSqlAndSqlFileTogether()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--sql-file' conflicts with other option(s): --sql"));
			}
		}

		[Test]
		public async Task QueryRequiresProvider()
		{
			var result = await RunCli("query", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--provider' must be specified."));
			}
		}

		[Test]
		public async Task QueryRequiresConnectionString()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--connection-string' must be specified."));
			}
		}

		[Test]
		public async Task QueryImpersonateRequiresUserAndPassword()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--impersonate", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--impersonate' requires resolved '--user' and '--password' values."));
			}
		}

		[Test]
		public async Task QueryRejectsMissingProviderLocation()
		{
			var result = await RunCli("query", "--provider", "DB2", "--connection-string", "Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!", "--sql", "select 1 from SYSIBM.SYSDUMMY1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Cannot locate IBM.Data.Db2.dll provider assembly."));
				Assert.That(result.Error,    Does.Contain("Due to huge size of it, we don't include Net.IBM.Data.Db2 provider into installation."));
				Assert.That(result.Error,    Does.Contain("--provider-location <path_to_assembly>"));
			}
		}

		[Test]
		public async Task QueryReportsProviderAliasSuggestion()
		{
			var result = await RunCli("query", "--provider", "Oracle.19.Managed", "--connection-string", "Data Source=localhost/XE;User Id=test;Password=test", "--sql", "select 1 from dual");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain("Cannot create database provider 'Oracle.19.Managed'."));
				Assert.That(result.Error,    Does.Contain("looks like a test data source alias"));
				Assert.That(result.Error,    Does.Contain("Oracle.Managed"));
			}
		}

		[Test]
		public async Task QueryRejectsMissingProviderLocationForInformixDB2Provider()
		{
			var result = await RunCli("query", "--provider", "Informix.DB2", "--connection-string", "Server=localhost:9189;Database=testdatadb2;userid=informix;password=in4mix", "--sql", "select 1 as Value from systables where tabid = 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Cannot locate IBM.Data.Db2.dll provider assembly."));
				Assert.That(result.Error,    Does.Contain("--provider-location <path_to_assembly>"));
			}
		}

		[Test]
		public async Task QueryRejectsMissingExplicitProviderLocationFile()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--provider-location", "missing\\provider.dll", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain("Provider assembly 'missing\\provider.dll' not found."));
			}
		}

		[Test]
		public async Task QueryRejectsDB2ProviderLocationWithoutDB2Factory()
		{
			var providerLocation = typeof(QueryCommandTests).Assembly.Location;
			var result           = await RunCli("query", "--provider", "DB2", "--provider-location", providerLocation, "--connection-string", "Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!", "--sql", "select 1 from SYSIBM.SYSDUMMY1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain($"Provider assembly '{providerLocation}' doesn't contain DB2Factory type."));
			}
		}

		[Test]
		public async Task QueryAcceptsSql()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsDml()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "update Person set Name = 'test'");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'UPDATE' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsDdl()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'DROP' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsProcedureCalls()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "call DoWork()");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query is not read-only: token 'CALL' is not allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatements()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1; select 2");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Only single SQL statement is allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsMultipleStatementsEvenWhenUnsafeSqlAllowed()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "allow"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1; drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Only single SQL statement is allowed."));
			}
		}

		[Test]
		public async Task QueryRejectsUnsafeSqlConfirmationWhenUnsafeSqlPolicyDenies()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--allow-unsafe-sql", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--allow-unsafe-sql' cannot be used because unsafe SQL policy is 'deny'."));
			}
		}

		[Test]
		public async Task QueryConfirmUnsafeSqlPolicyRequiresUnsafeSqlConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "confirm"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Unsafe SQL requires '--allow-unsafe-sql'"));
			}
		}

		[Test]
		public async Task QueryConfirmUnsafeSqlPolicyAllowsUnsafeSqlWithConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "confirm"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--allow-unsafe-sql", "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query execution failed"));
				Assert.That(result.Error,    Does.Not.Contain("token 'DROP' is not allowed"));
			}
		}

		[Test]
		public async Task QueryAllowUnsafeSqlPolicyAllowsUnsafeSqlWithoutConfirmation()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "allow"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "drop table Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Query execution failed"));
				Assert.That(result.Error,    Does.Not.Contain("token 'DROP' is not allowed"));
			}
		}

		[Test]
		public async Task QueryIgnoresForbiddenTokensInStringsAndComments()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 'drop table' as Value -- update table");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"drop table\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public void QueryGuardAllowsSqlServerSelect()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = ReadOnlySqlGuard.Validate(provider, "select 1 as Value");

			Assert.That(result.IsAllowed, Is.True);
		}

		[Test]
		public void QueryGuardRejectsSqlServerDml()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = ReadOnlySqlGuard.Validate(provider, "update dbo.Person set Name = 'test'");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsAllowed, Is.False);
				Assert.That(result.Error,  Does.Contain("UpdateStatement"));
			}
		}

		[Test]
		public void QueryGuardRejectsSqlServerExecute()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = ReadOnlySqlGuard.Validate(provider, "exec dbo.DoWork");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsAllowed, Is.False);
				Assert.That(result.Error,  Does.Contain("EXECUTE is not allowed"));
			}
		}

		[Test]
		public void QueryGuardRejectsSqlServerSelectInto()
		{
			var provider = DataConnection.GetDataProvider("SqlServer", "Server=.;Database=test;Trusted_Connection=True")!;

			var result = ReadOnlySqlGuard.Validate(provider, "select * into dbo.NewPerson from dbo.Person");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.IsAllowed, Is.False);
				Assert.That(result.Error,  Does.Contain("SELECT INTO is not allowed"));
			}
		}

		[Test]
		public void QueryHonorsCancellationToken()
		{
			using var cancellation = new CancellationTokenSource();

			cancellation.Cancel();

			Assert.ThrowsAsync<OperationCanceledException>(async () =>
				await new LinqToDBCliController()
					.Execute(
						["query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1"],
						new TestCliEnvironment(),
						cancellation.Token)
					.AsTask()
					.ConfigureAwait(false));
		}

		[Test]
		public async Task QueryAcceptsSqlFile()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("query.sql", "select 'test' as Value");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"test\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryResolvesEnvironmentVariablesInSqlFilePath()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("QUERY_DIR", "queries");
			environment.Files.Add("queries\\query.sql", "select 'test' as Value");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "%QUERY_DIR%\\query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"test\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsMissingSqlFile()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("SQL file 'query.sql' not found."));
			}
		}

		[Test]
		public async Task QueryRejectsProfileWithoutConfig()
		{
			var result = await RunCli("query", "--profile", "uat", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--profile' requires option '--config'."));
			}
		}

		[Test]
		public async Task QueryAcceptsOutputOptions()
		{
			var environment = new TestCliEnvironment();
			var result      = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "query.csv", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"Value{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryResolvesEnvironmentVariablesInOutputFilePath()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("OUTPUT_DIR", "output");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "${OUTPUT_DIR}\\query.csv", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["output\\query.csv"], Is.EqualTo($"Value{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryCsvEscapesSpecialCharacters()
		{
			var environment = new TestCliEnvironment();
			var result      = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "query.csv", "--sql", "select 'a,b' as [Comma,Name], 'a\"b' as QuoteValue, 'a' || char(10) || 'b' as MultilineValue, '' as EmptyValue, null as NullValue");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"\"Comma,Name\",QuoteValue,MultilineValue,EmptyValue,NullValue{Environment.NewLine}\"a,b\",\"a\"\"b\",\"a\nb\",\"\",{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryRejectsExistingOutputFileWithoutOverwrite()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("query.csv", "existing");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "query.csv", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("Output file 'query.csv' already exists."));
				Assert.That(result.Error,    Does.Contain("--overwrite"));
				Assert.That(environment.Files["query.csv"], Is.EqualTo("existing"));
			}
		}

		[Test]
		public async Task QueryOverwritesExistingOutputFileWithOverwrite()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("query.csv", "existing");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "csv", "--output-file", "query.csv", "--overwrite", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"Value{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryJsonRejectsDuplicateColumnNames()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value, 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Error,    Does.Contain("JSON output requires unique column names."));
				Assert.That(result.Error,    Does.Contain("Duplicate column name 'Value' found."));
				Assert.That(result.Error,    Does.Contain("Use explicit SQL aliases"));
				Assert.That(result.Error,    Does.Contain("json-table"));
			}
		}

		[Test]
		public async Task QueryJsonAcceptsExplicitColumnAliases()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value1, 2 as Value2");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value1\":\"1\""));
				Assert.That(result.Output,   Does.Contain("\"Value2\":\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryJsonTableAcceptsDuplicateColumnNames()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "json-table", "--sql", "select 1 as Value, 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"rowCount\":1"));
				Assert.That(result.Output,   Does.Contain("\"truncated\":false"));
				Assert.That(result.Output,   Does.Contain("\"columns\":["));
				Assert.That(result.Output,   Does.Contain("\"ordinal\":0"));
				Assert.That(result.Output,   Does.Contain("\"ordinal\":1"));
				Assert.That(result.Output,   Does.Contain("\"name\":\"Value\""));
				Assert.That(result.Output,   Does.Contain("\"fieldType\":\"System.Int64\""));
				Assert.That(result.Output,   Does.Contain("\"providerSpecificFieldType\":\"System.Int64\""));
				Assert.That(result.Output,   Does.Contain("\"dataTypeName\":\"INTEGER\""));
				Assert.That(result.Output,   Does.Contain("\"rows\":["));
				Assert.That(result.Output,   Does.Contain("\"1\""));
				Assert.That(result.Output,   Does.Contain("\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryAcceptsJsonTableConfigOutput()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "json-table"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"rowCount\":1"));
				Assert.That(result.Output,   Does.Contain("\"columns\":["));
				Assert.That(result.Output,   Does.Contain("\"rows\":["));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryAppliesDefaultMaxRows()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql", "with recursive c(Value) as (select 1 union all select Value + 1 from c where Value < 1001) select Value from c");
			var rowCount = result.Output.Split("\"Value\":", StringSplitOptions.None).Length - 1;

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(rowCount,        Is.EqualTo(1000));
				Assert.That(result.Error,    Does.Contain("Query result truncated to 1000 row(s)."));
				Assert.That(result.Error,    Does.Contain("--max-rows"));
			}
		}

		[Test]
		public async Task QueryAppliesCliMaxRows()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--max-rows", "1", "--sql", "select 1 as Value union all select 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Output,   Does.Not.Contain("\"Value\":\"2\""));
				Assert.That(result.Error,    Does.Contain("Query result truncated to 1 row(s)."));
			}
		}

		[Test]
		public async Task QueryMaxRowsZeroDisablesCliRowLimit()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--max-rows", "0", "--sql", "select 1 as Value union all select 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Output,   Does.Contain("\"Value\":\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryAppliesConfigMaxRows()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"maxRows": 1
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value union all select 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Output,   Does.Not.Contain("\"Value\":\"2\""));
				Assert.That(result.Error,    Does.Contain("Query result truncated to 1 row(s)."));
			}
		}

		[Test]
		public async Task QueryMaxRowsZeroDisablesConfigRowLimit()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"maxRows": 0
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value union all select 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Output,   Does.Contain("\"Value\":\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsInvalidMaxRowsOption()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--max-rows", "-1", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--max-rows' must be a non-negative integer row count."));
			}
		}

		[Test]
		public async Task QueryRejectsInvalidConfigMaxRows()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"maxRows": -1
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'maxRows' must be a non-negative integer row count."));
			}
		}

		[Test]
		public async Task QueryJsonTableReportsTruncationInOutput()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--output", "json-table", "--max-rows", "1", "--sql", "select 1 as Value union all select 2 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"rowCount\":1"));
				Assert.That(result.Output,   Does.Contain("\"truncated\":true"));
				Assert.That(result.Output,   Does.Contain("\"1\""));
				Assert.That(result.Output,   Does.Not.Contain("\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryAcceptsTimeoutOptions()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--command-timeout", "30", "--lock-timeout", "5", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryTimeoutZeroDisablesTimeoutOptions()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--command-timeout", "0", "--lock-timeout", "0", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsInvalidTimeoutOption()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--command-timeout", "slow", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--command-timeout' must be a non-negative integer number of seconds."));
			}
		}

		[Test]
		public async Task QueryAcceptsDefaultConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"commandTimeout": 30,
						"lockTimeout": "5",
						"maxRows": 1000,
						"output": "csv",
						"outputFile": "query.csv"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["query.csv"], Is.EqualTo($"1{Environment.NewLine}1{Environment.NewLine}"));
			}
		}

		[Test]
		public async Task QueryResolvesConfigRelativePathsFromConfigDirectory()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("config\\query.json", """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "csv",
						"outputFile": "query.csv"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", "config\\query.json", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["config\\query.csv"], Is.EqualTo($"1{Environment.NewLine}1{Environment.NewLine}"));
				Assert.That(environment.Files.ContainsKey("query.csv"), Is.False);
			}
		}

		[Test]
		public async Task QueryResolvesCommandLinePathsFromApplicationCurrentDirectory()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("config\\query.json", """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "csv",
						"outputFile": "config.csv"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", "config\\query.json", "--output-file", "cli.csv", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["cli.csv"], Is.EqualTo($"1{Environment.NewLine}1{Environment.NewLine}"));
				Assert.That(environment.Files.ContainsKey("config\\cli.csv"), Is.False);
				Assert.That(environment.Files.ContainsKey("config\\config.csv"), Is.False);
			}
		}

		[Test]
		public async Task QueryResolvesConfigRelativeProviderLocationFromConfigDirectory()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("config\\query.json", """
				{
					"default": {
						"provider": "SQLite",
						"providerLocation": "providers\\MySql.Data.dll",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", "config\\query.json", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-3));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain("Provider assembly 'config\\providers\\MySql.Data.dll' not found."));
			}
		}

		[Test]
		public async Task QueryResolvesEnvironmentVariablesInConfigPath()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("CONFIG_DIR", "config");
			environment.Files.Add("config\\query.json", """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", "${CONFIG_DIR}\\query.json", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryResolvesEnvironmentVariablesInProviderLocationPath()
		{
			var environment      = new TestCliEnvironment();
			var providerLocation = typeof(QueryCommandTests).Assembly.Location;
			var providerDir      = Path.GetDirectoryName(providerLocation)!;

			environment.EnvironmentVariables.Add("PROVIDER_DIR", providerDir);

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--provider-location", "%PROVIDER_DIR%\\" + Path.GetFileName(providerLocation), "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryRejectsMissingEnvironmentVariableInProviderLocationPath()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--provider-location", "%PROVIDER_DIR%\\provider.dll", "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain("Environment variable 'PROVIDER_DIR' referenced by option '--provider-location' is not set."));
			}
		}

		[Test]
		public async Task QueryRejectsMissingEnvironmentVariableInConfigProviderLocationPath()
		{
			var environment = new TestCliEnvironment();

			environment.Files.Add("config\\query.json", """
				{
					"default": {
						"provider": "SQLite",
						"providerLocation": "%PROVIDER_DIR%\\provider.dll",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", "config\\query.json", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Does.Contain("Environment variable 'PROVIDER_DIR' referenced by option '--provider-location' is not set."));
			}
		}

		[Test]
		public async Task QueryRejectsMissingEnvironmentVariableInPath()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--sql-file", "%QUERY_DIR%\\query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Environment variable 'QUERY_DIR' referenced by option '--sql-file' is not set."));
			}
		}

		[Test]
		public async Task QueryRejectsInvalidConfigTimeout()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"commandTimeout": -1
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'commandTimeout' must be a non-negative integer number of seconds."));
			}
		}

		[Test]
		public async Task QueryAcceptsNamedConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					},
					"uat": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			environment.Files.Add("query.sql", "select 2 as Value");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"2\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryUsesDefaultValuesForMissingProfileValues()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"commandTimeout": 30,
						"output": "csv",
						"outputFile": "default.csv"
					},
					"uat": {
						"output": "json"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["default.csv"], Does.Contain("\"1\":\"1\""));
			}
		}

		[Test]
		public async Task QueryCliOptionsOverrideConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=default;",
						"output": "csv",
						"outputFile": "default.csv"
					},
					"uat": {
						"connectionString": "Data Source=uat;"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--connection-string", "Data Source=:memory:", "--output", "json", "--output-file", "cli.json", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.Empty);
				Assert.That(result.Error,    Is.Empty);
				Assert.That(environment.Files["cli.json"], Does.Contain("\"Value\":\"1\""));
			}
		}

		[Test]
		public async Task QueryFormatsConnectionStringWithCliCredentials()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--user", ":memory:", "--password", "ignored", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReportsInvalidConnectionStringFormat()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source={memory};Mode=Memory;Cache=Shared", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Invalid connection string format:"));
				Assert.That(result.Error,    Does.Contain("{0}"));
				Assert.That(result.Error,    Does.Contain("{1}"));
				Assert.That(result.Error,    Does.Contain("{{"));
				Assert.That(result.Error,    Does.Contain("}}"));
			}
		}

		[Test]
		public async Task QueryAcceptsEscapedConnectionStringBraces()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source={{memory}};Mode=Memory;Cache=Shared", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryFormatsConnectionStringWithConfigCredentials()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"user": ":memory:",
						"password": "ignored"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReadsConnectionStringFromEnvironmentVariable()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_CONNECTION", "Data Source=:memory:");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string-env", "LINQ2DB_QUERY_CONNECTION", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReadsUserFromEnvironmentVariable()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_USER", ":memory:");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source={0}", "--user-env", "LINQ2DB_QUERY_USER", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReadsPasswordFromEnvironmentVariable()
		{
			var environment = new TestCliEnvironment();

			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_PASSWORD", ":memory:");

			var result = await RunCli(environment, "query", "--provider", "SQLite", "--connection-string", "Data Source={1}", "--password-env", "LINQ2DB_QUERY_PASSWORD", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReportsMissingEnvironmentVariable()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source={1}", "--password-env", "LINQ2DB_QUERY_PASSWORD", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Environment variable 'LINQ2DB_QUERY_PASSWORD' specified for option '--password' is not set."));
			}
		}

		[Test]
		public async Task QueryCliLiteralOverridesConfigEnvironmentVariable()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionStringEnv": "LINQ2DB_QUERY_CONNECTION"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--connection-string", "Data Source=:memory:", "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReadsConnectionStringFromConfigEnvironmentVariable()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionStringEnv": "LINQ2DB_QUERY_CONNECTION"
					}
				}
				""");

			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_CONNECTION", "Data Source=:memory:");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryReadsConnectionSettingsFromConfigEnvironmentVariables()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source={0}",
						"userEnv": "LINQ2DB_QUERY_USER",
						"passwordEnv": "LINQ2DB_QUERY_PASSWORD"
					}
				}
				""");

			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_USER", ":memory:");
			environment.EnvironmentVariables.Add("LINQ2DB_QUERY_PASSWORD", "ignored");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Value");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("\"Value\":\"1\""));
				Assert.That(result.Error,    Is.Empty);
			}
		}

		[Test]
		public async Task QueryStillRequiresSqlOrSqlFileWithConfig()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Either '--sql' or '--sql-file' option must be specified."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--profile", "uat", "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' doesn't contain 'uat' profile."));
			}
		}

		[Test]
		public async Task QueryRejectsSqlInConfigProfile()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"sql": "select 1"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql-file", "query.sql");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' contains unknown property 'sql'."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigOutput()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"output": "xml"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'output' has unknown value 'xml'."));
			}
		}

		[Test]
		public async Task QueryRejectsUnknownConfigUnsafeSql()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"unsafeSql": "prompt"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'unsafeSql' has unknown value 'prompt'."));
			}
		}

		[Test]
		public async Task QueryConfigImpersonateRequiresUserAndPassword()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"impersonate": true
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain("Option '--impersonate' requires resolved '--user' and '--password' values."));
			}
		}

		[Test]
		public async Task QueryRejectsInvalidConfigImpersonate()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"impersonate": 1
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'impersonate' must be boolean."));
			}
		}

		[Test]
		public async Task QueryRejectsInvalidConfigImpersonateMode()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"impersonateMode": "bad-mode"
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.EqualTo(-1));
				Assert.That(result.Error,    Does.Contain($"Configuration file '{config}' profile 'default' property 'impersonateMode' has unknown value 'bad-mode'."));
			}
		}

		[Test]
		public async Task QueryAcceptsCommandLineImpersonateModeSystemCode()
		{
			var result = await RunCli("query", "--provider", "SQLite", "--connection-string", "Data Source=:memory:", "--impersonate-mode", "8", "--sql", "select 1 as Id");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.EqualTo("""[{"Id":"1"}]"""));
			}
		}

		[Test]
		public async Task QueryAcceptsConfigImpersonateModeSystemCode()
		{
			var environment = new TestCliEnvironment();
			var config      = AddConfigFile(environment, """
				{
					"default": {
						"provider": "SQLite",
						"connectionString": "Data Source=:memory:",
						"impersonateMode": 8
					}
				}
				""");

			var result = await RunCli(environment, "query", "--config", config, "--sql", "select 1 as Id");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Is.EqualTo("""[{"Id":"1"}]"""));
			}
		}

		[Test]
		public async Task QueryHelpShowsSqlInputOptions()
		{
			var result = await RunCli("help", "query");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query <options>"));
				Assert.That(result.Output,   Does.Contain("--config"));
				Assert.That(result.Output,   Does.Contain("--profile"));
				Assert.That(result.Output,   Does.Contain("--provider"));
				Assert.That(result.Output,   Does.Contain("--provider-location"));
				Assert.That(result.Output,   Does.Contain("dependencies must be available next to it or through normal application probing"));
				Assert.That(result.Output,   Does.Contain("--connection-string"));
				Assert.That(result.Output,   Does.Contain("--connection-string-env"));
				Assert.That(result.Output,   Does.Contain("--user"));
				Assert.That(result.Output,   Does.Contain("--user-env"));
				Assert.That(result.Output,   Does.Contain("--password"));
				Assert.That(result.Output,   Does.Contain("--password-env"));
				Assert.That(result.Output,   Does.Contain("--impersonate"));
				Assert.That(result.Output,   Does.Contain("run database access under resolved user/password credentials"));
				Assert.That(result.Output,   Does.Contain("--impersonate-mode"));
				Assert.That(result.Output,   Does.Contain("Windows impersonation logon mode"));
				Assert.That(result.Output,   Does.Contain("network-cleartext"));
				Assert.That(result.Output,   Does.Contain("system code for network cleartext logon"));
				Assert.That(result.Output,   Does.Contain("--command-timeout"));
				Assert.That(result.Output,   Does.Contain("--lock-timeout"));
				Assert.That(result.Output,   Does.Contain("--max-rows"));
				Assert.That(result.Output,   Does.Contain("--allow-unsafe-sql"));
				Assert.That(result.Output,   Does.Contain("ask the user before using this option"));
				Assert.That(result.Output,   Does.Contain("agents can analyze code together with live database data"));
				Assert.That(result.Output,   Does.Contain("--output"));
				Assert.That(result.Output,   Does.Contain("--output-file"));
				Assert.That(result.Output,   Does.Contain("--overwrite"));
				Assert.That(result.Output,   Does.Contain("json-table"));
				Assert.That(result.Output,   Does.Contain("--sql"));
				Assert.That(result.Output,   Does.Contain("--sql-file"));
				Assert.That(result.Output,   Does.Contain("single user-provided SQL query text"));
				Assert.That(result.Output,   Does.Contain("configure provider-specific connection timeout here"));
				Assert.That(result.Output,   Does.Contain("SQL command timeout in seconds; 0 disables the option"));
				Assert.That(result.Output,   Does.Contain("provider-specific lock wait timeout in seconds; 0 disables the option"));
				Assert.That(result.Output,   Does.Contain("maximum number of result rows to read; 0 disables the limit"));
				Assert.That(result.Output,   Does.Contain("Examples:"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\""));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql-file query.sql"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --config query.json --profile uat --command-timeout 30 --sql-file query.sql"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --config query.json --profile uat --user readonly --password secret --sql-file query.sql"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --config query.json --profile uat --output json-table --sql \"select p.Id, o.Id from Person p join Orders o on o.PersonId = p.Id\""));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider DB2 --provider-location \"C:\\path\\to\\IBM.Data.Db2.dll\" --connection-string \"Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!\" --sql \"select * from SYSIBM.SYSDUMMY1\""));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --output csv --output-file result.csv --sql \"select * from Person\""));
			}
		}

		[Test]
		public async Task GeneralHelpShowsShortQueryCommandAndExample()
		{
			var result = await RunCli("help");

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.ExitCode, Is.Zero);
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query <options> : execute read-oriented SQL query so agents can analyze code together with live database data"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db scaffold <options> : generate database data model classes from database schema"));
				Assert.That(result.Output,   Does.Contain("dotnet linq2db query --provider SQLite --connection-string \"Data Source=data.db\" --sql \"select * from Person\""));
				Assert.That(result.Output,   Does.Contain("execute read-oriented SQL query against SQLite database and write JSON result to console"));
			}
		}

		private static string AddConfigFile(TestCliEnvironment environment, string content)
		{
			var fileName = $"query-{environment.Files.Count + 1}.json";

			environment.Files.Add(fileName, content);

			return fileName;
		}

		private static async Task<CliResult> RunCli(params string[] arguments)
		{
			return await RunCli(new TestCliEnvironment(), arguments).ConfigureAwait(false);
		}

		private static async Task<CliResult> RunCli(TestCliEnvironment environment, params string[] arguments)
		{
			var exitCode = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);

		private sealed class TestCliEnvironment : ICliEnvironment
		{
			private readonly StringWriter _output = new();
			private readonly StringWriter _error  = new();

			public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);
			public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.Ordinal);

			public TextWriter Out   => _output;
			public TextWriter Error => _error;

			public int BufferWidth => 120;

			public string Output      => _output.ToString();
			public string ErrorOutput => _error .ToString();

			public bool FileExists(string path)
			{
				return Files.ContainsKey(path) || File.Exists(path);
			}

			public string ReadAllText(string path)
			{
				return Files[path];
			}

			public void WriteAllText(string path, string contents)
			{
				Files[path] = contents;
			}

			public TextWriter CreateTextWriter(string path)
			{
				return new TestFileWriter(contents => Files[path] = contents);
			}

			public string? GetEnvironmentVariable(string name)
			{
				return EnvironmentVariables.GetValueOrDefault(name);
			}

			sealed class TestFileWriter(Action<string> save) : StringWriter
			{
				bool _saved;

				public override ValueTask DisposeAsync()
				{
					Save();
					return base.DisposeAsync();
				}

				protected override void Dispose(bool disposing)
				{
					if (disposing)
						Save();

					base.Dispose(disposing);
				}

				void Save()
				{
					if (_saved)
						return;

					save(ToString());
					_saved = true;
				}
			}
		}
	}
}
