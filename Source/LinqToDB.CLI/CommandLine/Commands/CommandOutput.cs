using System;
using System.IO;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine.Commands
{
	internal sealed class CommandOutput : IAsyncDisposable
	{
		readonly ICliEnvironment _environment;
		readonly string?         _outputFile;
		readonly string?         _temporaryFile;
		bool                     _writerDisposed;
		bool                     _committed;

		CommandOutput(ICliEnvironment environment, string? outputFile)
		{
			_environment = environment;
			_outputFile  = outputFile;

			if (outputFile == null)
			{
				Writer = environment.Out;
				return;
			}

			var directory = Path.GetDirectoryName(outputFile);
			var fileName  = Path.GetFileName(outputFile);

			do
			{
				_temporaryFile = Path.Combine(directory ?? string.Empty, $".{fileName}.{Guid.NewGuid():N}.tmp");
			}
			while (environment.FileExists(_temporaryFile));

			Writer = environment.CreateTextWriter(_temporaryFile);
		}

		public TextWriter Writer { get; }

		public static CommandOutput Create(ICliEnvironment environment, string? outputFile)
		{
			return new(environment, outputFile);
		}

		public async ValueTask<bool> Commit(bool overwrite)
		{
			if (_temporaryFile == null || _outputFile == null)
				return true;

			await DisposeWriter().ConfigureAwait(false);

			try
			{
				_environment.MoveFile(_temporaryFile, _outputFile, overwrite);
				_committed = true;
				return true;
			}
			catch (IOException) when (!overwrite && _environment.FileExists(_outputFile))
			{
				return false;
			}
		}

		public async ValueTask DisposeAsync()
		{
			await DisposeWriter().ConfigureAwait(false);

			if (!_committed && _temporaryFile != null)
			{
				try
				{
					_environment.DeleteFile(_temporaryFile);
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					await _environment.Error.WriteLineAsync($"Cannot delete temporary output file '{_temporaryFile}': {ex.Message}").ConfigureAwait(false);
				}
			}
		}

		async ValueTask DisposeWriter()
		{
			if (_writerDisposed || _temporaryFile == null)
				return;

			_writerDisposed = true;
			await Writer.DisposeAsync().ConfigureAwait(false);
		}
	}
}
