using System;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

namespace LinqToDB.Tools
{
	internal static class Program
	{
		private static async Task<int> Main(string[] args)
		{
			using var cancellation = new CancellationTokenSource();

			void CancelHandler(object? sender, ConsoleCancelEventArgs e)
			{
				e.Cancel = true;
				cancellation.Cancel();
			}

			Console.CancelKeyPress += CancelHandler;

			try
			{
				return await new LinqToDBCliController().Execute(args, SystemCliEnvironment.Instance, cancellation.Token);
			}
			catch (OperationCanceledException)
			{
				await Console.Error.WriteLineAsync("Command cancelled.");
				return StatusCodes.EXPECTED_ERROR;
			}
			catch (Exception ex)
			{
				await Console.Error.WriteLineAsync($"Unhandled exception: {ex.Message}");

				var iex = ex.InnerException;
				while (iex != null)
				{
					await Console.Error.WriteLineAsync($"\t{iex.Message}");
					iex = iex.InnerException;
				}

				await Console.Error.WriteLineAsync($"{ex.StackTrace}");

				return StatusCodes.INTERNAL_ERROR;
			}
			finally
			{
				Console.CancelKeyPress -= CancelHandler;
			}
		}
	}
}
