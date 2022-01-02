using System;
using System.Collections.Generic;

namespace LinqToDB.CLI
{
	partial class ScaffoldCommand : CliCommand
	{
		public override int Execute(CLIController controller, string[] rawArgs, IReadOnlyCollection<CliOption> options, IReadOnlyCollection<string> unknownArgs)
		{
			Console.Out.WriteLine("TODO: scaffold not implemented");
			return StatusCodes.TODO;
		}
	}
}
