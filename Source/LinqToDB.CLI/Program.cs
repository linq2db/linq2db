using System;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

namespace LinqToDB.Tools
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			try
			{
				return await new LinqToDBCliController().Execute(args).ConfigureAwait(false);
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
