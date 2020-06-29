using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	public interface IDataReaderAsync : IDisposable
#if NETSTANDARD2_1 || NETCOREAPP3_0
		, IAsyncDisposable
#endif
	{
		IDataReader DataReader { get; }
		Task<bool>  ReadAsync(CancellationToken cancellationToken);
	}
}
