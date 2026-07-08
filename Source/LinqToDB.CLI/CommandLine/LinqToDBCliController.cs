using System;

using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Commands.Help;
using LinqToDB.CommandLine.Commands.Query;
using LinqToDB.CommandLine.Commands.Scaffold;
using LinqToDB.CommandLine.Commands.Skill;
using LinqToDB.CommandLine.Commands.Template;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// CLI controller for current utility.
	/// </summary>
	internal sealed class LinqToDBCliController : CliController
	{
		public LinqToDBCliController()
			: base(HelpCommand.Instance)
		{
			AddCommand(HelpCommand.Instance);
			AddCommand(QueryCommand.Instance);
			AddCommand(SkillCommand.Skill);
			AddCommand(SkillCommand.Skills);
			AddCommand(ScaffoldCommand.Instance);
			AddCommand(TemplateCommand.Instance);
		}
	}
}
