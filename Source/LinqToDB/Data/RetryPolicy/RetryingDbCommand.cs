using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Metrics;

namespace LinqToDB.Data.RetryPolicy
{
	sealed class RetryingDbCommand : DbCommand
	{
		readonly DataConnection _dataConnection;
		readonly DbCommand      _command;
		readonly IRetryPolicy   _policy;

		public RetryingDbCommand(DataConnection dataConnection, DbCommand command, IRetryPolicy policy)
		{
			_dataConnection = dataConnection;
			_command        = command;
			_policy         = policy;
		}

		public override void Prepare()
		{
			_command.Prepare();
		}

		[AllowNull]
		public override string CommandText
		{
			get => _command.CommandText;
			set => _command.CommandText = value;
		}

		public override int CommandTimeout
		{
			get => _command.CommandTimeout;
			set => _command.CommandTimeout = value;
		}

		public override CommandType CommandType
		{
			get => _command.CommandType;
			set => _command.CommandType = value;
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get => _command.UpdatedRowSource;
			set => _command.UpdatedRowSource = value;
		}

		protected override DbConnection? DbConnection
		{
			get => _command.Connection;
			set => _command.Connection = value;
		}

		protected override DbParameterCollection DbParameterCollection => _command.Parameters;

		protected override DbTransaction? DbTransaction
		{
			get => _command.Transaction;
			set => _command.Transaction = value;
		}

		public override bool DesignTimeVisible
		{
			get => _command.DesignTimeVisible;
			set => _command.DesignTimeVisible = value;
		}

		public override void Cancel()
		{
			_policy.Execute(_command.Cancel);
		}

		protected override DbParameter CreateDbParameter()
		{
			return _command.CreateParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			using var m = ActivityService.Start(ActivityID.CommandExecuteReader)?.AddQueryInfo(_dataConnection, _command.Connection, _command);
			return _policy.Execute(() => _command.ExecuteReader(behavior));
		}

		public override int ExecuteNonQuery()
		{
			using var m = ActivityService.Start(ActivityID.CommandExecuteNonQuery)?.AddQueryInfo(_dataConnection, _command.Connection, _command);
			return _policy.Execute(_command.ExecuteNonQuery);
		}

		public override object? ExecuteScalar()
		{
			using var m = ActivityService.Start(ActivityID.CommandExecuteScalar)?.AddQueryInfo(_dataConnection, _command.Connection, _command);
			return _policy.Execute(_command.ExecuteScalar);
		}

		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteReaderAsync)?.AddQueryInfo(_dataConnection, _command.Connection, _command);

			if (a is null)
				return _policy.ExecuteAsync(ct => _command.ExecuteReaderAsync(behavior, ct), cancellationToken);

			return CallAwaitUsing();

			async Task<DbDataReader> CallAwaitUsing()
			{
				await using (a)
					return await _policy.ExecuteAsync(ct => _command.ExecuteReaderAsync(behavior, ct), cancellationToken)
						.ConfigureAwait(false);
			}
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteNonQueryAsync)?.AddQueryInfo(_dataConnection, _command.Connection, _command);

			if (a is null)
				return _policy.ExecuteAsync(_command.ExecuteNonQueryAsync, cancellationToken);

			return CallAwaitUsing();

			async Task<int> CallAwaitUsing()
			{
				await using (a)
					return await _policy.ExecuteAsync(_command.ExecuteNonQueryAsync, cancellationToken)
						.ConfigureAwait(false);
			}
		}

		public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			var a = ActivityService.StartAndConfigureAwait(ActivityID.CommandExecuteScalarAsync)?.AddQueryInfo(_dataConnection, _command.Connection, _command);

			if (a is null)
				return _policy.ExecuteAsync(_command.ExecuteScalarAsync, cancellationToken);

			return CallAwaitUsing();

			async Task<object?> CallAwaitUsing()
			{
				await using (a)
					return await _policy.ExecuteAsync(_command.ExecuteScalarAsync, cancellationToken)
						.ConfigureAwait(false);
			}
		}

		public DbCommand UnderlyingObject => _command;
	}
}
