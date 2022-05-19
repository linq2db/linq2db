namespace LinqToDB.CommandLine;

/// <summary>
/// CLI controller for current utility.
/// </summary>
internal sealed class LinqToDBCliController : CliController
{
	public LinqToDBCliController()
		: base(HelpCommand.Instance)
	{
		// for now (and probably in future) we have only three commands only
		AddCommand(HelpCommand.Instance);
		AddCommand(ScaffoldCommand.Instance);
		AddCommand(TemplateCommand.Instance);
	}
}
