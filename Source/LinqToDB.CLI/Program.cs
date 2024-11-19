using System;
using LinqToDB.CommandLine;

namespace LinqToDB.Tools
{
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
				Console.Error.WriteLine($"Unhandled exception: {ex.Message}");

				var iex = ex.InnerException;
				while (iex != null)
				{
					Console.Error.WriteLine($"\t{iex.Message}");
					iex = iex.InnerException;
				}

				Console.Error.WriteLine($"{ex.StackTrace}");
				
				return StatusCodes.INTERNAL_ERROR;
			}
		}
	}
}
