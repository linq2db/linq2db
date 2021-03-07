using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace LinqToDB.DataProvider
{
	/// <summary>
	/// This is internal API and is not intended for use by Linq To DB applications.
	/// It may change or be removed without further notice.
	/// </summary>
	public abstract class RawTransaction : IDisposable
#if !NETFRAMEWORK
		, IAsyncDisposable
#endif
	{
		protected readonly DataConnection DataConnection;

		protected RawTransaction(DataConnection dataConnection)
		{
			DataConnection = dataConnection;
		}

		public abstract RawTransaction BeginTransaction();
		public abstract Task<RawTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

		protected abstract void RollbackTransaction();

		void IDisposable.Dispose() => RollbackTransaction();

#if !NETFRAMEWORK
		protected abstract ValueTask RollbackTransactionAsync();
		ValueTask IAsyncDisposable.DisposeAsync() => RollbackTransactionAsync();
#endif
	}
}
