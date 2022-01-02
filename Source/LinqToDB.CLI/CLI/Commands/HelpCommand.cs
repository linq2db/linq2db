using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.CLI
{
	internal sealed class HelpCommand : CliCommand
	{
		public static CliCommand Instance { get; } = new HelpCommand();

		private HelpCommand()
			: base( "help", "[<command>]", "print this help or help on specific command", Array.Empty<CommandExample>())
		{
		}

		public override int Execute(CLIController controller, string[] rawArgs, IReadOnlyCollection<CliOption> options, IReadOnlyCollection<string> unknownArgs)
		{
			if (options.Count == 0 && (rawArgs.Length == 0 || (rawArgs.Length == 1 && rawArgs[0] == "help")))
			{
				// "dotnet linq2db"
				// "dotnet linq2db help"
				PrintGeneralHelp(controller, Array.Empty<string>());
				return StatusCodes.SUCCESS;
			}
			
			if (options.Count == 0 && rawArgs.Length == 2 && rawArgs[0] == "help")
			{
				foreach (var command in controller.Commands)
				{
					if (command != this)
					{
						if (command.Name == rawArgs[1])
						{
							// "dotnet linq2db help <known_command>"
							PrintCommandHelp(command, unknownArgs);
							return StatusCodes.SUCCESS;
						}
					}
				}

				PrintGeneralHelp(controller, new[] { rawArgs[1] });
				return StatusCodes.INVALID_ARGUMENTS;
			}

			// anything else except known command, e.g. "dotnet linq2db scaffold ..."
			PrintGeneralHelp(controller, rawArgs);
			return StatusCodes.INVALID_ARGUMENTS;
		}

		private void PrintCommandHelp(CliCommand command, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader();
			PrintBadArgumentsError(unknownArgs);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Usage:");
			Console.Out.WriteLine("\tdotnet linq2db {0}{1}", command.Name, command.Template != string.Empty ? " " + command.Template : command.Template);

			// options per-category
			Console.Out.WriteLine();
			Console.Out.WriteLine("Options:");

			var maxOptionWidth = 0;
			var maxTypeWidth = 0;

			foreach (var category in command.Categories)
			{
				foreach (var option in command.GetCategoryOptions(category))
				{
					var width = getOptionWidth(option);
					var typeWidth = getTypeName(option).Length;

					if (width > maxOptionWidth)
						maxOptionWidth = width;
					if (typeWidth > maxTypeWidth)
						maxTypeWidth = typeWidth;
				}
			}

			foreach (var category in command.Categories)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("=== {0} : {1}", category.Name, category.Description);
				foreach (var option in command.GetCategoryOptions(category))
				{
					Console.Out.WriteLine();
					Console.Out.Write("   ");
					var width = getOptionWidth(option);
					var type = getTypeName(option);

					if (option.ShortName != null)
						Console.Out.Write("-{0}, ", option.ShortName.Value);

					var padding = "      ";

					Console.Out.Write("--{0}", option.Name);
					Console.Out.Write(new string(' ', maxOptionWidth - width + 2));
					Console.Out.WriteLine(" : {0}", option.Help);
					Console.Out.Write("{0}   type: ", padding);
					Console.Out.WriteLine(type);
					Console.Out.Write("{0}   json: ", padding);
					if (option.AllowInJson)
						Console.Out.WriteLine("{0}.{1}", category.JsonProperty, option.Name);
					else
						Console.Out.WriteLine("not allowed");

					if (!option.Required)
					{
						if (option is BooleanCliOption booleanOption)
							Console.Out.WriteLine("{0}   default: {1}", padding, booleanOption.Default ? "true" : "false");
						if (option is StringCliOption stringOption)
						{
							if (stringOption.Default != null)
							{
								if (!stringOption.AllowMultiple)
									Console.Out.WriteLine("{0}   default: {1}", padding, stringOption.Default[0]);
								else
									Console.Out.WriteLine("{0}   default: {1}", padding, string.Join(", ", stringOption.Default));
							}
						}
						else if (option is StringEnumCliOption enumOption)
						{
							var defaults = enumOption.Values.Where(o => o.Default).Select(o => o.Value).ToArray();
							if (defaults.Length > 0)
								Console.Out.WriteLine("{0}   default: {1}", padding, string.Join(", ", defaults));
						}
					}

					if (option is StringEnumCliOption enumStringOption)
					{
						Console.Out.WriteLine("{0}   supported values:", padding);
						var maxValueWidth = enumStringOption.Values.Select(_ => _.Value.Length).Max();

						foreach (var value in enumStringOption.Values)
							Console.Out.WriteLine("{0}{0}   {1}{3} : {2}", padding, value.Value, value.Help, new string(' ', maxValueWidth - value.Value.Length));
					}

					if (option.Examples != null)
					{
						Console.Out.WriteLine("{0}   examples:", padding);
						foreach (var example in option.Examples)
							Console.Out.WriteLine("{0}{0}   {1}", padding, example);
					}
					if (option.JsonExamples != null)
					{
						Console.Out.WriteLine("{0}   JSON examples:", padding);
						foreach (var example in option.JsonExamples)
							Console.Out.WriteLine("{0}{0}   {1}", padding, example);
					}

					if (option.DetailedHelp != null)
					{
						Console.Out.WriteLine();
						var lines = option.DetailedHelp.Split("\r\n");
						for (var i = 0; i < lines.Length; i++)
						{
							var line = lines[i];

							var lineWidth = Console.BufferWidth - padding.Length - 1;
							var incompleteLineLength = line.Length % lineWidth;
							var partsCount = line.Length / lineWidth + (incompleteLineLength > 0 ? 1 : 0);
							for (var j = 0; j < partsCount; j++)
							{
								var part = line.Substring(
									j * lineWidth,
									j == partsCount - 1 && incompleteLineLength > 0 ? incompleteLineLength : lineWidth);

								Console.Out.Write(padding);
								Console.Out.WriteLine(part);
							}
						}
					}

				}
			}

			if (command.Examples.Length > 0)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Examples:");
				foreach (var example in command.Examples)
				{
					Console.Out.WriteLine();
					Console.Out.WriteLine("\t{0}", example.Command);
					Console.Out.WriteLine("\t\t{0}", example.Help);
				}
			}

			static int getOptionWidth(CliOption option)
			{
				var width = option.Name.Length;
				if (option.ShortName != null)
					width += 4;
				return width;
			}

			static string getTypeName(CliOption option)
			{
				string type;
				switch (option.Type)
				{
					case OptionType.Boolean:
						type = "bool";
						break;
					case OptionType.String:
					case OptionType.StringEnum:
						type = "string";
						break;
					default:
						throw new NotImplementedException($"Option type {option.Type} not implemented");
				}

				if (option.AllowMultiple)
					type += " list";
				return  type + (option.Required ? " (required)" : " (optional)");
			}
		}

		private void PrintGeneralHelp(CLIController controller, IReadOnlyCollection<string> unknownArgs)
		{
			PrintHeader();
			PrintBadArgumentsError(unknownArgs);

			Console.Out.WriteLine();
			Console.Out.WriteLine("Usage:");
			Console.Out.WriteLine("\tdotnet linq2db [help]: print this help");
			Console.Out.WriteLine("\tdotnet linq2db help <command>: print help on specific command");
			Console.Out.WriteLine("\tdotnet linq2db <command> [<options>]: execute specific command");

			Console.Out.WriteLine();
			Console.Out.WriteLine("linq2db tool provides database model scaffolding (reverse engineering) of database into Linq To DB (https://linq2db.github.io/) database model classes.");

			if (controller.Commands.Count > 1)
			{
				Console.Out.WriteLine();
				Console.Out.WriteLine("Supported commands:");
				foreach (var command in controller.Commands)
				{
					if (command != this)
					{
						Console.Out.WriteLine("\tdotnet linq2db {0}{1} : {2}", command.Name, command.Template != string.Empty ? " " + command.Template : command.Template, command.Help);
					}
				}
			}

			Console.Out.WriteLine();
			Console.Out.WriteLine("Examples:");
			Console.Out.WriteLine("\tdotnet linq2db scaffold -o c:\\my_project\\model -p SqlServer -c \"Server=MySqlServer;Database=MyDatabase;User Id=scaffold_user;Password=secret;\"");
			Console.Out.WriteLine("\tdotnet linq2db scaffold -i path\\to\\my_scaffold_options.json");
		}

		private static void PrintBadArgumentsError(IReadOnlyCollection<string> unknownArgs)
		{
			// error
			if (unknownArgs.Count > 0)
			{
				Console.Error.WriteLine();
				Console.Error.WriteLine("Error: unrecognized arguments:");
				foreach (var arg in unknownArgs)
				{
					Console.Error.WriteLine("\t{0}", arg);
				}
			}
		}

		private void PrintHeader()
		{
			// header
			Console.Out.WriteLine("dotnet linq2db - Linq To DB command-line utilities. Version: {0}", GetType().Assembly.GetName().Version);
		}
	}
}
