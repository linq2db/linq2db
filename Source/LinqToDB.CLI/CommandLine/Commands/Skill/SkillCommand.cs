using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Prints agent-oriented Markdown instructions for linq2db CLI usage.
	/// </summary>
	internal sealed class SkillCommand : CliCommand
	{
		public static CliCommand Skill  { get; } = new SkillCommand("skill");
		public static CliCommand Skills { get; } = new SkillCommand("skills");

		private SkillCommand(string name)
			: base(name, false, false, string.Empty, "print agent instructions in Markdown format", Array.Empty<CommandExample>())
		{
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			if (rawArgs.Length != 1)
			{
				await environment.Error.WriteLineAsync($"Command '{Name}' doesn't accept arguments.").ConfigureAwait(false);
				return StatusCodes.INVALID_ARGUMENTS;
			}

			var markdown = SkillResource.ReadMarkdown();

			await environment.Out.WriteAsync(markdown.AsMemory(), cancellationToken).ConfigureAwait(false);
			return StatusCodes.SUCCESS;
		}
	}
}
