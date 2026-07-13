using System;

using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Commands.ConfigInit;
using LinqToDB.CommandLine.Commands.Execute;
using LinqToDB.CommandLine.Commands.Help;
using LinqToDB.CommandLine.Commands.Mcp;
using LinqToDB.CommandLine.Commands.Query;
using LinqToDB.CommandLine.Commands.Schema;
using LinqToDB.CommandLine.Commands.Scaffold;
using LinqToDB.CommandLine.Commands.Skill;
using LinqToDB.CommandLine.Commands.Template;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// CLI controller for current utility.
	/// </summary>
	public sealed class LinqToDBCliController : CliController
	{
		/// <summary>
		/// Creates a controller with all linq2db CLI commands registered.
		/// </summary>
		public LinqToDBCliController()
			: base(HelpCommand.Instance)
		{
			AddCommand(HelpCommand.Instance);
			AddCommand(ConfigInitCommand.Instance);
			AddCommand(ExecuteCommand.Instance);
			AddCommand(McpCommand.Instance);
			AddCommand(QueryCommand.Instance);
			AddCommand(SchemaCommand.Instance);
			AddCommand(SkillCommand.Skill);
			AddCommand(SkillCommand.Skills);
			AddCommand(ScaffoldCommand.Instance);
			AddCommand(TemplateCommand.Instance);
		}
	}
}
