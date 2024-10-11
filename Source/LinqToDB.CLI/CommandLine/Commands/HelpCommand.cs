using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

		public override int Execute(
			CliController                  controller,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs)
		{
			if (options.Count == 0 && (rawArgs.Length == 0 || (rawArgs.Length == 1 && rawArgs[0] == "help")))
			{
				// handle proper calls with general help display:
				// "dotnet linq2db"
				// "dotnet linq2db help"
				PrintGeneralHelp(controller, Array.Empty<string>());
				return StatusCodes.SUCCESS;
			}

			// handle command-specific help requests (except help command itself):
			// "dotnet linq2db help <known_command>"
			if (options.Count == 0 && rawArgs.Length == 2 && rawArgs[0] == "help")
			{
				foreach (var command in controller.Commands)
				{
					if (command != this)
					{
						if (command.Name == rawArgs[1])
						{
							// request for command help for known non-help command - print specific command help
							PrintCommandHelp(command, unknownArgs);
							return StatusCodes.SUCCESS;
						}
					}
				}

				// help request for unknown command:
				// dotnet linq2db help <unknown-command>
				PrintGeneralHelp(controller, new[] { rawArgs[1] });
				return StatusCodes.INVALID_ARGUMENTS;
			}

			// all other cases - print default help and error message:
			// "dotnet linq2db help <whatever> <arguments> <here>"
			PrintGeneralHelp(controller, rawArgs);
			return StatusCodes.INVALID_ARGUMENTS;
		}

		/// <summary>
		/// Prints help text for specific command.
		/// </summary>
		/// <param name="command">Command descriptor.</param>
		/// <param name="unknownArgs">List of unrecognized command-line arguments.</param>
		private void PrintCommandHelp(CliCommand command, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader();

			PrintBadArgumentsError(unknownArgs);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Usage:");
			Console.Out.WriteLine("        dotnet linq2db {0}{1}", command.Name, command.Template.Length != 0 ? " " + command.Template : command.Template);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Options:");

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
				WriteOptionHelp(command, maxOptionNameWidth, indent, null, option);

			foreach (var category in command.Categories)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("=== {0} : {1}", category.Name, category.Description);

				foreach (var option in command.GetCategoryOptions(category))
				{
					WriteOptionHelp(command, maxOptionNameWidth, indent, category, option);
				}
			}

			// print command examples
			if (command.Examples.Count > 0)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Examples:");

				foreach (var example in command.Examples)
				{
					Console.Out.WriteLine();
					Console.Out.WriteLine("        {0}", example.Command);
					Console.Out.WriteLine("                {0}", example.Help);
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
			string type;
			switch (option.Type)
			{
				case OptionType.Boolean:
					type = "bool";
					break;
				case OptionType.String:
				case OptionType.StringEnum:
				case OptionType.JSONImport:
					type = "string";
					break;
				case OptionType.DatabaseObjectFilter:
					type = "(string | object)";
					break;
				case OptionType.Naming:
					type = "object";
					break;
				case OptionType.StringDictionary:
					type = "[string]: string";
					break;
				default:
					throw new NotImplementedException($"Option type {option.Type} not implemented");
			}

			if (option.AllowMultiple && option.Type != OptionType.StringDictionary)
				type += " list";

			return type + (option.Required ? " (required)" : " (optional)");
		}

		private void WriteOptionHelp(CliCommand command, int maxOptionNameWidth, string indent, OptionCategory? category, CliOption option)
		{
			// workaround for https://github.com/linq2db/linq2db/issues/3612
			var consoleWidth = 80;
			try
			{
				consoleWidth = Console.BufferWidth;
			}
			catch { };

			Console.Out.WriteLine();
			Console.Out.Write("   ");

			var optionNameWidth = GetOptionWidth(option);
			var type            = GetOptionTypeName(option);

			if (option.AllowInCli)
			{
				// -x, --x-option    : <help>
				if (option.ShortName != null)
					Console.Out.Write("-{0}, ", option.ShortName.Value);

				Console.Out.Write("--{0}", option.Name);
				Console.Out.Write(new string(' ', maxOptionNameWidth - optionNameWidth + 2));
				Console.Out.WriteLine(" : {0}", option.Help);
			}
			else if (command.SupportsJSON)
			{
				// json-option       : (allowed only in JSON) <help>
				Console.Out.Write("  {0}", option.Name);
				Console.Out.Write(new string(' ', maxOptionNameWidth - optionNameWidth + 2));
				Console.Out.WriteLine(" : (allowed only in JSON) {0}", option.Help);
			}

			// option data type
			Console.Out.Write("{0}   type: ", indent);
			Console.Out.WriteLine(type);

			// display options, that cannot be used together with current option
			var incompatibleWith = command.GetIncompatibleOptions(option);
			if (incompatibleWith != null)
				Console.Out.WriteLine("{0}   cannot use with: {1}", indent, string.Join(", ", incompatibleWith.Select(o => $"--{o.Name}")));

			if (command.SupportsJSON)
			{
				// option property path in json or "not allowed" text if not supported
				Console.Out.Write("{0}   json: ", indent);
				if (option.AllowInJson)
				{
					if (category != null)
						Console.Out.WriteLine("{0}.{1}", category.JsonProperty, option.Name);
					else
						Console.Out.WriteLine("{0}", option.Name);
				}
				else
					Console.Out.WriteLine("not allowed");
			}

			// print default value (if set) for non-required option
			if (!option.Required)
			{
				if (option is BooleanCliOption booleanOption)
				{
					Console.Out.WriteLine("{0}   default: {1}", indent, booleanOption.Default ? "true" : "false");
					Console.Out.WriteLine("{0}   default (T4 mode): {1}", indent, booleanOption.T4Default ? "true" : "false");
				}

				if (option is StringCliOption stringOption)
				{
					if (stringOption.Default != null)
					{
						if (!stringOption.AllowMultiple)
							Console.Out.WriteLine("{0}   default: {1}", indent, stringOption.Default[0]);
						else
							Console.Out.WriteLine("{0}   default: {1}", indent, string.Join(",", stringOption.Default));
					}

					if (stringOption.T4Default != null)
					{
						if (!stringOption.AllowMultiple)
							Console.Out.WriteLine("{0}   default (T4 mode): {1}", indent, stringOption.T4Default[0]);
						else
							Console.Out.WriteLine("{0}   default (T4 mode): {1}", indent, string.Join(",", stringOption.T4Default));
					}
				}
				else if (option is StringEnumCliOption enumOption)
				{
					var defaults = enumOption.Values.Where(o => o.Default).Select(o => o.Value).ToArray();
					if (defaults.Length > 0)
						Console.Out.WriteLine("{0}   default: {1}", indent, string.Join(", ", defaults));

					defaults = enumOption.Values.Where(o => o.T4Default).Select(o => o.Value).ToArray();
					if (defaults.Length > 0)
						Console.Out.WriteLine("{0}   default (T4 mode): {1}", indent, string.Join(", ", defaults));
				}
				else if (option is NamingCliOption namingOption)
				{
					if (namingOption.Default != null)
						PrintNamingOptionDefaults(indent, "default", namingOption.Default);
					if (namingOption.T4Default != null)
						PrintNamingOptionDefaults(indent, "default (T4 mode)", namingOption.T4Default);
				}
			}

			// for enum-typed option print list of supported values with description
			if (option is StringEnumCliOption enumStringOption)
			{
				Console.Out.WriteLine("{0}   supported values:", indent);
				var maxValueWidth = enumStringOption.Values.Select(_ => _.Value.Length).Max();

				foreach (var value in enumStringOption.Values)
					Console.Out.WriteLine("{0}{0}   {1}{3} : {2}", indent, value.Value, value.Help, new string(' ', maxValueWidth - value.Value.Length));
			}

			// print option CLI and JSON examples if provided
			if (option.Examples != null)
			{
				Console.Out.WriteLine("{0}   examples:", indent);
				foreach (var example in option.Examples)
					Console.Out.WriteLine("{0}{0}   {1}", indent, example);
			}

			if (option.JsonExamples != null)
			{
				Console.Out.WriteLine("{0}   JSON examples:", indent);
				foreach (var example in option.JsonExamples)
					Console.Out.WriteLine("{0}{0}   {1}", indent, example);
			}

			// print detailed option help if provided
			if (option.DetailedHelp != null)
			{
				Console.Out.WriteLine();

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

						Console.Out.Write(indent);
						Console.Out.WriteLine(part);
					}
				}
			}
		}

		private static void PrintNamingOptionDefaults(string indent, string mode, NormalizationOptions options)
		{
			Console.Out.WriteLine("{0}   {1}: {{", indent, mode);

			string value;

			switch (options.Casing)
			{
				case NameCasing.None                 : value = "\"none\""         ; break;
				case NameCasing.Pascal               : value = "\"pascal_case\""  ; break;
				case NameCasing.CamelCase            : value = "\"camel_case\""   ; break;
				case NameCasing.SnakeCase            : value = "\"snake_case\""   ; break;
				case NameCasing.LowerCase            : value = "\"lower_case\""   ; break;
				case NameCasing.UpperCase            : value = "\"upper_case\""   ; break;
				case NameCasing.T4CompatPluralized   : value = "\"t4_pluralized\""; break;
				case NameCasing.T4CompatNonPluralized: value = "\"t4\""           ; break;
				default                              :
					throw new InvalidOperationException($"Unknown casing option: {options.Casing}");
			}

			printJsonProperty(indent, "case", value);

			switch (options.Pluralization)
			{
				case Pluralization.None                 : value = "\"none\""                      ; break;
				case Pluralization.Singular             : value = "\"singular\""                  ; break;
				case Pluralization.Plural               : value = "\"plural\""                    ; break;
				case Pluralization.PluralIfLongerThanOne: value = "\"plural_multiple_characters\""; break;
				default                                 :
					throw new InvalidOperationException($"Unknown pluralization option: {options.Pluralization}");
			}

			printJsonProperty(indent, "pluralization", value);

			printJsonProperty(indent, "prefix", options.Prefix == null ? "null" : $"\"{options.Prefix}\"");
			printJsonProperty(indent, "suffix", options.Suffix == null ? "null" : $"\"{options.Suffix}\"");

			switch (options.Transformation)
			{
				case NameTransformation.SplitByUnderscore: value = "\"split_by_underscore\""; break;
				case NameTransformation.Association      : value = "\"association\""        ; break;
				case NameTransformation.None             : value = "\"none\""               ; break;
				default:
					throw new InvalidOperationException($"Unknown transformation option: {options.Transformation}");
			}

			printJsonProperty(indent, "transformation", value);

			printJsonProperty(indent, "pluralize_if_ends_with_word_only", options.PluralizeOnlyIfLastWordIsText ? "true" : "false");
			printJsonProperty(indent, "ignore_all_caps", options.DontCaseAllCaps ? "true" : "false");
			if (options.MaxUpperCaseWordLength > 1)
				printJsonProperty(indent, "max_uppercase_word_length", options.MaxUpperCaseWordLength.ToString(CultureInfo.InvariantCulture));

			Console.Out.WriteLine("{0}            }}", indent);

			static void printJsonProperty(string padding, string optionName, string value)
			{
				Console.Out.WriteLine("{0}                \"{1}\"{3}: {2},", padding, optionName, value, new string(' ', "pluralize_if_ends_with_word_only".Length - optionName.Length));
			}
		}

		/// <summary>
		/// Print general help (supported commands list).
		/// </summary>
		/// <param name="controller">CLI controller instance.</param>
		/// <param name="unknownArgs">Unrecognized command line arguments for current call.</param>
		private void PrintGeneralHelp(CliController controller, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader();

			PrintBadArgumentsError(unknownArgs);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Usage:");
			Console.Out.WriteLine("        dotnet linq2db [help]: print this help");
			Console.Out.WriteLine("        dotnet linq2db help <command>: print help on specific command");
			Console.Out.WriteLine("        dotnet linq2db <command> [<options>]: execute specific command");

			Console.Out.WriteLine();
			Console.Out.WriteLine("linq2db tool provides database model scaffolding (reverse engineering) of database into Linq To DB (https://linq2db.github.io/) database model classes.");

			// list all commands with short description
			if (controller.Commands.Count > 1)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Supported commands:");

				foreach (var command in controller.Commands)
				{
					if (command != this)
						Console.Out.WriteLine("        dotnet linq2db {0}{1} : {2}", command.Name, command.Template.Length != 0 ? " " + command.Template : command.Template, command.Help);
				}
			}

			// TODO: load command examples from command object?
			// for now it's fine, as we have only one command
			Console.Out.WriteLine();
			Console.Out.WriteLine("Examples:");
			Console.Out.WriteLine("        dotnet linq2db scaffold -o c:\\my_project\\model -p SqlServer -c \"Server=MySqlServer;Database=MyDatabase;User Id=scaffold_user;Password=secret;\"");
			Console.Out.WriteLine("            generate data model code for SQL Server database in c:\\my_project\\model folder");
			Console.Out.WriteLine();
			Console.Out.WriteLine("        dotnet linq2db scaffold -i path\\to\\my_scaffold_options.json");
			Console.Out.WriteLine("            generate data model code using options from JSON file");
			Console.Out.WriteLine();
			Console.Out.WriteLine("        dotnet linq2db template");
			Console.Out.WriteLine("            create base T4 template for scaffolding customization in current folder");
		}

		/// <summary>
		/// Prints error message about unrecognized arguments.
		/// </summary>
		/// <param name="unknownArgs">List of unknown arguments.</param>
		private static void PrintBadArgumentsError(IReadOnlyCollection<string> unknownArgs)
		{
			if (unknownArgs.Count > 0)
			{
				// all errors from console must be directed to stderr
				Console.Error.WriteLine();
				Console.Error.WriteLine("Error: unrecognized arguments:");

				foreach (var arg in unknownArgs)
					Console.Error.WriteLine("    {0}", arg);
			}
		}

		/// <summary>
		/// Prints help command header. Used for both general and command-specific help display.
		/// </summary>
		private void PrintHeader()
		{
			Console.Out.WriteLine("dotnet linq2db - Linq To DB command-line utilities. Version: {0}", GetType().Assembly.GetName().Version);
		}
	}
}
