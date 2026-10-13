using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

namespace Tests.LinqToDB.CLI
{
	// scaffold --customize runs out of process: CLI loads interceptor assemblies under fixed names into default load context
	[TestFixture]
	public sealed class ScaffoldCustomizeTests : CliProcessTestBase
	{
		static readonly TimeSpan _timeout = TimeSpan.FromMinutes(2);

		const string TemplateHeader = """
			<#@ template language="C#" inherits="LinqToDB.LinqToDBHost" linePragmas="false" #>
			<#@ import namespace="System"                     #>
			<#@ import namespace="System.Collections.Generic" #>
			<#@ import namespace="System.Linq"                #>
			<#@ import namespace="LinqToDB.CodeModel"         #>
			<#@ import namespace="LinqToDB.DataModel"         #>
			<#@ import namespace="LinqToDB.Schema"            #>
			<#@ import namespace="LinqToDB.Scaffold"          #>

			""";

		const string Usings = """
			using System;
			using System.Collections.Generic;
			using System.Linq;
			using LinqToDB.CodeModel;
			using LinqToDB.DataModel;
			using LinqToDB.Schema;
			using LinqToDB.Scaffold;

			""";

		// removes long-named table and renames Customers entity
		const string Interceptors = """
			public class Interceptors : ScaffoldInterceptors
			{
				public Interceptors(ScaffoldOptions options)
				{
				}

				public override IEnumerable<Table> GetTables(IEnumerable<Table> tables)
				{
					return tables.Where(t => t.Name.Name.Length < 100);
				}

				public override void PreprocessEntity(ITypeParser typeParser, EntityModel entityModel)
				{
					if (entityModel.Metadata.Name?.Name == "Customers")
						entityModel.Class.Name = "Client";
				}
			}
			""";

		static string NewDirectory(string prefix)
		{
			var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{prefix}-{Guid.NewGuid():N}");
			Directory.CreateDirectory(path);
			return path;
		}

		static Task<CliProcessResult> Scaffold(string database, string output, string customize, string language)
		{
			return RunCliProcess(
				_timeout,
				"scaffold",
				"-p", "SQLite",
				"-c", $"Data Source={database};Pooling=False",
				"-o", output,
				"-n", "Gen.Customize",
				"--overwrite", "true",
				"--target-language", language,
				"--customize", customize);
		}

		static void AssertCustomizedOutput(string output, string language)
		{
			var extension = language == "f#" ? "*.fs" : "*.cs";
			var sources   = Directory.GetFiles(output, extension).Select(static f => new SourceFile(Path.GetFileName(f), File.ReadAllText(f))).ToArray();

			sources.ShouldNotBeEmpty();

			var code = string.Join("\n", sources.Select(static s => s.Code));

			code.ShouldContain(language == "f#" ? "type Client" : "class Client");
			code.ShouldNotContain("aaaaaaaaaa");

			var name = $"CliCustomize_{Guid.NewGuid():N}";

			// F# output is single file; C# output is file per class in no particular compile order requirement
			var result = language == "f#"
				? FSharpCompiler.Compile(name, sources, nullable: true)
				: CSharpCompiler.Compile(name, sources);

			result.Success.ShouldBeTrue(result.Report());
		}

		[Test]
		public async Task TemplateApplied([Values("c#", "f#")] string language)
		{
			var database = CreateCliSqliteDatabase("customize", false);
			var output   = NewDirectory("customize-out");
			var template = Path.Combine(NewDirectory("customize-tt"), "scaffold.tt");

			try
			{
				await File.WriteAllTextAsync(template, TemplateHeader + Interceptors);

				var result = await Scaffold(database, output, template, language);

				result.ExitCode.ShouldBe(0, result.Error + result.Output);

				AssertCustomizedOutput(output, language);
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task AssemblyApplied()
		{
			var database = CreateCliSqliteDatabase("customize", false);
			var output   = NewDirectory("customize-out");
			var assembly = Path.Combine(NewDirectory("customize-dll"), "Interceptors.dll");

			try
			{
				var compiled = CSharpCompiler.Compile("CustomInterceptors", [new SourceFile("Interceptors.cs", Usings + Interceptors)], outputPath: assembly);
				compiled.Success.ShouldBeTrue(compiled.Report());

				var result = await Scaffold(database, output, assembly, "c#");

				result.ExitCode.ShouldBe(0, result.Error + result.Output);

				AssertCustomizedOutput(output, "c#");
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		public async Task InterceptorCompileErrorReported()
		{
			var database = CreateCliSqliteDatabase("customize", false);
			var template = Path.Combine(NewDirectory("customize-tt"), "scaffold.tt");

			try
			{
				await File.WriteAllTextAsync(template, TemplateHeader + Interceptors.Replace("entityModel.Class.Name = \"Client\";", "entityModel.Class.Name = 42;", StringComparison.Ordinal));

				var result = await Scaffold(database, NewDirectory("customize-out"), template, "c#");

				// EXPECTED_ERROR
				result.ExitCode.ShouldBe(-3, result.Error);
				result.Error.ShouldContain("T4 compilation failed");
				result.Error.ShouldContain("CS0029");
			}
			finally
			{
				File.Delete(database);
			}
		}

		[Test]
		[ActiveIssue(6036, Details = "Template compilation status ignored", ErrorMessage = "Object reference not set")]
		public async Task TemplateControlBlockCompileErrorReported()
		{
			var database = CreateCliSqliteDatabase("customize", false);
			var template = Path.Combine(NewDirectory("customize-tt"), "scaffold.tt");

			try
			{
				await File.WriteAllTextAsync(template, TemplateHeader + "<# int broken = \"text\"; #>\n" + Interceptors);

				var result = await Scaffold(database, NewDirectory("customize-out"), template, "c#");

				// template compilation error is reported as T4/compilation error, not as internal error (-2)
				result.ExitCode.ShouldBeOneOf([-3, -4], result.Error);
				result.Error.ShouldContain("CS0029");
			}
			finally
			{
				File.Delete(database);
			}
		}
	}
}
