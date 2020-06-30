using LinqToDB.Async;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable, IAsyncDisposable
	{
		IDataReader DataReader { get; }
		Task<bool>  ReadAsync(CancellationToken cancellationToken);
	}
}
