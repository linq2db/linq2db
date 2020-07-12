using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data.RetryPolicy
{
	using Configuration;
	using DbCommandProcessor;

	class RetryingDbCommand : DbCommand, IProxy<DbCommand>
	{
		readonly DbCommand    _command;
		readonly IRetryPolicy _policy;

		public RetryingDbCommand(DbCommand command, IRetryPolicy policy)
		{
			_command = command;
			_policy  = policy;
		}

		public override void Prepare()
		{
			_command.Prepare();
		}

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

		protected override DbConnection DbConnection
		{
			get => _command.Connection;
			set => _command.Connection = value;
		}

		protected override DbParameterCollection DbParameterCollection => _command.Parameters;

		protected override DbTransaction DbTransaction
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
			return _policy.Execute(() => _command.ExecuteReaderExt(behavior));
		}

		public override int ExecuteNonQuery()
		{
			return _policy.Execute(() => _command.ExecuteNonQueryExt());
		}

		public override object? ExecuteScalar()
		{
			return _policy.Execute(() => _command.ExecuteScalarExt());
		}

		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteReaderExtAsync(behavior, ct), cancellationToken);
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteNonQueryExtAsync(ct), cancellationToken);
		}

		public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteScalarExtAsync(ct), cancellationToken);
		}

		public DbCommand UnderlyingObject => _command;
	}
}
