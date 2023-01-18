using System;
using System.Data.Common;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable,
#if NATIVE_ASYNC
		IAsyncDisposable
#else
		Async.IAsyncDisposable
#endif
	{
		DbDataReader DataReader { get; }
		Task<bool>   ReadAsync(CancellationToken cancellationToken);
	}
}
