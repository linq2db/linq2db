using System;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class ScaffoldTemplateErrorTests : CliProcessTestBase
	{
		const string Template = """
			<#@ template language="C#" inherits="LinqToDB.LinqToDBHost" linePragmas="false" #>
			<#@ import namespace="LinqToDB.Scaffold" #>
			<# int broken = "text"; #>
			public class Interceptors : ScaffoldInterceptors
			{
			}
			""";

		[Test]
		public async Task TemplateCompileErrorReported()
		{
			var database = CreateCliSqliteDatabase("template-error", false);
			var folder   = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"template-error-{Guid.NewGuid():N}");
			var template = Path.Combine(folder, "scaffold.tt");

			Directory.CreateDirectory(folder);

			try
			{
				await File.WriteAllTextAsync(template, Template);

				var result = await RunCliProcess(
					TimeSpan.FromMinutes(2),
					"scaffold",
					"-p", "SQLite",
					"-c", $"Data Source={database};Pooling=False",
					"-o", Path.Combine(folder, "output"),
					"--customize", template);

				using (Assert.EnterMultipleScope())
				{
					// EXPECTED_ERROR with compiler diagnostics instead of INTERNAL_ERROR (-2) from NullReferenceException
					result.ExitCode.ShouldBe(-3, result.Error);
					result.Error.ShouldContain("T4 compilation failed");
					result.Error.ShouldContain("CS0029");
				}
			}
			finally
			{
				File.Delete(database);
			}
		}
	}
}
