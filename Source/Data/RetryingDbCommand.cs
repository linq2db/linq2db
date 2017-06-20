using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Data
{
	internal class RetryingDbCommand : DbCommand
	{
		private readonly DbCommand _command;
		private readonly IRetryPolicy _policy;

		public RetryingDbCommand(DbCommand command, IRetryPolicy policy)
		{
			_command = command;
			_policy = policy;
		}

		public override void Prepare()
		{
			_command.Prepare();
		}

		public override string CommandText
		{
			get { return _command.CommandText; }
			set { _command.CommandText = value; }
		}

		public override int CommandTimeout
		{
			get { return _command.CommandTimeout; }
			set { _command.CommandTimeout = value; }
		}

		public override CommandType CommandType
		{
			get { return _command.CommandType; }
			set { _command.CommandType = value; }
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get { return _command.UpdatedRowSource; }
			set { _command.UpdatedRowSource = value; }
		}

		protected override DbConnection DbConnection
		{
			get { return _command.Connection; }
			set { _command.Connection = value; }
		}

		protected override DbParameterCollection DbParameterCollection
		{
			get { return _command.Parameters; }
		}

		protected override DbTransaction DbTransaction
		{
			get { return _command.Transaction; }
			set { _command.Transaction = value; }
		}

		public override bool DesignTimeVisible
		{
			get { return _command.DesignTimeVisible; }
			set { _command.DesignTimeVisible = value; }
		}

		public override void Cancel()
		{
			_policy.Execute(
				() =>
				{
					_command.Cancel();
					return 0;
				});
		}

		protected override DbParameter CreateDbParameter()
		{
			return _command.CreateParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return _policy.Execute(() => _command.ExecuteReader(behavior));
		}

		public override int ExecuteNonQuery()
		{
			return _policy.Execute(() => _command.ExecuteNonQuery());
		}

		public override object ExecuteScalar()
		{
			return _policy.Execute(() => _command.ExecuteScalar());
		}

#if !NOASYNC
		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteReaderAsync(behavior, ct), cancellationToken);
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteNonQueryAsync(ct), cancellationToken);
		}

		public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			return _policy.ExecuteAsync(ct => _command.ExecuteScalarAsync(ct), cancellationToken);
		}
#endif
	}
}