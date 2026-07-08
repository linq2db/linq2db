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

			ConsoleCancelEventHandler cancelHandler = (_, e) =>
			{
				e.Cancel = true;
				cancellation.Cancel();
			};

			Console.CancelKeyPress += cancelHandler;

			try
			{
				return await new LinqToDBCliController().Execute(args, SystemCliEnvironment.Instance, cancellation.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				await Console.Error.WriteLineAsync("Command cancelled.").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
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
			finally
			{
				Console.CancelKeyPress -= cancelHandler;
			}
		}
	}
}
