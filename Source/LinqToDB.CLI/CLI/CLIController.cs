using System;
using System.Collections.Generic;

namespace LinqToDB.CLI
{
	internal abstract class CLIController
	{
		private readonly CliCommand? _defaultCommand;

		private readonly List<CliCommand> _commands = new ();
		public IReadOnlyCollection<CliCommand> Commands => _commands;

		public CLIController(CliCommand? defaultCommand)
		{
			_defaultCommand = defaultCommand;
		}

		protected void AddCommand(CliCommand command)
		{
			_commands.Add(command);
		}

		public virtual int Execute(string[] args)
		{
			if (args.Length == 0)
			{
				if (_defaultCommand != null)
				{
					return _defaultCommand.Execute(this, args, Array.Empty<CliOption>(), args);
				}

				return StatusCodes.SUCCESS;
			}
			else
			{
				var commandName = args[0];

				foreach (var command in _commands)
				{
					if (command.Name == commandName)
					{
						// TODO: parse options
						var unknownArgs = new List<string>();
						var options = new List<CliOption>();
						return command.Execute(this, args, options, unknownArgs);
					}
				}
			}

			if (_defaultCommand != null)
			{
				return _defaultCommand.Execute(this, args, Array.Empty<CliOption>(), args);
			}

			return StatusCodes.INVALID_ARGUMENTS;
		}
	}
}
