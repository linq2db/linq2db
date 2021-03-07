using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.SqlServer
{
	public sealed class SqlServerRawTransaction : RawTransaction
	{
		private DataConnectionTransaction? _marsTransaction;

		public SqlServerRawTransaction(DataConnection dataConnection)
			: base(dataConnection)
		{
		}

		public override RawTransaction BeginTransaction()
		{
			// explicit transaction management requires single batch when used over MARS-enabled connection
			// https://docs.microsoft.com/en-us/archive/blogs/cbiyikoglu/mars-transactions-and-sql-error-3997-3988-or-3983
			// in theory we can refactor eager load in future to support MARS batching to unblock this scenario
			if (DataConnection.IsMarsEnabled)
				_marsTransaction = DataConnection.BeginTransaction();
			else
				DataConnection.Execute("BEGIN TRAN");
			return this;
		}

		protected override void RollbackTransaction()
		{
			if (DataConnection.IsMarsEnabled)
				_marsTransaction!.Rollback();
			else
				DataConnection.Execute("ROLLBACK TRAN");
		}

		public override async Task<RawTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
		{
			if (DataConnection.IsMarsEnabled)
				_marsTransaction = await DataConnection.BeginTransactionAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			else
				await DataConnection.ExecuteAsync("BEGIN TRAN", cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

			return this;
		}

#if !NETFRAMEWORK
		protected override async ValueTask RollbackTransactionAsync()
		{
			if (DataConnection.IsMarsEnabled)
				await _marsTransaction!.RollbackAsync()
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			else
				await DataConnection.ExecuteAsync("ROLLBACK TRAN")
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
#endif
	}
}
