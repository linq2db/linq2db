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
		const string Header = """
			<#@ template language="C#" inherits="LinqToDB.LinqToDBHost" linePragmas="false" #>
			<#@ import namespace="LinqToDB.Scaffold" #>

			""";

		const string Body = """

			public class Interceptors : ScaffoldInterceptors
			{
			}
			""";

		[Test]
		public async Task TemplateCompileErrorReported()
		{
			var result = await RunTemplate(Header + """<# int broken = "text"; #>""" + Body);

			using (Assert.EnterMultipleScope())
			{
				// EXPECTED_ERROR with compiler diagnostics instead of INTERNAL_ERROR (-2) from NullReferenceException
				result.ExitCode.ShouldBe(-3, result.Error);
				result.Error.ShouldContain("T4 compilation of template '");
				result.Error.ShouldContain("scaffold.tt' failed:");
				result.Error.ShouldContain("CS0029");
			}
		}

		[Test]
		public async Task TemplateMissingAssemblyReported()
		{
			var result = await RunTemplate("""<#@ assembly name="/nonexistent/Missing.dll" #>""" + "\n" + Header + Body);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-4, result.Error);
				result.Error.ShouldContain("Missing.dll");
				result.Error.ShouldNotContain("Unhandled exception");
			}
		}

		[Test]
		public async Task TemplateRuntimeExceptionReported()
		{
			var result = await RunTemplate(Header + """<# throw new System.InvalidOperationException("boom from template"); #>""" + Body);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3, result.Error);
				result.Error.ShouldContain("execution failed");
				result.Error.ShouldContain("boom from template");
				result.Error.ShouldNotContain("Unhandled exception");
			}
		}

		static async Task<CliProcessResult> RunTemplate(string templateText)
		{
			var database = CreateCliSqliteDatabase("template-error", false);
			var folder   = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"template-error-{Guid.NewGuid():N}");
			var template = Path.Combine(folder, "scaffold.tt");

			Directory.CreateDirectory(folder);

			try
			{
				await File.WriteAllTextAsync(template, templateText);

				return await RunCliProcess(
					TimeSpan.FromMinutes(2),
					"scaffold",
					"-p", "SQLite",
					"-c", $"Data Source={database};Pooling=False",
					"-o", Path.Combine(folder, "output"),
					"--include-tables", "Customers",
					"--customize", template);
			}
			finally
			{
				File.Delete(database);
				Directory.Delete(folder, true);
			}
		}
	}
}
