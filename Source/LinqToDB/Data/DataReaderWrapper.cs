using System;
using System.Data.Common;
using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Metrics;

namespace LinqToDB.Data
{
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

		// An extra object disposed after the reader (and command): the DbBatch backing a batched combined command, so
		// the batch is released with the reader. Disposed asynchronously in DisposeAsync when it is also IAsyncDisposable.
		internal IDisposable? AdditionalDisposable { get; set; }

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (DataReader != null)
			{
				if (_dataConnection is IInterceptable<ICommandInterceptor> { Interceptor: {} interceptor })
					using (ActivityService.Start(ActivityID.CommandInterceptorBeforeReaderDispose))
						interceptor.BeforeReaderDispose(new (_dataConnection), Command, DataReader);

				DataReader.Dispose();
				DataReader = null;
			}

			if (Command != null)
			{
				OnBeforeCommandDispose?.Invoke(Command);
				OnBeforeCommandDispose = null;

				// if command set, _dataConnection is set too
				_dataConnection!.DataProvider.DisposeCommand(Command);
			}

			if (AdditionalDisposable != null)
			{
				AdditionalDisposable.Dispose();
				AdditionalDisposable = null;
			}

			if (((IDataContext?)_dataConnection)?.CloseAfterUse == true)
				_dataConnection.Close();
		}

		public async ValueTask DisposeAsync()
		{
			if (_disposed)
				return;

			_disposed = true;

			if (DataReader != null)
			{
				if (_dataConnection is IInterceptable<ICommandInterceptor> { Interceptor: {} interceptor })
					await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInterceptorBeforeReaderDisposeAsync))
						await interceptor.BeforeReaderDisposeAsync(new(_dataConnection), Command, DataReader)
							.ConfigureAwait(false);

				await DataReader.DisposeAsync().ConfigureAwait(false);
				DataReader = null;
			}

			if (Command != null)
			{
				OnBeforeCommandDispose?.Invoke(Command);

				// if command set, _dataConnection is set too
				await _dataConnection!.DataProvider.DisposeCommandAsync(Command).ConfigureAwait(false);
			}

			if (AdditionalDisposable != null)
			{
				if (AdditionalDisposable is IAsyncDisposable asyncDisposable)
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				else
					AdditionalDisposable.Dispose();

				AdditionalDisposable = null;
			}

			if (((IDataContext?)_dataConnection)?.CloseAfterUse == true)
				await _dataConnection.CloseAsync().ConfigureAwait(false);
		}
	}
}
