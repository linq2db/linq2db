using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Template command implementation.
	/// </summary>
	internal sealed class TemplateCommand : CliCommand
	{
		private const string DEFAULT_PATH = "scaffold.tt";

		/// <summary>
		/// Template output path.
		/// </summary>
		public static readonly CliOption Output = new StringCliOption(
			"output",
			'o',
			false,
			false,
			"relative or full path to generated sample template file (with file name)",
			"If target folder doesn't exists, it will be created.",
			null,
			null,
			null,
			null);

		public static CliCommand Instance { get; } = new TemplateCommand();

		private TemplateCommand()
			: base(
				"template",
				true,
				false,
				"[-o output_template_file_path]",
				"creates empty T4 template for scaffolding customization",
				new CommandExample[]
				{
					new("dotnet linq2db template", $"puts basic template file to current folder as file with name {DEFAULT_PATH}"),
					new("dotnet linq2db template -o c:\\my_project\\context_customization.tt", "puts basic template file to c:\\my_project\\ folder as file with name context_customization.tt"),
				})
		{
			AddOption(Output);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs)
		{
			var path     = options.TryGetValue(Output, out var value) ? (string)value! : DEFAULT_PATH;
			var fullPath = Path.GetFullPath(path);

			if (File.Exists(fullPath))
			{
				await Console.Error.WriteLineAsync($"Template file aleady exists at location {fullPath}").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

			using var template = GetType().Assembly.GetManifestResourceStream("LinqToDB.CLI.Template.tt")!;
			using var file     = File.Create(fullPath);

			await template.CopyToAsync(file).ConfigureAwait(false);

			return StatusCodes.SUCCESS;
		}
	}
}
