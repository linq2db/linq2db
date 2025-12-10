using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Base class for CLI controller.
	/// </summary>
	internal abstract class CliController
	{
		/// <summary>
		/// Optional default command to execute when unknown or no command specified by caller.
		/// </summary>
		private readonly CliCommand?                       _defaultCommand;
		/// <summary>
		/// List of supported commands.
		/// </summary>
		private readonly Dictionary<string, CliCommand>    _commands = new ();

		/// <summary>
		/// Gets list of supported commands.
		/// </summary>
		public IReadOnlyCollection<CliCommand> Commands => _commands.Values;

		/// <summary>
		/// Creates controller instance.
		/// </summary>
		/// <param name="defaultCommand">Optional default command.</param>
		protected CliController(CliCommand? defaultCommand)
		{
			_defaultCommand = defaultCommand;
		}

		/// <summary>
		/// Register command handler.
		/// </summary>
		/// <param name="command">Command to register.</param>
		protected void AddCommand(CliCommand command)
		{
			_commands.Add(command.Name, command);
		}

		/// <summary>
		/// Process CLI arguments and invoke corresponding command.
		/// </summary>
		/// <param name="args">Raw CLI arguments.</param>
		/// <returns>Command execution status code.</returns>
		public virtual ValueTask<int> Execute(string[] args)
		{
			if (args.Length == 0)
			{
				// no arguments specified - invoke default command (if set)
				if (_defaultCommand != null)
					return _defaultCommand.Execute(this, args, new Dictionary<CliOption, object?>(), args);

				return new(StatusCodes.SUCCESS);
			}
			else
			{
				var commandName = args[0];

				if (_commands.TryGetValue(commandName, out var command))
				{
					var unknownArgs = new List<string>();

					Dictionary<CliOption, object?>? options = null;
					if (command.HasOptions)
					{
						(options, var hasErrors) = ParseCommandOptions(command, args, unknownArgs, true);
						if (hasErrors)
							return new(StatusCodes.INVALID_ARGUMENTS);
					}

					return command.Execute(this, args, options ?? new(), unknownArgs);
				}
			}

			// cannot find matching command - invoke default command (if set)
			if (_defaultCommand != null)
				return _defaultCommand.Execute(this, args, new Dictionary<CliOption, object?>(), args);

			return new(StatusCodes.INVALID_ARGUMENTS);
		}

		private (Dictionary<CliOption, object?> options, bool hasErrors) ParseCommandOptions(CliCommand command, string[] args, List<string> unknownArgs, bool reportFirstErrorOnly)
		{
			var hasErrors          = false;
			var cliOptions         = new Dictionary<CliOption, object?>();
			var conflictingOptions = new HashSet<CliOption>();

			// arg[0] is command name
			for (var i = 1; i < args.Length; i++)
			{
				CliOption? option = null;

				// detect option
				if (args[i].StartsWith("--"))
				{
					var name = args[i].Substring(2);
					option = command.GetOptionByName(name);
					if (option == null)
					{
						unknownArgs.Add(args[i]);
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Unrecognized option: {0}", args[i]);
						hasErrors = true;
					}
				}
				else if (args[i] is ['-', var name])
				{
					option = command.GetOptionByShortName(name);
					if (option == null)
					{
						unknownArgs.Add(args[i]);
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Unrecognized option: {0}", args[i]);
						hasErrors = true;
					}
				}
				else
				{
					hasErrors = true;
					if (!hasErrors || !reportFirstErrorOnly)
						Console.Error.WriteLine("Unrecognized argument: {0}", args[i]);
					unknownArgs.Add(args[i]);
				}

				if (option != null)
				{
					if (cliOptions.ContainsKey(option))
					{
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Duplicate option: {0}", args[i]);
						hasErrors = true;
					}
					else if (conflictingOptions.Contains(option))
					{
						var incompatibleOptions = command.GetIncompatibleOptions(option)!;
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Option '{0}' conflicts with other option(s): {1}", args[i], string.Join(", ", incompatibleOptions.Select(o => $"--{o.Name}")));
						hasErrors = true;
					}
					else if (!option.AllowInCli)
					{
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Option '{0}' not allowed in command line", args[i]);
						hasErrors = true;
					}
					else if (args.Length == i + 1)
					{
						// currently all options has exactly one argument
						if (!hasErrors || !reportFirstErrorOnly)
							Console.Error.WriteLine("Option '{0}' must have value", args[i]);
						hasErrors = true;
					}
					else
					{
						i++;

						var incompatibleOptions = command.GetIncompatibleOptions(option);
						if (incompatibleOptions != null)
						{
							foreach (var opt in incompatibleOptions)
								conflictingOptions.Add(opt);
						}

						var value = option.ParseCommandLine(command, args[i], out var errorMessage);
						if (value == null)
						{
							if (!hasErrors || !reportFirstErrorOnly)
								Console.Error.WriteLine("Cannot parse option value ({0} {1}): {2}", args[i - 1], args[i], errorMessage);
							hasErrors = true;
						}

						cliOptions.Add(option, value);
					}
				}
			}

			// merge JSON and CLI options into single set
			Dictionary<CliOption, object?>? options = null;
			foreach (var option in cliOptions)
			{
				if (option.Key is ImportCliOption && option.Value != null)
					options = MergeOptions(options, (Dictionary<CliOption, object?>)option.Value);
			}

			options = MergeOptions(options, cliOptions);

			// validate required options set
			foreach (var option in command.AllOptions)
			{
				// this is the reason why we require option to add null value on parse error:
				// to not trigger additional "required" error
				if (option.Required && !options.ContainsKey(option))
				{
					if (!hasErrors || !reportFirstErrorOnly)
						Console.Error.WriteLine("Required option '{0}' not specified", option.Name);
					hasErrors = true;
				}
			}

			return (options, hasErrors);
		}

		/// <summary>
		/// Merge two option sets into single set. When both sets contain same option - value from <paramref name="second"/> set used.
		/// Method doesn't create new set but updates <paramref name="first"/> set with values from <paramref name="second"/> set.
		/// </summary>
		/// <param name="first">First set of options. Could be <c>null</c>.</param>
		/// <param name="second">Second set of options.</param>
		/// <returns>Merged options.</returns>
		private Dictionary<CliOption, object?> MergeOptions(Dictionary<CliOption, object?>? first, Dictionary<CliOption, object?> second)
		{
			if (first == null)
				return second;

			foreach (var value in second)
				first[value.Key] = value.Value;

			return first;
		}
	}
}
