using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB.Naming;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Help command implementation.
	/// Supports:
	/// <list type="bullet">
	/// <item>default help : general help information and available commands</item>
	/// <item>command help : help on specific command</item>
	/// </list>
	/// </summary>
	internal sealed class HelpCommand : CliCommand
	{
		public static CliCommand Instance { get; } = new HelpCommand();

		private HelpCommand()
			: base("help", false, false, "[<command>]", "print this help or help on specific command", Array.Empty<CommandExample>())
		{
		}

		public override ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs)
		{
			if (options.Count == 0 && (rawArgs.Length == 0 || (rawArgs.Length == 1 && string.Equals(rawArgs[0], "help", StringComparison.Ordinal))))
			{
				// handle proper calls with general help display:
				// "dotnet linq2db"
				// "dotnet linq2db help"
				PrintGeneralHelp(environment, controller, Array.Empty<string>());
				return new(StatusCodes.SUCCESS);
			}

			// handle command-specific help requests (except help command itself):
			// "dotnet linq2db help <known_command>"
			if (options.Count == 0 && rawArgs.Length == 2 && string.Equals(rawArgs[0], "help", StringComparison.Ordinal))
			{
				foreach (var command in controller.Commands)
				{
					if (command != this)
					{
						if (string.Equals(command.Name, rawArgs[1], StringComparison.Ordinal))
						{
							// request for command help for known non-help command - print specific command help
							PrintCommandHelp(environment, command, unknownArgs);
							return new(StatusCodes.SUCCESS);
						}
					}
				}

				// help request for unknown command:
				// dotnet linq2db help <unknown-command>
				PrintGeneralHelp(environment, controller, new[] { rawArgs[1] });
				return new(StatusCodes.INVALID_ARGUMENTS);
			}

			// all other cases - print default help and error message:
			// "dotnet linq2db help <whatever> <arguments> <here>"
			PrintGeneralHelp(environment, controller, rawArgs);
			return new(StatusCodes.INVALID_ARGUMENTS);
		}

		/// <summary>
		/// Prints help text for specific command.
		/// </summary>
		/// <param name="command">Command descriptor.</param>
		/// <param name="unknownArgs">List of unrecognized command-line arguments.</param>
		private void PrintCommandHelp(ICliEnvironment environment, CliCommand command, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader(environment);

			PrintBadArgumentsError(environment, unknownArgs);

			environment.Out.WriteLine();
			environment.Out.WriteLine("Usage:");
			environment.Out.WriteLine("        dotnet linq2db {0}{1}", command.Name, command.Template.Length != 0 ? " " + command.Template : command.Template);

			environment.Out.WriteLine();
			environment.Out.WriteLine("Options:");

			// calculate column width for column layout
			var maxOptionNameWidth = 0;
			var maxOptionTypeWidth = 0;

			foreach (var option in command.GetOptionsWithoutCategory())
				CalculateOptionSizes(option);

			foreach (var category in command.Categories)
			{
				foreach (var option in command.GetCategoryOptions(category))
					CalculateOptionSizes(option);
			}

			// print options help grouped by option category
			const string indent = "      ";

			foreach (var option in command.GetOptionsWithoutCategory())
				WriteOptionHelp(environment, command, maxOptionNameWidth, indent, null, option);

			foreach (var category in command.Categories)
			{
				environment.Out.WriteLine();
				environment.Out.WriteLine("=== {0} : {1}", category.Name, category.Description);

				foreach (var option in command.GetCategoryOptions(category))
				{
					WriteOptionHelp(environment, command, maxOptionNameWidth, indent, category, option);
				}
			}

			// print command examples
			if (command.Examples.Count > 0)
			{
				environment.Out.WriteLine();
				environment.Out.WriteLine("Examples:");

				foreach (var example in command.Examples)
				{
					environment.Out.WriteLine();
					environment.Out.WriteLine("        {0}", example.Command);
					environment.Out.WriteLine("                {0}", example.Help);
				}
			}

			void CalculateOptionSizes(CliOption option)
			{
				var optionWidth = GetOptionWidth(option);
				var typeWidth   = GetOptionTypeName(option).Length;

				if (optionWidth > maxOptionNameWidth)
					maxOptionNameWidth = optionWidth;

				if (typeWidth > maxOptionTypeWidth)
					maxOptionTypeWidth = typeWidth;
			}
		}

		private static int GetOptionWidth(CliOption option)
		{
			var width = option.Name.Length;
			if (option.ShortName != null)
				width += 4;
			return width;
		}

		// generate option type name:
		// <base_type>[ list] <(optional)|(required)>
		private static string GetOptionTypeName(CliOption option)
		{
			var type = option.Type switch
			{
				OptionType.Boolean              => "bool",
				OptionType.String               or
				OptionType.StringEnum           or
				OptionType.JSONImport           => "string",
				OptionType.DatabaseObjectFilter => "(string | object)",
				OptionType.Naming               => "object",
				OptionType.StringDictionary     => "[string]: string",

				_ => throw new NotImplementedException($"Option type {option.Type} not implemented"),
			};
			if (option.AllowMultiple && option.Type != OptionType.StringDictionary)
				type += " list";

			return type + (option.Required ? " (required)" : " (optional)");
		}

		private void WriteOptionHelp(ICliEnvironment environment, CliCommand command, int maxOptionNameWidth, string indent, OptionCategory? category, CliOption option)
		{
			// workaround for https://github.com/linq2db/linq2db/issues/3612
			var consoleWidth = environment.BufferWidth;

			environment.Out.WriteLine();
			environment.Out.Write("   ");

			var optionNameWidth = GetOptionWidth(option);
			var type            = GetOptionTypeName(option);

			if (option.AllowInCli)
			{
				// -x, --x-option    : <help>
				if (option.ShortName != null)
					environment.Out.Write("-{0}, ", option.ShortName.Value);

				environment.Out.Write("--{0}", option.Name);
				environment.Out.Write(new string(' ', maxOptionNameWidth - optionNameWidth + 2));
				environment.Out.WriteLine(" : {0}", option.Help);
			}
			else if (command.SupportsJSON)
			{
				// json-option       : (allowed only in JSON) <help>
				environment.Out.Write("  {0}", option.Name);
				environment.Out.Write(new string(' ', maxOptionNameWidth - optionNameWidth + 2));
				environment.Out.WriteLine(" : (allowed only in JSON) {0}", option.Help);
			}

			// option data type
			environment.Out.Write("{0}   type: ", indent);
			environment.Out.WriteLine(type);

			// display options, that cannot be used together with current option
			var incompatibleWith = command.GetIncompatibleOptions(option);
			if (incompatibleWith != null)
				environment.Out.WriteLine("{0}   cannot use with: {1}", indent, string.Join(", ", incompatibleWith.Select(o => $"--{o.Name}")));

			if (command.SupportsJSON)
			{
				// option property path in json or "not allowed" text if not supported
				environment.Out.Write("{0}   json: ", indent);
				if (option.AllowInJson)
				{
					if (category != null)
						environment.Out.WriteLine("{0}.{1}", category.JsonProperty, option.Name);
					else
						environment.Out.WriteLine("{0}", option.Name);
				}
				else
					environment.Out.WriteLine("not allowed");
			}

			// print default value (if set) for non-required option
			if (!option.Required)
			{
				if (option is BooleanCliOption booleanOption)
				{
					environment.Out.WriteLine("{0}   default: {1}", indent, booleanOption.Default ? "true" : "false");
					environment.Out.WriteLine("{0}   default (T4 mode): {1}", indent, booleanOption.T4Default ? "true" : "false");
				}

				if (option is StringCliOption stringOption)
				{
					if (stringOption.Default != null)
					{
						if (!stringOption.AllowMultiple)
							environment.Out.WriteLine("{0}   default: {1}", indent, stringOption.Default[0]);
						else
							environment.Out.WriteLine("{0}   default: {1}", indent, string.JoinStrings(',', stringOption.Default));
					}

					if (stringOption.T4Default != null)
					{
						if (!stringOption.AllowMultiple)
							environment.Out.WriteLine("{0}   default (T4 mode): {1}", indent, stringOption.T4Default[0]);
						else
							environment.Out.WriteLine("{0}   default (T4 mode): {1}", indent, string.JoinStrings(',', stringOption.T4Default));
					}
				}
				else if (option is StringEnumCliOption enumOption)
				{
					var defaults = enumOption.Values.Where(o => o.Default).Select(o => o.Value).ToArray();
					if (defaults.Length > 0)
						environment.Out.WriteLine("{0}   default: {1}", indent, string.Join(", ", defaults));

					defaults = enumOption.Values.Where(o => o.T4Default).Select(o => o.Value).ToArray();
					if (defaults.Length > 0)
						environment.Out.WriteLine("{0}   default (T4 mode): {1}", indent, string.Join(", ", defaults));
				}
				else if (option is NamingCliOption namingOption)
				{
					if (namingOption.Default != null)
						PrintNamingOptionDefaults(environment, indent, "default", namingOption.Default);
					if (namingOption.T4Default != null)
						PrintNamingOptionDefaults(environment, indent, "default (T4 mode)", namingOption.T4Default);
				}
			}

			// for enum-typed option print list of supported values with description
			if (option is StringEnumCliOption enumStringOption)
			{
				environment.Out.WriteLine("{0}   supported values:", indent);
				var maxValueWidth = enumStringOption.Values.Select(_ => _.Value.Length).Max();

				foreach (var value in enumStringOption.Values)
					environment.Out.WriteLine("{0}{0}   {1}{3} : {2}", indent, value.Value, value.Help, new string(' ', maxValueWidth - value.Value.Length));
			}

			// print option CLI and JSON examples if provided
			if (option.Examples != null)
			{
				environment.Out.WriteLine("{0}   examples:", indent);
				foreach (var example in option.Examples)
					environment.Out.WriteLine("{0}{0}   {1}", indent, example);
			}

			if (option.JsonExamples != null)
			{
				environment.Out.WriteLine("{0}   JSON examples:", indent);
				foreach (var example in option.JsonExamples)
					environment.Out.WriteLine("{0}{0}   {1}", indent, example);
			}

			// print detailed option help if provided
			if (option.DetailedHelp != null)
			{
				environment.Out.WriteLine();

				// split long text into lines manually and prepend each line with help indent for nicer formatting
				// TODO: dunno wether it works on linux/macos, not tested yet
				var lines = option.DetailedHelp.Split("\r\n");
				for (var i = 0; i < lines.Length; i++)
				{
					var line = lines[i];

					var lineWidth            = consoleWidth - indent.Length - 1;
					var incompleteLineLength = line.Length % lineWidth;
					var partsCount           = line.Length / lineWidth + (incompleteLineLength > 0 ? 1 : 0);

					for (var j = 0; j < partsCount; j++)
					{
						var part = line.Substring(
									j * lineWidth,
									j == partsCount - 1 && incompleteLineLength > 0 ? incompleteLineLength : lineWidth);

						environment.Out.Write(indent);
						environment.Out.WriteLine(part);
					}
				}
			}
		}

		private static void PrintNamingOptionDefaults(ICliEnvironment environment, string indent, string mode, NormalizationOptions options)
		{
			environment.Out.WriteLine("{0}   {1}: {{", indent, mode);

			var value = options.Casing switch
			{
				NameCasing.None                  => "\"none\"",
				NameCasing.Pascal                => "\"pascal_case\"",
				NameCasing.CamelCase             => "\"camel_case\"",
				NameCasing.SnakeCase             => "\"snake_case\"",
				NameCasing.LowerCase             => "\"lower_case\"",
				NameCasing.UpperCase             => "\"upper_case\"",
				NameCasing.T4CompatPluralized    => "\"t4_pluralized\"",
				NameCasing.T4CompatNonPluralized => "\"t4\"",

				_ => throw new InvalidOperationException($"Unknown casing option: {options.Casing}"),
			};

			printJsonProperty(indent, "case", value);

			value = options.Pluralization switch
			{
				Pluralization.None                  => "\"none\"",
				Pluralization.Singular              => "\"singular\"",
				Pluralization.Plural                => "\"plural\"",
				Pluralization.PluralIfLongerThanOne => "\"plural_multiple_characters\"",
				_ => throw new InvalidOperationException($"Unknown pluralization option: {options.Pluralization}"),
			};

			printJsonProperty(indent, "pluralization", value);

			printJsonProperty(indent, "prefix", options.Prefix == null ? "null" : $"\"{options.Prefix}\"");
			printJsonProperty(indent, "suffix", options.Suffix == null ? "null" : $"\"{options.Suffix}\"");

			value = options.Transformation switch
			{
				NameTransformation.None              => "\"none\"",
				NameTransformation.Association       => "\"association\"",
				NameTransformation.SplitByUnderscore => "\"split_by_underscore\"",

				_ => throw new InvalidOperationException($"Unknown transformation option: {options.Transformation}"),
			};

			printJsonProperty(indent, "transformation", value);

			printJsonProperty(indent, "pluralize_if_ends_with_word_only", options.PluralizeOnlyIfLastWordIsText ? "true" : "false");
			printJsonProperty(indent, "ignore_all_caps", options.DontCaseAllCaps ? "true" : "false");
			if (options.MaxUpperCaseWordLength > 1)
				printJsonProperty(indent, "max_uppercase_word_length", options.MaxUpperCaseWordLength.ToString(CultureInfo.InvariantCulture));

			environment.Out.WriteLine("{0}            }}", indent);

			void printJsonProperty(string padding, string optionName, string value)
			{
				environment.Out.WriteLine("{0}                \"{1}\"{3}: {2},", padding, optionName, value, new string(' ', "pluralize_if_ends_with_word_only".Length - optionName.Length));
			}
		}

		/// <summary>
		/// Print general help (supported commands list).
		/// </summary>
		/// <param name="controller">CLI controller instance.</param>
		/// <param name="unknownArgs">Unrecognized command line arguments for current call.</param>
		private void PrintGeneralHelp(ICliEnvironment environment, CliController controller, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader(environment);

			PrintBadArgumentsError(environment, unknownArgs);

			if (OperatingSystem.IsWindows())
			{
				environment.Out.WriteLine();
				environment.Out.WriteLine("Choosing 32-bit vs 64-bit on Windows:");
				environment.Out.WriteLine("    The tool ships as per-RID packages (win-x64, win-x86, win-arm64). `dotnet tool install -g linq2db.cli`");
				environment.Out.WriteLine("    picks one variant based on your SDK architecture (usually x64). The default x64 install works for");
				environment.Out.WriteLine("    most providers; you need a specific variant only when a database driver constrains the bitness:");
				environment.Out.WriteLine();
				environment.Out.WriteLine("    Always 32-bit (must install win-x86):");
				environment.Out.WriteLine("        - Microsoft.Jet.OLEDB (legacy Access .mdb databases)");
				environment.Out.WriteLine();
				environment.Out.WriteLine("    Bitness must match the installed driver (install the matching variant of linq2db.cli):");
				environment.Out.WriteLine("        - Microsoft.ACE.OLEDB.12.0 / .16.0 — must match the installed Office bitness");
				environment.Out.WriteLine("        - SQL Server Compact Edition — must match the installed SQL CE runtime bitness");
				environment.Out.WriteLine("        - SAP HANA — must match the installed HANA driver bitness (the HANA ODBC driver ships");
				environment.Out.WriteLine("          under different names for x86 vs x64; the native dotnet client is also bitness-specific)");
				environment.Out.WriteLine();
				environment.Out.WriteLine("    Install the x86 variant explicitly:");
				environment.Out.WriteLine("        dotnet tool install -g linq2db.cli --arch x86");
				environment.Out.WriteLine();
				environment.Out.WriteLine("    A single tool ID can only have ONE arch installed under -g. To switch architectures, either");
				environment.Out.WriteLine("    use 'dotnet tool update -g linq2db.cli --arch <x86|x64>', or uninstall first:");
				environment.Out.WriteLine("        dotnet tool uninstall -g linq2db.cli");
				environment.Out.WriteLine("        dotnet tool install -g linq2db.cli --arch x86");
				environment.Out.WriteLine();
				environment.Out.WriteLine("    To keep both x86 and x64 available, install each to a separate path and manage PATH order");
				environment.Out.WriteLine("    yourself. Use any paths you like (quote if they contain spaces):");
				environment.Out.WriteLine("        dotnet tool install linq2db.cli --tool-path C:\\tools\\linq2db-x64 --arch x64");
				environment.Out.WriteLine("        dotnet tool install linq2db.cli --tool-path C:\\tools\\linq2db-x86 --arch x86");
			}

			environment.Out.WriteLine();
			environment.Out.WriteLine("Usage:");
			environment.Out.WriteLine("        dotnet linq2db [help]: print this help");
			environment.Out.WriteLine("        dotnet linq2db help <command>: print help on specific command");
			environment.Out.WriteLine("        dotnet linq2db <command> [<options>]: execute specific command");

			environment.Out.WriteLine();
			environment.Out.WriteLine("linq2db tool provides database model scaffolding (reverse engineering) of database into Linq To DB (https://linq2db.github.io/) database model classes.");

			// list all commands with short description
			if (controller.Commands.Count > 1)
			{
				environment.Out.WriteLine();
				environment.Out.WriteLine("Supported commands:");

				foreach (var command in controller.Commands)
				{
					if (command != this)
						environment.Out.WriteLine("        dotnet linq2db {0}{1} : {2}", command.Name, command.Template.Length != 0 ? " " + command.Template : command.Template, command.Help);
				}
			}

			// TODO: load command examples from command object?
			// for now it's fine, as we have only one command
			environment.Out.WriteLine();
			environment.Out.WriteLine("Examples:");
			environment.Out.WriteLine("        dotnet linq2db scaffold -o c:\\my_project\\model -p SqlServer -c \"Server=MySqlServer;Database=MyDatabase;User Id=scaffold_user;Password=secret;\"");
			environment.Out.WriteLine("            generate data model code for SQL Server database in c:\\my_project\\model folder");
			environment.Out.WriteLine();
			environment.Out.WriteLine("        dotnet linq2db scaffold -i path\\to\\my_scaffold_options.json");
			environment.Out.WriteLine("            generate data model code using options from JSON file");
			environment.Out.WriteLine();
			environment.Out.WriteLine("        dotnet linq2db template");
			environment.Out.WriteLine("            create base T4 template for scaffolding customization in current folder");
		}

		/// <summary>
		/// Prints error message about unrecognized arguments.
		/// </summary>
		/// <param name="unknownArgs">List of unknown arguments.</param>
		private static void PrintBadArgumentsError(ICliEnvironment environment, IReadOnlyCollection<string> unknownArgs)
		{
			if (unknownArgs.Count > 0)
			{
				// all errors from console must be directed to stderr
				environment.Error.WriteLine();
				environment.Error.WriteLine("Error: unrecognized arguments:");

				foreach (var arg in unknownArgs)
					environment.Error.WriteLine("    {0}", arg);
			}
		}

		/// <summary>
		/// Prints help command header. Used for both general and command-specific help display.
		/// </summary>
		private void PrintHeader(ICliEnvironment environment)
		{
			environment.Out.WriteLine("dotnet linq2db - Linq To DB command-line utilities. Version: {0}", GetType().Assembly.GetName().Version);
		}
	}
}
