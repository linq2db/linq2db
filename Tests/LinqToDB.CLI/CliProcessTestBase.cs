using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Shared out-of-process CLI invocation and SQLite fixture helpers for command test fixtures.
	/// </summary>
	public abstract class CliProcessTestBase
	{
		protected static string CreateCliSqliteDatabase(string namePrefix, bool seedCustomers)
		{
			var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{namePrefix}-{Guid.NewGuid():N}.db");

			var dataProvider = DataConnection.GetDataProvider("SQLite", $"Data Source={fileName};Pooling=False")!;
			using var db     = new DataConnection(new DataOptions().UseConnectionString(dataProvider, $"Data Source={fileName};Pooling=False"));

			db.Execute("""
				create table Customers
				(
					Id   integer not null primary key,
					Name text    not null
				)
				""");

			if (seedCustomers)
				db.Execute("insert into Customers (Id, Name) values (1, 'original')");

			db.Execute("""
				create table Orders
				(
					Id         integer not null primary key,
					CustomerId integer not null references Customers(Id),
					Amount     decimal(10, 2) null
				)
				""");

			db.Execute("""
				create table ChildOrders
				(
					Id      integer not null primary key,
					OrderId integer not null references Orders(Id)
				)
				""");

			var longName = new string('a', 512) + "b";
			db.Execute($"create table \"{longName}\" (Id integer not null primary key)");

			return fileName;
		}

		protected static async Task<CliProcessResult> RunCliProcess(params string[] arguments)
		{
			var cliAssembly = Path.Combine(AppContext.BaseDirectory, "dotnet-linq2db.dll");

			if (!File.Exists(cliAssembly))
				throw new FileNotFoundException("Cannot find CLI assembly for CLI process tests.", cliAssembly);

			var startInfo = new ProcessStartInfo("dotnet")
			{
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
			};

			startInfo.ArgumentList.Add(cliAssembly);

			foreach (var argument in arguments)
				startInfo.ArgumentList.Add(argument);

			using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start CLI process.");

			var outputTask = process.StandardOutput.ReadToEndAsync();
			var errorTask  = process.StandardError.ReadToEndAsync();

			try
			{
				await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20)).ConfigureAwait(false);
			}
			catch (TimeoutException)
			{
				process.Kill(entireProcessTree: true);
				throw;
			}

			return new CliProcessResult(NormalizeExitCode(process.ExitCode), await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
		}

		static int NormalizeExitCode(int exitCode)
		{
			// On non-Windows OSes, a negative process exit code wraps to its unsigned byte
			// representation (e.g. -1 -> 255); sign-extend the low byte so negative CLI
			// status codes compare consistently across platforms.
			return unchecked((sbyte)exitCode);
		}

		protected sealed record CliProcessResult(int ExitCode, string Output, string Error);
	}
}
