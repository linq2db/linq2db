using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	/// <summary>
	/// Disposable wrapper over <see cref="DbDataReader"/> instance, which properly disposes associated objects.
	/// </summary>
	public class DataReaderWrapper : IDisposable
#if !NETFRAMEWORK
		, IAsyncDisposable
#endif
	{
		private          bool            _disposed;
		private readonly DataConnection? _dataConnection;

		internal DataReaderWrapper(DbDataReader dataReader)
		{
			DataReader = dataReader;
		}

		internal DataReaderWrapper(DataConnection dataConnection, DbDataReader dataReader, DbCommand? command)
		{
			_dataConnection = dataConnection;
			DataReader      = dataReader;
			Command         = command;
		}

		public  DbDataReader? DataReader { get; private set; }
		internal DbCommand?   Command    { get; }

		internal Action<DbCommand>? OnBeforeCommandDispose { get; set; }

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (DataReader != null)
			{
				DataReader.Dispose();
				DataReader = null;
			}

			if (Command != null)
			{
				OnBeforeCommandDispose?.Invoke(Command);
				OnBeforeCommandDispose = null;

				if (_dataConnection != null)
					_dataConnection.DataProvider.DisposeCommand(Command);
				else
					Command.Dispose();
			}
		}

#if NETSTANDARD2_1PLUS
		public async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (DataReader != null)
			{
				await DataReader.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				DataReader = null;
			}

			if (Command != null)
			{
				OnBeforeCommandDispose?.Invoke(Command);

				if (_dataConnection != null)
					_dataConnection.DataProvider.DisposeCommand(Command);
				else
					Command.Dispose();
			}
		}
#elif !NETFRAMEWORK
		public ValueTask DisposeAsync()
		{
			Dispose();
			return new ValueTask(Task.CompletedTask);
		}
#endif
	}
}
