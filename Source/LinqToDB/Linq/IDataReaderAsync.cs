using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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
