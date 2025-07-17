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
				await Console.Error.WriteLineAsync($"Unhandled exception: {ex.Message}").ConfigureAwait(false);

				var iex = ex.InnerException;
				while (iex != null)
				{
					await Console.Error.WriteLineAsync($"\t{iex.Message}").ConfigureAwait(false);
					iex = iex.InnerException;
				}

				await Console.Error.WriteLineAsync($"{ex.StackTrace}").ConfigureAwait(false);
				
				return StatusCodes.INTERNAL_ERROR;
			}
		}
	}
}
