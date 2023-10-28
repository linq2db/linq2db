using System;
using System.Data.Common;
using System.Threading.Tasks;
#if NATIVE_ASYNC
using IAsyncDisposable = System.IAsyncDisposable;
#else
using IAsyncDisposable = LinqToDB.Async.IAsyncDisposable;
#endif

namespace LinqToDB.Data
{
	using Interceptors;

	/// <summary>
	/// Disposable wrapper over <see cref="DbDataReader"/> instance, which properly disposes associated objects.
	/// </summary>
	public class DataReaderWrapper : IDisposable, IAsyncDisposable
	{
		private          bool            _disposed;
		private readonly DataConnection? _dataConnection;

		/// <summary>
		/// Creates wrapper instance for specified data reader.
		/// </summary>
		/// <param name="dataReader">Wrapped data reader instance.</param>
		public DataReaderWrapper(DbDataReader dataReader)
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
				if (_dataConnection is IInterceptable<ICommandInterceptor> interceptable)
					interceptable.Interceptor?.BeforeReaderDispose(new (_dataConnection), Command, DataReader);

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

#if NATIVE_ASYNC
		public async ValueTask DisposeAsync()
#else
		public async Task DisposeAsync()
#endif
		{
			if (_disposed)
				return;

			_disposed = true;

			if (DataReader != null)
			{
				if (_dataConnection is IInterceptable<ICommandInterceptor> interceptable && interceptable.Interceptor != null)
					await interceptable.Interceptor.BeforeReaderDisposeAsync(new(_dataConnection), Command, DataReader).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

#if NETSTANDARD2_1PLUS
				await DataReader.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#else
				DataReader.Dispose();
#endif
				DataReader = null;
			}

			if (Command != null)
			{
				OnBeforeCommandDispose?.Invoke(Command);

				if (_dataConnection != null)
				{
#if NETSTANDARD2_1PLUS
					await _dataConnection.DataProvider.DisposeCommandAsync(Command).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#else
					_dataConnection.DataProvider.DisposeCommand(Command);
#endif
				}
				else
				{
#if NETSTANDARD2_1PLUS
					await Command.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
#else
					Command.Dispose();
#endif
				}
			}
		}
	}
}
