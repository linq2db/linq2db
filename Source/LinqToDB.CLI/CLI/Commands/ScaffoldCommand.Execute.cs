using System;
using System.Collections.Generic;

namespace LinqToDB.CLI
{
	partial class ScaffoldCommand : CliCommand
	{
		public override int Execute(
			CLIController controller,
			string[] rawArgs,
			IReadOnlyDictionary<CliOption, object?> options,
			IReadOnlyCollection<string> unknownArgs)
		{
			// TODO: validate options and create ScaffoldOptions from it
			// TODO: restart util when process architecture doesn't match requested one
			// TODO: perform scaffolding

			Console.Out.WriteLine("TODO: scaffold not implemented");
			return -100500;
		}
	}
}
