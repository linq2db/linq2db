namespace LinqToDB.CLI
{
	/// <summary>
	/// CLI controller for current utility.
	/// </summary>
	internal sealed class LinqToDBCLIController : CLIController
	{
		public LinqToDBCLIController()
			: base(HelpCommand.Instance)
		{
			// for now (and probably in future) we have only three commands only
			AddCommand(HelpCommand.Instance);
			AddCommand(ScaffoldCommand.Instance);
			AddCommand(TemplateCommand.Instance);
		}
	}
}
