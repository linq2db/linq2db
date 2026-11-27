using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB.CodeModel;
using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Scaffold;
using LinqToDB.CommandLine.Options;
using LinqToDB.Naming;
using LinqToDB.Scaffold;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	[TestFixture]
	public sealed class ScaffoldTargetLanguageTests
	{
		// C#-only options F# generation forces to one value, with the setting each one controls
		static readonly (CliOption Option, Func<ScaffoldOptions, bool> Get, Action<ScaffoldOptions, bool> Set)[] _fsharpForcedOptions =
		[
			(ScaffoldCommand.CodeGen.  SingleFile                     , o => !o.CodeGeneration.ClassPerFile          , (o, v) => o.CodeGeneration.ClassPerFile          = !v),
			(ScaffoldCommand.DataModel.EntityClassIsPartial           , o => o.DataModel.EntityClassIsPartial          , (o, v) => o.DataModel.EntityClassIsPartial          = v),
			(ScaffoldCommand.DataModel.EmitInitDataContextMethod      , o => o.DataModel.GenerateInitDataContextMethod , (o, v) => o.DataModel.GenerateInitDataContextMethod = v),
			(ScaffoldCommand.DataModel.EmitStaticInitDataContextMethod, o => o.DataModel.GenerateStaticInitDataContextMethod, (o, v) => o.DataModel.GenerateStaticInitDataContextMethod = v),
			(ScaffoldCommand.DataModel.EmitDefaultConstructor         , o => o.DataModel.HasDefaultConstructor         , (o, v) => o.DataModel.HasDefaultConstructor         = v),
			(ScaffoldCommand.DataModel.EmitConfigurationConstructor   , o => o.DataModel.HasConfigurationConstructor   , (o, v) => o.DataModel.HasConfigurationConstructor   = v),
			(ScaffoldCommand.DataModel.EmitOptionsConstructor         , o => o.DataModel.HasUntypedOptionsConstructor  , (o, v) => o.DataModel.HasUntypedOptionsConstructor  = v),
			(ScaffoldCommand.DataModel.EmitTypedOptionsConstructor    , o => o.DataModel.HasTypedOptionsConstructor    , (o, v) => o.DataModel.HasTypedOptionsConstructor    = v),
			(ScaffoldCommand.DataModel.EmitAssociationExtensions      , o => o.DataModel.GenerateAssociationExtensions , (o, v) => o.DataModel.GenerateAssociationExtensions = v),
			(ScaffoldCommand.DataModel.EmitSchemaErrors               , o => o.DataModel.GenerateProceduresSchemaError , (o, v) => o.DataModel.GenerateProceduresSchemaError = v),
		];

		[TestCase("--base-entity"   , "My.Base")]
		[TestCase("--no-xmldoc-warn", "true"   )]
		[TestCase("--no-xmldoc-warn", "false"  )]
		public async Task FSharpRejectsCSharpOnlyOption(string option, string value)
		{
			var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "--target-language", "f#", option, value);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Error.   ShouldContain($"Option '{option}' is not supported for target language F#.");
			}
		}

		[TestCase("single-file")]
		[TestCase("partial-entities")]
		[TestCase("add-init-context")]
		[TestCase("add-static-init-context")]
		[TestCase("add-default-ctor")]
		[TestCase("add-configuration-ctor")]
		[TestCase("add-options-ctor")]
		[TestCase("add-typed-options-ctor")]
		[TestCase("add-association-extensions")]
		[TestCase("emit-schema-errors")]
		public async Task FSharpAcceptsCSharpOnlyOptionWithFSharpValue(string name)
		{
			var option   = _fsharpForcedOptions.Single(o => o.Option.Name == name).Option;
			var accepted = option.FSharpValueText!;
			var rejected = accepted == "true" ? "false" : "true";
			var output   = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"scaffold-{Guid.NewGuid():N}");

			try
			{
				var acceptedResult = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "-o", output, "--target-language", "f#", $"--{name}", accepted);
				var rejectedResult = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "-o", output, "--target-language", "f#", $"--{name}", rejected);

				using (Assert.EnterMultipleScope())
				{
					acceptedResult.ExitCode.ShouldBe(0, acceptedResult.Error);
					rejectedResult.ExitCode.ShouldBe(-1);
					rejectedResult.Error.   ShouldContain($"Option '--{name}' accepts only value '{accepted}' for target language F#.");
				}
			}
			finally
			{
				if (Directory.Exists(output))
					Directory.Delete(output, true);
			}
		}

		// the CLI's FSharpValue must be what Scaffolder forces for F#
		[Test]
		public void FSharpValuesMatchScaffolderAdjustments()
		{
			var options = ScaffoldOptions.Default();

			foreach (var (option, _, set) in _fsharpForcedOptions)
				set(options, !(bool)option.FSharpValue!);

			_ = new Scaffolder(LanguageProviders.FSharp, HumanizerNameConverter.Instance, options, null);

			var withFSharpValue = typeof(ScaffoldCommand)
				.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
				.SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
				.Where(f => typeof(CliOption).IsAssignableFrom(f.FieldType))
				.Select(f => (CliOption)f.GetValue(null)!)
				.Where(o => o.FSharpValue != null)
				.Select(o => o.Name)
				.OrderBy(n => n, StringComparer.Ordinal)
				.ToArray();

			var forced = _fsharpForcedOptions
				.Select(o => o.Option.Name)
				.OrderBy(n => n, StringComparer.Ordinal)
				.ToArray();

			using (Assert.EnterMultipleScope())
			{
				withFSharpValue.ShouldBe(forced);

				foreach (var (option, get, _) in _fsharpForcedOptions)
				{
					var actual = get(options);

					actual.ShouldBe((bool)option.FSharpValue!, option.Name);
				}
			}
		}

		[Test]
		public async Task FSharpRejectsT4Mode()
		{
			var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "--target-language", "f#", "-t", "t4");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Error.   ShouldContain("F# generation (--target-language f#) is not supported together with the T4 template mode (-t t4).");
			}
		}

		[Test]
		public async Task CSharpAcceptsCSharpOnlyOption()
		{
			var output = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"scaffold-{Guid.NewGuid():N}");

			try
			{
				var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "-o", output, "--target-language", "c#", "--add-init-context", "true");

				using (Assert.EnterMultipleScope())
				{
					result.ExitCode.ShouldBe(0);
					result.Error.   ShouldNotContain("is not supported for target language");
				}
			}
			finally
			{
				if (Directory.Exists(output))
					Directory.Delete(output, true);
			}
		}

		[Test]
		public async Task FSharpScaffoldWritesFSharpFile()
		{
			var output = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"scaffold-{Guid.NewGuid():N}");

			try
			{
				var result = await RunCli("scaffold", "-p", "SQLite", "-c", "Data Source=:memory:", "-o", output, "--target-language", "f#");

				result.ExitCode.ShouldBe(0, result.Error);

				var files = Directory.GetFiles(output);

				files.ShouldHaveSingleItem();

				var extension = Path.GetExtension(files[0]);
				var code      = File.ReadAllText(files[0]);

				extension.ShouldBe(".fs");
				code.ShouldContain("namespace rec ");
			}
			finally
			{
				if (Directory.Exists(output))
					Directory.Delete(output, true);
			}
		}

		[Test]
		public async Task HelpShowsSupportedLanguages()
		{
			var result = await RunCli("help", "scaffold");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0);
				result.Output.  ShouldContain("Supported in: C#");
				result.Output.  ShouldContain("Supported in: C# (F#: true only)");
				result.Output.  ShouldNotContain("Supported in: F#");
			}
		}

		private static async Task<CliResult> RunCli(params string[] arguments)
		{
			var environment = new TestCliEnvironment();
			var exitCode    = await new LinqToDBCliController()
				.Execute(arguments, environment)
				.ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
