using LinqToDB.CommandLine;

namespace LinqToDB.Tools;

internal static class Program
{
	private static int Main(string[] args)
	{
		try
		{
			return new LinqToDBCliController().Execute(args);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Unhandled exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
			return StatusCodes.INTERNAL_ERROR;
		}
	}
}
